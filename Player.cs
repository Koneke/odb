using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    public class Player
    {
        public static ODBGame Game;

        public static void PlayerInput()
        {
            if (Game.Player.HpCurrent <= 0) return;
            //should probably be moved into some 'input-preprocess',
            //since we do the same thing for brains
            if (Game.Player.HasEffect(StatusType.Stun))
            {
                Game.Player.Pass(ODBGame.StandardActionLength);
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

            CheckApply();
            CheckChant();
            CheckClose();
            CheckDrop();
            CheckEat();
            CheckEngrave();
            CheckFire();
            CheckGet();
            CheckLook();
            CheckOpen();
            CheckQuaff();
            CheckQuiver();
            CheckRead();
            CheckRemove();
            CheckSheath();
            CheckWield();
            CheckWear();
            CheckZap();
        }

        //return whether we moved or not
        private static bool MovementInput()
        {
            #region stairs

            bool descending =
                (KeyBindings.Pressed(Bind.Down) &&
                    Game.Level.At(Game.Player.xy).Stairs == Stairs.Down);
            bool ascending = 
                (KeyBindings.Pressed(Bind.Up) &&
                Game.Level.At(Game.Player.xy).Stairs == Stairs.Up);

            if (descending || ascending)
            {
                //there should always be a connector at the stairs,
                //so we assume there is one.
                LevelConnector connector = Game.Level.Connectors
                    .First(lc => lc.Position == Game.Player.xy);
                
                if(descending)
                    if (connector.Target == null)
                    {
                        Generator g = new Generator();
                        connector.Target = g.Generate(
                            Game.Level,
                            Game.Level.Depth + 1
                        );
                        Game.Levels.Add(connector.Target);
                    }

                if (connector.Target == null) return false;

                Game.SwitchLevel(connector.Target, true);
                Game.Log(
                    "You {1} the stairs...",
                    descending
                    ? "descend"
                    : "ascend"
                );
            }
            #endregion

            Point offset = new Point(0, 0);

                 if (KeyBindings.Pressed(Bind.North)) offset.Nudge(0, -1);
            else if (KeyBindings.Pressed(Bind.NorthEast)) offset.Nudge(1, -1);
            else if (KeyBindings.Pressed(Bind.East)) offset.Nudge(1, 0);
            else if (KeyBindings.Pressed(Bind.SouthEast)) offset.Nudge(1, 1);
            else if (KeyBindings.Pressed(Bind.South)) offset.Nudge(0, 1);
            else if (KeyBindings.Pressed(Bind.SouthWest)) offset.Nudge(-1, 1);
            else if (KeyBindings.Pressed(Bind.West)) offset.Nudge(-1, 0);
            else if (KeyBindings.Pressed(Bind.NorthWest)) offset.Nudge(-1, -1);

            if (KeyBindings.Pressed(Bind.Wait)) Game.Player.Pass(true);

            if (offset.x == 0 && offset.y == 0) return false;

            return Game.Player.TryMove(offset);
        }

        private static void CheckApply()
        {
            if (!KeyBindings.Pressed(Bind.Apply)) return;

            IO.AcceptedInput.Clear();
            IO.AcceptedInput.AddRange(
                Game.Player.Inventory
                    .Where(item => item.HasComponent<UsableComponent>())
                    .Select(item => Game.Player.Inventory.IndexOf(item))
                    .Select(index => IO.Indexes[index])
            );

            string question = "Use what? [";
            question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
            question += "]";

            if (IO.AcceptedInput.Count <= 0)
            {
                Game.Log("You don't have anything to apply or use.");
                return;
            }

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Use
            );
        }

        private static void CheckChant()
        {
            if (!KeyBindings.Pressed(Bind.Chant)) return;

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

        private static void CheckClose()
        {
            if (!KeyBindings.Pressed(Bind.Close)) return;

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

        private static void CheckDrop()
        {
            if (!KeyBindings.Pressed(Bind.Drop)) return;

            IO.AcceptedInput.Clear();
            for (int i = 0; i < Game.Player.Inventory.Count; i++)
                IO.AcceptedInput.Add(IO.Indexes[i]);

            if (IO.AcceptedInput.Count <= 0)
            {
                Game.Log("You have nothing you can drop.");
                return;
            }

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

        private static void CheckEat()
        {
            if (!KeyBindings.Pressed(Bind.Eat)) return;

            string question = "Eat what? [";
            IO.AcceptedInput.Clear();
            foreach (
                char index in
                    from item in Game.Player.Inventory
                    where item.HasComponent<EdibleComponent>()
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
            if (!KeyBindings.Pressed(Bind.Engrave)) return;

            if(Game.Level.At(Game.Player.xy).Stairs != Stairs.None)
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

        private static void CheckFire()
        {
            if (!KeyBindings.Pressed(Bind.Fire)) return;

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
                x => x.HasComponent<LauncherComponent>()))
            {
                weapon = Game.Player.GetEquippedItems()
                    .Where(x => x.HasComponent<LauncherComponent>())
                    .ToList()[0];
                lc = weapon.GetComponent<LauncherComponent>();
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

            List<Actor> visible =
                Game.Level.Actors
                .Where(a => a != Game.Player)
                .Where(a => Game.Player.Sees(a.xy))
                .ToList();

            if(visible.Count > 0)
                Game.Target =
                    visible
                    .OrderBy(a => Util.Distance(a.xy, Game.Player.xy))
                    .Select(a => a.xy)
                    .First();
        }

        private static void CheckGet()
        {
            if (!KeyBindings.Pressed(Bind.Get)) return;

            if (Game.Player.Inventory.Count >= InventoryManager.InventorySize)
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

        private static void CheckLook()
        {
            if (!KeyBindings.Pressed(Bind.Look)) return;

            IO.AskPlayer(
                "Examine what?",
                InputType.Targeting,
                PlayerResponses.Look
            );
        }

        private static void CheckOpen()
        {
            if (!KeyBindings.Pressed(Bind.Open)) return;

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

        private static void CheckQuaff()
        {
            if (!KeyBindings.Pressed(Bind.Quaff)) return;

            IO.AcceptedInput.Clear();
            foreach (int index in Game.Player.Inventory
                .Where(item => item.HasComponent<DrinkableComponent>())
                .Select(it => Game.Player.Inventory.IndexOf(it))
            ) {
                IO.AcceptedInput.Add(IO.Indexes[index]);
            }

            string question = "Drink what? [";
            question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
            question += "]";

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quaff
                );
            else Game.Log("You have nothing to drink.");

        }

        private static void CheckQuiver()
        {
            //todo: like mentioned in the checkwield comment
            //      might want to make a size check or something here.
            //      quivering an orc corpse sounds a bit so-so...
            if (!KeyBindings.Pressed(Bind.Quiver)) return;

            IO.AcceptedInput.Clear();
            foreach (int index in Game.Player.Inventory
                .Where(it => !Game.Player.IsEquipped(it))
                .Where(it => it != Game.Player.Quiver)
                .Select(it => Game.Player.Inventory.IndexOf(it))
            ) {
                IO.AcceptedInput.Add(IO.Indexes[index]);
            }

            string question = "Quiver what? [";
            question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
            question += "]";

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quiver
                );
            else Game.Log("You have nothing to quiver.");
        }

        private static void CheckRead()
        {
            if (!KeyBindings.Pressed(Bind.Read)) return;

            List<Item> readable = Game.Player.Inventory
                .Where(item =>
                    item.HasComponent<ReadableComponent>() ||
                    item.HasComponent<LearnableComponent>())
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

        private static void CheckRemove()
        {
            if (!KeyBindings.Pressed(Bind.Remove)) return;

            //LH-021214: Since we can (soon) wield anything, anything in your
            //           hands should be sheathable.
            List<Item> worn = (
                    from bp in Game.Player.PaperDoll
                    where bp.Item != null
                    where bp.Item.HasComponent<WearableComponent>()
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

        private static void CheckSheath()
        {
            if (!KeyBindings.Pressed(Bind.Sheath)) return;

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

                foreach(Item item in wielded)
                    IO.AcceptedInput.Add(IO.Indexes
                        [Game.Player.Inventory.IndexOf(item)]
                    );

                if(Game.Player.Quiver != null)
                    IO.AcceptedInput.Add(IO.Indexes
                        [Game.Player.Inventory.IndexOf(Game.Player.Quiver)]
                    );

                string question = "Sheath what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

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

        private static void CheckWield()
        {
            if (!KeyBindings.Pressed(Bind.Wield)) return;

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
            if (!KeyBindings.Pressed(Bind.Wear)) return;

            List<Item> wearable = Game.Player.Inventory
                .Where(x => !Game.Player.GetEquippedItems().Contains(x))
                .Where(item => item.HasComponent<WearableComponent>())
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

        private static void CheckZap()
        {
            if (!KeyBindings.Pressed(Bind.Zap)) return;

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

    }
}
