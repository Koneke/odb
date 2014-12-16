using System;
using Microsoft.Xna.Framework;

namespace ODB
{
    public class Stream
    {
        string _stream;

        //ReSharper disable once UnusedMember.Local
        //Makes debugging easier, since we can see how far we have read.
        private string SoFar 
        {
            get { return _stream.Substring(0, Read); }
        }

        public int Read;

        public Stream()
        {
            Read = 0;
        }

        public Stream(string content)
        {
            _stream = content;
            Read = 0;
        }

        public bool AtFinish { get { return Read == _stream.Length; } }

        public override string ToString()
        {
            return _stream;
        }

        public void Write(Color c)
        {
            string s = "";
            s += String.Format("{0:X2}", c.R);
            s += String.Format("{0:X2}", c.G);
            s += String.Format("{0:X2}", c.B);
            Read += s.Length;
            _stream += s;
        }

        public void Write(Color? c)
        {
            string s = "";
            if (c.HasValue) Write(c.Value);
            else s += "XXXXXX";
            Read += s.Length;
            _stream += s;
        }

        public void Write(Point p)
        {
            string s = p.x + "x" + p.y + ";";
            Read += s.Length;
            _stream += s;
        }

        public void Write(string ss, bool delimit = true)
        {
            string s = ss + (delimit ? ";" : "");
            Read += s.Length;
            _stream += s;
        }

        public void Write(int i, int len)
        {
            string s = IO.WriteHex(i, len);
            Read += s.Length;
            _stream += s;
        }

        public void Write(int i)
        {
            string s = i + ";";
            Read += s.Length;
            _stream += s;
        }

        public void Write(int? i)
        {
            string s;
            if (i.HasValue) s = i + ";";
            else s = "X;";
            Read += s.Length;
            _stream += s;
        }

        public void Write(bool b)
        {
            string s = IO.Write(b);
            Read += s.Length;
            _stream += s;
        }

        public void Write(Stream s, bool delimit = true)
        {
            Write(s.ToString(), delimit);
        }

        public void Tab()
        {
            Write("\t", false);
        }

        public Color ReadColor()
        {
            Color c = new Color(
                Int32.Parse(_stream.Substring(Read, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(_stream.Substring(Read + 2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(_stream.Substring(Read + 4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            Read += 6;
            return c;
        }

        public Color? ReadNullableColor()
        {
            if (!_stream.Substring(Read, 6).Contains("X"))
                return ReadColor();

            Read += 6;
            return null;
        }

        public Point ReadPoint()
        {
            string s = _stream.Substring(Read, _stream.Length - Read);
            s = s.Split(';')[0];
            Point p = new Point(
                int.Parse(s.Split('x')[0]),
                int.Parse(s.Split('x')[1])
            );
            Read += s.Length + 1;
            return p;
        }

        public string ReadString(int length)
        {
            string s = _stream.Substring(Read, length);
            Read += length;
            return s;
        }

        public string ReadString()
        {
            string s = _stream.Substring(Read, _stream.Length - Read);
            s = s.Split(';')[0];
            Read += s.Length + 1;
            return s;
        }

        public int ReadHex(int length)
        {
            string s = _stream.Substring(Read, _stream.Length - Read);
            s = s.Substring(0, length);
            Read += length;
            return IO.ReadHex(s);
        }

        public int ReadInt()
        {
            string s = ReadString();
            return int.Parse(s);
        }

        public int? ReadNInt()
        {
            string s = ReadString();
            if (s.Contains("X")) return null;
            return int.Parse(s);
        }

        public bool ReadBool()
        {
            return _stream.Substring(Read++, 1) == "1";
        }

        public string ReadBlock(
            char opener = '{', char closer = '}'
        ) {
            string s = _stream.Substring(Read, _stream.Length - Read);

            if(s[0] != opener) throw new ArgumentException();

            int depth = 1;
            int i = 1;
            while (depth > 0)
            {
                if (s[i] == opener) depth++;
                if (s[i] == closer) depth--;
                i++;
            }
            string ss = s.Substring(1, i - 2);
            Read += ss.Length + 2;

            return ss;
        }

        public string ReadTo(string target)
        {
            string s = _stream.Substring(Read, _stream.Length - Read);
            s = s.Split(
                new[] { target },
                StringSplitOptions.RemoveEmptyEntries)[0];

            Read += s.Length + target.Length;

            return s;
        }

        public void Back(int length = 1)
        {
            _stream = _stream.Substring(0, _stream.Length - length);
            Read -= length;
        }
    }
}
