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

        /// <summary>
        /// 添付プロパティ <c>HideClearButton</c> の値を取得する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <returns>クリアボタンを非表示にする場合は <see langword="true"/>。</returns>
        public static bool GetHideClearButton(DependencyObject obj) =>
            (bool)obj.GetValue(HideClearButtonProperty);

        /// <summary>
        /// 添付プロパティ <c>HideClearButton</c> の値を設定する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <param name="value">クリアボタンを非表示にする場合は <see langword="true"/>。</param>
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

        /// <summary>
        /// <c>HideClearButton</c> が変更されたときのコールバック。値が <see langword="true"/> の場合、
        /// TextBox が読み込み済みなら即座に、未読み込みなら Loaded 後にクリアボタンを非表示にする。
        /// </summary>
        /// <param name="d">対象の依存関係オブジェクト。</param>
        /// <param name="e">変更前後の値を含むイベント引数。</param>
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

        /// <summary>
        /// TextBox の Loaded イベントハンドラー。ハンドラーを解除してからクリアボタンを非表示にする。
        /// </summary>
        /// <param name="sender">イベントの発生元(TextBox)。</param>
        /// <param name="e">ルーティングイベントの引数。</param>
        private static void HideClearButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBoxControl)
            {
                WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), HideClearButton_Loaded);
                HideDeleteButton(textBoxControl);
            }
        }

        /// <summary>
        /// テンプレートを適用し、クリアボタンのパーツ(<see cref="DeleteButtonPartName"/>)を <see cref="Visibility.Collapsed"/> にする。
        /// </summary>
        /// <param name="textBoxControl">対象の TextBox。</param>
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
