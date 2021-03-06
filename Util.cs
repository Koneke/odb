﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Console = SadConsole.Consoles.Console;

/*
 * Mainly for just storing general, loose utility functions.
 * Classes/enums/structs live in Header.cs
 */

namespace ODB
{
    public static class Extensions
    {
        public static List<T> Shuffle<T>(this List<T> l)
        {
            //we create a new one everytime so the shuffle returns
            //the same result every time for the same input and gameseed
            Random rng = new Random(Game.Seed);
            //so we only return a shuffled copy instead
            List<T> list = new List<T>(l);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static T SelectRandom<T>(this List<T> l)
        {
            return l[Util.Random.Next(0, l.Count)];
        }

        public static T SelectRandom<T>(this IEnumerable<T> l)
        {
            return l.ElementAt(Util.Random.Next(0, l.Count()));
        }

        public static IEnumerable<T> TakeLast<T>(
            this List<T> coll,
            int n
        ) {
            return coll.Skip(Math.Max(0, coll.Count() - n)).Take(n);
        }

        public static List<string> NeatSplit(
            this string s,
            string split,
            bool removeEmpty = true
        ) {
            return s.Split(new []{ split }, removeEmpty
                ? StringSplitOptions.RemoveEmptyEntries
                : StringSplitOptions.None)
            .ToList();
        }

        public static void DrawColorString(
            this Console console,
            int x, int y,
            ColorString cs
        ) {
            for (int j = -1; j < cs.ColorPoints.Count; j++)
            {
                int current = j == -1
                    ? 0
                    : cs.ColorPoints[j].Item1;

                int next = j == cs.ColorPoints.Count - 1
                    ? cs.String.Length
                    : cs.ColorPoints[j + 1].Item1;

                console.CellData.Print(
                    x + current, y,
                    cs.String.Substring(current, next - current),
                    j == -1
                        ? Color.White
                        : cs.ColorPoints[j].Item2
                );
            }
        }

        public static void DrawColorString(
            this Console console,
            int x, int y,
            string s
        ) {
            foreach(string split in s.NeatSplit("\\n"))
                console.DrawColorString(x, y, new ColorString(split));
        }

        public static int GetWidth(this Console console)
        {
            return console.ViewArea.Width;
        }

        public static int GetHeight(this Console console)
        {
            return console.ViewArea.Height;
        }

        public static void Paint<T>(this T[,] arr, Rect area, T value)
        {
            for (int x = 0; x < area.wh.x; x++)
            for (int y = 0; y < area.wh.y; y++)
                arr[area.xy.x + x, area.xy.y + y] = value;
        }

        public static void Fill<T>(this T[,] arr, T value)
        {
            for (int x = 0; x < arr.GetLength(0); x++)
            for (int y = 0; y < arr.GetLength(1); y++)
                arr[x, y] = value;
        }
    }

    public class Util
    {
        public static Random Random;

        public static void SetSeed(int seed)
        {
            Random = new Random(seed);
        }

        public static Item GetItemByID(int id)
        {
            return World.Instance.AllItems.Find(x => x.ID == id);
        }

        public static Actor GetActorByID(int id)
        {
            return World.Instance.WorldActors.Find(x => x.ID == id);
        }

        public static ItemDefinition ItemDefByName(string name)
        {
            return ItemDefinition.DefDict 
                .FirstOrDefault(kvp =>
                    kvp.Value.Name.ToLower() == name.ToLower())
                .Value;
        }

        public static ActorDefinition ADefByName(string name)
        {
            return ActorDefinition.DefDict 
                .FirstOrDefault(kvp =>
                    kvp.Value.Name.ToLower() == name.ToLower())
                .Value;
        }

        public static Spell SpellByName(string name)
        {
            return Spell.SpellDict
                .FirstOrDefault(kvp =>
                    kvp.Value.Name.ToLower() == name.ToLower())
                .Value;
        }

        public static TickingEffectDefinition
            TickingEffectDefinitionByName(string name)
        {
            for (int i = 0; i < 0xFFFF; i++)
                if (TickingEffectDefinition.Definitions[i] != null)
                    if (TickingEffectDefinition.Definitions[i].Name == name)
                        return TickingEffectDefinition.Definitions[i];
            return null;
        }

        //just terser this way
        public static TileDefinition TileDefinitionByName(int id)
        {
            return TileDefinition.Definitions[id];
        }

        public static List<Room> GetRooms(Point xy)
        {
            return World.Level.Rooms.Where(r => r.ContainsPoint(xy)).ToList();
        }

        public static int Roll(string s, bool max = false)
        {
            if (s == "") return 0;

            s = s.ToLower();
            int number = int.Parse(s.Split('d')[0]);
            int sides = int.Parse(s.Split('d')[1]
                .Split(new[]{'-', '+'})[0]
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
                mod,
                max
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

        class Node
        {
            public readonly Node Parent;
            public Node(Node parent)
            {
                Parent = parent;
            }
        }

        public static Point NextGoalOnRoute(Point xy, List<Room> route)
        {
            if (route == null) //honey, I'm home!
                return xy;
            int n = 0;
            //todo: euuh, this looks n_a_s_t_y...
            //      but it hasn't crashed yet...
            while (true)
            {
                foreach (Rect rr in GetRooms(xy).SelectMany(r => r.Rects))
                {
                    for (int x = 0; x < rr.wh.x; x++)
                    {
                        for (int y = 0; y < rr.wh.y; y++)
                        {
                            Point np = new Point(
                                rr.xy.x + x,
                                rr.xy.y + y
                            );
                            if (!route[n].ContainsPoint(np)) continue;
                            if (World.Level.At(np).Solid) continue;

                            //if we're actually going anywhere...
                            if (np != xy) return np;
                            n++; break;
                        }
                    }
                }
            }
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

            if (GetRooms(source).Any(
                    room => GetRooms(destination).Contains(room)))
                return null;

            Dictionary<Node, Room> path = new Dictionary<Node, Room>();
            Dictionary<Room, Node> htap = new Dictionary<Room, Node>();

            Node head = new Node(null);
            Node current = head;
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
                    foreach (Rect rr in r.Rects)
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
                                    Node n = new Node(current);
                                    path[n] = c;
                                    htap[c] = n;
                                }
                                sharingTiles.AddRange(rooms);
                            }
                    closed.Add(r);
                }

                open.AddRange(sharingTiles);
                open.RemoveAll(closed.Contains);
                //in the future, sort the open list so we actually find
                //the fastest path, not just /any/ path ;)
            }

            while (current.Parent != null)
            {
                route.Add(path[current]);
                current = current.Parent;
            }
            route.Reverse();

            return route;
        }

        public static List<Item> GetWornItems(Actor a)
        {
            List<Item> list = new List<Item>();
            foreach (
                BodyPart bp in
                    a.PaperDoll
                    .Where(bp => bp.Item != null)
                    .Where(bp => !list.Contains(bp.Item)))
                list.Add(bp.Item);
            return list;
        }

        public static List<Mod> GetModsOfType(ModType mt, List<Item> items)
        {
            return (
                from it in items
                from m in it.Mods
                where m.Type == mt
                select m
            ).ToList();
        }

        public static List<Mod> GetModsOfType(ModType mt, Actor a)
        {
            return a.Intrinsics
                .Where(m => m.Type == mt)
                .ToList();
        }

        public static Color InvertColor(Color c) {
            return new Color(
                255 - c.R,
                255 - c.G,
                255 - c.B
            );
        }

        public static string Article(string name)
        {
            //not ENTIRELY correct, whatwith exceptions,
            //but close enough.
            return
                new List<char> { 'a', 'e', 'i', 'o', 'u' }
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

        public static AttackType ReadAttackType(string s)
        {
            switch (s.ToLower())
            {
                case "at_slash": return AttackType.Slash;
                case "at_pierce": return AttackType.Pierce;
                case "at_bash": return AttackType.Bash;
                case "at_bite": return AttackType.Bite;
                default: throw new ArgumentException();
            }
        }
        public static string WriteAttackType(AttackType type)
        {
            switch (type)
            {
                case AttackType.Slash: return "at_slash";
                case AttackType.Pierce: return "at_pierce";
                case AttackType.Bash: return "at_bash";
                case AttackType.Bite: return "at_bite";
                default: throw new ArgumentException();
            }
        }

        public static DamageType ReadDamageType(string s)
        {
            switch (s.ToLower())
            {
                case "dt_physical": return DamageType.Physical;
                case "dt_ratking": return DamageType.Ratking;
                default: throw new ArgumentException();
            }
        }

        public static string WriteDamageType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return "dt_physical";
                case DamageType.Ratking: return "dt_ratking";
                default: throw new ArgumentException();
            }
        }

        //for every y points of source, return x
        //e.g., 1 per 2, strength, one point for every two points of strength
        public static int XperY(int x, int y, int source)
        {
            int result = source - (source % y);
            result /= y;
            result *= x;
            return result;
        }

        public static Point NumpadToDirection(char c)
        {
            Point p;
            switch (c)
            {
                case 'y': case (char)Keys.D7: p = new Point(-1, -1); break;
                case 'k': case (char)Keys.D8: p = new Point(0, -1); break;
                case 'u': case (char)Keys.D9: p = new Point(1, -1); break;
                case 'h': case (char)Keys.D4: p = new Point(-1, 0); break;
                case 'l': case (char)Keys.D6: p = new Point(1, 0); break;
                case 'b': case (char)Keys.D1: p = new Point(-1, 1); break;
                case 'j': case (char)Keys.D2: p = new Point(0, 1); break;
                case 'n': case (char)Keys.D3: p = new Point(1, 1); break;
                case (char)Keys.D5: p = new Point(0, 0); break;
                default: throw new Exception(
                        "Bad input (expected numpad keycode, " +
                        "got something weird instead).");
            }
            return p;
        }

        //both below picked from:
        //http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static List<Point> Line(int x0, int y0, int x1, int y1)
        {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
            if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

            int dX = (x1 - x0);
            int dY = Math.Abs(y1 - y0);
            int err = (dX / 2);
            int ystep = y0 < y1 ? 1 : -1;
            int y = y0;

            List<Point> line = new List<Point>();

            for (int x = x0; x <= x1; ++x)
            {
                line.Add(steep
                    ? new Point(y, x)
                    : new Point(x, y)
                );
                err = err - dY;
                if (err < 0)
                {
                    y += ystep;
                    err += dX;
                }
            }

            return line;
        }

        public static void QuickTarget()
        {
            List<Actor> visible =
                World.Level.Actors
                .Where(a => a != Game.Player)
                .Where(a => Game.Player.Sees(a.xy))
                .ToList();

            if(visible.Count > 0)
                IO.Target =
                    visible
                    .OrderBy(a => Distance(a.xy, Game.Player.xy))
                    .Select(a => a.xy)
                    .First();
        }
    }
}
