using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ODB
{
    public class ActorDefinition : gObjectDefinition
    {
        public static ActorDefinition[] ActorDefinitions =
            new ActorDefinition[0xFFFF];

        public int strength, dexterity, intelligence, hpMax;
        public int speed, quickness;
        public List<DollSlot> BodyParts;
        public int CorpseType;

        public ActorDefinition(
            Color? bg, Color fg,
            string tile, string name,
            int strength, int dexterity, int intelligence, int hp,
            List<DollSlot> BodyParts
        )
        : base(bg, fg, tile, name) {
            this.strength = strength;
            this.dexterity = dexterity;
            this.intelligence = intelligence;
            this.hpMax = hp;
            this.BodyParts = BodyParts;
            ActorDefinitions[this.type] = this;
            ItemDefinition Corpse = new ItemDefinition(
                null, Color.Red, "%", name + " corpse");
            CorpseType = Corpse.type;
        }

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
        }

        public string WriteActorDefinition()
        {
            string output = WriteGObjectDefinition();
            output += IO.WriteHex(strength, 2);
            output += IO.WriteHex(dexterity, 2);
            output += IO.WriteHex(intelligence, 2);
            output += IO.WriteHex(hpMax, 2);
            output += IO.WriteHex(speed, 2);
            output += IO.WriteHex(quickness, 2);
            foreach (DollSlot ds in BodyParts)
                output += (int)ds + ",";
            output += ";";
            output += IO.WriteHex(CorpseType, 4);
            return output;
        }

        public int ReadActorDefinition(string s)
        {
            int read = ReadGObjectDefinition(s);
            strength = IO.ReadHex(s, 2, ref read, read);
            dexterity = IO.ReadHex(s, 2, ref read, read);
            intelligence = IO.ReadHex(s, 2, ref read, read);
            hpMax = IO.ReadHex(s, 2, ref read, read);
            speed = IO.ReadHex(s, 2, ref read, read);
            quickness = IO.ReadHex(s, 2, ref read, read);

            BodyParts = new List<DollSlot>();
            foreach (string ss in IO.ReadString(s, ref read, read).Split(','))
                if(ss != "")
                    BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = IO.ReadHex(s, 4, ref read, read);

            ActorDefinitions[type] = this;
            return read;
        }
    }

    public class Actor : gObject
    {
        public static int IDCounter = 0;
        public int id;

        public new ActorDefinition Definition;

        public int hpCurrent;

        public int Cooldown;

        public List<BodyPart> PaperDoll;
        public List<Item> inventory;

        public Actor(
            Point xy, ActorDefinition def
        )
            : base(xy, def)
        {
            Definition = def;
            id = IDCounter++;
            this.hpCurrent = def.hpMax;
            inventory = new List<Item>();
            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in def.BodyParts)
                PaperDoll.Add(new BodyPart(ds));
            Cooldown = 0;
        }

        public Actor(string s)
            : base(s)
        {
            ReadActor(s);
        }

        public bool HasFree(DollSlot slot)
        {
            return PaperDoll.Any(
                x => x.Type == slot &&
                x.Item == null
            );
        }

        public void Equip(Item it)
        {
            foreach (DollSlot ds in it.Definition.equipSlots)
            {
                foreach(BodyPart bp in PaperDoll)
                    if (bp.Type == ds && bp.Item == null)
                    {
                        bp.Item = it;
                        break;
                    }
            }
        }

        public bool IsEquipped(Item it)
        {
            return PaperDoll.Any(x => x.Item == it);
        }

        public int GetAC()
        {
            int ac = 8;
            List<Item> equipped = new List<Item>();
            foreach (
                BodyPart bp in PaperDoll.FindAll(
                    x =>
                        //might seem dumb, but ds.Hand is currently for
                        //eh, like, the grip, more than the hand itself
                        //glove-hands currently do not exist..?
                        //idk, we'll get to it
                        x.Type != DollSlot.Hand &&
                        x.Item != null
                    )
                )
                if(!equipped.Contains(bp.Item))
                    equipped.Add(bp.Item);

            foreach(Item it in equipped)
                ac += it.Definition.AC + it.mod;

            return ac;
        }

        //we can probably condense these a bit by doing something cool with
        //the stats. so you'd do like player.Get(Stat.Strength) or something
        //instead, but that's a later thing, it works atm.

        public int GetStrength(bool modded = true)
        {
            return Definition.strength +
                (modded ? GetStrengthMod() : 0); 
        }
        public int GetStrengthMod()
        {
            int modifier = 0;

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(ModType.AddStr, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(ModType.DecStr, worn))
                modifier -= m.Value;

            return modifier;
        }

        public int GetDexterity(bool modded = true)
        {
            return Definition.dexterity +
                (modded ? GetDexterityMod() : 0); 
        }
        public int GetDexterityMod()
        {
            int modifier = 0;

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(ModType.AddDex, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(ModType.DecDex, worn))
                modifier -= m.Value;

            return modifier;
        }

        public int GetIntelligence(bool modded = true)
        {
            return Definition.intelligence +
                (modded ? GetIntelligenceMod() : 0); 
        }
        public int GetIntelligenceMod()
        {
            int modifier = 0;

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(ModType.AddInt, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(ModType.DecInt, worn))
                modifier -= m.Value;

            return modifier;
        }

        public int GetQuickness(bool modded = true)
        {
            return Definition.quickness +
                (modded ? GetQuicknessMod() : 0);
        }
        public int GetQuicknessMod()
        {
            int modifier = 0;

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(ModType.AddQck, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(ModType.DecQck, worn))
                modifier -= m.Value;

            return modifier;
        }

        public int GetSpeed(bool modded = true)
        {
            return Definition.speed +
                (modded ? GetSpeedMod() : 0);
        }
        public int GetSpeedMod()
        {
            int modifier = 0;

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(ModType.AddSpd, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(ModType.DecSpd, worn))
                modifier -= m.Value;

            return modifier;
        }

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + GetDexterity();
            int dodgeRoll = target.GetAC();

            if (hitRoll >= dodgeRoll) {
                int damageRoll = GetStrength();

                foreach (
                    BodyPart bp in PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand && x.Item != null)
                    )
                    if (bp.Item.Definition.Damage != "")
                        damageRoll += Util.Roll(bp.Item.Definition.Damage);
                    else
                        //barehanded/bash damage
                        damageRoll += Util.Roll("1d4");

                target.hpCurrent -= damageRoll;

                Game.log.Add(
                    Definition.name + " strikes " +target.Definition.name +
                    " (" + hitRoll + " vs AC" + dodgeRoll + ")" +
                    " (-" + damageRoll + "hp)"
                );

                if (target.hpCurrent <= 0)
                {
                    Game.log.Add(target.Definition.name + " dies!");
                    Item corpse = new Item(
                        target.xy,
                        ItemDefinition.ItemDefinitions[
                            target.Definition.CorpseType]
                    );
                    Game.worldItems.Add(corpse);
                    Game.allItems.Add(corpse);
                    Game.worldActors.Remove(target);
                }
            }
            else
            {
                Game.log.Add(Definition.name + " swings in the air." +
                    " (" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }
        }

        //movement/standard action
        //standard is e.g. attacking, manipulating inventory, etc.
        public void Pass(bool movement = false)
        {
            Cooldown = Game.standardActionLength -
                (movement ? GetSpeed() : GetQuickness());
        }

        public string WriteActor()
        {
            string output = base.WriteGOBject();
            output += IO.WriteHex(Definition.type, 4);
            output += IO.WriteHex(id, 4);
            output += IO.WriteHex(hpCurrent, 2);
            output += IO.WriteHex(Cooldown, 2);

            foreach (BodyPart bp in PaperDoll)
            {
                output += IO.WriteHex((int)bp.Type, 2) + ":";

                if (bp.Item == null) output += "XXXX";
                else output += IO.WriteHex(bp.Item.id, 4);

                output += ",";
            }
            output += ";";

            foreach (Item it in inventory)
                output += IO.WriteHex(it.id, 4) + ",";
            output += ";";

            return output;
        }
        public int ReadActor(string s)
        {
            int read = base.ReadGOBject(s);
            Definition =
                ActorDefinition.ActorDefinitions[
                    IO.ReadHex(s, 4, ref read, read)
                ];
            id = IO.ReadHex(s, 4, ref read, read);
            hpCurrent = IO.ReadHex(s, 2, ref read, read);
            Cooldown = IO.ReadHex(s, 2, ref read, read);

            PaperDoll = new List<BodyPart>();
            foreach (string ss in
                IO.ReadString(s, ref read, read).Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                DollSlot type =
                        (DollSlot)IO.ReadHex(ss.Split(':')[0]);
                Item item = 
                        ss.Split(':')[1].Contains("X") ?
                            null :
                            Util.GetItemByID(IO.ReadHex(ss.Split(':')[1]));
                PaperDoll.Add(new BodyPart(type, item));
            }

            inventory = new List<Item>();
            foreach (string ss in
                IO.ReadString(s, ref read, read).Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                Item it = Util.GetItemByID(IO.ReadHex(ss));
                inventory.Add(it);
                Game.worldItems.Remove(it);
            }

            return read;
        }
    }

}
