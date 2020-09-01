using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using Hive.Play;
using System.Threading;

namespace WpfClient
{
    public class HomeViewModel : ViewModelBase
    {
        bool _isUpdating = false;

        public HomeViewModel()
        {
            Game = HiveGame.New;
            ThinkDepth = 999;
            ThinkTime = TimeSpan.FromSeconds(30);
            GamePlayMode = PlayMode.HumanGoesFirst;
            GameStatus = Status.WaitingToStart;
            Start = new DelegateCommand(
                canExecute: () => !IsGameInProgress,
                action: () =>
                {
                    Game = Hive.Play.HiveGame.New;
                    GameStatus = CalculateStatus();
                });
            Abort = new DelegateCommand(
                canExecute: () =>
                {
                    return GameStatus == Status.NextTurnByHumanBlack || GameStatus == Status.NextTurnByHumanWhite;
                },
                action: () =>
                {
                    GameStatus = Status.PriorGameAborted;
                    SelectedInPlay = null;
                    SelectedReserveBlack = null;
                    SelectedReserveWhite = null;
                });
            SkipTurn = new DelegateCommand(
                canExecute: () =>
                {
                    return
                        (GameStatus == Status.NextTurnByHumanBlack || GameStatus == Status.NextTurnByHumanWhite) &&
                        !Game.LegalMoves.Any() &&
                        !Game.LegalPlace.Bugs.Any();
                },
                action: () =>
                {
                    Game = Game.SkipTurn;
                    GameStatus = CalculateStatus();
                });
        }

        private IHiveGame Game
        {
            get { return base.Get<IHiveGame>(); }
            set
            {
                base.Set<IHiveGame>(value);
                Stacks = new ObservableCollection<BoardHex>(value.Stacks.Select(s => new BoardHex(s, false)));
                ReserveBlack = value.Reserve(Color.Black);
                ReserveWhite = value.Reserve(Color.White);
                SelectedReserveBlack = null;
                SelectedReserveWhite = null;
                SelectedInPlay = null;
            }
        }

        public Status GameStatus
        {
            get { return Get<Status>(); }
            private set
            {
                if (Set<Status>(value)) {
                    IsGameInProgress = new List<Status> { 
                        Status.NextTurnByHumanWhite, 
                        Status.NextTurnByHumanBlack, 
                        Status.NextTurnByComputerWhite, 
                        Status.NextTurnByComputerBlack }.Contains(value);
                    IsGameNotInProgress = !IsGameInProgress;
                    IsThinking = value == Status.NextTurnByComputerBlack || value == Status.NextTurnByComputerWhite;
                    ThinkProgress = 0;
                    CanDragReserveWhite = value == Status.NextTurnByHumanWhite && Game.Reserve(Color.White).Any();
                    CanDragReserveBlack = value == Status.NextTurnByHumanBlack && Game.Reserve(Color.Black).Any();
                    CanDragTileOnBoard = value == Status.NextTurnByHumanBlack || value == Status.NextTurnByHumanWhite;
                    if (value != Status.WaitingToStart) {
                        Start.RaiseCanExecuteChanged();
                        Abort.RaiseCanExecuteChanged();
                        SkipTurn.RaiseCanExecuteChanged();
                    }
                    // http://stackoverflow.com/questions/13978879/c-sharp-and-tasks-ui-thread-hang-pre-async-await-keywords
                    if (value == Status.NextTurnByComputerBlack || value == Status.NextTurnByComputerWhite) {
                        var progress = new DispatcherTimer(
                            TimeSpan.FromSeconds(ThinkTime.TotalSeconds / 100),
                            DispatcherPriority.Normal,
                            (o, e) => { ThinkProgress = ThinkProgress + 1; },
                            Dispatcher.CurrentDispatcher);
                        Task<IHiveGame> takeAutomaticTurn = new Task<IHiveGame>(() =>
                        {
                            progress.Start();
                            var result = Game.TakeBestTurn(ThinkDepth, (int)ThinkTime.TotalSeconds);
                            progress.Stop();
                            return result;
                        });
                        takeAutomaticTurn.ContinueWith(
                            continuationAction: t =>
                            {
                                Game = t.Result;
                                GameStatus = CalculateStatus();
                            },
                            scheduler: TaskScheduler.FromCurrentSynchronizationContext());
                        takeAutomaticTurn.Start();
                    }
                }
            }
        }

        public int ThinkProgress
        {
            get { return Get<int>(); }
            private set { Set<int>(value); }
        }

        public bool IsThinking
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public bool IsGameInProgress
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public bool IsGameNotInProgress
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public bool CanDragReserveWhite
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public bool CanDragReserveBlack
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public bool CanDragTileOnBoard
        {
            get { return Get<bool>(); }
            private set { Set<bool>(value); }
        }

        public TimeSpan ThinkTime
        {
            get { return Get<TimeSpan>(); }
            set { Set<TimeSpan>(value); }
        }

        public PlayMode GamePlayMode
        {
            get { return Get<PlayMode>(); }
            set { Set<PlayMode>(value); }
        }

        public int ThinkDepth
        {
            get { return Get<int>(); }
            set { Set<int>(value); }
        }

        public DelegateCommand Start
        {
            get { return Get<DelegateCommand>(); }
            set { Set<DelegateCommand>(value); }
        }

        public DelegateCommand Abort
        {
            get { return Get<DelegateCommand>(); }
            set { Set<DelegateCommand>(value); }
        }

        public DelegateCommand SkipTurn
        {
            get { return Get<DelegateCommand>(); }
            set { Set<DelegateCommand>(value); }
        }

        public ObservableCollection<BoardHex> Stacks
        {
            get { return base.Get<ObservableCollection<BoardHex>>(); }
            set { base.Set<ObservableCollection<BoardHex>>(value); }
        }

        public BoardHex SelectedInPlay
        {
            get { return base.Get<BoardHex>(); }
            set
            {
                if (base.Set<BoardHex>(value)) {
                    if (!_isUpdating) {
                        _isUpdating = true;
                        SelectedReserveBlack = null;
                        SelectedReserveWhite = null;
                        UpdateDropTargets();
                        _isUpdating = false;
                    }
                }
            }
        }

        public IEnumerable<Tile> ReserveBlack
        {
            get { return Get<IEnumerable<Tile>>(); }
            set { base.Set<IEnumerable<Tile>>(value); }
        }

        public IEnumerable<Tile> ReserveWhite
        {
            get { return Get<IEnumerable<Tile>>(); }
            set { base.Set<IEnumerable<Tile>>(value); }
        }

        public Tile SelectedReserveWhite
        {
            get { return base.Get<Tile>(); }
            set
            {
                if (base.Set<Tile>(value)) {
                    if (!_isUpdating) {
                        _isUpdating = true;
                        SelectedReserveBlack = null;
                        SelectedInPlay = null;
                        UpdateDropTargets();
                        _isUpdating = false;
                    }
                }
            }
        }

        public Tile SelectedReserveBlack
        {
            get { return base.Get<Tile>(); }
            set
            {
                if (base.Set<Tile>(value)) {
                    if (!_isUpdating) {
                        _isUpdating = true;
                        SelectedReserveWhite = null;
                        SelectedInPlay = null;
                        UpdateDropTargets();
                        _isUpdating = false;
                    }
                }
            }
        }

        public bool CanMove(Hex source, Hex target)
        {
            return (Game.LegalMoves.Any(i => (i.Source == source) && i.Targets.Contains(target)));
        }

        public bool CanPlace(Bug bug, Hex target)
        {
            return Game.LegalPlace.Bugs.Contains(bug) && Game.LegalPlace.Targets.Contains(target);
        }

        public void AttemptMove(Hex source, Hex target)
        {
            if (Game.LegalMoves.Any(i => (i.Source == source) && i.Targets.Contains(target))) {
                Game = Game.Move(source, target);
                GameStatus = CalculateStatus();
            }
        }

        public void AttemptPlace(Bug bug, Hex target)
        {
            if (Game.LegalPlace.Bugs.Contains(bug) && Game.LegalPlace.Targets.Contains(target)) {
                Game = Game.Place(bug, target);
                GameStatus = CalculateStatus();
            }
        }

        private void UpdateDropTargets()
        {
            HashSet<Hex> targets = new HashSet<Hex>();
            if (
                SelectedInPlay != null &&
                SelectedInPlay.Top != null &&
                ((Game.State == State.NextTurnByWhite && SelectedInPlay.Top.Color == Color.White) || (Game.State == State.NextTurnByBlack && SelectedInPlay.Top.Color == Color.Black)) &&
                (Game.LegalMoves.Any(i => i.Source == SelectedInPlay.Hex))) {
                targets = new HashSet<Hex>(Game.LegalMoves.First(i => i.Source == SelectedInPlay.Hex).Targets);
            }
            else if (
                SelectedReserveBlack != null &&
                Game.State == State.NextTurnByBlack &&
                Game.LegalPlace.Bugs.Contains(SelectedReserveBlack.Bug)) {
                targets = new HashSet<Hex>(Game.LegalPlace.Targets);
            }
            else if (
                SelectedReserveWhite != null &&
                Game.State == State.NextTurnByWhite &&
                Game.LegalPlace.Bugs.Contains(SelectedReserveWhite.Bug)) {
                targets = new HashSet<Hex>(Game.LegalPlace.Targets);
            }
            // Remove all existing frontier
            foreach (var h in Stacks.Where(i => i.Height == 0).ToList()) {
                Stacks.Remove(h);
            }
            // Add all target frontier
            foreach (var h in targets.Except(Stacks.Select(i => i.Hex))) {
                Stacks.Add(new BoardHex(h));
            }
            // Turn on/off drop target property
            foreach (var h in Stacks) {
                h.IsDropTarget = targets.Contains(h.Hex);
            }
        }

        private Status CalculateStatus()
        {
            switch (Game.State) {
                case State.Tie: return Status.Tie;
                case State.WonByBlack: return Status.WonByBlack;
                case State.WonByWhite: return Status.WonByWhite;
                case State.NextTurnByWhite:
                    switch (GamePlayMode) {
                        case PlayMode.ComputerGoesFirst: return Status.NextTurnByComputerWhite;
                        case PlayMode.HumanGoesFirst:
                        case PlayMode.HumanVersusHuman: return Status.NextTurnByHumanWhite;
                        default: throw new NotImplementedException();
                    }
                case State.NextTurnByBlack:
                    switch (GamePlayMode) {
                        case PlayMode.HumanGoesFirst: return Status.NextTurnByComputerBlack;
                        case PlayMode.ComputerGoesFirst:
                        case PlayMode.HumanVersusHuman: return Status.NextTurnByHumanBlack;
                        default: throw new NotImplementedException();
                    }
                default: throw new NotImplementedException();
            }
        }
    }
}
