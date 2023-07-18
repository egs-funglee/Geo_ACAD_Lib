using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class SBP_Fetch_Fix_to_CSV
    {
        internal static void Fetch_Fix_to_CSV_Profile()
        {
            Document adoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = adoc.Editor;
            Database db = adoc.Database;

            TypedValue[] acTypValAr = new TypedValue[1];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "Text"), 0);
            SelectionFilter filter = new SelectionFilter(acTypValAr);
            Autodesk.AutoCAD.DatabaseServices.DBText atext;

            PromptSelectionResult psr;
            int fcount = 0;
            List<string> astr = new List<string>();
            string promptstr;
            Point3d apoint;
            psr = ed.SelectAll(filter);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo Text in active drawing.\n");
                System.IO.File.WriteAllLines(@"C:\EGS\SBPHL_Temp.txt", astr);
                return;
            }

            ed.WriteMessage("\nReading Fix.\n");
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> layerlocked = new List<string>();
                LayerTable acLayerTbl = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId id in acLayerTbl)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (ltr.IsLocked | ltr.IsFrozen)
                        layerlocked.Add(ltr.Name);
                }
                if (layerlocked.Count > 0)
                {
                    promptstr = "The following frozen or locked layer(s) was/were ignored : ";
                    foreach (string tstr in layerlocked)
                        promptstr = promptstr + " " + tstr;
                    ed.WriteMessage($"\n{promptstr}\n");
                }

                foreach (SelectedObject so in psr.Value)
                {
                    atext = tr.GetObject(so.ObjectId, OpenMode.ForRead) as DBText;
                    if (atext.Justify != AttachmentPoint.MiddleCenter) continue;
                    if (!IsNumeric(atext.TextString)) continue;
                    if (layerlocked.Contains(atext.Layer)) continue;

                    if (atext.Hyperlinks.Count == 1)
                    {
                        if (IsStrEN(atext.Hyperlinks[0].Name))
                        {
                            apoint = atext.AlignmentPoint;
                            string tstr = $"{apoint[0]:F16},{apoint[1]:F4},{atext.TextString},{atext.Hyperlinks[0].Name}";
                            if (!astr.Contains(tstr))
                            {
                                astr.Add(tstr);
                                fcount += 1;
                            }
                            else
                                ed.WriteMessage($"\nDuplicate Fix# {atext.TextString}\n");
                        }
                        else
                            ed.WriteMessage($"No EN in Fix# {atext.TextString}\n");
                    }
                }
            }

            if (fcount > 0)
            {
                ed.WriteMessage($"\nFinish reading Fix. Total Fix# {fcount}\n");
                System.IO.File.WriteAllLines("C:\\EGS\\SBPHL_Temp.txt", astr);
                ed.WriteMessage("Temp file saved at: C:\\EGS\\SBPHL_Temp.txt\n");
            }
            else
            {
                ed.WriteMessage("\nFinish reading, but no Text/Fix has EN in the hyperlink\n");
                System.IO.File.WriteAllText(@"C:\EGS\SBPHL_Temp.txt", "");
            }
        }

        private static bool IsStrEN(string instr)
        {
            string[] tempstr = instr.Split(',');
            if (tempstr.Length == 2)
                if (IsNumericD(tempstr[0]) && IsNumericD(tempstr[1]))
                    return true;
            return false;
        }
    }
}