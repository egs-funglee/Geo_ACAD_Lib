using System.Collections.Generic;
using System.Threading.Tasks;
using static EGS_RF.SharedFunctions;

namespace EGS_RF
{
    internal class Class_RPL
    {
        internal static double GetGridKP(double x, double y, ref List<RPL> lRPL)
        {
            bool found, found_up, found_dn = false;
            double[] aco = new double[lRPL.Count];
            double ipx = 0, ipy = 0, ipx_up, ipy_up, ipx_dn = 0, ipy_dn = 0, minACO = -1;
            int minACOi = 0, row_up, row_dn, i, ub = lRPL.Count - 1;

            //Calc HD between the point and each points on RPL
            //Parallel.ForEach(lRPL, (iRPL, state, index) => { aco[index] = HD(x, y, iRPL.E, iRPL.N); });

            for (i = 0; i < lRPL.Count; i++) //Loop for the smallest HD
            {
                aco[i] = HD(x, y, lRPL[i].E, lRPL[i].N);
                if (aco[i] < minACO || minACO < 0)
                {
                    minACO = aco[i];
                    minACOi = i; //index of the smallest HD
                }
            }

            if (minACOi == 0)//set searching start row
            {
                row_up = 1;
                row_dn = ub + 1; //check first segment only, skipping the if check below
            }
            else
            {
                row_up = minACOi;
                row_dn = minACOi + 1;
            }

            ipx_up = IPX(x, y, lRPL[row_up]);
            ipy_up = IPY(x, y, lRPL[row_up]);
            found_up = CheckPoint(ref lRPL, row_up, ipx_up, ipy_up);
            if (row_dn <= ub)
            {
                ipx_dn = IPX(x, y, lRPL[row_dn]);
                ipy_dn = IPY(x, y, lRPL[row_dn]);
                found_dn = CheckPoint(ref lRPL, row_dn, ipx_dn, ipy_dn);
            }

            if (!found_up && !found_dn)
            {
                found = false;//out of AC
            }
            else if (found_up && found_dn)
            {
                double ro_up = HD(ipx_up, ipy_up, x, y);//roffset
                double ro_dn = HD(ipx_dn, ipy_dn, x, y);//roffset
                if (ro_dn > ro_up)
                {
                    i = row_up; ipx = ipx_up; ipy = ipy_up;
                    found = true;
                }
                else
                {
                    i = row_dn; ipx = ipx_dn; ipy = ipy_dn;
                    found = true;
                }
            }
            else if (found_up) //and not found_dn
            {
                i = row_up; ipx = ipx_up; ipy = ipy_up;
                found = true;
            }
            else//if found_dn and not found_up
            {
                i = row_dn; ipx = ipx_dn; ipy = ipy_dn;
                found = true;
            }

            double gridx;

            if (found)
            {
                gridx = HD(ipx, ipy, lRPL[i - 1].E, lRPL[i - 1].N) + lRPL[i - 1].segch;
            }
            else
            {
                if (minACOi == 0) //out first RPL pt
                {
                    ipx = IPX(x, y, lRPL[1]);
                    ipy = IPY(x, y, lRPL[1]);
                    gridx = 0 - HD(ipx, ipy, lRPL[0].E, lRPL[0].N);
                }
                else if (minACOi == ub) //out last RPL pt
                {
                    ipx = IPX(x, y, lRPL[ub]);
                    ipy = IPY(x, y, lRPL[ub]);
                    gridx = HD(ipx, ipy, lRPL[ub].E, lRPL[ub].N) + lRPL[ub].segch;
                }
                else //out of AC ankle, return the gridx of AC
                {
                    gridx = lRPL[minACOi].segch;
                }
            }
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
            bool is_on_seg = true;
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