using System;

namespace FairyGUI.Dynamic
{
    public sealed partial class UIAssetManager : IDisposable
    {
        /// <summary>
        /// 外部主动依赖指定的UI包 引用次数加一
        /// </summary>
        public void AcquireUIPackage(string packageName, Action<UIPackage> callback = null)
        {
            AcquireUIPackageInner(packageName, callback);
        }
        
        /// <summary>
        /// 外部主动释放指定的UI包 引用次数减一
        /// </summary>
        public void ReleaseUIPackage(string packageName)
        {
            ReleaseUIPackageInner(packageName);
        }
        
        /// <summary>
        /// 强制卸载指定的UI包 无视引用次数
        /// </summary>
        public void UnloadUIPackage(string packageName)
        {
            UnloadUIPackageInner(packageName);
        }
        
        /// <summary>
        /// 强制卸载所有UI包 无视引用次数
        /// </summary>
        public void UnloadAllUIPackages()
        {
        }
        
        public void Dispose()
        {
        }
    }
}
