using System;
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

        //should not be saved
        public Dictionary<Room, List<Actor>> ActorPositions;
        public Dictionary<Actor, List<Room>> ActorRooms;

        public Level(
            int LevelWidth, int LevelHeight
        ) {
            this.LevelWidth = LevelWidth;
            this.LevelHeight = LevelHeight;
            this.LevelSize = new Point(LevelWidth, LevelHeight);
            Clear();
            ActorPositions = new Dictionary<Room, List<Actor>>();
            ActorRooms = new Dictionary<Actor, List<Room>>();
        }

        public Level(string s)
        {
            LoadLevelSave(s);
            ActorPositions = new Dictionary<Room, List<Actor>>();
            ActorRooms = new Dictionary<Actor, List<Room>>();
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

        public List<Room> NeighbouringRooms(Room r)
        {
            List<Room> list = new List<Room>();
            foreach (Rect rr in r.rects)
                for (int x = 0; x < rr.wh.x; x++)
                    for (int y = 0; y < rr.wh.y; y++)
                        list.AddRange(
                            Util.GetRooms(
                                new Point(rr.xy.x + x, rr.xy.y + y)
                            ).FindAll(
                                z => z != r && !list.Contains(z)
                            )
                        );
            return list;
        }

        public void CalculateRoomLinks()
        {
            foreach (Room r in Rooms)
            {
                r.Linked.Clear();
                r.Linked.AddRange(NeighbouringRooms(r));
            }
        }

        public void CalculateActorPositions()
        {
            ActorPositions.Clear();
            ActorRooms.Clear();
            foreach (Actor a in WorldActors)
                ActorRooms.Add(a, new List<Room>());

            foreach (Room r in Rooms)
            {
                ActorPositions.Add(r, new List<Actor>());
                foreach (Actor a in WorldActors)
                    if (r.ContainsPoint(a.xy))
                    {
                        ActorPositions[r].Add(a);
                        ActorRooms[a] = new List<Room>();
                        ActorRooms[a].Add(r);
                    }
            }
        }

        public void MakeNoise(int noisemod, Point p)
        {
            List<Room> closed = new List<Room>();
            List<Room> open = new List<Room>();

            closed = Util.GetRooms(p);
            open = Util.GetRooms(p);
            Dictionary<int, List<Room>> roomdistances =
                new Dictionary<int,List<Room>>();
            int dist = 0;
            List<Room> newopen = new List<Room>();

            int maxdist = 6 + noisemod - 1;
            maxdist -= maxdist % 2;
            maxdist /= 2;

            while (dist <= maxdist)
            {
                newopen.Clear();
                roomdistances[dist] = new List<Room>();
                foreach (Room r in open)
                {
                    roomdistances[dist].Add(r);
                    foreach (Room n in r.Linked)
                        if (!closed.Contains(n) && !open.Contains(n))
                            newopen.Add(n);
                    closed.Add(r);
                }
                open.Clear();
                open.AddRange(newopen);
                dist++;
            }

            for (int i = 0; i <= maxdist; i++)
                foreach (Room r in roomdistances[i])
                    foreach (Actor a in ActorPositions[r])
                    {
                        if (a == Util.Game.player) continue;
                        int roll = Util.Roll("1d6") + noisemod - i * 2;
                        if (roll > 1) a.Awake = true;
                    }
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
            stream.Write(Name);
            stream.Write("</HEADER>", false);

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
                    new string[] { "</HEADER>" },
                    StringSplitOptions.None
                )[0];
            read += dimensions.Length + "</HEADER>".Length;

            Stream stream = new Stream(dimensions);

            //Point levelSize = IO.ReadPoint(dimensions);
            Point levelSize = stream.ReadPoint();
            LevelWidth = levelSize.x;
            LevelHeight = levelSize.y;
            LevelSize = new Point(LevelWidth, LevelHeight);
            Clear();

            Name = stream.ReadString();

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
                    Tile t = new Tile();
                    Stream s = t.ReadTile(tiles[i]);
                    Seen[x, y] = s.ReadBool();
                    Map[x, y] = t;
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
                foreach (Item item in actor.Inventory)
                    WorldItems.Remove(item);
            }
        }

        public void Spawn(Item item)
        {
            WorldItems.Add(item);
            AllItems.Add(item);
        }

        public void CreateRoom(
            Rect rect,
            TileDefinition floor,
            TileDefinition walls = null
        ) {
            CreateRoom(new List<Rect>{ rect }, floor, walls);
        }

        public void CreateRoom(
            List<Rect> rects,
            TileDefinition floor,
            TileDefinition walls = null
        ) {
            Room R = new Room();
            R.rects.AddRange(rects);
            Rooms.Add(R);

            bool[,] drawn = new bool[LevelSize.x, LevelSize.y];
            foreach (Rect r in rects)
            {
                for (int x = 0; x < r.wh.x; x++)
                {
                    for (int y = 0; y < r.wh.y; y++)
                    {
                        Map[r.xy.x + x, r.xy.y + y] = new Tile(floor);
                        drawn[r.xy.x + x, r.xy.y + y] = true;
                    }
                }
            }

            if (walls == null) return;

            for (int x = 1; x < LevelSize.x-1; x++)
            {
                for (int y = 1; y < LevelSize.y-1; y++)
                {
                    bool border = false;
                    if(
                        drawn[x, y] &&
                        (
                            !drawn[x-1, y] ||
                            !drawn[x+1, y] ||
                            !drawn[x, y-1] ||
                            !drawn[x, y+1]
                        )
                    ) {
                        //do overlap check here like old wall rendering
                        //so we can draw a room on top of a corridor
                        //without closing the corridor
                        border = true;
                    }

                    if(border) Map[x, y].Definition = walls;
                }
            }
        }
    }
}
