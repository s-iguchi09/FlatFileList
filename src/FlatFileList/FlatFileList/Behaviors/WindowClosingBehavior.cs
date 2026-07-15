using FlatFileList.Interfaces;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;

namespace FlatFileList.Behaviors
{
    public class WindowClosingBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Closing += Window_Closing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Closing -= Window_Closing;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            var window = sender as Window;

            // NOTE:ViewModelがインターフェイスを実装していたらメソッドを実行する
            if (window?.DataContext is IWindowClosing windowClosing)
                e.Cancel = windowClosing.OnClosing();
        }
    }
}
