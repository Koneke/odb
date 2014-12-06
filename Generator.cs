using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public struct Connector
    {
        public GenRoom Parent;

        public Point Position;
        public int Direction;

        public Connector(GenRoom parent, Point p, int direction)
        {
            Parent = parent;
            Position = p;
            Direction = direction;
        }
    }

    public class GenRoom
    {
        public Point Position;
        public Point Size;
        public List<Connector> Connectors;

        public Rect Rect()
        {
            return new Rect(Position, Size);
        }

        public GenRoom()
        {
            Connectors = new List<Connector>();
        }

        public void RemoveCoveredConnectors(List<GenRoom> others )
        {
            Connectors.RemoveAll(
                connector => others.Any(
                    other => other.Rect().ContainsPoint(connector.Position)
                    )
                );
        }

        public bool TryAttachTo(Generator gen, GenRoom other, List<GenRoom> rooms)
        {
            List<GenRoom> obstacles = rooms
                .Where(room => room != other && room != this)
                .ToList();

            Dictionary<Connector, List<Connector>> connectorPairs =
                new Dictionary<Connector, List<Connector>>();

            Rect screen = new Rect(new Point(1, 4), new Point(78, 19));

            Connectors
                .ForEach(myConnector =>
                    connectorPairs.Add(
                        myConnector,
                        other.Connectors
                            //0 - up, 2 - down. 1 - right, 3 - left.
                            .Where(otherConnector =>
                                (myConnector.Direction + otherConnector.Direction)
                                    % 2 == 0)
                            .Where(otherConnector =>
                                otherConnector.Direction != myConnector.Direction)
                            .Where(otherConnector =>
                                !Generator.Covers(
                                    //not us or the one we try connecting to
                                    obstacles,
                                    new Rect(
                                        //connector positions are offsets from room pos
                                        other.Position +
                                            otherConnector.Position -
                                            myConnector.Position,
                                        Size
                                        )
                                    )
                            )
                            .Where(otherConnector =>
                                otherConnector.Parent != myConnector.Parent)
                            //todo: DRY in some smart way
                            .Where(otherConnector =>
                                screen.Contains(
                                    new Rect(
                                        other.Position +
                                            otherConnector.Position -
                                            myConnector.Position,
                                        Size
                                        )
                                    )
                            )
                            .ToList()
                        )
                );

            List<Connector> empty = connectorPairs
                .Where(kvp => connectorPairs[kvp.Key].Count == 0)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (Connector key in empty)
                connectorPairs.Remove(key);

            if (connectorPairs.Count <= 0) return false;

            //select a random of our connectors with friends in the other room
            Connector mc = connectorPairs.Keys.ToList()
                [Util.Random.Next(0, connectorPairs.Count)];
            //select a random friend
            Connector oc = connectorPairs
                [mc]
                [Util.Random.Next(0, connectorPairs[mc].Count)];

            Position = other.Position + oc.Position - mc.Position;
            //place a door at the joint
            gen.Doors.Add(other.Position + oc.Position);

            gen.Connections.Add(new KeyValuePair<Connector, Connector>(oc, mc));

            if(Position.x < 6 || Position.y < 6)
                Game1.Game.WizMode = true;

            return true;
        }
    }

    public class Generator
    {
        public Point RollSize()
        {
            int r = Util.Random.Next(0, 5);
            const int min = 5;
            const int max = 8;
            if(r == 0)
                return new Point(
                    Util.Random.Next(min, max),
                    Util.Random.Next(3, 3)
                    );
            if(r == 1)
                return new Point(
                    Util.Random.Next(3, 3),
                    Util.Random.Next(min, Math.Max(max-2, min))
                    );
            return new Point(
                Util.Random.Next(min, max),
                Util.Random.Next(min, Math.Max(max-2, min))
                );
        }

        public GenRoom GenerateRoom(List<GenRoom> others = null)
        {
            GenRoom gr = new GenRoom { Size = RollSize() };

            const int border = 1;

            for (int x = border; x < gr.Size.x - border; x++)
            {
                gr.Connectors.Add(new Connector(gr, new Point(x, 0), 0));
                int y = gr.Size.y - 1;
                gr.Connectors.Add(new Connector(gr, new Point(x, y), 2));
            }

            for (int y = border; y < gr.Size.y - border; y++)
            {
                gr.Connectors.Add(new Connector(gr, new Point(0, y), 3));
                int x = gr.Size.x - 1;
                gr.Connectors.Add(new Connector(gr, new Point(x, y), 1));
            }

            return gr;
        }

        public static bool Covers(List<GenRoom> rooms, Rect rect)
        {
            return rooms.Any(
                room => room.Rect().Interects(rect)
                );
        }

        //tiles to generate doors instead of walls on
        public List<Point> Doors;
        public List<GenRoom> Rooms;
        public List<KeyValuePair<Connector, Connector>> Connections; 

        //todo: Current issues:
        //      Sometimes generates nonconnected rooms, and keeps working on
        //        those (resulting in two separate room "blobs").
        //      Occasionally generates doors in corners, which should not be
        //        possible.
        public Level Generate()
        {
            Level newLevel = new Level(80, 25);

            Rooms = new List<GenRoom>();
            Doors = new List<Point>();
            Connections = new List<KeyValuePair<Connector, Connector>>();

            GenRoom root;
            Rooms.Add(root = GenerateRoom());
            root.Position = new Point(40, 12);
            root.Position -= root.Size / 2;

            for (int i = 0; i < 30; i++)
            {
                GenRoom n;
                Rooms.Add(n = GenerateRoom());

                List<GenRoom> candidates = new List<GenRoom>();
                candidates.AddRange(Rooms);
                candidates.Remove(n);
                candidates = candidates.Shuffle();

                bool connected = false;
                while (candidates.Count > 0 && !connected)
                {
                    connected = n.TryAttachTo(this, candidates[0], Rooms);
                    if (!connected) candidates.RemoveAt(0);
                }
            }

            //foreach(GenRoom g in rooms)
            foreach (GenRoom room in Rooms)
                newLevel.CreateRoom(
                    room.Rect(),
                    TileDefinition.Definitions[0],
                    TileDefinition.Definitions[1]
                    );

            foreach (Point p in Doors)
            {
                newLevel.Map[p.x, p.y].Definition =
                    TileDefinition.Definitions[0];
                newLevel.Map[p.x, p.y].Door = Door.Closed;
            }

            return newLevel;
        }
    }
}