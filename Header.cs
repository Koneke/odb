﻿using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ODB
{
    public struct Point
    {
        public int x, y;
        
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Nudge(int x, int y)
        {
            this.x += x;
            this.y += y;
        }

        public void Nudge(Point p)
        {
            this += p;
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y);
        }
    }

    public struct Rect
    {
        public Point xy, wh;

        public Rect(Point xy, Point wh)
        {
            this.xy = xy;
            this.wh = wh;
        }

        public bool ContainsPoint(Point p)
        {
            return
                p.x >= xy.x &&
                p.y >= xy.y &&
                p.x < xy.x + wh.x &&
                p.y < xy.y + wh.y;
        }
    }

    public class Room
    {
        public List<Rect> rects;

        //should not be saved to file
        public List<Room> Linked;

        public Room()
        {
            rects = new List<Rect>();
            Linked = new List<Room>();
        }

        public Room(string s)
        {
            ReadRoom(s);
            Linked = new List<Room>();
        }

        public bool ContainsPoint(Point p)
        {
            foreach (Rect r in rects)
            {
                if (r.ContainsPoint(p)) return true;
            }
            return false;
        }

        //should probably switch to stream for ease
        public string WriteRoom()
        {
            string output = "";
            output += IO.WriteHex(rects.Count, 2);
            foreach (Rect r in rects)
            {
                output += IO.Write(r.xy);
                output += IO.Write(r.wh);
            }
            return output;
        }

        public Stream ReadRoom(string s)
        {
            Stream stream = new Stream(s);
            int numRects = stream.ReadHex(2);
            rects = new List<Rect>();
            for (int i = 0; i < numRects; i++)
            {
                rects.Add(new Rect(
                    stream.ReadPoint(),
                    stream.ReadPoint()
                    )
                );
            }
            return stream;
        }
    }

    public enum DollSlot
    {
        Head,
        //Eyes,
        //Face,
        //Neck,
        Torso,
        //Gloves, //maybe?
        Hand,
        Legs,
        Feet,
        Quiver
    }

    public class BodyPart
    {
        public DollSlot Type;
        public Item Item;
        public BodyPart(DollSlot Type, Item Item = null)
        {
            this.Type = Type;
            this.Item = Item;
        }
    }

    class Pair<T, S>
    {
        T first;
        S second;

        public Pair(T a, S b)
        {
            first = a;
            second = b;
        }
    }

    public enum ModType
    {
        AddStr, DecStr,
        AddDex, DecDex,
        AddInt, DecInt,
        AddSpd, DecSpd,
        AddQck, DecQck,
        AddHPm, DecHPm,
        PoisonRes
    }

    public class Mod {
        public ModType Type;
        public int Value;
        public Mod(ModType Type, int Value)
        {
            this.Type = Type;
            this.Value = Value;
        }
    }

    public class Spell
    {
        public static Spell[] Spells = new Spell[0xFFFF];
        public static int IDCounter = 0;
        public int id;

        public string Name;
        //0 should mean self-cast..? (or just non-targetted)
        //projectile should explode without moving, so should be on self
        public int Range;
        public int Cost;
        public int CastDifficulty;
        public List<Action<Actor, Point>> Effects;

        public Spell(
            string Name,
            List<Action<Actor, Point>> Effects,
            int Cost = 0,
            int CastDifficulty = 0,
            int Range = 0
        ) {
            this.id = IDCounter++;
            this.Name = Name;
            this.Range = Range;
            this.Cost = Cost;
            this.CastDifficulty = CastDifficulty;
            this.Effects = Effects;

            Spells[id] = this;
        }

        public Projectile Cast(Actor caster, Point target)
        {
            Projectile p = new Projectile();
            if(Effects != null)
                p.Effects = new List<Action<Actor, Point>>(Effects);
            p.Caster = caster;
            p.origin = caster.xy;
            p.Delta = target - caster.xy;
            //don't try to go further than we targeted
            p.Range = Math.Max(
                Math.Abs(p.Delta.x),
                Math.Abs(p.Delta.y)
            );
            //and don't go longer than allowed
            //that should probably be handled somewhere else though
            if (p.Range > Range) p.Range = Range;
            return p;
        }
    }

    //should probably make this a gObj?
    public class Projectile
    {
        public int Range;
        public int Moved;
        public bool Die;
        public Point xy;
        public Point origin;
        public Point Delta;
        public List<Action<Actor, Point>> Effects;
        public Actor Caster;

        void calculatePosition()
        {
            float deltaRatio;
            int offs;

            int x, y;

            if(Delta.x == 0) {
                xy = new Point(
                    origin.x,
                    origin.y + Math.Sign(Delta.y) * Moved
                );
                return;
            }
            else if(Delta.y == 0) {
                xy = new Point(
                    origin.x + Math.Sign(Delta.x) * Moved,
                    origin.y
                );
                return;
            }

            if (Delta.x > Delta.y)
            {
                deltaRatio = (float)Math.Abs(Delta.x) / Math.Abs(Delta.y);
                offs = (int)Math.Floor(deltaRatio / 2);

                x = Moved + offs;
                y = (int)((x - (x % deltaRatio)) / deltaRatio);

                xy = origin + new Point(
                    Math.Sign(Delta.x) * Moved,
                    Math.Sign(Delta.y) * y
                );
            }
            else if (Delta.y > Delta.x)
            {
                deltaRatio = (float)Math.Abs(Delta.y) / Math.Abs(Delta.x);
                offs = (int)Math.Floor(deltaRatio / 2);

                y = Moved + offs;
                x = (int)((y - (y % deltaRatio)) / deltaRatio);

                xy = origin + new Point(
                    Math.Sign(Delta.x) * x,
                    Math.Sign(Delta.y) * Moved
                );
            }
            else
            {
                xy = origin + new Point(
                    Math.Sign(Delta.x) * Moved,
                    Math.Sign(Delta.y) * Moved
                );
            }
        }

        public void Move()
        {
            calculatePosition();

            if (Moved < Range) {
                Moved++;
                calculatePosition();
            }
            if (Util.Game.Level.Map[xy.x, xy.y] == null)
            {
                Moved--;
                calculatePosition();
                Die = true;
            }
            else if (Util.Game.Level.Map[xy.x, xy.y].solid)
            {
                //unmove
                Moved--;
                calculatePosition();
                Die = true;
            }
            if (Moved >= Range) Die = true;

            if(!Die)
                if (Util.Game.Level.ActorOnTile(xy) != null)
                    Die = true; //explode without unmoving

            if (Die)
                foreach(Action<Actor, Point> effect in Effects)
                    effect(Caster, xy);

            if (!Die) Move();
        }
    }

}
