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
    public class BoolToBrushConverter : IValueConverter
    {
        public BoolToBrushConverter()
        {
            TrueBrush = Brushes.Red;
            FalseBrush = Brushes.Transparent;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return "none!";
            }
            else {
                bool isTrue = (bool)value;
                return isTrue ? TrueBrush : FalseBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public Brush TrueBrush { get; set; }
        public Brush FalseBrush { get; set; }
    }
}
