using System;
using System.Linq;

namespace ODB
{
    internal class Attack
    {
        public Actor Attacker;
        public Actor Target;
        public Item Weapon;
        public bool Crit;

        public Attack(Actor a, Actor t, Item w)
        {
            Attacker = a;
            Target = t;
            Weapon = w;
        }
    }

    internal class MeleeAttack : Attack
    {
        public AttackComponent AttackComponent;

        public MeleeAttack(Actor a, Actor t, Item w) : base(a, t, w)
        {
            if (w != null)
                AttackComponent = Combat.GetAttackComponent(w);
            else AttackComponent = a.Definition.NaturalAttack;
        }
    }

    internal class RangedAttack : Attack
    {
        public Item Launcher;
        public ProjectileComponent ProjectileComponent;
        public LauncherComponent LauncherComponent;

        public RangedAttack(Actor a, Actor t, Item w, Item l) : base(a, t, w)
        {
            Launcher = l;
            ProjectileComponent = Combat.GetProjectileComponent(w);

            if (l != null)
            {
                LauncherComponent = l.GetComponent<LauncherComponent>();
                if (!LauncherComponent.AmmoTypes.Contains(w.ItemType))
                    LauncherComponent = null;
            }
        }
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

        internal static AttackComponent GetAttackComponent(Item item)
        {
            return item.HasComponent<AttackComponent>()
                ? item.GetComponent<AttackComponent>()
                : Bash;
        }

        internal static ProjectileComponent GetProjectileComponent(Item item)
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

        public static bool Attack(MeleeAttack attack, Action<string> log)
        {
            DiceRoll hitRoll = GenerateMeleeHitRoll(attack);
            RollInfo roll = hitRoll.Roll();
            attack.Crit =
                roll.Result == 20 ||
                attack.Target.HasEffect(StatusType.Sleep);

            string message = "";
            DamageSource ds = null;

            if(Game.OpenRolls)
                roll.Log(true, s => message += s);

            if (roll.Result >= attack.Target.GetArmor())
            {
                message +=
                    AttackMessage.AttackMessages
                    [attack.AttackComponent.AttackType]
                    .SelectRandom()
                    .Instantiate(
                        attack.Attacker,
                        attack.Target,
                        attack.Weapon
                    );

                message += (attack.Crit ? "!" : ".") + " ";

                attack.AttackComponent.Effects
                    .ForEach(ec => ec.Apply(attack.Target));

                DiceRoll damageRoll = GenerateMeleeDamageRoll(attack);
                RollInfo damageInfo = damageRoll.Roll(attack.Crit);

                ds = new DamageSource
                {
                    Level = World.LevelByID(attack.Attacker.LevelID),
                    Position = attack.Attacker.xy,
                    Damage = damageInfo.Result,
                    AttackType = attack.AttackComponent.AttackType,
                    DamageType = attack.AttackComponent.DamageType,
                    Source = attack.Attacker,
                    Target = attack.Target
                };

                if (attack.Weapon != null)
                    attack.Weapon.Damage(0, s => message += s);

                if (Game.OpenRolls) damageInfo.Log(true, s => message += s);
            }
            else
            {
                message += SwingMessage(attack);
            }

            log(message);
            if (ds != null)
            {
                attack.Target.Damage(ds);
                return true;
            }
            return false;
        }

        public static Item SpawnProjectile(RangedAttack attack)
        {
            Item ammo = attack.Weapon;

            Item projectile;
            if (ammo.Stacking)
            {
                projectile = ammo.Clone();
                projectile.Count = 1;
                projectile.xy = attack.Target.xy;
                ammo.SpendCharge();
            }
            else
            {
                projectile = ammo;
                attack.Attacker.RemoveItem(ammo);
                ammo.xy = attack.Target.xy;
            }

            return projectile;
        }

        //send in atk instead so we cna mod it beforehand?
        public static void Shoot(
            Actor attacker,
            Actor target
        ) {
            Item weapon = attacker.GetWieldedItems()
                .FirstOrDefault(it => it.HasComponent<LauncherComponent>());

            Item ammo = attacker.Quiver;

            RangedAttack attack = new RangedAttack(
                attacker,
                target,
                ammo,
                weapon
            );

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

        public static bool Throw(
            RangedAttack attack,
            Action<string> log
        ) {
            DiceRoll hitRoll = GenerateRangedHitRoll(attack);
            RollInfo roll = hitRoll.Roll();

            attack.Crit = roll.Roll == 20 || attack.Target.FreeCrit();
            DamageSource ds = null;
            Item projectile = SpawnProjectile(attack);

            if (Game.OpenRolls) roll.Log();

            if (roll.Result >= attack.Target.GetArmor())
            {
                DiceRoll damageRoll = GenerateRangedDamageRoll(attack);
                RollInfo damageInfo = damageRoll.Roll(attack.Crit);

                ds = new DamageSource
                {
                    Level = World.LevelByID(attack.Attacker.LevelID),
                    Position = attack.Target.xy,
                    Damage = damageInfo.Result,
                    AttackType = AttackType.Pierce,
                    DamageType = DamageType.Physical,
                    Source = attack.Attacker,
                    Target = attack.Target
                };

                log(
                    string.Format(
                        "{0} is hit by {1}{2} ",
                        attack.Target.GetName("Name"),
                        projectile.GetName("the"),
                        attack.Crit ? "!" : "."
                    )
                );

                if (Game.OpenRolls) damageInfo.Log();
            }
            else
            {
                log(
                    string.Format(
                        "{0} {1}. ",
                        attack.Attacker.GetName("Name"),
                        attack.Attacker.Verb("miss")
                    )
                );
            }

            projectile.Damage(4, log);
            if (projectile.Health > 0)
                World.LevelByID(attack.Attacker.LevelID).Spawn(projectile);

            World.LevelByID(attack.Attacker.LevelID)
                .MakeNoise(attack.Target.xy, NoiseType.Combat, -2);

            if (ds != null)
            {
                attack.Target.Damage(ds);
                return true;
            }

            return false;
        }
    }
}
