using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ODB
{
    public enum Stat
    {
        Strength,
        Dexterity,
        Intelligence,
        Speed,
        Quickness,
    }

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

        public Stream WriteActorDefinition()
        {
            Stream stream = WriteGObjectDefinition();
            stream.Write(strength, 2);
            stream.Write(dexterity, 2);
            stream.Write(intelligence, 2);
            stream.Write(hpMax, 2);
            stream.Write(speed, 2);
            stream.Write(quickness, 2);

            foreach (DollSlot ds in BodyParts)
                stream.Write((int)ds + ",", false);
            stream.Write(";", false);
            stream.Write(CorpseType, 4);
            return stream;
        }

        public Stream ReadActorDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            strength = stream.ReadHex(2);
            dexterity = stream.ReadHex(2);
            intelligence = stream.ReadHex(2);
            hpMax = stream.ReadHex(2);
            speed = stream.ReadHex(2);
            quickness = stream.ReadHex(2);

            BodyParts = new List<DollSlot>();
            foreach (string ss in stream.ReadString().Split(','))
                if(ss != "")
                    BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = stream.ReadHex(4);

            ActorDefinitions[type] = this;
            return stream;
        }
    }

    public class Actor : gObject
    {
        public static int IDCounter = 0;

        #region written to save
        public int id;

        public new ActorDefinition Definition;
        public int hpCurrent;
        public int Cooldown;

        public List<BodyPart> PaperDoll;
        public List<Item> inventory;
        public List<Spell> Spellbook; //not yet written to file
        #endregion

        #region temporary/cached (nonwritten)
        public bool[,] Vision;
        #endregion

        public Actor(
            Point xy, ActorDefinition def
        )
            : base(xy, def)
        {
            id = IDCounter++;

            Definition = def;
            this.hpCurrent = def.hpMax;
            Cooldown = 0;

            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in def.BodyParts)
                PaperDoll.Add(new BodyPart(ds));
            inventory = new List<Item>();
            Spellbook = new List<Spell>();

            //not sure how this handles changing level sizes?
            //maybe we just won't have that, we'll experiment later
            Vision = new bool[Game.lvlW, Game.lvlH];
        }

        public Actor(string s)
            : base(s)
        {
            ReadActor(s);

            Vision = new bool[Game.lvlW, Game.lvlH];
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

        public int Get(Stat stat, bool modded = true)
        {
            switch (stat)
            {
                case Stat.Strength:
                    return Definition.strength +
                        (modded ? GetMod(stat) : 0);
                case Stat.Dexterity:
                    return Definition.dexterity +
                        (modded ? GetMod(stat) : 0);
                case Stat.Intelligence:
                    return Definition.intelligence +
                        (modded ? GetMod(stat) : 0);
                case Stat.Speed:
                    return Definition.speed +
                        (modded ? GetMod(stat) : 0);
                case Stat.Quickness:
                    return Definition.quickness +
                        (modded ? GetMod(stat) : 0);
                default:
                    return -1;
            }
        }

        public int GetMod(Stat stat)
        {
            int modifier = 0;

            ModType addMod, decMod;
            switch (stat)
            {
                case Stat.Strength:
                    addMod = ModType.AddStr; decMod = ModType.DecStr; break;
                case Stat.Dexterity:
                    addMod = ModType.AddStr; decMod = ModType.DecStr; break;
                case Stat.Intelligence:
                    addMod = ModType.AddInt; decMod = ModType.DecInt; break;
                case Stat.Speed:
                    addMod = ModType.AddSpd; decMod = ModType.DecSpd; break;
                case Stat.Quickness:
                    addMod = ModType.AddQck; decMod = ModType.DecQck; break;
                default:
                    return 0;
            }

            List<Item> worn = Util.GetWornItems(this);
            foreach (Mod m in Util.GetModsOfType(addMod, worn))
                modifier += m.Value;
            foreach (Mod m in Util.GetModsOfType(decMod, worn))
                modifier -= m.Value;

            return modifier;
        }

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + Get(Stat.Dexterity);
            int dodgeRoll = target.GetAC();

            if (hitRoll >= dodgeRoll) {
                int damageRoll = Get(Stat.Strength);

                foreach (
                    BodyPart bp in PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand && x.Item != null)
                    )
                    if (bp.Item.Definition.Damage != "")
                        damageRoll += Util.Roll(bp.Item.Definition.Damage);
                    else
                        //barehanded/bash damage
                        damageRoll += Util.Roll("1d4");

                Game.log.Add(
                    Definition.name + " strikes " +target.Definition.name +
                    " (" + hitRoll + " vs AC" + dodgeRoll + ")"
                );

                target.Damage(damageRoll);
            }
            else
            {
                Game.log.Add(Definition.name + " swings in the air." +
                    " (" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }
        }
        public void Damage(int d)
        {
            hpCurrent -= d;
            if (hpCurrent <= 0)
            {
                Game.log.Add(Definition.name + " dies!");
                Item corpse = new Item(
                    xy,
                    ItemDefinition.ItemDefinitions[
                        Definition.CorpseType]
                );
                Game.Level.WorldItems.Add(corpse);
                Game.Level.AllItems.Add(corpse);
                Game.Level.WorldActors.Remove(this);
            }
        }

        public void Cast(Spell s, Point target)
        {
            if (Util.Roll("1d6") + Get(Stat.Intelligence) > s.CastDifficulty)
            {
                Game.log.Add(Definition.name + " casts " + s.Name + ".");
                Projectile p = s.Cast(this, target);
                Game.projectiles.Add(p);
                //all projectiles are instant move
                //atleast right now
                //so just go ahead and move as soon as we cast
                p.Move();
            }
            else Game.log.Add("The spell fizzles.");
            Pass();
        }

        //movement/standard action
        //standard is e.g. attacking, manipulating inventory, etc.
        public void Pass(bool movement = false)
        {
            Cooldown = Game.standardActionLength -
                (movement ? Get(Stat.Speed) : Get(Stat.Quickness));
        }

        //will atm only be called by the player,
        //but should, I guess, be called by monsters as well in the future
        public void TryMove(Point offset)
        {
            Tile target = Game.Level.Map[
                Game.player.xy.x + offset.x,
                Game.player.xy.y + offset.y
            ];

            bool legalMove = true;

            if (target == null)
                legalMove = false;
            else if (target.doorState == Door.Closed || target.solid)
                legalMove = false;

            if (!legalMove)
            {
                offset = new Point(0, 0);
                if(this == Game.player)
                    Game.log.Add("Bump!");
            }
            else
            {
                if (Game.Level.ActorsOnTile(target).Count <= 0)
                {
                    int numberOfLegs = 0;
                    int numberOfFreeHands = 0;
                    foreach (BodyPart bp in PaperDoll)
                    {
                        if (bp.Type == DollSlot.Legs) numberOfLegs++;
                        if (bp.Type == DollSlot.Hand && bp.Item == null)
                            numberOfFreeHands++;
                    }
                    if (!(numberOfLegs >= 1 || numberOfFreeHands > 2))
                        if(this == Game.player)
                            Game.log.Add("You roll forwards!");

                    xy.Nudge(offset.x, offset.y);
                    Pass(true);
                }
                else
                {
                    //should only be 1, but... eh
                    foreach (Actor a in Game.Level.ActorsOnTile(target))
                        Attack(a);
                    Game.player.Pass();
                }
            }
        }

        public Stream WriteActor()
        {
            Stream stream = WriteGOBject();
            stream.Write(Definition.type, 4);
            stream.Write(id, 4);
            stream.Write(hpCurrent, 2);
            stream.Write(Cooldown, 2);

            foreach (BodyPart bp in PaperDoll)
            {
                stream.Write((int)bp.Type, 2);
                stream.Write(":", false);

                if (bp.Item == null) stream.Write("XXXX", false);
                else stream.Write(bp.Item.id, 4);

                stream.Write(",", false);
            }
            stream.Write(";", false);

            foreach (Item it in inventory)
            {
                stream.Write(it.id, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            foreach (Spell s in Spellbook)
            {
                stream.Write(s.id, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }
        public Stream ReadActor(string s)
        {
            Stream stream = ReadGOBject(s);
            Definition =
                ActorDefinition.ActorDefinitions[
                    stream.ReadHex(4)
                ];

            id = stream.ReadHex(4);
            hpCurrent = stream.ReadHex(2);
            Cooldown = stream.ReadHex(2);

            PaperDoll = new List<BodyPart>();
            foreach (string ss in
                stream.ReadString().Split(
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
                stream.ReadString().Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                inventory.Add(
                    Util.GetItemByID(IO.ReadHex(ss))
                );
            }

            Spellbook = new List<Spell>();
            foreach (string ss in
                stream.ReadString().Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                Spellbook.Add(Spell.Spells[IO.ReadHex(ss)]);
            }

            return stream;
        }

        public void ResetVision()
        {
            for (int x = 0; x < Game.lvlW; x++)
                for (int y = 0; y < Game.lvlH; y++)
                    Vision[x, y] = false;
        }

        public void AddRoomToVision(Room r)
        {
            foreach (Rect rr in r.rects)
                for (int x = 0; x < rr.wh.x; x++)
                    for (int y = 0; y < rr.wh.y; y++)
                    {
                        Vision[
                            rr.xy.x + x,
                            rr.xy.y + y
                        ] = true;

                        if(this == Game.player)
                            Game.Level.Seen[
                                rr.xy.x + x,
                                rr.xy.y + y
                            ] = true;
                    }
        }
    }
}