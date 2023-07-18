using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class NU_Insert_Geotiff
    {
        internal static void Insert_Geotiff()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Keywords.Add("Yes"); pKeyOpts.Keywords.Add("No"); pKeyOpts.Keywords.Default = "Yes";
            pKeyOpts.AllowNone = true;

            bool bool_seperatelayer = true;
            bool bool_mergefiles = true;

            pKeyOpts.Message = "\nPlot to Seperate layers (by file name)? ";
            PromptResult pKeyRes = ed.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK) return;
            else if (pKeyRes.StringResult == "No") bool_seperatelayer = false;

            if (bool_seperatelayer)
            {
                pKeyOpts.Message = "\nMerge files along same lines (.001 .002... files) to one layer? ";
                pKeyRes = ed.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK) return;
                else if (pKeyRes.StringResult == "No") bool_mergefiles = false;
            }

            List<TifFile> tiffiles = Open_Tiff_Files();
            if (tiffiles.Count == 0) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                if (bool_seperatelayer) // prepare layer
                {
                    LayerTable acLayerTbl = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    string lastlayer = "0";

                    foreach (TifFile itif in tiffiles)
                    {
                        string layername = CleanLayerName(itif.linename);
                        if (bool_mergefiles) layername = MergeFiles_to_One_LayerName(layername);
                        itif.layername = layername;

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

                ObjectId imageDictId = RasterImageDef.GetImageDictionary(db);
                if (imageDictId.IsNull) imageDictId = RasterImageDef.CreateImageDictionary(db);//if no image dict, make one
                DBDictionary imageDict = (DBDictionary)tr.GetObject(imageDictId, OpenMode.ForRead);

                foreach (TifFile itif in tiffiles)
                {
                    RasterImageDef imageDef; ObjectId imageDefId;

                    if (imageDict.Contains(itif.linename))//using exsiting imagedef
                    {
                        imageDefId = imageDict.GetAt(itif.linename);
                        imageDef = (RasterImageDef)tr.GetObject(imageDefId, OpenMode.ForWrite);
                    }
                    else
                    {
                        imageDef = new RasterImageDef() { SourceFileName = itif.path }; imageDef.Load();
                        imageDict.UpgradeOpen();
                        imageDefId = imageDict.SetAt(itif.linename, imageDef);//set name, def to dict, get def id
                        imageDict.DowngradeOpen();
                        tr.AddNewlyCreatedDBObject(imageDef, true);
                    }

                    RasterImage acimage = new RasterImage
                    {
                        ImageDefId = imageDefId,
                        ImageTransparency = true
                    };

                    if (bool_seperatelayer) acimage.Layer = itif.layername;

                    acimage.Orientation = new CoordinateSystem3d(
                        new Point3d(itif.ix, itif.iy, 0),
                        new Vector3d(itif.scalex * itif.iwidth, 0, 0),
                        new Vector3d(0, -itif.scaley * itif.iheight, 0));

                    acimage.ShowImage = true;

                    acBlkTblRec.AppendEntity(acimage); tr.AddNewlyCreatedDBObject(acimage, true);
                    acimage.AssociateRasterDef(imageDef);
                }
                RasterImage.EnableReactors(true);
                tr.Commit();
            }
        }

        private static List<TifFile> Open_Tiff_Files()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor; ed.WriteMessage("\n\n");
            List<TifFile> tiffiles = new List<TifFile>();

            System.IO.Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            System.Collections.IEnumerable datfile;

            IniParser.Model.IniData ini = Read_ini();
            string sTiff = ini["Last Path"]["Tiff Path"];
            if (System.IO.Directory.Exists(sTiff))
                openFileDialog.InitialDirectory = sTiff;

            openFileDialog.Title = "Open GeoTiff/JPG/PNG Files";
            openFileDialog.Filter = "All Files (*.*)|*.*|GeoTiff/JPG/PNG Files (*.Tiff;*.JPG;*.PNG)|*.tif;*.tiff;*.jpg;*.png";
            openFileDialog.Multiselect = true;
            openFileDialog.FilterIndex = 2;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    myStream = openFileDialog.OpenFile();
                    datfile = openFileDialog.FileNames;
                    if ((myStream != null))
                    {
                        foreach (string filename in datfile)
                        {
                            string tempstr = Path.GetExtension(filename).ToUpper();
                            if (tempstr.StartsWith(".TIF"))
                            {
                                TifFile itiffile = new TifFile
                                {
                                    path = filename,
                                    linename = CleanLayerName(Path.GetFileNameWithoutExtension(filename))
                                };
                                tempstr = filename.Substring(0, filename.Length - tempstr.Length) + ".tfw";
                                if (File.Exists(tempstr)) itiffile.has_tfw = true;

                                using (Tiff image = Tiff.Open(filename, "r"))//get image size, scale and insertion point
                                {
                                    if (image == null) continue;//cannot open file
                                    FieldValue[] value = image.GetField(TiffTag.IMAGEWIDTH);
                                    itiffile.iwidth = value[0].ToInt();

                                    value = image.GetField(TiffTag.IMAGELENGTH);
                                    itiffile.iheight = value[0].ToInt();

                                    value = image.GetField((TiffTag)33550);
                                    if (value != null)//no tag
                                    {
                                        double[] tempdbls = value[1].ToDoubleArray();
                                        itiffile.scalex = tempdbls[0];
                                        itiffile.scaley = -tempdbls[1];
                                    }
                                    else
                                        itiffile.isgeotiff = false;

                                    value = image.GetField((TiffTag)33922);
                                    if (value != null)//no tag
                                    {
                                        double[] tempdbls = value[1].ToDoubleArray();
                                        itiffile.easting = tempdbls[3];
                                        itiffile.northing = tempdbls[4];
                                    }
                                    else
                                        itiffile.isgeotiff = false;
                                }

                                if (itiffile.isgeotiff) //is geotiff use info in the tiff tag
                                {
                                    itiffile.ix = itiffile.easting;// - itiffile.scalex / 2;
                                    itiffile.iy = (itiffile.scaley * itiffile.iheight) + itiffile.northing;// - itiffile.scaley / 2;
                                    ed.WriteMessage($"{Path.GetFileName(itiffile.path)} is a GeoTiff. GeoTiff tags: 33550 & 33922 were used to insert\n");
                                }
                                else if (itiffile.has_tfw) //is not geotiff, no tiff tag but has tfw
                                {
                                    string[] tfws = System.IO.File.ReadAllLines(tempstr);
                                    double[] tempdbls;
                                    try
                                    {
                                        tempdbls = Array.ConvertAll(tfws, new Converter<string, double>(double.Parse));
                                        itiffile.scalex = tempdbls[0];
                                        itiffile.scaley = tempdbls[3];
                                        itiffile.easting = tempdbls[4];
                                        itiffile.northing = tempdbls[5];
                                        itiffile.ix = itiffile.easting - itiffile.scalex / 2;
                                        itiffile.iy = (itiffile.scaley * itiffile.iheight) + itiffile.northing - itiffile.scaley / 2;
                                        ed.WriteMessage($"{Path.GetFileName(itiffile.path)} is not a GeoTiff. Using info from associcated tfw file to insert.\n");
                                    }
                                    catch (Exception err)
                                    {
                                        itiffile.valid = false;
                                        itiffile.has_tfw = false;
                                        ed.WriteMessage($"\nCannot load tfw:\n{tempstr}\n\n" + err.Message);
                                    }
                                }
                                else //is not gettiff and no tfw, invalid file
                                {
                                    ed.WriteMessage($"* * * {Path.GetFileName(itiffile.path)} is not a GeoTiff and no associcated tfw file. File skipped. * * *\n");
                                    itiffile.valid = false;
                                }

                                if (itiffile.valid)
                                    tiffiles.Add(itiffile);
                            }

                            if (tempstr == ".JPG" || tempstr == ".PNG")
                            {
                                TifFile itiffile = new TifFile //GeoJPG has the same struct as GeoTiff
                                {
                                    path = filename,
                                    linename = CleanLayerName(Path.GetFileNameWithoutExtension(filename)),
                                    isgeotiff = false
                                };

                                tempstr = filename.Substring(0, filename.Length - tempstr.Length + 2) + "gw";//jgw pgw

                                if (File.Exists(tempstr))
                                {
                                    using (FileStream file = new FileStream(itiffile.path, FileMode.Open, FileAccess.Read))
                                    {
                                        using (System.Drawing.Image jpg = System.Drawing.Image.FromStream(stream: file,
                                                                            useEmbeddedColorManagement: false,
                                                                            validateImageData: false))
                                        {
                                            if (jpg == null) continue;//cannot open file
                                            itiffile.iwidth = ((int)jpg.PhysicalDimension.Width);
                                            itiffile.iheight = ((int)jpg.PhysicalDimension.Height);
                                        }
                                    }

                                    string[] jgws = System.IO.File.ReadAllLines(tempstr);
                                    double[] tempdbls;
                                    try
                                    {
                                        tempdbls = Array.ConvertAll(jgws, new Converter<string, double>(double.Parse));
                                        itiffile.scalex = tempdbls[0];
                                        itiffile.scaley = tempdbls[3];
                                        itiffile.easting = tempdbls[4];
                                        itiffile.northing = tempdbls[5];
                                        itiffile.ix = itiffile.easting - itiffile.scalex / 2;
                                        itiffile.iy = (itiffile.scaley * itiffile.iheight) + itiffile.northing - itiffile.scaley / 2;
                                    }
                                    catch (Exception err)
                                    {
                                        itiffile.valid = false;
                                        itiffile.has_tfw = false;
                                        ed.WriteMessage($"\nCannot load World file:\n{tempstr}\n\n" + err.Message);
                                    }

                                    if (itiffile.valid)
                                    {
                                        tiffiles.Add(itiffile);
                                    }
                                }
                                else
                                {
                                    ed.WriteMessage($"\nCannot find World file:\n{tempstr}\n\n");
                                }
                            }
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

            if (tiffiles.Count > 0)
            {
                ini["Last Path"]["Tiff Path"] = System.IO.Path.GetDirectoryName(tiffiles[0].path);
                Save_ini(ini);
            }

            return tiffiles;
        }

        private class TifFile
        {
            public double easting = 0;
            public bool has_tfw = false;
            public int iheight = 0;
            public bool isgeotiff = true;
            public int iwidth = 0;
            public double ix = 0;
            public double iy = 0;
            public string layername = "";
            public string linename = "";
            public double northing = 0;
            public string path = "";
            public double ro1 = 0;
            public double ro2 = 0;
            public double scalex = 0;
            public double scaley = 0;
            public bool valid = true;
        }
    }
}