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
    public class DatetimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dt)
            {
                return value;
            }

            return parameter is string p ? dt.ToString(p) : dt.ToString();
        }

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
