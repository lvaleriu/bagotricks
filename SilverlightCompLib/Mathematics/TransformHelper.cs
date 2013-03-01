using System.Windows;
using System.Windows.Media;

namespace SilverlightCompLib.Mathematics
{
    public static class TransformHelper
    {
        public static void SetX(UIElement uielement, double value)
        {
            ((uielement.RenderTransform as TransformGroup).Children[0] as TranslateTransform).X = value;
        }
        public static void SetY(UIElement uielement, double value)
        {
            ((uielement.RenderTransform as TransformGroup).Children[0] as TranslateTransform).Y = value;
        }
        public static double GetX(UIElement uielement)
        {
            return ((uielement.RenderTransform as TransformGroup).Children[0] as TranslateTransform).X;
        }
        public static double GetY(UIElement uielement)
        {
            return ((uielement.RenderTransform as TransformGroup).Children[0] as TranslateTransform).Y;
        }
    }
}
