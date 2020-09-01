using System.Collections.Generic;

namespace TestEditor
{
    class Hex : ViewModelBase
    {
        public Hex()
        {
            Color = "empty";
            Tag = null;
        }

        static public IEnumerable<string> Colors => new string[] { "white", "black", "empty" };

        public int X
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Y
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Color
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Tag
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
