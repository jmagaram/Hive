using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TestEditor
{
    class Board : ViewModelBase
    {
        public Board()
        {
            CreatedOn = DateTime.UtcNow;
            Title = string.Empty;
            Hexes = new ObservableCollection<Hex>();
            Note = string.Empty;
        }

        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }

        public DateTime CreatedOn
        {
            get => Get<DateTime>();
            set => Set(value);
        }

        public string Note
        {
            get => Get<string>();
            set => Set(value);
        }

        public virtual ICollection<Hex> Hexes
        {
            get => Get<ICollection<Hex>>();
            set => Set(value);
        }
    }
}
