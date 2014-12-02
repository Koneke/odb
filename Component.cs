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

        public static WeaponComponent Create(string content)
        {
            Stream stream = new Stream(content);
            return new WeaponComponent {
                Damage = stream.ReadString()
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

        public static LauncherComponent Create(string content)
        {
            Stream stream = new Stream(content);

            List<int> ammoTypes =
                stream.ReadString().Split(',')
                    .Where(x => x != "")
                    .Select(IO.ReadHex)
            .ToList();

            return new LauncherComponent {
                AmmoTypes = ammoTypes
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
            stream.Write("}", false);
            return stream;
        }
    }
}