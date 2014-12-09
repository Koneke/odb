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
                PlayerResponses.Examine();
            }
            #endregion

            CheckGet();
            CheckDrop();
            CheckWield();
            CheckWear();
            CheckSheath();
            CheckRemove();
            CheckOpen();
            CheckClose();
            CheckZap();
            CheckApply();
            CheckFire();
            CheckQuiver();
            CheckLook();
            CheckEat();
            CheckEngrave();
            CheckChant();
            CheckRead();
        }

        //return whether we moved or not
        private static bool MovementInput()
        {
            #region stairs
            if (IO.KeyPressed(Keys.OemPeriod) && IO.ShiftState)
                if (Game.Level.Map[
                    Game.Player.xy.x,
                    Game.Player.xy.y].Stairs == Stairs.Down)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth + 1 > Game.Levels.Count - 1)
                    {
                        Generator g = new Generator();
                        Game.Levels.Add(g.Generate(depth + 1));
                    }
                    Game.SwitchLevel(Game.Levels[depth + 1], true);
                    Game.Log("You descend the stairs...");
                }

            if (IO.KeyPressed(Keys.OemComma) && IO.ShiftState)
                if (Game.Level.Map[
                    Game.Player.xy.x,
                    Game.Player.xy.y].Stairs == Stairs.Up)
                {
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth - 1 >= 0)
                    {
                        Game.SwitchLevel(Game.Levels[depth - 1], true);
                        Game.Log("You ascend the stairs...");
                    }
                }
            #endregion

            Point offset = new Point(0, 0);

                 if (IO.KeyPressed(IO.Input.North)) offset.Nudge(0, -1);
            else if (IO.KeyPressed(IO.Input.NorthEast)) offset.Nudge(1, -1);
            else if (IO.KeyPressed(IO.Input.East)) offset.Nudge(1, 0);
            else if (IO.KeyPressed(IO.Input.SouthEast)) offset.Nudge(1, 1);
            else if (IO.KeyPressed(IO.Input.South)) offset.Nudge(0, 1);
            else if (IO.KeyPressed(IO.Input.SouthWest)) offset.Nudge(-1, 1);
            else if (IO.KeyPressed(IO.Input.West)) offset.Nudge(-1, 0);
            else if (IO.KeyPressed(IO.Input.NorthWest)) offset.Nudge(-1, -1);

            if (IO.KeyPressed(Keys.NumPad5)) Game.Player.Pass(true);

            if (offset.x == 0 && offset.y == 0) return false;

            return Game.Player.TryMove(offset);
        }

        private static void CheckGet()
        {
            if (
                (!IO.KeyPressed(Keys.G) || IO.ShiftState) &&
                (!IO.KeyPressed(Keys.OemComma) || IO.ShiftState)
            ) return;

            if (Game.Player.Inventory.Count >= 23)
            {
                Game.Log("You are carrying too much!");
                return;
            }

            List<Item> onFloor = Game.Level.ItemsOnTile(Game.Player.xy);
            if (onFloor.Count > 1)
            {
                IO.AcceptedInput.Clear();
                for (int i = 0; i < onFloor.Count; i++)
                    IO.AcceptedInput.Add(IO.Indexes[i]);

                string question = "Pick up what? [";
                question += IO.AcceptedInput.Aggregate(
                    "", (c, n) => c + n);
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

        private static void CheckDrop()
        {
            if (!IO.KeyPressed(Keys.D) || IO.ShiftState) return;

            IO.AcceptedInput.Clear();
            for (int i = 0; i < Game.Player.Inventory.Count; i++)
                IO.AcceptedInput.Add(IO.Indexes[i]);

            string question = "Drop what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Drop
            );
        }

        private static void CheckWield()
        {
            if (!(IO.KeyPressed(Keys.W) && !IO.ShiftState)) return;

            List<Item> wieldable = Game.Player.Inventory
                .Where(x => !Game.Player.GetEquippedItems().Contains(x))
                .ToList();

            if(wieldable.Count <= 0)
            {
                Game.Log("You have nothing to wield.");
                return;
            }

            IO.AcceptedInput.Clear();
            foreach(Item item in wieldable)
                IO.AcceptedInput.Add(IO.Indexes
                    [Game.Player.Inventory.IndexOf(item)]
                );

            string question = "Wield what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Wield
            );
        }

        private static void CheckWear()
        {
            if (!(IO.KeyPressed(Keys.W) && IO.ShiftState)) return;

            List<Item> wearable = Game.Player.Inventory
                .Where(x => !Game.Player.GetEquippedItems().Contains(x))
                .Where(item => item.Definition.HasComponent("cWearable"))
                .ToList();

            if (wearable.Count <= 0)
            {
                Game.Log("You have nothing to wear.");
                return;
            }

            IO.AcceptedInput.Clear();
            foreach(Item item in wearable)
                IO.AcceptedInput.Add(
                    IO.Indexes[Game.Player.Inventory.IndexOf(item)]);

            string question = "Wear what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Wear
            );
        }

        private static void CheckSheath()
        {
            if (!(IO.KeyPressed(Keys.S) && IO.ShiftState)) return;

            //LH-021214: Since we can (soon) wield anything, anything in your
            //           hands should be sheathable.
            List<Item> wielded = (
                    from bp in Game.Player.PaperDoll
                    where bp.Item != null
                    where bp.Type == DollSlot.Hand
                    select bp.Item
                ).Distinct().ToList();

            if (wielded.Count > 0 || Game.Player.Quiver != null)
            {
                IO.AcceptedInput.Clear();
                for(int i = 0; i < wielded.Count; i++)
                    IO.AcceptedInput.Add(IO.Indexes[i]);

                if(Game.Player.Quiver != null)
                    IO.AcceptedInput.Add(
                        IO.Indexes
                        [Game.Player.Inventory.IndexOf(Game.Player.Quiver)]
                    );

                string question = "Sheath what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "] ";

                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Sheath
                );
            }
            else
            {
                Game.Log("You don't have anything quivered or wielded!");
            }
        }

        private static void CheckRemove()
        {
            if (!(IO.KeyPressed(Keys.R) && IO.ShiftState)) return;

            //LH-021214: Since we can (soon) wield anything, anything in your
            //           hands should be sheathable.
            List<Item> worn = (
                    from bp in Game.Player.PaperDoll
                    where bp.Item != null
                    where bp.Item.HasComponent("cWearable")
                    select bp.Item
                ).ToList();

            if (worn.Count > 0)
            {
                IO.AcceptedInput.Clear();
                for (int i = 0; i < worn.Count; i ++)
                    IO.AcceptedInput.Add(IO.Indexes[i]);

                string question = "Remove what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "] ";

                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Remove
                );
            }
            else
            {
                Game.Log("You have nothing to remove, you shameless beast!");
            }
        }

        private static void CheckOpen()
        {
            if (!IO.KeyPressed(Keys.O) || IO.ShiftState) return;

            IO.AcceptedInput.Clear();

            for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                IO.AcceptedInput.Add((char)(48 + i - Keys.NumPad0));
            IO.AcceptedInput.AddRange(IO.ViKeys.ToCharArray());
                
            IO.AskPlayer(
                "Open where?",
                InputType.QuestionPromptSingle,
                PlayerResponses.Open
            );
        }

        private static void CheckClose()
        {
            if (!IO.KeyPressed(Keys.C) || IO.ShiftState) return;

            IO.AcceptedInput.Clear();

            for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                IO.AcceptedInput.Add((char)(48 + i - Keys.NumPad0));
            IO.AcceptedInput.AddRange(IO.ViKeys.ToCharArray());
                
            IO.AskPlayer(
                "Close where?",
                InputType.QuestionPromptSingle,
                PlayerResponses.Close
            );
        }

        private static void CheckZap()
        {
            if (!IO.KeyPressed(Keys.Z) || IO.ShiftState) return;

            IO.AcceptedInput.Clear();
            for (int i = 0; i < Game.Player.Spellbook.Count; i++)
                IO.AcceptedInput.Add(IO.Indexes[i]);

            if (IO.AcceptedInput.Count <= 0)
            {
                Game.Log("You don't know any spells.");
                return;
            }

            string question = "Cast what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Zap
            );
        }

        private static void CheckApply()
        {
            if (!IO.KeyPressed(Keys.A) || IO.ShiftState) return;

            IO.AcceptedInput.Clear();
            IO.AcceptedInput.AddRange(
                Game.Player.Inventory
                    .Where(item => item.HasComponent("cUsable"))
                    .Select(item => Game.Player.Inventory.IndexOf(item))
                    .Select(index => IO.Indexes[index])
            );

            string question = "Use what? [";
            question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
            question += "]";

            if (IO.AcceptedInput.Count <= 0)
            {
                Game.Log("You don't know any spells.");
                return;
            }

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Use
            );
        }

        private static void CheckFire()
        {
            if (!IO.KeyPressed(Keys.F) || IO.ShiftState) return;

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

            Item weapon = null;
            LauncherComponent lc = null;

            if (Game.Player.GetEquippedItems().Any(
                x => x.HasComponent("cLauncher")))
            {
                weapon = Game.Player.GetEquippedItems()
                    .Where(x => x.HasComponent("cLauncher"))
                    .ToList()[0];
                lc = (LauncherComponent)
                    weapon.GetComponent("cLauncher");
            }

            bool throwing;

            //weapon and appropriate ammo?
            //todo: get all items in hands
            //      check if any one of them has a matching ammo type to
            //      quiver. that one is then our weapon used to fire.
            if (weapon != null && lc != null)
                throwing = !lc.AmmoTypes.Contains(ammo.Type);
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

        private static void CheckQuiver()
        {
            //todo: like mentioned in the checkwield comment
            //      might want to make a size check or something here.
            //      quivering an orc corpse sounds a bit so-so...
            if (!IO.KeyPressed(Keys.Q) || !IO.ShiftState) return;

            IO.AcceptedInput.Clear();
            foreach (int index in Game.Player.Inventory
                .Where(it => !Game.Player.IsEquipped(it))
                .Where(it => it != Game.Player.Quiver)
                .Select(it => Game.Player.Inventory.IndexOf(it))
            ) {
                IO.AcceptedInput.Add(IO.Indexes[index]);
            }

            string question = "Quiver what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quiver
                );
            else Game.Log("You have nothing to quiver.");
        }

        private static void CheckLook()
        {
            if (IO.KeyPressed(Keys.OemSemicolon) && IO.ShiftState)
            {
                IO.AskPlayer(
                    "Examine what?",
                    InputType.Targeting,
                    PlayerResponses.Look
                );
            }
        }

        private static void CheckEat()
        {
            if (!IO.KeyPressed(Keys.E) || IO.ShiftState) return;

            string question = "Eat what? [";
            IO.AcceptedInput.Clear();
            foreach (
                char index in
                    from item in Game.Player.Inventory
                    where item.HasComponent("cEdible")
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

        private static void CheckEngrave()
        {
            if (!IO.KeyPressed(Keys.E) || !IO.ShiftState) return;

            if (Game.Level.TileAt(Game.Player.xy).Stairs != Stairs.None)
            {
                Game.Log("You can't engrave here.");
                return;
            }

            const string question = "Engrave what?";
            IO.AcceptedInput.Clear();
            IO.AcceptedInput.AddRange(IO.Indexes.ToCharArray());

            IO.AskPlayer(
                question,
                InputType.QuestionPrompt,
                PlayerResponses.Engrave
            );
        }

        private static void CheckChant()
        {
            if (!IO.KeyPressed(Keys.C) || !IO.ShiftState) return;

            const string question = "Chant what?";
            IO.AcceptedInput.Clear();
            IO.AcceptedInput.AddRange(IO.Indexes.ToCharArray());
            IO.AcceptedInput.Add(' ');

            IO.AskPlayer(
                question,
                InputType.QuestionPrompt,
                PlayerResponses.Chant
            );
        }

        private static void CheckRead()
        {
            if (!IO.KeyPressed(Keys.R) || IO.ShiftState) return;

            List<Item> readable = Game.Player.Inventory
                .Where(item =>
                    item.HasComponent(ReadableComponent.Type) ||
                    item.HasComponent(LearnableComponent.Type))
                .ToList();

            if (readable.Count <= 0)
            {
                Game.Log("You have nothing to read.");
                return;
            }

            IO.AcceptedInput.Clear();
            IO.AcceptedInput.AddRange(
                readable
                .Select(item => Game.Player.Inventory.IndexOf(item))
                .Select(index => IO.Indexes[index])
            );

            string question = "Read what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Read
            );
        }
    }
}
