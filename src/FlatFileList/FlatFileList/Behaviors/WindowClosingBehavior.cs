using FlatFileList.Interfaces;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// ウィンドウの Closing イベントを、DataContext が実装する <see cref="IWindowClosing"/> へ橋渡しするビヘイビア。
    /// ViewModel 側で閉じる処理の可否を制御できるようにする。
    /// </summary>
    public class WindowClosingBehavior : Behavior<Window>
    {
        /// <summary>
        /// ビヘイビアがアタッチされた際に、ウィンドウの Closing イベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Closing += Window_Closing;
        }

        /// <summary>
        /// ビヘイビアが取り外された際に、購読していた Closing イベントを解除する。
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Closing -= Window_Closing;
        }

        /// <summary>
        /// ウィンドウの Closing ハンドラー。DataContext が <see cref="IWindowClosing"/> を実装していれば、その結果でクローズを制御する。
        /// </summary>
        /// <param name="sender">イベントの発生元(<see cref="Window"/>)。</param>
        /// <param name="e">クローズをキャンセルするための情報。</param>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            var window = sender as Window;

            // NOTE:ViewModelがインターフェイスを実装していたらメソッドを実行する
            if (window?.DataContext is IWindowClosing windowClosing)
                e.Cancel = windowClosing.OnClosing();
        }
    }
}
