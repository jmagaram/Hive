using System;

namespace WpfClient
{
    public class DelegateCommand : DelegateCommand<object>
    {
        public DelegateCommand(Func<bool> canExecute, Action action)
            : base(
            canExecute: canExecute == null ? (Func<object, bool>)null : (object p) => { return canExecute(); },
            action: action == null ? (Action<object>)null : (object p) => { action(); }) {
        }

        public DelegateCommand(Action action)
            : this(null, action) {
        }
    }
}
