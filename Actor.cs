using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ODB
{
    [DataContract]
    public class Actor
    {
        public ActorDefinition Definition
        {
            get { return ActorDefinition.DefDict[ActorType]; }
        }

        [DataMember] public int ID;
        [DataMember] private ActorID ActorType;
        [DataMember] public Point xy;
        [DataMember] public int LevelID;

        [DataMember] public ActorStats Stats;

        [DataMember] public int Xplevel;
        [DataMember] public int ExperiencePoints;
        [DataMember] public int Cooldown;

        [DataMember] private int _food;
        public int Food {
            get { return _food; }
            set
            {
                FoodStatus fs = GetFoodStatus();
                int before = _food;
                _food = value;

                if (fs != GetFoodStatus())
                    UpdateFoodStatus(before < _food);
            }
        }

        [DataMember] private int? _quiver;
        public Item Quiver {
            get { return _quiver == null
                ? null
                : Util.GetItemByID(_quiver.Value); }
            set
            {
                if (value == null) _quiver = null;
                else _quiver = value.ID;
            }
        }

        [DataMember] private Doll _doll;
        public List<BodyPart> PaperDoll { get { return _doll.Get(); } }

        [DataMember] private List<int> _inventory;
        public ReadOnlyCollection<Item> Inventory
        {
            get
            {
                return _inventory
                    .Select(Util.GetItemByID)
                    .Where(item => item != null)
                    .ToList()
                    .AsReadOnly();
            }
        }

        [DataMember] private List<LastingEffect> _lastingEffects;
        public List<LastingEffect> LastingEffects
        {
            get { return _lastingEffects; }
        }

        [DataMember] public List<Mod> Intrinsics;

        private BurdenStatus _carried;

        private bool[,] _vision;
        public List<Spell> Spellbook {
            get
            {
                return Definition.Spellbook.Select(
                    spellId => Spell.SpellDict[spellId]
                ).ToList();
            }
        }
        public bool IsAlive { get { return Stats.HpCurrent > 0; } }

        public Actor() { }

        public Actor(
            Point xy,
            ActorDefinition definition,
            int xplevel
        ) {
            ID = Game.IDCounter++;
            ActorType = definition.ActorType;
            this.xy = xy;

            Stats = new ActorStats(this);

            Stats.Strength = Util.Roll(definition.Strength);
            Stats.Dexterity = Util.Roll(definition.Dexterity);
            Stats.Intelligence = Util.Roll(definition.Intelligence);

            Stats.HpMax = Util.Roll(definition.HitDie, true);
            for (int i = 0; i < xplevel - 1; i++)
                Stats.HpMax += Math.Max(
                    Util.Roll(definition.HitDie),
                    Stats.Strength
                );

            Stats.MpMax = Util.Roll(definition.ManaDie, true);
            for (int i = 0; i < xplevel - 1; i++)
                Stats.MpMax += Math.Max(
                    Util.Roll(definition.ManaDie),
                    Stats.Intelligence
                );

            Stats.HpCurrent = Stats.HpMax;
            Stats.MpCurrent = Stats.MpMax;

            Xplevel = xplevel;
            ExperiencePoints = RequiredExperienceForLevel(xplevel);

            Cooldown = 0;

            _food = 9000;

            _doll = new Doll();
            foreach (DollSlot ds in definition.BodyParts)
                _doll.Add(
                    ds == DollSlot.Hand
                    ? new Hand(ds)
                    : new BodyPart(ds)
                );
            _inventory = new List<int>();
            Intrinsics = new List<Mod>(Definition.SpawnIntrinsics);
            _lastingEffects = new List<LastingEffect>();

            Stats.HpRegCooldown = 10;
            Stats.MpRegCooldown = 30 - Stats.Intelligence;
        }

        public string GetName(string format, bool realname = false)
        {
            string result;

            if (Definition.Named && ID != 0)
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

            if (ID == 0 && !realname) result = "you";

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
            if (item.HasComponent<AttackComponent>())
                item.Identify();

            List<DollSlot> slots = item.GetHands(this);

            foreach (DollSlot ds in slots)
            {
                //ReSharper disable once AccessToForEachVariableInClosure
                //LH-011214: only reading value
                foreach (BodyPart bp in PaperDoll
                    .Where(bp =>
                        bp.Type == ds &&
                        bp.Item == null))
                {
                    bp.Item = item;
                    ((Hand)bp).Wielding = true;
                    break;
                }
            }
        }
        public void Wear(Item item)
        {
            WearableComponent wc = item.GetComponent<WearableComponent>();
            item.Identify();

            foreach (DollSlot ds in wc.EquipSlots)
            {
                //ReSharper disable once AccessToForEachVariableInClosure
                //LH-011214: only reading value
                foreach (BodyPart bp in PaperDoll
                    .Where(bp => bp.Type == ds && bp.Item == null))
                {
                    bp.Item = item;
                    if (bp.Type == DollSlot.Hand)
                        ((Hand)bp).Wielding = false;
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
        public bool IsWorn(Item item)
        {
            return GetWornItems().Contains(item);
        }
        public List<Item> GetWornItems()
        {
            List<Item> equipped = new List<Item>();
            foreach (
                BodyPart bp in
                from bp in PaperDoll
                    where bp != null
                    where bp.Item != null
                    where
                        bp.Type != DollSlot.Hand ||
                        bp.Item.HasTag(ItemTag.NonWeapon)
                    where !equipped.Contains(bp.Item)
                    where bp.Item.HasComponent<WearableComponent>()
                select bp)
                equipped.Add(bp.Item);
            return equipped;
        }
        public bool IsWielded(Item item)
        {
            return GetWieldedItems().Contains(item);
        }
        public List<Item> GetWieldedItems()
        {
            return PaperDoll
                .Where(bp => bp != null)
                .Where(bp => bp.Type == DollSlot.Hand)
                //note, just assuming we can do this cast, since, at least at
                //the moment, the only things we consider wielded are in the
                //hands of the actor, which should be castable to Hand.
                .Where(bp => ((Hand)bp).Wielding)
                .Where(bp => bp.Item != null)
                .Select(bp => bp.Item)
                .Distinct()
                .ToList();
        }
        public List<BodyPart> GetSlots(DollSlot type)
        {
            return PaperDoll.Where(bp => bp.Type == type).ToList();
        }
        public void DropItem(Item item)
        {
            World.Instance.WorldItems.Add(item);

            item.xy = xy;

            RemoveItem(item);

            foreach (BodyPart bp in PaperDoll.Where(bp => bp.Item == item))
                bp.Item = null;

            if (Quiver == item)
                Quiver = null;
        }

        public int GetArmor()
        {
            return 8 +
                GetWornItems()
                .Select(item => item.GetComponent<WearableComponent>())
                .Select(c => c.ArmorClass).Sum() +
                Stats.Get(Stat.Dexterity);
        }

        public int GetCarriedWeight()
        {
            return Inventory.Sum(x => x.GetWeight());
        }
        public int GetCarryingCapacity()
        {
            return 1200 + Xplevel * 100 + Stats.Get(Stat.Strength) * 400;
        }

        public void SplatterBlood()
        {
            TileInfo tileInfo = World.Level.At(xy);

            tileInfo.Blood = true;
            tileInfo.Neighbours
                .Where(n => !n.Solid)
                .Where(n => Util.Random.Next(0, 4) >= 3)
                .ToList().ForEach(n =>
                    {
                        n.Blood = true;
                        if(Game.Player.Sees(n.Position))
                            Game.UI.UpdateAt(n.Position);
                    }
                );
        }

        public void Damage(DamageSource ds)
        {
            if(this == Game.Player && IO.IOState == InputType.Inventory)
                //make sure that the player doesn't killed while invmanaging
                IO.IOState = InputType.PlayerInput;

            //todo: maybe not return on 0 damage?
            if (ds.Damage <= 0) return;
            if (Stats.HpCurrent <= 0) return;

            if(Game.Player.Sees(xy))
                Game.UI.UpdateAt(xy);

            switch (ds.DamageType)
            {
                case DamageType.Physical:
                    SplatterBlood();
                    break;
                case DamageType.Ratking:
                    Debug.Assert(ds.Source != null, "ds.Source != null");
                    List<TileInfo> neighbours =
                        World.Level.At(ds.Source.xy).Neighbours
                        .Where(ti => ti.Actor == null)
                        .Where(ti => !ti.Solid)
                        .Where(ti => ti.Door != Door.Closed)
                        .ToList();

                    if (neighbours.Any(ti => ti.Actor == null))
                    {
                        Point p = neighbours
                            .SelectRandom()
                            .Position;
                        Actor rat = new Actor(
                            p,
                            Util.ADefByName("rat"),
                            ds.Source.Xplevel
                        );
                        World.Level.Spawn(rat);
                    }
                    break;
            }

            Stats.HpCurrent -= ds.Damage;

            if (HasEffect(StatusType.Sleep) && Stats.HpCurrent > 0)
            {
                RemoveEffect(StatusType.Sleep);

                Game.UI.Log(
                    "{1} {2} up!",
                    GetName("Name"),
                    Verb("wake")
                );

                //still sleepy
                Pass();
            }

            if (Stats.HpCurrent > 0) return;

            Game.UI.Log(GetName("Name") + " " + Verb("die") + "!");
            if (this == Game.Player)
                Game.UI.Log(ds.GenerateKillMessage());

            World.Level.Despawn(this);

            Item corpse = new Item(
                xy,
                ItemDefinition.DefDict[Definition.CorpseType],
                0, Intrinsics
            );
            //should always be ided
            //or maybe not..? could be a mechanic in and of itself
            corpse.Identify(true);
            World.Level.Spawn(corpse);
            Game.Brains.RemoveAll(b => b.MeatPuppet == this);

            if(ds.Source != null)
                ds.Source.GiveExperience(Definition.Experience * Xplevel);
        }

        public void GiveExperience(int amount)
        {
            int lPre = Xplevel;
            ExperiencePoints += amount;
            int levelNew = LevelFromExperience(ExperiencePoints);

            if (levelNew == lPre) return;

            Xplevel = levelNew;
            LevelUp();
        }
        private void LevelUp()
        {
            Game.UI.Log(
                "{1} stronger",
                this == Game.Player
                    ? "You feel" :
                    GetName("Name") + " looks"
            );
            Stats.HpMax +=
                Math.Max(
                    Util.Roll(Definition.HitDie),
                    Stats.Strength
                );
            Stats.MpMax +=
                Math.Max(
                    Util.Roll(Definition.ManaDie),
                    Stats.Intelligence
                );
        }
        public int RequiredExperienceForLevel(int target)
        {
            int level = 1;
            int xp = 0;
            while (level < target)
            {
                xp++;
                level = LevelFromExperience(xp);
            }
            return xp;
        }
        public int LevelFromExperience(int amount)
        {
            int xp = amount;
            int levelReq = 20; //for level 2

            int newLevel = 1;
            while (xp >= levelReq)
            {
                newLevel++;
                xp -= levelReq;
                levelReq += newLevel * 30;
            }

            return newLevel;
        }

        public bool HasEffect(
            StatusType type,
            TickingEffectDefinition ticker = null
        ) {
            if (ticker == null)
                return _lastingEffects.Any(
                    le => le.Type == type);
            return _lastingEffects.Any(
                le => le.Type == type &&
                le.Ticker == ticker
            );
        }
        public void AddEffect(LastingEffect le)
        {
            if(!HasEffect(le.Type))
                _lastingEffects.Add(le);
        }
        public void AddEffect(
            StatusType type,
            int duration,
            TickingEffectDefinition ticker = null
        ) {
            _lastingEffects.Add(new LastingEffect(ID, type, duration, ticker));
        }
        public void RemoveEffect(StatusType type)
        {
            _lastingEffects.Remove(
                _lastingEffects.Find(effect => effect.Type == type)
            );

            if (this != Game.Player) return;

            switch (type)
            {
                case StatusType.Bleed:
                    Game.UI.Log("You stop bleeding.");
                    break;
                case StatusType.Confusion:
                    Game.UI.Log("You feel clear of mind again.");
                    break;
                case StatusType.Poison:
                    Game.UI.Log("You feel better.");
                    break;
                case StatusType.Sleep:
                    Game.UI.Log("You wake up.");
                    break;
                case StatusType.Stun:
                    Game.UI.Log("You're able to move again.");
                    break;
            }
        }

        public void Eat(Item item)
        {
            if (item.Stacking) item.SpendCharge();
            else
            {
                RemoveItem(item);
                World.Level.Despawn(item);
            }

            Game.UI.Log(
                string.Format("{0} ate {1}.",
                GetName("Name"),
                item.GetName("a"))
            );

            EdibleComponent ec = item.GetComponent<EdibleComponent>();

            Food += ec.Nutrition;

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
            int r = Util.Roll("1d" + (Math.Pow(2, count)-1));
            //less than 2^n-1 = "loss", check later intrinsics

            //ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            //LH-011214: resharper stop being dumb pls
            while (r < Math.Pow(2, n-1))
                n--;
            Intrinsics.Add(item.Mods[count - n]);
        }

        public void Pass(bool movement = false)
        {
            //movement/standard action
            //standard is e.g. attacking, manipulating inventory, etc.

            //switch this to +=? could mean setting cd to -10 = free action
            int sneakMod = 0;
            if(HasEffect(StatusType.Sneak) && movement)
                sneakMod += 5;

            Cooldown = Game.StandardActionLength + sneakMod;
        }
        public void Pass(int length)
        {
            Cooldown = length;
        }

        public List<Point> GetPossibleMoves(bool disallowActorTiles = false)
        {
            return
                World.Level.At(xy)
                .Neighbours
                .Where(ti => !ti.Solid)
                .Where(ti => ti.Door != Door.Closed)
                .Where(ti => ti.Actor == null || !disallowActorTiles)
                .Select(ti => ti.Position - xy)
                .ToList()
            ;
        }
        private void TryMove(Point offset)
        {
            if(Game.Player.Sees(xy))
                Game.UI.UpdateAt(xy);

            List<Point> possiblesMoves = GetPossibleMoves();

            if(HasEffect(StatusType.Confusion))
            {
                if (Util.Roll("1d3") > 1)
                {
                    if (this == Game.Player) Game.UI.Log("You stumble...");
                    offset = possiblesMoves
                        [Util.Random.Next(0, possiblesMoves.Count)];
                }
            }

            if (!GetPossibleMoves().Contains(offset))
            {
                if(this == Game.Player) Game.UI.Log("Bump!");
                //else "... bumps into a wall..?"
                return;
            }

            bool moved = false;

            if (World.Level.At(xy + offset).Actor == null)
            {
                xy.Nudge(offset.x, offset.y);
                moved = true;
                Pass(true);

                //walking noise
                World.Level.MakeNoise(
                    xy,
                    NoiseType.FootSteps,
                    HasEffect(StatusType.Sneak)
                        ? -Util.XperY(1, 2, Stats.Get(Stat.Dexterity))
                        : -1
                );
            }
            else
            {
                Do(
                    new Command("Bump")
                    .Add("Direction", Point.ToCardinal(offset))
                );
            }

            if(Game.Player.Sees(xy))
                Game.UI.UpdateAt(xy);

            HasMoved = moved;
        }
        public bool HasMoved;

        public void UpdateVision()
        {
            if (_vision == null)
                _vision = new bool[World.Level.Size.x, World.Level.Size.y];

            if (this == Game.Player)
            {
                for (int x = 0; x < World.Level.Size.x; x++)
                    for (int y = 0; y < World.Level.Size.y; y++)
                        //make sure to update all we SAW as well,
                        //so that's drawn as not visible
                        if (_vision[x, y]) Game.UI.UpdateAt(x, y);
            }

            //nil all
            _vision.Paint(
                new Rect(new Point(0, 0), World.Level.Size),
                false
            );

            //shadowcast
            ShadowCaster.ShadowCast(
                Game.Player.xy,
                5,
                (p) =>
                    World.Level.At(p) == null ||
                    World.Level.At(p).Solid ||
                    World.Level.At(p).Door == Door.Closed,
                (p, d) =>
                {
                    if (this == Game.Player)
                    {
                        //for now, only player has the "seen"
                        //and only the player updates the drawn map
                        World.Level.See(p);
                        Game.UI.UpdateAt(p);
                    }
                    if (World.Level.At(p) != null)
                        _vision[p.x, p.y] = true;
                }
            );
        }

        public enum Tempus
        {
            Present,
            Passive
        }
        public string Verb(string verb, Tempus tempus = Tempus.Present)
        {
            switch (tempus)
            {
                case Tempus.Present:
                    switch (verb)
                    {
                        case "#feel":
                            if (this == Game.Player) verb = "feel";
                            else verb = "looks";
                            break;
                        case "be":
                            if (this == Game.Player) verb = "are";
                            else verb = "is";
                            break;
                        default:
                            if (this != Game.Player)
                            {
                                //bashES, slashES, etc.
                                //missES
                                if (verb[verb.Length - 1] == 'h' ||
                                    verb[verb.Length - 1] == 's')
                                    verb += "e";
                                verb += "s";
                            }
                            break;
                    }
                    break;
                case Tempus.Passive:
                    verb += "ed";
                    break;
            }

            return verb;
        }
        public string Genitive(string format = "")
        {
            string result;
            switch (format.ToLower())
            {
                case "name":
                    result = this == Game.Player
                        ? "your"
                        : Definition.Name + "'s";
                    break;
                default:
                    result = this == Game.Player
                        ? "your"
                        : "their";
                    break;
            }
            if (format.Length == 0) return result;
            if (format[0] <= 'Z') return Util.Capitalize(result);
            return result;
        }

        public void LearnSpell(Spell spell)
        {
            Definition.Spellbook.Add(spell.SpellID);
        }

        public bool Sees(Point other)
        {
            if (_vision == null) return false;
            return _vision[other.x, other.y];
        }

        public void Heal(int amount)
        {
            Stats.HpCurrent = Math.Min(Stats.HpCurrent + amount, Stats.HpMax);
        }

        public enum FoodStatus
        {
            Starving,
            Hungry,
            Satisfied,
            Full,
            Stuffed
        }
        public static string FoodStatusString(FoodStatus fs)
        {
            switch (fs)
            {
                case FoodStatus.Starving: return "Starving";
                case FoodStatus.Hungry: return "Hungry";
                case FoodStatus.Satisfied: return "Satisfied";
                case FoodStatus.Full: return "Full";
                case FoodStatus.Stuffed: return "Stuffed";
            }
            throw new ArgumentException();
        }
        public FoodStatus GetFoodStatus()
        {
            if (_food <= 0)
            {
                Damage(new DamageSource(
                    "R.I.P. {0}, starved to death.")
                    { Damage = Stats.HpCurrent, Target = this }
                );
                return FoodStatus.Starving;
            }

            if (_food <= 500) return FoodStatus.Starving;
            if (_food <= 1500) return FoodStatus.Hungry;
            if (_food <= 9000) return FoodStatus.Satisfied;
            if (_food <= 15000) return FoodStatus.Full;
            if (_food <= 20000) return FoodStatus.Stuffed;

            Damage(
                new DamageSource(
                    "R.I.P. {0}, choked to death on their food."
                ) {
                    Damage = Stats.HpCurrent, Target = this
                }
            );
            return FoodStatus.Stuffed;
        }

        public void UpdateFoodStatus(bool increased = false)
        {
            string message = "";

            if (!increased && GetFoodStatus() < FoodStatus.Satisfied)
            {
                if (HasEffect(StatusType.Sleep))
                {
                    RemoveEffect(StatusType.Sleep);
                    message = "You wake up from hunger. ";
                }
            }

            switch (GetFoodStatus())
            {
                case FoodStatus.Starving:
                    message += string.Format(
                        "#ff0000{0} needs food, badly!",
                        Util.Capitalize(Definition.Name)
                    );
                    break;
                case FoodStatus.Hungry:
                    message +=
                        increased
                        ? "You still feel hungry."
                        : "You are starting to feel peckish."
                    ;
                    break;
                case FoodStatus.Satisfied:
                    if(increased)
                        message += "Man, that hit the spot.";
                    break;
                case FoodStatus.Full:
                    if (increased)
                        message += "You feel full.";
                    else
                    {
                        message += "You burp loudly.";
                        World.Level.MakeNoise(xy, NoiseType.Burp, 1);
                    }
                    break;
                case FoodStatus.Stuffed:
                    message += "Eugh, no thanks, no mint.";
                    break;
                default:
                    throw new Exception();
            }

            if (this == Game.Player && message != "")
                Game.UI.Log(message);
        }

        public enum BurdenStatus
        {
            Unburdened,
            Burdened,
            Stressed
        }
        public static string BurdenStatusString(BurdenStatus bs)
        {
            switch (bs)
            {
                case BurdenStatus.Unburdened: return "Unburdened";
                case BurdenStatus.Burdened: return "Burdened";
                case BurdenStatus.Stressed: return "Stressed";
            }
            throw new ArgumentException();
        }
        public BurdenStatus GetBurdenStatus()
        {
            int carried = GetCarriedWeight();
            int capacity = GetCarryingCapacity();

            BurdenStatus bs;
            if (carried > 1.5f * capacity) bs = BurdenStatus.Stressed;
            else if (carried > capacity) bs = BurdenStatus.Burdened;
            else bs = BurdenStatus.Unburdened;

            if (bs != _carried && this == Game.Player && IsAlive)
            {
                if (bs == BurdenStatus.Unburdened)
                    Game.UI.Log("You're no longer burdened.");
                else if (bs == BurdenStatus.Burdened)
                    Game.UI.Log("You're burdened by your load.");
                else if (bs == BurdenStatus.Stressed)
                    Game.UI.Log("You feel stressed by your load.");
            }

            _carried = bs;
            return bs;
        }

        public void Do()
        {
            Do(IO.CurrentCommand);
        }

        //mig to Combat.cs
        private void SwingSpear(Direction direction, Item weapon)
        {
            Actor target =
                World.LevelByID(LevelID)
                .At(xy + Point.FromCardinal(direction))
                .Actor;

            if (target == null) return;

            bool hit = Combat.Attack(
                new MeleeAttack(this, target, weapon),
                s => Game.UI.Log(s)
            );

            Actor secondary =
                World.LevelByID(LevelID)
                .At(xy + Point.FromCardinal(direction) * 2)
                .Actor;

            if (hit && secondary != null)
            {
                Combat.Attack(
                    new MeleeAttack(
                        this,
                        secondary,
                        weapon
                    ),
                    s => Game.UI.Log(s)
                );
            }
        }
        private void SwingTwoHander(Direction direction, Item weapon)
        {
            List<TileInfo> sweep =
                World.Level.At(xy)
                .Neighbours
                .Intersect(
                    World.Level
                    .At(xy + Point.FromCardinal(direction))
                    .Neighbours
                )
                .Where(ti => !ti.Solid)
                .ToList();

            List<Actor> targets =
                sweep
                .Where(ti => ti.Actor != null)
                .Select(ti => ti.Actor)
                .Where(a => a != this)
                .ToList();

            //clumpsy/lack of space
            //2 spots are us and the target
            //2 other are the neighbours
            if (sweep.Count < 4)
            {
                //todo: penalized swing
                targets.ForEach(
                    a => Combat.Attack(
                        new MeleeAttack(this, a, weapon),
                        s => Game.UI.Log(s)
                    )
                );
            }
            else
            {
                targets.ForEach(
                    a => Combat.Attack(
                        new MeleeAttack(this, a, weapon),
                        s => Game.UI.Log(s)
                    )
                );
            }
        }
        private void SwingLongsword(Direction direction, Item weapon)
        {
            Actor target =
                World.LevelByID(LevelID)
                .At(xy + Point.FromCardinal(direction))
                .Actor;

            if (target == null) return;

            Combat.Attack(
                new MeleeAttack(this, target, weapon),
                s => Game.UI.Log(s)
            );

            List<Actor> sweep =
                World.Level.At(xy)
                .Neighbours
                .Where(ti => ti.Actor != null)
                .Where(ti => ti.Actor != Game.Player) //no self-harm
                .Where(ti => ti.Actor != target) //no double-swing
                .Select(ti => ti.Actor)
                .Intersect(
                    World.Level
                    .At(xy + Point.FromCardinal(direction))
                    .Neighbours
                    .Select(ti2 => ti2.Actor)
                )
                .ToList();

            if (sweep.Count > 0)
            {
                Actor secondary = sweep.SelectRandom();
                Combat.Attack(
                    new MeleeAttack(this, secondary, weapon),
                    s => Game.UI.Log(s)
                );
            }
        }

        private void Swing(Direction direction, Item weapon)
        {
            Actor target =
                World.LevelByID(LevelID)
                .At(xy + Point.FromCardinal(direction))
                .Actor;

            switch (weapon.ItemType)
            {
                case ItemID.Item_Spear:
                    SwingSpear(direction, weapon);
                    break;

                case ItemID.Item_Zweihander:
                    SwingTwoHander(direction, weapon);
                    break;

                case ItemID.Item_Longsword:
                    SwingLongsword(direction, weapon);
                    break;

                default:
                    Combat.Attack(
                        new MeleeAttack(this, target, weapon),
                        s => Game.UI.Log(s)
                    );
                    break;
            }
        }

        private void HandleBump(Command cmd)
        {
            Direction direction = (Direction)cmd.Get("Direction");
            Point targetPoint = xy + Point.FromCardinal(direction);
            Actor target =
                World.LevelByID(LevelID)
                .At(targetPoint)
                .Actor;

            Debug.Assert(target != null, "target != null");

            if (GetWieldedItems().Count == 0)
                Combat.Attack(
                    new MeleeAttack(this, target, null),
                    s => Game.UI.Log(s)
                );

            foreach (Item weapon in GetWieldedItems())
            {
                Swing(direction, weapon);
            }

            Pass();
        }
        private void HandleCast(Command cmd)
        {
            //we can trust the "spell" key to always be a spell,
            //because if it isn't, the blame isn't here, it's somewhere
            //earlier in the chain
            Spell spell = (Spell)cmd.Get("spell");

            //always spend energy, no matter if we succeed or not
            Stats.MpCurrent -= spell.Cost;

            if (Util.Roll("1d20") + Stats.Get(Stat.Intelligence) >=
                spell.CastDifficulty)
            {
                if(spell.CastType == InputType.Targeting)
                    spell.Cast(this, cmd.Get("target"));
                else
                    spell.Cast(this, cmd.Get("answer"));
            }
            else
            {
                Game.UI.Log(
                    "{1} {2} and {3}, but nothing happens!",
                    GetName("Name"),
                    Verb("mumble"),
                    Verb("wave")
                );
            }

            Pass();
        }
        private void HandleChant(Command cmd)
        {
            string chant = (string)cmd.Get("chant");
            Game.UI.Log(
                "{1} {2}...",
                GetName("Name"),
                Verb("chant")
            );
            Game.UI.Log("\"{1}...\"", Util.Capitalize(chant));
            BlackMagic.CheckCircle(this, chant);
        }
        private void HandleClose(Command cmd)
        {
            TileInfo targetTile = (TileInfo)cmd.Get("door");
            if(Game.Player.Sees(xy))
                Game.UI.Log(
                    "{1} {2} {3} door.",
                    GetName("Name"),
                    Verb("close"),
                    this == Game.Player ? "the" : "a"
                );
            targetTile.Door = Door.Closed;

            if(Game.Player.Sees(targetTile.Position))
                Game.UI.UpdateAt(targetTile.Position);

            int squeakChance = 6;
            if (HasEffect(StatusType.Sneak)) squeakChance = 8;

            if (Util.Random.Next(1, squeakChance) == 1)
            {
                World.Level.MakeNoise(targetTile.Position, NoiseType.Door);
                if (this == Game.Player)
                    //player would normally not see anything, since they see
                    //the door.
                    Game.UI.Log("The door squeaks.");
            }

            Pass(true);
        }
        private void HandleDrop(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            int count = (int)cmd.Get("count");

            if (count != item.Count)
            {
                Item clone = item.Clone();
                clone.Count = count;
                item.Count -= count;

                item = clone;
            }

            Item stack = World.Level.At(xy).Items
                .FirstOrDefault(it => it.CanStack(item));

            if (stack != null)
                stack.Stack(item);
            else DropItem(item);

            Game.UI.Log(
                "{1} {2} {3}.",
                GetName("Name"),
                Verb("drop"),
                item.GetName("count")
            );

            Pass();
        }
        private void HandleEat(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            Eat(item);
            Pass();
        }
        private void HandleEngrave(Command cmd)
        {
            string answer = (string)cmd.Get("text");

            World.Level.At(Game.Player.xy).Tile.Engraving = answer;

            if(this == Game.Player)
                Game.UI.Log(
                    "You wrote \"{1}\" on the dungeon floor.",
                    answer
                );
        }
        private void HandleGet(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            World.Instance.WorldItems.Remove(item);

            Item stack = Inventory.FirstOrDefault(it => it.CanStack(item));

            if (stack != null)
            {
                Game.UI.Log("Picked up " + item.GetName("count") + ".");
                stack.Stack(item);
                //so we can get the right char below
                item = stack;
            }
            else Game.Player.GiveItem(item);

            char index = IO.Indexes
                [Inventory.IndexOf(Inventory.First(it => it.ID == item.ID))];

            if(this == Game.Player)
                Game.UI.Log(index + " - "  + item.GetName("count") + ".");

            Pass();
        }
        private void HandleLearn(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log("You read {1}...",item.GetName("the"));

            item.Identify();

            Spell spell = item.GetComponent<LearnableComponent>().TaughtSpell;

            LearnSpell(spell);

            if (this == Game.Player)
                Game.UI.Log("You feel knowledgable about {1}!", spell.Name);

            Pass();
        }
        private void HandleMove(Command cmd)
        {
            Direction direction = (Direction)cmd.Get("Direction");
            if (direction != Direction.Up && direction != Direction.Down)
            {
                Point offset = Point.FromCardinal(direction);
                //trymove passes for us
                TryMove(offset);

                if (this == Game.Player)
                {
                    IO.Target = Game.Player.xy;
                    PlayerResponses.Examine();
                }
            }
            else
            {
                LevelConnector connector = World.Level.Connectors
                    .FirstOrDefault(lc => lc.Position == xy);

                bool descending = direction == Direction.Down;

                if (connector == null)
                {
                    if (this == Game.Player)
                        Game.UI.Log(
                            "You can't go {1} here.",
                            descending ? "down" : "up"
                        );
                    return;
                }

                //this should only ever happen when going downwards, at least at
                //the moment. If this happens when we're going upwards,
                //something is weird.
                if (connector.Target == null)
                {
                    Generator g = new Generator();
                    Level l = g.Generate(
                        World.Level,
                        World.Level.Depth + 1
                    );
                    connector.Target = l.ID;
                }

                if (World.Level.At(xy).Stairs ==
                    (descending ? Stairs.Down : Stairs.Up)
                ) {
                    Game.SwitchLevel(
                        World.LevelByID(connector.Target.Value), true
                    );

                    if(this == Game.Player)
                        Game.UI.Log(
                            "You {1} the stairs...",
                            descending
                                ? "descend"
                                : "ascend"
                        );
                }
                else
                {
                    if (this == Game.Player)
                        Game.UI.Log(
                            "You can't go {1} here.",
                            descending ? "down" : "up"
                        );
                }
            }

            UpdateVision();
        }
        private void HandleOpen(Command cmd)
        {
            TileInfo targetTile = (TileInfo)cmd.Get("door");
            targetTile.Door = Door.Open;
            foreach (Actor a in World.LevelByID(LevelID).Actors)
                a.UpdateVision();

            if(Game.Player.Sees(targetTile.Position))
                Game.UI.Log(
                    "{1} {2} {3} door.",
                    Game.Player.Sees(xy) ? GetName("Name") : "Something",
                    Verb("open"),
                    this == Game.Player ? "the" : "a"
                );

            if(Game.Player.Sees(targetTile.Position))
                Game.UI.UpdateAt(targetTile.Position);

            int squeakChance = 6;
            if (HasEffect(StatusType.Sneak)) squeakChance = 8;

            if (Util.Random.Next(1, squeakChance) == 1)
            {
                World.Level.MakeNoise(targetTile.Position, NoiseType.Door);
                if (this == Game.Player)
                    //player would normally not see anything, since they see
                    //the door.
                    Game.UI.Log("The door squeaks.");
            }

            Pass(true);
        }
        private void HandleQuaff(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log("Drank {1}.", item.GetName("a"));

            DrinkableComponent dc = item.GetComponent<DrinkableComponent>();
            dc.Effect.Cast(this, null);

            item.SpendCharge();
            if (this == Game.Player)
                item.Identify();

            Pass();
        }
        private void HandleQuiver(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            Quiver = item;

            if(this == Game.Player)
                Game.UI.Log("Quivered {1}.", item.GetName("count"));

            Pass();
        }
        private void HandleRead(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log(
                    "You read {1}...",
                    item.GetName("the")
                );

            Spell spell = item.GetComponent<ReadableComponent>().Effect;

            if(spell.CastType == InputType.Targeting)
                spell.Cast(this, cmd.Get("target"));
            else
                spell.Cast(this, cmd.Get("answer"));

            Pass();
        }
        private void HandleRemove(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            foreach (BodyPart bp in PaperDoll.Where(bp => bp.Item == item))
                bp.Item = null;

            if (this == Game.Player)
                Game.UI.Log(
                    "You remove your {1}.",
                    item.GetName("name")
                );

            Pass();
        }
        private void HandleSheathe(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (IsWielded(item))
            {
                foreach (BodyPart bp in PaperDoll.Where(bp => bp.Item == item))
                {
                    if (bp.Type == DollSlot.Hand)
                        ((Hand)bp).Wielding = false;
                    bp.Item = null;
                }

                if (this == Game.Player)
                    Game.UI.Log(
                        "You sheathe your {1}.",
                        item.GetName("name")
                    );
            }
            else
            {
                Quiver = null;

                if (this == Game.Player)
                    Game.UI.Log(
                        "You unquiver your {1}.",
                        item.GetName("count")
                    );
            }

            Pass();
        }
        private void HandleShoot(Command cmd)
        {
            Actor target = (Actor)cmd.Get("actor");
            Item ammo = (Item)cmd.Get("ammo");

            Combat.Throw(
                new RangedAttack(
                    this,
                    target,
                    ammo,
                    Inventory.FirstOrDefault(it => it.CanFire(ammo))
                ),
                s => Game.UI.Log(s)
            );

            Pass();
        }
        private void HandleSleep(Command cmd)
        {
            if (this == Game.Player)
                Game.UI.Log("You lie down on the dungeon floor.");

            AddEffect(
                new LastingEffect(
                    ID,
                    StatusType.Sleep,
                    ((int)cmd.Get("length")) * 10 - 1
                )
            );
        }
        private void HandleUse(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            Spell spell = item.GetComponent<UsableComponent>().Effect;

            //same thing that goes for readables, where to put this and id?
            item.SpendCharge();

            if(spell.CastType == InputType.Targeting)
                spell.Cast(this, cmd.Get("target"));
            else
                spell.Cast(this, cmd.Get("answer"));

            Pass();
        }
        private void HandleWear(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            Wear(item);

            if (this == Game.Player)
                Game.UI.Log(
                    "Wore {1}.",
                    item.GetName("a")
                );

            Pass();
        }
        private void HandleWield(Command cmd)
        {
            Item item = (Item)cmd.Get("item");
            Wield(item);

            if (this == Game.Player)
                Game.UI.Log(
                    "You wield {1}.",
                    item.GetName("a")
                );

            Pass();
        }

        public void Do(Command cmd)
        {
            switch(cmd.Type)
            {
                case "bump": HandleBump(cmd); break;
                case "cast": HandleCast(cmd); break; //zap
                case "chant": HandleChant(cmd); break;
                case "close": HandleClose(cmd); break;
                case "drop": HandleDrop(cmd); break;
                case "eat": HandleEat(cmd); break;
                case "engrave": HandleEngrave(cmd); break;
                case "get": HandleGet(cmd); break;
                case "learn": HandleLearn(cmd); break;
                case "move": HandleMove(cmd); break;
                case "open": HandleOpen(cmd); break;
                case "quaff": HandleQuaff(cmd); break;
                case "quiver": HandleQuiver(cmd); break;
                case "read": HandleRead(cmd); break;
                case "remove": HandleRemove(cmd); break;
                case "sheathe": HandleSheathe(cmd); break;
                case "shoot": HandleShoot(cmd); break;
                case "sleep": HandleSleep(cmd); break;
                case "use": HandleUse(cmd); break;
                case "wear": HandleWear(cmd); break;
                case "wield": HandleWield(cmd); break;
                default: throw new ArgumentException();
            }
        }

        public bool CanMove(bool disregardStatus = false)
        {
            if (!IsAlive) return false;
            if (Cooldown > 0) return false;
            if (disregardStatus) return true;
            if (HasEffect(StatusType.Sleep)) return false;
            if (HasEffect(StatusType.Stun)) return false;
            return true;
        }

        public void Hear(NoiseType noise, Point p)
        {
            if (HasEffect(StatusType.Sleep))
            {
                RemoveEffect(StatusType.Sleep);

                if (this != Game.Player && Game.Player.Sees(xy))
                    Game.UI.Log(
                        "{1} {2} up.",
                        GetName("Name"),
                        Verb("wake")
                    );

                //don't act immediately on wakeup.
                Pass();
            }

            //only log hearing for the player, and only for things he/she
            //doesn't already see.
            if (this != Game.Player || Game.Player.Sees(p)) return;

            switch (noise)
            {
                case NoiseType.FootSteps:
                    Game.UI.Log("You hear footsteps.");
                    break;
                case NoiseType.Combat:
                    Game.UI.Log("You hear sounds of combat.");
                    break;
                case NoiseType.Door:
                    Game.UI.Log("You hear a door squeaking.");
                    break;
                default: throw new ArgumentException();
            }
        }

        //should probably be moved to a container class or something
        public void GiveItem(Item item)
        {
            _inventory.Add(item.ID);
        }
        public void RemoveItem(Item item)
        {
            _inventory.Remove(item.ID);
            foreach (BodyPart bp in PaperDoll)
                if (bp.Item == item) bp.Item = null;
            if (_quiver == item.ID) _quiver = null;
        }

        public void TickEffects()
        {
            foreach (LastingEffect effect in _lastingEffects)
                effect.Tick();

            List<LastingEffect> deadEffects =
                _lastingEffects
                .Where(x => x.Life > x.LifeLength && x.LifeLength != -1)
                .ToList();

            foreach (LastingEffect effect in deadEffects)
                RemoveEffect(effect.Type);
        }

        public bool FreeCrit()
        {
            return
                HasEffect(StatusType.Sleep) ||
                HasEffect(StatusType.Stun);
        }
    }

    [DataContract]
    public class Doll
    {
        [DataMember] public List<BodyPart> BodyParts;
        [DataMember] public List<Hand> Hands;

        public Doll()
        {
            BodyParts = new List<BodyPart>();
            Hands = new List<Hand>();
        }

        public List<BodyPart> Get()
        {
            List<BodyPart> bodyParts = new List<BodyPart>();
            bodyParts.AddRange(BodyParts);
            bodyParts.AddRange(Hands);
            return bodyParts;
        }

        public void Add(BodyPart bp)
        {
            if (bp.Type == DollSlot.Hand) Hands.Add((Hand)bp);
            else BodyParts.Add(bp);
        }
    }
}