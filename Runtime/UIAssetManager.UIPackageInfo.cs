using System;
using System.Collections.Generic;

namespace FairyGUI.Dynamic
{
    public partial class UIAssetManager
    {
        private sealed class UIPackageInfo
        {
            public string packageName;
            public int referenceCount;
            public UIPackage uiPackage;
            public readonly List<Action<UIPackage>> callbacks = new List<Action<UIPackage>>();

            public bool isLoadDone => uiPackage != null;

            public void Reset()
            {
                packageName = null;
                referenceCount = 0;
                uiPackage = null;
                callbacks.Clear();
            }
        }

        private void AcquireUIPackageInner(string packageName, Action<UIPackage> callback)
        {
            if (!_dictUIPackageInfos.TryGetValue(packageName, out var info))
            {
                info = _poolUIPackageInfos.Count > 0 ? _poolUIPackageInfos.Dequeue() : new UIPackageInfo();
                info.packageName = packageName;
                _dictUIPackageInfos.Add(packageName, info);
                
                LoadUIPackageAsyncInner(packageName);
            }

            ++info.referenceCount;

            if (callback == null)
                return;
            
            if (info.isLoadDone)
                callback(info.uiPackage);
            else
                info.callbacks.Add(callback);
        }
        
        private void ReleaseUIPackageInner(string packageName)
        {
            if (!_dictUIPackageInfos.TryGetValue(packageName, out var info))
                return;

            --info.referenceCount;
            if (info.referenceCount > 0)
                return;

            // 已加载完的包，直接释放
            UnloadUIPackageInner(packageName);
        }
        
        private void UnloadUIPackageInner(string packageName)
        {
            if (!_dictUIPackageInfos.TryGetValue(packageName, out var info))
                return;
            
            _dictUIPackageInfos.Remove(packageName);

            if (info.isLoadDone)
            {
                // 解除对依赖包的引用
                foreach (var dependency in info.uiPackage.dependencies)
                {
                    if (dependency.TryGetValue("name", out var dependencyPackageName))
                        ReleaseUIPackageInner(dependencyPackageName);
                }

                UIPackage.RemovePackage(packageName);
            }
            else
            {
                // 回调所有等待加载的回调
                foreach (var callback in info.callbacks)
                    callback(null);
            }

            info.Reset();
            _poolUIPackageInfos.Enqueue(info);
        }

        private void LoadUIPackageAsyncInner(string packageName)
        {
        }
        
        private readonly Queue<UIPackageInfo> _poolUIPackageInfos = new Queue<UIPackageInfo>();
        private readonly Dictionary<string, UIPackageInfo> _dictUIPackageInfos = new Dictionary<string, UIPackageInfo>();
    }
}