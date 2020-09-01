using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Hive.Play;

namespace WpfClient
{
    public class BugValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Bug bug = (Bug)value;
            switch (bug) 
            { 
                case Bug.Ant : return Ant;
                case Bug.Bee: return Bee;
                case Bug.Grasshopper: return Grasshopper;
                case Bug.Beetle: return Beetle;
                case Bug.Spider: return Spider;
                default: throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Bee { get; set; }
        public object Grasshopper { get; set; }
        public object Spider { get; set; }
        public object Beetle { get; set; }
        public object Ant { get; set; }
    }
}
