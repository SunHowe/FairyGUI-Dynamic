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

   