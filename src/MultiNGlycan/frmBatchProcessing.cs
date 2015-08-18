﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using COL.GlycoLib;
using COL.MassLib;
using System.Threading.Tasks;
using System.Linq;
using ZedGraph;

namespace COL.MultiGlycan
{
    public partial class frmBatchProcessing : Form
    {
        private int NumberOfParallel = 1;
        private string ErrMsg = "";
        private MultiGlycanESI _MultiNGlycan;
        private List<string> _RawFilesList;
        private int CurrentScan = 0;
        DateTime Start;
        bool DoLog = false;
        private int ProcessedFileCount = 0;
        private int SucceedFileCount = 0;
        private Dictionary<string, int> ProgressUpdate; //Recording how many finish scan;
        private Dictionary<string, int> TotalScanNum;
        private string[] ProcessingFile;  //Save filename for each processing slot
        private string[] ProcessingStatus;  //Save status for each processing slot
       
        public frmBatchProcessing(MultiGlycanESI argMultiNGlycan, List<string> argRawFiles, int argConCurrent, bool argLog)
        {
            NumberOfParallel = argConCurrent;
            InitializeComponent();
            DoLog = argLog;
            
            _MultiNGlycan = argMultiNGlycan;
            _MultiNGlycan.MaxGlycanCharge = 5;
            _RawFilesList = argRawFiles;
            //ProcessMultiGlycan();
            ProgressUpdate = new Dictionary<string, int>();
            TotalScanNum = new Dictionary<string, int>();
            _MultiNGlycan.ReadGlycanList();
            _MultiNGlycan.CandidateMzList = _MultiNGlycan.GenerateCandidateGlycanMZList(_MultiNGlycan.GlycanList);
            ProcessingFile = new string[] { "0", "0", "0" };
            ProcessingStatus = new string[] { "ProcessingStatus", "ProcessingStatus", "ProcessingStatus" };

            progressBar1.Maximum = 100;
            progressBar2.Maximum = 100;
            progressBar3.Maximum = 100;
            lblCurrentFile.Text = "0 / " + _RawFilesList.Count.ToString();
            bgWorker_Process.RunWorkerAsync();
        }
        private void ProcessMultiGlycanAsync(string argFile)
        {
            int runningSlot = -1;
            for (int i = 0; i < 3; i++)
            {
                if (ProcessingFile[i] == "0")
                {
                    ProcessingFile[i] = argFile;
                    runningSlot = i;
                    break;
                }
            }
            MultiGlycanESI multiGlycan = null;
            ZedGraphControl zedGraphControl = new ZedGraphControl();
            try
            {
                ProcessingStatus[runningSlot] = "Status: Reading Raw file";
                bgWorker_Process.ReportProgress(0);
                ProgressUpdate.Add(argFile, 0);
                TotalScanNum.Add(argFile, 99999);
                if (_MultiNGlycan.LabelingRatio.Count != 0)
                {
                    multiGlycan = new MultiGlycanESI(argFile, 1, 99999, _MultiNGlycan.GlycanFile, _MultiNGlycan.MassPPM, _MultiNGlycan.GlycanPPM, _MultiNGlycan.IsPermethylated, _MultiNGlycan.IsReducedReducingEnd, _MultiNGlycan.SiaType, _MultiNGlycan.LabelingRatio, _MultiNGlycan.AdductMassToLabel, _MultiNGlycan.AdductMass, _MultiNGlycan.DoLOG);
                }
                else
                {
                    multiGlycan = new MultiGlycanESI(argFile, 1, 99999, _MultiNGlycan.GlycanFile, _MultiNGlycan.MassPPM, _MultiNGlycan.GlycanPPM, _MultiNGlycan.IsPermethylated, _MultiNGlycan.IsReducedReducingEnd, _MultiNGlycan.SiaType, _MultiNGlycan.DoLOG);
                }
                multiGlycan.LabelingMethod = _MultiNGlycan.LabelingMethod;
                multiGlycan.MergeDifferentChargeIntoOne = _MultiNGlycan.MergeDifferentChargeIntoOne;
                multiGlycan.ExportFilePath = _MultiNGlycan.ExportFilePath + "\\" + Path.GetFileNameWithoutExtension(argFile);
                multiGlycan.MaxLCBackMin = _MultiNGlycan.MaxLCBackMin;
                multiGlycan.MaxLCFrontMin = _MultiNGlycan.MaxLCFrontMin;
                multiGlycan.IsotopePPM = _MultiNGlycan.IsotopePPM;
                multiGlycan.MininumPeakCount = _MultiNGlycan.MininumPeakCount;
                multiGlycan.PeakSN = _MultiNGlycan.PeakSN;
                multiGlycan.IsMatchMonoisotopicOnly = _MultiNGlycan.IsMatchMonoisotopicOnly;
                multiGlycan.MinAbundance = _MultiNGlycan.MinAbundance;
                multiGlycan.MinLengthOfLC = _MultiNGlycan.MinLengthOfLC;
                multiGlycan.IncludeMZMatch = _MultiNGlycan.IncludeMZMatch;
                multiGlycan.IndividualImgs = _MultiNGlycan.IndividualImgs;
                multiGlycan.QuantificationImgs = _MultiNGlycan.QuantificationImgs;
                multiGlycan.GlycanList = _MultiNGlycan.GlycanList;
                multiGlycan.CandidateMzList = _MultiNGlycan.CandidateMzList;
                if (_MultiNGlycan.ApplyLinearRegLC)
                {
                    multiGlycan.ApplyLinearRegLC = _MultiNGlycan.ApplyLinearRegLC;
                    multiGlycan.TotalLCTime = _MultiNGlycan.TotalLCTime;
                    multiGlycan.LCTimeTolerance = _MultiNGlycan.LCTimeTolerance;
                }
                TotalScanNum[argFile] = multiGlycan.RawReader.NumberOfScans;


                //ProcessingStatus[runningSlot] = "Status:  Processing";
                //Parallel.ForEach(Enumerable.Range(1, multiGlycan.RawReader.NumberOfScans),
                //    new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (i) =>
                //    {
                //        //Console.WriteLine(argFile + "-" + i.ToString());
                //        ProgressUpdate[argFile] = ProgressUpdate[argFile] + 1;
                //        bgWorker_Process.ReportProgress(Convert.ToInt32(ProgressUpdate[argFile] / TotalScanNum[argFile]));
                //        if (multiGlycan.RawReader.GetMsLevel(i) == 1)
                //        {
                //            multiGlycan.ProcessSingleScan(i);
                //        }
                //    });
                for (int i = 1; i <= multiGlycan.RawReader.NumberOfScans; i++)
                {
                    ProgressUpdate[argFile] = ProgressUpdate[argFile] + 1;
                    bgWorker_Process.ReportProgress(Convert.ToInt32(ProgressUpdate[argFile] / TotalScanNum[argFile]));
                    if (multiGlycan.RawReader.GetMsLevel(i) == 1)
                    {
                        multiGlycan.ProcessSingleScan(i);
                    }
                }
                ProcessingStatus[runningSlot] = "Status:  Mergeing Result";
                multiGlycan.MergeSingleScanResultToPeak();
                multiGlycan.SolveDuplicateAssignment();
                multiGlycan.MergeSingleScanResultToPeak();
                if (multiGlycan.GlycanLCorderExist)
                {
                    multiGlycan.ApplyLCordrer();
                }
                if (!Directory.Exists(multiGlycan.ExportFilePath + "\\Pic"))
                {
                    Directory.CreateDirectory(multiGlycan.ExportFilePath + "\\Pic");
                }
                if (multiGlycan.LabelingMethod == GlycoLib.enumGlycanLabelingMethod.MultiplexPermethylated)
                {
                    multiGlycan.EstimatePurity();
                    ZedGraphControl zedGraph = new ZedGraphControl();
                    foreach (GlycoLib.enumLabelingTag tag in multiGlycan.LabelingRatio.Keys)
                    {
                        if (tag == enumLabelingTag.MP_CH3 || !multiGlycan.HasEstimatePurity((tag)))
                            continue;
                        multiGlycan.GetPurityEstimateImage(ref zedGraph, tag).Save(multiGlycan.ExportFilePath + "\\Pic\\EstimatePurity_" + tag.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    zedGraph.Dispose();
                    zedGraph = null;
                }
                ProcessingStatus[runningSlot] = "Status:  Waiting for Export";
                bgWorker_Process.ReportProgress(0);

              
                ProcessingStatus[runningSlot] = "Status:  Exporting";
                bgWorker_Process.ReportProgress(0);
                multiGlycan.ExportToCSV();
                ProcessingStatus[runningSlot] = "Export completed";
                bgWorker_Process.ReportProgress(0);
                SucceedFileCount = SucceedFileCount + 1;
            }
            catch (Exception e)
            {
                ErrMsg = ErrMsg + (argFile + "  error.  ErrMSG:" + e.ToString() + "\n");
            }
            finally
            {
                ProcessingStatus[runningSlot] = "Status:  Ready";
                ProcessingFile[runningSlot] = "0";
                ProcessedFileCount = ProcessedFileCount + 1;
            }

        }

        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e)
        {
            Parallel.ForEach(_RawFilesList, new ParallelOptions() { MaxDegreeOfParallelism = NumberOfParallel }, (rawFile) =>
            {
                ProcessMultiGlycanAsync(rawFile);
            });
        }

        private void bgWorker_Process_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                lblCurrentFile.Text = ProcessedFileCount.ToString() + " /  " + _RawFilesList.Count.ToString();
                if (ProcessingFile[0] != "0")
                {
                    lblFileName1.Text = "FileName:" + Path.GetFileName(ProcessingFile[0]);
                    lblStatus1.Text = ProcessingStatus[0];
                    try
                    {
                        progressBar1.Value = Convert.ToInt32(ProgressUpdate[ProcessingFile[0]] / (float)TotalScanNum[ProcessingFile[0]] * 100);
                        lblPercentage1.Text = (ProgressUpdate[ProcessingFile[0]] / (float)TotalScanNum[ProcessingFile[0]] * 100).ToString("00") + "%";
                    }
                    catch (Exception)
                    {
                        progressBar1.Value = 0;
                        lblPercentage1.Text = "0%";
                    }
                }
                else
                {
                    lblFileName1.Text = "FileName:";
                    lblStatus1.Text = "Status: Ready";
                    progressBar1.Value = 0;
                    lblPercentage1.Text = "0%";
                }
                if (ProcessingFile[1] != "0")
                {
                    lblFileName2.Text = "FileName:" + Path.GetFileName(ProcessingFile[1]);
                    lblStatus2.Text = ProcessingStatus[1];
                    try
                    {
                        progressBar2.Value = Convert.ToInt32(ProgressUpdate[ProcessingFile[1]] / (float)TotalScanNum[ProcessingFile[1]] * 100);
                        lblPercentage2.Text = (ProgressUpdate[ProcessingFile[1]] / (float)TotalScanNum[ProcessingFile[1]] * 100).ToString("00") + "%";
                    }
                    catch (Exception)
                    {
                        progressBar2.Value = 0;
                        lblPercentage2.Text = "0%";
                    }
                }
                else
                {
                    lblFileName2.Text = "FileName:";
                    lblStatus2.Text = "Status: Ready";
                    progressBar2.Value = 0;
                    lblPercentage2.Text = "0%";
                }
                if (ProcessingFile[2] != "0")
                {
                    lblFileName3.Text = "FileName:" + Path.GetFileName(ProcessingFile[2]);
                    lblStatus3.Text = ProcessingStatus[2];
                    try
                    {
                        progressBar3.Value = Convert.ToInt32(ProgressUpdate[ProcessingFile[2]] / (float)TotalScanNum[ProcessingFile[2]] * 100);
                        lblPercentage3.Text = (ProgressUpdate[ProcessingFile[2]] / (float)TotalScanNum[ProcessingFile[2]] * 100).ToString("00") + "%";
                    }
                    catch (Exception)
                    {
                        progressBar3.Value = 0;
                        lblPercentage3.Text = "0%";
                    }
                }
                else
                {
                    lblFileName3.Text = "FileName:";
                    lblStatus3.Text = "Status: Ready";
                    progressBar3.Value = 0;
                    lblPercentage3.Text = "0%";
                }
            }
            catch (Exception)
            {
                ErrMsg = ErrMsg + ("ErrMSG:" + e.ToString() + "\n");
            }
        }

        private void bgWorker_Process_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblCurrentFile.Text = ProcessedFileCount.ToString() + " /  " + _RawFilesList.Count.ToString();
            lblFileName1.Text = "FileName:";
            lblStatus1.Text = "Status: Ready";
            progressBar1.Value = 0;
            lblPercentage1.Text = "0%";
            lblFileName2.Text = "FileName:";
            lblStatus2.Text = "Status: Ready";
            progressBar2.Value = 0;
            lblPercentage2.Text = "0%";
            lblFileName3.Text = "FileName:";
            lblStatus3.Text = "Status: Ready";
            progressBar3.Value = 0;
            lblPercentage3.Text = "0%";

            #region Export Merge result
            //Fetch Files
            List<string> ResultFiles = new List<string>();

            foreach (string filename in _RawFilesList)
            {
                if (!Directory.Exists(_MultiNGlycan.ExportFilePath + "\\" +
                                                         Path.GetFileNameWithoutExtension(filename)))
                {
                    continue;
                }
                foreach (
                    string files in
                        Directory.GetFiles(_MultiNGlycan.ExportFilePath + "\\" +
                                           Path.GetFileNameWithoutExtension(filename)))
                {
                    if (files.EndsWith(".csv") && !files.Contains("_FullLis") && !files.Contains("_Quant"))
                    {
                        ResultFiles.Add(files);
                    }
                }
            }
            //Read Files
            Dictionary<string, Dictionary<string, float>> AllResult = new Dictionary<string, Dictionary<string, float>>();
            for (int i = 0; i < ResultFiles.Count; i++)
            {
                Dictionary<string, Dictionary<string, float>> Result = ReadResultCSV(ResultFiles[i]);
                foreach (string GlycanKey in Result.Keys)
                {
                    if (!AllResult.ContainsKey(GlycanKey))
                    {
                        AllResult.Add(GlycanKey, new Dictionary<string, float>());
                    }
                    foreach (string DataSetName in Result[GlycanKey].Keys)
                    {
                        AllResult[GlycanKey].Add(DataSetName, Result[GlycanKey][DataSetName]);
                    }
                }
            }

            //Export File
            StreamWriter sw = new StreamWriter(_MultiNGlycan.ExportFilePath + "\\MergeResult.csv");
            string tmpStr = "";
            tmpStr = "Glycan,";
            foreach (string filename in _RawFilesList)
            {
                tmpStr = tmpStr + Path.GetFileNameWithoutExtension(filename) + ",";
            }
            sw.WriteLine(tmpStr);

            foreach (string GlycanKey in AllResult.Keys)
            {
                tmpStr = GlycanKey + ",";
                foreach (string filename in _RawFilesList)
                {
                    if (AllResult[GlycanKey].ContainsKey(Path.GetFileNameWithoutExtension(filename)))
                    {
                        tmpStr = tmpStr + AllResult[GlycanKey][Path.GetFileNameWithoutExtension(filename)] + ",";
                    }
                    else
                    {
                        tmpStr = tmpStr + "N/A,";
                    }
                }
                sw.WriteLine(tmpStr);
            }
            sw.Close();
            #endregion
            #region Get Images
            frmBatchGenImg frmBatchGenerateImages = new frmBatchGenImg(
                ResultFiles,
                _MultiNGlycan.ExportFilePath,
                _MultiNGlycan.LabelingMethod,
                _MultiNGlycan.IndividualImgs,
                _MultiNGlycan.QuantificationImgs);
            frmBatchGenerateImages.ShowDialog();
            #endregion
            MessageBox.Show("Finished files:" + SucceedFileCount.ToString() + Environment.NewLine + ErrMsg);
            this.Close();
        }

        private Dictionary<string, Dictionary<string, float>> ReadResultCSV(string argFile)
        {
            StreamReader sr = null;
            //Start Time	End Time	Start Scan Num	End Scan Num	Peak Intensity	LC Peak Area	HexNac-Hex-deHex-NeuAc-NeuGc	Composition mono
            //25.86727	,30.61827,	1366,	1791,	5807018.941,	59232.05865,	2-7-0-0-0	2011.208589
            try
            {
                string DataSetName = Path.GetFileNameWithoutExtension(argFile);

                Dictionary<string, Dictionary<string, float>> Result =
                    new Dictionary<string, Dictionary<string, float>>();
                //Key1 : Glycan Key, Key2: DataSet Name; Value = Peak Intensity

                sr = new StreamReader(argFile);
                string tmp = "";
                bool isInSection = false;
                int GlycanKeyIdx = 0;
                int PeakIntensityIdx = 0;
                int LabelIdx = 0;
                do
                {
                    tmp = sr.ReadLine();
                    if (tmp == null)
                    {
                        break;
                    }
                    if (tmp.StartsWith("Start Time"))
                    {
                        string[] tmpAry = tmp.Split(',');
                        for (int i = 0; i < tmpAry.Length; i++)
                        {
                            if (tmpAry[i] == "HexNac-Hex-deHex-NeuAc-NeuGc")
                            {
                                GlycanKeyIdx = i;
                            }
                            if (tmpAry[i] == "Peak Intensity")
                            {
                                PeakIntensityIdx = i;
                            }
                            if (tmpAry[i] == "Label Tag")
                            {
                                LabelIdx = i;
                            }
                        }
                        isInSection = true;
                        continue;
                    }
                    if (isInSection)
                    {
                        string[] tmpAry = tmp.Split(',');
                        string GlycanKey = tmpAry[GlycanKeyIdx];
                        if (LabelIdx != 0)
                        {
                            GlycanKey = GlycanKey + "-" + tmpAry[LabelIdx];
                        }
                        float PeakValue = Convert.ToSingle(tmpAry[PeakIntensityIdx]);
                        if (!Result.ContainsKey(GlycanKey))
                        {
                            Result.Add(GlycanKey, new Dictionary<string, float>());
                        }
                        if (!Result[GlycanKey].ContainsKey(DataSetName))
                        {
                            Result[GlycanKey].Add(DataSetName, 0);
                        }
                        Result[GlycanKey][DataSetName] = Result[GlycanKey][DataSetName] + PeakValue;
                    }

                } while (!sr.EndOfStream);

                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

        }

        //private void bgWorker_Process_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    try
        //    {
        //      //  _MultiNGlycan.ExportParametersToExcel();
        //        if (DoLog)
        //        {
        //            Logger.WriteLog("Start merge peaks");
        //        }
        //        lblStatus.Text = "Mergeing Peaks";


        //        _MultiNGlycan.MergeSingleScanResultToPeak();



        //        if (DoLog)
        //        {
        //            Logger.WriteLog("End merge peaks");
        //        }
        //        if (_MultiNGlycan.GlycanLCorderExist)
        //        {
        //            _MultiNGlycan.ApplyLCordrer();
        //        }

        //        if(!Directory.Exists(_MultiNGlycan.ExportFilePath + "\\Pic"))
        //        {
        //            Directory.CreateDirectory(_MultiNGlycan.ExportFilePath + "\\Pic");
        //        }

        //        if (_MultiNGlycan.LabelingMethod == GlycoLib.enumGlycanLabelingMethod.MultiplexPermethylated)
        //        {
        //            _MultiNGlycan.EstimatePurity();
        //            foreach (GlycoLib.enumLabelingTag tag in _MultiNGlycan.LabelingRatio.Keys)
        //            {
        //                if (tag == enumLabelingTag.MP_CH3 || !_MultiNGlycan.HasEstimatePurity((tag)))
        //                    continue;
        //                _MultiNGlycan.GetPurityEstimateImage(tag).Save(_MultiNGlycan.ExportFilePath + "\\Pic\\EstimatePurity_" + tag.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png); 
        //            }          

        //            //Correct Intensity;
        //            //_MultiNGlycan.CorrectIntensityByIsotope();
        //        }
        //        if (DoLog)
        //        {
        //            ;
        //            Logger.WriteLog("Start export");
        //        }
        //        lblStatus.Text = "Exporting";
        //        _MultiNGlycan.ExportToCSV();
        //        //_MultiNGlycan.ExportToExcel();
        //        if (DoLog)
        //        {
        //            Logger.WriteLog("End export");
        //        }
        //        TimeSpan TDiff = DateTime.Now.Subtract(Start);
        //        lblStatus.Text = "Finish in " + TDiff.TotalMinutes.ToString("0.00") + " mins";
        //        lblNumberOfMerge.Text = _MultiNGlycan.MergedPeak.Count.ToString();
        //        progressBar1.Value = 100;
        //        lblPercentage.Text = "100%";
        //        FlashWindow.Flash(this);
        //        this.Text = "Done";
        //        if (DoLog)
        //        {
        //            Logger.WriteLog("End process each scan");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private void frmProcessing_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    if (bgWorker_Process.IsBusy)
        //    {
        //        if (MessageBox.Show("Still processing, do you want to quit?", "Exit process?", MessageBoxButtons.YesNo) == DialogResult.Yes)
        //        {
        //            bgWorker_Process.CancelAsync();
        //            if (DoLog)
        //            {
        //                Logger.WriteLog("User terminate process");
        //            }
        //        }
        //        else
        //        {
        //            e.Cancel = true;
        //        }
        //    }
        //}    

    }
}
