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
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Type;
                return hashCode;
            }
        }

        public static Dictionary<int, gObjectDefinition> GObjectDefs =
            new Dictionary<int, gObjectDefinition>();

        public static int TypeCounter = 0;

        [DataMember(Order = 1, Name = "Name")]
        public string Name;

        //I'm just plain assuming this thing generates a whole lot of bugs now?
        [DataMember(Order = 2, Name = "Type")]
        public int Type;

        [DataMember(Order = 3, Name = "Tile")]
        public char Tile;

        [DataMember(Order = 4, Name = "Foreground")]
        public Color Foreground;

        [DataMember(Order = 5, Name = "Background")]
        public Color? Background;
    }

    //ReSharper disable once InconsistentNaming
    [DataContract]
    public class gObject
    {
        protected bool Equals(gObject other)
        {
            return xy.Equals(other.xy);
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
            return hashCode;
        }

        //ReSharper disable InconsistentNaming
        [DataMember(Name = "Position")]
        public Point xy;

        [DataMember(Name = "LevelID")]
        public int LevelID;

        public gObject() { }
        public gObject(Point xy) { this.xy = xy; }
    }
}
