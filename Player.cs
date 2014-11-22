﻿using System;
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
            if(Game.logPlayerActions)
                Game.log.Add(" > Drop item");

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
                Game.worldItems.Add(it);
                it.xy = Game.player.xy;

                //actually make sure to unwield/unwear as well
                foreach (BodyPart bp in Game.player.PaperDoll)
                    if (bp.Item == it) bp.Item = null;

                Game.log.Add("Dropped " + it.name + ".");
                Game.player.Cooldown = 10;
            }
        }

        public static void Sheath(string answer)
        {
            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);
            if (i >= Game.player.inventory.Count)
            {
                Game.log.Add("Invalid selection (" + answer[0] + ").");
                return;
            }

            bool itemWielded = false;
            Item it = null;

            foreach (BodyPart bp in Game.player.PaperDoll.FindAll(
                x => x.Type == DollSlot.Hand))
                if (bp.Item == Game.player.inventory[i]) {
                    itemWielded = true;
                    it = bp.Item;
                }

            if (itemWielded)
            {
                foreach (BodyPart bp in Game.player.PaperDoll.FindAll(
                    x => x.Type == DollSlot.Hand))
                    if (bp.Item == it) bp.Item = null;
                Game.log.Add("Sheathed " + it.name + ".");
                Game.player.Cooldown = 10;
            }
        }

        public static void Wield(string answer)
        {
            if(Game.logPlayerActions)
                Game.log.Add(" > Wield item");

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
            foreach (DollSlot ds in selected.equipSlots)
            {
                if (!Game.player.HasFree(ds))
                {
                    canEquip = false;
                    Game.log.Add("You need to remove something first.");
                }
            }
            if (canEquip)
            {
                Game.log.Add("Equipped "+ selected.name + ".");
                Game.player.Cooldown = 10;
                Game.player.Equip(selected);
            }
        }

        public static void Get(string answer)
        {
            if(Game.logPlayerActions)
                Game.log.Add(" > Pick up item");

            if (answer.Length <= 0) return;

            int i = letterAnswerToIndex(answer[0]);
            
            List<Item> onTile = Util.ItemsOnTile(
                Game.player.xy
            );

            if (i < onTile.Count)
            {
                Item it = onTile[i];
                Game.player.inventory.Add(it);
                Game.worldItems.Remove(it);
                Game.log.Add("Picked up " + it.name + ".");
                Game.player.Cooldown = 10;
            }
            else
            {
                Game.log.Add("Invalid selection ("+answer[0]+").");
                return;
            }
        }

        public static void Open(string answer)
        {
            if(Game.logPlayerActions)
                Game.log.Add(" > Open door");

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.doorState == Door.Closed)
            {
                t.doorState = Door.Open;
                Game.log.Add("You opened the door.");
            }
            else
            {
                Game.log.Add("There's no closed door there.");
            }
        }

        public static void Close(string answer)
        {
            if(Game.logPlayerActions)
                Game.log.Add(" > Close door");

            Point offset = Game.NumpadToDirection(answer[0]);
            Tile t = 
                Game.map[
                    Game.player.xy.x + offset.x,
                    Game.player.xy.y + offset.y
                ];
            if (t.doorState == Door.Open)
            {
                //first check if something's in the way
                if (Util.ItemsOnTile(t).Count <= 0)
                {
                    t.doorState = Door.Closed;
                    Game.log.Add("You closed the door.");
                }
                else
                {
                    Game.log.Add("There's something in the way.");
                }
            }
            else
            {
                Game.log.Add("There's no open door there.");
            }
        }
    }
}
