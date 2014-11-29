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

        public bool Named; //for uniques and what not
        public int strength, dexterity, intelligence;
        public int speed, quickness;
        public int hpMax, mpMax;
        public List<DollSlot> BodyParts;
        public int CorpseType;
        public List<int> Spellbook;
        //intrinsics spawned with
        public List<Mod> SpawnIntrinsics;

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
            this.BodyParts = BodyParts ?? new List<DollSlot>();
            ActorDefinitions[this.type] = this;
            ItemDefinition Corpse = new ItemDefinition(
                null, Color.Red, "%", name + " corpse");
            CorpseType = Corpse.type;
            this.Spellbook = Spellbook ?? new List<int>();
            this.Named = Named;
            SpawnIntrinsics = new List<Mod>();
        }

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
        }

        public void Set(Stat stat, int value)
        {
            switch (stat)
            {
                case Stat.Strength:
                    strength = value;
                    break;
                case Stat.Dexterity:
                    dexterity = value;
                    break;
                case Stat.Intelligence:
                    intelligence = value;
                    break;
                case Stat.Speed:
                    speed = value;
                    break;
                case Stat.Quickness:
                    quickness = value;
                    break;
                case Stat.PoisonRes:
                    //teh pRes, it does nathing
                default:
                    Util.Game.Log("Bad stat.");
                    break;
            }
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

            foreach (Mod m in SpawnIntrinsics)
            {
                stream.Write((int)m.Type, 4);
                stream.Write(":", false);
                stream.Write(m.Value, 4);
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

            SpawnIntrinsics = new List<Mod>();
            string sintrinsics = stream.ReadString();
            foreach (string mod in sintrinsics.Split(','))
            {
                if (mod == "") continue;
                Mod m = new Mod(
                    (ModType)IO.ReadHex(mod.Split(':')[0]),
                             IO.ReadHex(mod.Split(':')[1])
                );
                SpawnIntrinsics.Add(m);
            }

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
        public List<Item> Inventory;
        public List<TickingEffect> TickingEffects;
        public List<LastingEffect> LastingEffects;
        public List<Mod> Intrinsics;

        public bool Awake;
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
            Inventory = new List<Item>();
            TickingEffects = new List<TickingEffect>();
            Intrinsics = new List<Mod>(Definition.SpawnIntrinsics);
            Awake = false;
            LastingEffects = new List<LastingEffect>();
        }

        public Actor(string s)
            : base(s)
        {
            ReadActor(s);
        }

        public string GetName(bool def = false, bool capitalize = false)
        {
            if (this == Game.player)
                return capitalize ? "You" : "you";

            string name = (def ? "the" : Util.article(Definition.name));
            name += " ";

            if (Definition.Named) name = "";
            name += Definition.name;

            if (capitalize)
                name = name.Substring(0, 1).ToUpper() +
                    name.Substring(1, name.Length - 1);

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
            //no bonus from quivered itesm
            foreach (BodyPart bp in GetSlots(DollSlot.Quiver))
                if(bp.Item != null)
                    worn.Remove(bp.Item);

            if ((int)addMod != 0xFF)
            {
                foreach (Mod m in Util.GetModsOfType(addMod, worn))
                    modifier += m.Value;
                foreach (Mod m in Util.GetModsOfType(addMod, this))
                    modifier += m.Value;
            }
            if ((int)decMod != 0xFF)
            {
                foreach (Mod m in Util.GetModsOfType(decMod, worn))
                    modifier -= m.Value;
                foreach (Mod m in Util.GetModsOfType(decMod, this))
                    modifier -= m.Value;
            }

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

            Item weapon = null, ammo = null;

            foreach (BodyPart bp in GetSlots(DollSlot.Hand))
            {
                if (bp.Item == null) continue;
                if (bp.Item.Definition.Ranged)
                    weapon = bp.Item;
            }

            foreach(BodyPart bp in PaperDoll)
                if(bp.Type == DollSlot.Quiver)
                    ammo = bp.Item;

            bool throwing = false;
            if (!(ammo == null))
            {
                //weapon and appropriate ammo
                if (weapon != null)
                {
                    if (weapon.Definition.AmmoTypes.Contains(ammo.type))
                        throwing = false;
                    else throwing = true;
                }
                //ammo
                else throwing = true;
            }

            ammo.count--;
            if(ammo.count <= 0)
            {
                foreach(BodyPart bp in PaperDoll)
                    if(bp.Type == DollSlot.Quiver)
                        bp.Item = null;
                Game.Level.AllItems.Remove(ammo);
                Game.player.Inventory.Remove(ammo);
            }

            if(hitRoll >= dodgeRoll) {
                int damageRoll = 0;

                if (throwing)
                {
                    damageRoll = Util.Roll(ammo.Definition.RangedDamage);
                }
                else
                {
                    damageRoll = Util.Roll(weapon.Definition.Damage);
                    damageRoll += Util.Roll(ammo.Definition.RangedDamage);
                }

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
                        Definition.CorpseType],
                    0, Intrinsics
                );
                //should always be ided
                //or maybe not..? could be a mechanic in and of itself
                corpse.Identify();
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
        public bool HasEffect(StatusType type)
        {
            foreach (LastingEffect le in LastingEffects)
                if (le.Type == type) return true;
            return false;
        }

        public void Eat(Item it)
        {
            if(it.count > 1)
                Game.Log(GetName(false, true) + " ate " +
                    Util.article(it.GetName(false, true)) + " " +
                    it.GetName(false, true)
                );
            else
                Game.Log(GetName(false, true) + " ate " + it.GetName());
            if (Util.Roll("1d5") == 5)
            {
                //idea here is that earlier intrinsics in the list are more
                //"primary" attributes of the food (usually corpse if it has
                //intrinsics), so the weighting is done so that the earlier
                //intrinsics are a lot more likely, every intrinsic being double
                //as likely as the next one in the list
                //ex., 3 mods
                //weight looks like
                //2110000 (4 mods = 322111100000000 etc)
                //(number being index in list)
                //so bigger chance for mods earlier in the list
                int count, n = count = it.Mods.Count;
                //count = 3 => r = 1d7
                int r = Util.Roll("1d" + (Math.Pow(2, count)-1));
                //less than 2^n-1 = "loss", check later intrinsics
                while (r < Math.Pow(2, n-1))
                    n--;
                Intrinsics.Add(it.Mods[count - n]);
            }
        }

        //movement/standard action
        //standard is e.g. attacking, manipulating inventory, etc.
        public void Pass(bool movement = false)
        {
            //switch this to +=? could mean setting cd to -10 = free action
            Cooldown = Game.standardActionLength -
                (movement ? Get(Stat.Speed) : Get(Stat.Quickness));
        }
        public void Pass(int length)
        {
            Cooldown = length;
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

            if (moved) Game.Level.CalculateActorPositions();

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

            foreach (Item it in Inventory)
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

            foreach (LastingEffect le in LastingEffects)
            {
                stream.Write(le.WriteLastingEffect().ToString(), false);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            foreach (Mod m in Intrinsics)
            {
                stream.Write((int)m.Type + ":" + m.Value + ",");
            }
            stream.Write(";", false);

            stream.Write(Awake);

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

            Inventory = new List<Item>();
            foreach (string ss in
                stream.ReadString().Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                Inventory.Add(
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

            LastingEffects = new List<LastingEffect>();
            string lasting = stream.ReadString();
            foreach (string effect in lasting.Split(','))
            {
                if (effect == "") continue;
                LastingEffects.Add(new LastingEffect(effect));
            }

            Intrinsics = new List<Mod>();
            string intr = stream.ReadString();
            foreach (string mod in intr.Split(','))
            {
                if (mod == "") continue;
                Intrinsics.Add(new Mod(
                    (ModType)IO.ReadHex(mod.Split(':')[0]),
                             IO.ReadHex(mod.Split(':')[1]))
                );
            }

            Awake = stream.ReadBool();

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