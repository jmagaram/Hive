using System.Windows;
using System.Windows.Controls;

namespace TestEditor
{
    public partial class Home : Window
    {
        public Home()
        {
            InitializeComponent();
            Loaded += Home_Loaded;
        }

        void Home_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new HomeViewModel();
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as ContextMenu).DataContext = this.DataContext;
        }
    }
}
