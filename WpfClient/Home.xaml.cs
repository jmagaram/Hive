using Hive.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfClient
{
    public partial class Home : Window
    {
        HomeViewModel _viewModel;

        public Home()
        {
            InitializeComponent();
            _viewModel = FindResource("viewModel") as HomeViewModel;
            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
        }

        void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "GameStatus")
            {
                using (SoundPlayer snd = new SoundPlayer(Properties.Resources.TwoTone15B))
                {
                    snd.Play();
                }
            }
        }

        private void PolygonInReserve_MouseMove(object sender, MouseEventArgs e)
        {
            Polygon polygon = sender as Polygon;
            if (polygon != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Tile tile = (Tile)polygon.DataContext;
                DragDrop.DoDragDrop(polygon, tile, DragDropEffects.Move);
            }
        }

        private void PolygonInPlay_MouseMove(object sender, MouseEventArgs e)
        {
            Polygon polygon = sender as Polygon;
            if (polygon != null && e.LeftButton == MouseButtonState.Pressed)
            {
                BoardHex boardHex = (BoardHex)polygon.DataContext;
                DragDrop.DoDragDrop(polygon, boardHex, DragDropEffects.Move);
            }
        }

        private void PolygonInPlay_DragOver(object sender, DragEventArgs e)
        {
            var target = (BoardHex)((sender as FrameworkElement).DataContext);
            var sourceTileInReserve = e.Data.GetData(typeof(Tile)) as Tile;
            var sourceTileInPlay = e.Data.GetData(typeof(BoardHex)) as BoardHex;
            if (sourceTileInPlay != null)
            {
                e.Effects = _viewModel.CanMove(sourceTileInPlay.Hex, target.Hex) ? DragDropEffects.Move : DragDropEffects.None;
            }
            else if (sourceTileInReserve != null)
            {
                e.Effects = _viewModel.CanPlace(sourceTileInReserve.Bug, target.Hex) ? DragDropEffects.Move : DragDropEffects.None;
            }
            else
            {
                throw new NotImplementedException();
            }
            e.Handled = true;
        }

        private void PolygonInPlay_Drop(object sender, DragEventArgs e)
        {
            var target = (BoardHex)((sender as FrameworkElement).DataContext);
            var sourceTileInReserve = e.Data.GetData(typeof(Tile)) as Tile;
            var sourceTileInPlay = e.Data.GetData(typeof(BoardHex)) as BoardHex;
            if (sourceTileInPlay != null)
            {
                _viewModel.AttemptMove(sourceTileInPlay.Hex, target.Hex);
            }
            else if (sourceTileInReserve != null)
            {
                _viewModel.AttemptPlace(sourceTileInReserve.Bug, target.Hex);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
