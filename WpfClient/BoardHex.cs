using Hive;
using Hive.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfClient
{
    public class BoardHex : ViewModelBase
    {
        TileStack _stack;

        public BoardHex(TileStack stack, bool isDropTarget)
        {
            _stack = stack;
            IsDropTarget = isDropTarget;
        }

        public BoardHex(Hex hex)
            : this(new TileStack(hex, new Tile[] { }), true)
        {
        }

        public Hex Hex => _stack.Hex;
        public Tile Top => _stack.Tiles.FirstOrDefault();
        public int Height => _stack.Tiles.Count();
        public IEnumerable<Tile> Below => _stack.Tiles.Count() == 0 ? new Tile[] { } : _stack.Tiles.Skip(1);

        public bool IsDropTarget
        {
            get { return Get<bool>(); }
            set { Set<bool>(value); }
        }
    }
}
