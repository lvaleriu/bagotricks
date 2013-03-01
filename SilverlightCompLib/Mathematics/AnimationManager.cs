using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SilverlightCompLib.Mathematics
{
    public class AnimationManager
    {
        public FrameworkElement parent { get; set; }
        public AnimationManager(FrameworkElement control) { parent = control; }

        public Storyboard GetStoryOpac(FrameworkElement parent, UIElement uielement, System.Windows.Duration ShowDuration)
        {
            // Create Storyboard to hold the animations.  Check to see if the Storyboard exists.
            Storyboard stb = new Storyboard();
            string storyboardName = "animOpac_" + uielement.GetValue(Canvas.NameProperty);
            if (parent.Resources[storyboardName] == null)
            {
                parent.Resources.Add(storyboardName, stb);
            }
            else
            {
                stb = (parent.Resources[storyboardName] as Storyboard);
                stb.Stop();
                stb.Children.Clear();
            }

            DoubleAnimation daOpac = new DoubleAnimation() { From = 0, To = 1, Duration = ShowDuration, FillBehavior = FillBehavior.Stop };   //Added by me  :)
            stb.Duration = ShowDuration;
            stb.Children.Add(daOpac);

            Storyboard.SetTarget(daOpac, parent);
            Storyboard.SetTargetName(daOpac, "fadeInEffect");
            Storyboard.SetTargetProperty(daOpac, new PropertyPath("(UIElement.Opacity)"));

            return stb;
        }

        public Storyboard GetStoryMove(FrameworkElement parent, UIElement uielement, double toX, double toY, System.Windows.Duration ShowDuration)
        {
            // Creae and name the StoryBoard
            Storyboard stb = new Storyboard();
            string storyboardName = "animMove_" + uielement.GetValue(Canvas.NameProperty);
            if (parent.Resources[storyboardName] == null)
            {
                parent.Resources.Add(storyboardName, stb);
            }
            else
            {
                stb = (parent.Resources[storyboardName] as Storyboard);
                stb.Stop();
                stb.Children.Clear();
            }

            // Animate X
            DoubleAnimation daTranslateTransformX = new DoubleAnimation();
            stb.Children.Add(daTranslateTransformX);
            daTranslateTransformX.To = toX;
            daTranslateTransformX.Duration = ShowDuration;
            //daTranslateTransformX.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(daTranslateTransformX, uielement);
            Storyboard.SetTargetProperty(daTranslateTransformX,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));

            // Animate Y
            DoubleAnimation daTranslateTransformY = new DoubleAnimation();
            stb.Children.Add(daTranslateTransformY);
            daTranslateTransformY.To = toY;
            daTranslateTransformY.Duration = ShowDuration;
            //daTranslateTransformY.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(daTranslateTransformY, uielement);
            Storyboard.SetTargetProperty(daTranslateTransformY,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));

            return stb;
        }
        public Storyboard GetStoryHideOpac(FrameworkElement parent, UIElement uielement, System.Windows.Duration HideDuration)
        {
            // Create Storyboard to hold the animations.  Check to see if the Storyboard exists.
            Storyboard stb = new Storyboard();
            string storyboardName = "animHideOpac_" + uielement.GetValue(Canvas.NameProperty);
            if (parent.Resources[storyboardName] == null)
            {
                parent.Resources.Add(storyboardName, stb);
            }
            else
            {
                stb = (parent.Resources[storyboardName] as Storyboard);
                stb.Stop();
                stb.Children.Clear();
            }

            //HideDuration = new Duration(TimeSpan.FromSeconds(1));
            DoubleAnimation daOpac = new DoubleAnimation() { To = 0, Duration = HideDuration, FillBehavior = FillBehavior.Stop };
            stb.Duration = HideDuration;
            stb.Children.Add(daOpac);

            Storyboard.SetTarget(daOpac, parent);
            Storyboard.SetTargetName(daOpac, "fadeOutEffect");
            Storyboard.SetTargetProperty(daOpac, new PropertyPath("(UIElement.Opacity)"));

            return stb;
        }

        public Storyboard GetDissapearAnim(TimeSpan beginTime, TimeSpan endTime, double FromValue, double ToValue, FrameworkElement parent, UIElement uielement)
        {            
            Storyboard stb = GetStoryScale(beginTime, endTime, FromValue, ToValue, parent, uielement);
            DoubleAnimation daOpac = new DoubleAnimation() { To = 0, Duration = endTime - beginTime, FillBehavior = FillBehavior.Stop };
            stb.Duration = new Duration(endTime - beginTime);
            stb.Children.Add(daOpac);

            Storyboard.SetTarget(daOpac, parent);
            Storyboard.SetTargetName(daOpac, "fadeOutEffect");
            Storyboard.SetTargetProperty(daOpac, new PropertyPath("(UIElement.Opacity)"));

            return stb;
        }

        public Storyboard GetStoryScale(TimeSpan beginTime, TimeSpan endTime, double FromValue, double ToValue, FrameworkElement parent, UIElement uielement)
        {
            // Create Storyboard to hold the animations.  Check to see if the Storyboard exists.
            Storyboard stb = new Storyboard();
            string storyboardName = "animScaleCustom_" + uielement.GetValue(FrameworkElement.NameProperty);
            if (parent.Resources[storyboardName] == null)
            {
                parent.Resources.Add(storyboardName, stb);
            }
            else
            {
                stb = (parent.Resources[storyboardName] as Storyboard);
                stb.Stop();
                stb.Children.Clear();
            }

            // Animation for the ScaleX
            DoubleAnimationUsingKeyFrames dx = new DoubleAnimationUsingKeyFrames();
            dx.BeginTime = beginTime;

            SplineDoubleKeyFrame keyframeXFrom = new SplineDoubleKeyFrame();
            keyframeXFrom.KeyTime = beginTime;
            keyframeXFrom.Value = FromValue;
            dx.KeyFrames.Add(keyframeXFrom);

            SplineDoubleKeyFrame keyframeX = new SplineDoubleKeyFrame();
            keyframeX.KeyTime = endTime;
            keyframeX.Value = ToValue;

            dx.KeyFrames.Add(keyframeX);

            stb.Children.Add(dx);
            Storyboard.SetTarget(dx, uielement);
            Storyboard.SetTargetProperty(dx, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));


            // Animation for the ScaleY
            DoubleAnimationUsingKeyFrames dy = new DoubleAnimationUsingKeyFrames();            
            dy.BeginTime = beginTime;

            SplineDoubleKeyFrame keyframeYFrom = new SplineDoubleKeyFrame();
            keyframeYFrom.KeyTime = beginTime;
            keyframeYFrom.Value = FromValue;
            dy.KeyFrames.Add(keyframeYFrom);

            SplineDoubleKeyFrame keyframe = new SplineDoubleKeyFrame();
            keyframe.KeyTime = endTime;
            keyframe.Value = ToValue;

            dy.KeyFrames.Add(keyframe);
            stb.Children.Add(dy);
            Storyboard.SetTarget(dy, uielement);
            Storyboard.SetTargetProperty(dy, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));

            return stb;
        }

        public Storyboard GetStoryScale(FrameworkElement parent, UIElement uielement, Duration dur)
        {
            TimeSpan beginTime = TimeSpan.FromMilliseconds(0);
            TimeSpan endTime = dur.TimeSpan;
            return GetStoryScale(beginTime, endTime, .5, 1, parent, uielement);
        }

        public TransformGroup transformGroup(double x, double y, double angle, double scale)
        {
            TransformGroup tg = new TransformGroup();

            TranslateTransform tt = new TranslateTransform() { X = x, Y = y };
            tg.Children.Add(tt);

            ScaleTransform st = new ScaleTransform() { ScaleX = scale, ScaleY = scale };
            tg.Children.Add(st);

            RotateTransform rt = new RotateTransform() { CenterX = tt.X, CenterY = tt.Y, Angle = angle };
            tg.Children.Add(rt);

            return tg;
        }
    }
}
