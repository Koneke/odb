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

        public Color Background, Foreground;
        public string Character;
        public bool Solid;

        public TileDefinition(
            Color background,
            Color foreground,
            string character,
            bool solid
        ) {
            Background = background;
            Foreground = foreground;
            Character = character;
            Solid = solid;

            Type = TypeCounter++;
            Definitions[TypeCounter] = this;
        }

        public TileDefinition(string s)
        {
            ReadTileDefinition(s);
        }

        public Stream WriteTileDefinition()
        {
            Stream stream = new Stream();
            stream.Write(Type, 4);
            stream.Write(Background);
            stream.Write(Foreground);
            stream.Write(Character, false);
            stream.Write(Solid);
            return stream;
        }

        public Stream ReadTileDefinition(string s)
        {
            Stream stream = new Stream(s);
            Type = stream.ReadHex(4);
            if (Type >= TypeCounter) TypeCounter++;
            Background = stream.ReadColor();
            Foreground = stream.ReadColor();
            Character = stream.ReadString(1);
            Solid = stream.ReadBool();
            Definitions[Type] = this;
            return stream;
        }
    }

    public class Tile
    {
        public TileDefinition Definition;
        public Color Background { get { return Definition.Background; } }
        public Color Foreground { get { return Definition.Foreground; } }
        public string Character { get { return Definition.Character; } }
        public bool Solid { get { return Definition.Solid; } }

        public Door Door;
        public Stairs Stairs;
        public string Engraving;

        //no need to save
        public Point Position;

        public Tile(
            TileDefinition definition,
            Door doors = Door.None,
            Stairs stairs = Stairs.None,
            string engraving = ""
        ) {
            Definition = definition;
            Door = doors;
            Stairs = stairs;
            Engraving = engraving;
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
