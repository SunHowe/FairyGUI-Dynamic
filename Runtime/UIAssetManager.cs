using System;
using UnityEngine;

namespace FairyGUI.Dynamic
{
    public sealed partial class UIAssetManager : IDisposable
    {
        /// <summary>
        /// 是否立即卸载引用计数为0的UIPackage
        /// </summary>
        public bool UnloadUnusedUIPackageImmediately { get; set; } = true;
        
        private readonly IUIAssetLoader m_AssetLoader;
        private readonly IUIPackageHelper m_UIPackageHelper;

        public UIAssetManager(IUIAssetLoader assetLoader, IUIPackageHelper uiPackageHelper)
        {
            m_AssetLoader = assetLoader;
            m_UIPackageHelper = uiPackageHelper;
            
            NTexture.CustomDestroyMethod = CustomDestroyTexture;
            NAudioClip.CustomDestroyMethod = CustomDestroyAudioClip;
            UIPackage.OnPackageAcquire += OnUIPackageAcquire;
            UIPackage.OnPackageRelease += OnUIPackageRelease;
            UIPanel.GetPackageFunc = GetPackageFunc;

#if UNITY_EDITOR
            Debugger.CreateDebugger(this);
#endif
        }

        public void Dispose()
        {
            UnloadAllUIPackages();
            m_Buffer.Clear();
            m_Buffer2.Clear();
            m_Version = 0;
            m_NTextureAssetRefInfos.Clear();
            m_NAudioClipAssetRefInfos.Clear();
            m_DictUIPackageInfos.Clear();
            m_PoolUIPackageInfos.Clear();
            m_PoolUIAssetRefInfos.Clear();
            
#if UNITY_EDITOR
            Debugger.DestroyDebugger();
#endif
            
            NTexture.CustomDestroyMethod -= CustomDestroyTexture;
            NAudioClip.CustomDestroyMethod -= CustomDestroyAudioClip;
            UIPackage.OnPackageAcquire -= OnUIPackageAcquire;
            UIPackage.OnPackageRelease -= OnUIPackageRelease;
            UIPanel.GetPackageFunc -= GetPackageFunc;
        }

        /// <summary>
        /// 加载指定的UIPackage 不会增加引用计数
        /// </summary>
        public void LoadUIPackageAsync(string packageName, Action<UIPackage> callback = null)
        {
            AddUIPackageInner(packageName, callback, false);
        }

        /// <summary>
        /// 加载指定的UIPackage 并让引用计数+1
        /// </summary>
        public void LoadUIPackageAsyncAndAddRef(string packageName, Action<UIPackage> callback = null)
        {
            AddUIPackageInner(packageName, callback, true);
        }

        /// <summary>
        /// 通过id加载指定的UIPackage 不会增加引用计数
        /// </summary>
        public void LoadUIPackageAsyncById(string id, Action<UIPackage> callback = null)
        {
            var packageName = m_UIPackageHelper.GetPackageNameById(id);
            if (string.IsNullOrEmpty(packageName))
            {
                callback?.Invoke(null);
                return;
            }
            
            AddUIPackageInner(packageName, callback, false);
        }

        /// <summary>
        /// 通过id加载指定的UIPackage 并让引用计数+1
        /// </summary>
        public void LoadUIPackageAsyncAndAddRefById(string id, Action<UIPackage> callback = null)
        {
            var packageName = m_UIPackageHelper.GetPackageNameById(id);
            if (string.IsNullOrEmpty(packageName))
            {
                callback?.Invoke(null);
                return;
            }
            
            AddUIPackageInner(packageName, callback, true);
        }

        /// <summary>
        /// 令指定的UIPackage 引用次数减一
        /// </summary>
        public void ReleaseUIPackage(string packageName)
        {
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;
            
            ReleaseUIPackageInner(info);
        }

        /// <summary>
        /// 通过id令指定的UIPackage 引用次数减一
        /// </summary>
        public void ReleaseUIPackageById(string id)
        {
            var packageName = m_UIPackageHelper.GetPackageNameById(id);
            if (string.IsNullOrEmpty(packageName))
                return;
            
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;
            
            ReleaseUIPackageInner(info);
        }
        
        /// <summary>
        /// 卸载引用计数为0的UIPackage
        /// </summary>
        public void UnloadUnusedUIPackages()
        {
            // 一直遍历到无包可卸载为止
            while (true)
            {
                foreach (var (key, info) in m_DictUIPackageInfos)
                {
                    if (info.IsAnyReference)
                        continue;
                
                    m_Buffer.Enqueue(info);
                    m_Buffer2.Enqueue(key);
                }
                
                if (m_Buffer2.Count == 0)
                    break;
            
                while (m_Buffer2.Count > 0)
                    m_DictUIPackageInfos.Remove(m_Buffer2.Dequeue());
            
                while (m_Buffer.Count > 0)
                    UnloadUIPackageInner(m_Buffer.Dequeue());
            }
        }
        
        /// <summary>
        /// 强制卸载指定的UI包 无视引用次数
        /// </summary>
        public void UnloadUIPackage(string packageName)
        {
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;
            
            UnloadUIPackageInner(info);
        }
        
        /// <summary>
        /// 通过id强制卸载指定的UI包 无视引用次数
        /// </summary>
        public void UnloadUIPackageById(string id)
        {
            var packageName = m_UIPackageHelper.GetPackageNameById(id);
            if (string.IsNullOrEmpty(packageName))
                return;
            
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;
            
            UnloadUIPackageInner(info);
        }
        
        /// <summary>
        /// 强制卸载所有UI包 无视引用次数
        /// </summary>
        public void UnloadAllUIPackages()
        {
            foreach (var info in m_DictUIPackageInfos.Values)
                m_Buffer.Enqueue(info);
            
            m_DictUIPackageInfos.Clear();
            
            while (m_Buffer.Count > 0)
                UnloadUIPackageInner(m_Buffer.Dequeue());
        }

        private void CustomDestroyTexture(Texture texture)
        {
            if (m_AssetLoader == null)
                throw new Exception("请设置AssetLoader");
            
            m_AssetLoader.ReleaseTexture(texture);
        }

        private void CustomDestroyAudioClip(AudioClip audioClip)
        {
            if (m_AssetLoader == null)
                throw new Exception("请设置AssetLoader");
            
            m_AssetLoader.ReleaseAudioClip(audioClip);
        }

        private void GetPackageFunc(string packagePath, string packageName, Action<UIPackage> onComplete)
        {
            LoadUIPackageAsync(packageName, onComplete);
        }
    }
}
