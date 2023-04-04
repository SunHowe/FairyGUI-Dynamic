using System;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI.Dynamic
{
    public partial class UIAssetManager
    {
        public sealed class UIPackageInfo
        {
            /// <summary>
            /// 版本号
            /// </summary>
            public uint Version { get; set; }

            /// <summary>
            /// 包名
            /// </summary>
            public string PackageName { get; set; }

            /// <summary>
            /// UI包实例
            /// </summary>
            public UIPackage UIPackage { get; private set; }

            /// <summary>
            /// 是否加载已完成
            /// </summary>
            public bool IsLoadDone => UIPackage != null;

            /// <summary>
            /// 是否存在引用
            /// </summary>
            public bool IsAnyReference => ReferenceCount > 0;

            /// <summary>
            /// 引用数量
            /// </summary>
            public int ReferenceCount { get; private set; }

            /// <summary>
            /// 依赖的包名
            /// </summary>
            public IEnumerable<string> DependencePackageNames => m_DependencePackageNames;

            /// <summary>
            /// 添加引用
            /// </summary>
            public void AddRef()
            {
                ++ReferenceCount;
            }

            /// <summary>
            /// 移除引用
            /// </summary>
            public void RemoveRef()
            {
                --ReferenceCount;
            }

            /// <summary>
            /// 添加回调
            /// </summary>
            public void AddCallback(Action<UIPackage> callback)
            {
                if (callback == null)
                    return;

                if (IsLoadDone)
                    callback.Invoke(UIPackage);
                else
                    m_QueueCallbacks.Enqueue(callback);
            }

            /// <summary>
            /// 设置UIPackage
            /// </summary>
            public void SetUIPackage(UIPackage package)
            {
                if (UIPackage != null)
                    throw new Exception("重复设置UIPackage");

                UIPackage = package;

                while (m_QueueCallbacks.Count > 0)
                    m_QueueCallbacks.Dequeue().Invoke(UIPackage);

                if (UIPackage == null)
                    return;

                foreach (var dependency in UIPackage.dependencies)
                {
                    if (!dependency.TryGetValue("name", out var name))
                        continue;

                    if (string.IsNullOrEmpty(name))
                        continue;

                    if (string.Equals(name, PackageName))
                        continue;

                    m_DependencePackageNames.Add(name);
                }
            }

            public void Reset()
            {
                Version = 0;
                PackageName = string.Empty;
                UIPackage = null;
                ReferenceCount = 0;
                m_QueueCallbacks.Clear();
                m_DependencePackageNames.Clear();
            }

            private readonly Queue<Action<UIPackage>> m_QueueCallbacks = new Queue<Action<UIPackage>>();
            private readonly List<string> m_DependencePackageNames = new List<string>();
        }

        private void AddUIPackageInner(string packageName, Action<UIPackage> callback, bool addRef)
        {
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
            {
                info = m_PoolUIPackageInfos.Count > 0 ? m_PoolUIPackageInfos.Dequeue() : new UIPackageInfo();
                info.Version = ++m_Version;
                info.PackageName = packageName;

                m_DictUIPackageInfos.Add(packageName, info);

                if (m_AssetLoader == null)
                    throw new Exception("请设置AssetLoader");

                var version = info.Version;

                m_AssetLoader.LoadUIPackageAsync(packageName, (bytes, prefix) => { OnUIPackageLoadFinished(packageName, version, bytes, prefix); });
            }

            if (addRef)
                info.AddRef();

            info.AddCallback(callback);
        }

        private void ReleaseUIPackageInner(UIPackageInfo info)
        {
            info.RemoveRef();

            // 判断是否要卸载UIPackage
            if (info.IsAnyReference || !UnloadUnusedUIPackageImmediately)
                return;

            UnloadUIPackageInner(info);
        }

        private void UnloadUIPackageInner(UIPackageInfo info)
        {
            m_DictUIPackageInfos.Remove(info.PackageName);

            if (info.IsLoadDone)
            {
                // 解除对依赖包的引用
                foreach (var dependencePackageName in info.DependencePackageNames)
                    ReleaseUIPackage(dependencePackageName);

                if (UIPackage.GetByName(info.PackageName) != null)
                    UIPackage.RemovePackage(info.PackageName);
            }
            else
            {
                // 设置加载失败
                info.SetUIPackage(null);
            }

            info.Reset();
            m_PoolUIPackageInfos.Enqueue(info);
        }

        private void OnUIPackageLoadFinished(string packageName, uint version, byte[] bytes, string assetNamePrefix)
        {
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;

            if (info.Version != version)
                return;

            if (info.IsLoadDone)
                return;

            var package = bytes != null && bytes.Length > 0 ? UIPackage.AddPackage(bytes, assetNamePrefix, LoadResourceAsync) : null;

            if (package == null)
            {
                // 加载失败 卸载对应包
                UnloadUIPackageInner(info);
            }
            else
            {
                // 加载成功 设置UIPackage
                info.SetUIPackage(package);

                // 对依赖的包添加引用
                foreach (var dependencePackageName in info.DependencePackageNames)
                    AddUIPackageInner(dependencePackageName, null, true);
            }
        }

        private void LoadResourceAsync(string name, string extension, Type type, PackageItem item)
        {
            var packageName = item.owner.name;
            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;

            var version = info.Version;

            if (type == typeof(Texture))
            {
                if (m_AssetLoader == null)
                    throw new Exception("请设置AssetLoader");

                m_AssetLoader.LoadTextureAsync(packageName, name, extension, texture =>
                {
                    if (texture == null)
                        return;

                    if (!m_DictUIPackageInfos.TryGetValue(packageName, out var newInfo) || newInfo.Version != version)
                    {
                        // 对应UIPackage已经被卸载 直接释放该资源
                        m_AssetLoader.ReleaseTexture(texture);
                        return;
                    }

                    info.AddRef();
                    item.owner.SetItemAsset(item, texture, DestroyMethod.Custom);
                    item.texture.onRelease -= OnTextureRelease;
                    item.texture.onRelease += OnTextureRelease;
                    m_NTexture2PackageNames[item.texture.GetHashCode()] = packageName;
                });
            }
            else if (type == typeof(AudioClip))
            {
                if (m_AssetLoader == null)
                    throw new Exception("请设置AssetLoader");

                m_AssetLoader.LoadAudioClipAsync(packageName, name, extension, audioClip =>
                {
                    if (audioClip == null)
                        return;

                    if (!m_DictUIPackageInfos.TryGetValue(packageName, out var newInfo) || newInfo.Version != version)
                    {
                        // 对应UIPackage已经被卸载 直接释放该资源
                        m_AssetLoader.ReleaseAudioClip(audioClip);
                        return;
                    }

                    info.AddRef();
                    item.owner.SetItemAsset(item, audioClip, DestroyMethod.Custom);
                    item.audioClip.onRelease -= OnAudioClipRelease;
                    item.audioClip.onRelease += OnAudioClipRelease;
                    m_NAudioClip2PackageNames[item.audioClip.GetHashCode()] = packageName;
                });
            }
        }

        private void OnTextureRelease(NTexture nTexture)
        {
            nTexture.onRelease -= OnTextureRelease;

            var hashCode = nTexture.GetHashCode();
            if (!m_NTexture2PackageNames.TryGetValue(hashCode, out var packageName))
                return;

            m_NTexture2PackageNames.Remove(hashCode);
            ReleaseUIPackage(packageName);
        }

        private void OnAudioClipRelease(NAudioClip nAudioClip)
        {
            nAudioClip.onRelease -= OnAudioClipRelease;

            var hashCode = nAudioClip.GetHashCode();
            if (!m_NAudioClip2PackageNames.TryGetValue(hashCode, out var packageName))
                return;

            m_NAudioClip2PackageNames.Remove(hashCode);
            ReleaseUIPackage(packageName);
        }

        private void OnUIPackageAcquire(UIPackage uiPackage, string _)
        {
            var packageName = uiPackage.name;

            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;

            if (info.UIPackage != uiPackage)
                return;

            info.AddRef();
        }

        private void OnUIPackageRelease(UIPackage uiPackage, string _)
        {
            var packageName = uiPackage.name;

            if (!m_DictUIPackageInfos.TryGetValue(packageName, out var info))
                return;

            if (info.UIPackage != uiPackage)
                return;

            ReleaseUIPackageInner(info);
        }

        private uint m_Version;
        private readonly Queue<UIPackageInfo> m_PoolUIPackageInfos = new Queue<UIPackageInfo>();
        private readonly Queue<UIPackageInfo> m_Buffer = new Queue<UIPackageInfo>();
        private readonly Queue<string> m_Buffer2 = new Queue<string>();

        private readonly Dictionary<string, UIPackageInfo> m_DictUIPackageInfos = new Dictionary<string, UIPackageInfo>();
        private readonly Dictionary<int, string> m_NTexture2PackageNames = new Dictionary<int, string>();
        private readonly Dictionary<int, string> m_NAudioClip2PackageNames = new Dictionary<int, string>();
    }
}