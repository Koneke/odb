using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    //definition should hold all data non-specific to the instance
    //instance specific data is e.g. position, instance id, stack-count.
    //definitions should not be saved per level, but are global, so to speak
    //instances, in contrast, is per level rather than global

    //ReSharper disable once InconsistentNaming
    [DataContract]
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

        public static Dictionary<int, gObjectDefinition> GObjectDefs =
            new Dictionary<int, gObjectDefinition>();

        public static int TypeCounter = 0;

        [DataMember(Order=1)] public string Name;
        [DataMember(Order=2)] public int Type;
        [DataMember(Order=3)] public string Tile;
        [DataMember(Order=4)] public Color Foreground;
        [DataMember(Order=5)] public Color? Background;

        public gObjectDefinition() { }

        public gObjectDefinition(
            Color? background, Color foreground, string tile, string name
        ) {
            Name = name;
            Type = TypeCounter++;
            Tile = tile;
            Foreground = foreground;
            Background = background;

            GObjectDefs.Add(Type, this);
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
            Tile = (char)stream.ReadHex(2) + "";
            Name = stream.ReadString();

            GObjectDefs.Add(Type, this);
            return stream;
        }
        public Stream WriteGObjectDefinition()
        {
            Stream stream = new Stream();
            stream.Write(Type, 4);
            stream.Write(Background);
            stream.Write(Foreground);
            stream.Write(Tile[0], 2);
            stream.Write(Name);
            return stream;
        }
    }

    //ReSharper disable once InconsistentNaming
    [DataContract]
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

        //ReSharper disable InconsistentNaming
        [DataMember] public Point xy;
        [DataMember] public int LevelID;
        [DataMember] private int _type;

        public gObjectDefinition Definition {
            get { return gObjectDefinition.GObjectDefs[_type]; }
        }

        public gObject() { }

        public gObject(
            Point xy,
            gObjectDefinition definition
        ) {
            this.xy = xy;
            _type = definition.Type;
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
            _type = stream.ReadHex(4);

            xy = stream.ReadPoint();
            LevelID = stream.ReadHex(4);
            return stream;
        }
    }
}
