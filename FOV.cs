using System;
using System.Collections.Generic;

//from http://blogs.msdn.com/b/ericlippert/archive/2011/12/12/shadowcasting-in-c-part-one.aspx
//Partially tweaked by me, base still from there.

namespace ODB
{
    public static class ShadowCaster
    {
        public static void ShadowCast(
            Point origin,
            int radius,
            Func<Point, bool> isOpaque,
            Action<Point, double> see //point, distance
        ) {
            for (int octant = 0; octant < 8; ++octant)
            {
                ComputeOctant(
                    origin,
                    isOpaque,
                    see,
                    octant,
                    radius
                );
            }
        }

        private static void ComputeOctant(
            Point origin,
            Func<Point, bool> isOpaque,
            Action<Point, double> see,
            int octant,
            int radius
        ) {
            Queue<ColumnPortion> queue = new Queue<ColumnPortion>();

            queue.Enqueue(
                new ColumnPortion(
                    0,
                    new Point(1, 0),
                    new Point(1, 1)
                )
            );

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (current.X > radius)
                    continue;

                ComputeFoVForColumnPortion(
                    origin,
                    current.X,
                    current.TopVector,
                    current.BottomVector,
                    isOpaque,
                    see,
                    octant,
                    radius,
                    queue
                );
            }
        }

        private static void ComputeFoVForColumnPortion(
            Point origin,
            int x,
            Point topVector,
            Point bottomVector,
            Func<Point, bool> isOpaque,
            Action<Point, double> see,
            int octant,
            int radius,
            Queue<ColumnPortion> queue
        ) {
            int topY;

            if (x == 0)
                topY = 0;
            else
            {
                int quotient = (2 * x + 1) * topVector.y / (2 * topVector.x);
                int remainder = (2 * x + 1) * topVector.y % (2 * topVector.x);

                if (remainder > topVector.x)
                    topY = quotient + 1;
                else
                    topY = quotient;
            }

            int bottomY;
            if (x == 0)
                bottomY = 0;
            else
            {
                int quotient =
                    (2 * x - 1) * bottomVector.y / (2 * bottomVector.x);

                int remainder =
                    (2 * x - 1) * bottomVector.y % (2 * bottomVector.x);

                if (remainder >= bottomVector.x)
                    bottomY = quotient + 1;
                else
                    bottomY = quotient;
            }

            bool? wasLastCellOpaque = null;
            for (int y = topY; y >= bottomY; --y)
            {
                double pyth =
                    (2 * x - 1) * (2 * x - 1) + (2 * y - 1) * (2 * y - 1);
                bool inRadius =
                    /*(2 * x - 1) * (2 * x - 1) +
                    (2 * y - 1) * (2 * y - 1)*/
                    pyth
                    <=
                    4 * radius * radius;

                if (inRadius)
                {
                    see(
                        TranslateToOctant(
                            x, y, octant
                        ) + origin,
                        (pyth / 4) / radius
                    );
                }

                bool currentIsOpaque = !inRadius ||
                    isOpaque(
                        TranslateToOctant(
                            x, y, octant
                        ) + origin
                    );

                if (wasLastCellOpaque != null)
                {
                    if (currentIsOpaque)
                    {
                        if (!wasLastCellOpaque.Value)
                        {
                            queue.Enqueue(
                                new ColumnPortion(
                                    x + 1,
                                    new Point (x * 2 - 1, y * 2 + 1),
                                    topVector
                                )
                            );
                        }
                    }
                    else if (wasLastCellOpaque.Value)
                    {
                        topVector = new Point(x * 2 + 1, y * 2 + 1);
                    }
                }
                wasLastCellOpaque = currentIsOpaque;
            }

            if (wasLastCellOpaque != null && !wasLastCellOpaque.Value)
                queue.Enqueue(
                    new ColumnPortion(
                        x + 1,
                        bottomVector,
                        topVector
                    )
                );
        }

        private struct ColumnPortion
        {
            public int X { get; private set; }
            public Point BottomVector { get; private set; }
            public Point TopVector { get; private set; }

            public ColumnPortion(
                int x,
                Point bottom,
                Point top
            ) : this() {
                X = x;
                BottomVector = bottom;
                TopVector = top;
            }
        }

        private static Point TranslateToOctant(
            int x,
            int y,
            int octant
        ) {
            switch (octant)
            {
                default: return new Point(x, y);
                case 1: return new Point(y, x);
                case 2: return new Point(-y, x);
                case 3: return new Point(-x, y);
                case 4: return new Point(-x, -y);
                case 5: return new Point(-y, -x);
                case 6: return new Point(y, -x);
                case 7: return new Point(x, -y);
            }
        }
    }
}