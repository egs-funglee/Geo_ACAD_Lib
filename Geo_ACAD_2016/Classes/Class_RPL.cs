using System.Collections.Generic;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class Class_RPL
    {
        internal static double GetGridKP(double x, double y, ref List<RPL> lRPL)
        {
            double[] aco = new double[lRPL.Count];
            double[] ipx = new double[lRPL.Count];
            double[] ipy = new double[lRPL.Count];
            double[] ro = new double[lRPL.Count];
            double gridx, minACO = -1, minRO = -1;
            int i, minACOi = 0, minROi = 0, ub = lRPL.Count - 1;

            //Calc HD between the point and each points on RPL
            //Parallel.ForEach(lRPL, (iRPL, state, index) => { aco[index] = HD(x, y, iRPL.E, iRPL.N); });

            for (i = 0; i < lRPL.Count; i++) //Loop for the smallest HD
            {
                aco[i] = HD(x, y, lRPL[i].E, lRPL[i].N);
                if (aco[i] < minACO || minACO < 0) //find the shortest distance to each AC
                {
                    minACO = aco[i];
                    minACOi = i; //min 0, index of the smallest HD to AC
                }
                if (i > 0) //calc RO from 2nd pairs
                {
                    ipx[i] = IPX(x, y, lRPL[i]);
                    ipy[i] = IPY(x, y, lRPL[i]);
                    bool a = CheckPoint(ref lRPL, i, ipx[i], ipy[i]);
                    if (CheckPoint(ref lRPL, i, ipx[i], ipy[i])) //Check if the intersect is on the segment
                    {
                        ro[i] = HD(x, y, ipx[i], ipy[i]); //Calc distance / route offset between P and IP
                        if (minRO < 0 || ro[i] < minRO) //located the shortest pair
                        {
                            minRO = ro[i];
                            minROi = i; //min 1, index of the smallest RO
                        }
                    }
                }
            }

            if (minACOi == 0 && CheckPoint(ref lRPL, 1, ipx[1], ipy[1]) == false) //HEAD out of first RPL point use 2nd IP
            {
                gridx = 0 - HD(ipx[1], ipy[1], lRPL[0].E, lRPL[0].N);
            }
            else if (minACOi == ub && CheckPoint(ref lRPL, ub, ipx[ub], ipy[ub]) == false) //TAIL out of last RPL point
            {
                gridx = HD(ipx[ub], ipy[ub], lRPL[ub].E, lRPL[ub].N) + lRPL[ub].segch;
            }
            else if (minROi > 0 && minACO > minRO) //with RO and < ACO
            {
                gridx = HD(ipx[minROi], ipy[minROi], lRPL[minROi - 1].E, lRPL[minROi - 1].N) + lRPL[minROi - 1].segch;
            }
            else //out of AC return chainage to AC
            {
                gridx = lRPL[minACOi].segch;
            };

            return gridx;
        }

        internal static List<RPL> ReadRPL(string rplpath)//give filepah return RPL List
        {
            string[] sRPLlines = System.IO.File.ReadAllLines(rplpath);

            List<RPL> lRPL = new List<RPL>();
            string[] tempstr;
            char[] charSeparators = new char[] { ',' };

            //double dx, dy;
            int i = 0;

            foreach (string line in sRPLlines)
            {
                tempstr = line.Split(charSeparators, 2);
                if (tempstr.Length == 2)
                {
                    RPL iRPL = new RPL
                    {
                        E = double.Parse(tempstr[0]),
                        N = double.Parse(tempstr[1])
                    };

                    if (lRPL.Count > 0) //2nd line on RPL
                    {
                        //Update ABC
                        iRPL.A = lRPL[i - 1].N - iRPL.N;
                        iRPL.B = iRPL.E - lRPL[i - 1].E;

                        if (iRPL.A == 0 && iRPL.B == 0) continue; //skip duplicates - dx & dy = 0

                        iRPL.C = (lRPL[i - 1].E * iRPL.N) - (iRPL.E * lRPL[i - 1].N);
                        iRPL.AB2 = (iRPL.A * iRPL.A) + (iRPL.B * iRPL.B);
                        iRPL.segch = System.Math.Sqrt(iRPL.AB2) + lRPL[i - 1].segch;
                    };
                    lRPL.Add(iRPL);
                    i++;
                };
            };
            return lRPL;
        }

        private static bool CheckPoint(ref List<RPL> lRPL, int j, double px, double py)
        {
            bool is_on_seg = true; //Ax+By+C=0 => when B=0, Ax+C=0, Vertical, Check X only
            if (lRPL[j].B != 0) //dX=0 vertical, just check X
            {
                if ((px > lRPL[j - 1].E) && (px > lRPL[j].E)) is_on_seg = false;
                if ((px < lRPL[j - 1].E) && (px < lRPL[j].E)) is_on_seg = false;
            };
            if (lRPL[j].A != 0) //dY=0 horizontal, just check Y
            {
                if ((py > lRPL[j - 1].N) && (py > lRPL[j].N)) is_on_seg = false;
                if ((py < lRPL[j - 1].N) && (py < lRPL[j].N)) is_on_seg = false;
            };
            return is_on_seg;
        }
    }
}