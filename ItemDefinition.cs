using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    public enum ItemCategory
    {
        Potion,
        Scroll,
        Book,
    }

    [DataContract]
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

        public static Dictionary<int, ItemDefinition> DefDict =
            new Dictionary<int, ItemDefinition>();

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

        [DataMember(Order= 6)] public bool Stacking;
        [DataMember(Order= 7)] public int Category;
        [DataMember(Order= 8)] public int Weight;
        [DataMember(Order= 9)] public int Value;
        [DataMember(Order=10)] public Material Material;
        [DataMember(Order=11)] public int Health;
        [DataMember(Order=12)] public List<ItemTag> Tags; 
        [DataMember(Order=13)] public List<Component> Components;
        [DataMember(Order=14)] public int GenerationLowBound;
        [DataMember(Order=15)] public int GenerationHighBound;

        public ItemDefinition() { }

        //creating a NEW definition
        public ItemDefinition(
            Color? background, Color foreground,
            string tile, string name,
            bool stacking = false
        ) : base(background, foreground, tile, name) {
            Stacking = stacking;
            Components = new List<Component>();
            DefDict.Add(Type, this);
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
    }
}
