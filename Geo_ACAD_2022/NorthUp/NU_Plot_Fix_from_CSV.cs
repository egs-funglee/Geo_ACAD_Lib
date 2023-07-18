using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class NU_Plot_Fix_from_CSV
    {
        private static double acad_textheight = 10;
        private static bool bool_fliplinename = true;
        private static bool bool_mergefiles = true;
        private static readonly bool bool_plotcircle = true;
        private static bool bool_ploteachfix = true;
        private static bool bool_plotlinename = true;
        private static bool bool_plottrack = true;
        private static bool bool_recalc_cmg = true;
        private static bool bool_seperatelayer = true;
        private static int eachfix = 1;

        internal static void Plot_Fix_from_CSV_NU()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            if (Prompt_Input() == false) return;
            ed.WriteMessage("\n\n");

            List<string> PCs = Open_PC_Files();
            if (PCs.Count > 0)
            {
                ObjectId activelayer = db.Clayer;

                double acad_circleradius = acad_textheight / 4;
                short acad_linecolour = 214;
                string linename;
                string[] tempstr, wholefile;
                int i, fcount;
                double text_ro;
                string layername;
                int lastfix;

                ProgressWind myPGW = new ProgressWind();
                Polyline acPoly; DBText acText; Circle acCircle; HyperLink hyper = new HyperLink();
                List<Pcfix> apcfix = new List<Pcfix>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    LayerTable acLayerTbl = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    TextStyleTable textStyles = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                    if (!textStyles.Has("Arial75"))
                    {
                        textStyles.UpgradeOpen();
                        var newTextStyleTableRecord = new TextStyleTableRecord
                        {
                            Name = "Arial75",
                            XScale = 0.75,
                            TextSize = 20,
                            Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Arial", false, false, 0, 0)
                        };
                        textStyles.Add(newTextStyleTableRecord);
                        tr.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
                        db.Textstyle = newTextStyleTableRecord.ObjectId;
                        textStyles.DowngradeOpen();
                    }
                    else
                        db.Textstyle = textStyles["Arial75"];

                    if (bool_seperatelayer)
                    {
                        string lastlayer = "0";

                        // prepare layer
                        foreach (string filename in PCs)
                        {
                            layername = CleanLayerName(System.IO.Path.GetFileNameWithoutExtension(filename));
                            if (bool_mergefiles) layername = MergeFiles_to_One_LayerName(layername);
                            if (layername != lastlayer)
                            {
                                lastlayer = layername;
                                if (!acLayerTbl.Has(layername))
                                {
                                    acLayerTbl.UpgradeOpen();
                                    LayerTableRecord newLayer = new LayerTableRecord { Name = layername };
                                    acLayerTbl.Add(newLayer);
                                    tr.AddNewlyCreatedDBObject(newLayer, true);
                                    acLayerTbl.DowngradeOpen();
                                }
                            }
                        }
                    }

                    int tfile = PCs.Count;
                    int ifile = 0;
                    if (tfile > 1)
                    {
                        myPGW.Show();
                        myPGW.progressBar1.Maximum = tfile;
                    }

                    foreach (string filename in PCs)
                    {
                        wholefile = System.IO.File.ReadAllLines(filename);

                        linename = System.IO.Path.GetFileNameWithoutExtension(filename);
                        layername = CleanLayerName(linename);
                        if (bool_mergefiles) layername = MergeFiles_to_One_LayerName(layername);

                        ed.WriteMessage("Working on : {0}\n", linename);

                        if (tfile > 1)
                        {
                            ifile += 1;
                            myPGW.label1.Text = $"Reading : {linename}  ( File {ifile} of {tfile} )";
                            myPGW.progressBar1.PerformStep();
                            myPGW.Refresh();
                        }

                        hyper.Description = linename;
                        hyper.Name = linename;
                        hyper.SubLocation = "";
                        lastfix = -1;

                        bool_recalc_cmg = true;
                        for (i = 0; i < wholefile.Length; i++)
                        {
                            if (wholefile[i].Length > 0)
                            {
                                switch (wholefile[i][0])
                                {
                                    case '#': break;
                                    default:
                                        tempstr = wholefile[i].Split('\t');
                                        if (!tempstr[0].EndsWith(".5"))
                                        {
                                            Pcfix temppcfix = new Pcfix
                                            {
                                                fix = (int)float.Parse(tempstr[0]),
                                                x = double.Parse(tempstr[1]),
                                                y = double.Parse(tempstr[2]),
                                                gyro = 0
                                            };
                                            apcfix.Add(temppcfix);
                                        }
                                        break;
                                }
                            }
                        }

                        // check if there is any point from PC files
                        if (apcfix.Count == 0)
                            continue;

                        // recalc cmg
                        if (bool_recalc_cmg)
                        {
                            for (i = 1; i <= apcfix.Count - 1; i++)
                            {
                                Pcfix temppcfix = new Pcfix
                                {
                                    fix = apcfix[i].fix,
                                    x = apcfix[i].x,
                                    y = apcfix[i].y,
                                    gyro = Calc_HDG(apcfix[i - 1].x, apcfix[i - 1].y, apcfix[i].x, apcfix[i].y)
                                };
                                apcfix[i] = temppcfix;
                            }
                            Pcfix temppcfix0 = new Pcfix
                            {
                                fix = apcfix[0].fix,
                                x = apcfix[0].x,
                                y = apcfix[0].y,
                                gyro = Calc_HDG(apcfix[0].x, apcfix[0].y, apcfix[1].x, apcfix[1].y)
                            };
                            apcfix[0] = temppcfix0;
                        }

                        // plot line and fix
                        acPoly = new Polyline();
                        acPoly.SetDatabaseDefaults();
                        fcount = 0;
                        lastfix = apcfix[0].fix - eachfix;
                        foreach (Pcfix temppcfix in apcfix)
                        {
                            // ed.WriteMessage(vbCrLf & temppcfix.fix.ToString & " , " & temppcfix.x.ToString & " , " & temppcfix.y.ToString & " , " & temppcfix.gyro.ToString & " , " & vbCrLf)
                            if (bool_plottrack)
                                acPoly.AddVertexAt(fcount, new Point2d(temppcfix.x, temppcfix.y), 0, 0, 0);
                            fcount += 1;

                            if (bool_ploteachfix & temppcfix.fix > (lastfix + eachfix))
                            {
                                i = apcfix.IndexOf(temppcfix);
                                acText = new DBText();
                                acText.SetDatabaseDefaults();
                                acText.Position = new Point3d((apcfix[i - 1].x + temppcfix.x) / 2, (apcfix[i - 1].y + temppcfix.y) / 2, 0);
                                acText.Height = acad_textheight * 1.5;
                                acText.TextString = "   Fix Jump";
                                acText.WidthFactor = 0.75;
                                acText.Rotation = temppcfix.gyro / -57.29578; // to radian
                                acText.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 10);
                                if (bool_seperatelayer)
                                    acText.Layer = layername;
                                acBlkTblRec.AppendEntity(acText);
                                tr.AddNewlyCreatedDBObject(acText, true);
                                acText.Hyperlinks.Add(hyper);

                                if (bool_plotcircle)
                                {
                                    acCircle = new Circle();
                                    acCircle.SetDatabaseDefaults();
                                    acCircle.Radius = acad_circleradius * 10;
                                    acCircle.Center = new Point3d((apcfix[i - 1].x + temppcfix.x) / 2, (apcfix[i - 1].y + temppcfix.y) / 2, 0);
                                    acCircle.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 10);
                                    if (bool_seperatelayer)
                                        acCircle.Layer = layername;
                                    acBlkTblRec.AppendEntity(acCircle);
                                    tr.AddNewlyCreatedDBObject(acCircle, true);
                                    acCircle.Hyperlinks.Add(hyper);
                                }

                                lastfix = temppcfix.fix - eachfix;
                            }

                            if (bool_ploteachfix & temppcfix.fix < (lastfix + eachfix))
                                lastfix = temppcfix.fix - eachfix;

                            if (bool_ploteachfix & temppcfix.fix == (lastfix + eachfix))
                            {
                                lastfix = temppcfix.fix;
                                acText = new DBText();
                                acText.SetDatabaseDefaults();
                                acText.Position = new Point3d(temppcfix.x, temppcfix.y, 0);
                                acText.Height = acad_textheight;
                                acText.TextString = $"{temppcfix.fix,7:0}";
                                acText.WidthFactor = 0.75;
                                acText.Rotation = temppcfix.gyro / -57.2957795130823; // to radian
                                if (bool_seperatelayer) acText.Layer = layername;
                                acBlkTblRec.AppendEntity(acText);
                                tr.AddNewlyCreatedDBObject(acText, true);
                                acText.Hyperlinks.Add(hyper);

                                if (bool_plotcircle)
                                {
                                    acCircle = new Circle();
                                    acCircle.SetDatabaseDefaults();
                                    acCircle.Radius = acad_circleradius;
                                    acCircle.Center = new Point3d(temppcfix.x, temppcfix.y, 0);
                                    if (bool_seperatelayer) acCircle.Layer = layername;
                                    acBlkTblRec.AppendEntity(acCircle);
                                    tr.AddNewlyCreatedDBObject(acCircle, true);
                                    acCircle.Hyperlinks.Add(hyper);
                                }
                            }
                        }

                        if (bool_plottrack)
                        {
                            if (bool_seperatelayer) acPoly.Layer = layername;
                            acPoly.Hyperlinks.Add(hyper);
                            acPoly.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, acad_linecolour);
                            acBlkTblRec.AppendEntity(acPoly);
                            tr.AddNewlyCreatedDBObject(acPoly, true);
                        }

                        // plot linename
                        if (bool_plotlinename)
                        {
                            // line name at end at start
                            acText = new DBText();
                            acText.SetDatabaseDefaults();
                            if (bool_fliplinename)
                                acText.Justify = AttachmentPoint.BottomLeft; // BL
                            else
                                acText.Justify = AttachmentPoint.BottomRight;// BR
                            acText.AlignmentPoint = new Point3d(apcfix[0].x, apcfix[0].y, 0);
                            acText.Height = acad_textheight;
                            acText.TextString = linename;
                            text_ro = Calc_HDG(apcfix[0].x, apcfix[0].y, apcfix[1].x, apcfix[1].y);
                            acText.Rotation = (text_ro - 90) / -57.29578; // to radian
                            acText.WidthFactor = 0.75;
                            if (bool_seperatelayer)
                                acText.Layer = layername;

                            acBlkTblRec.AppendEntity(acText);
                            tr.AddNewlyCreatedDBObject(acText, true);

                            // line name at end
                            fcount -= 1;
                            acText = new DBText();
                            acText.SetDatabaseDefaults();
                            if (bool_fliplinename)
                                acText.Justify = AttachmentPoint.BottomRight; // BR
                            else
                                acText.Justify = AttachmentPoint.BottomLeft;// BL
                            acText.AlignmentPoint = new Point3d(apcfix[fcount].x, apcfix[fcount].y, 0);
                            acText.Height = acad_textheight;
                            acText.TextString = linename;
                            text_ro = Calc_HDG(apcfix[fcount - 1].x, apcfix[fcount - 1].y, apcfix[fcount].x, apcfix[fcount].y);
                            acText.Rotation = (text_ro - 90) / -57.29578; // to radian
                            acText.WidthFactor = 0.75;
                            if (bool_seperatelayer) acText.Layer = layername;

                            acBlkTblRec.AppendEntity(acText);
                            tr.AddNewlyCreatedDBObject(acText, true);
                        }

                        // clear array and next pc file
                        apcfix.Clear();
                    }
                    tr.Commit();
                    db.Clayer = activelayer;
                    if (tfile > 1) myPGW.Close();
                }
            }
        }

        private static List<string> Open_PC_Files()
        {
            if (!System.IO.File.Exists("C:\\EGS\\FENWDKPRO_files.txt")) return new List<string>();
            string[] filelist = System.IO.File.ReadAllLines("C:\\EGS\\FENWDKPRO_files.txt");
            List<string> list = new List<string>(filelist);
            return list;
        }

        private static bool Prompt_Input()
        {
            var adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            PromptStringOptions pStrOpts = new PromptStringOptions("");
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            PromptResult pKeyRes;

            pKeyOpts.Keywords.Add("Yes");
            pKeyOpts.Keywords.Add("No");
            pKeyOpts.Keywords.Default = "No";
            pKeyOpts.AllowNone = true;

            pKeyOpts.Message = "\nPlot Track Line *ONLY* ? ";
            pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "Yes")
            {
                pKeyOpts.Message = "\nPlot to Seperate layers (by file name)? ";
                pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;
                bool_plottrack = true;
                bool_plotlinename = false;
                bool_ploteachfix = false;
                return true;
            }

            pKeyOpts.Message = "\nPlot Line name ? ";
            pKeyOpts.Keywords.Default = "Yes";
            pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_plotlinename = false;
            if (bool_plotlinename)
            {
                pKeyOpts.Message = "\nFlip line name ? ";
                pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_fliplinename = false;
            }

            pKeyOpts.Message = "\nPlot Fix ? ";
            pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_ploteachfix = false;
            if (bool_ploteachfix)
            {
                pStrOpts.Message = "\nEach how many fix ? ";
                pStrOpts.DefaultValue = "1";
                pStrOpts.AllowSpaces = false;
                pKeyRes = adoc.Editor.GetString(pStrOpts);
                eachfix = int.Parse(pKeyRes.StringResult);
            }

            if (bool_plotlinename || bool_ploteachfix)
            {
                pStrOpts.Message = "\nText Height ? ";
                pStrOpts.DefaultValue = "10";
                pStrOpts.AllowSpaces = false;
                pKeyRes = adoc.Editor.GetString(pStrOpts);
                acad_textheight = double.Parse(pKeyRes.StringResult);
            }

            pKeyOpts.Message = "\nPlot to Seperate layers (by file name)? ";
            pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;

            if (bool_seperatelayer)
            {
                pKeyOpts.Message = "\nMerge files along same lines (.001 .002... files) to one layer? ";
                pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_mergefiles = false;
            }

            pKeyOpts.Message = "\nRe-calc CMG for Fix# Rotations ? ";
            pKeyOpts.Keywords.Default = "No";
            pKeyRes = adoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "Yes") bool_recalc_cmg = true;

            return true;
        }

        private class Pcfix
        {
            public int fix;
            public float gyro;
            public double x;
            public double y;
        }
    }
}