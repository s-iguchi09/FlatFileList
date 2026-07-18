using System.Globalization;
using System.Windows.Data;

namespace FlatFileList.Converters
{
    /// <summary>
    /// bool を Invert (反転)するConverter です。
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        /// <summary>
        /// <see cref="bool"/> 値を反転して返す。
        /// </summary>
        /// <param name="value">変換元の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">未使用。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>反転した <see cref="bool"/> 値。値が <see cref="bool"/> でない場合は元の値。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
                return value;
#pragma warning restore CS8603 // Null 参照戻り値である可能性があります。
            }

            if (!(value is bool b))
            {
                return value;
            }

            return !b;
        }

        /// <summary>
        /// 逆変換はサポートしない。
        /// </summary>
        /// <param name="value">変換元の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">未使用。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>常に例外をスローするため値を返さない。</returns>
        /// <exception cref="NotImplementedException">常にスローされる。</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
