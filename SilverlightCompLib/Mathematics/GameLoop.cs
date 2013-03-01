using System;

namespace SilverlightCompLib.Mathematics
{
    /// <summary>
    /// This class is taken from the following blog post:
    /// http://silverlightrocks.com/community/blogs/silverlight_games_101/archive/2008/03/28/the-start-of-a-game-helper-class-library.aspx
    /// </summary>
    public abstract class GameLoop
    {
        protected DateTime lastTick;
        public delegate void UpdateHandler(TimeSpan elapsed);
        public event UpdateHandler Update;

        public void Tick()
        {
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - lastTick;
            lastTick = now;
            if (Update != null) Update(elapsed);
        }

        public virtual void Start()
        {
            lastTick = DateTime.Now;
        }

        public virtual void Stop()
        {
        }
    }
}
