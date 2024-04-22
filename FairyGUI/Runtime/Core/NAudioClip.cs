using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// 
    /// </summary>
    public class NAudioClip
    {
        public static Action<AudioClip> CustomDestroyMethod;

        /// <summary>
        /// 
        /// </summary>
        public DestroyMethod destroyMethod;

        /// <summary>
        /// 
        /// </summary>
        public AudioClip nativeClip;

        /// <summary>
        /// 
        /// </summary>
        public int refCount;

        /// <summary>
        /// This event will trigger when ref count is not zero.
        /// </summary>
        public event Action<NAudioClip> onAcquire; 

        /// <summary>
        /// This event will trigger when ref count is zero.
        /// </summary>
        public event Action<NAudioClip> onRelease;
        
        /// <summary>
        /// This event will trigger when texture is disposing.
        /// </summary>
        public event Action<NAudioClip> onDispose;
        
        /// <summary>
        /// NAudioClip instance id.
        /// </summary>
        public int instanceID { get; private set; }
        
        private static int _instanceIDIncrease = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioClip"></param>
        public NAudioClip(AudioClip audioClip)
        {
            nativeClip = audioClip;
            instanceID = ++_instanceIDIncrease;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unload()
        {
            if (nativeClip == null)
                return;

            if (destroyMethod == DestroyMethod.Unload)
                Resources.UnloadAsset(nativeClip);
            else if (destroyMethod == DestroyMethod.Destroy)
                UnityEngine.Object.DestroyImmediate(nativeClip, true);
            else if (destroyMethod == DestroyMethod.Custom)
            {
                if (CustomDestroyMethod == null)
                    Debug.LogWarning("NAudioClip.CustomDestroyMethod must be set to handle DestroyMethod.Custom");
                else
                    CustomDestroyMethod(nativeClip);
            }

            nativeClip = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioClip"></param>
        public void Reload(AudioClip audioClip)
        {
            if (nativeClip != null && nativeClip != audioClip)
                Unload();

            nativeClip = audioClip;
        }

        public void AddRef()
        {
            refCount++;
            
            if (refCount == 1)
                onAcquire?.Invoke(this);
        }

        public void ReleaseRef()
        {
            refCount--;

            if (refCount != 0) 
                return;

            onRelease?.Invoke(this);
        }

        public void Dispose()
        {
            onDispose?.Invoke(this);
            
            Unload();
            
            onAcquire = null;
            onRelease = null;
            onDispose = null;
        }
#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeOnLoad()
        {
            CustomDestroyMethod = null;
        }
#endif
    }
}
