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
        public int count;
        public new ItemDefinition Definition;

        //SPAWNING a NEW item
        public Item(
            Point xy, ItemDefinition def, int count = 1
        ) : base(xy, def) {
            id = IDCounter++;
            this.count = count;
            this.Definition = def;
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
            s += IO.WriteHex(count, 2);

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
            count = IO.ReadHex(s, 2, ref read, read);

            return read;
        }
    }

}
