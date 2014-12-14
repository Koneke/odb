using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    public class InventoryManager
    {
        public static int InventorySize = 21;

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
                        e => ContainerIDs[e.Key]
                            .Select(Util.GetItemByID)
                            .ToList()
                    );
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

        public static ODBGame Game;

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
            if (KeyBindings.Pressed(Bind.Inv_Up)) Selection--;
            if (KeyBindings.Pressed(Bind.Inv_Down)) Selection++;

            if (Selection < 0)
                Selection += CurrentContents.Count;
            if (CurrentContents.Count > 0)
                Selection = Selection % CurrentContents.Count;

            for (int i = (int)Bind.Inv_Jump_0; i <= (int)Bind.Inv_Jump_23; i++)
                if (KeyBindings.Pressed((Bind)i))
                    if(i - (int)Bind.Inv_Jump_0 < CurrentContents.Count)
                        Selection = i - (int)Bind.Inv_Jump_0;
        }

        public void InventoryInput()
        {
            if (KeyBindings.Pressed(Bind.Inventory))
            {
                if (State != InventoryState.Browsing)
                {
                    State = InventoryState.Browsing;
                    Game.UI.Log("Nevermind.");
                }
                IO.IOState = InputType.PlayerInput;
            }

            SelectionInput();

            switch (State)
            {
                case InventoryState.Browsing:
                    BrowsingInput();
                    break;

                case InventoryState.Inserting:
                    if (KeyBindings.Pressed(Bind.Inv_Cancel))
                    {
                        State = InventoryState.Browsing;
                        Game.UI.Log("Nevermind.");
                    }

                    if (KeyBindings.Pressed(Bind.Inv_Select))
                    {
                        if (SelectedItem == null) break;
                        if (!SelectedItem.HasComponent<ContainerComponent>())
                            break;

                        if (SelectedItem == _selected)
                        {
                            Game.UI.Log("You can't bend space and/or time!");
                            break;
                        }

                        PutInto(_selected, SelectedItem.ID);
                    }
                    break;

                case InventoryState.Joining:
                    if (KeyBindings.Pressed(Bind.Inv_Cancel))
                    {
                        State = InventoryState.Browsing;
                        Game.UI.Log("Nevermind.");
                    }

                    if (KeyBindings.Pressed(Bind.Inv_Select))
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
            if (KeyBindings.Pressed(Bind.Inv_Cancel))
            {
                if (CurrentContainer == -1)
                    IO.IOState = InputType.PlayerInput;
                else CurrentContainer = GetParentContainer(CurrentContainer);
            }

            if (SelectedItem == null) return;

            if (KeyBindings.Pressed(Bind.Inv_Select))
                if (SelectedItem.HasComponent<ContainerComponent>())
                {
                    CurrentContainer = SelectedItem.ID;
                    Selection = 0;
                    return;
                }

            if (KeyBindings.Pressed(Bind.TakeOut) && CurrentContainer != -1)
            {
                ContainerIDs[CurrentContainer].Remove(SelectedItem.ID);
                if (GetParentContainer(CurrentContainer) == -1)
                    Game.Player.Inventory.Add(SelectedItem);
                else
                    ContainerIDs[GetParentContainer(CurrentContainer)]
                        .Add(SelectedItem.ID);
                Game.Player.Pass();
            }

            if (KeyBindings.Pressed(Bind.PutInto))
            {
                _selected = SelectedItem;
                State = InventoryState.Inserting;
                Game.UI.Log("Put " + SelectedItem.GetName("name") + " into what?");
            }

            if (CurrentContainer != -1) return;

            if (KeyBindings.Pressed(Bind.Drop)) CheckDrop(SelectedItem);
            if (KeyBindings.Pressed(Bind.Wield)) CheckWield(SelectedItem);
            if (KeyBindings.Pressed(Bind.Wear)) CheckWear(SelectedItem);
            if (KeyBindings.Pressed(Bind.Sheath)) CheckSheath(SelectedItem);
            if (KeyBindings.Pressed(Bind.Remove)) CheckRemove(SelectedItem);
            if (KeyBindings.Pressed(Bind.Quiver)) CheckQuiver(SelectedItem);
            if (KeyBindings.Pressed(Bind.Split)) CheckSplit(SelectedItem);
            if (KeyBindings.Pressed(Bind.Eat)) CheckEat(SelectedItem);
            if (KeyBindings.Pressed(Bind.Read)) CheckRead(SelectedItem);

            if (KeyBindings.Pressed(Bind.Join))
            {
                if (!SelectedItem.Stacking) return;

                State = InventoryState.Joining;
                _selected = SelectedItem;
                Game.UI.Log("Join " + _selected.GetName("count") +
                    " with what?");
                Selection--;
            }

            if (Selection > CurrentContents.Count - 1)
                Selection--;
        }

        private static void CheckSplit(Item item)
        {
            if (Game.Player.Inventory.Count >= InventorySize)
            {
                Game.UI.Log("You are carrying too much!");
                return;
            }

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
            if (!Game.Player.IsEquipped(item))
            {
                Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
                PlayerResponses.Quiver();
            }
            else
            {
                if (Game.Player.IsWielded(item))
                    Game.UI.Log("You have to sheath that first.");
                else Game.UI.Log("You have to remove that first");
            }
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
                Game.UI.Log("You are busy wearing that.");
                return;
            }

            if (Game.Player.IsWielded(item))
            {
                Game.UI.Log("You are already wielding that.");
                return;
            }

            Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
            PlayerResponses.Wield();
        }

        private static void CheckWear(Item item)
        {
            if (!item.HasComponent<WearableComponent>())
            {
                Game.UI.Log("You can't wear that!");
                return;
            }

            if (Game.Player.IsWorn(item))
            {
                Game.UI.Log("You are already wearing that.");
                return;
            }

            if (Game.Player.IsWielded(item))
            {
                Game.UI.Log("You are busy wielding that.");
                return;
            }

            Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
            PlayerResponses.Wear();
        }

        private static void PutInto(Item item, int container)
        {
            Game.UI.Log(
                "Put " +
                    item.GetName("name") + " into " +
                    Util.GetItemByID(container).GetName("name") + "."
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
                    PlayerResponses.Sheath();
            }
            else
                Game.UI.Log("You are not wielding that.");
        }

        private static void CheckRemove(Item item)
        {
            if (Game.Player.IsWorn(item))
            {
                Game.QpAnswerStack.Push(IO.Indexes[Selection] + "");
                if (Game.Player.IsEquipped(item) &&
                    item.HasComponent<WearableComponent>())
                    PlayerResponses.Remove();
            }
            else
                Game.UI.Log("You are not wearing that.");
        }

        private static void CheckEat(Item item)
        {
            if (!item.HasComponent<EdibleComponent>()) return;

            Game.Player.Eat(item);
            Game.Player.Pass();
        }

        private static void CheckRead(Item item)
        {
            if (!item.HasComponent<ReadableComponent>() &&
                !item.HasComponent<LearnableComponent>()) return;

            Game.QpAnswerStack.Push(
                IO.Indexes[Game.Player.Inventory.IndexOf(item)] + ""
            );
            PlayerResponses.Read();
            Game.Player.Pass();
        }

        public void HandleCancel()
        {
            if (State != InventoryState.Browsing)
            {
                State = InventoryState.Browsing;
                Game.UI.Log("Nevermind.");
            }
            else IO.IOState = InputType.PlayerInput;
        }
    }
}