using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class Class_CXYZ
    {
        internal static double GetWD(double gridx, ref List<CXYZ> lCXYZ, ref List<CXYZ_Table> lCXYZ_Table)
        {
            if (gridx < lCXYZ[0].C) return lCXYZ[0].Z; //return first Z when < min Grid KP
            if (gridx > lCXYZ.Last().C) return lCXYZ.Last().Z; //return last Z when > max Grid KP

            //Get Range for the Sublist
            int i1 = lCXYZ_Table.FindIndex(cc => cc.C >= gridx) - 1; //i1 to < gridx
            int i2 = lCXYZ_Table[i1 + 1].Row - lCXYZ_Table[i1].Row + 1; //Count
            List<CXYZ> slCXYZ = lCXYZ.GetRange(lCXYZ_Table[i1].Row, i2); //Get Sublist;

            //Search in the Sublist
            i1 = slCXYZ.FindIndex(cc => cc.C >= gridx) - 1; //i1 to < gridx
            i2 = i1 + 1; //gridx between i1 and i2, cxyz should be ascending by C, return linear interpolated Z
            return ((slCXYZ[i1].Z - slCXYZ[i2].Z) / (slCXYZ[i1].C - slCXYZ[i2].C) * (gridx - slCXYZ[i2].C)) + slCXYZ[i2].Z;
        }

        internal static List<CXYZ> ReadCXYZ(string sCXYZ, List<RPL> lRPL, ref List<CXYZ_Table> lCXYZ_Table)
        {
            string[] sCXYZlines = System.IO.File.ReadAllLines(sCXYZ);
            char[] charSeparators = new char[] { '\t' };

            List<CXYZ> lCXYZ = new List<CXYZ>();
            foreach (string line in sCXYZlines)
            {
                string[] tstr = line.Split(charSeparators, 4);
                lCXYZ.Add(new CXYZ
                {
                    X = double.Parse(tstr[1]),
                    Y = double.Parse(tstr[2]),
                    Z = double.Parse(tstr[3])
                });
            };

            Parallel.ForEach(lCXYZ, (iiCXYZ) => iiCXYZ.UpdateC(ref lRPL)); //Avoid duplicated C values

            for (int i = 0; i < lCXYZ.Count; i += 5000) //Each 5000 Record to Sublist
                lCXYZ_Table.Add(new CXYZ_Table { Row = i, C = lCXYZ[i].C });
            lCXYZ_Table.Add(new CXYZ_Table { Row = lCXYZ.Count - 1, C = lCXYZ.Last().C }); //Last record

            return lCXYZ;
        }
    }
}