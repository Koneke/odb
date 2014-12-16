using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    class PlayerResponses
    {
        public static ODBGame Game;

        public static void Chant()
        {
            Game.Player.Do(new Command("chant").Add("chant", IO.Answer));
        }

        public static void Close()
        {
            Point offset = Game.NumpadToDirection(IO.Answer[0]);
            TileInfo ti = World.Level.At(Game.Player.xy + offset);

            if(ti != null)
                if (ti.Door == Door.Open)
                {
                    Game.Player.Do(new Command("close").Add("door", ti));
                    return;
                }
            Game.UI.Log("There's no open door there.");
        }

        public static void Drop()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);

            Item item = Game.Player.Inventory[i];

            Game.CurrentCommand = new Command("drop").Add("item", item);

            if (item.Definition.Stacking && item.Count > 1)
            {
                IO.AcceptedInput.Clear();
                IO.AcceptedInput.AddRange(IO.Numbers.ToList());

                IO.AskPlayer(
                    "How many?",
                    InputType.QuestionPrompt,
                    DropCount
                );
            }
            //just do a full drop, no splitting necessary
            else Game.Player.Do(Game.CurrentCommand.Add("count", 0));
        }

        public static void DropCount()
        {
            int count = int.Parse(IO.Answer);

            if (count > ((Item)Game.CurrentCommand.Get("item")).Count)
            {
                Game.UI.Log("You don't have that many.");
                return;
            }

            Game.Player.Do(Game.CurrentCommand.Add("count", count));
        }

        public static void Eat()
        {
            int index = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[index];
            Game.Player.Do(new Command("eat").Add("item", item));
        }

        public static void Engrave()
        {
            Game.Player.Do(new Command("engrave").Add("text", IO.Answer));
        }

        public static void Examine(bool verbose = false)
        {
            TileInfo ti = World.Level.At(Game.Target);

            string distString =
                (Util.Distance(Game.Player.xy, Game.Target) > 0 ?
                    " there. " : " here. ");

            bool nonSeen = false;
            if (ti == null) nonSeen = true;
            else if (!ti.Seen) nonSeen = true;

            if (nonSeen)
            {
                Game.UI.Log(
                    "You see nothing" + distString
                );
                return;
            }

            if (ti.Solid)
            {
                Game.UI.Log("You see a dungeon wall" + distString);
                return;
            }

            List<Item> items = ti.Items;
            string str = "";

            if (verbose)
            {
                if (ti.Door != Door.None) str = "You see a door. ";
                else if (ti.Stairs != Stairs.None)
                    str = "You see a set of stairs.";
                else str = "You see the dungeon floor. ";
            }


            if (Game.Player.Vision[Game.Target.x, Game.Target.y])
            {
                if (ti.Tile.Engraving != "")
                    str += "\"" + ti.Tile.Engraving +
                        "\" is written on the floor" +
                        distString;

                if (ti.Actor != null)
                    if(ti.Actor != Game.Player)
                        str += "You see " + ti.Actor.GetName("a") + distString;

                if (items.Count > 0)
                {
                    if (items.Count == 1)
                        str +=
                            "There's " + items[0].GetName("count") +
                                distString;
                    else
                    {
                        if (!verbose)
                            str += "There's several items" + distString;
                        else
                        {
                            str += items.Aggregate(
                                "There's ",
                                (c, n) => c + n.GetName("count") + ", "
                                );
                            str = str.Substring(0, str.Length - 2);
                            str += distString;
                        }
                    }
                }
            }

            if(str != "")
                Game.UI.Log(str);
        }

        public static void Fire()
        {
            if (!Game.Player.Sees(Game.Target)) {
                Game.UI.Log("You can't see that place.");
                return;
            }
            Actor a = World.Level.ActorOnTile(Game.Target);

            //todo: allow firing anyways..?
            if (a == null)
            {
                Game.UI.Log("Nothing there to fire upon.");
                return;
            }

            //can't use "target" here, since that crashes with the
            //key used for InputType.Targeting.
            //also, might want to do the choose weapon/ammo bits here,
            //and add those as weapon/ammo keys to the command.
            Game.Player.Do(new Command("shoot").Add("actor", a));
        }

        public static void Get()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            List<Item> onTile = World.Level.ItemsOnTile(Game.Player.xy);
            Item item = onTile[i];

            Game.Player.Do(new Command("get").Add("item", item));
        }

        public static void Look()
        {
            Examine(true);
        }

        public static void Open()
        {
            Point offset = Game.NumpadToDirection(IO.Answer[0]);
            TileInfo ti = World.Level.At(Game.Player.xy + offset);

            if(ti != null)
                if (ti.Door == Door.Closed)
                {
                    Game.Player.Do(new Command("open").Add("door", ti));
                    return;
                }
            Game.UI.Log("There's no closed door there.");
        }

        public static void Quaff()
        {
            int index = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[index];

            Game.Player.Do(new Command("quaff").Add("item", item));
        }

        public static void Quiver()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("quiver").Add("item", item));
        }

        public static void Read()
        {
            int index = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[index];

            ReadableComponent rc =
                item.GetComponent<ReadableComponent>();

            //we know from Player.CheckRead that this item either has
            //a ReadableComponent, or a LearnableComponent, guaranteed.

            if (rc != null)
            {
                Spell effect = Spell.Spells[rc.Effect];

                Game.CurrentCommand = new Command("read").Add("item", item);

                if (effect.CastType == InputType.None)
                    Game.Player.Do(Game.CurrentCommand);
                else
                {
                    if (effect.SetupAcceptedInput != null)
                        effect.SetupAcceptedInput(Game.Player);

                    if (IO.AcceptedInput.Count <= 0)
                    {
                        Game.UI.Log("You have nothing to cast that on.");
                        return;
                    }

                    string question = effect.CastType == InputType.Targeting
                            ? "Where? ["
                            : "On what? [";
                    question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                    question += "]";

                    IO.AskPlayer(
                        question,
                        effect.CastType,
                        Game.Player.Do
                    );
                }
            }
            else Game.Player.Do(new Command("learn").Add("item", item));
        }

        public static void Remove()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("remove").Add("item", item));
        }

        public static void Sheathe()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("sheathe").Add("item", item));
        }

        //todo: mig this
        public static void Split()
        {
            int count = int.Parse(IO.Answer);
            int id = (int)Game.CurrentCommand.Get("item-id");
            int container = (int)Game.CurrentCommand.Get("container");

            Item item = Util.GetItemByID(id);
            if (count > item.Count)
            {
                Game.UI.Log("You don't have that many.");
                return;
            }

            item.Count -= count;
            Item stack = new Item(item.WriteItem().ToString()) {
                Count = count, ID = Item.IDCounter++
            };
            World.AllItems.Add(stack);

            if (container == -1)
                Game.Player.Inventory.Add(stack);
            else
                InventoryManager.ContainerIDs[container].Add(stack.ID);

            IO.IOState = InputType.Inventory;
        }

        public static void Use()
        {
            Item item = Game.Player.Inventory
                [IO.Indexes.IndexOf(IO.Answer[0])];

            if (item.Count <= 0)
            {
                //Note: This can never happen with stacking items, since there
                //      wouldn't be any of them to select.
                Game.UI.Log(item.GetName("The") + " lacks charges.");
                return;
            }

            Spell useEffect =
                Spell.Spells
                [item.GetComponent<UsableComponent>().UseEffect];

            Game.CurrentCommand.Add(
                "item",
                item
            );

            if (useEffect.CastType == InputType.None)
                Game.Player.Do(Game.CurrentCommand);
            else
            {
                if (useEffect.SetupAcceptedInput != null)
                    useEffect.SetupAcceptedInput(Game.Player);

                if (IO.AcceptedInput.Count <= 0)
                {
                    Game.UI.Log("You have nothing to cast that on.");
                    return;
                }

                string question = useEffect.CastType == InputType.Targeting
                        ? "Where? ["
                        : "On what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

                IO.AskPlayer(
                    question,
                    useEffect.CastType,
                    Game.Player.Do
                );
            }
        }

        public static void Wield()
        {
            //todo: if the player can one-hand it, ask if they want to 2hand it

            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[i];

            List<DollSlot> equipSlots = item.GetHands(Game.Player);

            if (!Game.Player.CanEquip(equipSlots))
            {
                Game.UI.Log("You'd need more hands to do that!");
                return;
            }

            Game.Player.Do(new Command("wield").Add("item", item));
        }

        public static void Wear()
        {
            int i = IO.Indexes.IndexOf(IO.Answer[0]);
            Item item = Game.Player.Inventory[i];

            bool canEquip = true;

            WearableComponent wc =
                item.Definition.GetComponent<WearableComponent>();

            if (!Game.Player.CanEquip(wc.EquipSlots))
            {
                canEquip = false;
                Game.UI.Log("You need to remove something first.");
            }

            if (!canEquip) return;

            Game.Player.Do(new Command("wear").Add("item", item));
        }

        public static void Zap()
        {
            //Player:CheckZap asks a qps question, so current cmd
            //has been feed a string into ccmd.Answer, read index from it
            //scratch that, we just read the answer ourselves.
            int index = IO.Indexes.IndexOf(IO.Answer[0]);

            Spell spell = Game.Player.Spellbook[index];

            if (Game.Player.MpCurrent < spell.Cost)
            {
                Game.UI.Log("You need more energy to do that.");
                return;
            }

            //save the spell to be cast using that index to the ccmd
            ODBGame.Game.CurrentCommand.Add("spell", spell);

            //no more data required, gogo
            if (spell.CastType == InputType.None)
                Game.Player.Do(Game.CurrentCommand);

            //or ask for a target
            //Game.Player.Do() handles Do specifically for the player
            //automatically promotes the Answer/Target to a "real" key
            //AI never does this, since they generate a complete cmd on the spot
            else
            {
                //qp/qps spells setup their own input
                //where they filter OK targets (i.e. say we can only cast on
                //weapons, or something). targetted spells doesn't need this,
                //so check if it's null.
                if(spell.SetupAcceptedInput != null)
                    spell.SetupAcceptedInput(Game.Player);

                if (IO.AcceptedInput.Count <= 0)
                {
                    Game.UI.Log("You have nothing to cast that on.");
                    return;
                }

                string question = spell.CastType == InputType.Targeting
                        ? "Where? ["
                        : "On what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

                IO.AskPlayer(
                    question,
                    spell.CastType,
                    Game.Player.Do
                );
            }
        }
    }
}
