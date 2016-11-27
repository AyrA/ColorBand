using System;
using System.Collections.Generic;
using System.Text;

namespace colorBand
{
    public struct StatusLine
    {
        public int Frame;
        public double FPS, Speed;
        public TimeSpan Time;

        public StatusLine(string Line)
        {
            bool skip = false;
            string Trimmed = string.Empty;

            Frame = 0;
            FPS = Speed = 0.0;
            Time = new TimeSpan(0L);

            if (Line.StartsWith("frame="))
            {
                foreach (char c in Line.Trim())
                {
                    if (skip)
                    {
                        if (c != ' ')
                        {
                            Trimmed += c;
                            skip = false;
                        }
                    }
                    else
                    {
                        if (c == '=')
                        {
                            skip = true;
                        }
                        Trimmed += c;
                    }
                }
                foreach (string Part in Trimmed.Split(' '))
                {
                    if (Part.Contains("="))
                    {
                        switch (Part.Split('=')[0])
                        {
                            case "time":
                                string[] Segments = Part.Split('=')[1].Split(':');

                                if (Segments.Length == 3)
                                {
                                    Time = new TimeSpan(
                                        TryInt(Segments[0], 0),
                                        TryInt(Segments[1], 0),
                                        TryInt(Segments[2].Split('.')[0], 0));
                                }

                                break;
                            case "frame":
                                int.TryParse(Part.Split('=')[1], out Frame);
                                break;
                            case "fps":
                                double.TryParse(Part.Split('=')[1], out FPS);
                                break;
                            case "speed":
                                double.TryParse(Part.Split('=')[1].Trim('x'), out Speed);
                                break;
                        }
                    }
                }
            }
        }

        private int TryInt(string s, int Default)
        {
            int i = 0;
            return int.TryParse(s, out i) ? i : Default;
        }
    }
}
