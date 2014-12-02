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
                case "cUsable":
                    return UsableComponent.Create(content);
                case "cWeapon":
                    return WeaponComponent.Create(content);
                case "cWearable":
                    return WearableComponent.Create(content);
                case "cProjectile":
                    return ProjectileComponent.Create(content);
                case "cLauncher":
                    return LauncherComponent.Create(content);
                case "cEdible":
                    return EdibleComponent.Create(content);
                case "cContainer":
                    return ContainerComponent.Create(content);
                default:
                    throw new ArgumentException();
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
            return new UsableComponent {
                UseEffect = stream.ReadHex(4)
            };
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

    public class WeaponComponent : Component
    {
        public override string GetComponentType() { return "cWeapon"; }

        public string Damage;
        //todo: future: Have handedness depend on weight?
        public int Hands;
        public List<DollSlot> EquipSlots {
            get {
                List<DollSlot> slots = new List<DollSlot>();
                for(int i = 0; i < Hands; i++)
                    slots.Add(DollSlot.Hand);
                return slots;
            }
        }

        public static WeaponComponent Create(string content)
        {
            Stream stream = new Stream(content);
            return new WeaponComponent {
                Damage = stream.ReadString(),
                Hands = stream.ReadHex(2)
            };
        }

        public override Stream WriteComponent()
        {
            Stream stream = new Stream();
            stream.Write(GetComponentType());
            stream.Write("{", false);
            stream.Write(Damage);
            stream.Write(Hands, 2);
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
        public int Hands;
        public List<DollSlot> EquipSlots {
            get {
                List<DollSlot> slots = new List<DollSlot>();
                for(int i = 0; i < Hands; i++)
                    slots.Add(DollSlot.Hand);
                return slots;
            }
        }

        public static LauncherComponent Create(string content)
        {
            Stream stream = new Stream(content);

            List<int> ammoTypes =
                stream.ReadString().Split(',')
                    .Where(x => x != "")
                    .Select(IO.ReadHex)
            .ToList();

            string damage = stream.ReadString();

            int hands = stream.ReadHex(2);

            return new LauncherComponent {
                AmmoTypes = ammoTypes,
                Damage = damage,
                Hands = hands
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
}