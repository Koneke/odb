using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    //ReSharper disable once InconsistentNaming
    [DataContract]
    public class GameObjectDefinition
    {
        public bool Equals(GameObjectDefinition other)
        {
            return
                Background.Equals(other.Background) &&
                Foreground.Equals(other.Foreground) &&
                string.Equals(Tile, other.Tile) &&
                string.Equals(Name, other.Name);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Background.GetHashCode();
                hashCode = (hashCode*397) ^ Foreground.GetHashCode();
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }

        [DataMember]
        public string Name;

        [DataMember]
        public char Tile;

        [DataMember]
        public Color Foreground;

        [DataMember]
        public Color? Background;
    }
}
