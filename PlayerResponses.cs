using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    class PlayerResponses
    {
        public static ODBGame Game;

        public static void Chant()
        {
            string answer = Game.QpAnswerStack.Pop();
            Game.UI.Log("You chant...");
            Game.UI.Log("\"" + Util.Capitalize(answer) + "...\"");
            Game.Player.Chant(answer);
        }

        public static void Close()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            TileInfo ti = World.Level.At(Game.Player.xy + offset);

            if(ti != null)
                if (ti.Door == Door.Open)
                {
                    //first check if something's in the way
                    if (ti.Items.Count <= 0)
                    {
                        ti.Door = Door.Closed;
                        Game.UI.Log("You closed the door.");

                        //counted as a movement action at the moment, based
                        //on the dnd rules.
                        Game.Player.Pass(true);
                        return;
                    }

                    Game.UI.Log("There's something in the way.");
                    return;
                }
            Game.UI.Log("There's no open door there.");
        }

        public static void Drop()
        {
            //NOTE, PEEK.
            //BECAUSE WE NEED TO REUSE THE ANSWER LATER?
            //not actually using the stack here yet, but we want
            //to drop the string answer bit in the sign later...
            //I think.
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

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
            string answer = Game.QpAnswerStack.Pop();
            int index = IO.Indexes.IndexOf(answer[0]);

            Item item = Game.Player.Inventory[index];

            Game.Player.Do(new Command("eat").Add("item", item));

        }

        public static void Engrave()
        {
            Game.Player.Do(new Command("engrave"));
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
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
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
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
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
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int index = IO.Indexes.IndexOf(answer[0]);
            Item item = Game.Player.Inventory[index];

            Game.Player.Do(new Command("quaff").Add("item", item));
        }

        public static void Quiver()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("quiver").Add("item", item));
        }

        public static void Read()
        {
            string answer = Game.QpAnswerStack.Pop();

            int index = IO.Indexes.IndexOf(answer[0]);
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
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("remove").Add("item", item));
        }

        public static void Sheathe()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item item = Game.Player.Inventory[i];

            Game.Player.Do(new Command("sheathe").Add("item", item));
        }

        public static void Split()
        {
            int count = int.Parse(Game.QpAnswerStack.Pop());
            int id = int.Parse(Game.QpAnswerStack.Pop());
            int container = int.Parse(Game.QpAnswerStack.Pop());

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
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            Item item = Game.Player.Inventory[IO.Indexes.IndexOf(answer[0])];

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

            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
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
            //make sure we start using this instead
            //so we can phase the argument out
            string answer = Game.QpAnswerStack.Pop();

            int i = IO.Indexes.IndexOf(answer[0]);
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
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            //Player:CheckZap asks a qps question, so current cmd
            //has been feed a string into ccmd.Answer, read index from it
            int index = IO.Indexes.IndexOf(
                Game.CurrentCommand.Answer[0]
            );
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
