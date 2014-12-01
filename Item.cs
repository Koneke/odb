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
            bool equipSlotsEqual = (EquipSlots.Count == other.EquipSlots.Count);
            if (!equipSlotsEqual) return false;
            for (int i = 0; i < EquipSlots.Count; i++)
                if (EquipSlots[i] != other.EquipSlots[i])
                    return false;

            bool ammoTypesEqual = (AmmoTypes.Count == other.AmmoTypes.Count);
            if (!ammoTypesEqual) return false;
            for (int i = 0; i < AmmoTypes.Count; i++)
                if (AmmoTypes[i] != other.AmmoTypes[i])
                    return false;

            return
                base.Equals(other) &&
                string.Equals(Damage, other.Damage) &&
                ArmorClass == other.ArmorClass &&
                Stacking.Equals(other.Stacking) &&
                Ranged.Equals(other.Ranged) &&
                string.Equals(RangedDamage, other.RangedDamage) &&
                UseEffect == other.UseEffect &&
                Category == other.Category &&
                Nutrition == other.Nutrition;
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
                hashCode = (hashCode*397) ^ (Damage != null ? Damage.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ ArmorClass;
                hashCode = (hashCode*397) ^ Stacking.GetHashCode();
                hashCode = (hashCode*397) ^ Ranged.GetHashCode();
                hashCode = (hashCode*397) ^ (RangedDamage != null ? RangedDamage.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ UseEffect;
                hashCode = (hashCode*397) ^ Category;
                hashCode = (hashCode*397) ^ Nutrition;
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

        public string Damage;
        public int ArmorClass;
        public bool Stacking;
        public List<DollSlot> EquipSlots;
        //ranged _weapons_
        public bool Ranged;
        public List<int> AmmoTypes;
        //addition of this, since some items can be used in both melee
        //and at range, like spears
        public string RangedDamage;
        public int UseEffect;
        //groups potions together and what not
        //mainly, unidentified items of different defs take from the same
        //random appearance pool (but still only one appearance per definition)
        public int Category;
        public int Nutrition;

        //saved in game file, not idef file
        public bool Identified {
            get { return IdentifiedDefs.Contains(Type); }
        }

        //creating a NEW definition
        public ItemDefinition(
            Color? background, Color foreground,
            string tile, string name,

            //item definition specific stuff
            string damage = "",
            int armorClass = 0,
            bool stacking = false,

            List<DollSlot> equipSlots = null,
            bool ranged = false,
            List<int> ammoTypes = null,
            int useEffect = 0xFFFF // SPELL ID

        ) : base(background, foreground, tile, name) {
            Damage = damage;
            ArmorClass = armorClass;
            Stacking = stacking;

            //todo: should we really be doing this ?? biz here?
            EquipSlots = equipSlots ?? new List<DollSlot>();
            Ranged = ranged;
            AmmoTypes = ammoTypes;
            UseEffect = useEffect;

            ItemDefinitions[Type] = this;
        }

        public ItemDefinition(string s) : base(s)
        {
            ReadItemDefinition(s);
        }

        public Stream ReadItemDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Damage = stream.ReadString();
            ArmorClass = stream.ReadHex(2);
            Stacking = stream.ReadBool();

            string slots = stream.ReadString();
            EquipSlots = new List<DollSlot>();
            foreach (
                string slot in slots.Split(',')
                    .Where(ss => ss != ""))
                EquipSlots.Add((DollSlot) int.Parse(slot));

        Ranged = stream.ReadBool();

            string ammos = stream.ReadString();
            AmmoTypes = new List<int>();
            foreach (
                string ss in ammos.Split(',')
                    .Where(ss => ss != ""))
                AmmoTypes.Add(IO.ReadHex(ss));

            RangedDamage = stream.ReadString();

            UseEffect = stream.ReadHex(4);

            Category = stream.ReadHex(2);

            Nutrition = stream.ReadHex(4);

            ItemDefinitions[Type] = this;
            return stream;
        }

        public Stream WriteItemDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Damage);
            stream.Write(ArmorClass, 2);
            stream.Write(Stacking);

            foreach (DollSlot ds in EquipSlots)
                stream.Write((int)ds + ",", false);
            stream.Write(";", false);

            stream.Write(Ranged);

            if(AmmoTypes != null)
                foreach (int ammoType in AmmoTypes)
                {
                    stream.Write(ammoType, 4);
                    stream.Write(",", false);
                }
            stream.Write(";", false);

            stream.Write(RangedDamage);

            stream.Write(UseEffect, 4);

            stream.Write(Category, 2);

            stream.Write(Nutrition, 4);

            return stream;
        }
    }

    public class Item : gObject
    {
        protected bool Equals(Item other)
        {
            bool modsEqual = Mods.Count == other.Count;
            if (!modsEqual) return false;
            for (int i = 0; i < Mods.Count; i++)
                if (Mods[i] != other.Mods[i]) return false;

            return
                base.Equals(other) &&
                ID == other.ID &&
                Mod == other.Mod &&
                Count == other.Count &&
                Equals(Definition, other.Definition);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ ID;
                hashCode = (hashCode*397) ^ Mod;
                hashCode = (hashCode*397) ^ Count;
                hashCode = (hashCode*397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Mods != null ? Mods.GetHashCode() : 0);
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Item)obj);
        }

        public static int IDCounter = 0;

        //instance specifics
        public int ID;
        public int Mod;
        //can be used as charges for non-stacking?
        //-1 should be inf. charges?
        public int Count;
        public new ItemDefinition Definition;
        public List<Mod> Mods;

        //not to file
        public bool Charged;

        //wrapping directly to the definition for ease
        public List<DollSlot> EquipSlots {
            get { return Definition.EquipSlots; }
        }

        public List<int> AmmoTypes
        {
            get { return Definition.AmmoTypes; }
        } 

        public int Type {
            get { return Definition.Type; }
        }

        public Spell UseEffect
        {
            get {
                if (Definition.UseEffect == 0xFFFF) return null;
                return Spell.Spells[Definition.UseEffect];
            }
        }

        public bool Identified { get { return Definition.Identified; } }

        //SPAWNING a NEW item
        public Item(
            Point xy,
            ItemDefinition definition,
            //might not make 100% sense, but non-stacking items are 0
            //this so we can easily separate stacking, nonstacking and charged
            int count = 0,
            IEnumerable<Mod> mods = null
        ) : base(xy, definition) {
            ID = IDCounter++;
            Count = count;
            Definition = definition;
            Mods = new List<Mod>();
            if (mods != null) Mods.AddRange(mods);
            Charged = !Definition.Stacking && count > 0;
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);
            Charged = !Definition.Stacking && Count > 0;
        }

        public string GetName(
            bool definite = false,
            bool noArt = false,
            bool capitalized = false
        ) {
            string name;
            if (!Identified)
            {
                name = ItemDefinition.Appearances
                    [Definition.Category].Shuffle()
                    [
                        (Definition.Type + Math.Abs(Game.Seed))
                        % ItemDefinition.Appearances[0].Count
                    ];
            } else name = Definition.Name;

            string article = Util.Article(name);
            if (definite) article = "the";
            if (Definition.Stacking && Count > 1)
                article = Count + "x";

            string s = (noArt ? "" : (article + " "));

            #region mods
            /*if (Mods.Count > 0 && Identified) {
                switch (Mods[0].Type)
                {
                    case ModType.AddStr: s += "fierce"; break;
                    case ModType.DecStr: s += "frail"; break;
                    case ModType.AddDex: s += "vile"; break;
                    case ModType.DecDex: s += "dull"; break;
                    case ModType.AddInt: s += "clever"; break;
                    case ModType.DecInt: s += "clouded"; break;
                    case ModType.AddSpd: s += "fast"; break;
                    case ModType.DecSpd: s += "trudging"; break;
                    case ModType.AddQck: s += "eager"; break;
                    case ModType.DecQck: s += "lazy"; break;
                }
                s += " ";
            }*/
            #endregion

            //s += Definition.name;
            s += name;

            #region mods
            /*if (Mods.Count > 1 && Identified) {
                s += " of ";
                switch (Mods[1].Type)
                {
                    case ModType.AddStr: s += "might"; break;
                    case ModType.DecStr: s += "twigs"; break;
                    case ModType.AddDex: s += "wounding"; break;
                    case ModType.DecDex: s += "bumbling"; break;
                    case ModType.AddInt: s += "clarity"; break;
                    case ModType.DecInt: s += "shrouds"; break;
                    case ModType.AddSpd: s += "wind"; break;
                    case ModType.DecSpd: s += "dawdling"; break;
                    case ModType.AddQck: s += "lightning"; break;
                    case ModType.DecQck: s += "dallying"; break;
                }
            }*/
            #endregion

            s = s.ToLower();
            if (capitalized)
                s = s.Substring(0, 1).ToUpper() +
                    s.Substring(1, s.Length - 1);
            return s;
        }

        public void Identify(
            //bool cursed,
            //bool blessed,
            //bool mods
        ) {
            ItemDefinition.IdentifiedDefs.Add(Type);
        }

        public Stream WriteItem()
        {
            Stream stream = WriteGObject();

            stream.Write(Definition.Type, 4);
            stream.Write(ID, 4);
            stream.Write(Mod, 2);
            stream.Write(Count, 2);

            foreach (Mod m in Mods)
            {
                stream.Write((int)m.Type, 2);
                stream.Write(":", false);
                stream.Write(m.RawValue, 2);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }

        public Stream ReadItem(string s)
        {
            Stream stream = ReadGObject(s);
            Definition =
                ItemDefinition.ItemDefinitions[
                    stream.ReadHex(4)
                ];
            //are we actually setting the IDef.ItemDefinitions when loading..?
            ID = stream.ReadHex(4);
            Mod = stream.ReadHex(2);
            Count = stream.ReadHex(2);

            Mods = new List<Mod>();
            List<string> mods = stream.ReadString().Split(
                new[] {','},
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string[] ss in mods.Select(mod => mod.Split(':')))
            {
                Mods.Add(new Mod(
                    (ModType)
                    IO.ReadHex(ss[0]),
                    IO.ReadHex(ss[1])
                ));
            }

            return stream;
        }
    }
}
