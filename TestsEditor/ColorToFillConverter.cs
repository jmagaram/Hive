using System;
using System.Windows.Data;
using System.Windows.Media;

namespace TestEditor
{
    public class ColorToFillConverter : IValueConverter
    {
        public ColorToFillConverter()
        {
            Black = Brushes.Navy;
            White = Brushes.Beige;
            Error = Brushes.Red;
            Empty = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string color = value as string;
            if (color == null)
            {
                return Error;
            }
            else
            {
                color = color.ToLower();
                switch (color)
                {
                    case "black": return Black;
                    case "white": return White;
                    case "empty": return Empty;
                    default: return Error;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public Brush Black { get; set; }
        public Brush White { get; set; }
        public Brush Error { get; set; }
        public Brush Empty { get; set; }
    }
}
