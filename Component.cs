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
        public static string ComponentName = "UsableComponent";

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class AttackComponent : Component
    {
        public static string ComponentName = "AttackComponent";

        [DataMember(Name = "Damage")]
        public string Damage;

        [DataMember(Name = "Modifier")]
        public int Modifier;

        [DataMember(Name = "AttackType")]
        public AttackType AttackType;

        [DataMember(Name = "DamageType")]
        public DamageType DamageType;

        [DataMember(Name = "Effects")]
        public List<EffectComponent> Effects;

        public AttackComponent()
        {
            Effects = new List<EffectComponent>();
        }
    }

    [DataContract]
    public class WearableComponent : Component
    {
        public static string ComponentName = "WearableComponent";

        [DataMember(Name = "EquipSlots")]
        public List<DollSlot> EquipSlots;

        [DataMember(Name = "ArmorClass")]
        public int ArmorClass;
    }

    [DataContract]
    public class ProjectileComponent : Component
    {
        public static string ComponentName = "ProjectileComponent";

        [DataMember(Name = "Damage")]
        public string Damage;
    }

    [DataContract]
    public class LauncherComponent : Component
    {
        public static string ComponentName = "LauncherComponent";

        [DataMember(Name = "AmmoTypes")]
        public List<int> AmmoTypes;

        [DataMember(Name = "Damage")]
        public string Damage;
    }

    [DataContract]
    public class EdibleComponent : Component
    {
        public static string ComponentName = "EdibleComponent";

        [DataMember(Name = "Nutrition")]
        public int Nutrition;
    }

    //LH-211214: Currently does nothing but tag an item as a container.
    //           Saving here because we might want to limit container size
    //           in the future? Or what it can contain? Or how it scales weight?
    //           Could have a shortcut to InvMan.Containers[id] here as well.
    [DataContract]
    public class ContainerComponent : Component
    {
        public static string ComponentName = "ContainerComponent";
    }

    [DataContract]
    public class EffectComponent : Component
    {
        public static string ComponentName = "EffectComponent";

        [DataMember(Name = "EffectType")]
        public StatusType EffectType;

        [DataMember(Name = "Chance")]
        public int Chance;

        [DataMember(Name = "Length")]
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
        public static string ComponentName = "ReadableComponent";

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class LearnableComponent : Component
    {
        public static string ComponentName = "LearnableComponent";

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell TaughtSpell { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class DrinkableComponent : Component
    {
        public static string ComponentName = "DrinkableComponent";

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    //armor component
    //ac
    //armor type
}