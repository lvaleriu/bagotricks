using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.Generic;

namespace SilverlightCompLib.Mathematics
{
    public class Animate
    {
        private static Dictionary<Storyboard, FrameworkElement> Storyboards = new Dictionary<Storyboard, FrameworkElement>();

        public enum SplineType
        {
            Even,
            FastStart, FastEnd,
            SlowStart, SlowEnd,
            FastStartSlowEnd, FastStartFastEnd,
            SlowStartSlowEnd, SlowStartFastEnd
        };

        public static void AnimateDouble(FrameworkElement frameworkElement, string targetProperty, double startValue, double endValue, Duration seconds)
        {
            AnimateDouble(frameworkElement, targetProperty, startValue, endValue, seconds, SplineType.Even, null);
        }
        public static void AnimateDouble(FrameworkElement frameworkElement, string targetProperty, double startValue, double endValue, Duration seconds, SplineType splineType)
        {
            AnimateDouble(frameworkElement, targetProperty, startValue, endValue, seconds, splineType, null);
        }
        public static void AnimateTranslate(FrameworkElement frameworkElement, double toX, double fromX, double toY, double fromY, Duration seconds)
        {
            Storyboard animation = new Storyboard();
            animation.Duration = seconds;
            Animate.Storyboards.Add(animation, frameworkElement);
            DoubleAnimation yAnimation = new DoubleAnimation();

            Storyboard.SetTarget(yAnimation, ((TransformGroup)frameworkElement.RenderTransform).Children[0]);
            Storyboard.SetTargetProperty(yAnimation, new PropertyPath("Y"));

            yAnimation.Duration = seconds;
            yAnimation.From = fromY;
            yAnimation.To = toY;

            animation.Children.Add(yAnimation);

            DoubleAnimation xAnimation = new DoubleAnimation();
            Storyboard.SetTarget(xAnimation, ((TransformGroup)frameworkElement.RenderTransform).Children[0]);
            Storyboard.SetTargetProperty(xAnimation, new PropertyPath("X"));

            xAnimation.Duration = seconds;
            xAnimation.From = fromX;
            xAnimation.To = toX;

            animation.Children.Add(xAnimation);

            animation.Begin();
        }
        public static void AnimateScale(FrameworkElement frameworkElement, double startValue, double endValue, Duration seconds)
        {
            Storyboard animation = new Storyboard();
            animation.Duration = seconds;
            Animate.Storyboards.Add(animation, frameworkElement);
            DoubleAnimation yAnimation = new DoubleAnimation();

            Storyboard.SetTarget(yAnimation, ((TransformGroup)frameworkElement.RenderTransform).Children[1]);
            Storyboard.SetTargetProperty(yAnimation, new PropertyPath("ScaleY"));

            yAnimation.Duration = seconds;
            yAnimation.From = startValue;
            yAnimation.To = endValue;

            animation.Children.Add(yAnimation);

            DoubleAnimation xAnimation = new DoubleAnimation();
            Storyboard.SetTarget(xAnimation, ((TransformGroup)frameworkElement.RenderTransform).Children[1]);
            Storyboard.SetTargetProperty(xAnimation, new PropertyPath("ScaleX"));

            xAnimation.Duration = seconds;
            xAnimation.From = startValue;
            xAnimation.To = endValue;

            animation.Children.Add(xAnimation);

            animation.Begin();
        }
        public static void AnimateDouble(FrameworkElement frameworkElement, string targetProperty, double startValue, double endValue, Duration seconds, SplineType splineType, EventHandler eventHandlerCompleted)
        {

            try
            {
                Duration duration = seconds;

                DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
                animation.BeginTime = TimeSpan.FromSeconds(0);
                KeySpline keySpline = GetKeySpline(splineType);

                SplineDoubleKeyFrame key = new SplineDoubleKeyFrame();
                key.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
                key.Value = startValue;

                animation.KeyFrames.Add(key);
                key = new SplineDoubleKeyFrame();
                key.KeyTime = KeyTime.FromTimeSpan(seconds.TimeSpan);
                key.KeySpline = keySpline;
                key.Value = endValue;
                animation.KeyFrames.Add(key);

                Storyboard sb = new Storyboard();

                sb.Duration = duration;
                sb.Children.Add(animation);                
                Storyboard.SetTarget(animation, frameworkElement);
                Storyboard.SetTargetProperty(animation, new PropertyPath(targetProperty));

                animation.Completed += new EventHandler(animation_Completed);

                if (eventHandlerCompleted != null)
                {
                    animation.Completed += eventHandlerCompleted;
                }
                
                sb.Completed += new EventHandler(sb_Completed);

                //frameworkElement.Resources.Add(sb);
                Animate.Storyboards.Add(sb, frameworkElement);

                sb.Begin();

            }
            catch (Exception e)
            {

            }
        }

        static void sb_Completed(object sender, EventArgs e)
        {            
            Storyboard storyboard = (sender as Storyboard);
            storyboard.Stop();
            storyboard.Children.Clear();
            FrameworkElement frameworkElement = (Animate.Storyboards[storyboard] as FrameworkElement);
            //System.Diagnostics.Debug.WriteLine("sb_Completed for [" + (frameworkElement as Graph.Graphic.GraphContentPresenter).Content.ToString());
            //frameworkElement.Resources.Remove(storyboard);
            Animate.Storyboards.Remove(storyboard);
        }

        public static void StopAll()
        {

            foreach (Object obj in Animate.Storyboards.Keys)
            {
                Storyboard storyboard = (obj as Storyboard);
                storyboard.Children.Clear();
                FrameworkElement frameworkElement = (Animate.Storyboards[storyboard] as FrameworkElement);
                //frameworkElement.Resources.Remove(storyboard);
            }
            Animate.Storyboards.Clear();
        }

        static void animation_Completed(object sender, EventArgs e)
        {

        }

        public static KeySpline GetKeySpline(SplineType splineType)
        {
            KeySpline ks = new KeySpline();

            switch (splineType)
            {
                case SplineType.FastStart:
                    ks.ControlPoint1 = new Point(0, 0.5); ks.ControlPoint2 = new Point(1, 1);
                    break;
                case SplineType.FastEnd:
                    ks.ControlPoint1 = new Point(0, 0); ks.ControlPoint2 = new Point(1, 0.5);
                    break;
                case SplineType.SlowStart:
                    ks.ControlPoint1 = new Point(0.5, 0); ks.ControlPoint2 = new Point(1, 1);
                    break;
                case SplineType.SlowEnd:
                    ks.ControlPoint1 = new Point(0, 0); ks.ControlPoint2 = new Point(0.5, 1);
                    break;
                case SplineType.FastStartFastEnd:
                    ks.ControlPoint1 = new Point(0, 0.5); ks.ControlPoint2 = new Point(1, 0.5);
                    break;
                case SplineType.FastStartSlowEnd:
                    ks.ControlPoint1 = new Point(0, 0.5); ks.ControlPoint2 = new Point(0.5, 1);
                    break;
                case SplineType.SlowStartFastEnd:
                    ks.ControlPoint1 = new Point(0.5, 0); ks.ControlPoint2 = new Point(1, 0.5);
                    break;
                case SplineType.SlowStartSlowEnd:
                    ks.ControlPoint1 = new Point(0.5, 0); ks.ControlPoint2 = new Point(0.5, 1);
                    break;
                case SplineType.Even:
                default:
                    ks.ControlPoint1 = new Point(0, 0); ks.ControlPoint2 = new Point(1, 1);
                    break;
            }

            return ks;
        }

        public static void AnimateTransform(FrameworkElement frameworkElement, string targetProperty, double? startValue, double endValue, Duration seconds)
        {
            AnimateTransform(frameworkElement, targetProperty, startValue, endValue, seconds, SplineType.Even, null);
        }
        public static void AnimateTransform(FrameworkElement frameworkElement, string targetProperty, double? startValue, double endValue, Duration seconds, SplineType splineType)
        {
            AnimateTransform(frameworkElement, targetProperty, startValue, endValue, seconds, splineType, null);
        }
        public static void AnimateTransform(FrameworkElement frameworkElement, string targetProperty,
                                            double? startValue, double endValue, Duration seconds, SplineType splineType,
                                            EventHandler eventHandlerCompleted)
        {

            int transformIndex = 0;

            //start values
            double scaleX = targetProperty == "(ScaleTransform.ScaleX)" && startValue.HasValue ? startValue.Value : 1.0;
            double scaleY = targetProperty == "(ScaleTransform.ScaleY)" && startValue.HasValue ? startValue.Value : 1.0;
            double translateX = targetProperty == "(TranslateTransform.X)" && startValue.HasValue ? startValue.Value : 0.0;
            double translateY = targetProperty == "(TranslateTransform.Y)" && startValue.HasValue ? startValue.Value : 0.0;
            double angleX = targetProperty == "(SkewTransform.AngleX)" && startValue.HasValue ? startValue.Value : 0.0;
            double angleY = targetProperty == "(SkewTransform.AngleY)" && startValue.HasValue ? startValue.Value : 0.0;
            double angle = targetProperty == "(RotateTransform.Angle)" && startValue.HasValue ? startValue.Value : 0.0;

            TransformGroup tg = frameworkElement.RenderTransform as TransformGroup;
            if (tg == null)
            {
                tg = new TransformGroup();
            }
            //tg.Children.Clear();
            FrameworkElement measureElement = frameworkElement;

            if (double.IsNaN(measureElement.Width) && measureElement is ContentControl)
            {
                measureElement = (FrameworkElement)(measureElement as ContentControl).Content;
            }

            switch (targetProperty)
            {
                case "(ScaleTransform.ScaleX)":
                case "(ScaleTransform.ScaleY)":
                    foreach (Transform t in tg.Children)
                    {
                        string type = t.GetType().ToString();
                        if (type != "System.Windows.Media.ScaleTransform")
                        {
                            transformIndex++;
                        }
                        else
                        {
                            ScaleTransform st = t as ScaleTransform;
                            st.ScaleX = scaleX;
                            st.ScaleY = scaleY;
                            break;
                        }
                    }
                    if (tg.Children.Count == transformIndex)
                    {
                        ScaleTransform st = new ScaleTransform();
                        st.ScaleX = scaleX;
                        st.ScaleY = scaleY;
                        st.CenterX = measureElement.Width / 2.0;
                        st.CenterY = measureElement.Height / 2.0;
                        tg.Children.Add(st);
                    }
                    break;
                case "(SkewTransform.AngleX)":
                case "(SkewTransform.AngleY)":
                    foreach (Transform t in tg.Children)
                    {
                        string type = t.GetType().ToString();
                        if (type != "System.Windows.Media.SkewTransform")
                        {
                            transformIndex++;
                        }
                        else
                        {
                            SkewTransform st = t as SkewTransform;
                            st.AngleX = angleX;
                            st.AngleY = angleY;
                            break;
                        }
                    }
                    if (tg.Children.Count == transformIndex)
                    {
                        SkewTransform st = new SkewTransform();
                        st.AngleX = angleX;
                        st.AngleY = angleY;
                        tg.Children.Add(st);
                    }
                    break;
                case "(RotateTransform.Angle)":
                    foreach (Transform t in tg.Children)
                    {
                        string type = t.GetType().ToString();
                        if (type != "System.Windows.Media.RotateTransform")
                        {
                            transformIndex++;
                        }
                        else
                        {
                            RotateTransform rt = t as RotateTransform;
                            rt.Angle = angle;
                            break;
                        }
                    }
                    if (tg.Children.Count == transformIndex)
                    {
                        RotateTransform rt = new RotateTransform();
                        rt.CenterX = measureElement.Width / 2.0;
                        rt.CenterY = measureElement.Height / 2.0;
                        rt.Angle = angle;
                        tg.Children.Add(rt);
                    }
                    break;
                case "(TranslateTransform.X)":
                case "(TranslateTransform.Y)":
                    foreach (Transform t in tg.Children)
                    {
                        string type = t.GetType().ToString();
                        if (type != "System.Windows.Media.TranslateTransform")
                        {
                            transformIndex++;
                        }
                        else
                        {
                            TranslateTransform tt = t as TranslateTransform;
                            tt.X = translateX;
                            tt.Y = translateY;
                            break;
                        }
                    }
                    if (tg.Children.Count == transformIndex)
                    {
                        TranslateTransform tt = new TranslateTransform();
                        tt.X = translateX;
                        tt.Y = translateY;
                        tg.Children.Add(tt);
                    }
                    break;

                default:
                    return;

            }
            frameworkElement.RenderTransform = tg;

            Duration duration = seconds;

            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.BeginTime = TimeSpan.FromSeconds(0);
            KeySpline keySpline = GetKeySpline(splineType);

            SplineDoubleKeyFrame key = new SplineDoubleKeyFrame();
            key = new SplineDoubleKeyFrame();
            key.KeyTime = KeyTime.FromTimeSpan(duration.TimeSpan);
            key.KeySpline = keySpline;
            key.Value = endValue;
            animation.KeyFrames.Add(key);

            animation.Duration = duration;

            animation.Completed += new EventHandler(animation_Completed);

            if (eventHandlerCompleted != null)
            {
                animation.Completed += eventHandlerCompleted;
            }

            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, frameworkElement);
            string target = "(UIElement.RenderTransform).(TransformGroup.Children)[" + transformIndex + "]." + targetProperty;
            PropertyPath propertyPath = new PropertyPath(target);
            Storyboard.SetTargetProperty(animation, propertyPath);

            //frameworkElement.Resources.Add(sb);
            Animate.Storyboards.Add(sb, frameworkElement);

            sb.Completed += new EventHandler(sb_Completed);

            try
            {
                sb.Begin();
            }
            catch (Exception e)
            {
                string errorMessage = e.Message;
            }
        }
    }
}
