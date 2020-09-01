using System;
using System.Windows.Data;

namespace TestEditor
{
    public class HexToCoordinateStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int x = (int)values[0];
                int y = (int)values[1];
                return string.Format("({0},{1})", x, y);
            }
            catch
            {
                return "(x,y)";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
