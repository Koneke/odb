using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    internal class Attack
    {
        public Actor Attacker;
        public Actor Target;
        public Item Item;
        public AttackComponent AttackComponent;
        public bool Crit;

        public Dice Dice { get { return new Dice(AttackComponent.Damage); } }
    }

    static class Combat
    {
        private static readonly AttackComponent Bash = new AttackComponent
        {
            Damage = "1d4",
            Modifier =  -2,
            AttackType = AttackType.Bash,
            DamageType = DamageType.Physical
        };

        private static AttackComponent GetAttackComponent(Item item)
        {
            return item.HasComponent<AttackComponent>()
                ? item.GetComponent<AttackComponent>()
                : Bash;
        }

        public static DiceRoll GenerateHitRoll(Actor attacker)
        {
            DiceRoll dr = new DiceRoll();

            dr.Dice = new Dice(1, 20);

            int dexBonus;
            dr.Bonus.Add(
                "Dexterity",
                dexBonus = Util.XperY(1, 3, attacker.Get(Stat.Dexterity))
            );
            dr.Bonus.Add(
                "Strength",
                Util.XperY(1, 2, attacker.Get(Stat.Strength))
            );
            dr.Bonus.Add(
                "Level",
                attacker.Xplevel
            );

            int multiWeaponPenalty = 3 * (attacker.GetWieldedItems().Count - 1);
            multiWeaponPenalty = Math.Max(0, multiWeaponPenalty - dexBonus);

            dr.Malus.Add(
                "Multi-Weapon",
                multiWeaponPenalty
            );

            return dr;
        }

        private static DiceRoll GenerateDamageRoll(Attack attack)
        {
            DiceRoll dr = new DiceRoll();

            dr.Dice = new Dice(attack.AttackComponent.Damage);

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

        private static string SwingMessage(Attack attack)
        {
            //message for missing

            //null => natural attack, fists/teeth/whatever
            string weaponString = attack.Item == null
                ? ""
                : string.Format(
                    "{0} {1} ",
                    attack.Attacker.Genitive(),
                    attack.Item.GetName("name")
                );

            return String.Format(
                "{0} {1} {2}in the air. ",
                attack.Attacker.GetName("Name"),
                attack.Attacker.Verb("swing"),
                weaponString
            );
        }

        public static void Attack(
            Actor attacker,
            Actor target
        ) {
            List<Attack> attacks = new List<Attack>();

            foreach (Item item in attacker.GetWieldedItems())
                attacks.Add(new Attack
                {
                    Attacker = attacker,
                    Target = target,
                    Item = item,
                    AttackComponent = GetAttackComponent(item)
                });

            if (attacks.Count == 0)
            {
                attacks.Add(new Attack
                {
                    Attacker = attacker,
                    Target = target,
                    Item = null,
                    AttackComponent = attacker.Definition.NaturalAttack
                });
            }

            List<DamageSource> damageSources = new List<DamageSource>();
            string message = "";

            foreach (Attack attack in attacks)
            {
                DiceRoll hitRoll = GenerateHitRoll(attacker);
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
                        .Instantiate(attacker, target, attack.Item);
                    message += (crit ? "!" : ".") + " ";

                    attack.AttackComponent.Effects
                        .ForEach(ec => ec.Apply(target));

                    DiceRoll damageRoll = GenerateDamageRoll(attack);
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

                    if (attack.Item != null)
                        attack.Item.Damage(0, s => message += s);

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
            damageSources.ForEach(target.Damage);
        }
    }
}
