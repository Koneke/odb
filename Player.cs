using System.Collections.Generic;

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

        public static void PlayerInput()
        {
            if (Game.player.hpCurrent <= 0) return;

            bool moved = MovementInput();

            //should be replaced with a look command
            //which could be called here maybe
            #region looking at our new tile
            if (moved)
            {
                Game.Target = Game.player.xy;
                PlayerResponses.Examine(false);
            }
            #endregion

            CheckGet();
            CheckDrop();
            CheckWieldWear();
            CheckSheathRemove();
            CheckDoors();
            CheckZap();
            CheckApply();
            CheckFire();
            CheckQuiver();
            CheckLook();
            CheckEat();
        }

        //return whether we moved or not
        static bool MovementInput()
        {
            #region stairs
            if (IO.KeyPressed(Keys.OemPeriod) && IO.shift)
                if (Game.Level.Map[
                    Game.player.xy.x,
                    Game.player.xy.y].Stairs == Stairs.Down)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth + 1 <= Game.Levels.Count - 1)
                    {
                        Game.SwitchLevel(Game.Levels[depth + 1]);
                        Game.Log("You descend the stairs...");
                    }
                }

            if (IO.KeyPressed(Keys.OemComma) && IO.shift)
                if (Game.Level.Map[
                    Game.player.xy.x,
                    Game.player.xy.y].Stairs == Stairs.Up)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth - 1 >= 0)
                    {
                        Game.SwitchLevel(Game.Levels[depth - 1]);
                        Game.Log("You ascend the stairs...");
                    }
                }
            #endregion

            bool moved = false;
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
                moved = Game.player.TryMove(offset);

            return moved;
        }

        static void CheckGet()
        {
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
                        PlayerResponses.Get
                    );
                }
                //just more convenient this way
                else if (onFloor.Count > 0)
                {
                    Game.qpAnswerStack.Push("a");
                    PlayerResponses.Get();
                }
            }
        }

        static void CheckDrop()
        {
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
                    PlayerResponses.Drop
                );
            }
        }

        static void CheckWieldWear()
        {
            bool wield = IO.KeyPressed(Keys.W) && !IO.shift;
            bool wear = IO.KeyPressed(Keys.W) && IO.shift;

            if (wield || wear)
            {
                List<Item> equipables = new List<Item>();

                foreach (Item it in Game.player.inventory)
                {
                    if (wield && it.equipSlots.Contains(DollSlot.Hand))
                        equipables.Add(it);
                    else if (
                        wear &&
                        !it.equipSlots.Contains(DollSlot.Hand) &&
                        !it.equipSlots.Contains(DollSlot.Quiver)
                    )
                        equipables.Add(it);
                }

                //remove items we've already equipped from the
                //list of potential items to equip
                foreach (Item item in Game.player.GetEquippedItems())
                    equipables.Remove(item);

                if (equipables.Count > 0)
                {
                    string _q = (IO.shift ? "Wear" : "Wield") + " what? [";
                    IO.AcceptedInput.Clear();
                    foreach (Item it in equipables)
                    {
                        char index = IO.indexes[
                            Game.player.inventory.IndexOf(it)
                        ];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                    _q += "]";

                    if(wield)
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            PlayerResponses.Wield
                        );
                    else
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            PlayerResponses.Wear
                        );
                }
                else
                {
                    Game.Log("Nothing to " +
                        (wield ? "wield" : "wear") + ".");
                }
            }

        }

        static void CheckSheathRemove()
        {
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
                    string _q = (sheath ? "Sheath" : "Remove")+" what? [";
                    IO.AcceptedInput.Clear();
                    foreach (Item it in equipped)
                    {
                        char index = IO.indexes[
                            Game.player.inventory.IndexOf(it)
                        ];
                        if (!IO.AcceptedInput.Contains(index))
                        {
                            _q += index;
                            IO.AcceptedInput.Add(index);
                        }
                    }
                    _q += "]";

                    if(sheath)
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            PlayerResponses.Sheath
                        );
                    else
                        IO.AskPlayer(
                            _q,
                            InputType.QuestionPromptSingle,
                            PlayerResponses.Remove
                        );
                }
                else
                {
                    Game.Log("Nothing to " +
                        (sheath ? "sheath." : "remove.")
                    );
                }
            }
        }

        static void CheckDoors()
        {
            #region open/close
            if (IO.KeyPressed(Keys.O) && !IO.shift)
            {
                IO.AcceptedInput.Clear();

                for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                    IO.AcceptedInput.Add(
                        (char)(48 + i - Keys.NumPad0)
                    );
                
                IO.AskPlayer(
                    "Open where?",
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Open
                );
            }

            if (IO.KeyPressed(Keys.C) && !IO.shift)
            {
                IO.AcceptedInput.Clear();

                for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                    IO.AcceptedInput.Add(
                        (char)(48 + i - Keys.NumPad0)
                    );
                
                IO.AskPlayer(
                    "Close where?",
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Close
                );
            }
            #endregion

        }

        static void CheckZap()
        {
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
                    PlayerResponses.Zap
                );
            }
        }

        static void CheckApply()
        {
            if (IO.KeyPressed(Keys.A) && !IO.shift)
            {
                string _q = "Use what? [";
                IO.AcceptedInput.Clear();
                for (int i = 0; i < Game.player.inventory.Count; i++)
                {
                    if (
                        Game.player.inventory[i].UseEffect != null
                    ) {
                        char index = IO.indexes[i];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                }
                _q += "]";

                IO.AskPlayer(
                    _q,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Use
                );
            }
        }

        static void CheckFire()
        {
            if (IO.KeyPressed(Keys.F) && !IO.shift)
            {
                Item weapon = null;
                Item ammo = null;

                foreach (Item it in Game.player.GetEquippedItems())
                    if (it.Definition.Ranged)
                        weapon = it;

                //future: if we pressed T instead, allow items in hands as
                //ammo
                foreach (BodyPart bp in Game.player.GetSlots(DollSlot.Quiver))
                {
                    if (bp.Item == null) continue;
                    //for now, only allowing one item at a time in the quiver
                    ammo = bp.Item; break;
                }

                if (ammo == null)
                {
                    Game.Log("You need something to fire.");
                    return;
                }

                bool throwing = false;
                if (!(ammo == null))
                {
                    //weapon and appropriate ammo
                    if (weapon != null)
                    {
                        if (weapon.Definition.AmmoTypes.Contains(ammo.type))
                            throwing = false;
                        else throwing = true;
                    }
                    //ammo
                    else throwing = true;
                }

                if (!throwing && weapon == null)
                {
                    Game.Log("You need something to fire with.");
                    return;
                }

                string _q;
                if(throwing)
                    _q = "Throwing your " + ammo.Definition.name;
                else
                    _q = "Firing your " + weapon.Definition.name;

                IO.AskPlayer(
                    _q,
                    InputType.Targeting,
                    PlayerResponses.Fire
                );
            }
        }

        static void CheckQuiver()
        {
            if (IO.KeyPressed(Keys.Q) && IO.shift)
            {
                string _q = "Quiver what? [";
                
                IO.AcceptedInput.Clear();
                foreach (Item it in Game.player.inventory)
                {
                    if (it.Definition.equipSlots.Contains(DollSlot.Quiver))
                    {
                        char index = IO.indexes[
                            Game.player.inventory.IndexOf(it)
                        ];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                }
                _q += "]";

                if (IO.AcceptedInput.Count > 0)
                    IO.AskPlayer(
                        _q,
                        InputType.QuestionPromptSingle,
                        PlayerResponses.Quiver
                    );
                else Game.Log("You have nothing to quiver.");
            }
        }

        static void CheckLook()
        {
            if (IO.KeyPressed(Keys.OemSemicolon) && IO.shift)
            {
                IO.AskPlayer(
                    "Examine what?",
                    InputType.Targeting,
                    PlayerResponses.Examine
                );
            }
        }

        static void CheckEat()
        {
            if (IO.KeyPressed(Keys.E) && !IO.shift)
            {
                string _q = "Eat what? [";
                IO.AcceptedInput.Clear();
                foreach(Item it in Game.player.inventory) {
                    if (it.Definition.Nutrition > 0)
                    {
                        char index = IO.indexes[
                            Game.player.inventory.IndexOf(it)
                        ];
                        _q += index;
                        IO.AcceptedInput.Add(index);
                    }
                }
                _q += "]";

                if (IO.AcceptedInput.Count > 0)
                    IO.AskPlayer(
                        _q,
                        InputType.QuestionPromptSingle,
                        PlayerResponses.Eat
                    );
                else Game.Log("You have nothing to eat.");
            }
        }

    }
}
