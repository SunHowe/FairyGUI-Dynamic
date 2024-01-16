using System;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI.Dynamic
{
    public partial class UIAssetManager
    {
        private void LoadResourceAsync(string name, string extension, Type type, PackageItem item)
        {
            var packageName = item.owner.name;
            var packageRef = FindUIPackageRef(packageName);
            if (packageRef == null)
            {
                // 触发加载失败
                item.owner.SetItemAsset(item, null, DestroyMethod.None);
                return;
            }

            if (m_AssetLoader == null)
                throw new Exception("请设置AssetLoader");

            // 在加载前添加引用 防止加载过程中UIPackage引用为0被卸载
            packageRef.AddRef();

            if (type == typeof(Texture))
            {
                m_AssetLoader.LoadTextureAsync(packageName, name, extension, asset =>
                {
                    var newPackageRef = FindUIPackageRef(packageName);
                    if (newPackageRef != packageRef)
                    {
                        // 如果加载完成后UIPackage引用不是当前引用 则卸载资源
                        DestroyTexture(asset);
                        return;
                    }

                    if (asset == null)
                    {
                        // 加载失败 归还引用
                        packageRef.RemoveRef();
                        item.owner.SetItemAsset(item, null, DestroyMethod.None);
                        return;
                    }

                    // 加载成功
                    m_LoadedTextures.Add(asset);

                    item.owner.SetItemAsset(item, asset, DestroyMethod.Custom);
                    item.texture.onRelease -= OnTextureRelease;
                    item.texture.onRelease += OnTextureRelease;
                    item.texture.onAcquire -= OnTextureAcquire;
                    item.texture.onAcquire += OnTextureAcquire;
                    item.texture.onDispose -= OnTextureDispose;
                    item.texture.onDispose += OnTextureDispose;
                    m_NTextureAssetRefInfos[item.texture.instanceID] = packageRef;

                    if (item.texture.refCount == 0)
                    {
                        // 如果加载完成后引用为0 则归还引用
                        packageRef.RemoveRef();
                    }
                });
            }
            else if (type == typeof(AudioClip))
            {
                m_AssetLoader.LoadAudioClipAsync(packageName, name, extension, asset =>
                {
                    var newPackageRef = FindUIPackageRef(packageName);
                    if (newPackageRef != packageRef)
                    {
                        // 如果加载完成后UIPackage引用不是当前引用 则卸载资源
                        DestroyAudioClip(asset);
                        return;
                    }

                    if (asset == null)
                    {
                        // 加载失败 归还引用
                        packageRef.RemoveRef();
                        item.owner.SetItemAsset(item, null, DestroyMethod.None);
                        return;
                    }

                    // 加载成功
                    m_LoadedAudioClips.Add(asset);

                    item.owner.SetItemAsset(item, asset, DestroyMethod.Custom);
                    item.audioClip.onAcquire -= OnAudioClipAcquire;
                    item.audioClip.onAcquire += OnAudioClipAcquire;
                    item.audioClip.onRelease -= OnAudioClipRelease;
                    item.audioClip.onRelease += OnAudioClipRelease;
                    item.audioClip.onDispose -= OnAudioClipDispose;
                    item.audioClip.onDispose += OnAudioClipDispose;
                    m_NAudioClipAssetRefInfos[item.audioClip.instanceID] = packageRef;

                    if (item.audioClip.refCount == 0)
                    {
                        // 如果加载完成后引用为0 则归还引用
                        packageRef.RemoveRef();
                    }
                });
            }
            else
            {
                // 暂不支持的类型 归还引用
                packageRef.RemoveRef();
            }
        }

        private void OnTextureAcquire(NTexture nTexture)
        {
            if (!m_NTextureAssetRefInfos.TryGetValue(nTexture.instanceID, out var refInfo))
                return;

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;
            
            // 新增引用
            packageRef.AddRef();
        }

        private void OnTextureRelease(NTexture nTexture)
        {
            if (!m_NTextureAssetRefInfos.TryGetValue(nTexture.instanceID, out var refInfo))
                return;

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;

            // 归还引用
            packageRef.RemoveRef();
        }

        private void OnTextureDispose(NTexture nTexture)
        {
            nTexture.onRelease -= OnTextureRelease;
            nTexture.onAcquire -= OnTextureAcquire;
            nTexture.onDispose -= OnTextureDispose;

            if (!m_NTextureAssetRefInfos.TryGetValue(nTexture.instanceID, out var refInfo))
                return;

            m_NTextureAssetRefInfos.Remove(nTexture.instanceID);

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;

            if (nTexture.refCount > 0)
            {
                // 归还引用
                packageRef.RemoveRef();
            }
        }

        private void OnAudioClipAcquire(NAudioClip nAudioClip)
        {
            if (!m_NAudioClipAssetRefInfos.TryGetValue(nAudioClip.instanceID, out var refInfo))
                return;

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;
            
            // 新增引用
            packageRef.AddRef();
        }

        private void OnAudioClipRelease(NAudioClip nAudioClip)
        {
            if (!m_NAudioClipAssetRefInfos.TryGetValue(nAudioClip.instanceID, out var refInfo))
                return;

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;

            // 归还引用
            packageRef.RemoveRef();
        }

        private void OnAudioClipDispose(NAudioClip nAudioClip)
        {
            nAudioClip.onAcquire -= OnAudioClipAcquire;
            nAudioClip.onRelease -= OnAudioClipRelease;
            nAudioClip.onDispose -= OnAudioClipDispose;

            if (!m_NAudioClipAssetRefInfos.TryGetValue(nAudioClip.instanceID, out var refInfo))
                return;

            m_NAudioClipAssetRefInfos.Remove(nAudioClip.instanceID);

            var packageRef = FindUIPackageRef(refInfo.Name);
            if (packageRef != refInfo)
                return;

            if (nAudioClip.refCount > 0)
            {
                // 归还引用
                packageRef.RemoveRef();
            }
        }

        private readonly Dictionary<int, UIPackageRef> m_NTextureAssetRefInfos = new Dictionary<int, UIPackageRef>();
        private readonly Dictionary<int, UIPackageRef> m_NAudioClipAssetRefInfos = new Dictionary<int, UIPackageRef>();
    }
}