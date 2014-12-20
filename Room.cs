using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    [DataContract]
    public class Room
    {
        [DataMember] public List<Rect> Rects;
        [DataMember] private int _level;

        public Level Level
        {
            get { return World.LevelByID(_level); }
            set { _level = value.ID; }
        }

        //should not be saved to file
        public List<Room> Linked;

        public Room() { }

        public Room(Level l)
        {
            Rects = new List<Rect>();
            Linked = new List<Room>();
            Level = l;
        }

        public Room(string s)
        {
            ReadRoom(s);
            Linked = new List<Room>();
        }

        public bool ContainsPoint(Point p)
        {
            return Rects.Any(r => r.ContainsPoint(p));
        }

        public List<Tile> GetTiles()
        {
            List<Tile> tiles = new List<Tile>();
            foreach (Rect rect in Rects)
                for (int x = 0; x < rect.wh.x; x++)
                    for (int y = 0; y < rect.wh.y; y++)
                        tiles.Add(Level.At(rect.xy + new Point(x, y)).Tile);
            return tiles;
        }

        //should probably switch to stream for ease
        public string WriteRoom()
        {
            string output = "";
            output += IO.WriteHex(Rects.Count, 2);
            foreach (Rect r in Rects)
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
            Rects = new List<Rect>();
            for (int i = 0; i < numRects; i++)
            {
                Rects.Add(new Rect(
                    stream.ReadPoint(),
                    stream.ReadPoint()
                    )
                    );
            }
            return stream;
        }
    }
}