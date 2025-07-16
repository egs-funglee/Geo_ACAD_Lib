using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Geo_AC2016.SharedFunctions;

namespace Geo_AC2016
{
    internal class Class_TG
    {
        internal static List<TG> ReadTG()//(string[] filelist)
        {
            string[] filelist = OpenTGFiles();

            List<TG> lTG = new List<TG>();
            char[] charSeparators = new char[] { ' ' };

            foreach (string filep in filelist)
            {
                string[] lines = System.IO.File.ReadAllLines(filep);
                string[] tempstrs;
                string linename = System.IO.Path.GetFileNameWithoutExtension(filep);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length == 0) continue;

                    TG iTG = new TG
                    {
                        xy = new List<XY>(),
                        linename = linename,
                        layername = CleanLayerName(linename),
                        valid = false
                    };

                    switch (lines[i][0])
                    {
                        case 'S': //Polyline
                            iTG.otype = 1;

                            for (int lastline = i + 1; lastline < lines.Length; lastline++)
                            {
                                if (!lines[lastline].StartsWith("RecNo"))
                                {
                                    lastline--;
                                    i = lastline;
                                    break;
                                }

                                tempstrs = lines[lastline].Split(charSeparators, 9, StringSplitOptions.RemoveEmptyEntries);

                                if (tempstrs.Length == 9)
                                {
                                    XY ixy = new XY
                                    {
                                        X = double.Parse(tempstrs[5]),
                                        Y = double.Parse(tempstrs[7])
                                    };
                                    iTG.xy.Add(ixy);
                                }
                            }

                            if (iTG.xy.Count > 1)
                            {
                                iTG.valid = true;
                                lTG.Add(iTG);
                            }

                            break;

                        case 'A': //Annotation
                            iTG.otype = 2;

                            if (lines[i + 1].StartsWith("RecNo"))
                            {
                                tempstrs = lines[i + 1].Split(charSeparators, 10, StringSplitOptions.RemoveEmptyEntries);
                                if (tempstrs.Length == 10)
                                {
                                    XY ixy = new XY
                                    {
                                        X = double.Parse(tempstrs[4]),
                                        Y = double.Parse(tempstrs[6])
                                    };
                                    iTG.annotation = tempstrs[9].Trim();
                                    iTG.valid = true;
                                    iTG.xy.Add(ixy);
                                    i++;
                                }
                                if (iTG.valid) lTG.Add(iTG);
                            }
                            break;

                        case 'C': //Contact
                            iTG.otype = 3;

                            if (lines[i + 1].StartsWith("RecNo"))
                            {
                                iTG.annotation = lines[i + 1].Substring(35, 21).Trim();
                                string tempstr = lines[i + 1].Substring(57, lines[i + 1].Length - 57);
                                tempstrs = tempstr.Split(charSeparators, 12, StringSplitOptions.RemoveEmptyEntries);
                                if (tempstrs.Length == 12)
                                {
                                    XY ixy = new XY
                                    {
                                        X = double.Parse(tempstrs[8]),
                                        Y = double.Parse(tempstrs[10])
                                    };
                                    iTG.Update_dimension(tempstrs[1], tempstrs[3], tempstrs[5], tempstrs[7]);
                                    iTG.valid = true;
                                    iTG.xy.Add(ixy);
                                    i++;
                                }
                                if (iTG.valid) lTG.Add(iTG);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            return lTG;
        }

        private static string[] OpenTGFiles()
        {
            List<string> fileslist = new List<string>();
            System.IO.Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            System.Collections.IEnumerable datfile;

            IniParser.Model.IniData ini = Read_ini();
            string sTG = ini["Last Path"]["TG Path"];
            if (System.IO.Directory.Exists(sTG))
                openFileDialog.InitialDirectory = sTG;

            openFileDialog.Title = "Open RF Files";
            openFileDialog.Filter = "All Files (*.*)|*.*|TG Files (*.TG*)|*.TG*";
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
                            if (filename.Substring(filename.Length - 4, 4).ToUpper().StartsWith(".TG"))
                                fileslist.Add(filename);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Cannot read file from disk. Original error: " + Ex.Message);
                }
                finally
                {
                    myStream?.Close();
                }
            }

            if (fileslist.Count > 0)
            {
                ini["Last Path"]["TG Path"] = System.IO.Path.GetDirectoryName(fileslist[0]);
                Save_ini(ini);
            }

            return fileslist.ToArray();
        }
    }
}