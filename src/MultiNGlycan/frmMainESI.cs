﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using COL.MassLib;
using Microsoft.Win32;
using ZedGraph;
using CSMSL.Chemistry;
using CSMSL.Spectral;
namespace COL.MultiGlycan
{
    public partial class frmMainESI : Form
    {
        frmPeakParameters frmPeakpara;
        bool DoLog = false;
        private int _endScan = 0;
        COL.MassLib.ThermoRawReader raw;
        public frmMainESI()
        {
            InitializeComponent();
            this.Text = this.Text + "  " + AssemblyVersion.Split('.')[0] + "." + AssemblyVersion.Split('.')[1];// +" (build: " + AssemblyVersion.Split('.')[2] + ")"; 
            cboSia.SelectedIndex = 0;
            //int MaxCPU = Environment.ProcessorCount;
            //for (int i = 1; i <= MaxCPU; i++)
            //{
            //    cboCPU.Items.Add(i); 
            //}
            //cboCPU.SelectedIndex = (int)Math.Floor(cboCPU.Items.Count / 2.0f)-1;   
          
        }


        private void btnBrowseRaw_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "RAW Files (*.raw; *.mzXML)|*.raw;*.mzxml";
            openFileDialog1.Filter = "RAW Files (*.raw)|*.raw";
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtRawFile.Text = openFileDialog1.FileName;

                if (Path.GetExtension(openFileDialog1.FileName) == ".raw")
                {
                    raw = new COL.MassLib.ThermoRawReader(txtRawFile.Text);
                    _endScan = raw.NumberOfScans;
                }
                //else
                //{
                //    COL.MassLib.RawReader raw = new COL.MassLib.RawReader(txtRawFile.Text,"mzxml");
                //    _endScan = raw.NumberOfScans;
                //}
                txtEndScan.Text = _endScan.ToString();
            }
        }

        private void rdoDefaultList_CheckedChanged(object sender, EventArgs e)
        {
            rdoUserList.Checked = !rdoDefaultList.Checked;
            txtGlycanList.Enabled = !rdoDefaultList.Checked;
            btnBrowseGlycan.Enabled = !rdoDefaultList.Checked;            
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            PreparingMultiGlycan(null);
        }
        private void PreparingMultiGlycan(Dictionary<COL.GlycoLib.enumLabelingTag,float> argLabeling)
        {
            DoLog = chkLog.Checked;
            //saveFileDialog1.Filter = "Excel Files (*.xslx)|*.xslx";
            //saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";

            DateTime time = DateTime.Now;             // Use current time
            string TimeFormat = "yyMMdd HHmm";            // Use this format
            if (DoLog)
            {
                Logger.WriteLog(System.Environment.NewLine + System.Environment.NewLine + "-----------------------------------------------------------");
                Logger.WriteLog("Start Process");
            }

            saveFileDialog1.FileName = Path.GetDirectoryName(txtRawFile.Text) + "\\" + Path.GetFileNameWithoutExtension(txtRawFile.Text) + "-" + time.ToString(TimeFormat);


            if (txtRawFile.Text == "" || (rdoUserList.Checked && txtGlycanList.Text == "") || txtMaxLCTime.Text == "")
            {
                MessageBox.Show("Please check input values.");
                if (DoLog)
                {
                    Logger.WriteLog("End Process- because input value not complete");
                }
                return;
            }

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

          

                string glycanlist = System.Windows.Forms.Application.StartupPath + "\\Default_Combination.csv";
                if (!rdoDefaultList.Checked)
                {
                    glycanlist = txtGlycanList.Text;
                }

                if (DoLog)
                {

                    Logger.WriteLog("Start initial program");

                }
                List<float> AdductMasses = new List<float>();
                Dictionary<float, string> AdductLabel = new Dictionary<float, string>();
                if (chkAdductK.Checked)
                {
                    AdductMasses.Add(MassLib.Atoms.Potassium);
                    AdductLabel.Add(MassLib.Atoms.Potassium, "K");
                }
                if (chkAdductNH4.Checked)
                {
                    AdductMasses.Add(MassLib.Atoms.NitrogenMass + 4 * MassLib.Atoms.HydrogenMass);
                    AdductLabel.Add(MassLib.Atoms.NitrogenMass + 4 * MassLib.Atoms.HydrogenMass, "NH4");
                }
                if (chkAdductNa.Checked)
                {
                    AdductMasses.Add(MassLib.Atoms.SodiumMass);
                    AdductLabel.Add(MassLib.Atoms.SodiumMass, "Na");
                }
                if (chkAdductProton.Checked)
                {
                    AdductMasses.Add(MassLib.Atoms.ProtonMass);
                    AdductLabel.Add(MassLib.Atoms.ProtonMass, "H");
                }
                float outMass = 0.0f;
                if (chkAdductUser.Checked && float.TryParse(txtAdductMass.Text, out outMass))
                {
                    AdductMasses.Add(outMass);
                    AdductLabel.Add(outMass, "User");
                }

                // MultiNGlycanESIMultiThreads MultiESIs = new MultiNGlycanESIMultiThreads(glycanlist, txtRawFile.Text, Convert.ToInt32(cboCPU.SelectedItem), _peakParameter, _transformParameters);
                MultiGlycanESI ESI = null;
                if (argLabeling.Count != 0)
                {
                    ESI = new MultiGlycanESI(txtRawFile.Text, Convert.ToInt32(txtStartScan.Text), Convert.ToInt32(txtEndScan.Text), glycanlist, Convert.ToDouble(txtPPM.Text), Convert.ToDouble(txtGlycanPPM.Text), Convert.ToDouble(txtMaxLCTime.Text), chkPermethylated.Checked, chkReducedReducingEnd.Checked,cboSia.SelectedIndex, argLabeling, AdductLabel,AdductMasses, DoLog);
                }
                else
                {
                    ESI = new MultiGlycanESI(txtRawFile.Text, Convert.ToInt32(txtStartScan.Text), Convert.ToInt32(txtEndScan.Text), glycanlist, Convert.ToDouble(txtPPM.Text), Convert.ToDouble(txtGlycanPPM.Text), Convert.ToDouble(txtMaxLCTime.Text), chkPermethylated.Checked, chkReducedReducingEnd.Checked, cboSia.SelectedIndex, DoLog);
                }
                ESI.LabelingMethod = GlycoLib.enumGlycanLabelingMethod.None;
                if (rdoDRAG.Checked)
                {
                    ESI.LabelingMethod = GlycoLib.enumGlycanLabelingMethod.DRAG;
                }
                else if (rdoMultiplePemrthylated.Checked)
                {
                    ESI.LabelingMethod = GlycoLib.enumGlycanLabelingMethod.MultiplexPermethylated;
                }

                ESI.MergeDifferentChargeIntoOne = chkMergeDffCharge.Checked;  
                ESI.ExportFilePath = saveFileDialog1.FileName;
                ESI.MaxLCBackMin = Convert.ToSingle(txtMaxLCTime.Text);
                ESI.MaxLCFrontMin = Convert.ToSingle(txtMinLCTime.Text);
                ESI.IsotopePPM = Convert.ToSingle(txtIsotopeEnvTolerence.Text);
                ESI.MininumPeakCount = Convert.ToInt32(txtIsotopeEnvMinPeakCount.Text);
                ESI.PeakSN = Convert.ToSingle(txtSN.Text);
                ESI.IsMatchMonoisotopicOnly = chkMonoOnly.Checked;
                if (chkAbundance.Checked)
                {
                    ESI.MinAbundance = Convert.ToDouble(txtAbundanceMin.Text);
                }
                else
                {
                    ESI.MinAbundance = 0;
                }
                if (chkMinLengthOfLC.Checked)
                {
                    ESI.MinLengthOfLC = Convert.ToSingle(txtScanCount.Text);
                }
                else
                {
                    ESI.MinLengthOfLC = 0;
                }
                if (chkMZMatch.Checked)
                {
                    ESI.IncludeMZMatch = true;
                }                
               
               // ESI.AdductMass = AdductMasses;
                //ESI.AdductMassToLabel = AdductLabel;
                ESI.IndividualImgs = chkIndividualImg.Checked;
                ESI.QuantificationImgs = chkQuantImgs.Checked;
                if (DoLog)
                {
                    Logger.WriteLog("Initial program complete");
                }

                frmProcessing frmProcess = new frmProcessing(ESI, DoLog);
                frmProcess.ShowDialog();

                if (DoLog)
                {
                    Logger.WriteLog("Finish process");
                }
            }          
        }
        private void btnBrowseGlycan_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "CSV Files (.csv)|*.csv";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {                
                txtGlycanList.Text = openFileDialog1.FileName;
            }
        }

        private void rdoAllRaw_CheckedChanged(object sender, EventArgs e)
        {
            rdoScanNum.Checked = !rdoAllRaw.Checked;
            txtStartScan.Enabled = !rdoAllRaw.Checked;         
            txtEndScan.Enabled = !rdoAllRaw.Checked;
            txtStartScan.Text = "1";
            txtEndScan.Text = _endScan.ToString();
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            frmPeakpara = new frmPeakParameters();
            frmPeakpara.ShowDialog();
            btnNext.Enabled = true;
            btnQuan.Enabled = true;
        }


        private void btnMergeTest_Click(object sender, EventArgs e)
        {
            //StreamReader sr = new StreamReader(@"D:\Dropbox\for_Yunli_Hu\b1_19_1_07142012-121002 1349_FullList.csv");
            //string tmp = sr.ReadLine();
            //List<ClusteredPeak> clu = new List<ClusteredPeak>();
            //do
            //{
            //    tmp = sr.ReadLine();
            //    string[] tmpArray = tmp.Split(',');
            //    ClusteredPeak tnpCluPeak = new ClusteredPeak(Convert.ToInt32(tmpArray[1]));
            //    tnpCluPeak.StartTime = Convert.ToDouble(tmpArray[0]);
            //    tnpCluPeak.EndTime = Convert.ToDouble(tmpArray[0]);
                
            //    tnpCluPeak.EndScan = Convert.ToInt32(tmpArray[1]);
            //    tnpCluPeak.Intensity = Convert.ToSingle(tmpArray[2]);
            //    tnpCluPeak.GlycanComposition = new COL.GlycoLib.GlycanCompound(
            //                                                                    Convert.ToInt32(tmpArray[8]),
            //                                                                    Convert.ToInt32(tmpArray[9]),
            //                                                                    Convert.ToInt32(tmpArray[10]),
            //                                                                    Convert.ToInt32(tmpArray[11]));
            //    tnpCluPeak.Charge = Convert.ToInt32(Math.Ceiling(  Convert.ToSingle(tmpArray[12]) / Convert.ToSingle(tmpArray[3])));

            //    clu.Add(tnpCluPeak);
            //} while (!sr.EndOfStream);
            //sr.Close();
            ////MultiNGlycanESI.MergeCluster(clu, 8.0);
        }

        private void chkAdductUser_CheckedChanged(object sender, EventArgs e)
        {
            txtAdductMass.Enabled = chkAdductUser.Checked;
        }

        private void eluctionProfileViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            frmView frView = new frmView();
            frView.ShowDialog();
            this.Visible = true;
        }

 

        private void chkAbundance_CheckedChanged(object sender, EventArgs e)
        {
            txtAbundanceMin.Enabled = chkAbundance.Checked;
        }

        private void chkScanCount_CheckedChanged(object sender, EventArgs e)
        {
            txtScanCount.Enabled = chkMinLengthOfLC.Checked;
        }

        private void massCalculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmCalculator frmCalc = new frmCalculator();
            frmCalc.Show();
        }


        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                StreamReader sr = new StreamReader( openFileDialog1.FileName);
                sr.ReadLine();//Title

                Dictionary<string, List<PeaksFromResult>> dictAllPeaks = new Dictionary<string,List<PeaksFromResult>>();
                do
                {
                    string[] tmpAry = sr.ReadLine().Split(',');
                    PeaksFromResult PKResult = new PeaksFromResult(Convert.ToSingle(tmpAry[0]),
                                                                                                               Convert.ToSingle(tmpAry[3]),
                                                                                                               tmpAry[6]);
                    string GlycanKey = tmpAry[5];

                    if (!dictAllPeaks.ContainsKey(GlycanKey))
                    {
                        dictAllPeaks.Add(GlycanKey, new List<PeaksFromResult>());
                    }
                    dictAllPeaks[GlycanKey].Add(PKResult);
                    
                } while (!sr.EndOfStream);

                ZedGraph.ZedGraphControl zgcGlycan = new ZedGraphControl();
                zgcGlycan.Width = 2400;
                zgcGlycan.Height = 1200;

                foreach (string GKey in dictAllPeaks.Keys)
                {
                    GraphPane GP = zgcGlycan.GraphPane;
                    GP.Title.Text = "Glycan: " + GKey;
                    GP.XAxis.Title.Text = "Scan time (min)";
                    GP.YAxis.Title.Text = "Abundance";
                    GP.CurveList.Clear();


                    Dictionary<string, PointPairList> dictAdductPoints = new Dictionary<string, PointPairList>();

                    Dictionary<float, float> MergeIntensity = new Dictionary<float, float>();
                    List<float> Time = new List<float>();
                    foreach (PeaksFromResult PKR in dictAllPeaks[GKey])
                    {
                        if (!dictAdductPoints.ContainsKey(PKR.Adduct))
                        {
                            dictAdductPoints.Add(PKR.Adduct, new PointPairList());
                        }
                        dictAdductPoints[PKR.Adduct].Add(PKR.Time, PKR.Intensity);

                        if (!MergeIntensity.ContainsKey(PKR.Time))
                        {
                            MergeIntensity.Add(PKR.Time, 0);
                        }
                        MergeIntensity[PKR.Time] = MergeIntensity[PKR.Time] + PKR.Intensity;

                        if (!Time.Contains(PKR.Time))
                        {
                            Time.Add(PKR.Time);
                        }
                    }
                    List<Color> LstColor = new List<Color>();
                    LstColor.Add(Color.DarkCyan);
                    LstColor.Add(Color.DarkGoldenrod);
                    LstColor.Add(Color.DarkGray);
                    LstColor.Add(Color.DarkGreen);
                    LstColor.Add(Color.DarkKhaki);
                    LstColor.Add(Color.DarkMagenta);
                    LstColor.Add(Color.DarkOliveGreen);
                    LstColor.Add(Color.DarkOrchid);
                    LstColor.Add(Color.DarkRed);
                    LstColor.Add(Color.DarkSalmon);
                    LstColor.Add(Color.DarkSeaGreen);
                    LstColor.Add(Color.DarkSlateBlue);
                    LstColor.Add(Color.DarkSlateGray);
                    LstColor.Add(Color.DarkTurquoise);
                    LstColor.Add(Color.DarkViolet);
                    LstColor.Add(Color.DeepPink);
                    LstColor.Add(Color.DeepSkyBlue);
                    int ColorIdx = 0;
                    foreach (string Adduct in dictAdductPoints.Keys)
                    {
                        LineItem Lne = GP.AddCurve(Adduct, dictAdductPoints[Adduct], LstColor[ColorIdx]);
                        Lne.Symbol.Size = 2.0f;
                        ColorIdx = ColorIdx + 1;
                    }

                    //Merge Intensity
                    Time.Sort();
                    PointPairList PPLMerge = new PointPairList();
                    foreach (float tim in Time)
                    {
                        PPLMerge.Add(tim, MergeIntensity[tim]);
                    }
                    LineItem Merge = GP.AddCurve("Merge", PPLMerge, Color.Black);
                    Merge.Symbol.Size = 2.0f;
                    Merge.Line.Width = 3.0f;




                    zgcGlycan.AxisChange();
                    zgcGlycan.Refresh();
                    string StorePath = Path.GetDirectoryName(openFileDialog1.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                    if (!Directory.Exists(StorePath))
                    {
                        Directory.CreateDirectory(StorePath);
                    }

                    zgcGlycan.MasterPane.GetImage().Save(StorePath+"\\"+GKey+".bmp");
                }
            }
        }
        private class PeaksFromResult
        {
            float _Time;
            float _Intensity;
            string _Adduct;

            public float Time
            {
                get { return _Time; }
            }
            public float Intensity
            {
                get { return _Intensity; }
            }
            public string Adduct
            {
                get { return _Adduct; }
            }
            public PeaksFromResult(float argTime, float argIntensity, string argAdduct)
            {
                _Time = argTime;
                _Intensity = argIntensity;
                _Adduct = argAdduct;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                sr.ReadLine();//Title

                Dictionary<string, List<PeaksFromResult>> dictAllPeaks = new Dictionary<string, List<PeaksFromResult>>();
                do
                {
                    string[] tmpAry = sr.ReadLine().Split(',');
                    PeaksFromResult PKResult = new PeaksFromResult(Convert.ToSingle(tmpAry[0]),
                                                                                                               Convert.ToSingle(tmpAry[3]),
                                                                                                               tmpAry[6]);
                    string GlycanKey = tmpAry[5];

                    if (!dictAllPeaks.ContainsKey(GlycanKey))
                    {
                        dictAllPeaks.Add(GlycanKey, new List<PeaksFromResult>());
                    }
                    dictAllPeaks[GlycanKey].Add(PKResult);

                } while (!sr.EndOfStream);


                foreach (string GKey in dictAllPeaks.Keys)
                {

                    Dictionary<float, float> MergeIntensity = new Dictionary<float, float>();
                    List<float> Time = new List<float>();
                    foreach (PeaksFromResult PKR in dictAllPeaks[GKey])
                    {

                        if (!MergeIntensity.ContainsKey(PKR.Time))
                        {
                            MergeIntensity.Add(PKR.Time, 0);
                        }
                        MergeIntensity[PKR.Time] = MergeIntensity[PKR.Time] + PKR.Intensity;

                        if (!Time.Contains(PKR.Time))
                        {
                            Time.Add(PKR.Time);
                        }
                    }

                    //Merge Intensity
                    Time.Sort();
                    float[] ArryIntesity = new float[Time.Count];
                    float[] ArryTime = Time.ToArray();
                    for (int i = 0; i < Time.Count; i++)
                    {
                        ArryIntesity[i] = MergeIntensity[Time[i]];
                    }

                    List<float[]> PeaksTime = new List<float[]>();
                    List<float[]> PeaksIntensity = new List<float[]>();
                    do
                    {
                        //Iter to find peak
                        int MaxIdx = FindMaxIdx(ArryIntesity);
                        int PeakStart = MaxIdx - 1;
                        if (PeakStart < 0)
                        {
                            PeakStart = 0;
                        }
                        int PeakEnd = MaxIdx + 1;
                        if (PeakEnd > ArryTime .Length- 1)
                        {
                            PeakEnd = ArryTime.Length - 1;
                        }
                        //PeakStartPoint
                        while (PeakStart>0) 
                        {
                            if (ArryTime[PeakStart] - ArryTime[PeakStart - 1] < 0.5 && ArryTime[MaxIdx] - ArryTime[PeakStart] < 5.0)
                            {
                                PeakStart = PeakStart - 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        //PeakEndPoint
                        while (PeakEnd < ArryTime.Length - 1)
                        {
                            if (ArryTime[PeakEnd + 1] - ArryTime[PeakEnd] < 0.5 && ArryTime[PeakEnd] - ArryTime[MaxIdx] < 5.0)
                            {
                                PeakEnd = PeakEnd + 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        //Peak Array
                        float[] PeakTime = new float[PeakEnd - PeakStart + 1];
                        float[] PeakInt = new float[PeakEnd - PeakStart + 1];
                        Array.Copy(ArryTime, PeakStart, PeakTime, 0,PeakEnd - PeakStart + 1);
                        Array.Copy(ArryIntesity, PeakStart, PeakInt, 0, PeakEnd - PeakStart + 1);
                        PeaksTime.Add(PeakTime);
                        PeaksIntensity.Add(PeakInt);


                        //MergeRest
                        int SizeOfRestArray = ArryTime.Length - PeakEnd + PeakStart - 1;
                        float[] NewArryTime = new float[SizeOfRestArray];
                        float[] NewArryIntensity = new float[SizeOfRestArray];
                        Array.Copy(ArryTime, 0, NewArryTime, 0, PeakStart);
                        Array.Copy(ArryTime, PeakEnd+1, NewArryTime, PeakStart, ArryTime.Length-1-  PeakEnd );
                        Array.Copy(ArryIntesity, 0, NewArryIntensity, 0, PeakStart);
                        Array.Copy(ArryIntesity, PeakEnd+1, NewArryIntensity, PeakStart, ArryTime.Length - 1 - PeakEnd);

                        ArryTime = NewArryTime;
                        ArryIntesity = NewArryIntensity;

                    } while (ArryTime.Length!=0);
                }
            }
        }
        
        private int FindMaxIdx(float[] argArry)
        {
            int MaxIdx = 0;
            for (int i = 1; i < argArry.Length; i++)
            {
                if (argArry[i] > argArry[MaxIdx])
                {
                    MaxIdx = i;
                }
            }
            return MaxIdx;
        }

        private void chkQuanCH3_CheckedChanged(object sender, EventArgs e)
        {
            txtQuanCH3.Enabled = chkQuanCH3.Checked;
        }

        private void chkQuanCH2D_CheckedChanged(object sender, EventArgs e)
        {
            txtQuanCH2D.Enabled = chkQuanCH2D.Checked;
        }

        private void chkQuanCHD2_CheckedChanged(object sender, EventArgs e)
        {
            txtQuanCHD2.Enabled = chkQuanCHD2.Checked;
        }

        private void chkQuanCD3_CheckedChanged(object sender, EventArgs e)
        {
            txtQuanCD3.Enabled = chkQuanCD3.Checked;
        }

        private void chkQuan13CH3_CheckedChanged(object sender, EventArgs e)
        {
            txtQuan13CH3.Enabled = chkQuan13CH3.Checked;
        }

        private void chkQuan13CH2D_CheckedChanged(object sender, EventArgs e)
        {
            txtQuan13CD3.Enabled = chkQuan13CD3.Checked;
        }

        private void chkQuan13CHD2_CheckedChanged(object sender, EventArgs e)
        {
            txtQuan13CHD2.Enabled = chkQuan13CHD2.Checked;
        }

        private void btnQuan_Click(object sender, EventArgs e)
        {
            Dictionary<COL.GlycoLib.enumLabelingTag, float> labelingRatio = new Dictionary<GlycoLib.enumLabelingTag, float>();

            if (rdoDRAG.Checked)
            {
                if (chkPermethylated.Checked)
                {
                    MessageBox.Show("Permethylated check box should not be checked");
                    chkPermethylated.Checked = false;
                }
                if (chkReducedReducingEnd.Checked)
                {
                    MessageBox.Show("Reduced reducing end check box should not be checked");
                    chkReducedReducingEnd.Checked = false;
                }
                if (chkDRAG_Light.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.DRAG_Light, Convert.ToSingle(txtDRAGLight.Text));
                }
                if (chkDRAG_Heavy.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.DRAG_Heavy, Convert.ToSingle(txtDRAGHeavy.Text));
                }
            }
            else // MP
            {
                if (!chkPermethylated.Checked)
                {
                    MessageBox.Show("Permethylated check box should be checked");
                    chkPermethylated.Checked = true;
                }                
                if (chkQuan13CD3.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_13CD3, Convert.ToSingle(txtQuan13CD3.Text));
                }
                if (chkQuan13CH3.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_13CH3, Convert.ToSingle(txtQuan13CH3.Text));
                }
                if (chkQuan13CHD2.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_13CHD2, Convert.ToSingle(txtQuan13CHD2.Text));
                }
                if (chkQuanCD3.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_CD3, Convert.ToSingle(txtQuanCD3.Text));
                }
                if (chkQuanCH2D.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_CH2D, Convert.ToSingle(txtQuanCH2D.Text));
                }
                if (chkQuanCH3.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_CH3, Convert.ToSingle(txtQuanCH3.Text));
                }
                if (chkQuanCHD2.Checked)
                {
                    labelingRatio.Add(GlycoLib.enumLabelingTag.MP_CHD2, Convert.ToSingle(txtQuanCHD2.Text));
                }
            }
     

            PreparingMultiGlycan(labelingRatio);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void rdoNeutral_CheckedChanged(object sender, EventArgs e)
        {
            grpDRAG.Enabled = false;
            grpMultiplexPreM.Enabled = false;
            chkQuantImgs.Checked = false;
            chkQuantImgs.Enabled = false;
        }

        private void rdoDRAG_CheckedChanged(object sender, EventArgs e)
        {
            grpDRAG.Enabled = true;
            grpMultiplexPreM.Enabled = false;
            chkReducedReducingEnd.Checked = false;
            chkPermethylated.Checked = false;
            chkQuantImgs.Checked = true;
            chkQuantImgs.Enabled = true;
        }

        private void rdoMultiplePemrthylated_CheckedChanged(object sender, EventArgs e)
        {
            grpDRAG.Enabled = false;
            grpMultiplexPreM.Enabled = true;
            chkQuantImgs.Checked = true;
            chkQuantImgs.Enabled = true;
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rdoLabelFree.Checked)
            {
                chkQuantImgs.Checked = false;
                chkQuantImgs.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MassLib.ThermoRawReader Raw = new ThermoRawReader(@"E:\Dropbox\Chuan-Yih\PerMethQuant\RiboB_7plex_05302014.raw");
            List<MSScan> scans = Raw.ReadScans(766, 1066);
            List<MSPeak> Peaks = new List<MSPeak>();
            foreach (MSScan s in scans)
            {
                foreach (MSPeak p in s.MSPeaks)
                {
                    if (p.ChargeState == 2)
                    {
                        Peaks.Add(p);
                        ;
                    }
                    //if (p.MonoisotopicMZ >= 834.62 && p.MonoisotopicMZ <= 834.65)
                    //{
                    //    Peaks.Add(p);
                    //}
                }
            }

        }





      
    }
}
