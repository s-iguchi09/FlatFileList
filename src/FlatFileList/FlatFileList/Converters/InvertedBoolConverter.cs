using System.Globalization;
using System.Windows.Data;

namespace FlatFileList.Converters
{
    /// <summary>
    /// bool を Invert (反転)するConverter です。
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
