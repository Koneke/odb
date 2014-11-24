using System;
using System.Collections.Generic;

namespace ODB
{
    public class Level
    {
        public int LevelWidth, LevelHeight;

        public Tile[,] Map;
        public bool[,] Seen;
        public List<Room> Rooms;

        public List<Actor> WorldActors;
        public List<Item> WorldItems;
        public List<Item> AllItems; //WorldItems + stuff in inventories

        public Level(
            int LevelWidth, int LevelHeight
        ) {
            this.LevelWidth = LevelWidth;
            this.LevelHeight = LevelHeight;

            Map = new Tile[LevelWidth, LevelHeight];
            Seen = new bool[LevelWidth, LevelHeight];
            Rooms = new List<Room>();

            WorldActors = new List<Actor>();
            WorldItems = new List<Item>();
            AllItems = new List<Item>();
        }

        public List<Actor> ActorsOnTile(Tile tile)
        {
            for(int x = 0; x < LevelWidth; x++)
                for (int y = 0; y < LevelHeight; y++)
                    //if (Map[x, y] == tile)
                    //do like this while migrating
                    if (Util.Game.Level.Map[x, y] == tile)
                        return ActorsOnTile(new Point(x, y));
            return null;
        }

        public List<Actor> ActorsOnTile(Point xy)
        {
            List<Actor> actors = new List<Actor>();
            foreach (Actor actor in WorldActors)
                if (actor.xy == xy)
                    actors.Add(actor);
            return actors;
        }

        public List<Item> ItemsOnTile(Point xy)
        {
            List<Item> items = new List<Item>();
            foreach (Item item in WorldItems)
                if (item.xy == xy)
                    items.Add(item);
            return items;
        }
    }
}
