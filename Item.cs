using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    public enum ItemTag
    {
        NonWeapon
    }

    public enum ItemID
    {
        Item_GoldCoin,
        Item_Apron,
        Item_Arrow,
        Item_Bow,
        Item_Longsword,
        Item_ClothBag,
        Item_Zweihander,
        Item_Chainmail,
        Item_Club,
        Item_Spear,
        Item_LeatherArmor,
        Item_Cloak,
        Item_WoodenShield,
        Item_ScrollOfForcebolt,
        Item_ScrollOfIdentify,
        Item_TomeOfForceBolt,
        Item_TomeOfIdentify,
        Item_PotionOfHealing,
        Item_Ration,
        Item_PlayerCorpse,
        Item_RatCorpse,
        Item_NewtCorpse,
        Item_KoboldCorpse,
        Item_KilikCorpse
    }

    [DataContract]
    public class Item
    {
        [DataMember] public int ID;
        [DataMember] public ItemID ItemType;
        [DataMember] public Point xy;
        [DataMember] public int LevelID;

        [DataMember] public int Mod;
        //can be used as charges for non-stacking?
        //-1 should be inf. charges?
        [DataMember] public int Count;
        [DataMember] public int Health;
        [DataMember] private int _type;
        [DataMember] public List<Mod> Mods;

        public ItemDefinition Definition
        {
            get { return ItemDefinition.DefDict[ItemType]; }
        }

        /*
         * bool BucKnown;
         * bool NameKnown; 
         * bool ModKnown;
         */

        //not to file
        //prop this?
        public bool Charged;

        //wraps
        public bool Stacking { get { return Definition.Stacking; } }

        public bool HasComponent<T>() where T : Component
            { return Definition.HasComponent<T>(); }
        public T GetComponent<T>() where T : Component
            { return Definition.GetComponent<T>(); }

        public Material Material { get { return Definition.Material; } }

        public Item() { }

        //SPAWNING a NEW item
        public Item(
            Point xy,
            ItemDefinition definition,
            int count = 0,
            IEnumerable<Mod> mods = null
        ) {
            this.xy = xy;
            ID = Game.IDCounter++;
            Count = count;
            ItemType = definition.ItemType;
            Health = definition.Health;
            Mods = new List<Mod>();
            if (mods != null) Mods.AddRange(mods);

            Charged = !Definition.Stacking && count > 0;
            if (Definition.HasComponent<ContainerComponent>())
                InventoryManager.ContainerIDs.Add(ID, new List<int>());
        }

        public bool Known
        {
            get
            {
                return
                    Game.IsIdentified(Definition.ItemType) ||
                    Definition.Category == 0xff;
            }
        }
        private string UnknownApperance
        {
            get
            {
                return ItemDefinition.Appearances
                    [Definition.Category].Shuffle()
                    [
                        ((int)Definition.ItemType + Math.Abs(Game.Seed))
                            % ItemDefinition.Appearances
                            [Definition.Category].Count
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

            Game.Identify(ItemType);

            if (!silent)
                Game.UI.Log("You identified " +
                    prename + " as " + GetName("count") + ".");
        }

        //LH-031214: We want to switch this to depend on the actor strength
        //           as well later, since a strong dude could probably one-hand
        //           an orc corpse or whatever.
        //           (LH-231214: ^ this bit is down, below is not)
        //           We probably also want to check if the item gives strength
        //           (or removes strength) via mods, since if you can one-hand
        //           a two-hander with the strength that two-hander gives, you
        //           should only need one hand.
        public List<DollSlot> GetHands(Actor a)
        {
            int hands;

            LauncherComponent lc = GetComponent<LauncherComponent>();

            if (lc != null) hands = 2;
            else
            {
                hands = Util.XperY(
                    1,
                    80 + 40 * a.Get(Stat.Strength), //240, 280, 320
                    Definition.Weight
                ) + 1;
            }

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
                ItemType == other.ItemType &&
                Health == other.Health
            ;
        }
        public void Stack(Item other)
        {
            Count += other.Count;
            World.Level.Despawn(other);
        }

        public void Damage(int mod = 0, Action<string> log = null)
        {
            WearableComponent wc;
            if ((wc = GetComponent<WearableComponent>()) != null)
            {
                //items actually made to be armour are more damage resistant
                mod -= wc.ArmorClass * 5;
            }

            if (Util.Random.Next(0, Materials.MaxHardness+1) + mod <
                Materials.GetHardness(Material))
                return;

            if(log != null)
                log(String.Format(
                    "#ff0000{0}#ffffff is " +
                    "#ff0000damaged by the impact#ffffff! ",
                    GetName("The")
                ));

            if (Stacking)
            {
                //spawn a stack of every item in the stack that wasn't the
                //wielded one
                Item stack = Clone();
                stack.Count = Count - 1;
                stack.xy = xy;
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
                        Game.Player.GiveItem(stack);
                        World.Instance.WorldItems.Remove(stack);
                    }
                    //otherwise, drop it into the world
                    else
                    {
                        if(log != null)
                            log(string.Format(
                                "{0} is dropped to the ground.",
                                GetName("The")
                            ));
                    }
                }
            }
            if (Health <= 1)
            {
                if(log != null)
                    log(string.Format(
                        "#ff0000{0} falls to pieces#ffffff!",
                        GetName("The")
                    ));
                Health--;
                World.Level.Despawn(this);
            }
            else Health--;
        }

        public void MoveTo(Level newLevel)
        {
            LevelID = newLevel.ID;
            if (!HasComponent<ContainerComponent>()) return;

            foreach (Item it in InventoryManager.Containers[ID])
                it.MoveTo(newLevel);
        }

        public bool HasTag(ItemTag nonWeapon)
        {
            return Definition.Tags.Contains(nonWeapon);
        }

        public Item Clone()
        {
            return new Item
            {
                xy = xy,
                ItemType = ItemType,
                LevelID = LevelID,
                ID = Game.IDCounter++,
                Mod = Mod,
                Count = Count,
                Health = Health,
                Mods =  Mods,
            };
        }
    }
}
