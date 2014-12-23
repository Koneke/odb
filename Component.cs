using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ODB
{
    public abstract class Component
    {
    }

    [DataContract]
    public class UsableComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "UsableComponent";

        [DataMember(Name = "Effect", Order = 2)]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class AttackComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "AttackComponent";

        [DataMember(Name = "Damage", Order = 2)]
        public string Damage;

        [DataMember(Name = "Modifier", Order = 2)]
        public int Modifier;

        [DataMember(Name = "AttackType", Order = 2)]
        public AttackType AttackType;

        [DataMember(Name = "DamageType", Order = 2)]
        public DamageType DamageType;

        [DataMember(Name = "Effects", Order = 2)]
        public List<EffectComponent> Effects;

        public AttackComponent()
        {
            Effects = new List<EffectComponent>();
        }
    }

    [DataContract]
    public class WearableComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "WearableComponent";

        [DataMember(Name = "EquipSlots", Order = 2)]
        public List<DollSlot> EquipSlots;

        [DataMember(Name = "ArmorClass", Order = 2)]
        public int ArmorClass;
    }

    [DataContract]
    public class ProjectileComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "ProjectileComponent";

        [DataMember(Name = "Damage", Order = 2)]
        public string Damage;
    }

    [DataContract]
    public class LauncherComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "LauncherComponent";

        [DataMember(Name = "AmmoTypes", Order = 2)]
        public List<int> AmmoTypes;

        [DataMember(Name = "Damage", Order = 2)]
        public string Damage;
    }

    [DataContract]
    public class EdibleComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "EdibleComponent";

        [DataMember(Name = "Nutrition", Order = 2)]
        public int Nutrition;
    }

    //LH-211214: Currently does nothing but tag an item as a container.
    //           Saving here because we might want to limit container size
    //           in the future? Or what it can contain? Or how it scales weight?
    //           Could have a shortcut to InvMan.Containers[id] here as well.
    [DataContract]
    public class ContainerComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "ContainerComponent";
    }

    [DataContract]
    public class EffectComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "EffectComponent";

        [DataMember(Name = "EffectType", Order = 2)]
        public StatusType EffectType;

        [DataMember(Name = "Chance", Order = 2)]
        public int Chance;

        [DataMember(Name = "Length", Order = 2)]
        public string Length;

        public void Apply(Actor target, bool noRoll = false)
        {
            if (!(Util.Random.NextDouble() <= Chance / 100f)) return;

            target.AddEffect(
                LastingEffect.Create(
                    target.ID, EffectType, Util.Roll(Length)
                )
            );
        }
    }
 
    [DataContract]
    public class ReadableComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "ReadableComponent";

        [DataMember(Name = "Effect", Order = 2)]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class LearnableComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "LearnableComponent";

        [DataMember(Name = "Effect", Order = 2)]
        private SpellID _effectID;

        public Spell TaughtSpell { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class DrinkableComponent : Component
    {
        [DataMember(Order = 1)]
        public static string ComponentName = "DrinkableComponent";

        [DataMember(Name = "Effect", Order = 2)]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    //armor component
    //ac
    //armor type
}