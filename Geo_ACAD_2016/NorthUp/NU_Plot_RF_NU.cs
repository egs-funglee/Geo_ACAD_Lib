using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class NU_Plot_RF_NU
    {
        internal static void Plot_RF_NU()//for cutting
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<RF> lRF = Class_RF.ReadRF(1530);
            if (lRF.Count == 0) return;

            Stopwatch stopwatch = new Stopwatch(); long elapsed_time;
            stopwatch.Reset(); stopwatch.Start();

            Polyline3d acPoly1, acPoly2, acPoly3;
            DBText acText1, acText2, acText3;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lt.Has("Reflector"))
                {
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = "Reflector"
                    };
                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                    lt.DowngradeOpen();
                }

                if (!lt.Has("Annotation"))
                {
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = "Annotation"
                    };
                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                    lt.DowngradeOpen();
                }

                string lastfile = "";
                int tfile = 0;
                List<string> processed_file = new List<string>();
                foreach (RF iRF in lRF)
                {
                    HyperLink hyper = new HyperLink
                    {
                        Description = iRF.linename,
                        Name = iRF.linename,
                        SubLocation = iRF.sample_rate.ToString()
                    };

                    if (iRF.linename != lastfile)
                    {
                        ed.WriteMessage("working on {0}\n", iRF.linename);
                        lastfile = iRF.linename;
                        tfile++;
                    }

                    switch (iRF.otype)
                    {
                        case 1:
                            acPoly1 = new Polyline3d(); acPoly1.SetDatabaseDefaults();
                            acPoly2 = new Polyline3d(); acPoly2.SetDatabaseDefaults();
                            acPoly3 = new Polyline3d(); acPoly3.SetDatabaseDefaults();

                            acBlkTblRec.AppendEntity(acPoly1); tr.AddNewlyCreatedDBObject(acPoly1, true);
                            acBlkTblRec.AppendEntity(acPoly2); tr.AddNewlyCreatedDBObject(acPoly2, true);
                            acBlkTblRec.AppendEntity(acPoly3); tr.AddNewlyCreatedDBObject(acPoly3, true);

                            Point3dCollection acPts3dPoly1 = new Point3dCollection();
                            Point3dCollection acPts3dPoly2 = new Point3dCollection();
                            Point3dCollection acPts3dPoly3 = new Point3dCollection();

                            foreach (XYZZ ixyzz in iRF.xyzz)
                            {
                                /*
                                acPts3dPoly1.Add(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.ZTxd - ixyzz.ZSb));
                                acPts3dPoly2.Add(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.ZSb));
                                acPts3dPoly3.Add(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.Rec));
                                */

                                PolylineVertex3d acPolVer3d1 = new PolylineVertex3d(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.ZTxd - ixyzz.ZSb));
                                PolylineVertex3d acPolVer3d2 = new PolylineVertex3d(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.ZSb));
                                PolylineVertex3d acPolVer3d3 = new PolylineVertex3d(new Point3d(ixyzz.X, ixyzz.Y, ixyzz.Rec));

                                acPoly1.AppendVertex(acPolVer3d1);
                                acPoly2.AppendVertex(acPolVer3d2);
                                acPoly3.AppendVertex(acPolVer3d3);

                                tr.AddNewlyCreatedDBObject(acPolVer3d1, true);
                                tr.AddNewlyCreatedDBObject(acPolVer3d2, true);
                                tr.AddNewlyCreatedDBObject(acPolVer3d3, true);
                            }

                            /*
                            for (int i = 0; i < acPts3dPoly1.Count; i++)
                            {
                                PolylineVertex3d acPolVer3d1 = new PolylineVertex3d(acPts3dPoly1[i]);
                                PolylineVertex3d acPolVer3d2 = new PolylineVertex3d(acPts3dPoly2[i]);
                                PolylineVertex3d acPolVer3d3 = new PolylineVertex3d(acPts3dPoly3[i]);
                                acPoly1.AppendVertex(acPolVer3d1); tr.AddNewlyCreatedDBObject(acPolVer3d1, true);
                                acPoly2.AppendVertex(acPolVer3d2); tr.AddNewlyCreatedDBObject(acPolVer3d2, true);
                                acPoly3.AppendVertex(acPolVer3d3); tr.AddNewlyCreatedDBObject(acPolVer3d3, true);
                            }
                            */

                            acPoly1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, iRF.acolor);
                            acPoly2.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, iRF.acolor);
                            acPoly3.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, iRF.acolor);

                            acPoly1.Linetype = "ByLayer"; //for xyzTX
                            acPoly2.Linetype = "ByBlock"; //for xyzSB
                            acPoly3.Linetype = "Continuous"; //for rec

                            acPoly1.Layer = "Reflector";
                            acPoly2.Layer = "Reflector";
                            acPoly3.Layer = "Reflector";

                            acPoly1.Hyperlinks.Add(hyper);
                            acPoly2.Hyperlinks.Add(hyper);
                            acPoly3.Hyperlinks.Add(hyper);

                            break;

                        case 2:
                            acText1 = new DBText(); acText1.SetDatabaseDefaults();
                            acText2 = new DBText(); acText2.SetDatabaseDefaults();
                            acText3 = new DBText(); acText3.SetDatabaseDefaults();

                            acText1.Justify = AttachmentPoint.MiddleCenter; acText1.Height = 10; acText1.TextString = iRF.annotation;
                            acText2.Justify = AttachmentPoint.MiddleCenter; acText2.Height = 10; acText2.TextString = iRF.annotation;
                            acText3.Justify = AttachmentPoint.MiddleCenter; acText3.Height = 10; acText3.TextString = iRF.annotation;

                            acText1.AlignmentPoint = new Point3d(iRF.xyzz[0].X, iRF.xyzz[0].Y, iRF.xyzz[0].ZTxd - iRF.xyzz[0].ZSb);
                            acText2.AlignmentPoint = new Point3d(iRF.xyzz[0].X, iRF.xyzz[0].Y, iRF.xyzz[0].ZSb);
                            acText3.AlignmentPoint = new Point3d(iRF.xyzz[0].X, iRF.xyzz[0].Y, iRF.xyzz[0].Rec);

                            acText1.Linetype = "ByLayer"; //for xyzTX
                            acText2.Linetype = "ByBlock"; //for xyzSB
                            acText3.Linetype = "Continuous"; //for rec

                            acText1.Layer = "Annotation";
                            acText2.Layer = "Annotation";
                            acText3.Layer = "Annotation";

                            acBlkTblRec.AppendEntity(acText1); tr.AddNewlyCreatedDBObject(acText1, true); acText1.Hyperlinks.Add(hyper);
                            acBlkTblRec.AppendEntity(acText2); tr.AddNewlyCreatedDBObject(acText2, true); acText2.Hyperlinks.Add(hyper);
                            acBlkTblRec.AppendEntity(acText3); tr.AddNewlyCreatedDBObject(acText3, true); acText3.Hyperlinks.Add(hyper);

                            break;

                        default:
                            break;
                    }
                }

                tr.Commit();
                stopwatch.Stop(); elapsed_time = stopwatch.ElapsedMilliseconds;
                ed.WriteMessage("{0} ms used to plot {1} file(s) to AcDB\nDone\n", elapsed_time, tfile);
            }
        }
    }
}