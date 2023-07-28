using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class NU_Fetch_RF_to_Files
    {
        internal static void Fetch_RF_NU_to_Files()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            //string path = "C:\\EGS\\";
            int vel_sedi = 1600;
            int vel_water = 1530;
            //int sample_rate = 20000;

            TypedValue[] acTypValAr = new TypedValue[2];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "POLYLINE"), 0);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.LayerName, "Reflector"), 1);
            SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
            PromptSelectionResult psr, psrA;
            psr = ed.SelectAll(acSelFtr);
            if (psr.Status != PromptStatus.OK) return;
            if (psr.Value.Count == 0) return;

            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "TEXT"), 0);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.LayerName, "Annotation"), 1);
            acSelFtr = new SelectionFilter(acTypValAr);
            psrA = ed.SelectAll(acSelFtr);
            //if (psrA.Status != PromptStatus.OK) return;

            List<RF> lRF = new List<RF>();//RecNo
            List<RF> lRF_ZTxd = new List<RF>();
            List<RF> lRF_ZSb = new List<RF>();
            List<string> linenames = new List<string>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    Polyline3d acPoly3d = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Polyline3d;
                    if (acPoly3d.Length > 0)
                    {
                        Point3dCollection acPts3d = new Point3dCollection();
                        foreach (ObjectId acObjIdVert in acPoly3d)
                        {
                            PolylineVertex3d acPolVer3d;
                            acPolVer3d = tr.GetObject(acObjIdVert, OpenMode.ForRead) as PolylineVertex3d;
                            acPts3d.Add(acPolVer3d.Position);
                        }

                        RF iRF = new RF
                        {
                            xyzz = new List<XYZZ>(),
                            linename = acPoly3d.Hyperlinks[0].Name,
                            sample_rate = int.Parse(acPoly3d.Hyperlinks[0].SubLocation),
                            valid = false,
                            otype = 1,
                            length2d = HD(acPts3d[0].X, acPts3d[0].Y, acPts3d[acPts3d.Count - 1].X, acPts3d[acPts3d.Count - 1].Y)
                        };

                        if (!linenames.Contains(iRF.linename)) linenames.Add(iRF.linename);

                        switch (acPoly3d.Linetype) //1:zTxd 2:ZSb 3:Rec
                        {
                            case "ByLayer"://ZTxd
                                foreach (Point3d Pt3d in acPts3d)
                                {
                                    XYZZ iXYZZ = new XYZZ
                                    {
                                        X = Pt3d.X,
                                        Y = Pt3d.Y,
                                        ZTxd = Pt3d.Z
                                    };
                                    iRF.xyzz.Add(iXYZZ);
                                }
                                lRF_ZTxd.Add(iRF);
                                break;

                            case "ByBlock"://ZSb
                                foreach (Point3d Pt3d in acPts3d)
                                {
                                    XYZZ iXYZZ = new XYZZ
                                    {
                                        X = Pt3d.X,
                                        Y = Pt3d.Y,
                                        ZSb = Pt3d.Z
                                    };
                                    iRF.xyzz.Add(iXYZZ);
                                }
                                lRF_ZSb.Add(iRF);
                                break;

                            case "Continuous"://Rec
                                foreach (Point3d Pt3d in acPts3d)
                                {
                                    XYZZ iXYZZ = new XYZZ
                                    {
                                        X = Pt3d.X,
                                        Y = Pt3d.Y,
                                        Rec = Convert.ToInt32(Pt3d.Z)
                                    };
                                    iRF.xyzz.Add(iXYZZ);
                                }
                                lRF.Add(iRF);

                                break;

                            default:
                                break;
                        }
                    }
                }

                if (psrA.Status == PromptStatus.OK)
                {
                    foreach (SelectedObject so in psrA.Value)
                    {
                        DBText aText = tr.GetObject(so.ObjectId, OpenMode.ForRead) as DBText;
                        Point3d Pt3d = aText.AlignmentPoint;
                        RF iRF = new RF
                        {
                            xyzz = new List<XYZZ>(),
                            linename = aText.Hyperlinks[0].Name,
                            sample_rate = int.Parse(aText.Hyperlinks[0].SubLocation),
                            valid = false,
                            otype = 2,
                            annotation = aText.TextString,
                            length2d = (Pt3d.X + Pt3d.Y) / 2//hmm simple test
                        };

                        if (!linenames.Contains(iRF.linename)) linenames.Add(iRF.linename);
                        XYZZ iXYZZ = new XYZZ
                        {
                            X = Pt3d.X,
                            Y = Pt3d.Y
                        };

                        switch (aText.Linetype) //1:zTxd 2:ZSb 3:Rec
                        {
                            case "ByLayer"://ZTxd
                                iXYZZ.ZTxd = Pt3d.Z;
                                iRF.xyzz.Add(iXYZZ); lRF_ZTxd.Add(iRF);
                                break;

                            case "ByBlock"://ZSb
                                iXYZZ.ZSb = Pt3d.Z;
                                iRF.xyzz.Add(iXYZZ); lRF_ZSb.Add(iRF);
                                break;

                            case "Continuous"://Rec
                                iXYZZ.Rec = Convert.ToInt32(Pt3d.Z);
                                iRF.xyzz.Add(iXYZZ); lRF.Add(iRF);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\\";
            if (lRF.Count > 0) System.IO.Directory.CreateDirectory(path);

            //Consolidate ZTxd ZSb to lRF(Rec)
            foreach (RF iRF in lRF)
            {
                bool bool_j = false, bool_k = false;

                foreach (RF kRF in lRF_ZSb)
                {
                    if (iRF.length2d == kRF.length2d && iRF.xyzz.Count == kRF.xyzz.Count && iRF.linename == kRF.linename)
                    {
                        for (int i = 0; i < iRF.xyzz.Count; i++)
                        {
                            iRF.xyzz[i].ZSb = kRF.xyzz[i].ZSb;
                        }
                        bool_k = true;
                    }
                }

                foreach (RF jRF in lRF_ZTxd)
                {
                    if (iRF.length2d == jRF.length2d && iRF.xyzz.Count == jRF.xyzz.Count && iRF.linename == jRF.linename)
                    {
                        for (int i = 0; i < iRF.xyzz.Count; i++)
                        {
                            iRF.xyzz[i].ZTxd = jRF.xyzz[i].ZTxd + iRF.xyzz[i].ZSb;
                        }
                        bool_j = true;
                    }
                }

                if (bool_j && bool_k) iRF.valid = true;
            }

            //Sort with 1st rec#
            lRF.Sort((x, y) => x.xyzz[0].Rec.CompareTo(y.xyzz[0].Rec));

            //prepare filelist for writing
            List<RF_File> outfiles = new List<RF_File>();
            foreach (string linename in linenames)
            {
                RF_File newRFF = new RF_File
                {
                    filename = linename
                };
                outfiles.Add(newRFF);
            }

            //merge line with same filename v20210729 \r\n CRLF for C-View Tools
            foreach (RF iRF in lRF)
            {
                if (iRF.valid)
                {
                    int index = outfiles.FindIndex(x => x.filename.Equals(iRF.linename));
                    outfiles[index].lines += NU_RecLineOut.RecLine_Out(iRF, vel_sedi, vel_water, iRF.sample_rate) + "\r\n";
                }
            }

            //write files
            foreach (RF_File ofn in outfiles)
            {
                System.IO.File.WriteAllText($"{path}{ofn.filename}", ofn.lines);
            }

            if (lRF.Count > 0) ed.WriteMessage($"Check the RF files in {path}");
        }
    }

    internal class RF_File
    {
        public string filename;
        public string lines;
    }
}