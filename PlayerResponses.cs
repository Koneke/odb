using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class PlayerResponses
    {
        public static Game1 Game;

        public static void Get()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            
            List<Item> onTile = Game.Level.ItemsOnTile(
                Game.Player.xy
            );

            Item it = onTile[i];
            Game.Level.WorldItems.Remove(it);

            if (it.Definition.Stacking)
            {
                bool alreadyHolding = false;
                foreach (Item item in Game.Player.Inventory
                    .Where(item => item.Definition.Type == it.Definition.Type)
                    .Where(item => !Game.Player.IsEquipped(item))
                ) {
                    alreadyHolding = true;
                    item.Count+=it.Count;
                    //merging into the already held item
                    Game.Level.AllItems.Remove(it);
                }
                if (!alreadyHolding)
                    Game.Player.Inventory.Add(it);
            }
            else
            {
                Game.Player.Inventory.Add(it);
            }
            Util.Game.Log("Picked up " + it.GetName("count") + ".");
            Game.Player.Pass();
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
                for (Keys k = Keys.D0; k <= Keys.D9; k++)
                    IO.AcceptedInput.Add((char)k);

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
            Game.Level.WorldItems.Add(it);

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
                        //mod essential stuff
                        ID = Item.IDCounter++,
                        Count = count
                    };

                it.Count -= count;
                Game.Level.Spawn(droppedStack);
                droppedStack.xy = Game.Player.xy;

                Game.Log("Dropped " +
                    droppedStack.GetName("count") + "."
                );

                Game.Player.Pass();
            }
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
            Item stack = new Item(item.WriteItem().ToString());
            stack.Count = count;
            stack.ID = Item.IDCounter++;
            Game.Level.AllItems.Add(stack);

            if (container == -1)
                Game.Player.Inventory.Add(stack);
            else
                Game.InvMan.ContainerIDs[container].Add(stack.ID);

            IO.IOState = InputType.Inventory;
        }

        public static void Wield()
        {
            //todo: if the player can one-hand it, ask if they want to 2hand it

            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item item = Game.Player.Inventory[i];

            WeaponComponent wc =
                (WeaponComponent)
                item.Definition.GetComponent("cWeapon");

            List<DollSlot> equipSlots;
            if (wc != null) equipSlots = wc.EquipSlots;
            else equipSlots = item.GetHands(Game.Player);

            if (!Game.Player.CanEquip(equipSlots))
                Game.Log("You'd need more hands to do that!");
            else
            {
                Game.Log("Wielded " + item.GetName("a") + ".");
                Game.Player.Wield(item);
                item.Identify();
                Game.Player.Pass();
            }
        }

        public static void Wear()
        {
            //make sure we start using this instead
            //so we can phase the argument out
            string answer = Game.QpAnswerStack.Pop();

            int i = IO.Indexes.IndexOf(answer[0]);
            Item it = Game.Player.Inventory[i];

            bool canEquip = true;

            WearableComponent wc =
                (WearableComponent)
                it.Definition.GetComponent("cWearable");

            if (!Game.Player.CanEquip(wc.EquipSlots))
            {
                canEquip = false;
                Game.Log("You need to remove something first.");
            }

            if (!canEquip) return;

            //make sure we're not equipping "2x ..."
            if (it.Definition.Stacking && it.Count > 1)
            {
                Item clone = new Item(
                    //clone
                    it.WriteItem().ToString()
                ) {
                    //no dupe ids pls
                    ID = Item.IDCounter++
                };

                clone.Count--;
                it.Count = 1;
                Game.Player.Inventory.Add(clone);
                Game.Level.AllItems.Add(clone);
            }
            Game.Player.Wear(it);
            it.Identify();
            Game.Log("Wore " + it.GetName("a") + ".");

            Game.Player.Pass();
        }

        public static void Quiver()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

            Item selected = Game.Player.Inventory[i];
            Game.Player.Quiver = selected;

            //todo: should we really identify on quiver?
            //      or should we rather do it on fire?
            selected.Identify();
            Game.Log("Quivered "+ selected.GetName("count") + ".");

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
            Game.Level.AllItems.Remove(it);

            Game.Player.Pass();
        }

        public static void Open()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.Player.xy.x + offset.x,
                    Game.Player.xy.y + offset.y
                ];

            if(t != null)
                if (t.Door == Door.Closed)
                {
                    t.Door = Door.Open;
                    Game.Log("You opened the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.Player.Pass(true);
                    return;
                }
            Game.Log("There's no closed door there.");
        }

        public static void Close()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Point p = new Point(
                Game.Player.xy.x + offset.x,
                Game.Player.xy.y + offset.y);
            Tile t = Game.Level.Map[p.x, p.y];

            if(t != null)
                if (t.Door == Door.Open)
                {
                    //first check if something's in the way
                    if (Game.Level.ItemsOnTile(p).Count <= 0)
                    {
                        t.Door = Door.Closed;
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

        public static void Zap()
        {
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int index = IO.Indexes.IndexOf(answer[0]);

            //LH-021214: If the spells is nontargetted, just trigger the effect
            //           instantly.
            if (Spell.Spells[index].CastType == InputType.None)
            {
                Spell.Spells[index].Cast();
            }
            //LH-021214: Otherwise, ask a player of the spell's kind.
            //           Flexible, fancy, and reuses code! Woo!
            else
            {
                Game.Caster = Game.Player;
                Spell.Spells[index].SetupAcceptedInput();

                if (IO.AcceptedInput.Count <= 0)
                {
                    Game.Log("You have nothing to cast that on.");
                    return;
                }

                string question = "Cast " + Spell.Spells[index].Name;
                switch (Spell.Spells[index].CastType)
                {
                    case InputType.QuestionPrompt:
                    case InputType.QuestionPromptSingle:
                        question += " on what? ";
                        break;
                    case InputType.Targeting:
                        question += " where? ";
                        break;
                }
                question += "[";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

                IO.AskPlayer(
                    question,
                    Spell.Spells[index].CastType,
                    Spell.Spells[index].Cast
                );
            }
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

            UsableComponent uc =
                (UsableComponent)
                item.GetComponent("cUsable");

            //LH-021214: if uc is null here, we failed an earlier check.
            Debug.Assert(uc != null);

            //LH-021214: If the spells is nontargetted, just trigger the effect
            //           instantly.
            if (Spell.Spells[uc.UseEffect].CastType == InputType.None)
                Spell.Spells[uc.UseEffect].Cast();
            //LH-021214: Otherwise, ask a player of the spell's kind.
            //           Flexible, fancy, and reuses code! Woo!
            else
            {
                Game.Caster = Game.Player;
                Spell.Spells[uc.UseEffect].SetupAcceptedInput();

                string question = "Use " + item.GetName("the");
                switch (Spell.Spells[uc.UseEffect].CastType)
                {
                    case InputType.QuestionPrompt:
                    case InputType.QuestionPromptSingle:
                        question += " on what? ";
                        break;
                    case InputType.Targeting:
                        question += " where? ";
                        break;
                }
                question += "[";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

                IO.AskPlayer(
                    question,
                    Spell.Spells[uc.UseEffect].CastType,
                    Spell.Spells[uc.UseEffect].Cast
                );
            }
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

        public static void Look()
        {
            Game.Target = Game.Player.xy;
            Examine(true);
        }

        public static void Examine(bool verbose = false)
        {
            Tile t = Game.Level.Map[Game.Target.x, Game.Target.y];

            string distString =
                (Util.Distance(Game.Player.xy, Game.Target) > 0 ?
                    " there. " : " here. ");

            if (t == null || !Game.Level.Seen[Game.Target.x, Game.Target.y])
            {
                Game.Log(
                    "You see nothing" + distString
                );
                return;
            }
            if (t.Solid)
            {
                Game.Log("You see a dungeon wall" + distString);
                return;
            }

            List<Item> items = Game.Level.ItemsOnTile(Game.Target);
            string str = "";

            if (verbose)
            {
                if (t.Door != Door.None) str = "You see a door. ";
                else if (t.Stairs != Stairs.None)
                    str = "You see a set of stairs.";
                else str = "You see the dungeon floor. ";
            }


            if (Game.Player.Vision[Game.Target.x, Game.Target.y])
            {
                if (t.Engraving != "")
                    str += "\"" + t.Engraving + "\" is written on the floor" +
                        distString;

                Actor a;
                if ((a = Game.Level.ActorOnTile(t)) != null)
                    if(a != Game.Player)
                        str += "You see " + a.GetName("a") + distString;
                if (items.Count > 0)
                {
                    if (items.Count == 1)
                        str +=
                            "There's " + items[0].GetName("count") +
                            distString;
                    else
                        str += "There's several items" + distString;
                }
            }

            if(str != "")
                Game.Log(str);
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

        }
    }
}
