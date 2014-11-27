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
        PoisonRes
    }

    public class ActorDefinition : gObjectDefinition
    {
        public static ActorDefinition[] ActorDefinitions =
            new ActorDefinition[0xFFFF];

        public int strength, dexterity, intelligence;
        public int hpMax, mpMax;
        public int speed, quickness;
        public List<DollSlot> BodyParts;
        public int CorpseType;
        public List<int> Spellbook;
        public bool Named; //for uniques and what not

        public ActorDefinition(
            Color? bg, Color fg,
            string tile, string name,
            int strength, int dexterity, int intelligence, int hp,
            List<DollSlot> BodyParts,
            List<int> Spellbook,
            bool Named
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
            this.Spellbook = Spellbook ?? new List<int>();
            this.Named = Named;
        }

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
        }

        public Stream WriteActorDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Named);
            stream.Write(strength, 2);
            stream.Write(dexterity, 2);
            stream.Write(intelligence, 2);
            stream.Write(hpMax, 2);
            stream.Write(mpMax, 2);
            stream.Write(speed, 2);
            stream.Write(quickness, 2);

            foreach (DollSlot ds in BodyParts)
                stream.Write((int)ds + ",", false);
            stream.Write(";", false);

            stream.Write(CorpseType, 4);

            foreach (int spellId in Spellbook)
            {
                stream.Write(spellId, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }

        public Stream ReadActorDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Named = stream.ReadBool();
            strength = stream.ReadHex(2);
            dexterity = stream.ReadHex(2);
            intelligence = stream.ReadHex(2);
            hpMax = stream.ReadHex(2);
            mpMax = stream.ReadHex(2);
            speed = stream.ReadHex(2);
            quickness = stream.ReadHex(2);

            BodyParts = new List<DollSlot>();
            foreach (string ss in stream.ReadString().Split(','))
                if(ss != "")
                    BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = stream.ReadHex(4);

            if (Util.IDefByName(name + " corpse") == null)
            {
                //should be put into a func of its own..?
                ItemDefinition Corpse = new ItemDefinition(
                    null, Color.Red, "%", name + " corpse");
                CorpseType = Corpse.type;
            }

            Spellbook = new List<int>();
            string spellbook = stream.ReadString();
            foreach (string spell in spellbook.Split(','))
                if (spell != "") Spellbook.Add(IO.ReadHex(spell));

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
        public int mpCurrent;
        public int Cooldown;

        public List<BodyPart> PaperDoll;
        public List<Item> inventory;

        //not yet to save
        public List<TickingEffect> TickingEffects;
        #endregion

        #region temporary/cached (nonwritten)
        public bool[,] Vision;
        #endregion

        #region wraps
        public List<Spell> Spellbook {
            get {
                List<Spell> spells = new List<Spell>();
                foreach(int spellId in Definition.Spellbook) {
                    spells.Add(Spell.Spells[spellId]);
                }
                return spells;
            }
        }

        public int hpMax { get { return Definition.hpMax; } }
        public int mpMax { get { return Definition.mpMax; } }
        #endregion

        public Actor(
            Point xy, ActorDefinition def
        )
            : base(xy, def)
        {
            id = IDCounter++;

            Definition = def;
            this.hpCurrent = def.hpMax;
            this.mpCurrent = def.mpMax;
            Cooldown = 0;

            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in def.BodyParts)
                PaperDoll.Add(new BodyPart(ds));
            inventory = new List<Item>();
            TickingEffects = new List<TickingEffect>();
        }

        public Actor(string s)
            : base(s)
        {
            ReadActor(s);
        }

        public string GetName(bool def = false)
        {
            string name = (def ? "the" : Util.article(Definition.name));
            name += " ";

            if (Definition.Named) name = "";
            name += Definition.name;

            return name;
        }

        public bool CanEquip(List<DollSlot> slots)
        {
            List<DollSlot> availableSlots = new List<DollSlot>();
            foreach (BodyPart bp in PaperDoll)
                if (bp.Item == null) availableSlots.Add(bp.Type);

            bool canEquip = true;
            foreach (DollSlot ds in slots)
            {
                if (!availableSlots.Contains(ds))
                    canEquip = false;
                else availableSlots.Remove(ds);
            }
            return canEquip;
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
        public List<Item> GetEquippedItems()
        {
            List<Item> equipped = new List<Item>();
            foreach (BodyPart bp in PaperDoll)
                if (bp != null)
                    if (bp.Item != null)
                        if (!equipped.Contains(bp.Item))
                            equipped.Add(bp.Item);
            return equipped;
        }
        public List<BodyPart> GetSlots(DollSlot type)
        {
            List<BodyPart> parts = new List<BodyPart>();
            foreach (BodyPart bp in PaperDoll)
                if (bp.Type == type)
                    parts.Add(bp);
            return parts;
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
                case Stat.PoisonRes: return GetMod(stat);
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
                    addMod = ModType.AddDex; decMod = ModType.DecDex; break;
                case Stat.Intelligence:
                    addMod = ModType.AddInt; decMod = ModType.DecInt; break;
                case Stat.Speed:
                    addMod = ModType.AddSpd; decMod = ModType.DecSpd; break;
                case Stat.Quickness:
                    addMod = ModType.AddQck; decMod = ModType.DecQck; break;
                case Stat.PoisonRes:
                    addMod = ModType.PoisonRes; decMod = (ModType)0xFF; break;
                default:
                    return 0;
            }

            List<Item> worn = Util.GetWornItems(this);
            if((int)addMod != 0xFF)
                foreach (Mod m in Util.GetModsOfType(addMod, worn))
                    modifier += m.Value;
            if((int)decMod != 0xFF)
                foreach (Mod m in Util.GetModsOfType(decMod, worn))
                    modifier -= m.Value;

            return modifier;
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

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + Get(Stat.Strength);
            int dodgeRoll = target.GetAC();
            bool crit = Util.Roll("1d30") >= 30 -
                Math.Max(
                    Get(Stat.Dexterity)-5,
                    0
                );

            if (hitRoll >= dodgeRoll) {
                int damageRoll = Get(Stat.Strength);

                foreach (
                    BodyPart bp in PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand && x.Item != null)
                    )
                    if (
                        bp.Item.Definition.Damage != "" &&
                        //no bow damage when bashing with it kthx 
                        !bp.Item.Definition.Ranged
                    )
                        damageRoll += Util.Roll(
                            bp.Item.Definition.Damage,
                            crit
                        );
                    else
                        //barehanded/bash damage
                        damageRoll += Util.Roll("1d4", crit);

                Game.Log(
                    Definition.name + " strikes " +
                    target.Definition.name + (crit ? "!" : ".")
                );

                target.Damage(damageRoll);
            }
            else
            {
                Game.Log(Definition.name + " swings in the air.");
            }
        }
        public void Shoot(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + Get(Stat.Dexterity);
            int dodgeRoll =
                target.GetAC() + Util.Distance(xy, target.xy)
            ;

            Item ammo = null;
            foreach(BodyPart bp in PaperDoll)
                if(bp.Type == DollSlot.Quiver)
                    ammo = bp.Item;

            ammo.count--;
            if(ammo.count <= 0)
            {
                foreach(BodyPart bp in PaperDoll)
                    if(bp.Type == DollSlot.Quiver)
                        bp.Item = null;
                Game.Level.AllItems.Remove(ammo);
                Game.player.inventory.Remove(ammo);
            }

            if(hitRoll >= dodgeRoll) {
                int damageRoll = 0;
                Item weapon = null;
                foreach (BodyPart bp in GetSlots(DollSlot.Hand))
                {
                    if (bp.Item == null) continue;
                    if (bp.Item.Definition.Ranged)
                        weapon = bp.Item;
                }

                damageRoll = Util.Roll(weapon.Definition.Damage);
                damageRoll += Util.Roll(ammo.Definition.Damage);
                Game.Log(
                    target.Definition.name + " is hit! " +
                    "(" + hitRoll + " vs " + dodgeRoll + ")"
                );
                target.Damage(damageRoll);
            } else {
                Game.Log(
                    Definition.name + " misses " +
                    "(" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }

            Pass();
        }
        public void Damage(int d)
        {
            hpCurrent -= d;
            if (hpCurrent <= 0)
            {
                Game.Log(Definition.name + " dies!");
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

        public void Cast(Spell s, Point target, bool suppressMsg = false)
        {
            if(!suppressMsg)
                Game.Log(Definition.name + " casts " + s.Name + ".");
            if (Util.Roll("1d6") + Get(Stat.Intelligence) > s.CastDifficulty)
            {
                Projectile p = s.Cast(this, target);
                p.Move();
            }
            else if(suppressMsg)
                Game.Log("The spell fizzles.");
            Pass();
        }

        public bool HasEffect(TickingEffectDefinition def)
        {
            return TickingEffects.Any(x => x.Definition == def);
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
        public bool TryMove(Point offset)
        {
            bool moved = false;

            Tile target = Game.Level.Map[
                Game.player.xy.x + offset.x,
                Game.player.xy.y + offset.y
            ];

            bool legalMove = true;

            if (target == null)
                legalMove = false;
            else if (target.Door == Door.Closed || target.solid)
                legalMove = false;

            if (!legalMove)
            {
                offset = new Point(0, 0);
                if(this == Game.player)
                    Game.Log("Bump!");
            }
            else
            {
                if (Game.Level.ActorOnTile(target) == null)
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
                            Game.Log("You roll forwards!");

                    xy.Nudge(offset.x, offset.y);
                    moved = true;
                    Pass(true);
                }
                else
                {
                    Attack(Game.Level.ActorOnTile(target));
                    Game.player.Pass();
                }
            }
            return moved;
        }

        public Stream WriteActor()
        {
            Stream stream = WriteGOBject();
            stream.Write(Definition.type, 4);
            stream.Write(id, 4);
            stream.Write(hpCurrent, 2);
            stream.Write(mpCurrent, 2);
            stream.Write(Cooldown, 2);

            foreach (BodyPart bp in PaperDoll)
            {
                stream.Write((int)bp.Type, 2);
                stream.Write(":", false);

                if (bp.Item == null) stream.Write("X", false);
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

            foreach (TickingEffect te in TickingEffects)
            {
                stream.Write(te.WriteTickingEffect().ToString(), false);
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
            mpCurrent = stream.ReadHex(2);
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

            TickingEffects = new List<TickingEffect>();
            string tickers = stream.ReadString();
            foreach (string ticker in tickers.Split(','))
            {
                if (ticker == "") continue;
                TickingEffect te;
                TickingEffects.Add(te = new TickingEffect(ticker));
                te.Holder = this;
            }

            return stream;
        }

        public void ResetVision()
        {
            if (Vision == null)
                Vision = new bool[
                    Game.Level.LevelSize.x,
                    Game.Level.LevelSize.y
                ];
            for (int x = 0; x < Game.Level.LevelSize.x; x++)
                for (int y = 0; y < Game.Level.LevelSize.y; y++)
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