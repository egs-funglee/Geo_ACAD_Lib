using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Geo_AC2022
{
    internal class Polyline1
    {
        internal static void Fix_Polylines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            TypedValue[] acTypValAr = new TypedValue[1];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "Lwpolyline"), 0); //is lwpolyline
            //acTypValAr.SetValue(new TypedValue((int)DxfCode.Int16, 1), 1); //is closed
            SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
            PromptSelectionResult psr;
            psr = ed.SelectAll(acSelFtr);
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo Polyline was found on this drawing.\n");
                return;
            }
            if (psr.Value.Count == 0)
            {
                ed.WriteMessage("\nNo Polyline was found on this drawing.\n");
                return;
            }

            int c_count = 0;
            int c_opcount = 0;
            int c_addvertex = 0;
            bool closed_lwp_detected = false;

            int rm_vertex_removed = 0;
            int rm_count = 0;
            bool empty_vertex_detected = false;

            int z_count = 0;
            int z_erasecount = 0;
            bool zero_length_detected = false;

            Point2d pt2d_zero = new Point2d(0, 0);

            ed.WriteMessage("\nThis function is to Erase Zero Length Polylines and Fix Closed Polylines (by appending 1st Vertex and the end).\n");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    Polyline acPoly = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Polyline;

                    if (acPoly.Length == 0)
                    {
                        zero_length_detected = true;
                        z_count++; //Number of Zero Length Polyline were found
                        try
                        {
                            acPoly.UpgradeOpen();
                            acPoly.Erase();
                            acPoly.DowngradeOpen();
                            z_erasecount++; //Number of Zero Length Polyline were erased
                        }
                        catch { }
                        continue;
                    }
                    else if (acPoly.Closed) //if the LWP is closed
                    {
                        closed_lwp_detected = true;
                        c_count++; //Number of Closed Polyline were found
                        try //to open it
                        {
                            acPoly.UpgradeOpen();
                            acPoly.Closed = false;
                            c_opcount++; //Number of Closed Polyline were opened
                        }
                        catch
                        {
                            continue; //skip end point check.
                        }

                        int vn = acPoly.NumberOfVertices;
                        Point2d pt2d0 = acPoly.GetPoint2dAt(0); //1st vertex, 0-based
                        Point2d pt2d1 = acPoly.GetPoint2dAt(vn - 1); //last vertex

                        if (!pt2d1.Equals(pt2d0)) //check if last vertex equal to first vertex
                        {
                            try //to append 1st vertex at the end
                            {
                                acPoly.AddVertexAt(vn, pt2d0, 0, 0, 0);
                                c_addvertex++;
                            }
                            finally { acPoly.DowngradeOpen(); }
                        }
                    }

                    Point2d pt2do = acPoly.GetPoint2dAt(0); //1st vertex, 0-based
                    int v = 1;
                    int vm = acPoly.NumberOfVertices;
                    while (v < vm)
                    {
                        Point2d pt2d = acPoly.GetPoint2dAt(v);
                        if (pt2d.Equals(pt2d_zero) || pt2d.Equals(pt2do) || double.IsNaN(pt2d.X) || double.IsNaN(pt2d.Y))
                        {
                            empty_vertex_detected = true;
                            rm_count++;
                            try
                            {
                                acPoly.UpgradeOpen();
                                acPoly.RemoveVertexAt(v);
                                acPoly.DowngradeOpen();
                                vm--; //NumberOfVertices reduced
                                rm_vertex_removed++; //# of vertex removed
                            }
                            catch { break; }
                        }
                        else
                        {
                            pt2do = pt2d;
                            v++; //check next point
                        }
                    }

                }

                if (closed_lwp_detected)
                {
                    ed.WriteMessage($"\nNumber of Closed Polyline checked : {c_count}\n" +
                        $"\nNumber of Fixed Polyline: {c_opcount}\n" +
                        $"\nNumber of Vertex Added: {c_addvertex}\n");
                    if (!(c_count == c_opcount))
                        ed.WriteMessage($"\n{c_count - c_opcount} Closed Polylines are in Locked layer, they are not modified.\n");
                }

                if (zero_length_detected)
                {
                    ed.WriteMessage($"\nNumber of Zero Length Polyline were found : {z_count}\n" +
                        $"\nNumber of Zero Length Polyline were Erased : {z_erasecount}\n");
                    if (!(z_count == z_erasecount))
                        ed.WriteMessage($"\n{z_count - z_erasecount} Zero Length Polylines are in Locked layer, they are not modified.\n");
                }

                if (empty_vertex_detected)
                {
                    ed.WriteMessage($"\nNumber of Empty Vertex were found : {rm_count}\n" +
                        $"\nNumber of Vertex were Erased : {rm_vertex_removed}\n");
                    if (!(rm_count == rm_vertex_removed))
                        ed.WriteMessage($"\n{rm_count - rm_vertex_removed} Zero Length Polylines are in Locked layer, they are not modified.\n");
                }

                if (closed_lwp_detected || zero_length_detected || empty_vertex_detected)
                {
                    tr.Commit();
                    ed.WriteMessage($"\nDone.\n");
                }
                else
                    ed.WriteMessage($"\nNo Zero Length or Closed Polyline was found on this drawing.\n");
            }
        }
    }
}