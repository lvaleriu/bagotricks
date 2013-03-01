using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SilverlightCompLib.Mathematics
{
    public class Animations
    {
        public FrameworkElement el { get; set; }
        public double XFrom { get; set; }
        public double YFrom { get; set; }
        public double XTo { get; set; }
        public double YTo { get; set; }
        public double OpFrom { get; set; }
        public double OpTo { get; set; }

        public event EventHandler onCompleted;

        public Animations(FrameworkElement element, int Fps, double from, double to)
            : this(element, Fps, from, from, to, to, from, to)
        {

        }
        public Animations(FrameworkElement element, int Fps, double xfrom, double yfrom, double xto, double yto, double opfrom, double opto)
        {
            el = element;
            _fps = Fps;
            XFrom = xfrom;
            XTo = xto;
            YFrom = yfrom;
            YTo = yto;
            OpFrom = opfrom;
            OpTo = opto;

            _scaleTransform = new ScaleTransform();
            // start the enter frame event
            _storyBoard = new Storyboard();            
            if (_fps != 0)
                _storyBoard.Duration = TimeSpan.FromMilliseconds(1000 / _fps);
            _storyBoard.Completed += new EventHandler(_storyBoard_Completed);
        }

        // start the animation and enter frame event
        public void startAnimation()
        {
            //_scaleTransform = (el as Graph.Graphic.GraphContentPresenter)._scaleTransform;
            
            _scaleTransform.ScaleX = XFrom;
            _scaleTransform.ScaleY = YFrom;

            el.RenderTransform = _scaleTransform;

            el.Opacity = OpFrom;
            _storyBoard.Begin();
        }

        void _storyBoard_Completed(object sender, EventArgs e)
        {
            _temp = _temp * DECAY + (XTo - _scaleTransform.ScaleX) * SPRINGNESS;
            if (System.Math.Abs(_temp) < LIMIT) // Save CPU time
            {
                _scaleTransform.ScaleX = XTo;
                _scaleTransform.ScaleY = YTo;
                el.RenderTransform = _scaleTransform;

                el.Opacity = OpTo;
                _storyBoard.Stop();
                if (onCompleted != null) onCompleted(sender, e);
            }
            else
            {
                _scaleTransform.ScaleX += _temp;
                _scaleTransform.ScaleY += _temp;
                el.RenderTransform = _scaleTransform;
                el.Opacity += _temp;
                _storyBoard.Begin();
            }
        }

        private static double SPRINGNESS = 0.1;	// Control the return Speed
        private static double DECAY = 0.1;		// Control the bounce Speed
        private static double LIMIT = 0.005;	// Save CPU time. Smaller value means smoother animation

        private double _temp = 0;				// Temporary Variable for calculating spring effect	
        Storyboard _storyBoard;                 // on enter frame simulator
        private int _fps = 24;                  // fps of the on enter frame event

        ScaleTransform _scaleTransform;
    }
}
