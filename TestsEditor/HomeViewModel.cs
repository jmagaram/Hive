using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Newtonsoft.Json;

namespace TestEditor
{
    class HomeViewModel : ViewModelBase
    {
        private const string _defaultTestFileName = "TestData";
        readonly DelegateCommand _save;
        readonly DelegateCommand _load;
        readonly DelegateCommand _deleteBoard;
        readonly DelegateCommand _addBoard;
        readonly DelegateCommand _addNeighbors;
        readonly DelegateCommand _deleteHex;
        readonly DelegateCommand _cloneBoard;
        readonly DelegateCommand<string> _setHexColor;
        ObservableCollection<Board> _boards;

        public HomeViewModel()
        {
            _boards = new ObservableCollection<Board>();
            Boards = CollectionViewSource.GetDefaultView(_boards);
            Boards.CurrentChanged += (sender, args) =>
                 {
                     _deleteBoard.RaiseCanExecuteChanged();
                     _cloneBoard.RaiseCanExecuteChanged();
                     if (Boards.CurrentItem == null)
                     {
                         Hexes = null;
                     }
                     else
                     {
                         var currentBoard = GetCurrentBoard();
                         Hexes = CollectionViewSource.GetDefaultView(currentBoard.Hexes);
                     }
                     if (Hexes != null)
                     {
                         Hexes.CurrentChanged -= Hexes_CurrentChanged;
                         Hexes.CurrentChanged += Hexes_CurrentChanged;
                     }
                 };
            _load = new DelegateCommand(
                canExecute: () => true,
                action: () =>
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        FileName = _defaultTestFileName,
                        DefaultExt = ".json",
                        Filter = "JSON documents (.json)|*.json"
                    };
                    var result = dlg.ShowDialog();
                    if (result == true)
                    {
                        string filename = dlg.FileName;
                        var jsonText = File.ReadAllText(filename);
                        var loadedBoards = JsonConvert.DeserializeObject<List<Board>>(jsonText);
                        _boards.Clear();
                        foreach(var b in _boards)
                        {
                            _boards.Remove(b);
                        }
                        foreach(var b in loadedBoards)
                        {
                            _boards.Add(b);
                        }
                        if (_boards.Count > 0)
                        {
                            Boards.MoveCurrentToFirst();
                        }
                    }
                });
            _save = new DelegateCommand(
                canExecute: () => true,
                action: () =>
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = _defaultTestFileName,
                        DefaultExt = ".json",
                        Filter = "JSON documents (.json)|*.json"
                    };
                    var result = dlg.ShowDialog();
                    if (result == true)
                    {
                        string filename = dlg.FileName;
                        File.WriteAllText(dlg.FileName, JsonConvert.SerializeObject(
                            _boards
                            .OrderBy(i => i.Title)
                            .ThenBy(i => i.CreatedOn)));
                    }
                });
            _deleteBoard = new DelegateCommand(
                canExecute: () => Boards.CurrentItem != null,
                action: () =>
                {
                    _boards.Remove(GetCurrentBoard());
                }
            );
            _cloneBoard = new DelegateCommand(
                canExecute: () => Boards.CurrentItem != null,
                action: () =>
                {
                    Board currentBoard = GetCurrentBoard();
                    Board clone = new Board { Title = string.Empty };
                    foreach (var h in currentBoard.Hexes)
                    {
                        clone.Hexes.Add(new Hex { X = h.X, Y = h.Y, Tag = h.Tag, Color = h.Color });
                    }
                    _boards.Add(clone);
                }
            );
            _addBoard = new DelegateCommand(
                action: () =>
                {
                    Board b = new Board();
                    b.Hexes.Add(new Hex { X = 0, Y = 0 });
                    _boards.Add(b);
                    Boards.MoveCurrentTo(b);
                });
            _addNeighbors = new DelegateCommand(
                canExecute: () => Hexes != null && Hexes.CurrentItem != null,
                action: () =>
                {
                    Hex currentHex = Hexes.CurrentItem as Hex;
                    Board currentBoard = GetCurrentBoard();
                    var neighborsToAdd =
                        new List<Tuple<int, int>> { 
                            Tuple.Create(1, 0), 
                            Tuple.Create(1, -1), 
                            Tuple.Create(0, -1), 
                            Tuple.Create(-1, 0), 
                            Tuple.Create(-1, 1), 
                            Tuple.Create(0, 1) }
                        .Select(i => new Tuple<int, int>(currentHex.X + i.Item1, currentHex.Y + i.Item2))
                        .Except(currentBoard.Hexes.Select(i => new Tuple<int, int>(i.X, i.Y)));
                    foreach (var newNeighbor in neighborsToAdd)
                    {
                        var hex = new Hex { Color = "empty", Tag = null, X = newNeighbor.Item1, Y = newNeighbor.Item2 };
                        currentBoard.Hexes.Add(hex);
                    }
                }
            );
            _deleteHex = new DelegateCommand(
                canExecute: () => Hexes != null && Hexes.CurrentItem != null,
                action: () =>
                {
                    Hex currentHex = Hexes.CurrentItem as Hex;
                    Board currentBoard = GetCurrentBoard();
                    currentBoard.Hexes.Remove(currentHex);
                    if (currentBoard.Hexes.Count == 0)
                    {
                        currentBoard.Hexes.Add(new Hex { Color = "black", X = 0, Y = 0 });
                    }
                });
            _setHexColor = new DelegateCommand<string>(
                action: (color) =>
                {
                    var currentHex = Hexes.CurrentItem as Hex;
                    if (currentHex != null)
                    {
                        currentHex.Color = color;
                    }
                });
            BoardFilter = null;
        }

        private Board GetCurrentBoard() => Boards.CurrentItem as Board;

        private void Hexes_CurrentChanged(object sender, EventArgs e)
        {
            _addNeighbors.RaiseCanExecuteChanged();
            _deleteHex.RaiseCanExecuteChanged();
        }

        public ICommand Save => _save;

        public ICommand Load => _load;

        public ICommand DeleteBoard => _deleteBoard;

        public ICommand AddBoard => _addBoard;

        public ICommand AddNeighbors => _addNeighbors;

        public ICommand DeleteHex => _deleteHex;

        public ICommand CloneBoard => _cloneBoard;

        public ICommand SetHexColor => _setHexColor;

        public IEnumerable<string> Colors => Hex.Colors;

        public ICollectionView Boards
        {
            get => Get<ICollectionView>();
            set => Set(value);
        }

        public ICollectionView Hexes
        {
            get => Get<ICollectionView>();
            set => Set(value);
        }

        public string BoardFilter
        {
            get
            {
                return Get<string>();
            }

            set
            {
                Boards.Filter = (obj) =>
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return true;
                    }
                    else
                    {
                        Board board = (Board)obj;
                        return string.IsNullOrEmpty(board.Title) || (board.Title.ToLower().Contains(value.ToLower()));
                    }
                };
                Set(value);
            }
        }
    }
}
