using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FlatFileList.Helpers
{
    /// <summary>
    /// TextBoxのPlaceHolder
    /// </summary>
    /// <remarks>https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/controls/how-to-add-a-watermark-to-a-textbox を改良。</remarks>
    public static partial class TextBoxHelper
    {
        /// <summary>
        /// 添付プロパティ <c>Placeholder</c>(プレースホルダー文字列)の値を取得する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <returns>プレースホルダーとして表示する文字列。</returns>
        public static string GetPlaceholder(DependencyObject obj) =>
            (string)obj.GetValue(PlaceholderProperty);

        /// <summary>
        /// 添付プロパティ <c>Placeholder</c>(プレースホルダー文字列)の値を設定する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <param name="value">プレースホルダーとして表示する文字列。</param>
        public static void SetPlaceholder(DependencyObject obj, string value) =>
            obj.SetValue(PlaceholderProperty, value);

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(
                    defaultValue: null,
                    propertyChangedCallback: OnPlaceholderChanged)
                );

        /// <summary>
        /// 添付プロパティ <c>Padding</c>(プレースホルダー描画位置の補正)の値を取得する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <returns>描画位置を補正する余白。</returns>
        public static Thickness GetPadding(DependencyObject obj) => (Thickness)obj.GetValue(PaddingProperty);

        /// <summary>
        /// 添付プロパティ <c>Padding</c>(プレースホルダー描画位置の補正)の値を設定する。
        /// </summary>
        /// <param name="obj">対象の依存関係オブジェクト。</param>
        /// <param name="value">描画位置を補正する余白。</param>
        public static void SetPadding(DependencyObject obj, Thickness value)=> obj.SetValue(PaddingProperty, value);

        public static readonly DependencyProperty PaddingProperty = DependencyProperty.RegisterAttached("Padding",
                                                                                                        typeof(Thickness),
                                                                                                        typeof(TextBoxHelper),
                                                                                                        new FrameworkPropertyMetadata(defaultValue: new Thickness(0),
                                                                                                                                      propertyChangedCallback: OnPaddingChanged));

        /// <summary>
        /// <c>Padding</c> が変更されたときのコールバック。必要なイベントを購読し、プレースホルダーの装飾を再描画する。
        /// </summary>
        /// <param name="d">対象の依存関係オブジェクト。</param>
        /// <param name="e">変更前後の値を含むイベント引数。</param>
        private static void OnPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBoxControl)
            {
                return;
            }

            if (!textBoxControl.IsLoaded)
            {
                // 弱参照で購読(多重登録防止のため一度解除してから登録)
                WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), TextBoxControl_Loaded);
                WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(textBoxControl, nameof(FrameworkElement.Loaded), TextBoxControl_Loaded);
            }

            WeakEventManager<TextBox, TextChangedEventArgs>.RemoveHandler(textBoxControl, nameof(TextBox.TextChanged), TextBoxControl_TextChanged);
            WeakEventManager<TextBox, TextChangedEventArgs>.AddHandler(textBoxControl, nameof(TextBox.TextChanged), TextBoxControl_TextChanged);

            // If the adorner exists, invalidate it to draw the current text
            if (GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner adorner))
                adorner.InvalidateVisual();
        }

        /// <summary>
        /// <c>Placeholder</c> が変更されたときのコールバック。必要なイベントを購読し、プレースホルダーの装飾を再描画する。
        /// </summary>
        /// <param name="d">対象の依存関係オブジェクト。</param>
        /// <param name="e">変更前後の値を含むイベント引数。</param>
        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBoxControl)
            {
                if (!textBoxControl.IsLoaded)
                {
                    // 弱参照で購読(多重登録防止のため一度解除してから登録)
                    WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), TextBoxControl_Loaded);
                    WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(textBoxControl, nameof(FrameworkElement.Loaded), TextBoxControl_Loaded);
                }

                WeakEventManager<TextBox, TextChangedEventArgs>.RemoveHandler(textBoxControl, nameof(TextBox.TextChanged), TextBoxControl_TextChanged);
                WeakEventManager<TextBox, TextChangedEventArgs>.AddHandler(textBoxControl, nameof(TextBox.TextChanged), TextBoxControl_TextChanged);

                // If the adorner exists, invalidate it to draw the current text
                if (GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner adorner))
                    adorner.InvalidateVisual();
            }
        }

        /// <summary>
        /// TextBox の Loaded イベントハンドラー。ハンドラーを解除し、プレースホルダー装飾を生成する。
        /// </summary>
        /// <param name="sender">イベントの発生元(TextBox)。</param>
        /// <param name="e">ルーティングイベントの引数。</param>
        private static void TextBoxControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBoxControl)
            {
                WeakEventManager<FrameworkElement, RoutedEventArgs>.RemoveHandler(textBoxControl, nameof(FrameworkElement.Loaded), TextBoxControl_Loaded);
                GetOrCreateAdorner(textBoxControl, out _);
            }
        }

        /// <summary>
        /// TextBox の TextChanged イベントハンドラー。テキストの有無に応じてプレースホルダー装飾の表示・非表示を切り替える。
        /// </summary>
        /// <param name="sender">イベントの発生元(TextBox)。</param>
        /// <param name="e">テキスト変更のイベント引数。</param>
        private static void TextBoxControl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBoxControl
                && GetOrCreateAdorner(textBoxControl, out PlaceholderAdorner adorner))
            {
                // Control has text. Hide the adorner.
                if (textBoxControl.Text.Length > 0)
                    adorner.Visibility = Visibility.Hidden;

                // Control has no text. Show the adorner.
                else
                    adorner.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// TextBox に対応するプレースホルダー装飾を取得する。存在しない場合は生成して装飾レイヤーに追加する。
        /// </summary>
        /// <param name="textBoxControl">対象の TextBox。</param>
        /// <param name="adorner">取得または生成した装飾。装飾レイヤーが未生成の場合は <see langword="null"/>。</param>
        /// <returns>装飾を取得・生成できた場合は <see langword="true"/>。装飾レイヤーが存在しない場合は <see langword="false"/>。</returns>
        private static bool GetOrCreateAdorner(TextBox textBoxControl, out PlaceholderAdorner adorner)
        {
            // Get the adorner layer
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBoxControl);

            // If null, it doesn't exist or the control's template isn't loaded
            if (layer == null)
            {
                adorner = null;
                return false;
            }

            // Layer exists, try to find the adorner
            adorner = layer.GetAdorners(textBoxControl)?.OfType<PlaceholderAdorner>().FirstOrDefault();

            // Adorner never added to control, so add it
            if (adorner == null)
            {
                adorner = new PlaceholderAdorner(textBoxControl);
                layer.Add(adorner);
            }

            return true;
        }

        /// <summary>
        /// TextBox が空のときにプレースホルダー文字列を描画する装飾(アドーナー)。
        /// </summary>
        private class PlaceholderAdorner : Adorner
        {
            /// <summary>
            /// 指定した TextBox を装飾対象として初期化する。ヒットテスト対象外にして入力を妨げないようにする。
            /// </summary>
            /// <param name="textBox">装飾対象の TextBox。</param>
            public PlaceholderAdorner(TextBox textBox) : base(textBox)
            {
                IsHitTestVisible = false;
            }

            /// <summary>
            /// プレースホルダー文字列を、TextBox のフォント・パディングに合わせた位置に描画する。
            /// </summary>
            /// <param name="drawingContext">描画に使用する <see cref="DrawingContext"/>。</param>
            protected override void OnRender(DrawingContext drawingContext)
            {
                TextBox textBoxControl = (TextBox)AdornedElement;

                string placeholderValue = TextBoxHelper.GetPlaceholder(textBoxControl);

                if (string.IsNullOrEmpty(placeholderValue))
                    return;

                // Create the formatted text object
                FormattedText text = new FormattedText(
                                            placeholderValue,
                                            System.Globalization.CultureInfo.CurrentCulture,
                                            textBoxControl.FlowDirection,
                                            new Typeface(textBoxControl.FontFamily,
                                                         textBoxControl.FontStyle,
                                                         textBoxControl.FontWeight,
                                                         textBoxControl.FontStretch),
                                            textBoxControl.FontSize,
                                            SystemColors.InactiveCaptionBrush,
                                            VisualTreeHelper.GetDpi(textBoxControl).PixelsPerDip);

                text.MaxTextWidth = System.Math.Max(textBoxControl.ActualWidth - textBoxControl.Padding.Left - textBoxControl.Padding.Right, 10);
                text.MaxTextHeight = System.Math.Max(textBoxControl.ActualHeight, 10);

                // Render based on padding of the control, to try and match where the textbox places text
                Point renderingOffset = new Point(textBoxControl.Padding.Left + TextBoxHelper.GetPadding(textBoxControl).Left, textBoxControl.Padding.Top + TextBoxHelper.GetPadding(textBoxControl).Top);

                // Template contains the content part; adjust sizes to try and align the text
                if (textBoxControl.Template.FindName("PART_ContentHost", textBoxControl) is FrameworkElement part)
                {
                    Point partPosition = part.TransformToAncestor(textBoxControl).Transform(new Point(0, 0));
                    renderingOffset.X += partPosition.X;
                    renderingOffset.Y += partPosition.Y;

                    text.MaxTextWidth = System.Math.Max(part.ActualWidth - renderingOffset.X, 10);
                    text.MaxTextHeight = System.Math.Max(part.ActualHeight, 10);
                }

                // Draw the text
                drawingContext.DrawText(text, renderingOffset);
            }
        }
    }
}
