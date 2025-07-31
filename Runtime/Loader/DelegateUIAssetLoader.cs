using System;
using UnityEngine;

namespace FairyGUI.Dynamic
{
    /// <summary>
    /// 自定义委托的IUIAssetLoader派生类 为不方便实现接口的情况提供功能支持
    /// </summary>
    public sealed class DelegateUIAssetLoader : IUIAssetLoader
    {
        public delegate void LoadUIPackageBytesAsyncHandler(string packageName, LoadUIPackageBytesCallback callback);

        public delegate void LoadUIPackageBytesHandler(string packageName, out byte[] bytes, out string assetNamePrefix);

        public delegate void LoadTextureAsyncHandler(string packageName, string assetName, string extension, LoadTextureCallback callback);

        public delegate void UnloadTextureHandler(Texture texture);

        public delegate void LoadAudioClipAsyncHandler(string packageName, string assetName, string extension, LoadAudioClipCallback callback);

        public delegate void UnloadAudioClipHandler(AudioClip audioClip);
        
#if FAIRYGUI_SPINE
        public delegate void LoadSpineAsyncHandler(string packageName, string assetName, string extension, LoadSpineCallback callback);

        public delegate void UnloadSpineHandler(Spine.Unity.SkeletonDataAsset skeletonDataAsset);
#endif

        public LoadUIPackageBytesAsyncHandler LoadUIPackageBytesAsyncHandlerImpl { get; set; }
        public LoadUIPackageBytesHandler LoadUIPackageBytesHandlerImpl { get; set; }
        public LoadTextureAsyncHandler LoadTextureAsyncHandlerImpl { get; set; }
        public UnloadTextureHandler UnloadTextureHandlerImpl { get; set; }
        public LoadAudioClipAsyncHandler LoadAudioClipAsyncHandlerImpl { get; set; }
        public UnloadAudioClipHandler UnloadAudioClipHandlerImpl { get; set; }
        
#if FAIRYGUI_SPINE
        public LoadSpineAsyncHandler LoadSpineAsyncHandlerImpl { get; set; }

        public UnloadSpineHandler UnloadSpineHandlerImpl { get; set; }
#endif

        public void LoadUIPackageBytesAsync(string packageName, LoadUIPackageBytesCallback callback)
        {
            if (LoadUIPackageBytesAsyncHandlerImpl == null)
                throw new NotImplementedException();

            LoadUIPackageBytesAsyncHandlerImpl(packageName, callback);
        }

        public void LoadUIPackageBytes(string packageName, out byte[] bytes, out string assetNamePrefix)
        {
            if (LoadUIPackageBytesHandlerImpl == null)
                throw new NotImplementedException();
            LoadUIPackageBytesHandlerImpl(packageName, out bytes, out assetNamePrefix);
        }

        public void LoadTextureAsync(string packageName, string assetName, string extension, LoadTextureCallback callback)
        {
            if (LoadTextureAsyncHandlerImpl == null)
                throw new NotImplementedException();
            LoadTextureAsyncHandlerImpl(packageName, assetName, extension, callback);
        }

        public void UnloadTexture(Texture texture)
        {
            if (UnloadTextureHandlerImpl == null)
                throw new NotImplementedException();
            UnloadTextureHandlerImpl(texture);
        }

        public void LoadAudioClipAsync(string packageName, string assetName, string extension, LoadAudioClipCallback callback)
        {
            if (LoadAudioClipAsyncHandlerImpl == null)
                throw new NotImplementedException();

            LoadAudioClipAsyncHandlerImpl(packageName, assetName, extension, callback);
        }

        public void UnloadAudioClip(AudioClip audioClip)
        {
            if (UnloadAudioClipHandlerImpl == null)
                throw new NotImplementedException();

            UnloadAudioClipHandlerImpl(audioClip);
        }
        
#if FAIRYGUI_SPINE
        public void LoadSpineAsync(string packageName, string assetName, string extension, LoadSpineCallback callback)
        {
            if (LoadSpineAsyncHandlerImpl == null)
            {
                throw new NotImplementedException();
            }
            
            LoadSpineAsyncHandlerImpl(packageName, assetName, extension, callback);
        }

        public void UnloadSpine(Spine.Unity.SkeletonDataAsset skeletonDataAsset)
        {
            if (UnloadSpineHandlerImpl == null)
            {
                throw new NotImplementedException();
            }
            
            UnloadSpineHandlerImpl(skeletonDataAsset);
        }
#endif
        
    }
}