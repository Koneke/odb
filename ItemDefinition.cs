using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ODB
{
    //todo: read/write for this
    public enum ItemCategory
    {
        Potion = 0x00,
        Scroll = 0x01,
        Book = 0x02,
    }

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
        #region appearances
             new Dictionary<int, List<string>>
            {
                {(int)ItemCategory.Potion, new List<string>{
                    "violet potion",
                    "turqoise potion",
                    "bubbling potion",
                }},
                {(int)ItemCategory.Scroll, new List<string>{
                    "scroll labelled ZELGO MER",
                    "scroll labelled JUYED AWK YACC",
                }},
                {(int)ItemCategory.Book, new List<string>{
                    "glittering tome",
                    "alluring booklet",
                    "silvery pamphlet",
                }},
            };
        #endregion

        public bool Stacking;
        public int GenerationLowBound;
        public int GenerationHighBound;
        public int Category;
        public int Weight;
        public int Value;
        public Material Material;
        public int Health;
        public List<ItemTag> Tags; 
        public List<Component> Components; 

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

        public void AddComponent<T>(T component) where T : Component
        {
            //NOTE: IF YOU DON'T FIRST CHECK WHETHER OR NOT WE HAVE A
            //      COMPONENT OF THIS KIND FIRST, YOU'RE IN FOR A BAD TIME.
            if (!HasComponent(component.GetType()))
                Components.Add(component);
            else throw new Exception();
        }
        public bool HasComponent<T>() where T : Component
        {
            return Components.Any(c => c is T);
        }
        //reason this is here because calling a <T> where T : Component from
        //another with the same sig-bit at the end made all T show up as
        //Component, which meant that no item could have two components, ever...
        public bool HasComponent(Type t)
        {
            return Components.Any(t.IsInstanceOfType);
        }
        public T GetComponent<T>() where T : Component
        {
            return (T)Components.FirstOrDefault(c => c is T);
        }

        public Stream ReadItemDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Stacking = stream.ReadBool();
            GenerationLowBound = stream.ReadInt();
            GenerationHighBound = stream.ReadInt();
            Category = stream.ReadHex(2);
            Weight = stream.ReadInt();
            Value = stream.ReadInt();
            Material = Materials.ReadMaterial(stream.ReadString());
            Health = stream.ReadInt();

            ItemDefinitions[Type] = this;

            Tags = new List<ItemTag>();
            string tags = stream.ReadString();
            foreach (string tag in tags.NeatSplit(","))
                Tags.Add(Item.ReadItemTag(tag));

            Components = new List<Component>();
            while (!stream.AtFinish)
            {
                AddComponent(
                    Component.CreateComponent(
                        stream.ReadString(),
                        stream.ReadBlock()
                    )
                );
            }

            return stream;
        }
        public Stream WriteItemDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Stacking);
            stream.Write(Category, 2);
            stream.Write(Weight);
            stream.Write(Value);
            stream.Write(Materials.WriteMaterial(Material));
            stream.Write(Health);

            foreach (Component c in Components)
                stream.Write(c.WriteComponent(), false);

            return stream;
        }
    }
}