using System;
using System.Diagnostics.Contracts;
using System.Windows.Input;

namespace WpfClient
{
    public class DelegateCommand<T> : ICommand
    {
        readonly Action<object> _action;
        readonly Func<object, bool> _canExecute;

        public DelegateCommand(Func<T, bool> canExecute, Action<T> action) {
            Contract.Requires(action != null);
            _canExecute = (param) => { return canExecute != null ? canExecute((T)param) : true; };
            _action = (param) => { action((T)param); };
        }

        public DelegateCommand(Action<T> action)
            : this(null, action) {
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute(parameter);

        public void Execute(object parameter) => _action(parameter);

        public void RaiseCanExecuteChanged() => OnCanExecuteChanged(EventArgs.Empty);

        protected virtual void OnCanExecuteChanged(EventArgs args) => CanExecuteChanged?.Invoke(this, args);
    }
}
