using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.IO;
using COL.GlycoLib;
using ZedGraph;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
namespace COL.MultiGlycan
{
    public static class GenerateImages
    {
        private static int MaxDegreeParallelism = 8;

        public static void GenGlycanLcImg(MultiGlycanESI argMultiGlycanESI)
        {
            string dir = argMultiGlycanESI.ExportFilePath + "\\Pic";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            List<Color> LstColor = new List<Color>() { Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta, Color.DarkOliveGreen, Color.DarkOrchid, Color.DarkRed, Color.DarkSalmon, Color.DarkSeaGreen, Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue };
            List<ZedGraph.SymbolType> LstSymbol = new List<ZedGraph.SymbolType>() { SymbolType.Circle, SymbolType.Triangle, SymbolType.TriangleDown, SymbolType.XCross, SymbolType.Diamond, SymbolType.Plus, SymbolType.Square, SymbolType.Star, SymbolType.VDash };
            Dictionary<string, List<ClusteredPeak>> dictCluster = new Dictionary<string, List<ClusteredPeak>>();
            foreach (ClusteredPeak clsPeak in argMultiGlycanESI.MergedResultList)
            {
                if (!dictCluster.ContainsKey(clsPeak.GlycanKey))
                {
                    dictCluster.Add(clsPeak.GlycanKey, new List<ClusteredPeak>());
                }
                dictCluster[clsPeak.GlycanKey].Add(clsPeak);
            }
            Parallel.ForEach(dictCluster.Keys, new ParallelOptions() {MaxDegreeOfParallelism = MaxDegreeParallelism},Gkey =>
            //foreach (string Gkey in dictCluster.Keys)
            {
              
                string ProcessingGlycanKey = "";
                try
                {
                 
                    Dictionary<string, PointPairList> dictAdductPoints = new Dictionary<string, PointPairList>();
                    Dictionary<float, float> MergeIntensity = new Dictionary<float, float>();
                    List<float> Time = new List<float>();
                    float maxIntensity = 0;
                    List<Tuple<double, double>> lstPeakMargin = new List<Tuple<double, double>>();
                    foreach (ClusteredPeak clsPeak in dictCluster[Gkey])
                    {
                        lstPeakMargin.Add(new Tuple<double, double>(clsPeak.StartTime,clsPeak.EndTime));
                        foreach (MatchedGlycanPeak Peak in clsPeak.MatchedPeaksInScan)
                        {
                            if (!dictAdductPoints.ContainsKey(Peak.AdductString))
                            {
                                dictAdductPoints.Add(Peak.AdductString, new PointPairList());
                            }
                                float TimeKey = Convert.ToSingle(Peak.ScanTime);

                                dictAdductPoints[Peak.AdductString].Add(new PointPair(Peak.ScanTime, Peak.MSPoints[0].Intensity));
                                if (!MergeIntensity.ContainsKey(TimeKey))
                                {
                                    MergeIntensity.Add(TimeKey, 0);
                                }
                                MergeIntensity[TimeKey] = MergeIntensity[TimeKey] + Peak.MSPoints[0].Intensity;
                                if (maxIntensity <= MergeIntensity[TimeKey])
                                {
                                    maxIntensity = MergeIntensity[TimeKey];
                                }
                                if (!Time.Contains(TimeKey))
                                {
                                    Time.Add(TimeKey);
                                }
                        }
                    }

                    #region LC Images
                    using (ZedGraphControl zgcGlycan = new ZedGraphControl())
                    {
                        //---------------Generate Graph-----------------                        
                        ProcessingGlycanKey = Gkey;

                        zgcGlycan.Width = 2400;
                        zgcGlycan.Height = 1200;

                        GraphPane GP = zgcGlycan.GraphPane;
                        GP.Title.Text = "Glycan: " + Gkey;
                        GP.XAxis.Title.Text = "Scan time (min)";
                        GP.YAxis.Title.Text = "Abundance";
                        GP.CurveList.Clear();
                        int ColorIdx = 0;
                        int SymbolIdx = 0;
                        foreach (string Adduct in dictAdductPoints.Keys)
                        {

                            dictAdductPoints[Adduct].Sort(delegate(PointPair M1, PointPair M2) { return M1.X.CompareTo(M2.X); }); //Sort by time
                            List<double> Mzs = new List<double>();
                            List<double> Intensities = new List<double>();
                            foreach (PointPair pp in dictAdductPoints[Adduct])
                            {
                                if (Mzs.Contains(pp.X))
                                {
                                    int idx = Mzs.IndexOf(pp.X);
                                    Intensities[idx] = Intensities[idx] + pp.Y;
                                }
                                else
                                {
                                    Mzs.Add(pp.X);
                                    Intensities.Add(pp.Y);
                                }
                            }
                            LineItem Lne = GP.AddCurve(Adduct, Mzs.ToArray(), Intensities.ToArray(), LstColor[ColorIdx], LstSymbol[SymbolIdx]);
                            Lne.Line.IsSmooth = true;
                            Lne.Line.SmoothTension = 0.15f;
                            Lne.Symbol.Size = 3.0f;
                            Lne.Symbol.Fill = new Fill(LstColor[ColorIdx]);
                            Lne.Symbol.Fill.Type = FillType.Solid;
                            ColorIdx = (ColorIdx + 1) % LstColor.Count;
                            SymbolIdx = (SymbolIdx + 1) % LstSymbol.Count;

                        }
                        //Merge Intensity
                        Time.Sort();
                        PointPairList PPLMerge = new PointPairList();
                        foreach (float tim in Time)
                        {
                            PPLMerge.Add(Convert.ToSingle(tim.ToString("0.00")), MergeIntensity[tim]);
                        }
                        LineItem Merge = GP.AddCurve("Merge", PPLMerge, Color.Black, SymbolType.Star);
                        Merge.Symbol.Size = 3.0f;
                        Merge.Symbol.Fill = new Fill(Color.Black);
                        Merge.Symbol.Fill.Type = FillType.Solid;
                        Merge.Line.Style = DashStyle.Custom;
                        Merge.Line.DashOff = 1;
                        Merge.Line.DashOn = 1;
                        Merge.Line.Width = 3.0f;
                        Merge.Line.IsSmooth = true;
                        Merge.Line.SmoothTension = 0.15f;

                        //Peak Margin 

                        for (int i = 0; i < lstPeakMargin.Count; i++)
                        {
                            PointPairList PPLL = new PointPairList();
                            PPLL.Add(lstPeakMargin[i].Item1, 0);
                            PPLL.Add(lstPeakMargin[i].Item1, maxIntensity);
                            LineItem MarginL = GP.AddCurve("Margin" + i.ToString() + "Left", PPLL, Color.HotPink);
                            MarginL.Line.Style = DashStyle.Custom;
                            MarginL.Symbol.IsVisible = false;
                            MarginL.Line.Width = 3;
                            MarginL.Line.DashOn = 5;
                            MarginL.Line.DashOff = 10;
                            MarginL.Label.IsVisible = false;

                            PointPairList PPLR = new PointPairList();
                            PPLR.Add(lstPeakMargin[i].Item2, 0);
                            PPLR.Add(lstPeakMargin[i].Item2, maxIntensity);
                            LineItem MarginR = GP.AddCurve("Margin" + i.ToString() + "Right", PPLR, Color.HotPink);
                            MarginR.Line.Style = DashStyle.Custom;
                            MarginR.Symbol.IsVisible = false;
                            MarginR.Line.Width = 3;
                            MarginR.Line.DashOn = 5;
                            MarginR.Line.DashOff = 10;
                            MarginR.Label.IsVisible = false;

                            PointPairList PPLH = new PointPairList();
                            PPLH.Add(lstPeakMargin[i].Item1, maxIntensity);
                            PPLH.Add(lstPeakMargin[i].Item2, maxIntensity);
                            LineItem MarginH = GP.AddCurve("Margin" + i.ToString() + "H", PPLH, Color.Cyan);
                            MarginH.Line.Style = DashStyle.Custom;
                            MarginH.Symbol.Type = SymbolType.VDash;
                            MarginH.Line.Width = 3;
                            MarginH.Line.DashOn = 1;
                            MarginH.Line.DashOff = 3;
                            MarginH.Label.IsVisible = false;
                        }
                        zgcGlycan.AxisChange();
                        zgcGlycan.Refresh();
                        zgcGlycan.Validate();
                        using(Bitmap pic = new Bitmap(zgcGlycan.GraphPane.GetImage()))
                        {                        
                           pic .Save(dir + "\\" + Gkey + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    throw new Exception("GetLC Pic failed " + ProcessingGlycanKey + "  Err Msg:" + ex.ToString());
                }      
            });
        }
        public static void GenGlycanLcImg(string argAllFile, string argExportFolder, out List<string> errorMsgs)
        {
            string Dir = argExportFolder + "\\Pic";
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            List<Color> LstColor = new List<Color>() { Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta, Color.DarkOliveGreen, Color.DarkOrchid, Color.DarkRed, Color.DarkSalmon, Color.DarkSeaGreen, Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue };
            List<ZedGraph.SymbolType> LstSymbol = new List<ZedGraph.SymbolType>() { SymbolType.Circle, SymbolType.Triangle, SymbolType.TriangleDown, SymbolType.XCross, SymbolType.Diamond, SymbolType.Plus, SymbolType.Square, SymbolType.Star, SymbolType.VDash };

            //Get Title
            Dictionary<string, int> dictTitle = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(argAllFile);
            string tmp = "";
            bool isLabeling = false;
            tmp = sr.ReadLine();
            for (int i = 0; i < tmp.Split(',').Length; i++)
            {
                dictTitle.Add(tmp.Split(',')[i], i);
            }
            if (dictTitle.ContainsKey("Label Tag"))
            {
                isLabeling = true;
            }
            Dictionary<string, Dictionary<string, Dictionary<float, float>>> dictData =
                new Dictionary<string, Dictionary<string, Dictionary<float, float>>>();
            //                Key-Label_Tag,      Adduct                      time    , intensity
            do
            {
                tmp = sr.ReadLine();
                if (tmp == null)
                {
                    break;
                }
                string[] tmpAry = tmp.Split(',');
                string GlycanKey = tmpAry[dictTitle["HexNac-Hex-deHex-NeuAc-NeuGc"]];
                string Adduct = "";
                if (isLabeling)
                {
                    GlycanKey = GlycanKey + "-" + tmpAry[dictTitle["Label Tag"]];
                }
                for (int i = 0; i < tmpAry[dictTitle["Adduct"]].Trim().Split(';').Length; i++)
                {
                    if (tmpAry[dictTitle["Adduct"]].Trim().Split(';')[i] == "")
                    {
                        continue;
                    }
                    Adduct = Adduct + tmpAry[dictTitle["Adduct"]].Trim().Split(';')[i].Trim().Split(' ')[0] + "+";
                }
                Adduct = Adduct.Substring(0, Adduct.Length - 1);

                float time = Convert.ToSingle(tmpAry[dictTitle["Time"]]);
                float intensity = Convert.ToSingle(tmpAry[dictTitle["Abundance"]]);

                if (!dictData.ContainsKey(GlycanKey))
                {
                    dictData.Add(GlycanKey, new Dictionary<string, Dictionary<float, float>>());
                }
                if (!dictData[GlycanKey].ContainsKey(Adduct))
                {
                    dictData[GlycanKey].Add(Adduct, new Dictionary<float, float>());
                }
                if (!dictData[GlycanKey][Adduct].ContainsKey(time))
                {
                    dictData[GlycanKey][Adduct].Add(time, 0);
                }
                dictData[GlycanKey][Adduct][time] = dictData[GlycanKey][Adduct][time] + intensity;
            } while (!sr.EndOfStream);
            sr.Close();


            string ProcessingGlycanKey = "";

            #region Get Data

            List<string> imgErrorMsg = new List<string>();
            //foreach (string Gkey in dictData.Keys)
            Parallel.ForEach(dictData.Keys, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeParallelism }, Gkey =>
            {
                
                try
                {
                    
                    Dictionary<string, PointPairList> dictAdductPoints = new Dictionary<string, PointPairList>();
                    Dictionary<float, float> MergeIntensity = new Dictionary<float, float>();
                    List<float> Time = new List<float>();

                    foreach (string adductKey in dictData[Gkey].Keys)
                    {
                        string adduct = "";
                        adduct = adduct + adductKey + "+";
                        adduct = adduct.Substring(0, adduct.Length - 1);
                        if (!dictAdductPoints.ContainsKey(adduct))
                        {
                            dictAdductPoints.Add(adduct, new PointPairList());
                        }
                        foreach (float TimeKey in dictData[Gkey][adductKey].Keys)
                        {
                            dictAdductPoints[adduct].Add(TimeKey, dictData[Gkey][adductKey][TimeKey]);
                            if (!MergeIntensity.ContainsKey(TimeKey))
                            {
                                MergeIntensity.Add(TimeKey, 0);
                            }
                            MergeIntensity[TimeKey] = MergeIntensity[TimeKey] + dictData[Gkey][adductKey][TimeKey];

                            if (!Time.Contains(TimeKey))
                            {
                                Time.Add(TimeKey);
                            }
                        }
                    }

            #endregion

                    #region LC Images
    

                    using (ZedGraphControl zgcGlycan = new ZedGraphControl())
                    {
                        ProcessingGlycanKey = Gkey;

                        zgcGlycan.Width = 2400;
                        zgcGlycan.Height = 1200;

                        GraphPane GP = zgcGlycan.GraphPane;
                        GP.Title.Text = "Glycan: " + Gkey;
                        GP.XAxis.Title.Text = "Scan time (min)";
                        GP.YAxis.Title.Text = "Abundance";
                        GP.CurveList.Clear();
                        //---------------Generate Graph-----------------
                        int ColorIdx = 0;
                        int SymbolIdx = 0;
                        foreach (string Adduct in dictAdductPoints.Keys)
                        {
                            dictAdductPoints[Adduct].Sort(delegate(PointPair M1, PointPair M2)
                            {
                                return M1.X.CompareTo(M2.X);
                            }
                                );
                            List<double> Mzs = new List<double>();
                            List<double> Intensities = new List<double>();
                            foreach (PointPair pp in dictAdductPoints[Adduct])
                            {
                                if (Mzs.Contains(pp.X))
                                {
                                    int idx = Mzs.IndexOf(pp.X);
                                    Intensities[idx] = Intensities[idx] + pp.Y;
                                }
                                else
                                {
                                    Mzs.Add(pp.X);
                                    Intensities.Add(pp.Y);
                                }
                            }
                            LineItem Lne = GP.AddCurve(Adduct, Mzs.ToArray(), Intensities.ToArray(), LstColor[ColorIdx % 17], LstSymbol[SymbolIdx % 9]);
                            Lne.Line.IsSmooth = true;
                            Lne.Line.SmoothTension = 0.15f;
                            Lne.Symbol.Size = 3.0f;
                            Lne.Symbol.Fill = new Fill(LstColor[ColorIdx % 17]);
                            Lne.Symbol.Fill.Type = FillType.Solid;
                            ColorIdx = ColorIdx + 1;
                            SymbolIdx = SymbolIdx + 1;
                        }
                        //Merge Intensity
                        Time.Sort();
                        PointPairList PPLMerge = new PointPairList();
                        foreach (float tim in Time)
                        {
                            PPLMerge.Add(Convert.ToSingle(tim.ToString("0.00")), MergeIntensity[tim]);
                        }
                        LineItem Merge = GP.AddCurve("Merge", PPLMerge, Color.Black, SymbolType.Star);
                        Merge.Symbol.Size = 3.0f;
                        Merge.Symbol.Fill = new Fill(Color.Black);
                        Merge.Symbol.Fill.Type = FillType.Solid;
                        Merge.Line.Width = 3.0f;
                        Merge.Line.IsSmooth = true;
                        Merge.Line.SmoothTension = 0.15f;
                        zgcGlycan.AxisChange();
                        zgcGlycan.Refresh();
                        zgcGlycan.Invalidate();
                        using (Bitmap pic = new Bitmap( zgcGlycan.GraphPane.GetImage()))
                        {
                            pic.Save(Dir + "\\" + Gkey + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(0);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();
                    imgErrorMsg.Add("GetLC Pic failed "+Dir+"\\" + ProcessingGlycanKey + "  @ " + line + "  Err Msg:" + ex.ToString());
                } 
            });            
            errorMsgs = imgErrorMsg;
        }

        public static void GenQuantImg(string argQuantFile, enumGlycanLabelingMethod argLabelingMethod,
            string argExportFolder)
        {
            string Dir = argExportFolder + "\\Pic";
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }

            List<Color> LstColor = new List<Color>()
            {
                Color.DarkCyan,
                Color.DarkGoldenrod,
                Color.DarkGray,
                Color.DarkGreen,
                Color.DarkKhaki,
                Color.DarkMagenta,
                Color.DarkOliveGreen,
                Color.DarkOrchid,
                Color.DarkRed,
                Color.DarkSalmon,
                Color.DarkSeaGreen,
                Color.DarkSlateBlue,
                Color.DarkSlateGray,
                Color.DarkTurquoise,
                Color.DarkViolet,
                Color.DeepPink,
                Color.DeepSkyBlue
            };
            List<ZedGraph.SymbolType> LstSymbol = new List<ZedGraph.SymbolType>()
            {
                SymbolType.Circle, 
                SymbolType.Triangle, 
                SymbolType.TriangleDown, 
                SymbolType.XCross,
                SymbolType.Diamond, 
                SymbolType.Plus, 
                SymbolType.Square, 
                SymbolType.Star, 
                SymbolType.VDash
            };
            //Get Title


            Dictionary<string, int> dictTitle = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(argQuantFile);
            string tmp = "";
            bool isLabeling = false;
            tmp = sr.ReadLine();

            #region Read Title

            string[] tmpAry = tmp.Split(',');
            string LabelTitle = "";
            dictTitle.Add("Glycan", 0);
            List<string> lstLabelingTag = new List<string>();
            if (argLabelingMethod == enumGlycanLabelingMethod.DRAG)
            {
                lstLabelingTag.Add("DRAG_Light(Adjusted 1)");
                lstLabelingTag.Add("DRAG_Heavy(Adjusted 1)");
            }
            else if (argLabelingMethod == enumGlycanLabelingMethod.MultiplexPermethylated)
            {
                lstLabelingTag.Add("MP_CH3");
                lstLabelingTag.Add("MP_CH2D");
                lstLabelingTag.Add("MP_CHD2");
                lstLabelingTag.Add("MP_CD3");
                lstLabelingTag.Add("MP_13CH3");
                lstLabelingTag.Add("MP_13CHD2");
                lstLabelingTag.Add("MP_13CD3");
            }
            for (int i = 1; i < tmpAry.Length; i++)
            {
                if (argLabelingMethod == enumGlycanLabelingMethod.MultiplexPermethylated &&
                    tmpAry[i].StartsWith("MP"))
                {
                    dictTitle.Add(tmpAry[i], i + 3);
                    // MP_CH3	Normalization Factor	 Estimated Purity	Normalizted and Adjusted Abundance (Adjusted Factor=1)
                }
                else if (argLabelingMethod == enumGlycanLabelingMethod.DRAG &&
                         tmpAry[i].Contains("DRAG_Light(Adjusted 1)") ||
                         tmpAry[i].Contains("DRAG_Heavy(Adjusted 1)"))
                {
                    dictTitle.Add(tmpAry[i], i);
                }
            }

            #endregion

            #region Get Data

            Dictionary<string, Dictionary<string, double>> dictData =
                new Dictionary<string, Dictionary<string, double>>();
            do
            {
                tmp = sr.ReadLine();
                tmpAry = tmp.Split(',');
                string GlycanKey = tmpAry[dictTitle["Glycan"]];
                if (!dictData.ContainsKey(GlycanKey))
                {
                    dictData.Add(GlycanKey, new Dictionary<string, double>());
                }
                foreach (string LabelTag in lstLabelingTag)
                {
                    if (!dictTitle.ContainsKey(LabelTag))
                    {
                        continue;
                    }
                    if (tmpAry[dictTitle[LabelTag]] != "N/A")
                    {
                        double intensity = Convert.ToDouble(tmpAry[dictTitle[LabelTag]]);
                        if (intensity < 0)
                        {
                            intensity = 0;
                        }
                        dictData[GlycanKey].Add(LabelTag, intensity);
                    }
                }
            } while (!sr.EndOfStream);
            sr.Close();

            #endregion

            #region Generate Quant Images

            //foreach(string Gkey in dictData.Keys)
            //ZedGraph.ZedGraphControl zgcGlycan = null;
            Parallel.ForEach(dictData.Keys, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeParallelism }, Gkey =>
            {
                ZedGraphControl zgcGlycan = null;
                try
                {

                    zgcGlycan = new ZedGraphControl();
                    zgcGlycan.Width = 2400;
                    zgcGlycan.Height = 1200;
                    zgcGlycan.GraphPane.CurveList.Clear();
                    GraphPane GP = zgcGlycan.GraphPane;
                    GP.Title.Text = "Glycan: " + Gkey;
                    GP.XAxis.Title.Text = "Labeling";
                    GP.YAxis.Title.Text = "Abundance(%)";
                    GP.CurveList.Clear();
                    Dictionary<enumLabelingTag, double> dictLabelIntensity = new Dictionary<enumLabelingTag, double>();
                    double YMax = 0;

                    //Find YMax
                    foreach (double intensity in dictData[Gkey].Values)
                    {
                        if (intensity > YMax)
                        {
                            YMax = intensity;
                        }
                    }
                    List<string> labels = new List<string>();
                    PointPairList ppl = new PointPairList();

                    int i = 0;
                    foreach (string labelTag in lstLabelingTag)
                    {
                        labels.Add(labelTag);
                        if (!dictTitle.ContainsKey(labelTag) || !dictData[Gkey].ContainsKey(labelTag))
                        {
                            ppl.Add(i, 0);
                        }
                        else
                        {
                            ppl.Add(i, dictData[Gkey][labelTag] / YMax * 100);
                        }
                        i++;
                    }
                    BarItem myBar = GP.AddBar("Data", ppl, Color.Red);
                    myBar.Bar.Fill.Type = FillType.Solid;
                    for (int j = 0; j < myBar.Points.Count; j++)
                    {
                        TextObj barLabel = new TextObj(myBar.Points[j].Y.ToString("0.00"), myBar.Points[j].X + 1,
                            myBar.Points[j].Y + 5);
                        barLabel.FontSpec.Border.IsVisible = false;
                        GP.GraphObjList.Add(barLabel);
                    }
                    myBar.Label.IsVisible = true;
                    GP.Legend.IsVisible = false;
                    GP.XAxis.Type = AxisType.Text;
                    GP.XAxis.Scale.TextLabels = labels.ToArray();
                    GP.XAxis.MajorTic.IsAllTics = false;
                    zgcGlycan.AxisChange();
                    zgcGlycan.Refresh();

                    zgcGlycan.MasterPane.GetImage()
                        .Save(Dir + "\\Quant-" + Gkey + ".png", System.Drawing.Imaging.ImageFormat.Png);

            #endregion
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (zgcGlycan != null)
                    {
                        zgcGlycan.Dispose();
                        zgcGlycan = null;
                    }
                }
            });
        }
    }
}
