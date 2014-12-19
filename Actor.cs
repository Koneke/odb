using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

    [DataContract]
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
                //Awake.Equals(other.Awake) &&
                Equals(Quiver, other.Quiver)
            ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ ID;
                hashCode = (hashCode*397) ^
                           (Definition != null ? Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ HpCurrent;
                hashCode = (hashCode*397) ^ MpCurrent;
                hashCode = (hashCode*397) ^ Cooldown;
                //hashCode = (hashCode*397) ^ Awake.GetHashCode();
                hashCode = (hashCode*397) ^
                           (Quiver != null ? Quiver.GetHashCode() : 0);
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

        public new ActorDefinition Definition
        {
            get { return ActorDefinition.ActorDefinitions[_type]; }
        }

        [DataMember] private int _type;
        [DataMember] public int ID;

        [DataMember] private int _strength, _dexterity, _intelligence;

        [DataMember] public int HpCurrent;
        [DataMember] public int MpCurrent;
        [DataMember] public int HpRegCooldown;

        [DataMember] public int HpMax;
        [DataMember] public int MpMax;
        [DataMember] public int MpRegCooldown;

        [DataMember] public int Level;
        [DataMember] public int ExperiencePoints;
        [DataMember] public int Cooldown;
        [DataMember] private int _food;

        [DataMember] public List<BodyPart> PaperDoll;
        [DataMember] private List<int> _inventory;

        public List<Item> Inventory
        {
            get { return _inventory.Select(Util.GetItemByID).ToList(); }
        }

        [DataMember] public List<LastingEffect> LastingEffects;
        [DataMember] public List<Mod> Intrinsics;

        //public bool Awake;
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
        public bool IsAlive { get { return HpCurrent > 0; } }
        #endregion

        public Actor() { }

        public Actor(
            Point xy,
            ActorDefinition definition,
            int level
        ) : base(xy, definition) {
            ID = IDCounter++;
            _type = definition.Type;

            _strength = Util.Roll(definition.Strength);
            _dexterity = Util.Roll(definition.Dexterity);
            _intelligence = Util.Roll(definition.Intelligence);

            HpMax = Util.Roll(definition.HitDie, true);
            for (int i = 0; i < level - 1; i++)
                HpMax += Util.Roll(definition.HitDie);

            MpMax = Util.Roll(definition.ManaDie, true);
            for (int i = 0; i < level - 1; i++)
                MpMax += Util.Roll(definition.ManaDie);

            HpCurrent = HpMax;
            MpCurrent = MpMax;

            Level = level;
            ExperiencePoints = RequiredExperienceForLevel(level);

            Cooldown = 0;

            _food = 9000;

            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in definition.BodyParts)
                PaperDoll.Add(
                    ds == DollSlot.Hand
                    ? new Hand(ds)
                    : new BodyPart(ds)
                );
            _inventory = new List<int>();
            Intrinsics = new List<Mod>(Definition.SpawnIntrinsics);
            //Awake = false;
            LastingEffects = new List<LastingEffect>();

            HpRegCooldown = 10;
            MpRegCooldown = 30 - _intelligence;
        }

        public Actor(string s) : base(s)
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
                //.Where(bp => !bp.Item.HasTag(ItemTag.NonWeapon))
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

        public int Get(Stat stat, bool modded = true)
        {
            switch (stat)
            {
                case Stat.Strength:
                    return _strength +
                        (modded ? GetMod(stat) : 0);
                case Stat.Dexterity:
                    return _dexterity +
                        (modded ? GetMod(stat) : 0);
                case Stat.Intelligence:
                    return _intelligence +
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

            switch (stat)
            {
                case Stat.Dexterity:
                    if (GetFoodStatus() == FoodStatus.Stuffed) modifier--;
                    break;
                case Stat.Strength:
                    if (GetFoodStatus() == FoodStatus.Starving) modifier--;
                    break;
                case Stat.Speed:
                    if (GetFoodStatus() == FoodStatus.Stuffed) modifier--;
                    break;
            }

            return modifier;
        }
        public int GetArmor()
        {
            return 8 +
                GetWornItems()
                .Select(item => item.GetComponent<WearableComponent>())
                .Select(c => c.ArmorClass).Sum() +
                Get(Stat.Dexterity);
        }

        public int GetCarriedWeight()
        {
            return Inventory.Sum(x => x.GetWeight());
        }
        public int GetCarryingCapacity()
        {
            return 1200 + Level * 100 + Get(Stat.Strength) * 400;
        }

        public void Attack(Actor target)
        {
            int dexBonus = Util.XperY(1, 3, Get(Stat.Dexterity));
            int strBonus = Util.XperY(1, 2, Get(Stat.Strength));

            int multiWeaponPenalty = 3 * GetWieldedItems().Count - 1;
            multiWeaponPenalty = Math.Max(0, multiWeaponPenalty - dexBonus);

            int targetDefense = target.GetArmor();

            AttackComponent bash = new AttackComponent
            {
                Damage = "1d4",
                Modifier =  -2,
                AttackType = AttackType.Bash,
                DamageType = DamageType.Physical
            };

            List<Tuple<Item, AttackComponent>> attacks =
                new List<Tuple<Item, AttackComponent>>();

            foreach (Item item in GetWieldedItems())
                attacks.Add(new Tuple<Item, AttackComponent>
                    (item, item.GetComponent<AttackComponent>() ??  bash));

            if (attacks.Count == 0)
                attacks.Add(new Tuple<Item, AttackComponent>(
                    null, Definition.NaturalAttack));

            string message = "";
            int totalDamage = 0;

            List<DamageSource> damageSources = new List<DamageSource>();

            //foreach (Item weapon in GetWieldedItems())
            foreach(Tuple<Item, AttackComponent> attack in attacks)
            {
                if (totalDamage > target.HpCurrent) continue;

                Item weapon = attack.Item1;

                int roll = Util.Roll("1d20");
                bool crit =
                    roll >= 20 ||
                    target.HasEffect(StatusType.Sleep)
                ;
                int mod = weapon == null ? 0 : weapon.Mod;

                int totalModifier =
                    strBonus + dexBonus + Level +
                    mod - multiWeaponPenalty;
                int hitRoll = roll + totalModifier;

                if (hitRoll < targetDefense)
                {
                    message += String.Format(
                        "{0} {1} {2}in the air. ",
                        GetName("Name"),
                        Verb("swing"),
                        weapon == null
                            ?  ""
                            : (Genitive() + " "+ weapon.GetName("name") + " ")
                    );

                    if (Game.OpenRolls)
                        message += String.Format(
                            "d20+{0} ({1}+{2}+{3}+{4}-{5}{9:+#;-#;+0}), " +
                                "{6}+{0}, {7} vs. {8}. ",
                            totalModifier,
                            strBonus, dexBonus, Level, mod, multiWeaponPenalty,
                            roll,
                            hitRoll,
                            targetDefense,
                            attack.Item2.Modifier
                        );

                    continue;
                }

                AttackComponent ac =
                    weapon == null
                    ? attack.Item2
                    : weapon.GetComponent<AttackComponent>();

                ac = ac ?? bash;

                message +=
                    AttackMessage.AttackMessages[ac.AttackType]
                    .SelectRandom().Instantiate(this, target, weapon) +
                    (crit ? "!" : ".") + " ";

                foreach (EffectComponent ec in ac.Effects)
                    //also rolls to check for success
                    ec.Apply(target);

                int damageRoll = Util.Roll(ac.Damage, crit);
                int damage = damageRoll + strBonus;

                if (Game.OpenRolls)
                {
                    message += String.Format(
                        "d20+{0} ({1}+{2}+{3}+{4}-{5}), " +
                            "{6}+{0}, {7} vs. {8}. ",
                        totalModifier,
                        strBonus, dexBonus, Level, mod, multiWeaponPenalty,
                        roll,
                        hitRoll,
                        targetDefense
                    );

                    message += String.Format(
                        "{0}+{2}, {1}+{2}, {3} hit points damage. ",
                        ac.Damage, damageRoll, strBonus, damage);
                }

                totalDamage += damage;
                Point position = xy;
                position.z = World.Level.Depth;
                damageSources.Add(new DamageSource
                {
                    Position =  position,
                    Damage = damage,
                    AttackType = ac.AttackType,
                    DamageType = ac.DamageType,
                    Source = this,
                    Target = target
                });

                if (weapon == null) continue;
                //does not -guarantee- damage, rolls the chance as well
                weapon.Damage(0, s => message += s);
            }

            Game.UI.Log(message);
            foreach (DamageSource ds in damageSources)
                target.Damage(ds);
        }
        public void Shoot(Actor target)
        {
            LauncherComponent lc = null;

            Item weapon = Game.Player.GetWieldedItems()
                .FirstOrDefault(it => it.HasComponent<LauncherComponent>());
            if (weapon != null)
                lc = weapon.GetComponent<LauncherComponent>();

            Item ammo = Quiver;
            Debug.Assert(ammo != null, "ammo != null");

            ProjectileComponent pc =
                ammo.GetComponent<ProjectileComponent>();

            if(lc != null) if(!lc.AmmoTypes.Contains(ammo.Type)) lc = null;

            int roll = Util.Roll("1d20");
            bool crit = roll == 20 || target.HasEffect(StatusType.Sleep);

            int dexBonus = Get(Stat.Dexterity);

            //to method?
            int distanceModifier = 1;
            if (pc == null) distanceModifier++;
            if (lc == null) distanceModifier++;
            int distancePenalty =
                Util.XperY(distanceModifier, 1, Util.Distance(xy, target.xy));

            int mod = ammo.Mod;
            if (weapon != null) mod += weapon.Mod;
            int totalModifier = dexBonus + mod - distancePenalty;

            int targetArmor = target.GetArmor();
            int hitRoll = roll + totalModifier;

            string message = "";

            //could probably be made into a generic "split" function
            Item projectile = new Item(ammo.WriteItem().ToString())
            {
                ID = IDCounter++,
                Count = ammo.Stacking ? 1 : 0,
                xy = target.xy,
                LevelID = World.Level.ID
            };
            ammo.Count--;
            if(ammo.Count <= 0) World.Level.Despawn(ammo);

            DamageSource ds = null;

            if(hitRoll >= targetArmor) {
                int ammoDamage = pc == null
                    ? Util.Roll("1d4", crit)
                    : Util.Roll(pc.Damage);

                int launcherDamage = lc == null
                    ? 0
                    : Util.Roll(lc.Damage, crit);

                int damageRoll = ammoDamage + launcherDamage;

                Point position = xy;
                position.z = World.Level.Depth;
                ds = new DamageSource
                {
                    Position = position,
                    Damage = damageRoll,
                    AttackType = AttackType.Pierce,
                    DamageType = DamageType.Physical,
                    Source = this,
                    Target = target
                };

                message += string.Format(
                    "{0} is hit{1} ",
                    target.GetName("Name"),
                    crit ? "!" : "."
                );

                #region rolls to log
                if (Game.OpenRolls)
                {
                    message +=
                        String.Format
                        (
                            "d20+{0} ({1}+{2}-{3}), " +
                                "{4}+{0}, {5} vs. {6}. ",
                            totalModifier,
                            dexBonus, mod, distancePenalty,
                            roll,
                            hitRoll,
                            targetArmor
                        );
                    message +=
                        String.Format
                        (
                            "{0}{1}, {2}{3}, {4} hit points damage.",
                            pc == null ? "1d4" : pc.Damage,
                            lc == null ? "" : ("+" + lc.Damage),
                            ammoDamage,
                            lc == null ? "" : ("+" + launcherDamage),
                            damageRoll
                        );
                }
                #endregion
            }
            else
            {
                message += GetName("Name") + " " + Verb("miss") + ". ";

                #region rolls to log
                if (Game.OpenRolls)
                    message +=
                        String.Format
                        (
                            "d20+{0} ({1}+{2}-{3}), " +
                                "{4}+{0}, {5} vs. {6}. ",
                            totalModifier,
                            dexBonus, mod, distancePenalty,
                            roll,
                            hitRoll,
                            targetArmor
                        );
                #endregion
            }

            projectile.Damage(4, s => message += s);
            if (projectile.Health > 0)
                World.Level.Spawn(projectile);

            Game.UI.Log(message);
            if(ds != null) target.Damage(ds);

            //ranged is fairly quiet.
            World.Level.MakeNoise(target.xy, NoiseType.Combat, -2);
            Pass();
        }
        public void Damage(DamageSource ds)
        {
            if(this == Game.Player && IO.IOState == InputType.Inventory)
                //make sure that the player doesn't killed while invmanaging
                IO.IOState = InputType.PlayerInput;

            if (ds.Damage <= 0) return;
            if (HpCurrent <= 0) return;

            TileInfo tileInfo = World.Level.At(xy);
            Game.UI.UpdateAt(xy);
            tileInfo.Blood = true;
            tileInfo.Neighbours
                .Where(n => !n.Solid)
                .Where(n => Util.Random.Next(0, 4) >= 3)
                .ToList().ForEach(n =>
                    {
                        n.Blood = true;
                        Game.UI.UpdateAt(n.Position);
                    }
                );

            HpCurrent -= ds.Damage;

            if (HasEffect(StatusType.Sleep))
            {
                RemoveEffect(StatusType.Sleep);
                Game.UI.Log(
                    "{1} {2} up!",
                    GetName("Name"),
                    Verb("wake")
                );
            }

            if (HpCurrent > 0) return;

            Game.UI.Log(GetName("Name") + " " + Verb("die") + "!");
            if (this == Game.Player)
                Game.UI.Log(ds.GenerateKillMessage());

            World.Level.Despawn(this);

            Item corpse = new Item(
                xy,
                ItemDefinition.ItemDefinitions[Definition.CorpseType],
                0, Intrinsics
            );
            //should always be ided
            //or maybe not..? could be a mechanic in and of itself
            corpse.Identify(true);
            World.Level.Spawn(corpse);
            Game.Brains.RemoveAll(b => b.MeatPuppet == this);

            if(ds.Source != null)
                ds.Source.GiveExperience(Definition.Experience * Level);

            switch (ds.DamageType)
            {
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
                            ds.Source.Level
                        );
                        World.Level.Spawn(rat);
                    }
                    break;
            }
        }

        public void GiveExperience(int amount)
        {
            int lPre = Level;

            ExperiencePoints += amount;

            if (LevelFromExperience(ExperiencePoints) == lPre) return;

            Game.UI.Log(
                "{1} stronger",
                this == Game.Player
                    ? "You feel" :
                    GetName("Name") + " looks", Game
            );
            HpMax += Util.Roll(Definition.HitDie);
            MpMax += Util.Roll(Definition.ManaDie);
            Level = LevelFromExperience(ExperiencePoints);
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
                return LastingEffects.Any(
                    le => le.Type == type);
            return LastingEffects.Any(
                le => le.Type == type &&
                le.Ticker == ticker
            );
        }
        public void AddEffect(LastingEffect le)
        {
            if(!HasEffect(le.Type))
                LastingEffects.Add(le);
        }
        public void AddEffect(
            StatusType type,
            int duration,
            TickingEffectDefinition ticker = null
        ) {
            LastingEffects.Add(new LastingEffect(ID, type, duration, ticker));
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
                RemoveItem(item);
                World.Level.Despawn(item);
            }

            Game.UI.Log(
                string.Format("{0} ate {1}.",
                GetName("Name"),
                item.GetName("a"))
            );

            EdibleComponent ec = item.GetComponent<EdibleComponent>();

            AddFood(ec.Nutrition);

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
            int sneakMod = 0;
            if(HasEffect(StatusType.Sneak) && movement)
                sneakMod += 5;

            Cooldown =
                ODBGame.StandardActionLength -
                (movement ? Get(Stat.Speed) : Get(Stat.Quickness)) +
                sneakMod;
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
        //will atm only be called by the player,
        //but should, I guess, be called by monsters as well in the future
        public bool TryMove(Point offset)
        {
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
                return false;
            }

            bool moved = false;

            Tile target = World.Level.At(xy + offset).Tile;

            if (World.Level.ActorOnTile(target) == null)
            {
                xy.Nudge(offset.x, offset.y);
                moved = true;
                Pass(true);

                //walking noise
                World.Level.CalculateActorPositions();
                World.Level.MakeNoise(
                    xy,
                    NoiseType.FootSteps,
                    HasEffect(StatusType.Sneak)
                        ? -Get(Stat.Dexterity)
                        : 0
                );
            }
            else
            {
                Attack(World.Level.ActorOnTile(target));
                Pass();

                //combat noise
                World.Level.MakeNoise(xy, NoiseType.Combat, +2);
            }

            Game.UI.UpdateAt(xy);
            HasMoved = moved;

            return moved;
        }

        public bool HasMoved;

        public void ResetVision()
        {
            if (Vision == null)
                Vision = new bool[World.Level.Size.x, World.Level.Size.y];
            for (int x = 0; x < World.Level.Size.x; x++)
            for (int y = 0; y < World.Level.Size.y; y++)
                if (Vision[x, y])
                {
                    Vision[x, y] = false;
                    Game.UI.UpdateAt(x, y);
                }
        }
        public void AddRoomToVision(Room r)
        {
            foreach (Rect rr in r.Rects)
            for (int x = 0; x < rr.wh.x; x++)
                for (int y = 0; y < rr.wh.y; y++)
                {
                    Vision[rr.xy.x + x, rr.xy.y + y] = true;

                    if (this == Game.Player)
                        World.Level.See(rr.xy + new Point(x, y));
                        //World.Level.At(rr.xy + new Point(x, y)).Seen = true;
                }
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

        public void LearnSpell(Spell spell)
        {
            Definition.Spellbook.Add(spell.ID);
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

        public bool Sees(Point other)
        {
            if (Vision == null) return false;
            return Vision[other.x, other.y];
        }

        public void Chant(string chant)
        {
            BlackMagic.CheckCircle(this, chant);
        }

        public void Heal(int amount)
        {
            HpCurrent += amount;
            HpCurrent = Math.Min(HpCurrent, HpMax);
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
                    { Damage = HpCurrent, Target = this }
                );
                return FoodStatus.Starving;
            }

            if (_food <= 500) return FoodStatus.Starving;
            if (_food <= 1500) return FoodStatus.Hungry;
            if (_food <= 9000) return FoodStatus.Satisfied;
            if (_food <= 15000) return FoodStatus.Full;
            if (_food <= 20000) return FoodStatus.Stuffed;

            Damage(new DamageSource(
                "R.I.P. {0}, choked to death on their food.")
                { Damage = HpCurrent, Target = this }
            );
            return FoodStatus.Stuffed;
        }
        public void AddFood(int amount)
        {
            FoodStatus pre = GetFoodStatus();
            _food += amount;
            FoodStatus neo = GetFoodStatus();
            if (neo == pre) return;

            string message = "";
            switch (neo)
            {
                case FoodStatus.Hungry:
                    message = "You still feel hungry.";
                    break;
                case FoodStatus.Satisfied:
                    message = "Man, that hit the spot.";
                    break;
                case FoodStatus.Full:
                    message = "You feel full.";
                    break;
                case FoodStatus.Stuffed:
                    message = "Eugh, not even a mint'd go down now.";
                    break;
            }
            Game.UI.Log(message);
        }
        public void RemoveFood(int amount)
        {
            FoodStatus pre = GetFoodStatus();
            _food -= amount;
            FoodStatus neo = GetFoodStatus();
            if (neo == pre) return;

            string message = "";
            switch (neo)
            {
                case FoodStatus.Starving:
                    message =
                        "#ff0000" +
                        Util.Capitalize(Definition.Name) +
                        " needs food, badly!";
                    break;
                case FoodStatus.Hungry:
                    message = "You are starting to feel peckish.";
                    break;
                case FoodStatus.Full:
                    message = "Hm, a mint maybe isn't such a bad idea.";
                    break;
            }

            if(neo == FoodStatus.Hungry || neo == FoodStatus.Starving)
                if (HasEffect(StatusType.Sleep))
                {
                    RemoveEffect(StatusType.Sleep);
                    message += " You wake up due to hunger.";
                }

            if(message != "" && this == Game.Player)
                Game.UI.Log(message);
        }

        public void Do()
        {
            Do(IO.CurrentCommand);
        }

        private void HandleCast(Command cmd)
        {
            //we can trust the "spell" key to always be a spell,
            //because if it isn't, the blame isn't here, it's somewhere
            //earlier in the chain
            Spell spell = (Spell)cmd.Get("spell");

            //always spend energy, no matter if we succeed or not
            MpCurrent -= spell.Cost;

            if (Util.Roll("1d20") + Get(Stat.Intelligence) >=
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
            Chant(chant);
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
                Item clone = new Item(item.WriteItem().ToString()) {
                    ID = Item.IDCounter++,
                    Count = count
                };
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

            char index = IO.Indexes[Inventory.IndexOf(item)];
            if(this == Game.Player)
                Util.Game.UI.Log(index + " - "  + item.GetName("count") + ".");

            Pass();
        }

        private void HandleLearn(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log("You read {1}...",item.GetName("the"));

            item.Identify();

            Spell spell = Spell.Spells
                [item.GetComponent<LearnableComponent>().Spell];

            LearnSpell(spell);

            if (this == Game.Player)
                Game.UI.Log("You feel knowledgable about {1}!", spell.Name);

            Pass();
        }

        private void HandleOpen(Command cmd)
        {
            TileInfo targetTile = (TileInfo)cmd.Get("door");
            if(Game.Player.Sees(targetTile.Position))
                Game.UI.Log(
                    "{1} {2} {3} door.",
                    Game.Player.Sees(xy) ? GetName("Name") : "Something",
                    Verb("open"),
                    this == Game.Player ? "the" : "a"
                );
            targetTile.Door = Door.Open;

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

        public void HandleQuaff(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log("Drank {1}.", item.GetName("a"));

            DrinkableComponent dc = item.GetComponent<DrinkableComponent>();
            Spell.Spells[dc.Effect].Cast(this, null);

            item.SpendCharge();

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

        //read is for scrolls, learn is for tomes
        private void HandleRead(Command cmd)
        {
            Item item = (Item)cmd.Get("item");

            if (this == Game.Player)
                Game.UI.Log(
                    "You read {1}...",
                    item.GetName("the")
                );

            Spell spell = Spell.Spells
                [item.GetComponent<ReadableComponent>().Effect];

            if(spell.CastType == InputType.Targeting)
                spell.Cast(this, cmd.Get("target"));
            else
                spell.Cast(this, cmd.Get("answer"));

            Pass();
        }

        public void HandleRemove(Command cmd)
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
            Shoot(target);
        }

        private void HandleSleep(Command cmd)
        {
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
            Spell spell = Spell.Spells
                [item.GetComponent<UsableComponent>().UseEffect];
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
                case "cast": HandleCast(cmd); break; //zap
                case "chant": HandleChant(cmd); break;
                case "close": HandleClose(cmd); break;
                case "drop": HandleDrop(cmd); break;
                case "eat": HandleEat(cmd); break;
                case "engrave": HandleEngrave(cmd); break;
                case "get": HandleGet(cmd); break;
                case "learn": HandleLearn(cmd); break;
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
                    ODBGame.Game.UI.Log(
                        "{1} {2} up.",
                        GetName("Name"),
                        Verb("wake")
                    );
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

        public Stream WriteActor()
        {
            Stream stream = WriteGObject();
            stream.Write(Definition.Type, 4);

            stream.Write(ID, 4);

            stream.Write(_strength, 2);
            stream.Write(_dexterity, 2);
            stream.Write(_intelligence, 2);

            stream.Write(HpCurrent, 2);
            stream.Write(HpMax, 2);
            stream.Write(HpRegCooldown);
            stream.Write(MpCurrent, 2);
            stream.Write(MpMax, 2);
            stream.Write(MpRegCooldown);

            stream.Write(Level);
            stream.Write(ExperiencePoints);

            stream.Write(Cooldown, 2);
            stream.Write(_food);
            stream.Write(_quiver);

            foreach (BodyPart bp in PaperDoll)
            {
                stream.Write((int)bp.Type, 2);
                if(bp.Type == DollSlot.Hand)
                    if (((Hand)bp).Wielding)
                        stream.Write("W", false);
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

            stream.Write("{", false);
            foreach (LastingEffect le in LastingEffects)
            {
                stream.Write(
                    '{' + le.WriteLastingEffect().ToString() + '}',
                    false);
                stream.Write(",", false);
            }
            stream.Write("}", false);
            //stream.Write(";", false);

            foreach (Mod m in Intrinsics)
            {
                stream.Write((int)m.Type + ":" + m.RawValue + ",");
            }
            stream.Write(";", false);

            //stream.Write(Awake);

            return stream;
        }
        public Stream ReadActor(string s)
        {
            Stream stream = ReadGObject(s);
            _type = stream.ReadHex(4);

            ID = stream.ReadHex(4);

            _strength = stream.ReadHex(2);
            _dexterity = stream.ReadHex(2);
            _intelligence = stream.ReadHex(2);

            HpCurrent = stream.ReadHex(2);
            HpMax = stream.ReadHex(2);
            HpRegCooldown = stream.ReadInt();
            MpCurrent = stream.ReadHex(2);
            MpMax = stream.ReadHex(2);
            MpRegCooldown = stream.ReadInt();

            Level = stream.ReadInt();
            ExperiencePoints = stream.ReadInt();

            Cooldown = stream.ReadHex(2);
            _food = stream.ReadInt();
            _quiver = stream.ReadNInt();

            PaperDoll = new List<BodyPart>();
            foreach (string ss in stream.ReadString().NeatSplit(","))
            {
                string dsType = ss.Split(':')[0];
                bool wielding = dsType.Last() == 'W'; //handW => wielding hand
                if (wielding) dsType = dsType.Substring(0, dsType.Length - 1);

                DollSlot type = (DollSlot)IO.ReadHex(dsType);
                Item item = 
                    ss.Split(':')[1].Contains("X") ?
                        null :
                        Util.GetItemByID(IO.ReadHex(ss.Split(':')[1]));

                if(type == DollSlot.Hand)
                    PaperDoll.Add(new Hand(type, item) { Wielding = wielding });
                else
                    PaperDoll.Add(new BodyPart(type, item));
            }

            _inventory = new List<int>();
            foreach (string ss in stream.ReadString().NeatSplit(","))
            {
                _inventory.Add(IO.ReadHex(ss));
            }

            LastingEffects = new List<LastingEffect>();
            string lasting = stream.ReadBlock();

            foreach (string effect in lasting.Split(',')
                .Where(effect => effect != ""))
            {
                Stream effBlock = new Stream(effect);
                string effString = effBlock.ReadBlock();
                LastingEffects.Add(new LastingEffect(effString));
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

            //Awake = stream.ReadBool();

            return stream;
        }

        //should probably be moved to a container class or something
        public void GiveItem(Item item)
        {
            _inventory.Add(item.ID);
        }

        public void RemoveItem(Item item)
        {
            _inventory.Remove(item.ID);
        }
    }

    public enum NoiseType
    {
        FootSteps,
        Combat,
        Door
    }
}