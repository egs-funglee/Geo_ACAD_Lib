using System;
using System.Collections.Generic;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class NU_RecLineOut
    {
        internal static string RecLine_Out(RF iRF, int vel_sedi, int vel_water, int sample_rate)
        {
            List<string> recout = new List<string>();
            double SeabedSN, RefSN;
            switch (iRF.otype)
            {
                case 1:
                    recout.Add("START REFLECTOR");
                    foreach (XYZZ iXYZZ in iRF.xyzz)
                    {
                        SeabedSN = (iXYZZ.ZTxd - iXYZZ.ZSb) * 2 / vel_water * sample_rate;
                        RefSN = SeabedSN + iXYZZ.ZSb * 2 / vel_sedi * sample_rate;
                        recout.Add(String.Format("RecNo {0,8:0} RefSampleNo {1,6:0} SeabedSampleNo {2,6:0} Pos {3,12:0.000} E {4,12:0.000} N ZTxd {5,6:0.00} ZSb {6,6:0.00}",
                            iXYZZ.Rec, RefSN, SeabedSN,
                            iXYZZ.X, iXYZZ.Y, iXYZZ.ZTxd, iXYZZ.ZSb));
                    }
                    break;

                case 2:
                    recout.Add("ANNOTATION RECORD");
                    SeabedSN = (iRF.xyzz[0].ZTxd - iRF.xyzz[0].ZSb) * 2 / vel_water * sample_rate;
                    RefSN = SeabedSN + iRF.xyzz[0].ZSb * 2 / vel_sedi * sample_rate;
                    recout.Add(String.Format("RecNo {0,8:0} RefSampleNo {1,6:0} SeabedSampleNo {2,6:0} Pos {3,12:0.000} E {4,12:0.000} N ZTxd {5,6:0.00} ZSb {6,6:0.00} Annotation {7,40}",
                        iRF.xyzz[0].Rec, RefSN, SeabedSN,
                        iRF.xyzz[0].X, iRF.xyzz[0].Y, iRF.xyzz[0].ZTxd, iRF.xyzz[0].ZSb, iRF.annotation));
                    break;

                default:
                    recout.Clear();
                    break;
            }
            return string.Join("\r\n", recout.ToArray());
        }
    }
}