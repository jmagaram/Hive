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
    public class PlayModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return "none!";
            }
            else {
                PlayMode playMode = (PlayMode)value;
                switch (playMode) {
                    case PlayMode.ComputerGoesFirst: return "Computer goes first";
                    case PlayMode.HumanGoesFirst: return "Person goes first";
                    case PlayMode.HumanVersusHuman: return "Person vs. person";
                    default: throw new NotImplementedException();
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
