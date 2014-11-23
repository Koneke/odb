using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ODB
{
    public class ItemDefinition : gObjectDefinition
    {
        public static ItemDefinition[] ItemDefinitions =
            new ItemDefinition[0xFFFF];

        public int AC;
        public string Damage;
        public bool stacking;
        public List<DollSlot> equipSlots;

        //creating a NEW definition
        public ItemDefinition(
            Color? bg, Color fg,
            string tile, string name,
            //item def specific stuff
            string Damage = "", int AC = 0,
            bool stacking = false, List<DollSlot> equipSlots = null)
        : base(bg, fg, tile, name) {
            this.Damage = Damage;
            this.AC = AC;
            this.stacking = stacking;
            this.equipSlots = equipSlots ?? new List<DollSlot>();
            ItemDefinitions[this.type] = this;
        }

        public ItemDefinition(string s) : base(s)
        {
            ReadItemDefinition(s);
        }

        public int ReadItemDefinition(string s)
        {
            int read = ReadGObjectDefinition(s);
            Damage = IO.ReadString(s, ref read, read);
            AC = IO.ReadHex(s, 2, ref read, read);
            stacking = IO.ReadBool(s, ref read, read);

            string slots = IO.ReadString(s, ref read, read);
            equipSlots = new List<DollSlot>();
            foreach (string ss in slots.Split(','))
                if(ss != "")
                    equipSlots.Add((DollSlot)int.Parse(ss));

            ItemDefinitions[type] = this;
            return read;
        }

        public string WriteItemDefinition()
        {
            string output = WriteGObjectDefinition();
            output += IO.Write(Damage);
            output += IO.WriteHex(AC, 2);
            output += IO.Write(stacking);
            foreach (DollSlot ds in equipSlots)
                output += (int)ds + ",";
            return output;
        }
    }

    public class Item : gObject
    {
        public static int IDCounter = 0;

        //instance specifics
        public int id;
        public int mod;
        public int count;
        public new ItemDefinition Definition;
        public List<Mod> Mods;

        //SPAWNING a NEW item
        public Item(
            Point xy, ItemDefinition def, int count = 1
        ) : base(xy, def) {
            id = IDCounter++;
            this.count = count;
            this.Definition = def;
            Mods = new List<Mod>();
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);
        }

        public string WriteItem()
        {
            //utilize our definition instead
            string s = base.WriteGOBject();
            s += IO.WriteHex(Definition.type, 4);
            s += IO.WriteHex(id, 4);
            s += IO.WriteHex(mod, 2); //even 1 should be enough, but eh
            s += IO.WriteHex(count, 2);
            foreach (Mod m in Mods)
            {
                s += IO.WriteHex((int)m.Type, 2);
                s += IO.Write(":", false);
                s += IO.WriteHex((int)m.Value, 2);
                s += ",";
            }
            s += ";";
            return s;
        }

        public int ReadItem(string s)
        {
            int read = base.ReadGOBject(s);
            Definition =
                ItemDefinition.ItemDefinitions[
                    IO.ReadHex(s, 4, ref read, read)
                ];
            id = IO.ReadHex(s, 4, ref read, read);
            mod = IO.ReadHex(s, 2, ref read, read);
            count = IO.ReadHex(s, 2, ref read, read);

            Mods = new List<Mod>();
            string modString = IO.ReadString(s, ref read, read);
            foreach (string ss in modString.Split(
                new char[]{','}, StringSplitOptions.RemoveEmptyEntries))
            {
                Mod m = new Mod(
                    (ModType)IO.ReadHex(ss.Split(':')[0]),
                    IO.ReadHex(ss.Split(':')[1])
                );
                Mods.Add(m);
            }

            return read;
        }
    }

}
