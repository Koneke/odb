using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class PlayerResponses
    {
        public static ODBGame Game;

        private static void Cast(Spell spell)
        {
            //LH-021214: If the spells is nontargetted, just trigger the effect
            //           instantly.
            if (spell.CastType == InputType.None) spell.Cast();
            //LH-021214: Otherwise, ask a player of the spell's kind.
            //           Flexible, fancy, and reuses code! Woo!
            else
            {
                Game.Caster = Game.Player;
                IO.AcceptedInput.Clear();
                if (spell.CastType != InputType.Targeting)
                    spell.SetupAcceptedInput();
                else
                    for (char c = '0'; c <= '9'; c++) IO.AcceptedInput.Add(c);

                if (IO.AcceptedInput.Count <= 0)
                {
                    Game.Log("You have nothing to cast that on.");
                    Game.Caster = null;
                    return;
                }

                string question = "Cast " + spell.Name;
                switch (spell.CastType)
                {
                    case InputType.QuestionPrompt:
                    case InputType.QuestionPromptSingle:
                        question += " on what? ";
                        break;
                    case InputType.Targeting:
                        question += " where? ";
                        break;
                }

                if (spell.CastType != InputType.Targeting)
                {
                    question += "[";
                    question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                    question += "]";
                }

                IO.AskPlayer(
                    question,
                    spell.CastType,
                    spell.Cast
                );
            }
        }

        public static void Chant()
        {
            string answer = Game.QpAnswerStack.Pop();
            Game.Log("You chant...");
            Game.Log("\"" + Util.Capitalize(answer) + "...\"");
            Game.Player.Chant(answer);
        }

        public static void Close()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            TileInfo ti = Game.Level.At(Game.Player.xy + offset);

            if(ti != null)
                if (ti.Door == Door.Open)
                {
                    //first check if something's in the way
                    if (ti.Items.Count <= 0)
                    {
                        ti.Door = Door.Closed;
                        Game.Log("You closed the door.");

                        //counted as a movement action at the moment, based
                        //on the dnd rules.
                        Game.Player.Pass(true);
                        return;
                    }

                    Game.Log("There's something in the way.");
                    return;
                }
            Game.Log("There's no open door there.");
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

            Item it = Game.Player.Inventory[i];

            if (it.Definition.Stacking && it.Count > 1)
            {
                IO.AcceptedInput.Clear();
                /*for (Keys k = Keys.D0; k <= Keys.D9; k++)
                    IO.AcceptedInput.Add((char)k);*/
                IO.AcceptedInput.AddRange(IO.Numbers.ToList());

                IO.AskPlayer(
                    "How many?",
                    InputType.QuestionPrompt,
                    DropCount
                );
            }
            //just do a full drop, no splitting necessary
            else DoDrop(i);

            Game.Player.Pass();
        }

        public static void DropCount()
        {
            string count = Game.QpAnswerStack.Pop();
            string index = Game.QpAnswerStack.Pop();
            int i = IO.Indexes.IndexOf(index[0]);
            int c = int.Parse(count);
            DoDrop(i, c);
        }

        static void DoDrop(int index)
        {
            //make sure that we actually pop since we only peeked before
            Game.QpAnswerStack.Pop();

            Item it = Game.Player.Inventory[index];

            Game.Player.Inventory.Remove(it);

            List<Item> iot;
            if ((iot = Game.Level.ItemsOnTile(Game.Player.xy))
                .Any(item => item.CanStack(it)))
            {
                Item stack = iot.First(item => item.CanStack(it));
                stack.Stack(it);
            }
            else World.WorldItems.Add(it);

            it.xy = Game.Player.xy;

            //actually make sure to unwield/unwear as well
            foreach (BodyPart bp in Game.Player.PaperDoll
                .Where(bp => bp.Item == it))
                bp.Item = null;

            if (Game.Player.Quiver == it)
                Game.Player.Quiver = null;

            Game.Log("Dropped " + it.GetName("count") + ".");

            Game.Player.Pass();
        }

        static void DoDrop(int index, int count)
        {
            Item it = Game.Player.Inventory[index];
            if (!it.Definition.Stacking || it.Count <= 1) return;

            if (count > it.Count)
            {
                Game.Log("You don't have that many.");
            }
            else if (count == it.Count)
            {
                //falling through to the normal itemdropping
                Game.QpAnswerStack.Push("");
                DoDrop(index);
            }
            else
            {
                //copy the item
                Item droppedStack =
                    new Item(
                        //clone item
                        it.WriteItem().ToString()
                    ) {
                        xy = Game.Player.xy,
                        //mod the essential stuff
                        ID = Item.IDCounter++,
                        Count = count
                    };

                it.Count -= count;

                List<Item> iot;
                if ((iot = Game.Level.ItemsOnTile(Game.Player.xy))
                    .Any(item => item.CanStack(droppedStack)))
                {
                    Item stack = iot.First(item => item.CanStack(droppedStack));
                    stack.Stack(droppedStack);
                }
                else Game.Level.Spawn(droppedStack);

                Game.Log("Dropped " +
                    droppedStack.GetName("count") + "."
                );

                Game.Player.Pass();
            }
        }

        public static void Eat()
        {
            string answer = Game.QpAnswerStack.Pop();
            int index = IO.Indexes.IndexOf(answer[0]);

            Item it = Game.Player.Inventory[index];

            if (it.Definition.Stacking)
            {
                if (it.Count > 1)
                {
                    it.Count--;
                    Game.Player.Eat(it);
                }
                else
                {
                    Game.Player.Inventory.RemoveAt(index);
                    Game.Player.Eat(it);
                }
            }
            else
            {
                Game.Player.Inventory.RemoveAt(index);
                Game.Player.Eat(it);
            }

            Game.Player.Pass();
        }

        public static void Engrave()
        {
            string answer = Game.QpAnswerStack.Pop();
            Game.Level.At(Game.Player.xy).Tile.Engraving = answer;
            Game.Log("You wrote \""+answer+"\" on the dungeon floor.");
        }

        public static void Examine(bool verbose = false)
        {
            TileInfo ti = Game.Level.At(Game.Target);

            string distString =
                (Util.Distance(Game.Player.xy, Game.Target) > 0 ?
                    " there. " : " here. ");

            bool nonSeen = false;
            if (ti == null) nonSeen = true;
            else if (!ti.Seen) nonSeen = true;

            if (nonSeen)
            {
                Game.Log(
                    "You see nothing" + distString
                );
                return;
            }

            if (ti.Solid)
            {
                Game.Log("You see a dungeon wall" + distString);
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
                Game.Log(str);
        }

        public static void Fire()
        {
            if (!Game.Player.Vision[
                Game.Target.x, Game.Target.y
            ]) {
                Game.Log("You can't see that place.");
                return;
            }
            Actor a = Game.Level.ActorOnTile(Game.Target);
            //todo: allow firing anyways..?
            if (a == null)
            {
                Game.Log("Nothing there to fire upon.");
                return;
            }

            Game.Player.Shoot(a);
        }

        public static void Get()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            
            List<Item> onTile = Game.Level.ItemsOnTile(Game.Player.xy);

            Item it = onTile[i];
            World.WorldItems.Remove(it);

            if (it.Definition.Stacking)
            {
                //todo: I don't know how wanted this is?
                //      We could either always add a new stack,
                //      or just don't let the player split stacks,
                //      and instead just ask the player how many they
                //      want to move with P in the inventory screen.
                //      For now, this is good enough.
                Item stack = Game.Player.Inventory
                    .FirstOrDefault(item => item.CanStack(it));

                if (stack != null)
                {
                    Game.Log("Picked up " + it.GetName("count") + ".");
                    stack.Stack(it);
                    //so we can get the right char below
                    it = stack;
                }
                else Game.Player.Inventory.Add(it);
            }
            else Game.Player.Inventory.Add(it);

            char index = IO.Indexes[Game.Player.Inventory.IndexOf(it)];
            Util.Game.Log(index + " - "  + it.GetName("count") + ".");

            Game.Player.Pass();
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
            TileInfo ti = Game.Level.At(Game.Player.xy + offset);

            if(ti != null)
                if (ti.Door == Door.Closed)
                {
                    ti.Door = Door.Open;
                    Game.Log("You opened the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.Player.Pass(true);
                    return;
                }
            Game.Log("There's no closed door there.");
        }

        public static void Quaff()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int index = IO.Indexes.IndexOf(answer[0]);
            Item selected = Game.Player.Inventory[index];

            Game.Log("You drank " + selected.GetName("a"));
            DrinkableComponent dc = selected.GetComponent<DrinkableComponent>();
            Game.Caster = Game.Player;
            Spell.Spells[dc.Effect].Cast();
            selected.Identify();

            Game.Player.Pass();
        }

        public static void Quiver()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

            Item selected = Game.Player.Inventory[i];
            Game.Player.Quiver = selected;

            Game.Log("Quivered "+ selected.GetName("count") + ".");

            Game.Player.Pass();
        }

        public static void Read()
        {
            string answer = Game.QpAnswerStack.Pop();
            int index = IO.Indexes.IndexOf(answer[0]);

            Item item = Game.Player.Inventory[index];

            Game.Log("You read {1}...", item.GetName("name"));

            ReadableComponent rc =
                item.GetComponent<ReadableComponent>();

            if (rc != null)
            {
                item.SpendCharge();
                IO.UsedItem = item;
                item.Identify();

                Cast(Spell.Spells[rc.Effect]);
                return;
            }

            LearnableComponent lc =
                item.GetComponent<LearnableComponent>();

            Spell spell = Spell.Spells[lc.Spell];

            Game.Log("You feel knowledgable about {1}!", spell.Name);
            item.Identify();
            Game.Player.LearnSpell(spell);
            Game.Player.Pass();
        }

        public static void Remove()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item it = Game.Player.Inventory[i];

            foreach (BodyPart bp in Game.Player.PaperDoll
                .Where(bp => bp.Item == it))
                bp.Item = null;

            Game.Log("Removed " + it.GetName("a") + ".");

            Item stack =  Game.Player.Inventory.Find(
                item => item != it && item.Type == it.Type);

            if (stack == null) return;

            stack.Count += it.Count;
            Game.Player.Inventory.Remove(it);
            World.AllItems.Remove(it);

            Game.Player.Pass();
        }

        public static void Sheath()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

            Item it = Game.Player.Inventory[i];

            if (Game.Player.PaperDoll.Any(
                x => x.Item == it))
            {
                foreach (BodyPart bp in Game.Player.PaperDoll
                    .Where(bp => bp.Item == it))
                    bp.Item = null;

                Util.Game.Log("Sheathed " + it.GetName("a") + ".");
            }
            //it's our quivered item
            else
            {
                Game.Player.Quiver = null;
                Util.Game.Log("Unreadied " + it.GetName("count") + ".");
            }

            Game.Player.Pass();
        }

        public static void Split()
        {
            int count = int.Parse(Game.QpAnswerStack.Pop());
            int id = int.Parse(Game.QpAnswerStack.Pop());
            int container = int.Parse(Game.QpAnswerStack.Pop());

            Item item = Util.GetItemByID(id);
            if (count > item.Count)
            {
                Game.Log("You don't have that many.");
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
                Game.Log(item.GetName("The") + " lacks charges.");
                return;
            }

            //LH-021214: Note! Because we're spending a charge here, I've set it
            //           up so that we can't actually cancel the question we
            //           set up (unless the effect has InputType.None).
            //           This mainly so you don't cancel and lose out on a
            //           charge. /Might/ be wanted behaviour, we'll see.
            item.SpendCharge();
            IO.UsedItem = item;

            UsableComponent uc =
                item.GetComponent<UsableComponent>();

            //LH-021214: if uc is null here, we failed an earlier check.
            Debug.Assert(uc != null);

            Cast(Spell.Spells[uc.UseEffect]);
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
                Game.Log("You'd need more hands to do that!");
            else
            {
                Game.Log("Wielded " + item.GetName("a") + ".");
                Game.Player.Wield(item);
                Game.Player.Pass();
            }
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
                Game.Log("You need to remove something first.");
            }

            if (!canEquip) return;

            //make sure we're not equipping "2x ..."
            if (item.Definition.Stacking && item.Count > 1)
            {
                Item clone = new Item(
                    //clone
                    item.WriteItem().ToString()
                ) {
                    //no dupe ids pls
                    ID = Item.IDCounter++
                };

                clone.Count--;
                item.Count = 1;
                Game.Player.Inventory.Add(clone);
                World.AllItems.Add(clone);
            }
            Game.Player.Wear(item);
            Game.Log("Wore " + item.GetName("a") + ".");

            Game.Player.Pass();
        }

        public static void Zap()
        {
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int index = IO.Indexes.IndexOf(answer[0]);
            Spell spell = Game.Player.Spellbook[index];

            if (Game.Player.MpCurrent >= spell.Cost)
                Cast(spell);
            else
                Game.Log("You need more energy to cast that.");
        }

    }
}
