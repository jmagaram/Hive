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
    public class GameStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return "none!";
            }
            else {
                Status status = (Status)value;
                switch (status)
                {
                    case Status.NextTurnByComputerBlack: return "Computer (black) thinking...";
                    case Status.NextTurnByComputerWhite: return "Computer (white) thinking...";
                    case Status.NextTurnByHumanBlack: return "Your turn (black)";
                    case Status.NextTurnByHumanWhite: return "Your turn (white)";
                    case Status.PriorGameAborted: return "Game aborted; no winner";
                    case Status.Tie: return "Tie!";
                    case Status.WaitingToStart: return "Ready to play?";
                    case Status.WonByBlack: return "Black won!";
                    case Status.WonByWhite: return "White won!";
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
