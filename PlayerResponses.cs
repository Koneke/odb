using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class PlayerResponses
    {
        public static Game1 Game;

        public static void Get()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            
            List<Item> onTile = Game.Level.ItemsOnTile(
                Game.Player.xy
            );

            Item it = onTile[i];
            Game.Level.WorldItems.Remove(it);

            if (it.Definition.Stacking)
            {
                bool alreadyHolding = false;
                foreach (Item item in Game.Player.Inventory
                    .Where(item => item.Definition.Type == it.Definition.Type)
                    .Where(item => !Game.Player.IsEquipped(item))
                ) {
                    alreadyHolding = true;
                    item.Count+=it.Count;
                    //merging into the already held item
                    Game.Level.AllItems.Remove(it);
                }
                if (!alreadyHolding)
                    Game.Player.Inventory.Add(it);
            }
            else
            {
                Game.Player.Inventory.Add(it);
            }
            Util.Game.Log("Picked up " + it.GetName() + ".");
            Game.Player.Pass();
        }

        public static void Drop()
        {
            //NOTE, PEEK.
            //BECAUSE WE NEED TO REUSE THE ANSWER LATER?
            //not actually using the stack here yet, but we want
            //to drop the string answer bit in the sign later...
            //I think.
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

            Item it = Game.Player.Inventory[i];

            if (it.Definition.Stacking && it.Count > 1)
            {
                IO.AcceptedInput.Clear();
                for (Keys k = Keys.D0; k <= Keys.D9; k++)
                    IO.AcceptedInput.Add((char)k);

                IO.AskPlayer(
                    "How many?",
                    InputType.QuestionPrompt,
                    DropCount
                );
            }
            //just do a full drop, no splitting necessary
            else DoDrop(i);
        }

        public static void DropCount()
        {
            string count = Game.QpAnswerStack.Pop();
            string index = Game.QpAnswerStack.Pop();
            int i = IO.Indexes.IndexOf(index[0]);
            int c = int.Parse(count);
            DoDrop(i, c);
        }

        static void DoDrop(int index)
        {
            Item it = Game.Player.Inventory[index];

            Game.Player.Inventory.Remove(it);
            Game.Level.WorldItems.Add(it);

            it.xy = Game.Player.xy;

            //actually make sure to unwield/unwear as well
            foreach (BodyPart bp in Game.Player.PaperDoll
                .Where(bp => bp.Item == it))
                bp.Item = null;

            if (Game.Player.Quiver == it)
                Game.Player.Quiver = null;

            Game.Log("Dropped " + it.GetName() + ".");

            Game.Player.Pass();
        }

        static void DoDrop(int index, int count)
        {
            Item it = Game.Player.Inventory[index];
            if (!it.Definition.Stacking || it.Count <= 1) return;

            if (count > it.Count)
            {
                Game.Log("You don't have that many.");
            }
            else if (count == it.Count)
            {
                //falling through to the normal itemdropping
                DoDrop(index);
            }
            else
            {
                //copy the item
                Item droppedStack =
                    new Item(
                        //clone item
                        it.WriteItem().ToString()
                    ) {
                        //mod essential stuff
                        ID = Item.IDCounter++,
                        Count = count
                    };

                it.Count -= count;
                Game.Level.Spawn(droppedStack);
                droppedStack.xy = Game.Player.xy;

                Game.Log("Dropped " +
                    droppedStack.GetName() + "."
                );

                Game.Player.Pass();
            }
        }

        public static void Wield()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item it = Game.Player.Inventory[i];

            if(!Game.Player.CanEquip(it.EquipSlots))
                Game.Log("You need to remove something first.");
            else {
                Game.Log("Equipped "+it.GetName() + ".");
                Game.Player.Equip(it);
                it.Identify();
                Game.Player.Pass();
            }
        }

        public static void Wear()
        {
            //make sure we start using this instead
            //so we can phase the argument out
            string answer = Game.QpAnswerStack.Pop();

            int i = IO.Indexes.IndexOf(answer[0]);
            Item it = Game.Player.Inventory[i];

            bool canEquip = true;

            if (!Game.Player.CanEquip(it.EquipSlots))
            {
                canEquip = false;
                Game.Log("You need to remove something first.");
            }

            if (!canEquip) return;

            //make sure we're not equipping "2x ..."
            if (it.Definition.Stacking && it.Count > 1)
            {
                Item clone = new Item(
                    //clone
                    it.WriteItem().ToString()
                ) {
                    //no dupe ids pls
                    ID = Item.IDCounter++
                };

                clone.Count--;
                it.Count = 1;
                Game.Player.Inventory.Add(clone);
                Game.Level.AllItems.Add(clone);
            }
            Game.Player.Equip(it);
            it.Identify();
            Game.Log("Wore " + it.GetName() + ".");

            Game.Player.Pass();
        }

        public static void Quiver()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);

            Item selected = Game.Player.Inventory[i];
            Game.Player.Quiver = selected;

            //todo: should we really identify on quiver?
            //      or should we rather do it on fire?
            selected.Identify();
            Game.Log("Quivered "+ selected.GetName() + ".");

            Game.Player.Pass();
        }

        public static void Sheath()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            if (i >= Game.Player.Inventory.Count)
            {
                Game.Log("Invalid selection (" + answer[0] + ").");
                return;
            }

            Item it = Game.Player.Inventory[i];

            foreach (BodyPart bp in Game.Player.PaperDoll
                .Where(bp => bp.Item == it))
                bp.Item = null;

            Util.Game.Log("Unequipped " + it.GetName() + ".");

            Game.Player.Pass();
        }

        public static void Remove()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.Indexes.IndexOf(answer[0]);
            Item it = Game.Player.Inventory[i];

            foreach (BodyPart bp in Game.Player.PaperDoll
                .Where(bp => bp.Item == it))
                bp.Item = null;

            Game.Log("Removed " + it.GetName());

            Item stack =  Game.Player.Inventory.Find(
                item => item != it && item.Type == it.Type);

            if (stack == null) return;

            stack.Count += it.Count;
            Game.Player.Inventory.Remove(it);
            Game.Level.AllItems.Remove(it);
        }

        public static void Open()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.Player.xy.x + offset.x,
                    Game.Player.xy.y + offset.y
                ];
            if (t.Door == Door.Closed)
            {
                t.Door = Door.Open;
                Game.Log("You opened the door.");

                //counted as a movement action at the moment, based
                //on the dnd rules.
                Game.Player.Pass(true);
            }
            else Game.Log("There's no closed door there.");
        }

        public static void Close()
        {
            string answer = Game.QpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Point p = new Point(
                Game.Player.xy.x + offset.x,
                Game.Player.xy.y + offset.y);
            Tile t = Game.Level.Map[p.x, p.y];

            if (t.Door == Door.Open)
            {
                //first check if something's in the way
                if (Game.Level.ItemsOnTile(p).Count <= 0)
                {
                    t.Door = Door.Closed;
                    Game.Log("You closed the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.Player.Pass(true);
                }
                else Game.Log("There's something in the way.");
            }
            else Game.Log("There's no open door there.");
        }

        public static void Zap()
        {
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;
            int i = IO.Indexes.IndexOf(answer[0]);
            if (i >= Game.Player.Spellbook.Count)
                Game.Log("Invalid selection (" + answer[0] + ").");
            else if (Game.Player.MpCurrent < Game.Player.Spellbook[i].Cost)
                Game.Log("You lack the energy.");
            else
            {
                Game.TargetedSpell = Game.Player.Spellbook[i];
                IO.AskPlayer(
                    "Casting " + Game.Player.Spellbook[i].Name,
                    InputType.Targeting,
                    Cast
                );
            }
        }

        public static void Use()
        {
            string answer = Game.QpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            Item it = Game.Player.Inventory[IO.Indexes.IndexOf(answer[0])];

            if (it.Count <= 0)
            {
                Game.Log(it.GetName(true, false, true) + " lacks charges.");
                return;
            }

            //nontargeted
            if (it.UseEffect.Range <= 0)
            {
                Projectile pr = it.UseEffect.Cast(
                    Game.Player,
                    Game.Player.xy
                );
                pr.Move();
                it.Count--;
                if (it.Count <= 0)
                {
                    Game.Player.Inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.Log(it.GetName(true, false, true) + " is spent!");
                }
                Game.Player.Pass();
            }
            //targeted
            else
            {
                Game.TargetedSpell = it.UseEffect;
                IO.AskPlayer(
                    "Using " + it.GetName(true),
                    InputType.Targeting,
                    UseCast
                );
            }
        }

        public static void Cast()
        {
            int index = IO.Indexes.IndexOf(Game.QpAnswerStack.Pop()[0]);
            if (Game.Level.Map[Game.Target.x, Game.Target.y] != null) {
                Game.Player.Cast(
                    Game.TargetedSpell, Game.Target
                );
                Game.Player.MpCurrent -= Game.Player.Spellbook[index].Cost;
            }
            else
                Game.Log("Invalid target.");
        }

        public static void UseCast()
        {
            if (Game.Level.Map[Game.Target.x, Game.Target.y] != null)
            {
                int i = IO.Indexes.IndexOf(Game.QpAnswerStack.Pop()[0]);
                Item it = Game.Player.Inventory[i];
                Game.Log("You use " + it.GetName(true) + ".");
                //Projectile pr = Game.TargetedSpell.Cast(Game.player, p);
                Projectile pr = Game.TargetedSpell.Cast(
                    Game.Player,
                    Game.Target
                );
                pr.Move();
                //spend charge
                it.Count--;
                //rechargin items should probably not trigger this
                //but that's a later issue
                if (it.Count <= 0)
                {
                    Game.Player.Inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.Log(it.GetName(true, false, true) + " is spent!");
                }
                Game.Player.Pass();
            }
            else
                Game.Log("Invalid target.");
        }

        public static void Fire()
        {
            if (!Game.Player.Vision[
                Game.Target.x, Game.Target.y
            ]) {
                Game.Log("You can't see that place.");
                return;
            }
            Actor a = Game.Level.ActorOnTile(Game.Target);
            //todo: allow firing anyways..?
            if (a == null)
            {
                Game.Log("Nothing there to fire upon.");
                return;
            }

            Game.Player.Shoot(a);
        }

        public static void Look()
        {
            Examine(true);
        }

        public static void Examine(bool verbose = false)
        {
            Tile t = Game.Level.Map[Game.Target.x, Game.Target.y];

            string distString =
                (Util.Distance(Game.Player.xy, Game.Target) > 0 ?
                    " there. " : " here. ");

            if (t == null || !Game.Level.Seen[Game.Target.x, Game.Target.y])
            {
                Game.Log(
                    "You see nothing" + distString
                );
                return;
            }
            if (t.Solid)
            {
                Game.Log("You see a dungeon wall" + distString);
                return;
            }

            List<Item> items = Game.Level.ItemsOnTile(Game.Target);
            string str = "";

            if (verbose)
            {
                if (t.Door != Door.None) str = "You see a door. ";
                else if (t.Stairs != Stairs.None)
                    str = "You see a set of stairs.";
                else str = "You see the dungeon floor. ";
            }


            if (Game.Player.Vision[Game.Target.x, Game.Target.y])
            {
                if (t.Engraving != "")
                    str += "\"" + t.Engraving + "\" is written on the floor" +
                        distString;

                Actor a;
                if ((a = Game.Level.ActorOnTile(t)) != null)
                    if(a != Game.Player)
                        str += "You see " + a.GetName() + distString;
                if (items.Count > 0)
                    str += "There's " + items[0].GetName() + distString;
                else if (items.Count > 1)
                    str += "There's several items " + distString;
            }

            if(str != "")
                Game.Log(str);
        }

        public static void Eat()
        {
            string answer = Game.QpAnswerStack.Pop();
            int index = IO.Indexes.IndexOf(answer[0]);

            Item it = Game.Player.Inventory[index];

            if (it.Definition.Stacking)
            {
                if (it.Count > 1)
                {
                    it.Count--;
                    Game.Player.Eat(it);
                }
                else
                {
                    Game.Player.Inventory.RemoveAt(index);
                    Game.Player.Eat(it);
                }
            }
            else
            {
                Game.Log("You eat " + it.GetName());
                Game.Player.Inventory.RemoveAt(index);
                Game.Player.Eat(it);
            }

        }
    }
}
