#region

using System;
using System.Windows.Controls;
using System.Windows.Threading;
using SilverlightCompLib.Mathematics;

#endregion

namespace SilverlightCompLib.Graph
{
    //See: http://msdn.microsoft.com/en-us/library/ms748838.aspx
    public static class CompositionTarget
    {
        private static StoryboardGameLoop _GameLoop;
        private static int frameRate;
        private static DispatcherTimer renderTimer;

        static CompositionTarget()
        {
            _FrameRate = 0;
        }

        /// <summary>
        ///     The refrsh rate in frames per second - NOT thread safe
        /// </summary>
        public static int _FrameRate
        {
            get { return frameRate; }
            set
            {
                if (_GameLoop != null)
                {
                    _GameLoop.Stop();
                }
                else
                {
                    if (value == 0)
                        _GameLoop = new StoryboardGameLoop(new Button());
                    else
                        _GameLoop = new StoryboardGameLoop(new Button(), 1000.0/value);

                    _GameLoop.Update += _GameLoop_Update;
                }

                frameRate = value;
                _GameLoop.Start();
            }
        }

        /// <summary>
        ///     The refrsh rate in frames per second - NOT thread safe
        /// </summary>
        public static int FrameRate
        {
            get { return frameRate; }
            set
            {
                if (renderTimer != null)
                {
                    renderTimer.Stop();
                }
                else
                {
                    renderTimer = new DispatcherTimer();
                    renderTimer.Tick += renderTimer_Tick;
                }

                frameRate = value;
                renderTimer.Interval = TimeSpan.FromSeconds(1.0/frameRate);
                renderTimer.Start();
            }
        }

        public static event EventHandler Rendering;
        internal static event EventHandler RenderingComplete;

        /// <summary>
        ///     Initialize and start the simulation.
        /// </summary>
        private static void RunSimulation()
        {
            _GameLoop = new StoryboardGameLoop(new Button(), frameRate);
            _GameLoop.Update += _GameLoop_Update;
            _GameLoop.Start();
        }

        /// <summary>
        ///     This is called by the game loop every frame.
        /// </summary>
        /// <param name="elapsed"></param>
        private static void _GameLoop_Update(TimeSpan elapsed)
        {
            //UpdatePhysics(elapsed);
            renderTimer_Tick(null, null);
        }

        private static void renderTimer_Tick(object sender, EventArgs e)
        {
            //All public classes can hook in here
            EventHandler handler = Rendering;
            if (handler != null)
            {
                //TODO: Populate with the correct values
                handler(null, null);
            }

            //Now inform internal classes they can render
            handler = RenderingComplete;
            if (handler != null)
            {
                handler(null, null);
            }
        }
    }
}