using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomControlLibrary
{
    /// <summary>
    /// Converter of long value (i.g. bytes into kbytes )
    /// </summary>
    public class LongValueConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(!double.TryParse(parameter as string, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out double coeff))
            {
                return value;
            }
            if (long.TryParse(value as string, out long lv))
            {
                return lv * coeff;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
