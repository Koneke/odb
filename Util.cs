using System;
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
        //todo: find the odd refs to this hanging around
        //      due to me being a lazy bum
        public static Game1 Game;
        public static Random Random = new Random();

        public static Item GetItemByID(int id)
        {
            return Game.Level.AllItems.Find(x => x.id == id);
        }

        public static Actor GetActorByID(int id)
        {
            return Game.Level.WorldActors.Find(x => x.id == id);
        }

        public static ItemDefinition IDefByName(string name)
        {
            for (int i = 0; i < 0xFFFF; i++)
                if (ItemDefinition.ItemDefinitions[i] != null)
                    if (ItemDefinition.ItemDefinitions[i].name == name)
                        return ItemDefinition.ItemDefinitions[i];
            return null;
        }

        public static ActorDefinition ADefByName(string name)
        {
            for (int i = 0; i < 0xFFFF; i++)
                if (ActorDefinition.ActorDefinitions[i] != null)
                    if (ActorDefinition.ActorDefinitions[i].name == name)
                        return ActorDefinition.ActorDefinitions[i];
            return null;
        }

        public static Spell SpellByName(string name)
        {
            for (int i = 0; i < 0xFFFF; i++)
                if (Spell.Spells[i] != null)
                    if (Spell.Spells[i].Name == name)
                        return Spell.Spells[i];
            return null;
        }

        public static TickingEffectDefinition TEDefByName(string name)
        {
            for (int i = 0; i < 0xFFFF; i++)
                if (TickingEffectDefinition.Definitions[i] != null)
                    if (TickingEffectDefinition.Definitions[i].Name == name)
                        return TickingEffectDefinition.Definitions[i];
            return null;
        }

        public static List<Room> GetRooms(Point xy)
        {
            List<Room> roomList = new List<Room>();
            foreach (Room r in Game.Level.Rooms)
                if (r.ContainsPoint(xy)) roomList.Add(r);
            return roomList;
        }

        public static List<Room> GetRooms(gObject go)
        {
            return GetRooms(go.xy);
        }

        public static List<Item> ItemsOnTile(Point xy)
        {
            return Game.Level.WorldItems.FindAll(x => x.xy == xy);
        }

        public static List<Item> ItemsOnTile(Tile t)
        {
            for (int x = 0; x < Game.Level.LevelSize.x; x++)
                for (int y = 0; y < Game.Level.LevelSize.y; y++)
                    if (Game.Level.Map[x, y] == t)
                        return Game.Level.WorldItems.
                            FindAll(z => z.xy == new Point(x, y));
            return null;
        }

        public static int Roll(string s, bool max = false)
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

        public static int Roll(
            int number,
            int sides,
            int mod = 0,
            bool max = false)
        {
            int sum = 0;
            for (int i = 0; i < number; i++)
                if (!max)
                    sum += Random.Next(1, sides + 1);
                else
                    sum += sides;
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

        public static string article(string name)
        {
            //not ENTIRELY correct, whatwith exceptions,
            //but close enough.
            return
                new List<char>() { 'a', 'e', 'i', 'o', 'u' }
                    .Contains(name.ToLower()[0]) ?
                "an" : "a";
        }

        public static int Distance(Point a, Point b)
        {
            int dx = Math.Abs(a.x - b.x);
            int dy = Math.Abs(a.y - b.y);
            return Math.Max(dx, dy);
        }

        public static string Capitalize(string s)
        {
            return
                s.Substring(0, 1).ToUpper() +
                s.Substring(1, s.Length - 1);
        }
    }
}
