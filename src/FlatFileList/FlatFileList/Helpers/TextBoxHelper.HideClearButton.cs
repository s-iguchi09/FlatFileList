using System.Windows;
using System.Windows.Controls;

namespace FlatFileList.Helpers
{
    /// <summary>
    /// Fluent テーマの TextBox がフォーカス時に表示するクリアボタン(テンプレートパーツ "DeleteButton")を非表示にする添付プロパティ。
    /// テンプレートのトリガーより優先されるローカル値で Visibility=Collapsed を設定するため、フォーカス時も非表示を維持する。
    /// AcceptsReturn を変更しないので、Enter 入力やペーストの挙動には一切影響しない。
    /// </summary>
    /// <remarks>参考: https://web-dev.hatenablog.com/entry/csharp/wpf/theme/fluent/clear-btn</remarks>
    public static partial class TextBoxHelper
    {
        /// <summary>Fluent テーマの TextBox テンプレートに定義されたクリアボタンのパーツ名。</summary>
        private const string DeleteButtonPartName = "DeleteButton";

        public static bool GetHideClearButton(DependencyObject obj) =>
            (bool)obj.GetValue(HideClearButtonProperty);

        public static void SetHideClearButton(DependencyObject obj, bool value) =>
            obj.SetValue(HideClearButtonProperty, value);

        public static readonly DependencyProperty HideClearButtonProperty =
            DependencyProperty.RegisterAttached(
                "HideClearButton",
                typeof(bool),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(
                    defaultValue: false,
                    propertyChangedCallback: OnHideClearButtonChanged)
                );

        private static void OnHideClearButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBoxControl || !(bool)e.NewValue)
            {
                return;
            }

            if (textBoxControl.IsLoaded)
            {
                HideDeleteButton(textBoxControl);
            }
            else
            {
                // 弱参照で購読し、TextBox が破棄されてもハンドラが到達参照を残さないようにする。
                // 多重登録防止のため一度解除してから登録する。
                WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), HideClearButton_Loaded);
                WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(textBoxControl, nameof(FrameworkElement.Loaded), HideClearButton_Loaded);
            }
        }

        private static void HideClearButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBoxControl)
            {
                WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), HideClearButton_Loaded);
                HideDeleteButton(textBoxControl);
            }
        }

        private static void HideDeleteButton(TextBox textBoxControl)
        {
            textBoxControl.ApplyTemplate();

            if (textBoxControl.Template?.FindName(DeleteButtonPartName, textBoxControl) is UIElement deleteButton)
            {
                deleteButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}
