using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    public class InventoryManager
    {
        public InventoryManager()
        {
            CurrentContainer = -1;
            ContainerIDs = new Dictionary<int, List<int>>();
        }

        public static Dictionary<int, List<int>> ContainerIDs;
        public static Dictionary<int, List<Item>> Containers {
            get
            {
                return
                    ContainerIDs.ToDictionary(
                    e => e.Key,
                    e => ContainerIDs[e.Key].Select(
                        Util.GetItemByID).ToList());
            }
        }
        public static int CurrentContainer;
        public List<Item> CurrentContents {
            get
            {
                return CurrentContainer == -1
                    ? Game.Player.Inventory
                    : Containers[CurrentContainer];
            }
        }
        public Item SelectedItem {
            get {
                Item it = null;
                if(CurrentContents.Count > 0)
                    it = CurrentContents[Selection];
                return it;
            }
        }

        public static Game1 Game;

        public static int Selection;
        private static Item _selected;

        private static int GetParentContainer(int container)
        {
            foreach (int id in
                Containers.Keys
                .Where(id => Containers[id]
                .Any(x => x.ID == container)))
            {
                return id;
            }
            return -1;
        }

        public enum InventoryState
        {
            Browsing,
            Inserting,
            Joining
        }

        public static InventoryState State;

        private void SelectionInput()
        {
            if (IO.KeyPressed(IO.Input.North)) Selection--;
            if (IO.KeyPressed(IO.Input.South)) Selection++;

            if (Selection < 0)
                Selection += CurrentContents.Count;
            if (CurrentContents.Count > 0)
                Selection = Selection % CurrentContents.Count;
        }

        public void InventoryInput()
        {
            if (IO.KeyPressed(Keys.I))
                IO.IOState = InputType.PlayerInput;

            SelectionInput();

            switch (State)
            {
                case InventoryState.Browsing:
                    BrowsingInput();
                    break;

                case InventoryState.Inserting:
                    if (IO.KeyPressed(IO.Input.West))
                    {
                        State = InventoryState.Browsing;
                        Game.Log("Nevermind.");
                    }

                    if (IO.KeyPressed(IO.Input.East) ||
                        IO.KeyPressed(IO.Input.Enter))
                    {
                        if (SelectedItem == null) break;
                        if (!SelectedItem.HasComponent("cContainer")) break;

                        if (SelectedItem == _selected)
                        {
                            Game.Log("You can't bend space and/or time!");
                            break;
                        }

                        PutInto(_selected, SelectedItem.ID);
                    }
                    break;

                case InventoryState.Joining:
                    if (IO.KeyPressed(IO.Input.West))
                    {
                        State = InventoryState.Browsing;
                        Game.Log("Nevermind.");
                    }

                    if (IO.KeyPressed(IO.Input.East) ||
                        IO.KeyPressed(IO.Input.Enter))
                    {
                        if (_selected.CanStack(SelectedItem))
                        {
                            SelectedItem.Stack(_selected);
                            CurrentContents.Remove(_selected);
                            State = InventoryState.Browsing;
                        }
                    }
                    break;
            }
        }

        private void BrowsingInput()
        {
            if (IO.KeyPressed(IO.Input.West))
            {
                if (CurrentContainer == -1)
                    IO.IOState = InputType.PlayerInput;
                else CurrentContainer = GetParentContainer(CurrentContainer);
            }

            if (SelectedItem == null) return;

            if (IO.KeyPressed(IO.Input.East) ||
                IO.KeyPressed(IO.Input.Enter))
                if (SelectedItem.HasComponent("cContainer"))
                {
                    CurrentContainer = SelectedItem.ID;
                    Selection = 0;
                    return;
                }

            if (IO.KeyPressed(Keys.T) && CurrentContainer != -1)
            {
                ContainerIDs[CurrentContainer].Remove(SelectedItem.ID);
                if (GetParentContainer(CurrentContainer) == -1)
                    Game.Player.Inventory.Add(SelectedItem);
                else
                    ContainerIDs[GetParentContainer(CurrentContainer)]
                        .Add(SelectedItem.ID);
                Game.Player.Pass();
            }

            if (IO.KeyPressed(Keys.P) ||
                IO.KeyPressed(IO.Input.East) ||
                IO.KeyPressed(IO.Input.Enter))
            {
                _selected = SelectedItem;
                State = InventoryState.Inserting;
                Game.Log("Put " + SelectedItem.GetName("name") + " into what?");
            }

            if (CurrentContainer != -1) return;

            if (IO.KeyPressed(Keys.D) && !IO.ShiftState)
                CheckDrop(SelectedItem);

            if (IO.KeyPressed(Keys.W) && !IO.ShiftState)
                CheckWield(SelectedItem);

            if (IO.KeyPressed(Keys.W) && IO.ShiftState)
                CheckWear(SelectedItem);

            if (IO.KeyPressed(Keys.S) && IO.ShiftState)
                CheckSheath(SelectedItem);

            if (IO.KeyPressed(Keys.R) && IO.ShiftState)
                CheckRemove(SelectedItem);

            if (IO.KeyPressed(Keys.Q) && IO.ShiftState)
                CheckQuiver(SelectedItem);

            if (IO.KeyPressed(Keys.S) && !IO.ShiftState)
                CheckSplit(SelectedItem);

            if (IO.KeyPressed(Keys.J) && IO.ShiftState)
            {
                if (!SelectedItem.Stacking) return;

                State = InventoryState.Joining;
                _selected = SelectedItem;
                Game.Log("Join " + _selected.GetName("count") +
                    " with what?");
                CurrentContainer--;
            }
        }

        private static void CheckSplit(Item item)
        {
            Game.QpAnswerStack.Push("" + CurrentContainer);
            Game.QpAnswerStack.Push("" + item.ID);
            IO.AcceptedInput.Clear();
            for (Keys k = Keys.D0; k <= Keys.D9; k++)
                IO.AcceptedInput.Add((char)k);
            IO.AskPlayer(
                "Split out how many?",
                InputType.QuestionPrompt,
                PlayerResponses.Split
                );
        }

        private static void CheckQuiver(Item item)
        {
            if (Game.Player.IsEquipped(item))
                Game.Player.Quiver = item;
        }

        private static void CheckDrop(Item item)
        {
            Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
            if (item.Count > 1)
                IO.IOState = InputType.PlayerInput;
            PlayerResponses.Drop();
        }

        private static void CheckWield(Item item)
        {
            if (Game.Player.IsWorn(item))
            {
                Game.Log("You are busy wearing that.");
                return;
            }

            if (Game.Player.IsWielded(item))
            {
                Game.Log("You are already wielding that.");
                return;
            }

            Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
            PlayerResponses.Wield();
        }

        private static void CheckWear(Item item)
        {
            if (Game.Player.IsWorn(item))
            {
                Game.Log("You are already wearing that.");
                return;
            }

            if (Game.Player.IsWielded(item))
            {
                Game.Log("You are busy wielding that.");
                return;
            }

            Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
            PlayerResponses.Wear();
        }

        private static void PutInto(Item item, int container)
        {
            Game.Log(
                "Put " +
                    _selected.GetName("name") + " into " +
                    item.GetName("name") + "."
                );

            if (GetParentContainer(container) == -1)
                Game.Player.Inventory.Remove(item);
            else
                ContainerIDs[GetParentContainer(container)].Remove(item.ID);
            ContainerIDs[container].Add(item.ID);

            State = InventoryState.Browsing;
            //not sure if nulling is necessary
            _selected = null;

            Game.Player.Pass();
        }

        private static void CheckSheath(Item item)
        {
            if (Game.Player.IsWielded(item))
            {
                Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
                if (Game.Player.IsEquipped(item) &&
                    !item.HasComponent("cWearable"))
                    PlayerResponses.Sheath();
            }
            else
            {
                Game.Log("You are not wielding that.");
            }
        }

        private static void CheckRemove(Item item)
        {
            if (Game.Player.IsWorn(item))
            {
                Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
                if (Game.Player.IsEquipped(item) &&
                    item.HasComponent("cWearable"))
                    PlayerResponses.Remove();
            }
            else
            {
                Game.Log("You are not wearing that.");
            }
        }
    }
}