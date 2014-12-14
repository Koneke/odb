﻿using System;
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
                hashCode = (hashCode*397) ^
                           (Definition != null ? Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ HpCurrent;
                hashCode = (hashCode*397) ^ MpCurrent;
                hashCode = (hashCode*397) ^ Cooldown;
                hashCode = (hashCode*397) ^ Awake.GetHashCode();
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

        public static int IDCounter = 0;
        public int ID;

        public new ActorDefinition Definition;
        private int _strength, _dexterity, _intelligence;
        public int HpCurrent;
        public int MpCurrent;
        public int HpMax;
        public int MpMax;
        public int Level;
        public int ExperiencePoints;
        public int Cooldown;
        private int _food;

        public List<BodyPart> PaperDoll;
        public List<Item> Inventory;
        public List<LastingEffect> LastingEffects;
        public List<Mod> Intrinsics;

        public bool Awake;
        public Item Quiver;

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

        public Actor(
            Point xy,
            int levelid,
            ActorDefinition definition,
            int level
        ) : base(xy, levelid, definition)
        {
            ID = IDCounter++;
            Definition = definition;

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

            Level = 1;
            ExperiencePoints = 0;

            Cooldown = 0;

            _food = 9000;

            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in definition.BodyParts)
                PaperDoll.Add(new BodyPart(ds));
            Inventory = new List<Item>();
            Intrinsics = new List<Mod>(Definition.SpawnIntrinsics);
            Awake = false;
            LastingEffects = new List<LastingEffect>();
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
                    .Where(bp => bp.Type == ds && bp.Item == null))
                {
                    bp.Item = item;
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
            return PaperDoll
                .Where(bp => bp.Type != DollSlot.Hand)
                .Any(x => x.Item == item);
        }
        public List<Item> GetWornItems()
        {
            List<Item> equipped = new List<Item>();
            foreach (
                BodyPart bp in
                from bp in PaperDoll
                    where bp != null
                    where bp.Item != null
                    where bp.Type != DollSlot.Hand
                    where !equipped.Contains(bp.Item)
                    where bp.Item.HasComponent<WearableComponent>()
                select bp)
                equipped.Add(bp.Item);
            return equipped;
        }
        public bool IsWielded(Item item)
        {
            return PaperDoll
                .Where(bp => bp.Type == DollSlot.Hand)
                .Any(x => x.Item == item);
        }
        public List<Item> GetWieldedItems()
        {
            return PaperDoll
                .Where(bp => bp != null)
                .Where(bp => bp.Type == DollSlot.Hand)
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
            World.WorldItems.Add(item);
            Inventory.Remove(item);
            foreach (BodyPart bp in PaperDoll.Where(bp => bp.Item == item))
                bp.Item = null;
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
                { AttackType = AttackType.Bash, Damage = "1d4" };

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

            //foreach (Item weapon in GetWieldedItems())
            foreach(Tuple<Item, AttackComponent> attack in attacks)
            {
                if (totalDamage > target.HpCurrent) continue;

                Item weapon = attack.Item1;

                int roll = Util.Roll("1d20");
                bool crit = roll >= 20;
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
                            "d20+{0} ({1}+{2}+{3}+{4}-{5}), " +
                                "{6}+{0}, {7} vs. {8}. ",
                            totalModifier,
                            strBonus, dexBonus, Level, mod, multiWeaponPenalty,
                            roll,
                            hitRoll,
                            targetDefense
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
                if (weapon == null) continue;

                if (Util.Random.Next(0, Materials.MaxHardness+1) <
                    Materials.GetHardness(weapon.Material))
                    continue;

                Game.Log(
                    "{1} #ff0000{2} #ffffffis #ff0000damaged by the impact.",
                    Util.Capitalize(Genitive()),
                    weapon.GetName("name")
                );
                if (weapon.Stacking)
                {
                    //spawn a stack of every item in the stack that wasn't the
                    //wielded one
                    Item stack = new Item(weapon.WriteItem().ToString())
                    {
                        ID = Item.IDCounter++,
                        Count = weapon.Count - 1,
                        xy = xy,
                        LevelID = Game.Level.ID
                    };
                    weapon.Count = 1;

                    //if the wielded falls apart, or we have more than enough
                    //space in the inventory, put it there
                    if (stack.Count > 0)
                    {
                        Game.Level.Spawn(stack);
                        if (Game.Player.Inventory.Count <
                            InventoryManager.InventorySize ||
                            weapon.Health <= 1)
                        {
                            Game.Player.Inventory.Add(stack);
                            World.WorldItems.Remove(stack);
                        }
                            //otherwise, drop it into the world
                        else
                        {
                            message +=
                                string.Format(
                                    "{0} {1} and {2} {3} {4}. ",
                                    GetName("Name"),
                                    Verb("fumble"),
                                    Verb("drop"),
                                    Genitive(),
                                    stack.GetName("name")
                                );
                        }
                    }
                }
                if (weapon.Health <= 1)
                {
                    message += "#ff0000" + weapon.GetName("The") +
                        " falls to pieces!";
                    weapon.Health--;
                    Game.Level.Despawn(weapon);
                }
                else weapon.Health--;
            }

            Game.Log(message);
            target.Damage(totalDamage, this);
        }
        public void Shoot(Actor target)
        {
            Item weapon = null;
            LauncherComponent lc = null;

            //Not currently considering have several launchers equipped.
            if (Game.Player.GetEquippedItems().Any(
                x => x.HasComponent<LauncherComponent>()))
            {
                weapon = Game.Player.GetEquippedItems()
                    .Where(x => x.HasComponent<LauncherComponent>())
                    .ToList()[0];
                lc = weapon.GetComponent<LauncherComponent>();
            }

            Item ammo = Quiver;
            Debug.Assert(ammo != null, "ammo != null");

            ProjectileComponent pc =
                ammo.GetComponent<ProjectileComponent>();

            bool throwing;
            if (weapon != null && lc != null)
                throwing = !lc.AmmoTypes.Contains(ammo.Type);
            else throwing = true;

            int roll = Util.Roll("1d30");

            int dexBonus = Get(Stat.Dexterity);
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
            int totalDamage = 0;

            Item projectile = new Item(ammo.WriteItem().ToString())
            {
                ID = Item.IDCounter++,
                Count = ammo.Stacking ? 1 : 0,
                xy = target.xy,
                LevelID = Game.Level.ID
            };

            //-2, items are more likely to be damaged when shot/thrown than
            //when used in melee
            if (Util.Random.Next(0, Materials.MaxHardness+1) >=
                Materials.GetHardness(ammo.Material) - 2)
            {
                Game.Log(
                    projectile.GetName("The") + " is damaged by the impact."
                );
                if (projectile.Health <= 1) {
                    Game.Log(projectile.GetName("The") + " falls to pieces!");
                    projectile.Health--;
                    Game.Level.Despawn(projectile);
                } else projectile.Health--;
            }
            if (projectile.Health > 0) Game.Level.Spawn(projectile);

            ammo.Count--;
            if(ammo.Count <= 0) Game.Level.Despawn(ammo);

            if(hitRoll >= targetArmor) {
                int ammoDamage;
                int launcherDamage = 0;

                if (throwing)
                {
                    if(pc != null)
                        ammoDamage = Util.Roll(pc.Damage);
                    else
                        ammoDamage = Util.Roll("1d4");
                }
                else
                {
                    launcherDamage = Util.Roll(lc.Damage);
                    Debug.Assert(pc != null, "pc != null");
                    ammoDamage = Util.Roll(pc.Damage);
                }

                int damageRoll = ammoDamage + launcherDamage;
                totalDamage += damageRoll;

                message += target.GetName("Name") + " is hit! ";
                if (Game.OpenRolls)
                {
                    message +=
                        String.Format(
                            "d20+{0} ({1}+{2}-{3}), " +
                                "{4}+{0}, {5} vs. {6}. ",
                            totalModifier,
                            dexBonus, mod, distancePenalty,
                            roll,
                            hitRoll,
                            targetArmor
                        );
                    message +=
                        String.Format(
                            "{0}{1}, {2}{3}, {4} hit points damage.",
                            pc == null ? "1d4" : pc.Damage,
                            lc == null ? "" : ("+" + lc.Damage),
                            ammoDamage,
                            lc == null ? "" : ("+" + launcherDamage),
                            damageRoll
                        );
                }
            }
            else
            {
                message += GetName("Name") + " " + Verb("miss") + ". ";
                if (Game.OpenRolls)
                    message +=
                        String.Format(
                            "d20+{0} ({1}+{2}-{3}), " +
                                "{4}+{0}, {5} vs. {6}. ",
                            totalModifier,
                            dexBonus, mod, distancePenalty,
                            roll,
                            hitRoll,
                            targetArmor
                        );
            }

            Game.Log(message);
            if(totalDamage > 0)
                target.Damage(totalDamage, this);

            Game.Level.MakeNoise(1, target.xy);
            Pass();
        }
        public void Damage(int d, Actor attacker)
        {
            if (d <= 0) return;
            if (HpCurrent <= 0) return;

            TileInfo ti = Game.Level.At(xy);
            ti.Blood = true;
            ti.Neighbours
                .Where(n => !n.Solid)
                .Where(n => Util.Random.Next(0, 4) >= 3)
                .ToList().ForEach(n => n.Blood = true);

            HpCurrent -= d;
            if (HpCurrent > 0) return;

            Game.Log(GetName("Name") + " " + Verb("die") + "!");
            Item corpse = new Item(
                xy,
                LevelID,
                ItemDefinition.ItemDefinitions[Definition.CorpseType],
                0, Intrinsics
            );
            //should always be ided
            //or maybe not..? could be a mechanic in and of itself
            corpse.Identify(true);
            Game.Level.Spawn(corpse);
            Game.Brains.RemoveAll(b => b.MeatPuppet == this);

            if(attacker != null)
                attacker.GiveExperience(Definition.Experience * Level);
        }

        public void GiveExperience(int amount)
        {
            int lPre = Level;

            ExperiencePoints += amount;

            if (LevelFromExperience(ExperiencePoints) == lPre) return;

            Game.Log(
                "{1} stronger",
                this == Game.Player ? "You feel" : GetName("Name") + " looks"
            );
            HpMax += Util.Roll(Definition.HitDie);
            MpMax += Util.Roll(Definition.ManaDie);
            Level = LevelFromExperience(ExperiencePoints);
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
                Inventory.Remove(item);
                Game.Level.Despawn(item);
            }

            Game.Log(
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
            Cooldown = Game.StandardActionLength -
                (movement ? Get(Stat.Speed) : Get(Stat.Quickness));
        }
        public void Pass(int length)
        {
            Cooldown = length;
        }

        public List<Point> GetPossibleMoves(bool disallowActorTiles = false)
        {
            return
                Game.Level.At(xy)
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

            Tile target = Game.Level.At(xy + offset).Tile;

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
                    Game.Level.Size.x,
                    Game.Level.Size.y
                ];
            for (int x = 0; x < Game.Level.Size.x; x++)
                for (int y = 0; y < Game.Level.Size.y; y++)
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

                        if (this == Game.Player)
                            Game.Level.At(rr.xy + new Point(x, y)).Seen = true;
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

        public bool Sees(Actor other)
        {
            return Vision[other.xy.x, other.xy.y];
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
                case FoodStatus.Starving: return "starving";
                case FoodStatus.Hungry: return "hungry";
                case FoodStatus.Satisfied: return "satisfied";
                case FoodStatus.Full: return "full";
                case FoodStatus.Stuffed: return "stuffed";
            }
            throw new ArgumentException();
        }
        public FoodStatus GetFoodStatus()
        {
            if (_food < 500) return FoodStatus.Starving;
            if (_food < 1500) return FoodStatus.Hungry;
            if (_food < 10000) return FoodStatus.Satisfied;
            if (_food < 20000) return FoodStatus.Full;
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
            Game.Log(message);
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
                    message = Definition.Name + " needs food, badly!";
                    break;
                case FoodStatus.Hungry:
                    message = "You are starting to feel peckish.";
                    break;
                case FoodStatus.Full:
                    message = "Hm, a mint maybe isn't such a bad idea.";
                    break;
            }
            if(message != "")
                Game.Log(message);
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
            stream.Write(MpCurrent, 2);
            stream.Write(MpMax, 2);
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
            _strength = stream.ReadHex(2);
            _dexterity = stream.ReadHex(2);
            _intelligence = stream.ReadHex(2);
            HpCurrent = stream.ReadHex(2);
            HpMax = stream.ReadHex(2);
            MpCurrent = stream.ReadHex(2);
            MpMax = stream.ReadHex(2);
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
    }
}