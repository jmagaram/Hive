using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Hive.Play;

namespace WpfClient
{
    public class ColorValueConverter : IValueConverter
    {
        public ColorValueConverter()
        {
            Black = Brushes.DarkGray;
            White = Brushes.White;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Hive.Play.Color color = (Hive.Play.Color)value;
            switch (color) 
            {
                case Hive.Play.Color.White: return White;
                case Hive.Play.Color.Black: return Black;
                default: throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public Brush Black { get; set; }
        public Brush White { get; set; }
    }
}
