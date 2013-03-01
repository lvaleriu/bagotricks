using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace SilverlightCompLib.Mathematics
{
    /// <summary>
    /// This class is taken from the following blog post:
    /// http://silverlightrocks.com/community/blogs/silverlight_games_101/archive/2008/03/28/the-start-of-a-game-helper-class-library.aspx
    /// </summary>
    public class StoryboardGameLoop : GameLoop
    {
        bool stopped = true;
        Storyboard gameLoop = new Storyboard();

        public StoryboardGameLoop(FrameworkElement parent)
            : this(parent, 0)
        {

        }

        public StoryboardGameLoop(FrameworkElement parent, double milliseconds)
        {
            gameLoop.Duration = TimeSpan.FromMilliseconds(milliseconds);
            gameLoop.SetValue(FrameworkElement.NameProperty, "gameloop");
            parent.Resources.Add("GameLoop", gameLoop);
            gameLoop.Completed += new EventHandler(gameLoop_Completed);
        }

        public override void Start()
        {
            stopped = false;
            gameLoop.Begin();
            base.Start();
        }

        public override void Stop()
        {
            stopped = true;
            base.Stop();
        }

        void gameLoop_Completed(object sender, EventArgs e)
        {
            if (stopped) return;
            base.Tick();
            (sender as Storyboard).Begin();
        }
    }
}
