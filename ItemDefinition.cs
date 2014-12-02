using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ODB
{
    public class ItemDefinition : gObjectDefinition
    {
        protected bool Equals(ItemDefinition other)
        {
            //todo: Check components

            return
                base.Equals(other) &&
                Stacking.Equals(other.Stacking) &&
                Category == other.Category
            ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                //todo: There is a risk of hashcollision.
                //      See comment in Actor.cs::GetHashCode().
                //      tl;dr: Not using all fields since we are
                //        1. Lazy IRL
                //        2. Trying to check equivalence by value, and not by
                //           reference.
                //      This should only ever be a problem if you have two
                //      in essence identical definitions though, and try to
                //      use them as separate keys.
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ Stacking.GetHashCode();
                hashCode = (hashCode*397) ^ Category;
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemDefinition)obj);
        }

        public static ItemDefinition[] ItemDefinitions =
            new ItemDefinition[0xFFFF];

        public static Dictionary<int, List<string>> Appearances =
            new Dictionary<int, List<string>>
            {
                {0, new List<string>{
                    "violet potion",
                    "turqoise potion"
                }}
            };
        public static List<int> IdentifiedDefs = new List<int>();

        public bool Stacking;
        //groups potions together and what not
        //mainly, unidentified items of different defs take from the same
        //random appearance pool (but still only one appearance per definition)
        public int Category;

        public List<Component> Components; 

        //saved in game file, not idef file
        public bool Identified {
            get { return IdentifiedDefs.Contains(Type); }
        }

        //creating a NEW definition
        public ItemDefinition(
            Color? background, Color foreground,
            string tile, string name,
            bool stacking = false
            ) : base(background, foreground, tile, name) {
            Stacking = stacking;
            Components = new List<Component>();
            ItemDefinitions[Type] = this;
        }

        public ItemDefinition(string s) : base(s)
        {
            ReadItemDefinition(s);
        }

        public void AddComponent(Component component)
        {
            //Only one of each kind of component, please and thanks
            if(GetComponent(component.GetComponentType()) != null)
                throw new InvalidOperationException();

            Components.Add(component);
        }
        public bool HasComponent(string type)
        {
            return Components.Any(x => x.GetComponentType() == type);
        }
        public Component GetComponent(string type)
        {
            return Components.FirstOrDefault(
                x => x.GetComponentType() == type);
        }

        public Stream ReadItemDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Stacking = stream.ReadBool();

            Category = stream.ReadHex(2);

            ItemDefinitions[Type] = this;

            Components = new List<Component>();
            while (!stream.AtFinish())
                AddComponent(
                    Component.CreateComponent(
                        stream.ReadString(),
                        stream.ReadBlock()
                        )
                    );

            return stream;
        }
        public Stream WriteItemDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Stacking);

            stream.Write(Category, 2);

            foreach (Component c in Components)
                stream.Write(c.WriteComponent(), false);

            return stream;
        }
    }
}