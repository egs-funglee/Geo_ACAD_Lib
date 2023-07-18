using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class SBP_Plot_RF
    {
        internal static void Plot_RF_Profile()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            IniParser.Model.IniData ini = Read_ini();
            string sRPL = ini["Parameters"]["RPL"];
            string sCXYZ = ini["Parameters"]["CXYZ"];
            if (!System.IO.File.Exists(sRPL)) sRPL = "";
            if (!System.IO.File.Exists(sCXYZ)) sCXYZ = "";
            string sVE = ini["Parameters"]["VE"];
            int ve = 10;
            if (!string.IsNullOrEmpty(sVE)) ve = int.Parse(sVE);

            PromptStringOptions pStrOpts = new PromptStringOptions("");
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            PromptResult pKeyRes;
            pStrOpts.AllowSpaces = true;

            pStrOpts.Message = "\nPath to RPL ";
            pStrOpts.DefaultValue = sRPL;
            pKeyRes = doc.Editor.GetString(pStrOpts);
            sRPL = pKeyRes.StringResult;

            pStrOpts.Message = "\nPath to CXYZ ";
            pStrOpts.DefaultValue = sCXYZ;
            pKeyRes = doc.Editor.GetString(pStrOpts);
            sCXYZ = pKeyRes.StringResult;

            pStrOpts.Message = "\nVertical Scale ";
            pStrOpts.DefaultValue = sVE;
            pKeyRes = doc.Editor.GetString(pStrOpts);
            sVE = pKeyRes.StringResult;
            if (!string.IsNullOrEmpty(sVE))
            {
                ve = int.Parse(sVE);
                if (ve > 0) ini["Parameters"]["VE"] = sVE;
            }

            if (!System.IO.File.Exists(sRPL))
            {
                ed.WriteMessage($"RPL: {sRPL} is not exist.");
                return;
            }
            if (!System.IO.File.Exists(sCXYZ))
            {
                ed.WriteMessage($"CXYZ: {sCXYZ} is not exist.");
                return;
            }

            ini["Parameters"]["RPL"] = sRPL;
            ini["Parameters"]["CXYZ"] = sCXYZ;
            Save_ini(ini);

            Stopwatch stopwatch = new Stopwatch(); long elapsed_time;
            stopwatch.Reset(); stopwatch.Start();
            List<RPL> lRPL = Class_RPL.ReadRPL(sRPL);
            stopwatch.Stop(); elapsed_time = stopwatch.ElapsedMilliseconds;
            ed.WriteMessage("{0} ms : Read the RPL.\n", elapsed_time);

            List<RF> lRF = Class_RF.ReadRF(1530);
            if (lRF.Count == 0) return;

            stopwatch.Reset(); stopwatch.Start();
            List<CXYZ_Table> lCXYZ_Table = new List<CXYZ_Table>();
            List<CXYZ> llCXYZ = Class_CXYZ.ReadCXYZ(sCXYZ, lRPL, ref lCXYZ_Table);
            stopwatch.Stop(); elapsed_time = stopwatch.ElapsedMilliseconds;
            ed.WriteMessage("{0} ms : Read the CXYZ.\n", elapsed_time);

            stopwatch.Reset(); stopwatch.Start();
            Parallel.ForEach<RF>(lRF, iRF =>
            {
                Parallel.ForEach<XYZZ>(iRF.xyzz, ixyzz =>
                {
                    ixyzz.GridX = Class_RPL.GetGridKP(ixyzz.X, ixyzz.Y, ref lRPL);
                    ixyzz.Sbl = Class_CXYZ.GetWD(ixyzz.GridX, ref llCXYZ, ref lCXYZ_Table);
                });
            });
            stopwatch.Stop(); elapsed_time = stopwatch.ElapsedMilliseconds;
            ed.WriteMessage("{0} ms : Read the RF files (Grid KP and depth below datum) by Parallel.ForEach method.\n", elapsed_time);

            stopwatch.Reset(); stopwatch.Start();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lt.Has("Prf_Description"))
                {
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = "Prf_Description"
                    };
                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                    lt.DowngradeOpen();
                }

                if (!lt.Has("Prf_Ref"))
                {
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = "Prf_Ref"
                    };
                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                    lt.DowngradeOpen();
                }


                foreach (RF iRF in lRF)
                {
                    HyperLink hyper = new HyperLink
                    {
                        Description = iRF.linename,
                        Name = iRF.linename,
                        SubLocation = ""
                    };

                    switch (iRF.otype)
                    {
                        case 1:
                            Polyline acPoly = new Polyline();
                            acPoly.SetDatabaseDefaults();
                            Point2d oPoint2d = new Point2d();
                            int v = 0;
                            for (int i = 0; i < iRF.xyzz.Count; i++)
                            {
                                Point2d iPoint2d = new Point2d(iRF.xyzz[i].GridX, (iRF.xyzz[i].ZSb + iRF.xyzz[i].Sbl) * (-ve));
                                if (iPoint2d.X != oPoint2d.X && iPoint2d.Y != oPoint2d.Y)
                                {
                                    acPoly.AddVertexAt(v, iPoint2d, 0, 0, 0);
                                    v++;
                                    oPoint2d = iPoint2d;
                                }
                            }
                            acPoly.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, iRF.acolor);

                            if (acPoly.Length > 0)
                            {
                                acBlkTblRec.AppendEntity(acPoly);
                                tr.AddNewlyCreatedDBObject(acPoly, true);
                                acPoly.Hyperlinks.Add(hyper);
                                acPoly.Layer = "Prf_Ref";
                            }
                            break;

                        case 2:
                            DBText acText = new DBText();
                            acText.SetDatabaseDefaults();
                            acText.Justify = AttachmentPoint.MiddleCenter;
                            acText.AlignmentPoint = new Point3d(iRF.xyzz[0].GridX, (iRF.xyzz[0].ZSb + iRF.xyzz[0].Sbl) * (-ve), 0);
                            acText.Height = 20;
                            acText.WidthFactor = 0.75;
                            acText.TextString = iRF.annotation;

                            acBlkTblRec.AppendEntity(acText);
                            tr.AddNewlyCreatedDBObject(acText, true);
                            acText.Hyperlinks.Add(hyper);
                            acText.Layer = "Prf_Description";
                            break;

                        default:
                            break;
                    }
                }
                tr.Commit();
                stopwatch.Stop(); elapsed_time = stopwatch.ElapsedMilliseconds;
                ed.WriteMessage("{0} ms : Create Polyline and Text objects - Plot to AutoCAD\nDone\n", elapsed_time);
            }
        }
    }
}