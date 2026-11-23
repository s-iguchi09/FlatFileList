using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// <see cref="DataGrid"/> のスクロールや表示件数の変化を監視し、画面に表示中の項目を <see cref="Command"/> へ通知するビヘイビア。
    /// </summary>
    /// <remarks>
    /// 表示範囲は行単位スクロール(既定の <see cref="VirtualizingPanel.ScrollUnitProperty"/> = Item)の
    /// VerticalOffset / ViewportHeight から求める。ピクセル単位スクロールの場合は通知しない。
    /// </remarks>
    public class DataGridVisibleItemsBehavior : Behavior<DataGrid>
    {
        /// <summary>
        /// 表示中の項目(<see cref="IList"/>、画面の上から順)を受け取るコマンド。
        /// </summary>
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary><see cref="Command"/> の依存関係プロパティ。</summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DataGridVisibleItemsBehavior), new PropertyMetadata(null));

        /// <summary>
        /// ビヘイビアがアタッチされた際に、配下の <see cref="ScrollViewer"/> のスクロール変化イベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
        }

        /// <summary>
        /// ビヘイビアが取り外された際に、購読していたイベントを解除する。
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
            base.OnDetaching();
        }

        /// <summary>
        /// スクロール位置・表示領域・全体件数が変わった際に、表示中の項目を集めて <see cref="Command"/> を実行する。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">スクロール変化の情報。</param>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Command is null || e.OriginalSource is not ScrollViewer { CanContentScroll: true })
            {
                return;
            }

            var items = AssociatedObject.Items;
            var first = Math.Max(0, (int)e.VerticalOffset);
            var last = Math.Min(items.Count, first + (int)Math.Ceiling(e.ViewportHeight) + 1);

            if (first >= last)
            {
                return;
            }

            var visibleItems = new List<object>(last - first);
            for (var index = first; index < last; index++)
            {
                visibleItems.Add(items[index]);
            }

            if (Command.CanExecute(visibleItems))
            {
                Command.Execute(visibleItems);
            }
        }
    }
}
