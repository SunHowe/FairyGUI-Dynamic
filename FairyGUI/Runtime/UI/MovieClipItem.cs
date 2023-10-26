using System;

namespace FairyGUI
{
    public class MovieClipItem
    {
        public float interval { get; private set; }
        public float repeatDelay { get; private set; }
        public bool swing { get; private set; }
        public MovieClip.Frame[] frames { get; private set; }
        public int refCount { get; private set; }

        public event Action<MovieClipItem> OnAcquire;
        public event Action<MovieClipItem> OnRelease;

        public MovieClipItem(float interval, float repeatDelay, bool swing, MovieClip.Frame[] frames)
        {
            this.interval = interval;
            this.repeatDelay = repeatDelay;
            this.swing = swing;
            this.frames = frames;
            foreach (var frame in frames)
            {
                if (frame.texture == null)
                    continue;
                
                frame.texture.AddRef();
            }
        }

        public void Dispose()
        {
            foreach (var frame in frames)
            {
                if (frame.texture == null)
                    continue;
                
                frame.texture.ReleaseRef();
            }
            
            frames = null;
        }

        public void AddRef()
        {
            if (frames == null) 
                return; // already disposed
            
            ++refCount;
            
            if (refCount == 1)
                OnAcquire?.Invoke(this);
        }
        
        public void ReleaseRef()
        {
            if (frames == null) 
                return; // already disposed
            
            if (refCount == 0)
                return;
            
            --refCount;
            
            if (refCount == 0)
                OnRelease?.Invoke(this);
        }
    }
}