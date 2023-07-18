using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Geo_AC2022.SharedFunctions;

namespace Geo_AC2022
{
    internal class Class_RF
    {
        internal static List<RF> ReadRF(int vel_water)//(string[] filelist)
        {
            string[] filelist = OpenRFFiles();

            List<RF> lRF = new List<RF>();
            char[] charSeparators = new char[] { ' ' };

            foreach (string file in filelist)
            {
                string[] lines = System.IO.File.ReadAllLines(file);
                string[] tempstrs;
                string linename = System.IO.Path.GetFileName(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length == 0) continue;

                    RF iRF = new RF
                    {
                        xyzz = new List<XYZZ>(),
                        acolor = AciColor(linename[linename.Length - 1])
                    };
                    int sample_rate = 0;

                    switch (lines[i][0])
                    {
                        case 'S':
                            iRF.otype = 1;
                            iRF.linename = linename;
                            iRF.annotation = "REFLECTOR";
                            iRF.valid = false;
                            XYZZ oxyzz = new XYZZ();

                            for (int lastline = i + 1; lastline < lines.Length; lastline++)
                            {
                                if (!lines[lastline].StartsWith("RecNo"))
                                {
                                    lastline--;
                                    i = lastline;
                                    break;
                                }
                                tempstrs = lines[lastline].Split(charSeparators, 15, StringSplitOptions.RemoveEmptyEntries);

                                if (tempstrs.Length == 15)
                                {
                                    try //when cannot convert to double/int
                                    {
                                        XYZZ ixyzz = new XYZZ
                                        {
                                            X = double.Parse(tempstrs[7]),
                                            Y = double.Parse(tempstrs[9]),
                                            ZTxd = double.Parse(tempstrs[12]),
                                            ZSb = double.Parse(tempstrs[14]),
                                            Rec = int.Parse(tempstrs[1])
                                        };
                                        if (ixyzz.IsNotEquals(oxyzz))
                                        {
                                            iRF.xyzz.Add(ixyzz);
                                            if (sample_rate == 0)
                                                sample_rate = (int)(Math.Round(int.Parse(tempstrs[5]) * vel_water / 200 / (ixyzz.ZTxd - ixyzz.ZSb))) * 100;
                                            oxyzz = ixyzz;
                                        }
                                    }
                                    catch { }
                                }
                            }
                            if (iRF.xyzz.Count > 0)
                            {
                                iRF.sample_rate = sample_rate;
                                iRF.valid = true;
                                lRF.Add(iRF);
                            }
                            break;

                        case 'A':
                            iRF.otype = 2;
                            iRF.linename = linename;
                            iRF.valid = false;

                            tempstrs = lines[i + 1].Split(charSeparators, 17, StringSplitOptions.RemoveEmptyEntries);
                            if (tempstrs.Length == 17)
                            {
                                XYZZ ixyzz = new XYZZ
                                {
                                    X = double.Parse(tempstrs[7]),
                                    Y = double.Parse(tempstrs[9]),
                                    ZTxd = double.Parse(tempstrs[12]),
                                    ZSb = double.Parse(tempstrs[14]),
                                    Rec = int.Parse(tempstrs[1])
                                };
                                iRF.sample_rate = (int)(Math.Round(int.Parse(tempstrs[5]) * vel_water / 200 / (ixyzz.ZTxd - ixyzz.ZSb))) * 100;
                                iRF.annotation = tempstrs[16];
                                iRF.valid = true;
                                iRF.xyzz.Add(ixyzz);
                                i++;
                            }

                            if (iRF.valid) lRF.Add(iRF);
                            break;

                        default:
                            break;
                    }
                }
            }
            return lRF;
        }

        private static string[] OpenRFFiles()
        {
            List<string> fileslist = new List<string>();
            System.IO.Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            System.Collections.IEnumerable datfile;

            IniParser.Model.IniData ini = Read_ini();
            string sRF = ini["Last Path"]["RF Path"];
            if (System.IO.Directory.Exists(sRF))
                openFileDialog.InitialDirectory = sRF;

            openFileDialog.Title = "Open RF Files";
            openFileDialog.Filter = "All Files (*.*)|*.*|RF Files (*.RF*)|*.RF*";
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
                            if (System.IO.Path.GetExtension(filename).ToUpper().StartsWith(".RF"))
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
                    if ((myStream != null))
                        myStream.Close();
                }
            }

            if (fileslist.Count > 0)
            {
                ini["Last Path"]["RF Path"] = System.IO.Path.GetDirectoryName(fileslist[0]);
                Save_ini(ini);
            }

            return fileslist.ToArray();
        }
    }
}