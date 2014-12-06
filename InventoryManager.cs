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

        public Dictionary<int, List<int>> ContainerIDs;
        public Dictionary<int, List<Item>> Containers {
            get
            {
                return
                    ContainerIDs.ToDictionary(
                        e => e.Key,
                        e => ContainerIDs[e.Key].Select(
                            Util.GetItemByID).ToList());
            }
        }
        public int CurrentContainer;

        public static Game1 Game;

        public int Selection;
        private int _insertedItem;

        private int GetParentContainer(int container)
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
            Inserting
        }

        public InventoryState State;

        public void InventoryInput()
        {
            if (IO.KeyPressed(Keys.I))
                IO.IOState = InputType.PlayerInput;

            int parentContainer = -1;
            if (CurrentContainer != -1)
                parentContainer = GetParentContainer(CurrentContainer);

            List<Item> currentContainer =
                CurrentContainer == -1
                    ? Game.Player.Inventory
                    : Containers[CurrentContainer];

            if (IO.KeyPressed(IO.Input.South)) Selection++;

            if (IO.KeyPressed(IO.Input.North)) Selection--;

            if (Selection < 0)
                Selection += currentContainer.Count;
            if (currentContainer.Count > 0)
                Selection = Selection % currentContainer.Count;

            switch (State)
            {
                case InventoryState.Browsing:
                    if (IO.KeyPressed(IO.Input.West))
                    {
                        if(CurrentContainer == -1)
                            IO.IOState = InputType.PlayerInput;
                        else CurrentContainer = parentContainer;
                    }
                    break;
                case InventoryState.Inserting:
                    if (IO.KeyPressed(IO.Input.West))
                    {
                        State = InventoryState.Browsing;
                        Game.Log("Nevermind.");
                    }
                    break;
            }

            /*if(IO.KeyPressed(IO.Input.West))
            {
                if (CurrentContainer == -1)
                {
                    if (Inserting)
                    {
                        Inserting = false;
                        Game.Log("Nevermind.");
                    }
                    else IO.IOState = InputType.PlayerInput;
                    return;
                }

                if(!Inserting)
                    CurrentContainer = parentContainer;
                else
                    Inserting = false;
            }*/

            int count = currentContainer.Count;

            if (count <= 0) return;

            Item item = currentContainer[Selection];

            if (State == InventoryState.Inserting)
            {
                if (!IO.KeyPressed(IO.Input.East) &&
                    !IO.KeyPressed(IO.Input.Enter)) return;

                if (!item.HasComponent("cContainer")) return;

                Game.Log(
                    "Put " + 
                        Util.GetItemByID(_insertedItem)
                            .GetName("name") + " into " +
                        item.GetName("name") + "."
                    );

                if (CurrentContainer == -1)
                    Game.Player.Inventory.Remove(
                        Util.GetItemByID(_insertedItem));
                else
                    ContainerIDs[CurrentContainer].Remove(
                        _insertedItem);
                ContainerIDs[item.ID].Add(_insertedItem);

                State = InventoryState.Browsing;
                _insertedItem = -1;
            }
            else
            {
                if (IO.KeyPressed(IO.Input.East) ||
                    IO.KeyPressed(IO.Input.Enter))
                    if (item.HasComponent("cContainer"))
                    {
                        CurrentContainer = item.ID;
                        Selection = 0;
                        return;
                    }

                if (IO.KeyPressed(Keys.T) && CurrentContainer != -1)
                {
                    ContainerIDs[CurrentContainer].Remove(item.ID);
                    if (parentContainer == -1)
                        Game.Player.Inventory.Add(item);
                    else
                        ContainerIDs[parentContainer].Add(item.ID);
                    Game.Player.Pass();
                }

                //if it wasn't a container, or we pressed p
                if (IO.KeyPressed(Keys.P) ||
                    IO.KeyPressed(IO.Input.East) ||
                    IO.KeyPressed(IO.Input.Enter))
                {
                    _insertedItem = item.ID;
                    State = InventoryState.Inserting;
                    Game.Log("Put " + item.GetName("name") + " into what?");
                }

                if (CurrentContainer != -1) return;

                if (IO.KeyPressed(Keys.W) && !Game.Player.IsEquipped(item))
                {
                    Game.QpAnswerStack.Push(IO.Indexes[Selection]+"");
                    if (!IO.ShiftState)
                        PlayerResponses.Wield();
                    else if (item.HasComponent("cWearable"))
                        PlayerResponses.Wear();
                }

                if (IO.KeyPressed(Keys.S) && IO.ShiftState)
                {
                    Game.QpAnswerStack.Push(IO.Indexes[Selection]+"");
                    if( Game.Player.IsEquipped(item) &&
                        !item.HasComponent("cWearable"))
                        PlayerResponses.Sheath();
                }

                if (IO.KeyPressed(Keys.R) && IO.ShiftState)
                {
                    Game.QpAnswerStack.Push(IO.Indexes[Selection]+"");
                    if( Game.Player.IsEquipped(item) &&
                        item.HasComponent("cWearable"))
                        PlayerResponses.Remove();
                }

                if (IO.KeyPressed(Keys.D) && !IO.ShiftState)
                {
                    Game.QpAnswerStack.Push(IO.Indexes[Selection]+"");
                    if(item.Count > 1)
                        IO.IOState = InputType.PlayerInput;
                    PlayerResponses.Drop();
                }

                if (IO.KeyPressed(Keys.Q) && IO.ShiftState)
                {
                    if(Game.Player.IsEquipped(item))
                        Game.Player.Quiver = item;
                }

                if (IO.KeyPressed(Keys.S) && !IO.ShiftState)
                {
                    //ooh yeah use that stack
                    Game.QpAnswerStack.Push(""+CurrentContainer);
                    Game.QpAnswerStack.Push(""+item.ID);
                    IO.AcceptedInput.Clear();
                    for (Keys k = Keys.D0; k <= Keys.D9; k++)
                        IO.AcceptedInput.Add((char)k);
                    IO.AskPlayer(
                        "Split out how many?",
                        InputType.QuestionPrompt,
                        PlayerResponses.Split
                        );
                }
            }
        }
    }
}