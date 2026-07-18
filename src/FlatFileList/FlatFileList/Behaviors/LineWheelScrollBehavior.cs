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
        /// <summary>
        /// ビヘイビアがアタッチされた際に、マウスホイールのプレビューイベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        /// <summary>
        /// ビヘイビアが取り外された際に、購読していたマウスホイールイベントを解除する。
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        /// <summary>
        /// マウスホイール操作を1行単位のスクロールに変換するハンドラー。既定のスクロール処理は抑止する。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">ホイールの回転量を含むイベント引数。</param>
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

        /// <summary>
        /// ビジュアルツリーを再帰的に探索し、最初に見つかった <see cref="ScrollViewer"/> を返す。
        /// </summary>
        /// <param name="parent">探索の起点となる要素。</param>
        /// <returns>見つかった <see cref="ScrollViewer"/>。存在しない場合は <see langword="null"/>。</returns>
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
