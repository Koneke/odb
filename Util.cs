using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

/*
 * Mainly for just storing general, loose utility functions.
 * Classes/enums/structs live in Header.cs
 */

namespace ODB
{
    public class Util
    {
        public static Game1 Game;
        public static Random Random = new Random();

        public static Item GetItemByID(int id)
        {
            return Game.allItems.Find(x => x.id == id);
        }

        public static Actor GetActorByID(int id)
        {
            return Game.worldActors.Find(x => x.id == id);
        }

        public static List<Room> GetRooms(Point xy)
        {
            List<Room> roomList = new List<Room>();
            foreach (Room r in Game.rooms)
                if (r.ContainsPoint(xy)) roomList.Add(r);
            return roomList;
        }

        public static List<Room> GetRooms(gObject go)
        {
            return GetRooms(go.xy);
        }

        public static List<Actor> ActorsOnTile(Point xy)
        {
            return Game.worldActors.FindAll(x => x.xy == xy);
        }

        public static List<Actor> ActorsOnTile(Tile t)
        {
            for (int x = 0; x < Game.lvlW; x++)
                for (int y = 0; y < Game.lvlH; y++)
                    if (Game.map[x, y] == t)
                        return Game.worldActors.
                            FindAll(z => z.xy == new Point(x, y));
            return null;
        }

        public static List<Item> ItemsOnTile(Point xy)
        {
            return Game.worldItems.FindAll(x => x.xy == xy);
        }

        public static List<Item> ItemsOnTile(Tile t)
        {
            for (int x = 0; x < Game.lvlW; x++)
                for (int y = 0; y < Game.lvlH; y++)
                    if (Game.map[x, y] == t)
                        return Game.worldItems.
                            FindAll(z => z.xy == new Point(x, y));
            return null;
        }

        public static int Roll(string s)
        {
            s = s.ToLower();
            int number = int.Parse(s.Split('d')[0]);
            int sides = int.Parse(s.Split('d')[1]
                .Split(new char[]{'-', '+'})[0]
            );
            int mod = 0;
            if(s.Contains("+") || s.Contains("-"))
                mod = s.Contains("+") ?
                    int.Parse(s.Split('+')[1]) :
                    -int.Parse(s.Split('-')[1])
                ;
            return Roll(
                number,
                sides,
                mod
            );
        }

        public static int Roll(int number, int sides, int mod = 0)
        {
            int sum = 0;
            for (int i = 0; i < number; i++)
                sum += Random.Next(1, sides + 1);
            return sum + mod;
        }

        class node
        {
            public node parent;
            public List<node> children;
            public node(node parent)
            {
                this.parent = parent;
                children = new List<node>();
            }
        }

        public static Point NextGoalOnRoute(Point xy, List<Room> Route)
        {
            if (Route == null) //honey, I'm home!
                return xy;
            int n = 0;
            while (true)
            {
                foreach (Room r in GetRooms(xy))
                {
                    foreach (Rect rr in r.rects)
                    {
                        for (int x = 0; x < rr.wh.x; x++)
                        {
                            for (int y = 0; y < rr.wh.y; y++)
                            {
                                Point np = new Point(
                                    rr.xy.x + x,
                                    rr.xy.y + y
                                );
                                if (Route[n].ContainsPoint(np))
                                {
                                    //if we're actually going anywhere...
                                    if (np != xy) return np;
                                    else { n++; break; };
                                }
                            }
                        }
                    }
                }
            }
            //if this every happens, you've been given a bad map son
            throw new Exception("Bad route, dying.");
        }

        public static List<Room> FindRouteToPoint(
            Point source,
            Point destination
        ) {
            List<Room> route = new List<Room>();
            List<Room> closed = new List<Room>();
            List<Room> open = new List<Room>();

            //only adding the first, because we don't really need to
            //find a path through the tile we're already standing on lol
            open.Add(GetRooms(source)[0]);

            foreach (Room r in GetRooms(source))
                if (GetRooms(destination).Contains(r))
                    //already in the right room
                    //obviously not all we need to do,
                    //but enough for now
                    return null;

            Dictionary<node, Room> path = new Dictionary<node, Room>();
            Dictionary<Room, node> htap = new Dictionary<Room, node>();

            node head = new node(null);
            node current = head;
            htap[open[0]] = head;
            path[head] = open[0];

            List<Room> sharingTiles = new List<Room>();
            bool foundTarget = false;
            while (open.Count > 0 && !foundTarget)
            {
                Room r = open[0];
                current = htap[r];

                if (GetRooms(destination).Contains(r))
                    foundTarget = true;
                else
                {
                    foreach (Rect rr in r.rects)
                        for (int x = 0; x < rr.wh.x; x++)
                            for (int y = 0; y < rr.wh.y; y++)
                            {
                                //what rooms this tile is included in
                                List<Room> rooms = GetRooms(
                                    new Point(rr.xy.x + x, rr.xy.y + y)
                                );
                                rooms.RemoveAll(z =>
                                    open.Contains(z) || closed.Contains(z)
                                );
                                foreach (Room c in rooms)
                                {
                                    node n = new node(current);
                                    path[n] = c;
                                    htap[c] = n;
                                }
                                sharingTiles.AddRange(rooms);
                            }
                    closed.Add(r);
                }

                open.AddRange(sharingTiles);
                open.RemoveAll(x => closed.Contains(x));
                //in the future, sort the open list so we actually find
                //the fastest path, not just /any/ path ;)
            }

            while (current.parent != null)
            {
                route.Add(path[current]);
                current = current.parent;
            }
            route.Reverse();

            return route;
        }

        public static List<Item> GetWornItems(Actor a)
        {
            List<Item> list = new List<Item>();
            foreach(BodyPart bp in a.PaperDoll)
                if (bp.Item != null)
                    if (!list.Contains(bp.Item))
                        list.Add(bp.Item);
            return list;
        }

        public static List<Mod> GetModsOfType(ModType mt, List<Item> items)
        {
            List<Mod> list = new List<Mod>();
            foreach (Item it in items)
                foreach (Mod m in it.Mods)
                    if (m.Type == mt)
                        list.Add(m);
            return list;
        }

        public static Color InvertColor(Color c) {
            return new Color(
                (byte)255 - c.R,
                (byte)255 - c.G,
                (byte)255 - c.B
            );
        }
    }
}
