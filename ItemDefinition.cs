using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ODB
{
    public enum ItemCategory
    {
        Potion,
        Scroll,
        Book,
    }

    [JsonConverter(typeof(ItemConverter))]
    public class ItemDefinition : GameObjectDefinition
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

        public static Dictionary<ItemID, ItemDefinition> DefDict =
            new Dictionary<ItemID, ItemDefinition>();

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

        //attributes only for show/reminding, ItemConverter handles what is
        //actually written.
        [DataMember] public ItemID ItemType;
        [DataMember] public bool Stacking;
        [DataMember] public int Category;
        [DataMember] public int Weight;
        [DataMember] public int Value;
        [DataMember] public Material Material;
        [DataMember] public int Health;
        [DataMember] public List<ItemTag> Tags; 
        [DataMember] public List<Component> Components;
        [DataMember] public int GenerationLowBound;
        [DataMember] public int GenerationHighBound;

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
