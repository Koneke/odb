using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Input;

using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    [DataContract]
    public class InventoryManager
    {
        public static int InventorySize = 21;

        public InventoryManager()
        {
            CurrentContainer = -1;
            ContainerIDs = new Dictionary<int, List<int>>();
        }

        [DataMember] public static Dictionary<int, List<int>> ContainerIDs;
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

                        if (Game.Player.IsWielded(_selected))
                        {
                            Game.UI.Log("You're busy wielding that.");
                            break;
                        }

                        if (Game.Player.IsWorn(_selected))
                        {
                            Game.UI.Log("You're busy wearing that.");
                            break;
                        }

                        if (Game.Player.Quiver == _selected)
                        {
                            Game.UI.Log("Take it out from your quiver first.");
                            break;
                        }

                        PutInto(_selected, SelectedItem.ID);
                        Selection--;
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
                if (GetParentContainer(CurrentContainer) == -1)
                    Game.Player.GiveItem(SelectedItem);
                else
                    ContainerIDs[GetParentContainer(CurrentContainer)]
                        .Add(SelectedItem.ID);
                ContainerIDs[CurrentContainer].Remove(SelectedItem.ID);
                Game.Player.Pass();
            }

            if (KeyBindings.Pressed(Bind.PutInto))
            {
                _selected = SelectedItem;
                State = InventoryState.Inserting;
                Game.UI.Log(
                    "Put {1} into what?",
                    SelectedItem.GetName("name")
                );
            }

            if (CurrentContainer != -1) return;

            //todo: apply/use?
            if (KeyBindings.Pressed(Bind.Drop)) CheckDrop(SelectedItem);
            if (KeyBindings.Pressed(Bind.Wield)) CheckWield(SelectedItem);
            if (KeyBindings.Pressed(Bind.Wear)) CheckWear(SelectedItem);
            if (KeyBindings.Pressed(Bind.Sheath)) CheckSheath(SelectedItem);
            if (KeyBindings.Pressed(Bind.Remove)) CheckRemove(SelectedItem);
            if (KeyBindings.Pressed(Bind.Quaff)) CheckQuaff(SelectedItem);
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

            if (!item.Stacking || (item.Stacking && item.Count < 2))
            {
                Game.UI.Log("You can't split that.");
                return;
            }

            //not actually passing this to an actor, just using as a vessel
            //to ferry things around.
            IO.CurrentCommand = new Command("split")
                .Add("container", CurrentContainer)
                .Add("item-id", item.ID);

            IO.AcceptedInput.Clear();
            for (Keys k = Keys.D0; k <= Keys.D9; k++)
                IO.AcceptedInput.Add((char)k);

            IO.AskPlayer(
                "Split out how many?",
                InputType.QuestionPrompt,
                PlayerResponses.Split
            );
        }

        private static void CheckQuaff(Item item)
        {
            if (item.HasComponent<DrinkableComponent>())
                Game.Player.Do(new Command("quaff").Add("item", item));
            else Game.UI.Log("You can't drink that.");
        }

        private static void CheckQuiver(Item item)
        {
            if (!Game.Player.IsEquipped(item))
            {
                Game.Player.Do(new Command("quiver").Add("item", item));
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
            IO.CurrentCommand = new Command("drop").Add("item", item);

            if (item.Stacking && item.Count > 1)
            {

                IO.AcceptedInput.Clear();
                IO.AcceptedInput.AddRange(IO.Numbers.ToList());

                IO.AskPlayer(
                    "How many?",
                    InputType.QuestionPrompt,
                    PlayerResponses.DropCount
                );
            }
            else
            {
                IO.CurrentCommand.Add("count", 0);
                Game.Player.Do();
            }
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

            if (Game.Player.Quiver == item)
            {
                Game.UI.Log("Take it out from your quiver first.");
                return;
            }

            if (Game.Player.CanEquip(item.GetHands(Game.Player)))
            {
                Game.Player.Do(new Command("wield").Add("item", item));
            }
            else
            {
                if (item.GetHands(Game.Player).Count >
                    Game.Player.PaperDoll
                        .Where(bp => bp != null)
                        .Sum(bp => bp.Type == DollSlot.Hand ? 1 : 0))
                    Game.UI.Log("You'd need more hands to do that.");
                else
                    Game.UI.Log("You have too much stuff in your hands.");
            }
        }

        private static void CheckWear(Item item)
        {
            WearableComponent wc = item.GetComponent<WearableComponent>();
            if(wc == null)
            {
                Game.UI.Log("You can't wear that.");
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

            //LH-191214: These error responses really need to be centralized
            //           somewhere. DRY, stupid.
            if (Game.Player.Quiver == item)
            {
                Game.UI.Log("Take it out from your quiver first.");
                return;
            }

            if (Game.Player.CanEquip(wc.EquipSlots))
                Game.Player.Do(new Command("wear").Add("item", item));
            else
                Game.UI.Log(
                    "You'd need to remove something first " +
                        "(or grow more limbs).");

        }

        private static void PutInto(Item item, int container)
        {
            Game.UI.Log(
                "Put {1} into {2}.",
                item.GetName("name"),
                Util.GetItemByID(container).GetName("name")
            );

            if (GetParentContainer(container) == -1)
                Game.Player.RemoveItem(item);
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
            if (Game.Player.IsWielded(item) || Game.Player.Quiver == item)
            {
                Game.Player.Do(new Command("sheathe").Add("item", item));
            }
            else
                Game.UI.Log("You are not wielding that.");
        }

        private static void CheckRemove(Item item)
        {
            if (Game.Player.IsWorn(item))
                Game.Player.Do(new Command("remove").Add("item", item));
            else
                Game.UI.Log("You are not wearing that.");
        }

        private static void CheckEat(Item item)
        {
            if (!item.HasComponent<EdibleComponent>()) return;
            Game.Player.Do(new Command("eat").Add("item", item));
        }

        private static void CheckRead(Item item)
        {
            if (!item.HasComponent<ReadableComponent>() &&
                !item.HasComponent<LearnableComponent>())
            {
                Game.UI.Log("You can't read that.");
                return;
            }

            ReadableComponent rc = item.GetComponent<ReadableComponent>();

            if (rc != null)
            {
                Spell effect = rc.Effect;

                IO.CurrentCommand = new Command("read").Add("item", item);

                item.Identify();

                //Make sure to spend the charge BEFORE setting up input
                //otherwise all indexes above our gets off-by-one'd.
                item.SpendCharge();

                if (effect.CastType == InputType.None)
                {
                    Game.Player.Do(IO.CurrentCommand);
                }
                else
                {
                    if (effect.SetupAcceptedInput != null)
                        effect.SetupAcceptedInput(Game.Player);

                    if (IO.AcceptedInput.Count <= 0)
                    {
                        Game.UI.Log("You have nothing to cast that on.");
                        return;
                    }

                    string question;

                    if (effect.CastType == InputType.Targeting)
                        question = "Where?";
                    else
                    {
                        question = "On what? [";
                        question += IO.AcceptedInput.Aggregate(
                            "", (c, n) => c + n
                        );
                        question += "]";
                    }

                    IO.AskPlayer(
                        question,
                        effect.CastType,
                        Game.Player.Do
                    );
                }
            }
            else
                Game.Player.Do(new Command("learn").Add("item", item));
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