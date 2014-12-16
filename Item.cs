﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public class Item : gObject
    {
        protected bool Equals(Item other)
        {
            bool modsEqual = Mods.Count == other.Count;
            if (!modsEqual) return false;
            if (Mods.Where((t, i) => t != other.Mods[i]).Any())
            {
                return false;
            }

            return
                base.Equals(other) &&
                ID == other.ID &&
                Mod == other.Mod &&
                Count == other.Count &&
                Equals(Definition, other.Definition);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ ID;
                hashCode = (hashCode*397) ^ Mod;
                hashCode = (hashCode*397) ^ Count;
                hashCode = (hashCode*397) ^
                           (Definition != null ? Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^
                           (Mods != null ? Mods.GetHashCode() : 0);
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Item)obj);
        }

        public static int IDCounter = 0;

        //instance specifics
        public int ID;
        public int Mod;
        //can be used as charges for non-stacking?
        //-1 should be inf. charges?
        public int Count;
        public int Health;
        public new ItemDefinition Definition;
        public List<Mod> Mods;

        /*
         * bool BucKnown;
         * bool NameKnown; 
         * bool ModKnown;
         */

        //not to file
        public bool Charged;

        //wraps
        public bool Stacking { get { return Definition.Stacking; } }
        public int Type { get { return Definition.Type; } }

        public bool HasComponent<T>() where T : Component
            { return Definition.HasComponent<T>(); }
        public T GetComponent<T>() where T : Component
            { return Definition.GetComponent<T>(); }

        public Material Material { get { return Definition.Material; } }

        //SPAWNING a NEW item
        public Item(
            Point xy,
            int levelid,
            ItemDefinition definition,
            int count = 0,
            IEnumerable<Mod> mods = null
        ) : base(xy, levelid, definition) {
            ID = IDCounter++;
            Count = count;
            Definition = definition;
            Health = definition.Health;
            Mods = new List<Mod>();
            if (mods != null) Mods.AddRange(mods);

            Charged = !Definition.Stacking && count > 0;
            if (Definition.HasComponent<ContainerComponent>())
                InventoryManager.ContainerIDs.Add(ID, new List<int>());
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);
            Charged = !Definition.Stacking && Count > 0;
        }

        public bool Known
        {
            get
            {
                return
                    ItemDefinition.IdentifiedDefs.Contains(Definition.Type) ||
                    Definition.Category == 0xff
                ;
            }
        }
        private string UnknownApperance
        {
            get
            {
                return ItemDefinition.Appearances
                    [Definition.Category].Shuffle()
                    [
                        (Definition.Type + Math.Abs(Game.Seed))
                            %
                            ItemDefinition.Appearances[Definition.Category]
                                .Count
                    ];
            }
        }

        public string GetName(string format)
        {
            string appearance =
                Known
                    ? Definition.Name
                    : UnknownApperance;

            if (Health != Definition.Health)
            {
                int damageStrings = Materials.DamageStrings[Material].Count;
                int start = damageStrings - Definition.Health;
                int damage = Definition.Health - Health;
                appearance =
                    Materials.DamageStrings[Material][start + damage] + " " +
                        appearance;
            }

            string result;

            //Stacking item, but only one of it
            if (format == "count")
                if (Count == 1  || !Stacking) format = "a";
            if (format == "Count")
                if (Count == 1 || !Stacking) format = "A";

            switch (format.ToLower())
            {
                case "name":
                    result = appearance;
                    break;
                case "a":
                    result =
                        Util.Article(appearance) +
                        " " +
                        appearance;
                    break;
                case "the":
                    if(Stacking && Count > 1)
                        result =
                            "the " +
                            Count + "x " +
                            appearance + "s";
                    else
                        result =
                            "the" +
                            " " +
                            appearance;
                    break;
                case "count":
                    result =
                        Count +
                        "x " +
                        appearance +
                        "s"; //Handled the single, stacking item above
                    break;
                default:
                    throw new ArgumentException();
            }

            if (format[0] >= 'A' && format[0] <= 'Z')
                result = Util.Capitalize(result);

            return result;
        }

        public void Identify(
            //bool cursed,
            //bool blessed,
            //bool mods
            bool silent = false
        ) {
            //no need to double ID
            if (Known) return;

            string prename = GetName("the");
            ItemDefinition.IdentifiedDefs.Add(Type);
            if (!silent)
                Game.UI.Log("You identified " +
                    prename + " as " + GetName("count") + ".");
        }

        //LH-031214: We want to switch this to depend on the actor strength
        //           as well later, since a strong dude could probably one-hand
        //           an orc corpse or whatever.
        //           We probably also want to check if the item gives strength
        //           (or removes strength) via mods, since if you can one-hand
        //           a two-hander with the strength that two-hander gives, you
        //           should only need one hand.
        public List<DollSlot> GetHands(Actor a)
        {
            int hands;

            LauncherComponent lc = GetComponent<LauncherComponent>();

            if (lc != null) hands = 2;
            //360 decagram, i.e. 3.6kg, or ~8lb
            //todo: adjust for actor strength
            else hands = Definition.Weight >= 280 ? 2 : 1;

            List<DollSlot> slots = new List<DollSlot>();
            for (int i = 0; i < hands; i++)
                slots.Add(DollSlot.Hand);

            return slots;
        }
        public int GetWeight()
        {
            int weight = Definition.Weight;
            if (Stacking) weight *= Count;

            if (!HasComponent<ContainerComponent>()) return weight;

            weight += InventoryManager.Containers[ID]
                .Sum(item => item.GetWeight());
            return weight;
        }

        public void SpendCharge()
        {
            if (Stacking)
            {
                Count--;
                if (Count > 0) return;

                //LH-021214: Spent last of stacking item -> Remove it.
                World.Level.Despawn(this);
            }
            else
            {
                //LH-021214: Charging items are not removed when they hit 0
                //           charges, they remain, but uncharged (so they can
                //           potentially be recharged later).
                Count--;

                //IF we already were at 0, but could still be used
                //we were a (single-use) consumable
                if (Count != -1) return;

                World.Level.Despawn(this);
            }
        }

        public bool CanStack(Item other)
        {
            if (!Stacking) return false;
            return
                Type == other.Type &&
                Health == other.Health
            ;
        }
        public void Stack(Item other)
        {
            Count += other.Count;
            World.Level.Despawn(other);
        }

        public void Damage(int mod = 0)
        {
            if (Util.Random.Next(0, Materials.MaxHardness+1) + mod <
                Materials.GetHardness(Material))
                return;

            Game.UI.Log(
                "#ff0000{1}#ffffff is #ffffffdamaged by the impact#ffffff!",
                GetName("The")
            );
            if (Stacking)
            {
                //spawn a stack of every item in the stack that wasn't the
                //wielded one
                Item stack = new Item(WriteItem().ToString())
                {
                    ID = IDCounter++,
                    Count = Count - 1,
                    xy = xy,
                    LevelID = World.Level.ID
                };
                Count = 1;

                //if the wielded falls apart, or we have more than enough
                //space in the inventory, put it there
                if (stack.Count > 0)
                {
                    World.Level.Spawn(stack);
                    if (Game.Player.Inventory.Count <
                        InventoryManager.InventorySize ||
                        Health <= 1)
                    {
                        Game.Player.Inventory.Add(stack);
                        World.WorldItems.Remove(stack);
                    }
                    //otherwise, drop it into the world
                    else
                    {
                        Game.UI.Log(
                            "{1} is dropped to the ground.",
                            GetName("The")
                        );
                    }
                }
            }
            if (Health <= 1)
            {
                Game.UI.Log(
                    "#ff0000{1} falls to pieces!",
                    GetName("The")
                );
                Health--;
                World.Level.Despawn(this);
            }
            else Health--;
        }

        public Stream WriteItem()
        {
            Stream stream = WriteGObject();

            stream.Write(Definition.Type, 4);
            stream.Write(ID, 4);
            stream.Write(Mod, 2);
            stream.Write(Count, 2);
            stream.Write(Health);

            foreach (Mod m in Mods)
            {
                stream.Write((int)m.Type, 2);
                stream.Write(":", false);
                stream.Write(m.RawValue, 2);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }
        public Stream ReadItem(string s)
        {
            Stream stream = ReadGObject(s);
            Definition =
                ItemDefinition.ItemDefinitions[
                    stream.ReadHex(4)
                ];

            ID = stream.ReadHex(4);
            Mod = stream.ReadHex(2);
            Count = stream.ReadHex(2);
            Health = stream.ReadInt();

            Mods = new List<Mod>();
            List<string> mods = stream.ReadString().Split(
                new[] {','},
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string[] ss in mods.Select(mod => mod.Split(':')))
            {
                Mods.Add(new Mod(
                    (ModType)
                    IO.ReadHex(ss[0]),
                    IO.ReadHex(ss[1])
                ));
            }

            return stream;
        }

        public void MoveTo(Level newLevel)
        {
            LevelID = newLevel.ID;
            if (!HasComponent<ContainerComponent>()) return;

            foreach (Item it in InventoryManager.Containers[ID])
                it.MoveTo(newLevel);
        }
    }
}
