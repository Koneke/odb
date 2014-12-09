using Microsoft.Xna.Framework;

namespace ODB
{
    //definition should hold all data non-specific to the instance
    //instance specific data is e.g. position, instance id, stack-count.
    //definitions should not be saved per level, but are global, so to speak
    //instances, in contrast, is per level rather than global

    //ReSharper disable once InconsistentNaming
    public class gObjectDefinition
    {
        public bool Equals(gObjectDefinition other)
        {
            return
                Background.Equals(other.Background) &&
                Foreground.Equals(other.Foreground) &&
                string.Equals(Tile, other.Tile) &&
                string.Equals(Name, other.Name) &&
                Type == other.Type;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Background.GetHashCode();
                hashCode = (hashCode*397) ^ Foreground.GetHashCode();
                hashCode = (hashCode*397) ^ (Tile != null ? Tile.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Type;
                return hashCode;
            }
        }

        //afaik, atm we won't have anything that's /just/
        // a game object, it'll always be an item/actor
        //but in case.
        public static gObjectDefinition[] Definitions =
            new gObjectDefinition[0xFFFF];
        public static int TypeCounter = 0;

        public Color? Background;
        public Color Foreground;
        public string Tile;
        public string Name;
        public int Type;

        public gObjectDefinition(
            Color? background, Color foreground, string tile, string name
        ) {
            Background = background;
            Foreground = foreground;
            Tile = tile;
            Name = name;
            Type = TypeCounter++;
            Definitions[Type] = this;
        }

        public gObjectDefinition(string s)
        {
            ReadGObjectDefinition(s);
        }

        public Stream ReadGObjectDefinition(string s)
        {
            Stream stream = new Stream(s);
            Type = stream.ReadHex(4);
            Background = stream.ReadNullableColor();
            Foreground = stream.ReadColor();
            Tile = stream.ReadString(1);
            Name = stream.ReadString();

            Definitions[Type] = this;
            //LH-011214: Creating new definitions after reading from file
            //           makes weird stuff happen without this, since we
            //           do not increment the type counter normally (i.e. when
            //           we "create" the def) here, so we make the counter
            //           search for a new empty spot.
            while (Definitions[TypeCounter++] != null) { }
            return stream;
        }
        public Stream WriteGObjectDefinition()
        {
            Stream stream = new Stream();
            stream.Write(Type, 4);
            stream.Write(Background);
            stream.Write(Foreground);
            stream.Write(Tile, false);
            stream.Write(Name);
            return stream;
        }
    }

    //ReSharper disable once InconsistentNaming
    public class gObject
    {
        protected bool Equals(gObject other)
        {
            return
                xy.Equals(other.xy) &&
                Definition.Equals(other.Definition);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((gObject)obj);
        }
        public override int GetHashCode()
        {
            int hashCode = xy.GetHashCode();
            hashCode = (hashCode*397) ^ Definition.GetHashCode();
            return hashCode;
        }

        public static Game1 Game;

        //ReSharper disable InconsistentNaming
        public Point xy;
        //ReSharper restore InconsistentNaming
        public int LevelID;
        public gObjectDefinition Definition;

        public gObject(
            Point xy,
            int level,
            gObjectDefinition definition
        ) {
            this.xy = xy;
            LevelID = level;
            Definition = definition;
        }

        public gObject(string s)
        {
            ReadGObject(s);
        }

        public Stream WriteGObject()
        {
            Stream stream = new Stream();
            stream.Write(Definition.Type, 4);
            stream.Write(xy);
            stream.Write(LevelID, 4);
            return stream;
        }

        public Stream ReadGObject(string s)
        {
            Stream stream = new Stream(s);
            Definition = gObjectDefinition.Definitions[
                stream.ReadHex(4)
            ];

            xy = stream.ReadPoint();
            LevelID = stream.ReadHex(4);
            return stream;
        }
    }
}
