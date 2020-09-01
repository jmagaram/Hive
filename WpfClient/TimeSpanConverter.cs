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
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return "none!";
            }
            else {
                TimeSpan timeSpan = (TimeSpan)value;
                int minutes = timeSpan.Minutes;
                int seconds = timeSpan.Seconds;
                if (minutes == 0) {
                    if (seconds == 1) {
                        return "1 second";
                    }
                    else {
                        return string.Format("{0} seconds", seconds);
                    }
                }
                else if (minutes == 1) {
                    if (seconds == 0) {
                        return "1 minute";
                    }
                    else {
                        return string.Format("1 minute {0} seconds", seconds);
                    }
                }
                else {
                    if (seconds == 0) {
                        return string.Format("{0} minutes", minutes);
                    }
                    else {
                        return string.Format("{0} minutes {1} seconds", minutes, seconds);
                    }
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
