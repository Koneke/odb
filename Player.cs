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
            Game.worldItems.Add(it);

            it.xy = Game.player.xy;

            //actually make sure to unwield/unwear as well
            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Dropped " + it.Definition.name + ".");

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
                    Item droppedStack = new Item(it.WriteItem());
                    //change id
                    droppedStack.id = Item.IDCounter++;
                    droppedStack.count = count;
                    it.count -= count;
                    Game.worldItems.Add(droppedStack);
                    Game.allItems.Add(droppedStack);

                    Game.log.Add("Dropped " + count + " " +
                        it.Definition.name + "."
                    );

                    Game.player.Pass();
                }
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

            if(Game.logPlayerActions)
                Game.log.Add(" > Drop item");

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
                    Game.setupQuestionPrompt("How many?", false);
                    Game.acceptedInput = Game.numbers;
                    Game.questionReaction = Player.DropCount;
                    Game.questionPromptOpen = true;
                }
                else
                {
                    drop(i);
                }
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

            bool itemWielded = false;
            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll.FindAll(
                x => x.Type == DollSlot.Hand))
                if (bp.Item == Game.player.inventory[i]) {
                    itemWielded = true;
                    it = bp.Item;
                }

            if (itemWielded)
            {
                foreach (BodyPart bp in Game.player.PaperDoll.FindAll(
                    x => x.Type == DollSlot.Hand))
                    if (bp.Item == it) bp.Item = null;
                Game.log.Add("Sheathed " + it.Definition.name + ".");

                Game.player.Pass();
            }
        }

        public static void Wield(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            if(Game.logPlayerActions)
                Game.log.Add(" > Wield item");

            int i = letterAnswerToIndex(answer[0]);

            List<Item> equipables = new List<Item>();

            foreach (Item it in Game.player.inventory)
                //is it equipable?
                if (it.Definition.equipSlots.Count > 0)
                    equipables.Add(it);

            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            if (!equipables.Contains(Game.player.inventory[i]))
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            Item selected = Game.player.inventory[i];
            bool canEquip = true;
            foreach (DollSlot ds in selected.Definition.equipSlots)
            {
                if (!Game.player.HasFree(ds))
                {
                    canEquip = false;
                    Game.log.Add("You need to remove something first.");
                }
            }

            if (canEquip)
            {
                Game.log.Add("Equipped "+ selected.Definition.name + ".");
                Game.player.Equip(selected);

                Game.player.Pass();
            }
        }

        public static void Get(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            if(Game.logPlayerActions)
                Game.log.Add(" > Pick up item");

            int i = letterAnswerToIndex(answer[0]);
            
            List<Item> onTile = Util.ItemsOnTile(
                Game.player.xy
            );

            if (i < onTile.Count)
            {
                Item it = onTile[i];
                Game.worldItems.Remove(it);

                if (it.Definition.stacking)
                {
                    bool alreadyHolding = false;
                    foreach (Item item in Game.player.inventory)
                        if (item.Definition.type == it.Definition.type)
                        {
                            alreadyHolding = true;
                            item.count++;
                            //merging into the already held item
                            Game.allItems.Remove(it);
                        }
                    if (!alreadyHolding)
                        Game.player.inventory.Add(it);
                }
                else
                {
                    Game.player.inventory.Add(it);
                }
                Game.log.Add("Picked up " + it.Definition.name + ".");

                Game.player.Pass();
            }
            else
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }
        }

        public static void Open(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            if(Game.logPlayerActions)
                Game.log.Add(" > Open door");

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.doorState == Door.Closed)
            {
                t.doorState = Door.Open;
                Game.log.Add("You opened the door.");

                //counted as a movement action at the moment, based
                //on the dnd rules.
                Game.player.Pass(true);
            }
            else
            {
                Game.log.Add("There's no closed door there.");
            }
        }

        public static void Close(string answer)
        {
            Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            if(Game.logPlayerActions)
                Game.log.Add(" > Close door");

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.doorState == Door.Open)
            {
                //first check if something's in the way
                if (Util.ItemsOnTile(t).Count <= 0)
                {
                    t.doorState = Door.Closed;
                    Game.log.Add("You closed the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.player.Pass(true);
                }
                else
                {
                    Game.log.Add("There's something in the way.");
                }
            }
            else
            {
                Game.log.Add("There's no open door there.");
            }
        }

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
                Game.targeting = true;
                Game.target = Game.player.xy;
                Game.targetingReaction = Player.Cast;
            }
        }

        public static void Cast(Point p)
        {
            int index = letterAnswerToIndex(Game.qpAnswerStack.Pop()[0]);
            if (Game.map[p.x, p.y] != null)
                Game.player.Cast(
                    Game.player.Spellbook[index], p
                );
            else
                Game.log.Add("Invalid target.");
        }

        public static void PlayerInputHandling()
        {
            #region movement
            Point offset = new Point(0, 0);

            if (Game.KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
            if (Game.KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
            if (Game.KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
            if (Game.KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
            if (Game.KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
            if (Game.KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            if (Game.KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            if (Game.KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

            if (Game.KeyPressed(Keys.NumPad5)) Game.player.Pass(true);

            Tile target = Game.map[
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
                            "There is " +
                            Util.article(
                                itemsOnSquare[0].Definition.name
                            ) + " " + itemsOnSquare[0].Definition.name +
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

            #region drop
            if (Game.KeyPressed(Keys.D) && !Game.shift)
            {
                string _q = "Drop what? [";
                Game.acceptedInput.Clear();
                for (int i = 0; i < Game.player.inventory.Count; i++)
                {
                    char index = (char)(97 + i);
                    _q += index;
                    Game.acceptedInput.Add((int)(index + "").ToUpper()[0]);
                }
                _q += "]";
                Game.setupQuestionPrompt(_q);
                Game.questionPromptOpen = true;

                Game.questionReaction = Player.Drop;
            }
            #endregion

            #region open
            if (Game.KeyPressed(Keys.O) && !Game.shift)
            {
                Game.acceptedInput.Clear();
                Game.acceptedInput.AddRange(Game.directions);
                Game.setupQuestionPrompt("Open where?");
                Game.questionPromptOpen = true;
                Game.questionReaction = Player.Open;
            }
            #endregion

            #region close
            if (Game.KeyPressed(Keys.C) && !Game.shift)
            {
                Game.acceptedInput.Clear();
                Game.acceptedInput.AddRange(Game.directions);
                Game.setupQuestionPrompt("Close where?");
                Game.questionPromptOpen = true;
                Game.questionReaction = Player.Close;
            }
            #endregion

            #region wield/wear //AHAHAHA IM A GENIUS
            if (Game.KeyPressed(Keys.W))
            {
                List<Item> equipables = new List<Item>();

                foreach (Item it in Game.player.inventory)
                    //is it wieldable?
                    if (
                        it.Definition.equipSlots.Contains(DollSlot.Hand)
                        && !Game.shift
                    )
                        equipables.Add(it);
                    else if (
                        it.Definition.equipSlots.FindAll(
                            x => x != DollSlot.Hand
                        ).Count > 0 && Game.shift
                    )
                        equipables.Add(it);

                //remove items we've already equipped from the
                //list of potential items to equip
                foreach (BodyPart bp in Game.player.PaperDoll)
                    equipables.Remove(bp.Item);

                if (equipables.Count > 0)
                {
                    string _q = (Game.shift ? "Wear" : "Wield") + " what? [";
                    Game.acceptedInput.Clear();
                    foreach (Item it in equipables)
                    {
                        //show the character corresponding with the one
                        //shown in the inventory.
                        char index =
                            (char)(97 + Game.player.inventory.IndexOf(it));
                        _q += index;
                        Game.acceptedInput.Add((int)(index + "")
                            .ToUpper()[0]
                        );
                    }
                    _q += "]";
                    Game.setupQuestionPrompt(_q);
                    Game.questionPromptOpen = true;

                    Game.questionReaction = Player.Wield;
                }
                else
                {
                    Game.log.Add("Nothing to " +
                        (Game.shift ? "wear" : "wield") + ".");
                }
            }
            #endregion

            #region sheath
            if (Game.KeyPressed(Keys.S) && Game.shift)
            {
                List<Item> equipped = new List<Item>();
                foreach (
                    BodyPart bp in Game.player.PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand
                    )
                )
                {
                    if (bp.Item != null)
                        equipped.Add(bp.Item);
                }

                if (equipped.Count > 0)
                {
                    string _q = "Sheath what? [";
                    foreach (Item it in equipped)
                    {
                        char index =
                            (char)(97 +
                                Game.player.inventory.IndexOf(it)
                            );
                        _q += index;
                        Game.acceptedInput.Add((int)(index + "")
                            .ToUpper()[0]
                        );
                    }
                    _q += "]";
                    Game.setupQuestionPrompt(_q);
                    Game.questionPromptOpen = true;

                    Game.questionReaction = Player.Sheath;
                }
                else
                {
                    Game.log.Add("Nothing to sheath.");
                }
            }
            #endregion

            #region get
            if (
                (Game.KeyPressed(Keys.G) && !Game.shift) ||
                (Game.KeyPressed(Keys.OemComma) && !Game.shift)
            ) {
                List<Item> onFloor = Util.ItemsOnTile(Game.player.xy);
                if (onFloor.Count > 0)
                {
                    if (onFloor.Count > 1)
                    {
                        string _q = "Pick up what? [";
                        Game.acceptedInput.Clear();
                        for (int i = 0; i < onFloor.Count; i++)
                        {
                            char index = (char)(97 + i);
                            _q += index;
                            Game.acceptedInput.Add(
                                (int)(index + "").ToUpper()[0]
                            );
                        }
                        _q += "]";
                        Game.setupQuestionPrompt(_q);
                        Game.questionPromptOpen = true;

                        Game.acceptedInput.Clear();
                        Game.acceptedInput.AddRange(Game.letters);

                        Game.questionReaction = Player.Get;
                    }
                    else //remove me? just show the option for just "a"
                    {
                        Game.qpAnswerStack.Push("a");
                        Player.Get("a");
                    }
                }
            }
            #endregion
        }
    }
}
