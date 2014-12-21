using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ODB
{
    public class FOV
    {
        public void CastFrom(
            int ox, int oy, int range, Level level, Action<int, int> paint
        ) {
            Func<int, int, bool> isSolid =
                (x, y) =>
                level.At(x, y) == null ||
                level.At(x, y).Solid ||
                level.At(x, y).Door == Door.Closed
            ;

            Func<int, int, int, int, int> pyth =
                (x1, y1, x2, y2) =>
                (int)Math.Pow(x1 - x2, 2) + (int)Math.Pow(y1 - y2, 2);

            List<Point> visible = new List<Point>();

            for (int i = -range; i <= range; i++)
            {
                List<List<Point>> ls = new List<List<Point>>();
                ls.Add(Util.Line(ox, oy, ox+i, oy-range));
                ls.Add(Util.Line(ox, oy, ox+i, oy+range));
                ls.Add(Util.Line(ox, oy, ox-range, oy+i));
                ls.Add(Util.Line(ox, oy, ox+range, oy+i));
                ls.Add(Util.Line(ox+i, oy-range, ox, oy));
                ls.Add(Util.Line(ox+i, oy+range, ox, oy));
                ls.Add(Util.Line(ox-range, oy+i, ox, oy));
                ls.Add(Util.Line(ox+range, oy+i, ox, oy));
                
                foreach (List<Point> l in ls)
                {
                    if(l[0].x != ox || l[0].y != oy)
                        l.Reverse();
                    int j = 0;
                    while (j < l.Count && !isSolid(l[j].x, l[j].y))
                        visible.Add(l[j++]);
                    if(j < l.Count)
                        visible.Add(l[j]);
                }
            }

            visible.ForEach(p => paint(p.x, p.y));
        }

        public void ShadowCast2(
            int ox,
            int oy,
            int range,
            Func<int, int, bool> isSolid,
            Action<int, int> paint
        ) {
            Func<int, int, double> pyth =
                (x1, y1) => (int)(Math.Pow(x1, 2) + Math.Pow(y1, 2));

            Func<int, int, int, int, double> slope =
                (x1, y1, x2, y2) => (double)(y2 - y1) / (x2 - x1);

            bool? lastWasSolid = null;
            List<Portion> blockers = new List<Portion>();

            //one, not zero. I assume we can see ourselves...
            for (int x = 1; x < range; x++)
            {
                int y = x;
                int top = y;
                int bot = 0;
                while (y >= 0)
                {
                    if (pyth(x, y) >= Math.Pow(range, 2))
                    { 
                        y--; continue;
                    }

                    if (lastWasSolid ?? false)
                    {
                        //solid => transparent
                        if (!isSolid(x, y))
                        {
                            bot = y + 1;
                            blockers.Add(new Portion(x, top + 0.5, bot - 0.5));
                        }
                    }
                    else
                    {
                        //transparent => solid
                        if (isSolid(x, y))
                        {
                            top = y;
                        }
                    }

                    lastWasSolid = isSolid(x, y);
                    y--;
                }

                //last cell was solid
                if(isSolid(x, y + 1))
                    blockers.Add(new Portion(x, top, y + 1));
            }

            List<Point> shaded = new List<Point>();

            foreach (Portion p in blockers)
            {
                double topSlope = (p.Top - oy) / (p.X - ox);
                double botSlope = (p.Bottom - oy) / (p.X - ox);

                for (int x = p.X; x < range; x++)
                {
                    int y = x;
                    while (y >= 0)
                    {
                        double s = slope(ox, oy, x, y);
                        if (s <= topSlope &&
                            s >= botSlope)
                            shaded.Add(new Point(x, y));
                        y--;
                    }
                }
            }

            var a = 0;
        }

        internal class Portion
        {
            public int X;
            public double Top;
            public double Bottom;

            public Portion(int x, double top, double bot)
            {
                X = x;
                Top = top;
                Bottom = bot;
            }
        }
    }
}