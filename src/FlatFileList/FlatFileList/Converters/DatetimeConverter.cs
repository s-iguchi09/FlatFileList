using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace FlatFileList.Converters
{
    /// <summary>
    /// <see cref="DateTime"/> と文字列を相互変換するコンバーター。
    /// <c>ConverterParameter</c> に書式文字列を指定すると、その書式で整形・解析を行う。
    /// </summary>
    public class DatetimeConverter : IValueConverter
    {
        /// <summary>
        /// <see cref="DateTime"/> を指定された書式の文字列に変換する。
        /// </summary>
        /// <param name="value">変換元の値。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">書式文字列。<see langword="null"/> の場合は既定の書式を使用する。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>整形した文字列。値が <see cref="DateTime"/> でない場合は元の値。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dt)
            {
                return value;
            }

            return parameter is string p ? dt.ToString(p) : dt.ToString();
        }

        /// <summary>
        /// 文字列を <see cref="DateTime"/> に変換する。指定書式での解析を試み、失敗した場合は一般的な解析にフォールバックする。
        /// </summary>
        /// <param name="value">変換元の文字列。</param>
        /// <param name="targetType">変換先の型。</param>
        /// <param name="parameter">解析に使用する書式文字列。</param>
        /// <param name="culture">使用するカルチャ。</param>
        /// <returns>解析された <see cref="DateTime"/>。解析できない場合は元の値。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return value;
            }

            var param = parameter as string;

            if (DateTime.TryParseExact(value as string, param, null, DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }

            if (DateTime.TryParse(value as string, out DateTime dt2))
            {
                return dt2;
            }

            return value;
        }
    }
}
