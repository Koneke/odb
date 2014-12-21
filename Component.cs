using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ODB
{
    public abstract class Component
    {
        public abstract string GetComponentType();

        /*public static Component CreateComponent(
            string component,
            string conten
        ) {
            Stream stream = new Stream(component);

            //Component type
            switch (stream.ReadString())
            {
                case "cUsable": return UsableComponent.Create(content);
                case "cAttack": return AttackComponent.Create(content);
                case "cWearable": return WearableComponent.Create(content);
                case "cProjectile": return ProjectileComponent.Create(content);
                case "cLauncher": return LauncherComponent.Create(content);
                case "cEdible": return EdibleComponent.Create(content);
                case "cContainer": return ContainerComponent.Create(content);
                case "cEffect": return EffectComponent.Create(content);
                case "cReadable": return ReadableComponent.Create(content);
                case "cLearnable": return LearnableComponent.Create(content);
                case "cDrinkable": return DrinkableComponent.Create(content);
                default: throw new ArgumentException();
            }
        }*/
    }

    [DataContract]
    public class UsableComponent : Component
    {
        public override string GetComponentType() { return "cUsable"; }

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class AttackComponent : Component
    {
        public override string GetComponentType() { return "cAttack"; }

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
        public override string GetComponentType() { return "cWearable"; }

        [DataMember(Name = "EquipSlots")]
        public List<DollSlot> EquipSlots;

        [DataMember(Name = "ArmorClass")]
        public int ArmorClass;
    }

    [DataContract]
    public class ProjectileComponent : Component
    {
        public override string GetComponentType() { return "cProjectile"; }

        [DataMember(Name = "Damage")]
        public string Damage;
    }

    [DataContract]
    public class LauncherComponent : Component
    {
        public override string GetComponentType() { return "cLauncher"; }

        [DataMember(Name = "AmmoTypes")]
        public List<int> AmmoTypes;

        [DataMember(Name = "Damage")]
        public string Damage;
    }

    [DataContract]
    public class EdibleComponent : Component
    {
        public override string GetComponentType() { return "cEdible"; }

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
        public override string GetComponentType() { return "cContainer"; }
    }

    [DataContract]
    public class EffectComponent : Component
    {
        public override string GetComponentType() { return "cEffect"; }

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
        public override string GetComponentType() { return "cReadable"; }

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class LearnableComponent : Component
    {
        public override string GetComponentType() { return "cLearnable"; }

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell TaughtSpell { get { return Spell.SpellDict[_effectID]; } }
    }

    [DataContract]
    public class DrinkableComponent : Component
    {
        public override string GetComponentType() { return "cDrinkable"; }

        [DataMember(Name = "Effect")]
        private SpellID _effectID;

        public Spell Effect { get { return Spell.SpellDict[_effectID]; } }
    }

    //armor component
    //ac
    //armor type
}