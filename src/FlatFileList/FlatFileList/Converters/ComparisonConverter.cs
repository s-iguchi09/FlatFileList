using System.Windows.Data;

namespace FlatFileList.Converters
{
    /// <summary>
    /// 値と <c>ConverterParameter</c> の等価性を <see cref="bool"/> に変換するコンバーター。
    /// ラジオボタンなどで列挙値と特定の値を対応付ける用途に使用する。
    /// </summary>
    public class ComparisonConverter : IValueConverter
    {
        /// <summary>
        /// バインディング値がパラメーターと等しいかどうかを返す。
        /// </summary>
        /// <param name="value">バインディング元の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">比較対象のパラメーター。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>値とパラメーターが等しければ <see langword="true"/>。</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is null && parameter is null ? true : value?.Equals(parameter) ?? false;
        }

        /// <summary>
        /// バインディング先が <see langword="true"/> の場合にパラメーターを返し、それ以外は <see cref="Binding.DoNothing"/> を返す。
        /// </summary>
        /// <param name="value">バインディング先の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">対応付けるパラメーター。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>選択された場合はパラメーター、そうでなければ <see cref="Binding.DoNothing"/>。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
