using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODB
{
    public class Player
    {
        public static Game1 Game;

        public static int letterAnswerToIndex(char c)
        {
            int i;
            if (c >= 97 && c <= 122) //lower case
                i = c - 97;
            else //upper case
                i = c - 39;
            return i; 
        }

        public static void Drop(string answer)
        {
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);

            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            if (i < Game.player.inventory.Count)
            {
                Item it = Game.player.inventory[i];

                Game.player.inventory.Remove(it);
                Game.items.Add(it);
                it.xy = Game.player.xy;

                //actually make sure to unwield/unwear as well
                foreach (dollSlot ds in it.equipSlots)
                    if(Game.player.paperDoll[ds] == it)
                        Game.player.paperDoll[ds] = null;

                Game.log.Add("Dropped " + it.name + ".");
            }
        }

        public static void Wield(string answer)
        {
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);

            List<Item> equipables = new List<Item>();

            foreach (Item it in Game.player.inventory)
                //is it equipable?
                if (it.equipSlots.Count > 0)
                    equipables.Add(it);

            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            if (!equipables.Contains(Game.player.inventory[i]))
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }

            Item selected = Game.player.inventory[i];
            bool canEquip = true;
            foreach (dollSlot ds in selected.equipSlots)
            {
                //something in the slot? => no equip
                if (Game.player.paperDoll[ds] != null)
                {
                    canEquip = false;
                    Game.log.Add("Already using that slot.");
                }
            }
            if (canEquip)
            {
                Game.log.Add("Equipped "+ selected.name + ".");
                foreach (dollSlot ds in selected.equipSlots)
                {
                    Game.player.paperDoll[ds] = selected;
                }
            }
        }

        public static void Get(string answer)
        {
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);
            
            List<Item> onTile = Game.ItemsOnTile(
                Game.player.xy
            );

            if (i < onTile.Count)
            {
                Item it = onTile[i];
                Game.player.inventory.Add(it);
                Game.items.Remove(it);
                Game.log.Add("Picked up " + it.name + ".");
            }
            else
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }
        }
    }
}
