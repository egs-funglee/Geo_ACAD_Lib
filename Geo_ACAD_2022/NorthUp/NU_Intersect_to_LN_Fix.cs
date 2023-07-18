using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class NU_Intersect_to_LN_Fix
    {
        internal static void Extract_Line_and_Fix_for_Strip_Chart()
        {
            List<string> csvlines = new List<string>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            //selection sets of text and polyline
            TypedValue[] acTypValAr = new TypedValue[1];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), 0);
            SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
            PromptSelectionResult psr, psrA;
            psr = ed.SelectAll(acSelFtr);
            if (psr.Status != PromptStatus.OK) { ed.WriteMessage("\nNo Polyline in the active drawing.\n"); return; }
            if (psr.Value.Count == 0) { ed.WriteMessage("\nNo Polyline in the active drawing.\n"); return; }

            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "TEXT"), 0);
            acSelFtr = new SelectionFilter(acTypValAr);
            psrA = ed.SelectAll(acSelFtr);
            if (psrA.Status != PromptStatus.OK) { ed.WriteMessage("\nNo Text in the active drawing.\n"); return; }
            if (psrA.Value.Count == 0) { ed.WriteMessage("\nNo Text in the active drawing.\n"); return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //Get activelayername
                LayerTableRecord currentLayer = (LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead);
                string currentLayerName = currentLayer.Name;

                //Get Fix (Numeric text with Hyperlink)
                List<FENLN> FENLN_List = new List<FENLN>();//main list contain all fix info
                FENLN tFENLN = new FENLN();//temp Fix info
                ed.WriteMessage("\nReading Fix.\n");
                foreach (SelectedObject so in psrA.Value)
                {
                    DBText atext = tr.GetObject(so.ObjectId, OpenMode.ForRead) as DBText;
                    if (atext == null) continue;
                    if (atext.Justify != AttachmentPoint.BaseLeft) continue;
                    if (!IsNumeric(atext.TextString)) continue;
                    if (atext.Hyperlinks.Count != 1) continue;
                    if (atext.Hyperlinks[0].Name.EndsWith(".rpl", StringComparison.OrdinalIgnoreCase)) continue;

                    tFENLN = new FENLN
                    {
                        e = atext.Position.X,
                        n = atext.Position.Y,
                        linename = atext.Hyperlinks[0].DisplayString,
                        fix = Convert.ToInt16(atext.TextString)
                    };
                    FENLN_List.Add(tFENLN);
                }
                if (FENLN_List.Count == 0)
                {
                    ed.WriteMessage("\nNo Fix in the active drawing.\n");
                    return;
                }

                //Get Polylines from tracks(with hyperlinks and withour RPL) and activelayer
                ed.WriteMessage("\nReading Polylines.\n");
                List<Polyline> tracks = new List<Polyline>();
                List<Polyline> hlines = new List<Polyline>();
                foreach (SelectedObject so in psr.Value)
                {
                    Polyline apoly = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Polyline;
                    if (apoly.Hyperlinks.Count == 0)
                    { if (apoly.Layer == currentLayerName) hlines.Add(apoly); }
                    else if (apoly.Hyperlinks.Count == 1)
                        if (!apoly.Hyperlinks[0].Name.EndsWith(".rpl", StringComparison.OrdinalIgnoreCase))
                            tracks.Add(apoly);
                }
                if (hlines.Count == 0)
                {
                    ed.WriteMessage("\nNo Polyline in the active layer.\n");
                    return;
                }
                if (tracks.Count == 0)
                {
                    ed.WriteMessage("\nCannot read Track Polylines in the active drawing.\n");
                    return;
                }

                //Locate intersects
                ed.WriteMessage("\nLocating Intersects.\n");
                List<FENLN> xFENLN_List = new List<FENLN>();
                foreach (Polyline tpoly in tracks)
                {
                    foreach (Polyline hpoly in hlines)
                    {
                        Point3dCollection xpoints = new Point3dCollection();//intersect point
                        tpoly.IntersectWith(hpoly, Intersect.OnBothOperands, xpoints, IntPtr.Zero, IntPtr.Zero);
                        if (xpoints.Count > 0)
                            foreach (Point3d xpt in xpoints)
                            {
                                tFENLN = new FENLN
                                {
                                    fix = -1,
                                    dist = 0,
                                    e = xpt.X, //1st point only
                                    n = xpt.Y,
                                    linename = tpoly.Hyperlinks[0].Name
                                };
                                xFENLN_List.Add(tFENLN);
                            }
                    }
                }
                if (xFENLN_List.Count == 0)
                {
                    ed.WriteMessage("\nNo intersects.\n");
                    return;
                }

                //Calc the Fix number by the nearest two Fix
                FENLN_List = FENLN_List.OrderBy(a => a.linename).ThenBy(a => a.fix).ToList();
                List<string> xlines = new List<string>();
                foreach (FENLN tobj in xFENLN_List)
                {
                    if (!xlines.Contains(tobj.linename)) xlines.Add(tobj.linename);

                    List<FENLN> line_FENLN_List = FENLN_List.Where(a => a.linename == tobj.linename).ToList();

                    //foreach (FENLN fobj in line_FENLN_List) fobj.Upate_Dist(tobj.e, tobj.n);
                    Parallel.ForEach(line_FENLN_List, (fobj) => fobj.Upate_Dist(tobj.e, tobj.n));

                    //Get the first 2 obj
                    line_FENLN_List = line_FENLN_List.OrderBy(a => a.dist).ToList();
                    FENLN obj1 = line_FENLN_List[0];
                    FENLN obj2 = line_FENLN_List[1];
                    double ratio = obj1.dist + obj2.dist; //length between them

                    //check if xpoint is between them while distance AB = Ax + xB (round to 2 digits for compare)
                    bool xpoint_is_on_segment = Math.Round(ratio, 2)
                        == Math.Round(Math.Sqrt((obj1.e - obj2.e) * (obj1.e - obj2.e) + (obj1.n - obj2.n) * (obj1.n - obj2.n)), 2);

                    if (xpoint_is_on_segment)
                        if (obj2.fix > obj1.fix) //check order
                        {
                            ratio = obj1.dist / ratio;
                            tobj.dist = obj1.fix + ratio;
                        }
                        else
                        {
                            ratio = obj2.dist / ratio;
                            tobj.dist = obj2.fix + ratio;
                        }
                    else //not on segment
                    {
                        if (obj2.dist > obj1.dist) //check order, return closest fix
                            tobj.dist = obj1.fix;
                        else
                            tobj.dist = obj2.fix;
                    }
                }

                //Prepare CSV Header
                csvlines.Add("Line Name,Start F#,End F#,Start E,Start N,End E,End N,Start Pt Circle,End Pt Circle");

                //Pair the Fix (in .dist) to Start and End by sorting along same track
                xFENLN_List = xFENLN_List.OrderBy(a => a.linename).ThenBy(a => a.dist).ToList();
                foreach (string linename in xlines)
                {
                    List<FENLN> line_FENLN_List = xFENLN_List.Where(a => a.linename == linename).ToList();//find fix record of each track line crossed
                    line_FENLN_List = line_FENLN_List.OrderBy(a => a.dist).ToList();//order by fix

                    //pair records
                    bool paired = false;
                    string str_line = "";
                    FENLN lobj = new FENLN();
                    foreach (FENLN tobj in line_FENLN_List)
                    {
                        if (str_line.Length > 0)
                        {
                            str_line = str_line
                                + String.Format("{0:0.#}", lobj.dist) + ","
                                + String.Format("{0:0.#}", tobj.dist) + ","
                                + lobj.ToStringEN() + ","
                                + tobj.ToStringEN() + ","
                                + "\"circle " + lobj.ToStringEN() + " 10\","
                                + "\"circle " + tobj.ToStringEN() + " 10\"";
                            paired = true;
                        }
                        else
                        {
                            str_line = tobj.linename + ",";
                            lobj = tobj;
                            paired = false;
                        }

                        if (paired)
                        {
                            csvlines.Add(str_line);
                            str_line = "";
                        }
                        else if (line_FENLN_List.IndexOf(tobj) == line_FENLN_List.Count - 1)//orphan
                        {
                            str_line = str_line
                                + String.Format("{0:0.#}", lobj.dist) + ",,"
                                + lobj.ToStringEN() + ",,,"
                                + "\"circle " + lobj.ToStringEN() + " 10\"";
                            csvlines.Add(str_line);
                        }
                    }
                }

                //Write results
                ed.WriteMessage("\nWriting file.\n");
                string csvfilepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\"
                    + "LineName_Fix_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                System.IO.File.WriteAllLines(csvfilepath, csvlines);
                ed.WriteMessage($"Check the CSV files at: {csvfilepath}");
            }
        }
    }
}