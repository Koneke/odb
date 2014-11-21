using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static List<Actor> ActorsOnTile(Point xy)
        {
            return Game.actors.FindAll(x => x.xy == xy);
        }

        public static List<Actor> ActorsOnTile(Tile t)
        {
            for (int x = 0; x < Game.lvlW; x++)
                for (int y = 0; y < Game.lvlH; y++)
                    if (Game.map[x, y] == t)
                        return Game.actors.
                            FindAll(z => z.xy == new Point(x, y));
            return null;
        }

        public static List<Item> ItemsOnTile(Point xy)
        {
            return Game.items.FindAll(x => x.xy == xy);
        }

        public static List<Item> ItemsOnTile(Tile t)
        {
            for (int x = 0; x < Game.lvlW; x++)
                for (int y = 0; y < Game.lvlH; y++)
                    if (Game.map[x, y] == t)
                        return Game.items.
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
    }
}
