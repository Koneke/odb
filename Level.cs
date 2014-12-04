using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public class Level
    {
        public string Name;

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
            int levelWidth, int levelHeight
        ) {
            LevelSize = new Point(levelWidth, levelHeight);
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
            Map = new Tile[LevelSize.x, LevelSize.y];
            Seen = new bool[LevelSize.x, LevelSize.y];
            Rooms = new List<Room>();

            WorldActors = new List<Actor>();
            WorldItems = new List<Item>();
            AllItems = new List<Item>();
        }

        public Actor ActorOnTile(Tile t)
        {
            for (int x = 0; x < LevelSize.x; x++)
                for (int y = 0; y < LevelSize.y; y++)
                    //do like this while migrating
                    //LH-011214: wtf? ^
                    //           migrating what?
                    if (Map[x, y] == t)
                        return ActorOnTile(
                            new Point(x, y)
                            );
            return null;
        }

        public Actor ActorOnTile(Point xy)
        {
            return WorldActors.
                FirstOrDefault(actor => actor.xy == xy);
        }

        public Actor ActorOnTile(int x, int y)
        {
            return ActorOnTile(new Point(x, y));
        }

        public List<Item> ItemsOnTile(Point xy)
        {
            return WorldItems.Where(item => item.xy == xy).ToList();
        }

        public List<Room> NeighbouringRooms(Room r)
        {
            List<Room> list = new List<Room>();
            foreach (Rect rr in r.Rects)
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

            foreach (Room r in Rooms.Where(r => r != null))
            {
                ActorPositions.Add(r, new List<Actor>());
                foreach (Actor a in WorldActors
                    //ReSharper disable once AccessToForEachVariableInClosure
                    //LH-011214: we're only /using/ the value here, so it's ok
                    .Where(a => r.ContainsPoint(a.xy)))
                {
                    ActorPositions[r].Add(a);
                    ActorRooms[a] = new List<Room> {r};
                }
            }
        }

        public void MakeNoise(int noisemod, Point p)
        {
            List<Room> closed = Util.GetRooms(p);
            List<Room> open = Util.GetRooms(p);

            Dictionary<int, List<Room>> roomdistances =
                new Dictionary<int, List<Room>>();
            int dist = 0;
            List<Room> newopen = new List<Room>();

            int maxDistance = 6 + noisemod - 1;
            maxDistance -= maxDistance % 2;
            maxDistance /= 2;

            while (dist <= maxDistance)
            {
                newopen.Clear();
                roomdistances[dist] = new List<Room>();
                foreach (Room r in open)
                {
                    roomdistances[dist].Add(r);
                    newopen.AddRange(
                        r.Linked.Where(
                            n => !closed.Contains(n) && !open.Contains(n))
                        );
                    closed.Add(r);
                }
                open.Clear();
                open.AddRange(newopen);
                dist++;
            }

            for (int distance = 0; distance <= maxDistance; distance++)
                foreach (
                    Actor actor in
                    from actor in (
                        from room in roomdistances[distance]
                        from actor in ActorPositions[room]
                        where actor != Util.Game.Player
                        select actor)
                    //ReSharper disable once AccessToModifiedClosure
                    //LH-011214: only reading the value (i), not changing it
                    let roll = Util.Roll("1d6") + noisemod - distance * 2 > 1
                    select actor
                )
            {
                actor.Awake = true;
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

            stream.Write(LevelSize);
            stream.Write(Name);
            stream.Write("</HEADER>", false);

            for (int y = 0; y < LevelSize.y; y++)
                for (int x = 0; x < LevelSize.x; x++)
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
                stream.Write(item.WriteItem() + "##", false);
            }
            stream.Write("</ITEMS>", false);

            foreach (Actor actor in WorldActors)
            {
                stream.Write(
                    actor.WriteActor() + "##", false);
            }
            stream.Write("</ACTORS>", false);

            SaveIO.WriteToFile(path, stream.ToString());
            return stream;
        }

        public void LoadLevelSave(string path)
        {
            string content = SaveIO.ReadFromFile(path);
            int read = 0;

            string dimensions =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</HEADER>"},
                    StringSplitOptions.None
                )[0];
            read += dimensions.Length + "</HEADER>".Length;

            Stream stream = new Stream(dimensions);

            LevelSize = stream.ReadPoint();
            Clear();

            Name = stream.ReadString();

            string levelSection =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</LEVEL>"},
                    StringSplitOptions.None
                )[0];
            read += levelSection.Length + "</LEVEL>".Length;

            string[] tiles = levelSection.Split(
                //do NOT remove empty entries, they are null tiles!
                new[] {"##"}, StringSplitOptions.None
            );

            for (int i = 0; i < LevelSize.x * LevelSize.y; i++)
            {
                int x = i % LevelSize.x;
                int y = (i - (i % LevelSize.x)) / LevelSize.x;
                if (tiles[i] == "")
                    Map[x, y] = null;
                else
                {
                    Tile t = new Tile();
                    Stream s = t.ReadTile(tiles[i]);
                    Seen[x, y] = s.ReadBool();
                    Map[x, y] = t;
                }
            }

            string roomSection =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</ROOMS>"},
                    StringSplitOptions.RemoveEmptyEntries
                )[0];
            read += roomSection.Length + "</ROOMS>".Length;

            string[] rooms = roomSection.Split(
                new[] {"##"},
                StringSplitOptions.RemoveEmptyEntries
                );

            foreach (string room in rooms)
                Rooms.Add(new Room(room));

            string itemSection =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</ITEMS>"},
                    StringSplitOptions.None
                )[0];
            read += itemSection.Length + "</ITEMS>".Length;

            string[] items = itemSection.Split(
                new[] {"##"},
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string item in items)
                AllItems.Add(new Item(item));
            WorldItems.AddRange(AllItems);

            string actorSection =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</ACTORS>"},
                    StringSplitOptions.None
                )[0];
            //LH-011214: it makes sense to keep this here even if "read"
            //           is not actually used after this point, since we might
            //           add on more sections later, and it'd be a dumb miss
            //           to miss on updating it when it then turns relevant ;)
            //ReSharper disable once RedundantAssignment
            read += actorSection.Length + "</ACTORS>".Length;

            string[] actorList = actorSection.Split(
                new[] {"##"},
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (Actor actor in actorList.Select(a => new Actor(a)))
            {
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

        public void Despawn(Item item)
        {
            WorldItems.Remove(item);
            AllItems.Remove(item);
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
            Room room = new Room();
            room.Rects.AddRange(rects);
            Rooms.Add(room);

            bool[,] drawn = new bool[LevelSize.x, LevelSize.y];
            foreach (Rect rect in rects)
            {
                for (int x = 0; x < rect.wh.x; x++)
                {
                    for (int y = 0; y < rect.wh.y; y++)
                    {
                        Map[rect.xy.x + x, rect.xy.y + y] = new Tile(floor);
                        drawn[rect.xy.x + x, rect.xy.y + y] = true;
                    }
                }
            }

            if (walls == null) return;

            for (int x = 1; x < LevelSize.x-1; x++)
            {
                for (int y = 1; y < LevelSize.y-1; y++)
                {
                    bool border =
                        drawn[x, y] &&
                        (
                            !drawn[x-1, y] || !drawn[x+1, y] ||
                            !drawn[x, y-1] || !drawn[x, y+1]
                        );

                    if(border) Map[x, y].Definition = walls;
                }
            }
        }
    }
}
