using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    internal class Attack
    {
        public Actor Attacker;
        public Actor Target;
        public Item Weapon;
        public bool Crit;
    }

    internal class MeleeAttack : Attack
    {
        public AttackComponent AttackComponent;
        public Dice Dice { get { return new Dice(AttackComponent.Damage); } }
    }

    internal class RangedAttack : Attack
    {
        public Item Launcher;
        public ProjectileComponent ProjectileComponent;
        public LauncherComponent LauncherComponent;
    }

    static class Combat
    {
        private static readonly AttackComponent Bash =
            new AttackComponent
        {
            Damage = "1d4",
            Modifier =  -2,
            AttackType = AttackType.Bash,
            DamageType = DamageType.Physical
        };

        private static readonly ProjectileComponent RangedBash =
            new ProjectileComponent
        {
            Damage = "1d4"
        };

        private static AttackComponent GetAttackComponent(Item item)
        {
            return item.HasComponent<AttackComponent>()
                ? item.GetComponent<AttackComponent>()
                : Bash;
        }

        private static ProjectileComponent GetProjectileComponent(Item item)
        {
            return item.HasComponent<ProjectileComponent>()
                ? item.GetComponent<ProjectileComponent>()
                : RangedBash;
        }

        private static DiceRoll GenerateMeleeHitRoll(Attack attack)
        {
            DiceRoll dr = new DiceRoll();
            Actor attacker = attack.Attacker;
            dr.Die.Add(new Dice(1, 20));

            int dexBonus;
            dr.Bonus.Add(
                "Dexterity",
                dexBonus = Util.XperY(1, 3, attacker.Get(Stat.Dexterity))
            );

            dr.Bonus.Add(
                "Strength",
                Util.XperY(1, 2, attacker.Get(Stat.Strength))
            );

            dr.Bonus.Add("Level", attacker.Xplevel);

            if(attack.Weapon != null)
                dr.Bonus.Add(
                    "Weapon Mod",
                    attack.Weapon.Mod
                );

            int multiWeaponPenalty = 3 * (attacker.GetWieldedItems().Count - 1);
            multiWeaponPenalty = Math.Max(0, multiWeaponPenalty - dexBonus);

            dr.Bonus.Add("Multi-Weapon", -multiWeaponPenalty);

            return dr;
        }

        private static DiceRoll GenerateMeleeDamageRoll(MeleeAttack attack)
        {
            DiceRoll dr = new DiceRoll();

            dr.Die.Add(new Dice(attack.AttackComponent.Damage));

            dr.Bonus.Add(
                "Strength",
                Util.XperY(1, 2, attack.Attacker.Get(Stat.Strength))
            );
            dr.Bonus.Add(
                "Level",
                attack.Attacker.Xplevel
            );

            return dr;
        }

        private static DiceRoll GenerateRangedHitRoll(RangedAttack attack)
        {
            DiceRoll dr = new DiceRoll();
            Actor attacker = attack.Attacker;
            dr.Die.Add(new Dice(1, 20));

            dr.Bonus.Add("Dexterity", attacker.Get(Stat.Dexterity));
            dr.Bonus.Add("Level", attacker.Xplevel);

            int distance = Util.Distance(attack.Attacker.xy, attack.Target.xy);
            int distanceModifier = 1;
            if (attack.ProjectileComponent == null) distanceModifier++;
            if (attack.LauncherComponent == null) distanceModifier++;

            dr.Bonus.Add(
                "Distance",
                -Util.XperY(distanceModifier, 1, distance)
            );

            dr.Bonus.Add("Ammo Mod", attack.Weapon.Mod);

            dr.Bonus.Add(
                "Weapon Mod",
                attack.Launcher == null ? 0 : attack.Launcher.Mod
            );

            return dr;
        }

        private static DiceRoll GenerateRangedDamageRoll(RangedAttack attack)
        {
            DiceRoll dr = new DiceRoll();

            dr.Die.Add(new Dice(attack.ProjectileComponent.Damage));
            if(attack.LauncherComponent != null)
                dr.Die.Add(new Dice(attack.LauncherComponent.Damage));

            dr.Bonus.Add("Level", attack.Attacker.Xplevel);

            return dr;
        }

        private static string SwingMessage(MeleeAttack attack)
        {
            //message for missing

            //null => natural attack, fists/teeth/whatever
            string weaponString = attack.Weapon == null
                ? ""
                : string.Format(
                    "{0} {1} ",
                    attack.Attacker.Genitive(),
                    attack.Weapon.GetName("name")
                );

            return String.Format(
                "{0} {1} {2}in the air. ",
                attack.Attacker.GetName("Name"),
                attack.Attacker.Verb("swing"),
                weaponString
            );
        }

        //todo: make this use an Attack-instance instead.
        //negative(?): more iteration of weapons and stuff outside of this
        //positive: handle different wielded weapons differently,
        //          return results of the different strikes separately.
        //todo: return whether or not attack hit (after above)
        public static void Attack(
            Actor attacker,
            Actor target
        ) {
            List<MeleeAttack> attacks = new List<MeleeAttack>();

            foreach (Item item in attacker.GetWieldedItems())
                attacks.Add(new MeleeAttack
                {
                    Attacker = attacker,
                    Target = target,
                    Weapon = item,
                    AttackComponent = GetAttackComponent(item)
                });

            if (attacks.Count == 0)
            {
                attacks.Add(new MeleeAttack
                {
                    Attacker = attacker,
                    Target = target,
                    Weapon = null,
                    AttackComponent = attacker.Definition.NaturalAttack
                });
            }

            List<DamageSource> damageSources = new List<DamageSource>();
            string message = "";

            foreach (MeleeAttack attack in attacks)
            {
                DiceRoll hitRoll = GenerateMeleeHitRoll(attack);
                RollInfo roll = hitRoll.Roll();

                bool crit = roll.Roll == 20;
                crit = crit || target.HasEffect(StatusType.Sleep);

                attack.Crit = crit;

                if (roll.Result > target.GetArmor())
                {
                    message +=
                        AttackMessage.AttackMessages
                            [attack.AttackComponent.AttackType]
                        .SelectRandom()
                        .Instantiate(attacker, target, attack.Weapon);
                    message += (crit ? "!" : ".") + " ";

                    attack.AttackComponent.Effects
                        .ForEach(ec => ec.Apply(target));

                    DiceRoll damageRoll = GenerateMeleeDamageRoll(attack);
                    RollInfo damageInfo = damageRoll.Roll(attack.Crit);

                    damageSources.Add(new DamageSource
                    {
                        Level = World.LevelByID(attacker.LevelID),
                        Position = attacker.xy,
                        Damage = damageInfo.Result,
                        AttackType = attack.AttackComponent.AttackType,
                        DamageType = attack.AttackComponent.DamageType,
                        Source = attacker,
                        Target = target
                    });

                    if (attack.Weapon != null)
                        attack.Weapon.Damage(0, s => message += s);

                    if (Game.OpenRolls)
                    {
                        roll.Log(true, s => message += s);
                        damageInfo.Log(true, s => message += s);
                    }
                }
                else
                {
                    message += SwingMessage(attack);
                    if (Game.OpenRolls) roll.Log(true, s => message += s);
                }

                if (damageSources.Sum(ds => ds.Damage) > target.HpCurrent)
                    break;
            }

            Game.UI.Log(message);
            World.Level.MakeNoise(attacker.xy, NoiseType.Combat, +2);
            damageSources.ForEach(target.Damage);
        }

        //send in atk instead so we cna mod it beforehand?
        public static void Shoot(
            Actor attacker,
            Actor target
        ) {
            LauncherComponent launcherComponent = null;
            Item weapon = attacker.GetWieldedItems()
                .FirstOrDefault(it => it.HasComponent<LauncherComponent>());
            if (weapon != null)
                launcherComponent = weapon.GetComponent<LauncherComponent>();

            Item ammo = attacker.Quiver;
            ProjectileComponent projectileComponent =
                //returns ranged bash if none is found
                GetProjectileComponent(ammo);

            //even if the weapon is a launcher, if we can't use it to launc this
            //kind of ammo, don't use it.
            if(launcherComponent != null)
                if (!launcherComponent.AmmoTypes.Contains(ammo.ItemType))
                    launcherComponent = null;

            RangedAttack attack = new RangedAttack
            {
                Attacker = attacker,
                Target = target,
                Launcher = weapon,
                Weapon = ammo,
                LauncherComponent = launcherComponent,
                ProjectileComponent = projectileComponent
            };

            DiceRoll hitRoll = GenerateRangedHitRoll(attack);
            RollInfo roll = hitRoll.Roll();

            attack.Crit = roll.Roll == 20 || target.HasEffect(StatusType.Sleep);

            string message = "";

            DamageSource damage = null;

            Item projectile;
            if (ammo.Stacking)
            {
                projectile = ammo.Clone();
                projectile.Count = 1;
                projectile.xy = target.xy;
                //ammo.Count--;
                ammo.SpendCharge();
            }
            else
            {
                projectile = ammo;
                attacker.RemoveItem(ammo);
                ammo.xy = target.xy;
            }

            if (Game.OpenRolls)
                roll.Log();

            if (roll.Result > target.GetArmor())
            {
                DiceRoll damageRoll = GenerateRangedDamageRoll(attack);
                RollInfo damageInfo = damageRoll.Roll(attack.Crit);

                damage = new DamageSource
                {
                    Level = World.LevelByID(attacker.LevelID),
                    Position = target.xy,
                    Damage = damageInfo.Result,
                    //todo: should be un-hardcoded
                    AttackType = AttackType.Pierce,
                    DamageType = DamageType.Physical,
                    Source = attacker,
                    Target = target
                };

                message += string.Format(
                    "{0} is hit by {1}{2} ",
                    target.GetName("Name"),
                    projectile.GetName("the"),
                    attack.Crit ? "!" : "."
                );

                if (Game.OpenRolls)
                    damageInfo.Log();
            }
            else
            {
                message += string.Format(
                    "{0} {1}. ",
                    attacker.GetName("Name"),
                    attacker.Verb("miss")
                );
            }

            projectile.Damage(4, s => message += s);
            if (projectile.Health > 0)
                World.LevelByID(attacker.LevelID).Spawn(projectile);

            Game.UI.Log(message);
            if (damage != null) target.Damage(damage);

            World.LevelByID(attacker.LevelID).MakeNoise(
                target.xy, NoiseType.Combat, -2
            );
        }
    }
}
