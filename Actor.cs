using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

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

    public class Actor : gObject
    {
        //LH-011214: Likewise here as in the definition, equality means that
        //           all the values contained are the same, not necessarily
        //           that it is the same reference.
        //           This might seem dumb, but, two actors should never have
        //           the same ID anyways, so they should test non-equal.
        protected bool Equals(Actor other)
        {
            bool paperDollEqual = PaperDoll.Count == other.PaperDoll.Count;
            if (!paperDollEqual) return false;
            if (PaperDoll.Where(
                (t, i) => !t.Equals(other.PaperDoll[i])).Any())
                return false;

            bool inventoryEqual = Inventory.Count == other.Inventory.Count;
            if (!inventoryEqual) return false;
            if (Inventory.Where(
                (t, i) => !t.Equals(other.Inventory[i])).Any())
                return false;

            bool tickingEffectsEqual =
                TickingEffects.Count == other.TickingEffects.Count;
            if (!tickingEffectsEqual) return false;
            if (TickingEffects.Where(
                (t, i) => !t.Equals(other.TickingEffects[i])).Any())
                return false;

            bool lastingEffectsEqual =
                LastingEffects.Count == other.LastingEffects.Count;
            if (!lastingEffectsEqual) return false;
            if (LastingEffects.Where(
                (t, i) => !t.Equals(other.LastingEffects[i])).Any())
                return false;

            bool intrinsicsEqual =
                Intrinsics.Count == other.Intrinsics.Count;
            if (!intrinsicsEqual) return false;
            if (Intrinsics.Where(
                (t, i) => !t.Equals(other.Intrinsics[i])).Any())
                return false;

            return
                base.Equals(other) &&
                ID == other.ID &&
                Equals(Definition, other.Definition) &&
                HpCurrent == other.HpCurrent &&
                MpCurrent == other.MpCurrent &&
                Cooldown == other.Cooldown &&
                //Equals(LastingEffects, other.LastingEffects) &&
                //Equals(Intrinsics, other.Intrinsics) &&
                Awake.Equals(other.Awake) &&
                Equals(Quiver, other.Quiver)
            ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ ID;
                hashCode = (hashCode*397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ HpCurrent;
                hashCode = (hashCode*397) ^ MpCurrent;
                hashCode = (hashCode*397) ^ Cooldown;
                //hashCode = (hashCode*397) ^ (PaperDoll != null ? PaperDoll.GetHashCode() : 0);
                //hashCode = (hashCode*397) ^ (Inventory != null ? Inventory.GetHashCode() : 0);
                //hashCode = (hashCode*397) ^ (TickingEffects != null ? TickingEffects.GetHashCode() : 0);
                //hashCode = (hashCode*397) ^ (LastingEffects != null ? LastingEffects.GetHashCode() : 0);
                //hashCode = (hashCode*397) ^ (Intrinsics != null ? Intrinsics.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Awake.GetHashCode();
                hashCode = (hashCode*397) ^ (Quiver != null ? Quiver.GetHashCode() : 0);
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Actor)obj);
        }

        public static int IDCounter = 0;

        #region written to save
        public int ID;

        public new ActorDefinition Definition;
        public int HpCurrent;
        public int MpCurrent;
        public int Cooldown;

        public List<BodyPart> PaperDoll;
        public List<Item> Inventory;
        public List<TickingEffect> TickingEffects;
        public List<LastingEffect> LastingEffects;
        public List<Mod> Intrinsics;

        public bool Awake;
        public Item Quiver;
        #endregion

        #region temporary/cached (nonwritten)
        public bool[,] Vision;
        #endregion

        #region wraps
        public List<Spell> Spellbook {
            get
            {
                return Definition.Spellbook.Select(
                    spellId => Spell.Spells[spellId]
                ).ToList();
            }
        }

        public int HpMax { get { return Definition.HpMax; } }
        public int MpMax { get { return Definition.MpMax; } }
        public bool IsAlive { get { return HpCurrent > 0; } }

        #endregion

        public Actor(
            Point xy, ActorDefinition definition
        )
            : base(xy, definition)
        {
            ID = IDCounter++;

            Definition = definition;
            HpCurrent = definition.HpMax;
            MpCurrent = definition.MpMax;
            Cooldown = 0;

            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in definition.BodyParts)
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

        //LH-021214: We have this function here because
        //           1. Named enemies and similar should get their name returned
        //              differently.
        //           2. Means we don't have to bother with capitalizing
        //              and crap like that everywhere else, just send a good
        //              format this-a-way.
        //           3. Automatically handle player being referred to as "you"
        //              instead of "Moribund" or whatever they choose.
        public string GetName(string format)
        {
            string result;

            if (Definition.Named && format[0] > 'Z')
                format = "name";
            else if (Definition.Named)
                format = "Name";

            switch (format.ToLower())
            {
                case "name":
                    result = Definition.Name;
                    break;
                case "a":
                    result = "a " + Definition.Name;
                    break;
                case "the":
                    result = "the " + Definition.Name;
                    break;
                default:
                    throw new ArgumentException();
            }

            if (ID == 0) result = "you";

            if (format[0] >= 'A' && format[0] <= 'Z')
                result = Util.Capitalize(result);

            return result;
        }

        public bool CanEquip(List<DollSlot> slots)
        {
            List<DollSlot> availableSlots = (
                    from bp in PaperDoll
                    where bp.Item == null
                    select bp.Type
                ).ToList();

            bool canEquip = true;
            foreach (DollSlot ds in slots)
            {
                if (!availableSlots.Contains(ds))
                    canEquip = false;
                else availableSlots.Remove(ds);
            }
            return canEquip;
        }
        public void Wield(Item item)
        {
            List<DollSlot> slots = item.GetHands();

            foreach (DollSlot ds in slots)
            {
                //ReSharper disable once AccessToForEachVariableInClosure
                //LH-011214: only reading value
                foreach (BodyPart bp in PaperDoll
                    .Where(bp => bp.Type == ds && bp.Item == null))
                {
                    bp.Item = item;
                    break;
                }
            }
        }
        public void Wear(Item it)
        {
            WearableComponent wc = 
                (WearableComponent)
                it.Definition.GetComponent("cWearable");

            foreach (DollSlot ds in wc.EquipSlots)
            {
                //ReSharper disable once AccessToForEachVariableInClosure
                //LH-011214: only reading value
                foreach (BodyPart bp in PaperDoll
                    .Where(bp => bp.Type == ds && bp.Item == null))
                {
                    bp.Item = it;
                    break;
                }
            }
        }
        public bool IsEquipped(Item it)
        {
            return PaperDoll.Any(x => x.Item == it) || Quiver == it;
        }
        public List<Item> GetEquippedItems()
        {
            List<Item> equipped = new List<Item>();
            foreach (
                BodyPart bp in
                from bp in PaperDoll
                    where bp != null
                    where bp.Item != null
                    where !equipped.Contains(bp.Item)
                select bp)
                equipped.Add(bp.Item);
            return equipped;
        }
        public List<BodyPart> GetSlots(DollSlot type)
        {
            return PaperDoll.Where(bp => bp.Type == type).ToList();
        }
        public void DropItem(Item item)
        {
            Game.Level.WorldItems.Add(item);
            Inventory.Remove(item);
        }

        public int Get(Stat stat, bool modded = true)
        {
            switch (stat)
            {
                case Stat.Strength:
                    return Definition.Strength +
                        (modded ? GetMod(stat) : 0);
                case Stat.Dexterity:
                    return Definition.Dexterity +
                        (modded ? GetMod(stat) : 0);
                case Stat.Intelligence:
                    return Definition.Intelligence +
                        (modded ? GetMod(stat) : 0);
                case Stat.Speed:
                    return Definition.Speed +
                        (modded ? GetMod(stat) : 0);
                case Stat.Quickness:
                    return Definition.Quickness +
                        (modded ? GetMod(stat) : 0);
                case Stat.PoisonRes: return GetMod(stat);
                default:
                    return -1;
            }
        }
        public int GetMod(Stat stat)
        {
            int modifier = 0;

            ModType mt;
            switch (stat)
            {
                case Stat.Strength:
                    mt = ModType.Strength; break;
                case Stat.Dexterity:
                    mt = ModType.Dexterity; break;
                case Stat.Intelligence:
                    mt = ModType.Intelligence; break;
                case Stat.Speed:
                    mt = ModType.Speed; break;
                case Stat.Quickness:
                    mt = ModType.Quickness; break;
                case Stat.PoisonRes:
                    mt = ModType.PoisonRes; break;
                default:
                    throw new ArgumentException();
            }

            List<Item> worn = Util.GetWornItems(this);

            //itembonuses
            modifier += Util.GetModsOfType(mt, worn).Sum(
                m => m.GetValue()
            );

            //intrinsics
            modifier += Util.GetModsOfType(mt, this).Sum(
                m => m.GetValue()
            );

            return modifier;
        }
        public int GetArmor()
        {
            List<Item> equipped = new List<Item>();
            foreach (BodyPart bp in PaperDoll.FindAll(
                x =>
                    //might seem dumb, but ds.Hand is currently for
                    //eh, like, the grip, more than the hand itself
                    //glove-hands currently do not exist..?
                    //idk, we'll get to it
                    x.Type != DollSlot.Hand &&
                    x.Item != null
                ).Where(bp => !equipped.Contains(bp.Item)))
                equipped.Add(bp.Item);

            return 8 + equipped
                .Select(
                    it => (WearableComponent)
                    it.Definition.GetComponent("cWearable"))
                .Select(wc => wc.ArmorClass).Sum();
        }

        public void Attack(Actor target)
        {
            //todo: Multiweapon fighting penalties?
            int hitRoll = Util.Roll("1d6") + Get(Stat.Strength);

            int dodgeRoll = target.GetArmor();
            bool crit = Util.Roll("1d30") >= 30 -
                Math.Max(
                    Get(Stat.Dexterity)-5,
                    0
                );

            if (hitRoll >= dodgeRoll) {
                int damageRoll = Get(Stat.Strength);

                foreach (WeaponComponent wc in PaperDoll
                    .FindAll(x => x.Type == DollSlot.Hand)
                    .Where(x => x.Item != null)
                    .Select(bp => bp.Item.Definition)
                    .Select(idef =>
                        (WeaponComponent)
                        idef.GetComponent("cWeapon"))
                ) {
                    //If the held item has a cWeapon, roll that damage
                    //Otherwise we go with standard bash damage
                    if (wc != null)
                        damageRoll += Util.Roll(wc.Damage, crit);
                    else damageRoll += Util.Roll("1d4", crit);
                }

                Game.Log(
                    Definition.Name + " strikes " +
                    target.Definition.Name + (crit ? "!" : ".")
                );

                target.Damage(damageRoll);
            }
            else
            {
                Game.Log(Definition.Name + " swings in the air.");
            }
        }
        public void Shoot(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + Get(Stat.Dexterity);
            int dodgeRoll = target.GetArmor() + Util.Distance(xy, target.xy);

            Item weapon = null;
            LauncherComponent lc = null;

            //Not currently considering have several launchers equipped.
            if (Game.Player.GetEquippedItems().Any(
                x => x.HasComponent("cLauncher")))
            {
                weapon = Game.Player.GetEquippedItems()
                    .Where(x => x.HasComponent("cLauncher"))
                    .ToList()[0];
                lc = (LauncherComponent)
                    weapon.GetComponent("cLauncher");
            }

            Item ammo = Quiver;
            Debug.Assert(ammo != null, "ammo != null");

            ProjectileComponent pc =
                (ProjectileComponent)
                ammo.GetComponent("cProjectile");

            bool throwing;

            //weapon and appropriate ammo
            if (weapon != null && lc != null)
                throwing = !lc.AmmoTypes.Contains(ammo.Type);
            //ammo
            else throwing = true;

            ammo.Count--;
            if(ammo.Count <= 0)
            {
                Quiver = null;
                Game.Level.AllItems.Remove(ammo);
                Game.Player.Inventory.Remove(ammo);
            }

            if(hitRoll >= dodgeRoll) {
                int damageRoll;

                if (throwing)
                {
                    damageRoll = Util.Roll(pc.Damage);
                }
                else
                {
                    damageRoll = Util.Roll(lc.Damage);
                    damageRoll += Util.Roll(pc.Damage);
                }

                Game.Log(
                    target.Definition.Name + " is hit! " +
                    "(" + hitRoll + " vs " + dodgeRoll + ")"
                );
                target.Damage(damageRoll);
            } else {
                Game.Log(
                    Definition.Name + " misses " +
                    "(" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }

            Game.Level.MakeNoise(1, target.xy);
            Pass();
        }
        public void Damage(int d)
        {
            HpCurrent -= d;
            if (HpCurrent > 0) return;

            Game.Log(Definition.Name + " dies!");
            Item corpse = new Item(
                xy,
                Util.ItemDefByName(Definition.Name + " corpse"),
                //ItemDefinition.ItemDefinitions[
                    //Definition.CorpseType],
                0, Intrinsics
                );
            //should always be ided
            //or maybe not..? could be a mechanic in and of itself
            corpse.Identify();
            Game.Level.WorldItems.Add(corpse);
            Game.Level.AllItems.Add(corpse);
            Game.Level.WorldActors.Remove(this);
        }

        public bool HasEffect(TickingEffectDefinition def)
        {
            return TickingEffects.Any(x => x.Definition == def);
        }
        public bool HasEffect(StatusType type)
        {
            return LastingEffects.Any(le => le.Type == type);
        }
        public void AddEffect(TickingEffectDefinition ted)
        {
            TickingEffects.Add(ted.Instantiate(this));
        }
        public void AddEffect(StatusType type, int duration)
        {
            LastingEffects.Add(new LastingEffect(type, duration));
        }
        public void RemoveEffect(StatusType type)
        {
            LastingEffects.Remove(
                LastingEffects.Find(effect => effect.Type == type)
            );
        }

        public void Eat(Item item)
        {
            if (item.Stacking) item.SpendCharge();
            else
            {
                Inventory.Remove(item);
                Game.Level.Despawn(item);
            }

            Game.Log(GetName("Name") + " ate " +
                item.GetName("a") + "."
            );

            EdibleComponent ec =
                (EdibleComponent)
                item.GetComponent("cEdible");

            Game.Food += ec.Nutrition;

            if (item.Mods.Count <= 0) return;
            if (Util.Roll("1d5") != 5) return;

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
            int count, n = count = item.Mods.Count;
            //count = 3 => r = 1d7
            int r = Util.Roll("1d" + (Math.Pow(2, count)-1));
            //less than 2^n-1 = "loss", check later intrinsics

            //ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            //LH-011214: resharper stop being dumb pls
            while (r < Math.Pow(2, n-1))
                n--;
            Intrinsics.Add(item.Mods[count - n]);
        }

        //movement/standard action
        //standard is e.g. attacking, manipulating inventory, etc.
        public void Pass(bool movement = false)
        {
            //switch this to +=? could mean setting cd to -10 = free action
            Cooldown = Game.StandardActionLength -
                (movement ? Get(Stat.Speed) : Get(Stat.Quickness));
        }
        public void Pass(int length)
        {
            Cooldown = length;
        }

        public List<Point> GetPossibleMoves(bool disallowActorTiles = false)
        {
            List<Point> possibleMoves = new List<Point>();

            for (int xx = -1; xx <= 1; xx++)
            {
                for (int yy = -1; yy <= 1; yy++)
                {
                    if(
                        xy.x + xx < 0 ||
                        xy.x + xx >= Game.Level.LevelSize.x ||
                        xy.y + yy < 0 ||
                        xy.y + yy >= Game.Level.LevelSize.y
                    ) continue;

                    bool legal = true;
                    Tile t = Game.Level.Map[xy.x + xx, xy.y + yy];

                    if (t == null) legal = false;
                    else if (t.Solid) legal = false;
                    else if (t.Door == Door.Closed) legal = false;

                    if(disallowActorTiles)
                        if (Game.Level.ActorOnTile(t) != null) legal = false;

                    if (legal) possibleMoves.Add(new Point(xx, yy));
                }
            }

            return possibleMoves;
        }
        //will atm only be called by the player,
        //but should, I guess, be called by monsters as well in the future
        public bool TryMove(Point offset)
        {
            List<Point> possiblesMoves = GetPossibleMoves();

            if(HasEffect(StatusType.Confusion))
            {
                if (Util.Roll("1d3") > 1)
                {
                    if (this == Game.Player) Game.Log("You stumble...");
                    offset = possiblesMoves
                        [Util.Random.Next(0, possiblesMoves.Count)];
                }
            }

            if (!GetPossibleMoves().Contains(offset))
            {
                if(this == Game.Player) Game.Log("Bump!");
                //else "... bumps into a wall..?"
                return false;
            }

            bool moved = false;

            Tile target = Game.Level.Map[
                xy.x + offset.x,
                xy.y + offset.y
            ];

            if (Game.Level.ActorOnTile(target) == null)
            {
                xy.Nudge(offset.x, offset.y);
                moved = true;
                Pass(true);

                //walking noise
                Game.Level.CalculateActorPositions();
                Game.Level.MakeNoise(0, xy);
            }
            else
            {
                Attack(Game.Level.ActorOnTile(target));
                Pass();

                //combat noise
                Game.Level.MakeNoise(1, xy);
            }

            return moved;
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
            foreach (Rect rr in r.Rects)
                for (int x = 0; x < rr.wh.x; x++)
                    for (int y = 0; y < rr.wh.y; y++)
                    {
                        Vision[
                            rr.xy.x + x,
                            rr.xy.y + y
                        ] = true;

                        if(this == Game.Player)
                            Game.Level.Seen[
                                rr.xy.x + x,
                                rr.xy.y + y
                            ] = true;
                    }
        }

        public Stream WriteActor()
        {
            Stream stream = WriteGObject();
            stream.Write(Definition.Type, 4);
            stream.Write(ID, 4);
            stream.Write(HpCurrent, 2);
            stream.Write(MpCurrent, 2);
            stream.Write(Cooldown, 2);

            foreach (BodyPart bp in PaperDoll)
            {
                stream.Write((int)bp.Type, 2);
                stream.Write(":", false);

                if (bp.Item == null) stream.Write("X", false);
                else stream.Write(bp.Item.ID, 4);

                stream.Write(",", false);
            }
            stream.Write(";", false);

            foreach (Item it in Inventory)
            {
                stream.Write(it.ID, 4);
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
                stream.Write((int)m.Type + ":" + m.RawValue + ",");
            }
            stream.Write(";", false);

            stream.Write(Awake);

            //todo: Write quiver to file

            return stream;
        }
        public Stream ReadActor(string s)
        {
            Stream stream = ReadGObject(s);
            Definition =
                ActorDefinition.ActorDefinitions[
                    stream.ReadHex(4)
                ];

            ID = stream.ReadHex(4);
            HpCurrent = stream.ReadHex(2);
            MpCurrent = stream.ReadHex(2);
            Cooldown = stream.ReadHex(2);

            PaperDoll = new List<BodyPart>();
            foreach (string ss in
                stream.ReadString().Split(
                    new[] { "," },
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
                    new[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                Inventory.Add(
                    Util.GetItemByID(IO.ReadHex(ss))
                );
            }

            TickingEffects = new List<TickingEffect>();
            string tickers = stream.ReadString();
            foreach (string ticker in tickers.Split(',')
                .Where(ticker => ticker != ""))
            {
                TickingEffect te;
                TickingEffects.Add(te = new TickingEffect(ticker));
                te.Holder = this;
            }

            LastingEffects = new List<LastingEffect>();
            string lasting = stream.ReadString();
            foreach (string effect in lasting.Split(',')
                .Where(effect => effect != ""))
            {
                LastingEffects.Add(new LastingEffect(effect));
            }

            Intrinsics = new List<Mod>();
            string intr = stream.ReadString();
            foreach (string mod in intr.Split(',')
                .Where(mod => mod != ""))
            {
                Intrinsics.Add(new Mod(
                    (ModType)IO.ReadHex(mod.Split(':')[0]),
                    IO.ReadHex(mod.Split(':')[1]))
                    );
            }

            Awake = stream.ReadBool();

            return stream;
        }

        public string Is()
        {
            return ID == 0
                ? "are"
                : "is";
        }
    }
}