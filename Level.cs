﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    [DataContract]
    public class LevelConnector
    {
        [DataMember] public Point Position;
        [DataMember] public int? Target;
        //public blabla theme, for more interesting forks

        public LevelConnector() { }

        public LevelConnector(Point position, Level target = null)
        {
            Position = position;
            Target = target == null ? (int?)null : target.ID;
        }

        public LevelConnector(string s)
        {
            ReadConnector(s);
        }

        public Stream WriteConnector()
        {
            Stream stream = new Stream();
            stream.Write("{", false);
            stream.Write(Position);
            stream.Write(Target);
            stream.Write("}", false);
            return stream;
        }

        public void ReadConnector(string s)
        {
            Stream stream = new Stream(s);
            Position = stream.ReadPoint();
            Target = stream.ReadNInt();
        }
    }

    [DataContract]
    public class Level
    {
        public static int IDCounter = 0;
        [DataMember] public string Name;
        [DataMember] public int ID;
        [DataMember] public int Depth;

        [DataMember] public Point Size;

        //notice: These are public, BUT, should in reality only ever be accesed
        //        by the TileInfo class. In part because it is neater that way,
        //        in part because it is ten thousand times as convenient.
        [DataMember] public Tile[,] Map;
        [DataMember] public bool[,] Seen;
        [DataMember] public bool[,] Blood;
        [DataMember] public List<Room> Rooms;
        [DataMember] public List<LevelConnector> Connectors; 

        public List<Actor> Actors
        {
            get
            {
                return World.Instance.WorldActors
                    .Where(a => a.LevelID == ID).ToList();
            }
        }
        public List<Item> WorldItems {
            get
            {
                return World.Instance.WorldItems
                    .Where(i => i.LevelID == ID).ToList();
            }
        }
        public List<Item> AllItems {
            get
            {
                return World.Instance.AllItems
                    .Where(i => i.LevelID == ID).ToList();
            }
        }

        public Level() { }

        public Level(
            int levelWidth, int levelHeight
        ) {
            Size = new Point(levelWidth, levelHeight);
            Clear();
            ID = IDCounter++;
            Connectors = new List<LevelConnector>();
        }
        public Level(string s)
        {
            LoadLevelSave(s);
        }

        public void Clear()
        {
            Map = new Tile[Size.x, Size.y];
            Seen = new bool[Size.x, Size.y];
            Blood = new bool[Size.x, Size.y];
            Rooms = new List<Room>();
        }

        public void See(Point p)
        {
            if (At(p) == null) return;
            if (Seen[p.x, p.y]) return;

            Game.UI.UpdateAt(p);
            Seen[p.x, p.y] = true;
        }

        public TileInfo At(Point p)
        {
            if (p.x >= Size.x ||
                p.y >= Size.y ||
                p.x < 0 ||
                p.y < 0)
                return null;

            if (Map[p.x, p.y] == null) return null;

            return new TileInfo(this)
            {
                Position = p,
                Items = ItemsOnTile(p),
                Actor = ActorOnTile(p)
            };
        }
        public TileInfo At(int x, int y)
        {
            return At(new Point(x, y));
        }

        public Tile TileAt(Point p)
        {
            return Map[p.x, p.y];
        }
        public Actor ActorOnTile(Tile t)
        {
            for (int x = 0; x < Size.x; x++)
                for (int y = 0; y < Size.y; y++)
                    //do like this while migrating
                    //LH-011214: wtf? ^
                    //           migrating what?
                    //LH-091214: And to this day, I still don't know what
                    //           the fuck that was about.
                    if (Map[x, y] == t)
                        return ActorOnTile(new Point(x, y));
            return null;
        }
        public Actor ActorOnTile(Point xy)
        {
            return World.Instance.WorldActors
                .Where(a => a.LevelID == ID)
                .FirstOrDefault(actor => actor.xy == xy);
        }
        public List<Item> ItemsOnTile(Point xy)
        {
            return World.Instance.WorldItems
                .Where(item => item.LevelID == ID)
                .Where(item => item.xy == xy).ToList();
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

        private int GetMuffleValue(TileInfo tile)
        {
            if (tile == null) return 12;
            if (tile.Solid) return 7;
            if (tile.Door == Door.Closed) return 6;
            return 0;
        }

        public void MakeNoise(Point p, NoiseType type, int noisemod = 0)
        {
            foreach (Actor a in World.Level.Actors)
            {
                List<Point> l = Util.Line(a.xy.x, a.xy.y, p.x, p.y);
                int obstruction = l
                    .Select(x => World.Level.At(x))
                    .Sum(ti => GetMuffleValue(ti));
                //only through walls, not void

                if (Util.Random.Next(1, 20)
                    + noisemod
                    - (obstruction)
                    - Util.XperY(1, 2, Util.Distance(p, a.xy))
                    >= 7)
                    a.Hear(type, p);
            }
        }

        public Stream WriteLevelSave(string path)
        {
            /*
             * Okay, so what do we need to write?
             * Header
                 * Name
                 * Dimensions
                 * Depth
                 * ID
                 * Connectors
             * Body
                 * Level itself
                 * Rooms
             */

            Stream stream = new Stream();

            stream.Write(Size);
            stream.Write(Name);
            stream.Write(Depth);
            stream.Write(ID, 2);
            stream.Write("{", false);
            foreach (LevelConnector c in Connectors)
                stream.Write(c.WriteConnector(), false);
            stream.Write("}", false);
            stream.Write("</HEADER>", false);

            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    if (Map[x, y] != null)
                    {
                        stream.Write(Map[x, y].WriteTile().ToString());
                        stream.Write(Seen[x, y]);
                        stream.Write(Blood[x, y]);
                    }
                    stream.Write("##", false);
                }
            stream.Write("</LEVEL>", false);

            foreach (Room room in Rooms)
            {
                stream.Write(room.WriteRoom() + "##", false);
            }
            stream.Write("</ROOMS>", false);

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

            Size = stream.ReadPoint();
            Clear();

            Name = stream.ReadString();
            Depth = stream.ReadInt();
            ID = stream.ReadHex(2);

            Connectors = new List<LevelConnector>();
            Stream connectors = new Stream(stream.ReadBlock());
            while(!connectors.AtFinish)
                Connectors.Add(new LevelConnector(connectors.ReadBlock()));

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

            for (int i = 0; i < Size.x * Size.y; i++)
            {
                int x = i % Size.x;
                int y = (i - (i % Size.x)) / Size.x;
                if (tiles[i] == "")
                    Map[x, y] = null;
                else
                {
                    Tile t = new Tile
                    {
                        Position = new Point(x, y),
                    };
                    Stream s = t.ReadTile(tiles[i]);
                    s.Read++; //skip the semicolon
                    Seen[x, y] = s.ReadBool();
                    Blood[x, y] = s.ReadBool();
                    Map[x, y] = t;
                }
            }

            string roomSection =
                content.Substring(read, content.Length - read).Split(
                    new[] {"</ROOMS>"},
                    StringSplitOptions.RemoveEmptyEntries
                )[0];
            // ReSharper disable once RedundantAssignment
            read += roomSection.Length + "</ROOMS>".Length;

            string[] rooms = roomSection.Split(
                new[] {"##"},
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string room in rooms)
                Rooms.Add(new Room(room));
        }

        public void Spawn(Actor actor)
        {
            if(actor.ID != 0)
                //no brain? give it one
                //'cuz we nice like that
                if(Game.Brains.All(b => b.MeatPuppet != actor))
                    Game.Brains.Add(new Brain(actor));

            actor.LevelID = ID;
            World.Instance.WorldActors.Add(actor);
        }
        public void Spawn(Item item)
        {
            item.LevelID = ID;

            bool stacked = false;
            if (item.Stacking)
            {
                List<Item> iot = ItemsOnTile(item.xy);
                Item stack = iot.FirstOrDefault(it => it.CanStack(item));
                if (stack != null)
                {
                    stack.Stack(item);
                    stacked = true;
                }
            }
            if (stacked) return;

            World.Instance.WorldItems.Add(item);
            World.Instance.AllItems.Add(item);
        }
        public void Despawn(Actor actor)
        {
            foreach (Item it in new List<Item>(actor.Inventory))
                Despawn(it);
            World.Instance.WorldActors.Remove(actor);
        }
        public void Despawn(Item item)
        {
            if (item.HasComponent<ContainerComponent>())
                foreach (Item it in InventoryManager.Containers[item.ID])
                    Despawn(it);
            World.Instance.WorldItems.Remove(item);
            World.Instance.AllItems.Remove(item);

            //properly despawn from every actor as well.
            foreach (BodyPart bp in Actors
                .SelectMany(a => a.PaperDoll)
                .Where(bp => bp.Item != null)
                .Where(bp => bp.Item.ID == item.ID))
                bp.Item = null;
            foreach (Actor a in Actors.Where(a => a.Inventory.Contains(item)))
                a.RemoveItem(item);
            foreach (Actor a in Actors.Where(a => a.Quiver != null))
                if (a.Quiver == item) a.Quiver = null;
        }

        public Room CreateRoom(
            Level level,
            Rect rect,
            TileDefinition floor,
            TileDefinition walls = null
        ) {
            return CreateRoom(level, new List<Rect>{ rect }, floor, walls);
        }
        public Room CreateRoom(
            Level level,
            List<Rect> rects,
            TileDefinition floor,
            TileDefinition walls = null
        ) {
            Room room = new Room(level);
            room.Rects.AddRange(rects);
            Rooms.Add(room);

            bool[,] drawn = new bool[Size.x, Size.y];
            foreach (Rect rect in rects)
            {
                for (int x = 0; x < rect.wh.x; x++)
                {
                    for (int y = 0; y < rect.wh.y; y++)
                    {
                        Map[rect.xy.x + x, rect.xy.y + y] = new Tile(floor)
                        {
                            Position =  new Point(
                                rect.xy.x + x,
                                rect.xy.y + y
                            )
                        };
                        drawn[rect.xy.x + x, rect.xy.y + y] = true;
                    }
                }
            }

            if (walls == null) return room;

            for (int x = 1; x < Size.x-1; x++)
            {
                for (int y = 1; y < Size.y-1; y++)
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

            return room;
        }

        public Point RandomOpenPoint() {

            List<TileInfo> tiles =
                Rooms.SelectMany(r => r.GetTiles())
                .Select(t => At(t.Position))
                .Where(t => !t.Solid)
                .Where(t => t.Door == Door.None)
                .Where(t => t.Stairs == Stairs.None)
                .Where(t => t.Actor == null)
                .Where(t => t.Items.Count == 0)
                .ToList();
            
            return tiles.SelectRandom().Position;
        }
    }
}
