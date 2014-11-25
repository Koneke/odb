using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Input;

namespace ODB
{
    public class Player
    {
        public static Game1 Game;

        public static int letterAnswerToIndex(char c)
        {
            int i;
            if (c >= 97 && c <= 122) //lower case
                i = c - 97;
            else //upper case
                i = c - 39;
            return i; 
        }

        /*
         * ToC:
         * Prompt answer functions:
             * Get
             * Drop
                 * DropCount
                 * Drop aux funcs
             * Wield
             * Sheath
             * Open
             * Close
             * Zap
         * Target answer functions:
             * Cast
         * Thanks for keeping order in the realm of man.
         */

        #region responses
        #region Get/Drop
        public static void Get(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);
            
            List<Item> onTile = Util.ItemsOnTile(
                Game.player.xy
            );

            if (i < onTile.Count)
            {
                Item it = onTile[i];
                Game.Level.WorldItems.Remove(it);

                if (it.Definition.stacking)
                {
                    bool alreadyHolding = false;
                    foreach (Item item in Game.player.inventory)
                        if (item.Definition.type == it.Definition.type)
                        {
                            if(!Game.player.IsEquipped(item)) {
                                alreadyHolding = true;
                                item.count+=it.count;
                                //merging into the already held item
                                Game.Level.AllItems.Remove(it);
                            }
                        }
                    if (!alreadyHolding)
                        Game.player.inventory.Add(it);
                }
                else
                {
                    Game.player.inventory.Add(it);
                }
                Game.log.Add("Picked up " + it.GetName() + ".");
                Game.player.Pass();
            }
            else
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }
        }

        public static void Drop(string answer)
        {
            //NOTE, PEEK.
            //BECAUSE WE NEED TO REUSE THE ANSWER LATER?
            //not actually using the stack here yet, but we want
            //to drop the string answer bit in the sign later...
            //I think.
            answer = Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);

            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            if (i < Game.player.inventory.Count)
            {
                Item it = Game.player.inventory[i];

                if (it.Definition.stacking && it.count > 1)
                {
                    IO.AcceptedInput.Clear();
                    for (Keys k = Keys.D0; k <= Keys.D9; k++)
                        IO.AcceptedInput.Add((char)k);

                    IO.AskPlayer(
                        "How many?",
                        InputType.QuestionPrompt,
                        Player.DropCount
                    );
                }
                else
                {
                    drop(i);
                }
            }
        }

        public static void DropCount(string answer)
        {
            string count = Game.qpAnswerStack.Pop();
            string index = Game.qpAnswerStack.Pop();
            int i = letterAnswerToIndex(index[0]);
            int c = int.Parse(count);
            drop(i, c);
        }

        //drop single nonstacking/all stacking
        static void drop(int index)
        {
            Item it = Game.player.inventory[index];

            Game.player.inventory.Remove(it);
            Game.Level.WorldItems.Add(it);

            it.xy = Game.player.xy;

            //actually make sure to unwield/unwear as well
            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Dropped " + it.GetName() + ".");

            Game.player.Pass();
        }

        static void drop(int index, int count)
        {
            Item it = Game.player.inventory[index];
            if (it.Definition.stacking && it.count > 1)
            {
                if (count > it.count)
                {
                    Game.log.Add("You don't have that many.");
                    return;
                }
                else if (count == it.count)
                {
                    //falling through to the normal itemdropping
                    drop(index);
                }
                else
                {
                    //copy the item
                    Item droppedStack =
                        new Item(
                            it.WriteItem().ToString()
                        );
                    //change id
                    droppedStack.id = Item.IDCounter++;
                    droppedStack.count = count;
                    it.count -= count;
                    Game.Level.WorldItems.Add(droppedStack);
                    Game.Level.AllItems.Add(droppedStack);

                    Game.log.Add("Dropped "+// count + " " +
                        it.GetName() + "."
                    );

                    Game.player.Pass();
                }
            }
        }
        #endregion

        #region Wield/Sheath
        public static void Wield(string answer)
        {
            answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);

            List<Item> equipables = new List<Item>();

            foreach (Item it in Game.player.inventory)
                //is it equipable?
                if (it.Definition.equipSlots.Count > 0)
                    equipables.Add(it);

            Item selected = Game.player.inventory[i];
            bool canEquip = true;
            foreach (DollSlot ds in selected.Definition.equipSlots)
                if (!Game.player.HasFree(ds))
                {
                    canEquip = false;
                    Game.log.Add("You need to remove something first.");
                }

            if (canEquip)
            {
                Game.log.Add("Equipped "+ selected.GetName() + ".");
                Game.player.Equip(selected);

                Game.player.Pass();
            }
        }

        public static void Wear(string answer)
        {
            //make sure we start using this instead
            //so we can phase the argument out
            answer = Game.qpAnswerStack.Pop();
            //and this instead of letterAnswerToIndex, which was dumb
            int i = IO.indexes.IndexOf(answer[0]);
            Item it = Game.player.inventory[i];

            bool canEquip = true;
            foreach (DollSlot ds in it.Definition.equipSlots)
                if (!Game.player.HasFree(ds))
                {
                    canEquip = false;
                    Game.log.Add("You need to remove something first.");
                }

            if (canEquip)
            {
                //make sure we're not equipping "2x ..."
                if (it.Definition.stacking && it.count > 1)
                {
                    Item clone = new Item(it.WriteItem().ToString());
                    clone.id = Item.IDCounter++;
                    clone.count--;
                    it.count = 1;
                    Game.player.inventory.Add(clone);
                    Game.Level.AllItems.Add(clone);
                }
                Game.player.Equip(it);
                Game.log.Add("Wore " + it.GetName() + ".");

                Game.player.Pass();
            }
        }

        public static void Sheath(string answer)
        {
            answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);
            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection (" + answer[0] + ").");
                return;
            }

            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == Game.player.inventory[i]) {
                    it = bp.Item;
                }

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Unequipped " + it.GetName() + ".");

            Game.player.Pass();
        }

        public static void Remove(string answer)
        {
            answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);
            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == Game.player.inventory[i])
                    it = bp.Item;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Removed " + it.GetName());

            Item stack = null;
            if (it.Definition.stacking)
                foreach (Item item in Game.player.inventory)
                    if (
                        item.Definition.type == it.Definition.type &&
                        item != it
                    ) stack = item;

            if (stack != null)
            {
                stack.count += it.count;
                Game.player.inventory.Remove(it);
                Game.Level.AllItems.Remove(it);
            }

        }
        #endregion

        #region Open/Close door
        public static void Open(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.door == Door.Closed)
            {
                t.door = Door.Open;
                Game.log.Add("You opened the door.");

                //counted as a movement action at the moment, based
                //on the dnd rules.
                Game.player.Pass(true);
            }
            else Game.log.Add("There's no closed door there.");
        }

        public static void Close(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.door == Door.Open)
            {
                //first check if something's in the way
                if (Util.ItemsOnTile(t).Count <= 0)
                {
                    t.door = Door.Closed;
                    Game.log.Add("You closed the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.player.Pass(true);
                }
                else Game.log.Add("There's something in the way.");
            }
            else Game.log.Add("There's no open door there.");
        }
        #endregion

        public static void Zap(string answer)
        {
            Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;
            int i = letterAnswerToIndex(answer[0]);
            if (i >= Game.player.Spellbook.Count)
            {
                Game.log.Add("Invalid selection (" + answer[0] + ").");
                return;
            }
            else
            {
                Game.TargetedSpell = Game.player.Spellbook[i];
                IO.AskPlayer(
                    "Casting " + Game.player.Spellbook[i].Name,
                    InputType.Targeting,
                    Player.Cast
                );
            }
        }

        public static void Use(string answer)
        {
            Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            Item it = Game.player.inventory[
                IO.indexes.IndexOf(answer[0])
            ];
            if (it.count <= 0)
            {
                Game.log.Add(it.GetName(true) + " lacks charges.");
                return;
            }

            //nontargeted
            if (it.UseEffect.Range <= 0)
            {
                Projectile pr = it.UseEffect.Cast(
                    Game.player,
                    Game.player.xy
                );
                pr.Move();
                it.count--;
                if (it.count <= 0)
                {
                    Game.player.inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.log.Add(it.GetName(true, false, true) + " is spent!");
                }
                Game.player.Pass();
            }
            //targeted
            else
            {
                Game.TargetedSpell = it.UseEffect;
                IO.AskPlayer(
                    "Using " + it.GetName(true),
                    InputType.Targeting,
                    Player.UseCast
                );
            }
        }

        public static void Cast(Point p)
        {
            int index = letterAnswerToIndex(Game.qpAnswerStack.Pop()[0]);
            if (Game.Level.Map[p.x, p.y] != null) {
                Game.player.Cast(
                    //Game.player.Spellbook[index], p
                    Game.TargetedSpell, p
                );
            }
            else
                Game.log.Add("Invalid target.");
        }

        public static void UseCast(Point p)
        {
            if (Game.Level.Map[p.x, p.y] != null)
            {
                int i = IO.indexes.IndexOf(Game.qpAnswerStack.Pop()[0]);
                Item it = Game.player.inventory[i];
                Game.log.Add("You use " + it.GetName(true) + ".");
                Projectile pr = Game.TargetedSpell.Cast(Game.player, p);
                pr.Move();
                //spend charge
                it.count--;
                //rechargin items should probably not trigger this
                //but that's a later issue
                if (it.count <= 0)
                {
                    Game.player.inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.log.Add(it.GetName(true, false, true) + " is spent!");
                }
                Game.player.Pass();
            }
            else
                Game.log.Add("Invalid target.");
        }
        #endregion

        public static void PlayerInput()
        {
            #region movement
            Point offset = new Point(0, 0);

            if (IO.KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
            if (IO.KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
            if (IO.KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
            if (IO.KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
            if (IO.KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
            if (IO.KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            if (IO.KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            if (IO.KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

            if (IO.KeyPressed(Keys.NumPad5)) Game.player.Pass(true);

            Tile target = Game.Level.Map[
                Game.player.xy.x + offset.x,
                Game.player.xy.y + offset.y
            ];

            if (offset.x != 0 || offset.y != 0)
                Game.player.TryMove(offset);

            //should be replaced with a look command
            //which could be called here maybe
            #region looking at our new tile
            //if we have moved, do fun stuff.
            if (offset.x != 0 || offset.y != 0)
            {
                List<Item> itemsOnSquare = Util.ItemsOnTile(Game.player.xy);

                switch (itemsOnSquare.Count)
                {
                    case 0:
                        break;
                    case 1:
                        Game.log.Add(
                            "There is " + itemsOnSquare[0].GetName() +
                            " here."
                        );
                        break;
                    default:
                        Game.log.Add(
                            "There are " + itemsOnSquare.Count +
                            " items here."
                        );
                        break;
                }
            }
            #endregion

            #endregion

            #region get/drop
            if (
                (IO.KeyPressed(Keys.G) && !IO.shift) ||
                (IO.KeyPressed(Keys.OemComma) && !IO.shift)
            ) {
                List<Item> onFloor = Util.ItemsOnTile(Game.player.xy);
                if (onFloor.Count > 1)
                {
                    string _q = "Pick up what? [";
                    IO.AcceptedInput.Clear();
                    for (int i = 0; i < onFloor.Count; i++)
                    {
                        char index = IO.indexes[i];
                        _q += index;
                        IO.AcceptedInput.Add(IO.indexes[i]);
                    }
                    _q += "]";

                    IO.AskPlayer(
                        _q,
                        InputType.QuestionPromptSingle,
                        Player.Get
                    );
                }
                //just more convenient this way
                else if (onFloor.Count > 0)
                {
                    Game.qpAnswerStack.Push("a");
                    Player.Get("a");
                }
            }

            if (IO.KeyPressed(Keys.D) && !IO.shift)
            {
                string _q = "Drop what? [";
                IO.AcceptedInput.Clear();
                for (int i = 0; i < Game.player.inventory.Count; i++)
                {
                    char index = IO.indexes[i];
                    _q += index;
                    IO.AcceptedInput.Add(IO.indexes[i]);
                }
                _q += "]";

                IO.AskPlayer(
                    _q,
                    InputType.QuestionPromptSingle,
                    Player.Drop
                );
            }
            #endregion

            #region wield/wear/sheath
            bool wield = IO.KeyPressed(Keys.W) && !IO.shift;
            bool wear = IO.KeyPressed(Keys.W) && IO.shift;
            if (wield || wear)
            {
                List<Item> equipables = new List<Item>();

                foreach (Item it in Game.player.inventory)
                    //is it wieldable?
                    if (
                        it.Definition.equipSlots.Contains(DollSlot.Hand)
                        && wield
                    )
                        equipables.Add(it);
                    else if (
                    //wearable?
                        it.Definition.equipSlots.FindAll(
                            x => x != DollSlot.Hand
                        ).Count > 0 && wear
                    )
                        equipables.Add(it);

                //remove items we've already equipped from the
                //list of potential items to equip
                foreach (BodyPart bp in Game.player.PaperDoll)
                    equipables.Remove(bp.Item);

                if (equipables.Count > 0)
                {
                    string _q = (IO.shift ? "Wear" : "Wield") + " what? [";
                    IO.AcceptedInput.Clear();
                    foreach (Item it in equipables)
                    {
                        //show the character corresponding with the one
                        //shown in the inventory.
                        char index =
                            IO.indexes[Game.player.inventory.IndexOf(it)];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                    _q += "]";

                    if(wield)
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            Player.Wield
                        );
                    else
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            Player.Wear
                        );
                }
                else
                {
                    Game.log.Add("Nothing to " +
                        (wield ? "wield" : "wear") + ".");
                }
            }

            //we probably actually want to split these
            //but it works well enough for right now
            bool sheath = IO.KeyPressed(Keys.S) && IO.shift;
            bool remove = IO.KeyPressed(Keys.R) && IO.shift;
            if (sheath || remove)
            {
                List<Item> equipped = new List<Item>();
                foreach (
                    BodyPart bp in Game.player.PaperDoll.FindAll(
                        x =>
                            (x.Type == DollSlot.Hand && sheath) ||
                            (x.Type != DollSlot.Hand && remove)
                    )
                ) {
                    if (bp.Item != null)
                        equipped.Add(bp.Item);
                }

                if (equipped.Count > 0)
                {
                    string _q =
                        (sheath?"Sheath":"Remove")+" what? [";
                    IO.AcceptedInput.Clear();
                    foreach (Item it in equipped)
                    {
                        char index =
                            IO.indexes[Game.player.inventory.IndexOf(it)];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                    _q += "]";

                    if(sheath)
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            Player.Sheath
                        );
                    else
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            Player.Remove
                        );
                }
                else
                {
                    Game.log.Add("Nothing to " +
                        (sheath ? "sheath." : "remove.")
                    );
                }
            }
            #endregion

            #region open/close
            if (IO.KeyPressed(Keys.O) && !IO.shift)
            {
                IO.AcceptedInput.Clear();

                for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                    //because some bright motherfucker at microsoft felt that
                    //97, aka 'a', was a good place for the numpad
                    IO.AcceptedInput.Add(
                        (char)(48 + i - Keys.NumPad0)
                    );
                
                IO.AskPlayer(
                    "Open where?",
                    InputType.QuestionPromptSingle,
                    Player.Open
                );
            }

            if (IO.KeyPressed(Keys.C) && !IO.shift)
            {
                IO.AcceptedInput.Clear();

                for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                    //this might seem wonk, but we're handling the
                    //numpad to normal number stuff, so it's k
                    IO.AcceptedInput.Add(
                        (char)(48 + i - Keys.NumPad0)
                    );
                
                IO.AskPlayer(
                    "Close where?",
                    InputType.QuestionPromptSingle,
                    Player.Close
                );
            }
            #endregion

            #region zap
            if (IO.KeyPressed(Keys.Z) && !IO.shift)
            {
                string _q = "Cast what? [";
                IO.AcceptedInput.Clear();
                for (int i = 0; i < Game.player.Spellbook.Count; i++)
                {
                    char index = IO.indexes[i];
                    _q += index;
                    IO.AcceptedInput.Add(index);
                }
                _q += "]";

                IO.AskPlayer(
                    _q,
                    InputType.QuestionPromptSingle,
                    Player.Zap
                );
            }
            #endregion

            if (IO.KeyPressed(Keys.A) && !IO.shift)
            {
                string _q = "Use what? [";
                IO.AcceptedInput.Clear();
                for (int i = 0; i < Game.player.inventory.Count; i++)
                {
                    if (Game.player.inventory[i].UseEffect != null)
                    {
                        char index = IO.indexes[i];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                }
                _q += "]";

                IO.AskPlayer(
                    _q,
                    InputType.QuestionPromptSingle,
                    Player.Use
                );
            }
        }
    }
}
