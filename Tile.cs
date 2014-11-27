using Microsoft.Xna.Framework;

namespace ODB
{
    public enum Door
    {
        None,
        Open,
        Closed
    }

    public enum Stairs
    {
        None,
        Up,
        Down
    }

    public class TileDefinition
    {
        public static TileDefinition[] Definitions =
            new TileDefinition[0xFFFF];
        public static int TypeCounter = 0;

        public int Type;

        public Color bg, fg;
        public string Tile;
        public bool Solid;

        public TileDefinition(
            Color bg,
            Color fg,
            string Tile,
            bool Solid
        ) {
            this.bg = bg;
            this.fg = fg;
            this.Tile = Tile;
            this.Solid = Solid;

            this.Type = TypeCounter++;
            Definitions[this.Type] = this;
        }

        public TileDefinition(string s)
        {
            ReadTileDefinition(s);
        }

        public Stream WriteTileDefinition()
        {
            Stream stream = new Stream();
            stream.Write(Type, 4);
            stream.Write(bg);
            stream.Write(fg);
            stream.Write(Tile, false);
            stream.Write(Solid);
            return stream;
        }

        public Stream ReadTileDefinition(string s)
        {
            Stream stream = new Stream(s);
            Type = stream.ReadHex(4);
            bg = stream.ReadColor();
            fg = stream.ReadColor();
            Tile = stream.ReadString(1);
            Solid = stream.ReadBool();
            return stream;
        }
    }

    public class Tile
    {
        public TileDefinition Definition;
        public Color bg { get { return Definition.bg; } }
        public Color fg { get { return Definition.fg; } }
        public string tile { get { return Definition.Tile; } }
        public bool solid { get { return Definition.Solid; } }

        public Door Door;
        public Stairs Stairs;
        public string Engraving;

        public Tile(
            TileDefinition Definition,
            Door doors = Door.None,
            Stairs stairs = Stairs.None,
            string Engraving = ""
        ) {
            this.Definition = Definition;
            this.Door = doors;
            this.Stairs = stairs;
            this.Engraving = Engraving;
        }

        public Tile(string s)
        {
            ReadTile(s);
            Engraving = "";
        }

        public Tile() { }

        public Stream WriteTile()
        {
            Stream stream = new Stream();
            stream.Write(Definition.Type, 4);
            stream.Write((int)Door, 1);
            stream.Write((int)Stairs, 1);
            stream.Write(Engraving);
            return stream;
        }

        public Stream ReadTile(string s)
        {
            Stream stream = new Stream(s);
            Definition = TileDefinition.Definitions[stream.ReadHex(4)];
            Door = (Door)stream.ReadHex(1);
            Stairs = (Stairs)stream.ReadHex(1);
            Engraving = stream.ReadString();
            return stream;
        }
    }
}
