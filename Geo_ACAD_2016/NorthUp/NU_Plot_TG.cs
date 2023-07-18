using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class NU_Plot_TG
    {
        internal static void Plot_TG()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            //set seperate layer and merge files to one layer
            IniParser.Model.IniData ini = Read_ini();

            string str_pKeyRes = ini["Parameters"]["Seperate Layer"];//Read ini
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";//Set default when failed to read ini

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("")
            {
                AllowNone = true,
                Message = "\nPlot to Seperate layers (by file name)? "
            };
            pKeyOpts.Keywords.Add("Yes");
            pKeyOpts.Keywords.Add("No");
            pKeyOpts.Keywords.Default = str_pKeyRes;
            PromptResult pKeyRes = ed.GetKeywords(pKeyOpts);

            bool bool_seperatelayer = true;
            if (pKeyRes.Status != PromptStatus.OK) return;
            else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;

            ini["Parameters"]["Seperate Layer"] = pKeyRes.StringResult;//put back to ini

            //merge .001 .002 files to one layer
            str_pKeyRes = ini["Parameters"]["Merge files"];//Read ini
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";//Set default when failed to read ini

            pKeyOpts.Message = "\nMerge files along same lines (.001 .002... files) to one layer? ";
            pKeyOpts.Keywords.Default = str_pKeyRes;

            pKeyRes = ed.GetKeywords(pKeyOpts);

            bool bool_mergefiles = true;
            if (pKeyRes.Status != PromptStatus.OK) return;
            else if (pKeyRes.StringResult == "No") bool_mergefiles = false;

            ini["Parameters"]["Merge files"] = pKeyRes.StringResult;//put back to ini

            //set font height
            str_pKeyRes = ini["Parameters"]["Text Height"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "10";

            PromptStringOptions pStrOpts = new PromptStringOptions("")
            {
                Message = "\nText Height ? ",
                DefaultValue = str_pKeyRes,
                AllowSpaces = false
            };

            pKeyRes = ed.GetString(pStrOpts);
            double acad_textheight = double.Parse(pKeyRes.StringResult);

            ini["Parameters"]["Text Height"] = pKeyRes.StringResult;
            Save_ini(ini);//Save ini

            List<TG> lTG = Class_TG.ReadTG();
            if (lTG.Count == 0) return;//no files

            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                TextStyleTable textStyles = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);//set text style
                if (textStyles.Has("Arial75"))
                {
                    db.Textstyle = textStyles["Arial75"];
                }
                else
                {
                    textStyles.UpgradeOpen();
                    TextStyleTableRecord newTextStyleTableRecord = new TextStyleTableRecord
                    {
                        Name = "Arial75",
                        XScale = 0.75,
                        TextSize = acad_textheight,
                        Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Arial", false, false, 0, 0)
                    };
                    textStyles.Add(newTextStyleTableRecord);
                    tr.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
                    db.Textstyle = newTextStyleTableRecord.ObjectId;
                    textStyles.DowngradeOpen();
                }

                if (bool_seperatelayer)
                {
                    LayerTable acLayerTbl = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    string lastlayer = "0";

                    // prepare layers
                    foreach (TG iTG in lTG)
                    {
                        if (bool_mergefiles) iTG.layername = MergeFiles_to_One_LayerName(iTG.layername);//clean 001 002 tail

                        if (iTG.layername != lastlayer)
                        {
                            lastlayer = iTG.layername;
                            if (!acLayerTbl.Has(iTG.layername))
                            {
                                acLayerTbl.UpgradeOpen();
                                LayerTableRecord newLayer = new LayerTableRecord { Name = iTG.layername };

                                //string rlayername;
                                //if (iTG.layername.Length > 11) rlayername = iTG.layername.Substring(iTG.layername.Length - 11, 11);
                                //else rlayername = iTG.layername;

                                switch (iTG.layername)
                                {
                                    case string a when a.Contains("-CL"):
                                        newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 211);
                                        break;

                                    case string a when a.Contains("-L"):
                                        newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 31);
                                        break;

                                    case string a when a.Contains("-R"):
                                        newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 71);
                                        break;

                                    default:
                                        break;
                                };
                                acLayerTbl.Add(newLayer);
                                tr.AddNewlyCreatedDBObject(newLayer, true);
                                acLayerTbl.DowngradeOpen();
                            }
                        }
                    }
                }

                foreach (TG iTG in lTG)
                {
                    if (iTG.valid)
                    {
                        HyperLink hyper = new HyperLink
                        {
                            Description = iTG.linename,
                            Name = iTG.linename,
                            SubLocation = ""
                        };

                        switch (iTG.otype)
                        {
                            case 1://Polyline
                                Polyline acPoly = new Polyline(); acPoly.SetDatabaseDefaults();

                                for (int i = 0; i < iTG.xy.Count; i++)
                                    acPoly.AddVertexAt(i, new Point2d(iTG.xy[i].X, iTG.xy[i].Y), 0, 0, 0);
                                if (bool_seperatelayer) acPoly.Layer = iTG.layername;

                                acBlkTblRec.AppendEntity(acPoly); tr.AddNewlyCreatedDBObject(acPoly, true); acPoly.Hyperlinks.Add(hyper);
                                break;

                            case 2://Annotation
                                DBText acText = new DBText(); acText.SetDatabaseDefaults();

                                acText.Height = acad_textheight;
                                acText.TextString = iTG.annotation;
                                acText.WidthFactor = 0.75;
                                acText.Position = new Point3d(iTG.xy[0].X, iTG.xy[0].Y, 0);
                                if (bool_seperatelayer) acText.Layer = iTG.layername;

                                acBlkTblRec.AppendEntity(acText); tr.AddNewlyCreatedDBObject(acText, true); acText.Hyperlinks.Add(hyper);
                                break;

                            case 3://Contact
                                DBText acText1 = new DBText(); acText1.SetDatabaseDefaults();
                                DBText acText2 = new DBText(); acText2.SetDatabaseDefaults();
                                Circle acCirc = new Circle(); acCirc.SetDatabaseDefaults();

                                acText1.Height = acad_textheight;
                                acText1.TextString = iTG.annotation;
                                acText1.WidthFactor = 0.75;
                                acText1.Justify = AttachmentPoint.BottomLeft;
                                acText1.AlignmentPoint = new Point3d(iTG.xy[0].X, iTG.xy[0].Y, 0);

                                acText2.Height = acad_textheight;
                                acText2.TextString = iTG.dimension;
                                acText2.WidthFactor = 0.75;
                                acText2.Justify = AttachmentPoint.TopLeft;
                                acText2.AlignmentPoint = new Point3d(iTG.xy[0].X, iTG.xy[0].Y, 0);

                                acCirc.Center = new Point3d(iTG.xy[0].X, iTG.xy[0].Y, 0);
                                acCirc.Radius = acad_textheight / 4;

                                if (bool_seperatelayer)
                                {
                                    acText1.Layer = iTG.layername;
                                    acText2.Layer = iTG.layername;
                                    acCirc.Layer = iTG.layername;
                                }

                                acBlkTblRec.AppendEntity(acText1); tr.AddNewlyCreatedDBObject(acText1, true); acText1.Hyperlinks.Add(hyper);
                                acBlkTblRec.AppendEntity(acText2); tr.AddNewlyCreatedDBObject(acText2, true); acText2.Hyperlinks.Add(hyper);
                                acBlkTblRec.AppendEntity(acCirc); tr.AddNewlyCreatedDBObject(acCirc, true); acCirc.Hyperlinks.Add(hyper);
                                break;

                            default:
                                break;
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}