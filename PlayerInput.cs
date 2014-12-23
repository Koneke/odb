using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    public class PlayerInput
    {
        //public static ODBGame Game;

        public static void HandlePlayerInput()
        {
            MovementInput();

            if (KeyBindings.Pressed(Bind.Inventory) && !Game.WizMode)
                IO.IOState = InputType.Inventory;

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
            CheckSheathe();
            CheckSleep();
            CheckSneak();
            CheckWield();
            CheckWear();
            CheckZap();
        }

        private static void CheckSneak()
        {
            if (!KeyBindings.Pressed(Bind.Sneak)) return;

            if (Game.Player.HasEffect(StatusType.Sneak))
            {
                Game.UI.Log("You stop sneaking.");
                Game.Player.RemoveEffect(StatusType.Sneak);
            }
            else
            {
                Game.UI.Log("You start sneaking.");
                Game.Player.AddEffect(StatusType.Sneak, -1);
            }
        }

        private static void MovementInput()
        {
            Direction? direction = null;
            if (KeyBindings.Pressed(Bind.North))
                direction = Direction.North;
            else if (KeyBindings.Pressed(Bind.NorthEast))
                direction = Direction.NorthEast;
            else if (KeyBindings.Pressed(Bind.East))
                direction = Direction.East;
            else if (KeyBindings.Pressed(Bind.SouthEast))
                direction = Direction.SouthEast;
            else if (KeyBindings.Pressed(Bind.South))
                direction = Direction.South;
            else if (KeyBindings.Pressed(Bind.SouthWest))
                direction = Direction.SouthWest;
            else if (KeyBindings.Pressed(Bind.West))
                direction = Direction.West;
            else if (KeyBindings.Pressed(Bind.NorthWest))
                direction = Direction.NorthWest;
            else if (KeyBindings.Pressed(Bind.Up))
                direction = Direction.Up;
            else if (KeyBindings.Pressed(Bind.Down))
                direction = Direction.Down;

            if (KeyBindings.Pressed(Bind.Wait))
                //even if we're slowed, just pass a standard?
                Game.Player.Pass(Game.StandardActionLength);

            if(direction != null)
                Game.Player.Do(
                    new Command("Move")
                    .Add("Direction", direction)
                );
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
                Game.UI.Log("You don't have anything to apply or use.");
                return;
            }

            IO.CurrentCommand = new Command("use");

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
                Game.UI.Log("You have nothing you can drop.");
                return;
            }

            IO.CurrentCommand = new Command("drop");

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
                select IO.Indexes
                    [Game.Player.Inventory.IndexOf(item)]
                ) {
                    question += index;
                    IO.AcceptedInput.Add(index);
                }
            question += "]";

            IO.CurrentCommand = new Command("eat");

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Eat
                );
            else Game.UI.Log("You have nothing to eat.");
        }

        private static void CheckEngrave()
        {
            if (!KeyBindings.Pressed(Bind.Engrave)) return;

            if(
                World.Level.At(Game.Player.xy)
                    .Stairs != Stairs.None &&
                World.Level.At(Game.Player.xy)
                    .Door != Door.None
            ) {
                Game.UI.Log("You can't engrave here.");
                return;
            }

            IO.SetInput(IO.Indexes, ' ');
            IO.CurrentCommand = new Command("engrave");

            IO.AskPlayer(
                "Engrave what?",
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
                Game.UI.Log("You need something to fire.");
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
                throwing = !lc.AmmoTypes.Contains(ammo.ItemType);
                //just some sort of ammo
            else throwing = true;

            string question = String.Format(
                "{0} your {1}",
                throwing ? "Throwing" : "Firing",
                throwing ? ammo.Definition.Name : weapon.Definition.Name
            );

            IO.CurrentCommand = new Command("shoot");

            IO.AskPlayer(
                question,
                InputType.Targeting,
                PlayerResponses.Fire
            );

            Util.QuickTarget();
        }

        private static void CheckGet()
        {
            if (!KeyBindings.Pressed(Bind.Get)) return;

            if (Game.Player.Inventory.Count >= InventoryManager.InventorySize)
            {
                Game.UI.Log("You are carrying too much!");
                return;
            }

            List<Item> onFloor = World.Level.ItemsOnTile(Game.Player.xy);
            if (onFloor.Count > 1)
            {
                IO.AcceptedInput.Clear();
                for (int i = 0; i < onFloor.Count; i++)
                    IO.AcceptedInput.Add(IO.Indexes[i]);

                string question = "Pick up what? [";
                question += IO.AcceptedInput.Aggregate(
                    "", (c, n) => c + n);
                question += "]";

                IO.CurrentCommand = new Command("get");

                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Get
                );
            }
            //just more convenient this way
            else if (onFloor.Count > 0)
                Game.Player.Do(new Command("get").Add("item", onFloor[0]));
        }

        private static void CheckLook()
        {
            if (!KeyBindings.Pressed(Bind.Look)) return;

            IO.AskPlayer(
                "Examine what?",
                InputType.Targeting,
                PlayerResponses.Look
            );

            Util.QuickTarget();
        }

        private static void CheckOpen()
        {
            if (!KeyBindings.Pressed(Bind.Open)) return;

            IO.AcceptedInput.Clear();

            for(int i = (int)Keys.NumPad1; i <= (int)Keys.NumPad9; i++)
                IO.AcceptedInput.Add((char)(48 + i - Keys.NumPad0));
            IO.AcceptedInput.AddRange(IO.ViKeys.ToCharArray());

            IO.CurrentCommand = new Command("open");
                
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

            IO.CurrentCommand = new Command("quaff");

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quaff
                );
            else Game.UI.Log("You have nothing to drink.");

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

            IO.CurrentCommand = new Command("quiver");

            if (IO.AcceptedInput.Count > 0)
                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Quiver
                );
            else Game.UI.Log("You have nothing to quiver.");
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
                Game.UI.Log("You have nothing to read.");
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

            IO.CurrentCommand = new Command("read");

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Read
            );
        }

        private static void CheckRemove()
        {
            if (!KeyBindings.Pressed(Bind.Remove)) return;

            List<Item> worn = Game.Player.GetWornItems();

            if (worn.Count > 0)
            {
                IO.AcceptedInput.Clear();
                foreach(Item item in Game.Player.GetWornItems())
                    IO.AcceptedInput.Add(IO.Indexes
                        [Game.Player.Inventory.IndexOf(item)]
                    );

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
                Game.UI.Log("You have nothing to remove, you shameless beast!");
        }

        private static void CheckSheathe()
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
                    IO.AcceptedInput.Add(
                        IO.Indexes
                        [Game.Player.Inventory.IndexOf(
                            Game.Player.Quiver)
                        ]
                    );

                IO.AcceptedInput.Sort();

                string question = "Sheathe what? [";
                question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
                question += "]";

                IO.AskPlayer(
                    question,
                    InputType.QuestionPromptSingle,
                    PlayerResponses.Sheathe
                );
            }
            else
            {
                Game.UI.Log("You don't have anything quivered or wielded!");
            }
        }

        private static void CheckSleep()
        {
            if (!KeyBindings.Pressed(Bind.Sleep)) return;

            IO.CurrentCommand = new Command("sleep");

            IO.SetInput(IO.Numbers);
            IO.AskPlayer(
                "Sleep for how long?",
                InputType.QuestionPrompt,
                PlayerResponses.Sleep
            );
        }

        private static void CheckWield()
        {
            if (!KeyBindings.Pressed(Bind.Wield)) return;

            List<Item> wieldable = Game.Player.Inventory
                .Where(x =>
                    !Game.Player.GetEquippedItems().Contains(x))
                .Where(x => x != Game.Player.Quiver)
                .ToList();

            if(wieldable.Count <= 0)
            {
                Game.UI.Log("You have nothing to wield.");
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

            IO.CurrentCommand = new Command("wield");

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
                .Where(x => !Game.Player.GetEquippedItems()
                    .Contains(x))
                .Where(item => item.HasComponent<WearableComponent>())
                .ToList();

            if (wearable.Count <= 0)
            {
                Game.UI.Log("You have nothing to wear.");
                return;
            }

            IO.AcceptedInput.Clear();
            foreach(Item item in wearable)
                IO.AcceptedInput.Add(
                    IO.Indexes[Game.Player.Inventory.IndexOf(item)]
                );

            string question = "Wear what? [";
            question += IO.AcceptedInput.Aggregate(
                "", (c, n) => c + n);
            question += "]";

            IO.CurrentCommand = new Command("wear");

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
                Game.UI.Log("You don't know any spells.");
                return;
            }

            string question = "Cast what? [";
            question += IO.AcceptedInput.Aggregate("", (c, n) => c + n);
            question += "]";

            //setup cmd, no info yet other than type
            IO.CurrentCommand = new Command("cast");

            IO.AskPlayer(
                question,
                InputType.QuestionPromptSingle,
                PlayerResponses.Zap
            );
        }
    }
}
