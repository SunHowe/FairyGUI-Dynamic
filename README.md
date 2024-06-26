# FairyGUI-Dynamic
基于引用计数，为FairyGUI的UIPackage与相关UI资源提供动态加载、卸载的功能

**注意: 该分支与主干采用完全不一样的机制实现，所以与主干的接口有大量差异，使用时可自行选择用哪种版本（推荐使用该分支版本，完全不用关心UIPackage的加载流程）。**

## Feature
1. 全面覆盖UIPackage与相关UI资源的加载与卸载

    通过Hook UIPackage中与加载相关的接口实现，需要使用配套的FairyGUI源码，在完成初始化后，管理器内部将自动判断是否要加载新资源，外部使用无感知，也不再需要手动加载Package。

2. 基于引用计数的UIPackage自动加载与卸载功能

    默认在某个UIPackage引用计数归零时，会自动进行卸载，也可以通过初始化时传入的IUIAssetManagerConfiguration实例来指定关闭自动卸载，在合适的时机（例如场景切换时）通过`UIPackage.RemoveUnusedPackages()`接口来卸载当前引用计数为0的包与相关资源。

3. 便于拓展的设计

    可结合自己项目的资源加载模块自行实现并传入IUIAssetLoader接口实例，例如Resources、AssetBundle、Addressables、YooAsets等。

## Usage
1. git clone或手动下载工程到项目的Packges路径下，为manifest.json文件加入一行`"com.howegame.fariygui.dynamic": "file:FairyGUI-Dynamic"`，例如:

    ```json
    {
        "dependencies": {
            "com.howegame.fariygui.dynamic": "file:FairyGUI-Dynamic",
            "其他的依赖": ""
        }
    }
    ```

2. 在使用任意FairyGUI之前，构造并初始化`IUIAssetManager`，在需要销毁时，调用它的`Dispose`方法，例如以下的初始化脚本（详情可通过导入DemoAssets参考样例代码）

    ```csharp
    public class UIAssetManagerDemo : MonoBehaviour, IUIAssetManagerConfiguration
    {
        private IUIAssetManager m_UIAssetManager;
        
        [SerializeField]
        private UIPackageMapping m_PackageMapping;
    
        [Header("是否立即卸载未使用的UIPackage")]
        public bool unloadUnusedUIPackageImmediately;
    
        private bool m_isQuiting;
    
        private void Awake()
        {
            AssetLoader = new ResourcesUIAssetLoader("UI");
            PackageHelper = m_PackageMapping;
            
            m_UIAssetManager = new UIAssetManager();
            m_UIAssetManager.Initialize(this);
    
            new DynamicLoadWindow().Show();
        }
    
        private void OnDestroy()
        {
            if (m_isQuiting)
                return;
            
            m_UIAssetManager.Dispose();
        }
    
        private void OnApplicationQuit()
        {
            m_isQuiting = true;
        }
    
        public IUIPackageHelper PackageHelper { get; private set; }
        public IUIAssetLoader AssetLoader { get; private set; }
        public bool UnloadUnusedUIPackageImmediately => unloadUnusedUIPackageImmediately;
    }
    ```

3. 由于动态加载需要UIPackage id=>UIPackage Name的映射，所以需要通过传入IUIAssetManagerConfiguration中的IUIPackageHelper实例来完成这项工作，这个实例的实现可以由各自的项目自己完成，亦可通过提供的`UIPackageMappingUtility.GenerateMappingFile(assetsRoot, generatePath)`来生成mapping文件，并在运行时自行加载该mapping文件用于初始化（工具的使用可通过导入DemoAssets参考样例代码）：

   ```csharp
   public static class UIPackageMappingGenerator
   {
       [MenuItem("FairyGUI/Generate Package Mapping")]
       public static void Generate()
       {
           UIPackageMappingUtility.GenerateMappingFile("Assets/Examples/Resources/UI", "Assets/Examples/Resources/UI/UIPackageMapping.asset");
       }
   }
   ```

## 关于小游戏平台的说明

有朋友问我这个问题，因为小游戏平台有要求所有加载必须使用异步加载，而本项目的`UIAssetManager`中，对于动态加载依赖的UIPackage的处理逻辑是采用同步加载的方式。这是由于FairyGUI在创建组件、加载贴图时，需要立即从对应的UIPackage中获取元数据信息（例如宽度、高度等），是无法避免的事情。

而这个问题也有解决方案，在项目的`启动初始化环节`，可以根据`IUIPackageHelper`中记录的信息，将项目中所有UIPackage的二进制提前预加载到内存中，不卸载（或根据业务判定某些确认能卸载的卸载），按照我这一个线上的手游项目来评估，总共222个UIPackage二进制文件，总占用4.52MB，应该在可以接受的范围内。

而用这种方式进行后，可以正常的使用`FairyGUI-Dynamic`提供的卸载无引用UIPackage及相关贴图的方法(`UIPackage.RemoveUnusedPackages()`)，这个方法被调用后，无引用的UIPackage的实例及相关贴图会被销毁、释放，但上述初始化环节预加载的UIPackage二进制数据`不会卸载`，它们依然保留在内存中，供下次加载UIPackage时使用。参考代码：

   ```csharp
    public sealed class MiniGameUIAssetLoader : IUIAssetLoader
    {
        private readonly Dictionary<string, byte[]> m_PackageBytes = new Dictionary<string, byte[]>();
        
        /// <summary>
        /// 预加载所有的UIPackage 在初始化环节调用
        /// </summary>
        public async Task PreLoadAsync(UIPackageMapping mapping, CancellationToken token)
        {
            m_PackageBytes.Clear();
            
            foreach (var packageName in mapping.PackageNames)
            {
                // 使用项目的资源加载方式加载UIPackage二进制数据
                var bytes = await AssetService.inst.globalLoader.LoadRawAsync(GetPackageAssetKey(packageName), token);

                if (token.IsCancellationRequested)
                    return;
                
                m_PackageBytes.Add(packageName, bytes);
            }
        }
        
        public void LoadUIPackageBytes(string packageName, out byte[] bytes, out string assetNamePrefix)
        {
            m_PackageBytes.TryGetValue(packageName, out bytes);
            assetNamePrefix = string.Empty;
        }
            
        private string GetPackageAssetKey(string packageName)
        {
            return packageName + "_fui";
        }
        
        // 其他代码省略
    }

    // 参考的Demo启动器代码
    public class UIAssetManagerDemo : MonoBehaviour, IUIAssetManagerConfiguration
    {
        private IUIAssetManager m_UIAssetManager;
        
        [SerializeField]
        private UIPackageMapping m_PackageMapping;
    
        [Header("是否立即卸载未使用的UIPackage")]
        public bool unloadUnusedUIPackageImmediately;
    
        private bool m_isQuiting;
    
        private async void Awake()
        {
            var miniGameAssetLoader = new MiniGameUIAssetLoader();
            AssetLoader = miniGameAssetLoader;
            PackageHelper = m_PackageMapping;
            
            m_UIAssetManager = new UIAssetManager();
            m_UIAssetManager.Initialize(this);
            
            // 异步初始化环节
            
            // 初始化其他模块 省略相关代码

            // 预加载UIPackage数据
            await miniGameAssetLoader.PreLoadAsync(m_PackageMapping, CancellationToken.None);
            
            // 初始化其他模块 省略相关代码
    
            // 初始化完成 打开首个界面
            new DynamicLoadWindow().Show();
        }
    
        private void OnDestroy()
        {
            if (m_isQuiting)
                return;
            
            m_UIAssetManager.Dispose();
        }
    
        private void OnApplicationQuit()
        {
            m_isQuiting = true;
        }
    
        public IUIPackageHelper PackageHelper { get; private set; }
        public IUIAssetLoader AssetLoader { get; private set; }
        public bool UnloadUnusedUIPackageImmediately => unloadUnusedUIPackageImmediately;
    }

   ```