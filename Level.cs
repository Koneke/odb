﻿using System;
using System.Collections.Generic;

namespace ODB
{
    public class Level
    {
        public string Name;

        public int LevelWidth, LevelHeight;
        public Point LevelSize;

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
            this.LevelSize = new Point(LevelWidth, LevelHeight);
            Clear();
        }

        public Level(string s)
        {
            LoadLevelSave(s);
        }

        public void Clear()
        {
            Map = new Tile[LevelWidth, LevelHeight];
            Seen = new bool[LevelWidth, LevelHeight];
            Rooms = new List<Room>();

            WorldActors = new List<Actor>();
            WorldItems = new List<Item>();
            AllItems = new List<Item>();
        }

        public Actor ActorOnTile(Tile t)
        {
            for(int x = 0; x < LevelWidth; x++)
                for (int y = 0; y < LevelHeight; y++)
                    //do like this while migrating
                    if (Map[x, y] == t) return ActorOnTile(
                        new Point(x, y)
                    );
            return null;
        }

        public Actor ActorOnTile(Point xy)
        {
            foreach (Actor actor in WorldActors)
                if (actor.xy == xy)
                    return actor;
            return null;
        }

        public List<Item> ItemsOnTile(Point xy)
        {
            List<Item> items = new List<Item>();
            foreach (Item item in WorldItems)
                if (item.xy == xy)
                    items.Add(item);
            return items;
        }

        public Stream WriteLevelSave(string path)
        {
            /*
             * Okay, so what do we need to write?
             * Header
                 * Name
                 * Dimensions
                 * (future: our own id)
             * Body
                 * Level itself
                 * Rooms
                 * Actors
                 * Items
             */

            Stream stream = new Stream();

            //should probably be a point not only here really
            Point levelSize = new Point(LevelWidth, LevelHeight);

            stream.Write(levelSize);
            stream.Write("</DIMENSIONS>", false);

            for (int y = 0; y < levelSize.y; y++)
                for (int x = 0; x < levelSize.x; x++)
                {
                    if (Map[x, y] != null)
                    {
                        stream.Write(Map[x, y].WriteTile().ToString());
                        stream.Write(Seen[x, y]);
                    }
                    stream.Write("##", false);
                }
            stream.Write("</LEVEL>", false);

            foreach (Room room in Rooms)
            {
                stream.Write(room.WriteRoom() + "##", false);
            }
            stream.Write("</ROOMS>", false);

            foreach (Item item in AllItems)
            {
                stream.Write(item.WriteItem().ToString() + "##", false);
            }
            stream.Write("</ITEMS>", false);

            foreach (Actor actor in WorldActors)
            {
                stream.Write(
                    actor.WriteActor().ToString() + "##", false);
            }
            stream.Write("</ACTORS>", false);

            IO.WriteToFile(path, stream.ToString());
            return stream;
        }

        public void LoadLevelSave(string path)
        {
            string content = IO.ReadFromFile(path);
            int read = 0;

            string dimensions =
                content.Substring(read, content.Length - read).Split(
                    new string[] { "</DIMENSIONS>" },
                    StringSplitOptions.None
                )[0];
            read += dimensions.Length + "</DIMENSIONS>".Length;

            Point levelSize = IO.ReadPoint(dimensions);
            LevelWidth = levelSize.x;
            LevelHeight = levelSize.y;
            LevelSize = new Point(LevelWidth, LevelHeight);
            Clear();

            string levelSection =
                content.Substring(read, content.Length - read).Split(
                    new string[] { "</LEVEL>" },
                    StringSplitOptions.None
                )[0];
            read += levelSection.Length + "</LEVEL>".Length;

            string[] tiles = levelSection.Split(
                //do NOT remove empty entries, they are null tiles!
                new string[]{"##"}, StringSplitOptions.None
            );

            for (int i = 0; i < levelSize.x * levelSize.y; i++)
            {
                int x = i % levelSize.x;
                int y = (i - (i % levelSize.x)) / levelSize.x;
                if(tiles[i] == "")
                    Map[x, y] = null;
                else {
                    string tile = tiles[i].Split(';')[0];
                    Map[x, y] = new Tile(tile);
                    Seen[x, y] = IO.ReadBool(tiles[i].Split(';')[1]);
                }
            }

            string roomSection = 
                content.Substring(read, content.Length - read).Split(
                    new string[] { "</ROOMS>" },
                    StringSplitOptions.RemoveEmptyEntries
                )[0];
            read += roomSection.Length + "</ROOMS>".Length;

            string[] rooms = roomSection.Split(
                new string[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            );

            for (int i = 0; i < rooms.Length; i++)
                Rooms.Add(new Room(rooms[i]));

            string itemSection =
                content.Substring(read, content.Length - read).Split(
                    new string[] { "</ITEMS>" },
                    StringSplitOptions.None
                )[0];
            read += itemSection.Length + "</ITEMS>".Length;

            string[] items = itemSection.Split(
                new string[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            );

            for (int i = 0; i < items.Length; i++)
                AllItems.Add(new Item(items[i]));
            WorldItems.AddRange(AllItems);

            string actorSection =
                content.Substring(read, content.Length - read).Split(
                    new string[] { "</ACTORS>" },
                    StringSplitOptions.None
                )[0];
            read += actorSection.Length + "</ACTORS>".Length;

            string[] actorList = actorSection.Split(
                new string[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            );

            for (int i = 0; i < actorList.Length; i++)
            {
                Actor actor = new Actor(actorList[i]);
                WorldActors.Add(actor);
                foreach (Item item in actor.inventory)
                    WorldItems.Remove(item);
            }
        }

        public void Spawn(Item item)
        {
            WorldItems.Add(item);
            AllItems.Add(item);
        }
    }
}
