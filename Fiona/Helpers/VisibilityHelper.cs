using Fiona.Core.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Fiona.Helpers
{
    public class VisibilityHelper : DependencyObject
    {
        public static readonly DependencyProperty VisibilityOnHoverProperty = DependencyProperty.RegisterAttached("VisibilityOnHover", typeof(UIElement), typeof(VisibilityHelper), new PropertyMetadata(null, VisibilityOnHoverChanged));

        public static void SetVisibilityOnHover(DependencyObject obj, UIElement value)
        {
            obj.SetValue(VisibilityOnHoverProperty, value);
        }

        public static UIElement GetVisibilityOnHover(DependencyObject obj)
        {
            return (UIElement)obj.GetValue(VisibilityOnHoverProperty);
        }

        private static void VisibilityOnHoverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement t = d as FrameworkElement;
            FrameworkElement s = e.NewValue as FrameworkElement;

            t.PointerEntered += (object sender, PointerRoutedEventArgs args) =>
            {
                if (s.DataContext is Applet a)
                {
                    if (a.HasPlayQueueFavorite)
                    {
                        s.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    s.Visibility = Visibility.Visible;
                }
            };

            t.PointerExited += (object sender, PointerRoutedEventArgs args) =>
            {
                s.Visibility = Visibility.Collapsed;
            };
        }
    }
}
