using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    public class TileInfo
    {
        public Level Level;

        public Point Position;

        public Tile Tile
        {
            get { return Level.Map[Position.x, Position.y]; }
            set { Level.Map[Position.x, Position.y] = value; }
        }

        public bool Blood
        {
            get { return Level.Blood[Position.x, Position.y]; }
            set { Level.Blood[Position.x, Position.y] = value; }
        }
        public bool Seen
        {
            get { return Level.Seen[Position.x, Position.y]; }
            set { Level.Seen[Position.x, Position.y] = value; }
        }

        public bool Solid { get { return Tile.Solid; } }

        public List<Item> Items;
        public Actor Actor;

        public List<TileInfo> Neighbours
        {
            get
            {
                List<TileInfo> neighbours = new List<TileInfo>();
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    neighbours.Add(Level.At(Position + new Point(x, y)));
                return neighbours
                    .Where(ti => ti != null)
                    .Where(ti => ti != this)
                    .ToList();
            }
        }

        public Door Door {
            get { return Tile.Door; }
            set { Tile.Door = value; }
        }
        public Stairs Stairs { get { return Tile.Stairs; } }

        public TileInfo(Level level)
        {
            Level = level;
        }
    }

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

    [DataContract]
    public class Tile
    {
        [DataMember] private int _type;

        public Color Background { get { return Definition.Background; } }
        public Color Foreground { get { return Definition.Foreground; } }
        public string Character { get { return Definition.Character; } }
        public bool Solid { get { return Definition.Solid; } }

        [DataMember] public Door Door;
        [DataMember] public Stairs Stairs;
        [DataMember] public string Engraving;

        public TileDefinition Definition
        {
            get { return TileDefinition.Definitions[_type]; }
            set { _type = value.Type; }
        }

        //no need to save
        public Point Position;

        public Tile(
            TileDefinition definition,
            Door doors = Door.None,
            Stairs stairs = Stairs.None,
            string engraving = ""
        ) {
            _type = definition.Type;
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
            _type = stream.ReadHex(4);
            Door = (Door)stream.ReadHex(1);
            Stairs = (Stairs)stream.ReadHex(1);
            Engraving = stream.ReadString();
            return stream;
        }

        //todo: would be nice to have a string class with colours
        //      like, text + "this bit has this bgfg, this bit has this bgfg"
        public string Render()
        {
            string tileToDraw = Character;

            if (Engraving != "") tileToDraw = RenderEngraving();

            if (Door == Door.Closed) tileToDraw = "+";
            if (Door == Door.Open) tileToDraw = "/";

            if (Stairs == Stairs.Down) tileToDraw = ">";
            if (Stairs == Stairs.Up) tileToDraw = "<";

            return tileToDraw;
        }

        public string RenderEngraving()
        {
            switch (Engraving.ToLower())
            {
                case "tor": return (char)(255 - 31) + "";
                case "zok": return (char)(255 - 30) + "";
                case "kel": return (char)(255 - 29) + "";
                case "bal": return (char)(255 - 28) + "";
                case "jol": return (char)(255 - 27) + "";
                case "khr": return (char)(255 - 26) + "";
                case "yyl": return (char)(255 - 25) + "";
                case "don": return (char)(255 - 24) + "";
                case "bik": return (char)(255 - 23) + "";
                default: return ",";
            }
        }
    }
}
