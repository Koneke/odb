using System.Collections.Generic;

using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class PlayerResponses
    {
        public static Game1 Game;

        public static void Get()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);
            
            List<Item> onTile = Util.ItemsOnTile(
                Game.player.xy
            );

            if (i < onTile.Count)
            {
                Item it = onTile[i];
                Game.Level.WorldItems.Remove(it);

                if (it.Definition.stacking)
                {
                    bool alreadyHolding = false;
                    foreach (Item item in Game.player.inventory)
                        if (item.Definition.type == it.Definition.type)
                        {
                            if(!Game.player.IsEquipped(item)) {
                                alreadyHolding = true;
                                item.count+=it.count;
                                //merging into the already held item
                                Game.Level.AllItems.Remove(it);
                            }
                        }
                    if (!alreadyHolding)
                        Game.player.inventory.Add(it);
                }
                else
                {
                    Game.player.inventory.Add(it);
                }
                Game.log.Add("Picked up " + it.GetName() + ".");
                Game.player.Pass();
            }
            else
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }
        }

        public static void Drop()
        {
            //NOTE, PEEK.
            //BECAUSE WE NEED TO REUSE THE ANSWER LATER?
            //not actually using the stack here yet, but we want
            //to drop the string answer bit in the sign later...
            //I think.
            string answer = Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);

            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            if (i < Game.player.inventory.Count)
            {
                Item it = Game.player.inventory[i];

                if (it.Definition.stacking && it.count > 1)
                {
                    IO.AcceptedInput.Clear();
                    for (Keys k = Keys.D0; k <= Keys.D9; k++)
                        IO.AcceptedInput.Add((char)k);

                    IO.AskPlayer(
                        "How many?",
                        InputType.QuestionPrompt,
                        PlayerResponses.DropCount
                    );
                }
                else
                {
                    drop(i);
                }
            }
        }

        public static void DropCount()
        {
            string count = Game.qpAnswerStack.Pop();
            string index = Game.qpAnswerStack.Pop();
            int i = IO.indexes.IndexOf(index[0]);
            int c = int.Parse(count);
            drop(i, c);
        }

        static void drop(int index)
        {
            Item it = Game.player.inventory[index];

            Game.player.inventory.Remove(it);
            Game.Level.WorldItems.Add(it);

            it.xy = Game.player.xy;

            //actually make sure to unwield/unwear as well
            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Dropped " + it.GetName() + ".");

            Game.player.Pass();
        }

        static void drop(int index, int count)
        {
            Item it = Game.player.inventory[index];
            if (it.Definition.stacking && it.count > 1)
            {
                if (count > it.count)
                {
                    Game.log.Add("You don't have that many.");
                    return;
                }
                else if (count == it.count)
                {
                    //falling through to the normal itemdropping
                    drop(index);
                }
                else
                {
                    //copy the item
                    Item droppedStack =
                        new Item(
                            it.WriteItem().ToString()
                        );
                    //change id
                    droppedStack.id = Item.IDCounter++;
                    droppedStack.count = count;
                    it.count -= count;
                    Game.Level.WorldItems.Add(droppedStack);
                    Game.Level.AllItems.Add(droppedStack);

                    Game.log.Add("Dropped "+// count + " " +
                        it.GetName() + "."
                    );

                    Game.player.Pass();
                }
            }
        }

        public static void Wield()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);
            Item it = Game.player.inventory[i];

            if(!Game.player.CanEquip(it.equipSlots))
                Game.log.Add("You need to remove something first.");
            else {
                Game.log.Add("Equipped "+it.GetName() + ".");
                Game.player.Equip(it);

                Game.player.Pass();
            }
        }

        public static void Wear()
        {
            //make sure we start using this instead
            //so we can phase the argument out
            string answer = Game.qpAnswerStack.Pop();

            int i = IO.indexes.IndexOf(answer[0]);
            Item it = Game.player.inventory[i];

            bool canEquip = true;
            //should even show up as a choice though
            if (it.equipSlots.Contains(DollSlot.Quiver))
                canEquip = false;

            if (!Game.player.CanEquip(it.equipSlots))
            {
                canEquip = false;
                Game.log.Add("You need to remove something first.");
            }

            if (canEquip)
            {
                //make sure we're not equipping "2x ..."
                if (it.Definition.stacking && it.count > 1)
                {
                    Item clone = new Item(it.WriteItem().ToString());
                    clone.id = Item.IDCounter++;
                    clone.count--;
                    it.count = 1;
                    Game.player.inventory.Add(clone);
                    Game.Level.AllItems.Add(clone);
                }
                Game.player.Equip(it);
                Game.log.Add("Wore " + it.GetName() + ".");

                Game.player.Pass();
            }
        }

        public static void Quiver()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);

            Item selected = Game.player.inventory[i];
            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Type == DollSlot.Quiver)
                    bp.Item = selected;

            Game.log.Add("Quivered "+ selected.GetName() + ".");

            Game.player.Pass();
        }

        public static void Sheath()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);
            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection (" + answer[0] + ").");
                return;
            }

            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == Game.player.inventory[i]) {
                    it = bp.Item;
                }

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Unequipped " + it.GetName() + ".");

            Game.player.Pass();
        }

        public static void Remove()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            int i = IO.indexes.IndexOf(answer[0]);
            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == Game.player.inventory[i])
                    it = bp.Item;

            foreach (BodyPart bp in Game.player.PaperDoll)
                if (bp.Item == it) bp.Item = null;

            Game.log.Add("Removed " + it.GetName());

            Item stack = null;
            if (it.Definition.stacking)
                foreach (Item item in Game.player.inventory)
                    if (
                        item.Definition.type == it.Definition.type &&
                        item != it
                    ) stack = item;

            if (stack != null)
            {
                stack.count += it.count;
                Game.player.inventory.Remove(it);
                Game.Level.AllItems.Remove(it);
            }

        }

        public static void Open()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.door == Door.Closed)
            {
                t.door = Door.Open;
                Game.log.Add("You opened the door.");

                //counted as a movement action at the moment, based
                //on the dnd rules.
                Game.player.Pass(true);
            }
            else Game.log.Add("There's no closed door there.");
        }

        public static void Close()
        {
            string answer = Game.qpAnswerStack.Pop();
            if (answer.Length <= 0) return;

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.Level.Map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.door == Door.Open)
            {
                //first check if something's in the way
                if (Util.ItemsOnTile(t).Count <= 0)
                {
                    t.door = Door.Closed;
                    Game.log.Add("You closed the door.");

                    //counted as a movement action at the moment, based
                    //on the dnd rules.
                    Game.player.Pass(true);
                }
                else Game.log.Add("There's something in the way.");
            }
            else Game.log.Add("There's no open door there.");
        }

        public static void Zap()
        {
            string answer = Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;
            int i = IO.indexes.IndexOf(answer[0]);
            if (i >= Game.player.Spellbook.Count)
            {
                Game.log.Add("Invalid selection (" + answer[0] + ").");
                return;
            }
            else
            {
                Game.TargetedSpell = Game.player.Spellbook[i];
                IO.AskPlayer(
                    "Casting " + Game.player.Spellbook[i].Name,
                    InputType.Targeting,
                    PlayerResponses.Cast
                );
            }
        }

        public static void Use()
        {
            string answer = Game.qpAnswerStack.Peek();
            if (answer.Length <= 0) return;

            Item it = Game.player.inventory[
                IO.indexes.IndexOf(answer[0])
            ];
            if (it.count <= 0)
            {
                Game.log.Add(it.GetName(true, false, true) + " lacks charges.");
                return;
            }

            //nontargeted
            if (it.UseEffect.Range <= 0)
            {
                Projectile pr = it.UseEffect.Cast(
                    Game.player,
                    Game.player.xy
                );
                pr.Move();
                it.count--;
                if (it.count <= 0)
                {
                    Game.player.inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.log.Add(it.GetName(true, false, true) + " is spent!");
                }
                Game.player.Pass();
            }
            //targeted
            else
            {
                Game.TargetedSpell = it.UseEffect;
                IO.AskPlayer(
                    "Using " + it.GetName(true),
                    InputType.Targeting,
                    PlayerResponses.UseCast
                );
            }
        }

        public static void Cast()
        {
            int index = IO.indexes.IndexOf(Game.qpAnswerStack.Pop()[0]);
            if (Game.Level.Map[Game.Target.x, Game.Target.y] != null) {
                Game.player.Cast(
                    //Game.TargetedSpell, p
                    Game.TargetedSpell, Game.Target
                );
            }
            else
                Game.log.Add("Invalid target.");
        }

        public static void UseCast()
        {
            if (Game.Level.Map[Game.Target.x, Game.Target.y] != null)
            {
                int i = IO.indexes.IndexOf(Game.qpAnswerStack.Pop()[0]);
                Item it = Game.player.inventory[i];
                Game.log.Add("You use " + it.GetName(true) + ".");
                //Projectile pr = Game.TargetedSpell.Cast(Game.player, p);
                Projectile pr = Game.TargetedSpell.Cast(
                    Game.player,
                    Game.Target
                );
                pr.Move();
                //spend charge
                it.count--;
                //rechargin items should probably not trigger this
                //but that's a later issue
                if (it.count <= 0)
                {
                    Game.player.inventory.Remove(it);
                    Game.Level.AllItems.Remove(it);
                    Game.log.Add(it.GetName(true, false, true) + " is spent!");
                }
                Game.player.Pass();
            }
            else
                Game.log.Add("Invalid target.");
        }

        public static void Fire()
        {
            if (!Game.player.Vision[
                Game.Target.x, Game.Target.y
            ]) {
                Game.log.Add("You can't see that place.");
                return;
            }
            Actor a = Game.Level.ActorOnTile(Game.Target);
            if (a == null)
            {
                Game.log.Add("Nothing there to fire upon.");
                return;
            }

            Game.player.Shoot(a);
        }
    }
}
