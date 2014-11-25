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
            return stream;
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
            id = stream.ReadHex(4);
            mod = stream.ReadHex(2);
            count = stream.ReadHex(2);

            Mods = new List<Mod>();
            foreach (string ss in stream.ReadString().Split(
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
