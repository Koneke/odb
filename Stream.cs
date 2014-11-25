using System;
using Microsoft.Xna.Framework;

namespace ODB
{
    public class Stream
    {
        string stream;
        public int read;

        public Stream()
        {
            read = 0;
        }

        public Stream(string content)
        {
            stream = content;
            read = 0;
        }

        public override string ToString()
        {
            return stream;
        }

        public void Write(Color c)
        {
            string s = "";
            s += String.Format("{0:X2}", c.R);
            s += String.Format("{0:X2}", c.G);
            s += String.Format("{0:X2}", c.B);
            read += s.Length;
            stream += s;
        }

        public void Write(Color? c)
        {
            string s = "";
            if (c.HasValue) Write(c.Value);
            else s += "XXXXXX";
            read += s.Length;
            stream += s;
        }

        public void Write(Point p)
        {
            string s = p.x + "x" + p.y + ";";
            read += s.Length;
            stream += s;
        }

        public void Write(string ss, bool delimit = true)
        {
            string s = ss + (delimit ? ";" : "");
            read += s.Length;
            stream += s;
        }

        public void Write(int i, int len)
        {
            string s = IO.WriteHex(i, len);
            read += s.Length;
            stream += s;
        }

        public void Write(bool b)
        {
            string s = IO.Write(b);
            read += s.Length;
            stream += s;
        }

        public Color ReadColor()
        {
            Color c = new Color(
                Int32.Parse(stream.Substring(read, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(stream.Substring(read + 2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(stream.Substring(read + 4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            read += 6;
            return c;
        }

        public Color? ReadNullableColor()
        {
            if (stream.Substring(read, 6).Contains("X"))
            {
                read += 6;
                return null;
            }
            return ReadColor();
        }

        public Point ReadPoint()
        {
            string s = stream.Substring(read, stream.Length - read);
            s = s.Split(';')[0];
            Point p = new Point(
                int.Parse(s.Split('x')[0]),
                int.Parse(s.Split('x')[1])
            );
            read += s.Length + 1;
            return p;
        }

        public string ReadString(int length)
        {
            string s = stream.Substring(read, length);
            read += length;
            return s;
        }

        public string ReadString()
        {
            string s = stream.Substring(read, stream.Length - read);
            s = s.Split(';')[0];
            read += s.Length + 1;
            return s;
        }

        public int ReadHex(int length)
        {
            string s = stream.Substring(read, stream.Length - read);
            s = s.Substring(0, length);
            read += length;
            return IO.ReadHex(s);
        }

        public bool ReadBool()
        {
            return stream.Substring(read++, 1) == "1";
        }
    }
}
