# FairyGUI-Dynamic
基于引用计数，为FairyGUI的包管理提供动态加载、卸载的功能

## Feature
1. UIPackage静态依赖自动加载
    
    在FairyGUI编辑器上静态配置的其他包的组件、图片等已支持自动跨包依赖，无需手动加载依赖的Package

2. 基于引用计数的UIPackage自动加载与卸载功能

    默认在某个UIPackage引用计数归零时，会自动进行卸载，也可以通过设置UIAssetManager的UnloadUnusedUIPackageImmediately值，来指定其不自动卸载，然后在合适的时机（比如切场景时），调用UnloadUnusedUIPackages方法进行主动卸载无用的UIPackage

3. 便于拓展的设计

    可结合自己项目的资源加载模块自行实现并传入IUIAssetLoader接口实例，例如Resources、AssetBundle、Addressables、YooAsets等

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

2. 在使用任意FairyGUI之前，实例化`UIAssetManager`，在需要销毁时，调用它的`Dispose`方法，例如以下的初始化脚本

    ```csharp
    public class Init : MonoBehaviour
    {
        private UIAssetManager m_UIAssetManager;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            m_UIAssetManager = new UIAssetManager(new ResourcesUIAssetLoader("UI"), null);
        }

        private void OnDestroy()
        {
            m_UIAssetManager.Dispose();
            m_UIAssetManager = null;
        }
    }
    ```

## RoadMap
1. 运行时动态跨包依赖

    目前存在的问题是，如果运行时动态添加了一个组件或图片，而它所处的包未被当前已经加载的包静态依赖时，会出现找不到它包的情况。这是由于FairyGUI原生的CreateObject接口仅支持同步GetUIPackage，需要在一定范围内改写源码才能实现异步加载。

2. 提供PackageId=>PackageName映射文件的生成工具
    主要用于有AddPackageById用途的情况