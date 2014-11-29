using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ODB
{
    public class ItemDefinition : gObjectDefinition
    {
        public static ItemDefinition[] ItemDefinitions =
            new ItemDefinition[0xFFFF];

        public static Dictionary<int, List<string>> Appearances =
            new Dictionary<int, List<string>>()
            {
                {0, new List<string>{
                    "violet potion",
                    "turqoise potion"
                }}
            };
        public static List<int> IdentifiedDefs = new List<int>();

        public int AC;
        public string Damage;
        public bool stacking;
        public List<DollSlot> equipSlots;
        //ranged _weapons_
        public bool Ranged;
        public List<int> AmmoTypes;
        public int UseEffect;
        //groups potions together and what not
        //mainly, unidentified items of different defs take from the same
        //random appearance pool (but still only one appearance per def)
        public int Category;
        public int Nutrition;

        //saved in game file, not idef file
        public bool Identified {
            get { return IdentifiedDefs.Contains(type); }
        }

        //creating a NEW definition
        public ItemDefinition(
            Color? bg, Color fg,
            string tile, string name,

            //item def specific stuff
            string Damage = "",
            int AC = 0,
            bool stacking = false,

            List<DollSlot> equipSlots = null,
            bool Ranged = false,
            List<int> AmmoTypes = null,
            int UseEffect = 0xFFFF // SPELL ID

        ) : base(bg, fg, tile, name) {
            this.Damage = Damage;
            this.AC = AC;
            this.stacking = stacking;

            this.equipSlots = equipSlots ?? new List<DollSlot>();
            this.Ranged = Ranged;
            this.AmmoTypes = AmmoTypes;
            this.UseEffect = UseEffect;

            ItemDefinitions[this.type] = this;
        }

        public ItemDefinition(string s) : base(s)
        {
            ReadItemDefinition(s);
        }

        public Stream ReadItemDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);
            
            Damage = stream.ReadString();
            AC = stream.ReadHex(2);
            stacking = stream.ReadBool();

            string slots = stream.ReadString();
            equipSlots = new List<DollSlot>();
            foreach (string ss in slots.Split(','))
                if(ss != "")
                    equipSlots.Add((DollSlot)int.Parse(ss));

            Ranged = stream.ReadBool();

            string ammos = stream.ReadString();
            AmmoTypes = new List<int>();
            foreach (string ss in ammos.Split(','))
                if(ss != "") AmmoTypes.Add(IO.ReadHex(ss));

            UseEffect = stream.ReadHex(4);

            Category = stream.ReadHex(2);

            Nutrition = stream.ReadHex(4);

            ItemDefinitions[type] = this;
            return stream;
        }

        public Stream WriteItemDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Damage);
            stream.Write(AC, 2);
            stream.Write(stacking);

            foreach (DollSlot ds in equipSlots)
                stream.Write((int)ds + ",", false);
            stream.Write(";", false);

            stream.Write(Ranged);

            if(AmmoTypes != null)
                foreach (int type in AmmoTypes)
                {
                    stream.Write(type, 4);
                    stream.Write(",", false);
                }
            stream.Write(";", false);

            stream.Write(UseEffect, 4);

            stream.Write(Category, 2);

            stream.Write(Nutrition, 4);

            return stream;
        }
    }

    public class Item : gObject
    {
        public static int IDCounter = 0;

        //instance specifics
        public int id;
        public int mod;
        //can be used as charges for non-stacking?
        //-1 should be inf. charges?
        public int count;
        public new ItemDefinition Definition;
        public List<Mod> Mods;

        //not to file
        public bool Charged;

        //wrapping directly to the def for ease
        public List<DollSlot> equipSlots {
            get { return Definition.equipSlots; }
        }

        public int type {
            get { return Definition.type; }
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
            ItemDefinition def,
            //might not make 100% sense, but non-stacking items are 0
            //this so we can easily separate stacking, nonstacking and charged
            int count = 0,
            List<Mod> Mods = null
        ) : base(xy, def) {
            id = IDCounter++;
            this.count = count;
            this.Definition = def;
            this.Mods = new List<Mod>();
            if (Mods != null) this.Mods.AddRange(Mods);
            Charged = !Definition.stacking && count > 0;
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);
            Charged = !Definition.stacking && count > 0;
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
                        (Definition.type + Math.Abs(Game.Seed))
                        % ItemDefinition.Appearances[0].Count
                    ];
            } else name = Definition.name;

            string article = Util.article(name);
            if (definite) article = "the";
            if (Definition.stacking && count > 1)
                article = count + "x";

            string s = (noArt ? "" : (article + " "));

            #region mods
            if (Mods.Count > 0 && Identified) {
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
            }
            #endregion

            //s += Definition.name;
            s += name;

            #region mods
            if (Mods.Count > 1 && Identified) {
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
            }
            #endregion

            s = s.ToLower();
            if (capitalized)
                s = s.Substring(0, 1).ToUpper() +
                    s.Substring(1, s.Length - 1);
            return s;
        }

        public Stream WriteItem()
        {
            Stream stream = WriteGOBject();

            stream.Write(Definition.type, 4);
            stream.Write(id, 4);
            stream.Write(mod, 2);
            stream.Write(count, 2);

            foreach (Mod m in Mods)
            {
                stream.Write((int)m.Type, 2);
                stream.Write(":", false);
                stream.Write((int)m.Value, 2);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }

        public Stream ReadItem(string s)
        {
            Stream stream = ReadGOBject(s);
            Definition =
                ItemDefinition.ItemDefinitions[
                    stream.ReadHex(4)
                ];
            //are we actually setting the IDef.ItemDefinitions when loading..?
            id = stream.ReadHex(4);
            mod = stream.ReadHex(2);
            count = stream.ReadHex(2);

            Mods = new List<Mod>();
            string mods = stream.ReadString();
            foreach (string ss in mods.Split(
                new char[]{','}, StringSplitOptions.RemoveEmptyEntries))
            {
                Mod m = new Mod(
                    (ModType)IO.ReadHex(ss.Split(':')[0]),
                             IO.ReadHex(ss.Split(':')[1])
                );
                Mods.Add(m);
            }

            return stream;
        }
    }
}
