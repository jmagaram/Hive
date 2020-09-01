using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Hive.Play;

namespace WpfClient
{
    public class HexMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Hex piece = value as Hex;
            if (piece != null)
            {
                double hexRadius = (parameter == null) ? 50 : double.Parse((string)parameter);
                const double degrees30 = Math.PI * 2 / 12;
                double hexHeight = hexRadius * Math.Cos(degrees30) * 2;
                double hexSide = hexRadius;
                double left = piece.X * (hexRadius + hexSide / 2);
                double top = -piece.Y * hexHeight - piece.X * hexHeight / 2;
                return new Thickness(left, top, 0, 0);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
