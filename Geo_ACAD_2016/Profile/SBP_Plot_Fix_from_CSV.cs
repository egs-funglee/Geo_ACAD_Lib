using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Geo_AC2016
{
    internal class SBP_Plot_Fix_from_CSV
    {
        internal static void Plot_Fix_from_CSV_Profile()
        {
            Document adoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = adoc.Editor;
            Database db = adoc.Database;

            string fixcsv = @"C:\EGS\SBP_Fix.txt";

            if (!System.IO.File.Exists(fixcsv))
            {
                ed.WriteMessage($"\n{fixcsv} is not exists.\n");
                return;
            }

            string[] fix = System.IO.File.ReadAllLines(fixcsv);
            if (fix.Length == 0)
            {
                ed.WriteMessage($"\n{fixcsv} is empty.\n");
                return;
            }

            string[] tempstr;
            DBText acText;
            Polyline acPoly;
            double gridx;
            string en;

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

                string lastlayer = "0";
                ObjectId activelayer = db.Clayer;

                foreach (string sline in fix)
                {
                    tempstr = sline.Split(','); // GridX0, Fix1, E2, N3, Layer4

                    if (tempstr[4] != lastlayer)
                    {
                        lastlayer = tempstr[4];
                        if (!acLayerTbl.Has(tempstr[4]))
                        {
                            acLayerTbl.UpgradeOpen();
                            LayerTableRecord newLayer = new LayerTableRecord { Name = tempstr[4] };
                            acLayerTbl.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                            acLayerTbl.DowngradeOpen();
                        }
                    }

                    if (tempstr[4] != "0")
                        db.Clayer = acLayerTbl[tempstr[4]];

                    gridx = double.Parse(tempstr[0]);
                    en = tempstr[2] + "," + tempstr[3];

                    HyperLink hyper = new HyperLink
                    {
                        Description = en,
                        Name = en,
                        SubLocation = ""
                    };

                    acPoly = new Polyline();
                    acPoly.SetDatabaseDefaults();
                    acPoly.AddVertexAt(0, new Point2d(gridx, 0), 0, 0, 0);
                    acPoly.AddVertexAt(1, new Point2d(gridx, 393.7), 0, 0, 0);
                    acPoly.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 214);

                    acText = new DBText();
                    acText.SetDatabaseDefaults();
                    acText.Justify = AttachmentPoint.MiddleCenter;
                    acText.AlignmentPoint = new Point3d(gridx, 437.89417382, 0);
                    acText.Height = 20;
                    acText.TextString = tempstr[1];

                    acBlkTblRec.AppendEntity(acText);
                    tr.AddNewlyCreatedDBObject(acText, true);
                    acText.Hyperlinks.Add(hyper);

                    acBlkTblRec.AppendEntity(acPoly);
                    tr.AddNewlyCreatedDBObject(acPoly, true);
                }

                tr.Commit();
                db.Clayer = activelayer;
            }
        }
    }
}