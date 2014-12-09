using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public abstract class Component
    {
        public abstract string GetComponentType();
        public abstract Stream WriteComponent();

        public static Component CreateComponent(
            string component,
            string content
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
                case ReadableComponent.Type:
                    return ReadableComponent.Create(content);
                case LearnableComponent.Type:
                    return LearnableComponent.Create(content);
                default: throw new ArgumentException();
            }
        }
    }

    public class UsableComponent : Component
    {
        public override string GetComponentType() { return "cUsable"; }

        public int UseEffect;

        public static UsableComponent Create(string content)
        {
            Stream stream = new Stream(content);
            return new UsableComponent { UseEffect = stream.ReadHex(4) };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(UseEffect, 4);
            stream.Write("}", false);
            return stream;
        }
    }

    public class AttackComponent : Component
    {
        public override string GetComponentType() { return "cAttack"; }

        public string Damage;
        public AttackType AttackType;
        public List<EffectComponent> Effects;

        public AttackComponent()
        {
            Effects = new List<EffectComponent>();
        }

        public static AttackComponent Create(string content)
        {
            Stream stream = new Stream(content);

            string damage = stream.ReadString();
            AttackType attackType = Util.ReadAttackType(stream.ReadString());

            List<EffectComponent> effects = new List<EffectComponent>();
            Stream effectsBlock = new Stream(stream.ReadBlock());
            while (!effectsBlock.AtFinish)
                effects.Add(
                    (EffectComponent)
                    CreateComponent(
                        effectsBlock.ReadString(),
                        effectsBlock.ReadBlock()));

            return new AttackComponent {
                Damage = damage,
                AttackType = attackType,
                Effects = effects
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(Damage);
            stream.Write(Util.WriteAttackType(AttackType));
            stream.Write("{", false);
            foreach (EffectComponent ec in Effects)
                stream.Write(ec.WriteComponent(), false);
            stream.Write("}", false);
            stream.Write("}", false);
            return stream;
        }
    }

    public class WearableComponent : Component
    {
        public override string GetComponentType() { return "cWearable"; }

        public List<DollSlot> EquipSlots;
        public int ArmorClass;

        public static WearableComponent Create(string content)
        {
            Stream stream = new Stream(content);

            List<DollSlot> equipSlots =
                stream.ReadString().Split(',')
                .Where(slot => slot != "")
                .Select(slot => (DollSlot)IO.ReadHex(slot))
                .ToList();

            int armorClass = stream.ReadHex(2);

            return new WearableComponent
            {
                EquipSlots = equipSlots,
                ArmorClass = armorClass
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());

            string slots =
                EquipSlots.Aggregate(
                    "",
                    (current, ds) => current + (IO.WriteHex((int)ds, 2) + ",")
                );

            stream.Write("{", false);
            stream.Write(slots);
            stream.Write(ArmorClass, 2);
            stream.Write("}", false);

            return stream;
        }
    }

    public class ProjectileComponent : Component
    {
        public override string GetComponentType() { return "cProjectile"; }

        public string Damage;

        public static ProjectileComponent Create(string content)
        {
            Stream stream = new Stream(content);
            string damage = stream.ReadString();
            return new ProjectileComponent {
                Damage = damage
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(Damage);
            stream.Write("}", false);
            return stream;
        }
    }

    public class LauncherComponent : Component
    {
        public override string GetComponentType() { return "cLauncher"; }

        public List<int> AmmoTypes;
        public string Damage;

        public static LauncherComponent Create(string content)
        {
            Stream stream = new Stream(content);

            List<int> ammoTypes =
                stream.ReadString().Split(',')
                    .Where(x => x != "")
                    .Select(IO.ReadHex)
            .ToList();

            string damage = stream.ReadString();

            return new LauncherComponent {
                AmmoTypes = ammoTypes,
                Damage = damage,
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            string ammoTypes = AmmoTypes.Aggregate(
                "", (current, next) => current + IO.WriteHex(next, 4) + ",");
            stream.Write(ammoTypes);
            stream.Write(Damage);
            stream.Write("}", false);
            return stream;
        }
    }

    public class EdibleComponent : Component
    {
        public override string GetComponentType() { return "cEdible"; }

        public int Nutrition;

        public static EdibleComponent Create(string content)
        {
            Stream stream = new Stream(content);

            int nutrition = stream.ReadHex(4);

            return new EdibleComponent {
                Nutrition =  nutrition,
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(Nutrition, 4);
            stream.Write("}", false);
            return stream;
        }
    }

    public class ContainerComponent : Component
    {
        public override string GetComponentType() { return "cContainer"; }

        public List<int> Contained; 

        public static ContainerComponent Create(string content)
        {
            Stream stream = new Stream(content);

            List<int> contained = new List<int>();
            int count = content.Length / 4;
            for (int i = 0; i < count; i++)
                contained.Add(stream.ReadHex(4));

            return new ContainerComponent {
                Contained = contained,
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(
                Contained.Aggregate(
                    "", (c, n) => c + IO.WriteHex(n, 4)
                ), false
            );
            stream.Write("}", false);
            return stream;
        }
    }

    public class EffectComponent : Component
    {
        public const string Type = "cEffect";
        public override string GetComponentType() { return Type; }

        public StatusType EffectType;
        public int Chance;
        public string Length;

        public static EffectComponent Create(string content)
        {
            Stream stream = new Stream(content);
            return new EffectComponent
            {
                EffectType = LastingEffect.ReadStatusType(stream.ReadString()),
                Chance = stream.ReadHex(2),
                Length = stream.ReadString()
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(Type);
            stream.Write("{", false);
            stream.Write(LastingEffect.WriteStatusType(EffectType));
            stream.Write(Chance, 2);
            stream.Write(Length);
            stream.Write("}", false);
            return stream;
        }
    }

    public class ReadableComponent : Component
    {
        public const string Type = "cReadable";
        public override string GetComponentType() { return Type; }

        public int Effect;

        public static ReadableComponent Create(string content)
        {
            Stream stream = new Stream(content);
            int effect = stream.ReadHex(4);
            return new ReadableComponent {
                Effect = effect,
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(Type);
            stream.Write("{", false);
            stream.Write(Effect, 4);
            stream.Write("}", false);
            return stream;
        }
    }

    public class LearnableComponent : Component
    {
        public const string Type = "cLearnable";
        public override string GetComponentType() { return Type; }

        public int Spell;

        public static LearnableComponent Create(string content)
        {
            Stream stream = new Stream(content);
            return new LearnableComponent
            {
                Spell = stream.ReadHex(4)
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(Type);
            stream.Write("{", false);
            stream.Write(Spell, 4);
            stream.Write("}", false);
            return stream;
        }
    }
}