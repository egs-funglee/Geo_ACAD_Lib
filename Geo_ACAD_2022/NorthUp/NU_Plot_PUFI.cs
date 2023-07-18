using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class NU_Plot_PUFI
    {
        private static double acad_textheight;
        private static bool bool_plottrack, bool_plotlinename, bool_ploteachfix, bool_seperatelayer, bool_recalc_cmg, bool_fliplinename, bool_plotcircle, bool_mergefiles;
        private static int eachfix, ffilterindex;

        internal static void Plot_PUFI_NU()
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

                // init field number for cnv file
                int f_fix = 7; // fix# default for cnv
                foreach (string filename in PCs)
                {
                    if (filename.ToUpper().EndsWith(".CNV"))
                    {
                        if (MessageBox.Show("Plot full data density of CNV files ?", "CNV Data Density",
                            MessageBoxButtons.YesNo) != DialogResult.No)
                        {
                            f_fix = 2; // rec#
                            acad_textheight /= 10;
                            bool_plotcircle = false;
                        }
                        break;
                    }
                }

                char[] charSeparators = new char[] { ' ' };
                double acad_circleradius = acad_textheight / 4;
                short acad_linecolour = 214;
                string linename, catchfix;
                string[] tempstr, wholefile;
                int i, fcount;
                double text_ro;
                string layername;
                int lastfix, thisfix;

                ProgressWind myPGW = new ProgressWind();
                Polyline acPoly; DBText acText; Circle acCircle; HyperLink hyper = new HyperLink();
                List<Pcfix> apcfix = new List<Pcfix>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    TextStyleTable textStyles = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
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
                            TextSize = 20,
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

                        // prepare layer
                        foreach (string filename in PCs)
                        {
                            layername = CleanLayerName(Path.GetFileNameWithoutExtension(filename));
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

                        if (filename.ToUpper().EndsWith(".FENWDKPRO"))
                        {
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
                        }
                        else if (filename.ToUpper().EndsWith(".CNV"))
                        {
                            for (i = 0; i < wholefile.Length; i++)
                            {
                                if (wholefile[i].Length > 0)
                                {
                                    tempstr = wholefile[i].Split(charSeparators, 9, StringSplitOptions.RemoveEmptyEntries);
                                    if (tempstr.Length >= 8)
                                    {
                                        thisfix = (int)double.Parse(tempstr[f_fix]);
                                        if (thisfix != lastfix && thisfix > 0)
                                        {
                                            lastfix = thisfix;
                                            Pcfix temppcfix = new Pcfix
                                            {
                                                fix = (int)double.Parse(tempstr[f_fix]),
                                                x = double.Parse(tempstr[3]),
                                                y = double.Parse(tempstr[4]),
                                                gyro = float.Parse(tempstr[5])
                                            };

                                            apcfix.Add(temppcfix);
                                        }
                                    }
                                }
                            }
                        }
                        else //pc files
                            for (i = 0; i < wholefile.Length; i++)
                            {
                                if (wholefile[i].Length > 0)
                                {
                                    switch (wholefile[i][0])
                                    {
                                        case '#': break;
                                        case '-': break;
                                        case '|': break;
                                        default:
                                            catchfix = wholefile[i].Substring(18, 6);
                                            if (IsNumeric(catchfix))
                                            {
                                                thisfix = int.Parse(catchfix); // Val() will give 0.0 when blank
                                                if (thisfix > 0 && thisfix != lastfix)
                                                {
                                                    lastfix = thisfix;
                                                    tempstr = wholefile[i].Split(charSeparators, 97, StringSplitOptions.RemoveEmptyEntries);
                                                    Pcfix temppcfix = new Pcfix
                                                    {
                                                        fix = thisfix,
                                                        x = double.Parse(tempstr[93]),
                                                        y = double.Parse(tempstr[95]),
                                                        gyro = float.Parse(tempstr[10])
                                                    };
                                                    apcfix.Add(temppcfix);
                                                }
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
                                    if (bool_seperatelayer)
                                        acCircle.Layer = layername;
                                    acBlkTblRec.AppendEntity(acCircle);
                                    tr.AddNewlyCreatedDBObject(acCircle, true);
                                    acCircle.Hyperlinks.Add(hyper);
                                }
                            }
                        }

                        if (bool_plottrack)
                        {
                            if (bool_seperatelayer)
                                acPoly.Layer = layername;
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
            List<string> OpenPCFiles = new List<string>();

            Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            System.Collections.IEnumerable datfile;

            IniParser.Model.IniData ini = Read_ini();
            string sPC = ini["Last Path"]["PC Path"];
            if (System.IO.Directory.Exists(sPC)) openFileDialog.InitialDirectory = sPC;
            ffilterindex = 2;
            sPC = ini["Parameters"]["Last Plot File Type"];
            if (!string.IsNullOrEmpty(sPC)) ffilterindex = int.Parse(sPC);

            openFileDialog.Title = "Open PC / FENWDKPRO / CNV Files";
            openFileDialog.Filter = "All Files (*.*)| *.*|PC Files (*.PC*)|*.PC*|FENWDKPRO Files (*.FENWDKPRO)|*.FENWDKPRO|CNV Files (*.CNV)|*.CNV";
            openFileDialog.Multiselect = true;
            openFileDialog.FilterIndex = ffilterindex;//2pc 3fen 4cnv;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    myStream = openFileDialog.OpenFile();
                    datfile = openFileDialog.FileNames;
                    if ((myStream != null))
                    {
                        foreach (string ifilename in datfile)
                        {
                            string filename = ifilename.Substring(ifilename.Length - 4, 4).ToUpper();
                            if ((filename.StartsWith(".PC") | ifilename.ToUpper().EndsWith(".FENWDKPRO") | filename.EndsWith(".CNV")))
                                OpenPCFiles.Add(ifilename);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Cannot read file from disk. Original error: " + Ex.Message);
                }
                finally
                {
                    if ((myStream != null))
                        myStream.Close();
                }
            }

            if (OpenPCFiles.Count > 0)
            {
                ini["Last Path"]["PC Path"] = System.IO.Path.GetDirectoryName(OpenPCFiles[0]);
                ini["Parameters"]["Last Plot File Type"] = openFileDialog.FilterIndex.ToString();
                Save_ini(ini);
            }

            return OpenPCFiles;
        }

        private static bool Prompt_Input()
        {
            IniParser.Model.IniData ini = Read_ini();
            string str_pKeyRes;

            bool_plottrack = true;
            bool_plotlinename = true;
            bool_ploteachfix = true;
            bool_seperatelayer = true;
            bool_mergefiles = true;
            bool_recalc_cmg = true;
            bool_fliplinename = true;
            bool_plotcircle = true;
            acad_textheight = 10;
            eachfix = 1;

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptStringOptions pStrOpts = new PromptStringOptions("");
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            PromptResult pKeyRes;

            pKeyOpts.Keywords.Add("Yes");
            pKeyOpts.Keywords.Add("No");
            str_pKeyRes = ini["Parameters"]["Plot Track Line Only"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "No";

            pKeyOpts.Keywords.Default = str_pKeyRes;
            pKeyOpts.AllowNone = true;

            pKeyOpts.Message = "\nPlot Track Line *ONLY* ? ";
            pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "Yes")
            {
                ini["Parameters"]["Plot Track Line Only"] = pKeyRes.StringResult;

                str_pKeyRes = ini["Parameters"]["Seperate Layer"];
                if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";
                pKeyOpts.Keywords.Default = str_pKeyRes;

                pKeyOpts.Message = "\nPlot to Seperate layers (by file name)? ";
                pKeyRes = ed.GetKeywords(pKeyOpts);

                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;

                bool_plottrack = true;
                bool_plotlinename = false;
                bool_ploteachfix = false;
                ini["Parameters"]["Seperate Layer"] = pKeyRes.StringResult;

                //merge .001 .002 files to one layer
                str_pKeyRes = ini["Parameters"]["Merge files"];//Read ini
                if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";//Set default when failed to read ini

                pKeyOpts.Message = "\nMerge files along same lines (.001 .002... files) to one layer? ";
                pKeyOpts.Keywords.Default = str_pKeyRes;

                pKeyRes = ed.GetKeywords(pKeyOpts);

                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_mergefiles = false;

                ini["Parameters"]["Merge files"] = pKeyRes.StringResult;//put back to ini

                Save_ini(ini);
                return true;
            }
            ini["Parameters"]["Plot Track Line Only"] = pKeyRes.StringResult;

            str_pKeyRes = ini["Parameters"]["Plot Line Name"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";
            pKeyOpts.Keywords.Default = str_pKeyRes;
            pKeyOpts.Message = "\nPlot Line Name ? ";
            pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_plotlinename = false;
            ini["Parameters"]["Plot Line Name"] = pKeyRes.StringResult;

            if (bool_plotlinename)
            {
                str_pKeyRes = ini["Parameters"]["Plot Line Name Flip"];
                if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";
                pKeyOpts.Keywords.Default = str_pKeyRes;
                pKeyOpts.Message = "\nFlip line name ? ";
                pKeyRes = ed.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK) return false;
                else if (pKeyRes.StringResult == "No") bool_fliplinename = false;
                ini["Parameters"]["Plot Line Name Flip"] = pKeyRes.StringResult;
            }

            str_pKeyRes = ini["Parameters"]["Plot Fix"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";
            pKeyOpts.Keywords.Default = str_pKeyRes;
            pKeyOpts.Message = "\nPlot Fix ? ";
            pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_ploteachfix = false;
            ini["Parameters"]["Plot Fix"] = pKeyRes.StringResult;

            if (bool_ploteachfix)
            {
                str_pKeyRes = ini["Parameters"]["Plot Each Fix"];
                if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "1";
                pStrOpts.DefaultValue = str_pKeyRes;
                pStrOpts.Message = "\nEach how many fix ? ";
                pStrOpts.AllowSpaces = false;
                pKeyRes = ed.GetString(pStrOpts);
                eachfix = int.Parse(pKeyRes.StringResult);
                ini["Parameters"]["Plot Each Fix"] = pKeyRes.StringResult;
            }

            if (bool_plotlinename || bool_ploteachfix)
            {
                str_pKeyRes = ini["Parameters"]["Text Height"];
                if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "10";
                pStrOpts.Message = "\nText Height ? ";
                pStrOpts.DefaultValue = str_pKeyRes;
                pStrOpts.AllowSpaces = false;
                pKeyRes = ed.GetString(pStrOpts);
                acad_textheight = double.Parse(pKeyRes.StringResult);
                ini["Parameters"]["Text Height"] = pKeyRes.StringResult;
            }

            str_pKeyRes = ini["Parameters"]["Seperate Layer"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";
            pKeyOpts.Keywords.Default = str_pKeyRes;
            pKeyOpts.Message = "\nPlot to Seperate layers (by file name)? ";
            pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;
            ini["Parameters"]["Seperate Layer"] = pKeyRes.StringResult;

            //merge .001 .002 files to one layer
            str_pKeyRes = ini["Parameters"]["Merge files"];//Read ini
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "Yes";//Set default when failed to read ini

            pKeyOpts.Message = "\nMerge files along same lines (.001 .002... files) to one layer? ";
            pKeyOpts.Keywords.Default = str_pKeyRes;

            pKeyRes = ed.GetKeywords(pKeyOpts);

            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "No") bool_mergefiles = false;

            ini["Parameters"]["Merge files"] = pKeyRes.StringResult;//put back to ini

            str_pKeyRes = ini["Parameters"]["Re-Calc CMG for Fix"];
            if (string.IsNullOrEmpty(str_pKeyRes)) str_pKeyRes = "No";
            pKeyOpts.Keywords.Default = str_pKeyRes;
            pKeyOpts.Message = "\nRe-Calc CMG for Fix# Rotations ? ";
            pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return false;
            else if (pKeyRes.StringResult == "Yes") bool_recalc_cmg = true;
            ini["Parameters"]["Re-Calc CMG for Fix"] = pKeyRes.StringResult;

            Save_ini(ini);
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