using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo_AC2016
{
    internal class SharedFunctions
    {
        private const string Inipath = @"C:\EGS\Geo_AC2016.ini";

        internal static short AciColor(char c)
        {
            short retval = 256;//bylayer
            switch (c)
            {
                case '1'://red
                    retval = 1; break;
                case '2'://l.green
                    retval = 3; break;
                case '3'://blue
                    retval = 5; break;
                case '4'://yellow
                    retval = 2; break;
                case '5'://magneta
                    retval = 6; break;
                case '6'://cyan
                    retval = 4; break;
                case '7'://d.green
                    retval = 96; break;
                case '8'://purple
                    retval = 192; break;
                case '9'://orange
                    retval = 30; break;
                default:
                    break;
            }
            return retval;
        }

        internal static float Calc_HDG(double x0, double y0, double x1, double y1)
        {
            double dx = x1 - x0;
            double dy = y1 - y0;
            float hdg;

            switch (true)
            {
                case true when dx == 0: // on y-axis or both zero
                    if (dy < 0) return 180; else return 0;
                case true when dy == 0: // on x-axis
                    if (dx > 0) return 90; else return 270;
                default:
                    if (dx < 0 && dy > 0)
                        hdg = (float)(450 - Math.Atan2(dy, dx) * (180 / Math.PI));
                    else
                        hdg = (float)(90 - Math.Atan2(dy, dx) * (180 / Math.PI));
                    break;
            }
            if (hdg > 180.0) hdg -= 360; // +/- 180 deg, north = zero
            return hdg;
        }

        internal static string CleanLayerName(string inputstr)
        {
            inputstr = inputstr.Replace("<", "$");
            inputstr = inputstr.Replace(">", "$");
            inputstr = inputstr.Replace("/", "$");
            inputstr = inputstr.Replace("\\", "$");
            inputstr = inputstr.Replace(":", "$");
            inputstr = inputstr.Replace(";", "$");
            inputstr = inputstr.Replace("?", "$");
            inputstr = inputstr.Replace("*", "$");
            inputstr = inputstr.Replace("|", "$");
            inputstr = inputstr.Replace(",", "$");
            inputstr = inputstr.Replace("=", "$");
            inputstr = inputstr.Replace("`", "$");
            return inputstr;
        }

        internal static double HD(double x0, double y0, double x1, double y1)
        {
            return System.Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        }

        internal static double IPX(double Px, double Py, RPL R)
        {
            if (R.A == 0) return Px; //dY=0 when horizontal line segment directly return Px
            if (R.B == 0) return R.E; //dX=0 when vertical line segment directly return RPLx
            return ((R.B * ((R.B * Px) - (R.A * Py)) - (R.A * R.C)) / R.AB2);
        }

        internal static double IPY(double Px, double Py, RPL R)
        {
            if (R.A == 0) return R.N; //dY = 0 when horizontal line segment directly return RPLy
            if (R.B == 0) return Py; //dX = 0 when vertical line segment directly return Py
            return ((R.A * ((R.A * Py) - (R.B * Px)) - (R.B * R.C)) / R.AB2);
        }

        //internal static double IPXO(double px, double py, double m, double c, bool isVertical)
        //{
        //    if (isVertical) return c;
        //    return (px + (m * py) - (m * c)) / ((m * m) + 1);
        //}

        //internal static double IPYO(double py, double ipx, double m, double c, bool isVertical)
        //{
        //    if (isVertical) return py;
        //    return (ipx * m) + c;
        //}

        internal static bool IsNumeric(string inputstr) => int.TryParse(inputstr, out _);

        internal static bool IsNumericA(string value) => value.All(char.IsNumber);

        internal static bool IsNumericD(string inputstr) => double.TryParse(inputstr, out _);

        internal static string MergeFiles_to_One_LayerName(string inputstr)
        {
            int i;
            for (i = inputstr.Length - 1; i > 0; i--)
                if (inputstr[i] == '.' | inputstr[i] == '_') break;

            if (i + 4 <= inputstr.Length)
                if (IsNumericA(inputstr.Substring(i + 1, 3)))
                    inputstr = inputstr.Substring(0, i);
            return inputstr;//Put files along same line (.001 .002 ... files) to one layer
        }

        internal static IniData Read_ini()
        {
            FileIniDataParser parser = new FileIniDataParser();
            string settingfile = Inipath;
            if (System.IO.File.Exists(settingfile))
                return parser.ReadFile(settingfile);
            else
                return new IniData();
        }

        internal static void Save_ini(IniData ini)
        {
            FileIniDataParser parser = new FileIniDataParser();
            parser.WriteFile(Inipath, ini);
        }

        internal class CXYZ
        {
            public double C = -1;
            public double X = -1;
            public double Y = -1;
            public double Z = -1;

            public void UpdateC(ref List<RPL> lRPL)
            { C = Class_RPL.GetGridKP(X, Y, ref lRPL); }
        }

        internal class CXYZ_Table
        {
            public double C = 0;
            public int Row = 0;
        }

        internal class FENLN //Fix E N LineName
        {
            public double e, n, dist = 0.0;
            public int fix = -1;
            public string linename = "";

            public string ToStringEN()
            {
                return String.Format("{0:0.##}", e) + "," + String.Format("{0:0.##}", n);
            }

            public void Upate_Dist(double X, double Y)
            {
                dist = Math.Sqrt((e - X) * (e - X) + (n - Y) * (n - Y));
            }
        }

        internal class RF
        {
            public short acolor = 0;
            public string annotation = "";
            public double length2d = -1;
            public string linename = "";
            public byte otype = 0;
            public int sample_rate = 0;
            public bool valid = false;
            public List<XYZZ> xyzz;
        }

        internal class RPL
        {
            public double A = 0;
            public double AB2 = 0;
            public double B = 0;
            public double C = 0;
            public double E = 0;
            public double N = 0;
            public double segch = 0;
        }

        internal class TG
        {
            public string annotation = "";
            public string dimension = "";
            public string layername = "";
            public string linename = "";
            public byte otype = 0;
            public bool valid = false;
            public List<XY> xy;

            public void Update_dimension(string sH, string sL, string sO, string sW)
            {
                dimension = $"L: {sL}, W: {sW}, H: {sH}, O: {sO}";
            }
        }

        internal class XY
        {
            public double X = -1;
            public double Y = -1;
        }

        internal class XYZZ
        {
            public double GridX = -1;
            public int Rec = -1;
            public double Sbl = -1;
            public double X = -1;
            public double Y = -1;
            public double ZSb = -1;
            public double ZTxd = -1;

            internal bool IsNotEquals(XYZZ oxyzz)
            {
                if (Rec == oxyzz.Rec && ZSb == oxyzz.ZSb) return false;
                return true;
            }
        }
    }
}