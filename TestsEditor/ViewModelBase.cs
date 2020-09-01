using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace TestEditor
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();

        protected void Set<T>(T value, [CallerMemberName]string key = null)
        {
            Contract.Requires(key != null);
            object oldValue;
            if (!_values.TryGetValue(key, out oldValue) || !EqualityComparer<T>.Default.Equals(value, (T)oldValue))
            {
                _values[key] = value;
                OnPropertyChanged(new PropertyChangedEventArgs(key));
            }
        }

        protected void Set<T>(ref T field, T value, [CallerMemberName]string key = null)
        {
            Contract.Requires(field != null);
            Contract.Requires(key != null);
            T oldValue = field;
            if (!EqualityComparer<T>.Default.Equals(value, oldValue))
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(key));
            }
        }

        protected T Get<T>([CallerMemberName]string key = null)
        {
            Contract.Requires(key != null);
            object value;
            if (!_values.TryGetValue(key, out value))
            {
                throw new ArgumentException();
            }
            else
            {
                return (T)value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }
    }
}
