using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public class Item : gObject
    {
        protected bool Equals(Item other)
        {
            bool modsEqual = Mods.Count == other.Count;
            if (!modsEqual) return false;
            if (Mods.Where((t, i) => t != other.Mods[i]).Any())
            {
                return false;
            }

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
        public int Weight;

        /*
         * bool BucKnown;
         * bool NameKnown; 
         * bool ModKnown;
         */

        //not to file
        public bool Charged;

        //wraps
        public bool Stacking { get { return Definition.Stacking; } }
        public int Type {
            get { return Definition.Type; }
        }
        public bool HasComponent(string s)
        {
            return Definition.HasComponent(s);
        }
        public Component GetComponent(string s)
        {
            return Definition.GetComponent(s);
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
            if (Definition.HasComponent("cContainer"))
                Game.Containers.Add(ID, new List<Item>());
            Weight = 1;
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);

            Charged = !Definition.Stacking && Count > 0;
            if (Definition.HasComponent("cContainer"))
                Game.Containers.Add(ID, new List<Item>());
            Weight = 1;
        }

        public bool Known
        {
            get
            {
                return ItemDefinition.IdentifiedDefs.Contains(Definition.Type);
            }
        }

        private string UnknownApperance
        {
            get
            {
                return ItemDefinition.Appearances
                    [Definition.Category].Shuffle()
                    [
                        (Definition.Type + Math.Abs(Game.Seed))
                            %
                            ItemDefinition.Appearances[Definition.Category]
                                .Count
                    ];
            }
        }

        public string GetName(string format)
        {
            string apperance =
                Known
                    ? Definition.Name
                    : UnknownApperance;

            string result;

            //Stacking item, but only one of it
            if (format == "count")
                if (Count < 2 || !Stacking) format = "a";
            if (format == "Count")
                if (Count < 2 || !Stacking) format = "A";

            switch (format.ToLower())
            {
                case "name":
                    result = apperance;
                    break;
                case "a":
                    result =
                        Util.Article(apperance) +
                        " " +
                        apperance;
                    break;
                case "the":
                    result =
                        "the" +
                        " " +
                        apperance;
                    break;
                case "count":
                    result =
                        Count +
                        "x " +
                        apperance +
                        "s"; //Handled the single, stacking item above
                    break;
                default:
                    throw new ArgumentException();
            }

            if (format[0] >= 'A' && format[0] <= 'Z')
                result = Util.Capitalize(result);

            return result;
        }

        public void Identify(
            //bool cursed,
            //bool blessed,
            //bool mods
        ) {
            ItemDefinition.IdentifiedDefs.Add(Type);
        }

        //LH-031214: We want to switch this to depend on the actor strength
        //           as well later, since a strong dude could probably one-hand
        //           an orc corpse or whatever.
        //           We probably also want to check if the item gives strength
        //           (or removes strength) via mods, since if you can one-hand
        //           a two-hander with the strength that two-hander gives, you
        //           should only need one hand.
        public List<DollSlot> GetHands(Actor a)
        {
            int hands;

            WeaponComponent wc = (WeaponComponent)GetComponent("cWeapon");
            LauncherComponent lc = (LauncherComponent)GetComponent("cLauncher");

            if(wc != null) hands = wc.Hands;
            else if (lc != null) hands = lc.Hands;
            else hands = Weight > 1 ? 2 : 1;

            List<DollSlot> slots = new List<DollSlot>();
            for (int i = 0; i < hands; i++)
                slots.Add(DollSlot.Hand);

            return slots;
        }

        public void SpendCharge()
        {
            if (Stacking)
            {
                Count--;
                if (Count > 0) return;

                //LH-021214: Spent last of stacking item -> Remove it.
                Game.Player.Inventory.Remove(this);
                Game.Level.AllItems.Remove(this);
                Game.Level.WorldItems.Remove(this);
            }
            else
            {
                //LH-021214: Charging items are not removed when they hit 0
                //           charges, they remain, but uncharged (so they can
                //           potentially be recharged later).
                Count--;
            }
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
