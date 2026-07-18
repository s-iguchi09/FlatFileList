using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlatFileList.Behaviors
{
    /// <summary>マウスホイールのスクロール量を1行単位にする。</summary>
    public class LineWheelScrollBehavior : Behavior<ItemsControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindScrollViewer(AssociatedObject);
            if (scrollViewer is null)
            {
                return;
            }
            if (e.Delta > 0)
            {
                scrollViewer.LineUp();
            }
            else
            {
                scrollViewer.LineDown();
            }
            e.Handled = true;
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                var result = FindScrollViewer(child);
                if (result is not null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
