using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    public class Player
    {
        public static Game1 Game;

        public static void PlayerInput()
        {
            if (Game.Player.HpCurrent <= 0) return;
            //should probably be moved into some 'input-preprocess',
            //since we do the same thing for brains
            if (Game.Player.HasEffect(StatusType.Stun))
            {
                Game.Player.Pass(Game.StandardActionLength);
                return;
            }

            bool moved = MovementInput();

            //should be replaced with a look command
            //which could be called here maybe
            #region looking at our new tile
            if (moved)
            {
                Game.Target = Game.Player.xy;
                PlayerResponses.Examine(false);
            }
            #endregion

            CheckGet();
            CheckDrop();
            CheckWieldWear();
            CheckSheathRemove();
            CheckOpen();
            CheckClose();
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
            if (IO.KeyPressed(Keys.OemPeriod) && IO.Shift)
                if (Game.Level.Map[
                    Game.Player.xy.x,
                    Game.Player.xy.y].Stairs == Stairs.Down)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth + 1 <= Game.Levels.Count - 1)
                    {
                        Game.SwitchLevel(Game.Levels[depth + 1]);
                        Game.Log("You descend the stairs...");
                    }
                }

            if (IO.KeyPressed(Keys.OemComma) && IO.Shift)
                if (Game.Level.Map[
                    Game.Player.xy.x,
                    Game.Player.xy.y].Stairs == Stairs.Up)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth - 1 >= 0)
                    {
                        Game.SwitchLevel(Game.Levels[depth - 1]);
                        Game.Log("You ascend the stairs...");
                    }
                }
            #endregion

            Point offset = new Point(0, 0);

                 if (IO.KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
            else if (IO.KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
            else if (IO.KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
            else if (IO.KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
            else if (IO.KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
            else if (IO.KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            else if (IO.KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            else if (IO.KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

            if (IO.KeyPressed(Keys.NumPad5)) Game.Player.Pass(true);

            if (offset.x == 0 && offset.y == 0) return false;

            return Game.Player.TryMove(offset);
        }

        static void CheckGet()
        {
            if (
                (!IO.KeyPressed(Keys.G) || IO.Shift) &&
                (!IO.KeyPressed(Keys.OemComma) || IO.Shift)
            ) return;

            List<Item> onFloor = Game.Level.ItemsOnTile(Game.Player.xy);
            if (onFloor.Count > 1)
            {
                string question = "Pick up what? [";
                IO.AcceptedInput.Clear();
                for (int i = 0; i < onFloor.Count; i++)
                {
                    char index = IO.Indexes[i];
                    question += index;
                    IO.AcceptedInput.Add(IO.Indexes[i]);
                }
                question += "]";

                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Get
                );
            }
            //just more convenient this way
            else if (onFloor.Count > 0)
            {
                Game.QpAnswerStack.Push("a");
                PlayerResponses.Get();
            }
        }

        static void CheckDrop()
        {
            if (!IO.KeyPressed(Keys.D) || IO.Shift) return;

            string question = "Drop what? [";
            IO.AcceptedInput.Clear();
            for (int i = 0; i < Game.Player.Inventory.Count; i++)
            {
                char index = IO.Indexes[i];
                question += index;
                IO.AcceptedInput.Add(IO.Indexes[i]);
            }
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Drop
            );
        }

        static void CheckWieldWear()
        {
            //todo: in reality, shouldn't in essence everything be wieldable?
            //      just like everything is quiverable? like, wielding just
            //      requires you to hold the item in your hand really.
            //      might want to make a strength/size check or something, but
            //      that really should be about it.
            bool wield = IO.KeyPressed(Keys.W) && !IO.Shift;
            bool wear = IO.KeyPressed(Keys.W) && IO.Shift;

            if (!wield && !wear) return;

            List<Item> equipables =
                wield ?
                    Game.Player.Inventory
                        .FindAll(x => x.EquipSlots.Contains(DollSlot.Hand))
                        .Where(x => x.EquipSlots.Count > 0)
                        .ToList() :
                    Game.Player.Inventory
                        .FindAll(x => !x.EquipSlots.Contains(DollSlot.Hand))
                        .Where(x => x.EquipSlots.Count > 0)
                        .ToList();

            //remove items we've already equipped from the
            //list of potential items to equip
            foreach (Item item in Game.Player.GetEquippedItems())
                equipables.Remove(item);

            if (equipables.Count > 0)
            {
                string question =
                    (IO.Shift ? "Wear" : "Wield") + " what? [";

                IO.AcceptedInput.Clear();
                foreach (char index in equipables
                        .Select(item => IO.Indexes
                            [Game.Player.Inventory.IndexOf(item)])
                    ) {
                        question += index;
                        IO.AcceptedInput.Add(index);
                    }

                question += "]";

                if(wield)
                    IO.AskPlayer(
                        question,
                        InputType.QuestionPromptSingle,
                        PlayerResponses.Wield
                    );
                else
                    IO.AskPlayer(
                        question,
                        InputType.QuestionPromptSingle,
                        PlayerResponses.Wear
                    );
            }
            else Game.Log("Nothing to " + (wield ? "wield" : "wear") + ".");
        }

        static void CheckSheathRemove()
        {
            bool sheath = IO.KeyPressed(Keys.S) && IO.Shift;
            bool remove = IO.KeyPressed(Keys.R) && IO.Shift;
            if (!sheath && !remove) return;

            List<Item> equipped;

            if(sheath)
                equipped = (
                    from bp in Game.Player.PaperDoll
                    where bp.Item != null
                    where bp.Item.EquipSlots.Contains(DollSlot.Hand)
                    select bp.Item
                ).ToList();
            else
                equipped = (
                    from bp in Game.Player.PaperDoll
                    where bp.Item != null
                    where bp.Item.EquipSlots.All(x => x != DollSlot.Hand)
                    select bp.Item
                ).ToList();

            if (equipped.Count > 0)
            {
                string question = 
                    (sheath ? "Sheath" : "Remove") + " what? [";
                IO.AcceptedInput.Clear();

                foreach (char index in equipped
                    .Select(it => IO.Indexes[Game.Player.Inventory.IndexOf(it)])
                    .Where(index => !IO.AcceptedInput.Contains(index))
                ) {
                    question += index;
                    IO.AcceptedInput.Add(index);
                }

                question += "]";

                if(sheath)
                    IO.AskPlayer(
                        question,
                        InputType.QuestionPromptSingle,
                        PlayerResponses.Sheath
                    );
                else
                    IO.AskPlayer(
                        question,
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

        static void CheckOpen()
        {
            if (!IO.KeyPressed(Keys.O) || IO.Shift) return;

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

        private static void CheckClose()
        {
            if (!IO.KeyPressed(Keys.C) || IO.Shift) return;

            IO.AcceptedInput.Clear();

            for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                IO.AcceptedInput.Add((char)(48 + i - Keys.NumPad0));
                
            IO.AskPlayer(
                "Close where?",
                InputType.QuestionPromptSingle,
                PlayerResponses.Close
            );

        }


        static void CheckZap()
        {
            if (!IO.KeyPressed(Keys.Z) || IO.Shift) return;

            string question = "Cast what? [";
            IO.AcceptedInput.Clear();

            for (int i = 0; i < Game.Player.Spellbook.Count; i++)
            {
                char index = IO.Indexes[i];
                question += index;
                IO.AcceptedInput.Add(index);
            }

            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Zap
            );
        }

        static void CheckApply()
        {
            if (!IO.KeyPressed(Keys.A) || IO.Shift) return;

            string question = "Use what? [";

            IO.AcceptedInput.Clear();
            for (int i = 0; i < Game.Player.Inventory.Count; i++)
            {
                if (Game.Player.Inventory[i].UseEffect == null) continue;

                char index = IO.Indexes[i];
                question += index;
                IO.AcceptedInput.Add(index);
            }

            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Use
            );
        }

        static void CheckFire()
        {
            if (!IO.KeyPressed(Keys.F) || IO.Shift) return;

            //todo: throwing
            //      if we pressed T instead, allow items in hands as ammo
            Item ammo = Game.Player.Quiver;

            if (ammo == null)
            {
                Game.Log("You need something to fire.");
                return;
            }

            //todo: notice! this only finds the FIRST weapon
            //      with this type of ammo! theoretically, you CAN have
            //      e.g. two bows wielded at once, if you have enough hands
            //      (and considering the genre of game, "2" is not an obvious
            //      answer to that question).
            //      for now, just get first one, if it becomes an issue the
            //      player will have to work around it.
            Item weapon = Game.Player.GetEquippedItems()
                .Find(item => item.AmmoTypes.Contains(ammo.Type));

            bool throwing;

            //weapon and appropriate ammo?
            //todo: get all items in hands
            //      check if any one of them has a matching ammo type to
            //      quiver. that one is then our weapon used to fire.
            if (weapon != null)
                throwing = !weapon.Definition.AmmoTypes.Contains(ammo.Type);
                //just some sort of ammo
            else throwing = true;

            string question = String.Format(
                "{0} your {1}",
                throwing ? "Throwing" : "Firing",
                throwing ? ammo.Definition.Name : weapon.Definition.Name
            );

            IO.AskPlayer(
                question,
                InputType.Targeting,
                PlayerResponses.Fire
            );
        }

        static void CheckQuiver()
        {
            //todo: like mentioned in the checkwield comment
            //      might want to make a size check or something here.
            //      quivering an orc corpse sounds a bit so-so...
            if (!IO.KeyPressed(Keys.Q) || !IO.Shift) return;

            string question = "Quiver what? [";
                
            IO.AcceptedInput.Clear();
            foreach (
                char index in Game.Player.Inventory
                    .Where(it => !Game.Player.IsEquipped(it))
                    .Select(it => IO.Indexes[Game.Player.Inventory.IndexOf(it)])
                ) {
                    question += index;
                    IO.AcceptedInput.Add(index);
                }

            question += "]";

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quiver
                );
            else Game.Log("You have nothing to quiver.");
        }

        static void CheckLook()
        {
            if (IO.KeyPressed(Keys.OemSemicolon) && IO.Shift)
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
            if (!IO.KeyPressed(Keys.E) || IO.Shift) return;

            string question = "Eat what? [";
            IO.AcceptedInput.Clear();
            foreach (
                char index in
                    from item in Game.Player.Inventory
                    where item.Definition.Nutrition > 0
                select IO.Indexes[Game.Player.Inventory.IndexOf(item)]
                ) {
                    question += index;
                    IO.AcceptedInput.Add(index);
                }
            question += "]";

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Eat
                    );
            else Game.Log("You have nothing to eat.");
        }
    }
}
