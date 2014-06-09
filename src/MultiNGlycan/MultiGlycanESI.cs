using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using COL.GlycoLib;
using COL.MassLib;
using System.Drawing;
using System.Threading;
using ZedGraph;
using System.Linq;
namespace COL.MultiGlycan
{

    //Mass Spectrometry Adduct Calculator http://fiehnlab.ucdavis.edu/staff/kind/Metabolomics/MS-Adduct-Calculator/
    public class MultiGlycanESI
    {
        private ManualResetEvent _doneEvent;
        private string _rawFile;
        private string _glycanFile;
        //private List<ClusteredPeak> _cluPeaks;
        private List<MatchedGlycanPeak> _MatchedPeaksInScan;
        private List<MatchedGlycanPeak> _2ndPassedPeaksInScan;
        private List<ClusteredPeak> _MergedResultList;
        private List<ClusteredPeak> _MergedResultListAfterApplyLCOrder;
        private List<ClusteredPeak> _Merged2ndPassedResultList;
        private double _massPPM;
        private double _glycanPPM;
        private List<GlycanCompound> _GlycanList;
        private bool _isPermethylated;
        private bool _isReducedReducingEnd;
        private int _StartScan;
        private int _EndScan;
        private GlypID.Peaks.clsPeakProcessorParameters _peakParameter;
        private GlypID.HornTransform.clsHornTransformParameters _transformParameters;
        private bool _MergeDifferentCharge = true;
        private int _MaxCharge = 5;
        private bool _FindClusterUseList = true;
        private string _ExportFilePath;
        private List<float> _adductMass;
        private Dictionary<float, string> _adductLabel;
        private float _maxLCBackMin = 5.0f;
        private float _maxLCFrontMin = 5.0f;
        private double _minAbundance = 10 ^ 6;
        private float _minLengthOfLC = 10;
        private bool _GlycanListContainLCOrder = false;
        private bool _IncludeMZMatch = false;
        IRawFileReader rawReader;
        List<int> MSScanList;
        List<CandidatePeak> _lstCandidatePeak; //Store candidate glycan m/z
        List<float> _candidateMzList;
        Dictionary<float, List<CandidatePeak>> _dicCandidatePeak;
        List<GlycanCompound> _identifiedGlycan;
        List<float> GlycanMassList;
        bool DoLog = false;
        Dictionary<int, string> _GlycanLCodrer;
        List<Color> LstColor = new List<Color>(){Color.DarkCyan,Color.DarkGoldenrod,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen,Color.DarkOrchid,Color.DarkRed,Color.DarkSalmon,Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkTurquoise,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue};
        public MultiGlycanESI(string argRawFile, int argStartScan, int argEndScan, string argGlycanList, double argMassPPM, double argGlycanMass, double argMergeDurationMax, bool argPermenthylated, bool argReducedReducingEnd, bool argLog)
        {
            DoLog = argLog;
            _rawFile = argRawFile;
            //_cluPeaks = new List<ClusteredPeak>();
            _MatchedPeaksInScan = new List<MatchedGlycanPeak>();
            _massPPM = argMassPPM;
            _glycanFile = argGlycanList;
            _isPermethylated = argPermenthylated;
            _isReducedReducingEnd = argReducedReducingEnd;
            _glycanPPM = argGlycanMass;
            _StartScan = argStartScan;
            _EndScan = argEndScan;
            _adductMass = new List<float>();
            _identifiedGlycan = new List<GlycanCompound>();
            //Read Glycan list           
            if (DoLog)
            {
                Logger.WriteLog("Start Reading glycan list");
            }
            ReadGlycanList();
            if (DoLog)
            {
                Logger.WriteLog("Finish Reading glycan list");
            }
            if (Path.GetExtension(argRawFile) == ".raw")
            {
                rawReader = new RawReader(_rawFile, enumRawDataType.raw);
            }
            else
            {
                rawReader = new RawReader(_rawFile, enumRawDataType.mzxml);
            }
        }
        public bool GlycanLCorderExist
        {
            get { return _GlycanListContainLCOrder; }
        }
        public bool IncludeMZMatch
        {
            get { return _IncludeMZMatch; }
            set { _IncludeMZMatch = value; }
        }
        public float MinLengthOfLC
        {
            set { _minLengthOfLC = value; }
        }
        public float MaxLCFrontMin
        {
            set { _maxLCFrontMin = value; }
        }
        public float MaxLCBackMin
        {
            set { _maxLCBackMin = value; }
        }
        public double MinAbundance
        {
            set { _minAbundance = value; }
        }
        public IRawFileReader RawReader
        {
            get { return rawReader; }
        }
        //public List<ClusteredPeak> ClustedPeak
        //{
        //    get { return _cluPeaks; }
        //}
        public List<MatchedGlycanPeak> MatchedPeakInScan
        {
            get { return _MatchedPeaksInScan; }
        }
        public List<ClusteredPeak> MergedPeak
        {
            get { return _MergedResultList; }
        }
        public List<ClusteredPeak> Merged2ndPassedResult
        {
            get { return _Merged2ndPassedResultList; }
        }
        public string ExportFilePath
        {
            set { _ExportFilePath = value; }
        }
        public int StartScan
        {
            get { return _StartScan; }
        }
        public int EndScan
        {
            get { return _EndScan; }
        }
        public int MaxGlycanCharge
        {
            set { _MaxCharge = value; }
            get { return _MaxCharge; }
        }
        public bool MergeDifferentChargeIntoOne
        {
            set { _MergeDifferentCharge = value; }
            get { return _MergeDifferentCharge; }
        }
        public GlypID.Peaks.clsPeakProcessorParameters PeakProcessorParameters
        {   
            get { return _peakParameter; }
            set { _peakParameter = value;
                  rawReader.SetPeakProcessorParameter(_peakParameter);
            }
        }
        public GlypID.HornTransform.clsHornTransformParameters TransformParameters
        {
            get { return _transformParameters; }
            set { _transformParameters = value; 
                  rawReader.SetTransformParameter(_transformParameters);
            }
        }
        public List<float> AdductMass
        {
            get { return _adductMass; }
            set { _adductMass = value; }
        }
        public Dictionary<float, string> AdductMassToLabel
        {
            get { return _adductLabel; }
            set { _adductLabel = value; }
        }
        public Dictionary<int, string> GlycanLCodrer
        {
            get { return _GlycanLCodrer; }
            set { _GlycanLCodrer = value; }
        }
        public void ProcessSingleScan(int argScanNo)
        {
            //rawReader.SetPeakProcessorParameter(_peakParameter);
            //rawReader.SetTransformParameter(_transformParameters);
            if (DoLog)
            {
                Logger.WriteLog("Start process scan:" + argScanNo.ToString());
            }
            if (rawReader.GetMsLevel(argScanNo) == 1)
            {
                if (DoLog)
                {
                    Logger.WriteLog("\tStart read raw file: "+argScanNo.ToString());
                }
                //Get MS Scan
                MSScan GMSScan = rawReader.ReadScan(argScanNo);
                if (DoLog)
                {
                    Logger.WriteLog("\tEnd read raw file: " + argScanNo.ToString());
                }
                //Get Peaks
                List<MSPeak> deIsotopedPeaks = GMSScan.MSPeaks;
                
                //Convert to Float List
                List<float> mzList = new List<float>();
                foreach (MSPeak Peak in GMSScan.MSPeaks)
                {
                    mzList.Add(Peak.MonoisotopicMZ);
                }
                mzList.Sort();

                //Glycan Cluster in this scan
                List<MatchedGlycanPeak> Cluster;
                if (_FindClusterUseList)
                {
                    if (_candidateMzList == null || _lstCandidatePeak == null) // Generate Candidate Peak
                    {
                        if (DoLog)
                        {
                            Logger.WriteLog("Start generate candidate peak");
                        }
                        _candidateMzList = GenerateCandidatePeakList(_GlycanList);
                        if (DoLog)
                        {
                            Logger.WriteLog("End generate candidate peak");
                        }
                    }
                    if (DoLog)
                    {
                        Logger.WriteLog("\tStart find cluster use default list:" + argScanNo.ToString());
                    }                   
                    Cluster = FindClusterWGlycanList(deIsotopedPeaks, argScanNo, GMSScan.Time);
                    foreach (MatchedGlycanPeak MatchedPeak in Cluster)
                    {
                        if(!_identifiedGlycan.Contains( MatchedPeak.GlycanComposition))
                        {
                            _identifiedGlycan.Add(MatchedPeak.GlycanComposition);
                        }
                    }
                    _MatchedPeaksInScan.AddRange(Cluster);
                    //_cluPeaks.AddRange(Cluster);
                    if (DoLog)
                    {
                        Logger.WriteLog("\tEnd find cluster use default list:" + argScanNo.ToString());
                    }

                    if (_IncludeMZMatch)
                    {
                        if (_2ndPassedPeaksInScan == null)
                        {
                            _2ndPassedPeaksInScan = new List<MatchedGlycanPeak>();
                        }
                        if (DoLog)
                        {
                            Logger.WriteLog("Start process 2nd passed scan:" + argScanNo.ToString());
                        }

                        for (int i =0;i<GMSScan.MZs.Length;i++) 
                        {
                            float targetMZ = GMSScan.MZs[i];
                            int ClosedPeaksIdx = MassLib.MassUtility.GetClosestMassIdx(_candidateMzList, targetMZ);
                            float PPM = Math.Abs(Convert.ToSingle(((targetMZ - _candidateMzList[ClosedPeaksIdx]) / _candidateMzList[ClosedPeaksIdx]) * Math.Pow(10.0, 6.0)));
                            if (PPM > _massPPM)
                            {
                                continue;
                            }
                            List<CandidatePeak> ClosedGlycans = _dicCandidatePeak[_candidateMzList[ClosedPeaksIdx]];

                            int MaxIntIdx = i;
                            float MAXInt = 0;
                            int StartIdx = i - 10;
                            int EndIdx = i + 10;
                            if (StartIdx < 0)
                            {
                                StartIdx = 0;
                            }
                            if (EndIdx > GMSScan.Intensities.Length - 1)
                            {
                                EndIdx = GMSScan.Intensities.Length - 1;
                            }
                            for (int j = StartIdx; j <= EndIdx; j++)
                            {
                                if (GMSScan.Intensities[j] > MAXInt)
                                {
                                    MAXInt = GMSScan.Intensities[j];
                                    MaxIntIdx = j;
                                }
                            }
                            MSPeak Peak = new MSPeak(targetMZ, GMSScan.Intensities[MaxIntIdx]);
                            //############SaveMatchedGlycanCompound
                            
                            foreach (CandidatePeak CP in ClosedGlycans)
                            {
                                _2ndPassedPeaksInScan.Add(new MatchedGlycanPeak(GMSScan.ScanNo, GMSScan.Time, Peak, CP.GlycanComposition, CP.AdductLabel, CP.AdductNo));
                            }
                        }
                       }
                }
                else //Not use the list
                {
                    //FIX
                    //if (DoLog)
                    //{
                    //    Logger.WriteLog("\tStart find cluster without list:" + argScanNo.ToString());
                    //}
                    //Cluster = FindClusterWOGlycanList(deIsotopedPeaks, argScanNo, GMSScan.Time);
                    //List<MSPeak> UsedPeakList = new List<MSPeak>();

                    ////ConvertGlycanListMz into MSPoint
                    //List<MSPoint> MSPs = new List<MSPoint>();
                    //foreach (GlycanCompound comp in _GlycanList)
                    //{
                    //    MSPs.Add(new MSPoint(Convert.ToSingle(comp.MonoMass), 0.0f));
                    //}
                    ////Find Composition for each Cluster
                    //foreach (ClusteredPeak cls in Cluster)
                    //{
                    //    int Idx = MassLib.MassUtility.GetClosestMassIdx(MSPs, Convert.ToSingle(cls.ClusterMono));
                    //    if (GetMassPPM(_GlycanList[Idx].MonoMass, cls.ClusterMono) < _glycanPPM)
                    //    {
                    //        cls.GlycanComposition = _GlycanList[Idx];
                    //    }
                    //    UsedPeakList.AddRange(cls.Peaks);
                    //    _cluPeaks.Add(cls);
                    //}
                    ////Find Composition for single peak
                    //foreach (MSPeak peak in deIsotopedPeaks)
                    //{
                    //    if (!UsedPeakList.Contains(peak))
                    //    {
                    //        int Idx = MassLib.MassUtility.GetClosestMassIdx(MSPs, peak.MonoMass);
                    //        if (GetMassPPM(_GlycanList[Idx].MonoMass, peak.MonoMass) < _glycanPPM)
                    //        {
                    //            ClusteredPeak cls = new ClusteredPeak(argScanNo);
                    //            cls.StartTime = GMSScan.Time;
                    //            cls.EndTime = GMSScan.Time;
                    //            cls.Charge = peak.ChargeState;
                    //            cls.Peaks.Add(peak);
                    //            cls.GlycanComposition = _GlycanList[Idx];
                    //            _cluPeaks.Add(cls);
                    //            UsedPeakList.Add(peak);
                    //        }
                    //    }
                    //}
                    //if (DoLog)
                    //{
                    //    Logger.WriteLog("\tEnd find cluster without list:" + argScanNo.ToString());
                    //}
                }// Don't use glycan list;
                if (DoLog)
                {
                    Logger.WriteLog("\tEnd find cluster:" + argScanNo.ToString());
                }
                GMSScan = null;
            } //MS scan only
           
        }
        //Merged to ProcessSingleScan(int argScanNo)
        public void ProcessSingleScanTwoPassID(int argScanNo)
        {
            if (_2ndPassedPeaksInScan == null)
            {
                _2ndPassedPeaksInScan = new List<MatchedGlycanPeak>();
            }

            if (DoLog)
            {
                Logger.WriteLog("Start process 2nd passed scan:" + argScanNo.ToString());
            }
            if (rawReader.GetMsLevel(argScanNo) == 1)
            {
  
                if (DoLog)
                {
                    Logger.WriteLog("\tStart read raw file: " + argScanNo.ToString());
                }
                //Get MS Scan
                MSScan MSScan = rawReader.ReadScan(argScanNo);
                if (DoLog)
                {
                    Logger.WriteLog("\tEnd read raw file: " + argScanNo.ToString());
                }
                //Use identifed glycan
                _candidateMzList = GenerateCandidatePeakList(_identifiedGlycan);

                foreach (float targetMZ in _candidateMzList)
                {
                    int ClosedPeaksIdx = MassLib.MassUtility.GetClosestMassIdx(MSScan.MZs, targetMZ);
                    if (MassLib.MassUtility.GetMassPPM(MSScan.MZs[ClosedPeaksIdx], targetMZ) > _massPPM)
                    {
                        continue;
                    }

                    //#################Find Peak##############
                    //float[] Intensities = MSScan.Intensities;
                    ////Left bound
                    //int LBound = ClosedPeaksIdx -1;
                    //if (LBound < 0)
                    //    LBound = 0;
                    //do
                    //{
                    //    LBound = LBound - 1;
                    //    if (LBound < 0)
                    //    {
                    //        LBound = 0;
                    //        break;
                    //    }
                    //} while (LBound>0 && Intensities[LBound - 1] != 0.0f);

                    ////Right Bound
                    //int RBound = ClosedPeaksIdx + 1;
                    //if (RBound >=Intensities.Length)
                    //    RBound = Intensities.Length -1;
                    //do
                    //{
                    //    RBound = RBound + 1;
                    //    if (RBound+1 >= Intensities.Length)
                    //    {
                    //        RBound = Intensities.Length - 1;
                    //        break;
                    //    }
                    //} while (RBound < Intensities.Length &&Intensities[RBound + 1] != 0.0);
                    //FindMax Intensity
                    int MaxIntIdx = ClosedPeaksIdx;
                    float MAXInt = 0;
                    int StartIdx = ClosedPeaksIdx - 10;
                    int EndIdx = ClosedPeaksIdx + 10;
                    if (StartIdx < 0)
                    {
                        StartIdx = 0;
                    }
                    if (EndIdx > MSScan.Intensities.Length -1)
                    {
                        EndIdx = MSScan.Intensities.Length - 1;
                    }
                    for (int i = StartIdx; i <= EndIdx; i++)
                    {
                        if (MSScan.Intensities[i] > MAXInt)
                        {
                            MAXInt = MSScan.Intensities[i];
                            MaxIntIdx = i;
                        }
                    }
                    MSPeak Peak = new MSPeak(MSScan.MZs[MaxIntIdx],MSScan.Intensities[MaxIntIdx]);
                    //############SaveMatchedGlycanCompound
                    List<CandidatePeak> ClosedGlycans = _dicCandidatePeak[targetMZ];
                    foreach (CandidatePeak CP in ClosedGlycans)
                    {
                        _2ndPassedPeaksInScan.Add(new MatchedGlycanPeak(MSScan.ScanNo, MSScan.Time, Peak, CP.GlycanComposition, CP.AdductLabel, CP.AdductNo));
                    }
                }
            }
        }
        public void ApplyLCordrer()
        {
            Dictionary<string, int> GlycanOrder = new Dictionary<string, int>(); 
            //Get LC Order
            foreach (GlycanCompound GlycanC in _GlycanList)
            {
                if (GlycanC.GlycanLCorder != 0)
                {
                    GlycanOrder.Add(GlycanC.GlycanKey, GlycanC.GlycanLCorder);
                }
            }
            List<ClusteredPeak> MergeWithLCOrder = new List<ClusteredPeak>();

            foreach (ClusteredPeak MCluster in _MergedResultList)
            {
                if (GlycanOrder.ContainsKey(MCluster.GlycanKey))
                {
                    MergeWithLCOrder.Add(MCluster);
                }
            }
            List<int> IdentifiedOrder = new List<int>();
            foreach (ClusteredPeak g in MergeWithLCOrder)
            {
                IdentifiedOrder.Add(GlycanOrder[g.GlycanKey]);
            }
            //LIC
            List<int> Length = new List<int>();
            List<int> Prev = new List<int>();

            for (int i = 0; i < IdentifiedOrder.Count; i++)
            {
                Length.Add(1);
                Prev.Add(-1);
            }
            for (int i = 0; i < IdentifiedOrder.Count; i++)
            {
                for (int j = i + 1; j < IdentifiedOrder.Count; j++)
                {
                    if (IdentifiedOrder[i] < IdentifiedOrder[j])
                    {
                        if (Length[i] + 1 > Length[j])
                        {
                            Length[j] = Length[i] + 1;
                            Prev[j] = i;
                        }
                    }
                }
            }
            int n = 0, pos = 0;
            for (int i = 0; i < IdentifiedOrder.Count; i++)
            {
                if (Length[i] > n)
                {
                    n = Length[i];
                    pos = i;
                }
            }
            List<int> LIC = new List<int>();
            for (; Prev[pos] != -1; pos = Prev[pos])
            {
                LIC.Add(IdentifiedOrder[pos]);
            }
            LIC.Add(IdentifiedOrder[pos]);
            LIC.Reverse();
            //insert glycan not in LIC within tolerence
            for (int i = 0; i < IdentifiedOrder.Count; i++)
            {
                if (LIC.Contains(IdentifiedOrder[i]))
                {
                    continue;
                }
                int PrvLICIdx = 0;
                int NxtLICIdx = IdentifiedOrder.Count;

                for (int j = i - 1; j >= 0; j--)
                {
                    if(LIC.Contains(IdentifiedOrder[j]))
                    {
                        PrvLICIdx = j;
                        break;
                    }
                }
                for (int j = i+1; j < IdentifiedOrder.Count; j++)
                {
                    if (LIC.Contains(IdentifiedOrder[j]))
                    {
                        NxtLICIdx = j;
                        break;
                    }
                }

                if (Math.Abs(IdentifiedOrder[i] - IdentifiedOrder[PrvLICIdx]) <= 3)
                {
                    LIC.Insert(LIC.IndexOf(IdentifiedOrder[PrvLICIdx]) + 1, IdentifiedOrder[i]);
                     continue;
                }
                if (i < IdentifiedOrder.Count - 1 && Math.Abs(IdentifiedOrder[NxtLICIdx] - IdentifiedOrder[i]) <= 3)
                {

                    LIC.Insert(LIC.IndexOf(IdentifiedOrder[NxtLICIdx]), IdentifiedOrder[i]);
                    continue;                   
                }
            }
            _MergedResultListAfterApplyLCOrder = new List<ClusteredPeak>();
            for (int i = 0; i < _MergedResultList.Count; i++)
            {
                if (!GlycanOrder.ContainsKey(_MergedResultList[i].GlycanKey)) // Glycan no LC order
                {
                    _MergedResultListAfterApplyLCOrder.Add(_MergedResultList[i]);
                }
                else
                {
                    if (LIC.Contains(GlycanOrder[_MergedResultList[i].GlycanKey]))  //Glycan in LIC
                    {
                        _MergedResultListAfterApplyLCOrder.Add(_MergedResultList[i]);
                    }                   
                }
            }          
        }
        public void Process(Object threadContext)
        {
            rawReader.SetPeakProcessorParameter(_peakParameter);
            rawReader.SetTransformParameter(_transformParameters);
            if (MSScanList == null)
            {
                MSScanList = new List<int>();
                for (int i = _StartScan; i <= _EndScan; i++)
                {
                    if (rawReader.GetMsLevel(i) == 1)
                    {
                        MSScanList.Add(i);
                    }
                }
            }
            foreach (int ScanNo in MSScanList)
            {
                ProcessSingleScan(ScanNo);
            }
            _doneEvent.Set();
        }       

        public void ExportToCSV()
        {
            //Merged Cluster
            StreamWriter sw=null;
            try
            {
                sw = new StreamWriter(_ExportFilePath + "\\" + Path.GetFileName(_ExportFilePath) + ".csv");
                //parameters
                sw.WriteLine("Parameters");
                sw.WriteLine("Raw Files:" + _rawFile);
                sw.WriteLine("Range:" + _StartScan + "~" + _EndScan);
                sw.WriteLine("Glycan List:" + _glycanFile);
                sw.WriteLine("Reduced Reducing End:" + _isReducedReducingEnd.ToString());
                sw.WriteLine("Permethylated:" + _isPermethylated.ToString());
                string adduct = "";
                foreach (float add in _adductLabel.Keys)
                {
                    adduct = adduct + add + "(" + _adductLabel[add] + ");";
                }
                sw.WriteLine("Adduct:"+adduct);
                sw.WriteLine("Mass tolerance (PPM):" + _massPPM.ToString());
                sw.WriteLine("Include m/z match only peak:" + _IncludeMZMatch.ToString());
                sw.WriteLine("Max minute in front of LC apex  (a):" + _maxLCFrontMin.ToString());
                sw.WriteLine("Max minute in back of LC apex  (b):" + _maxLCBackMin.ToString());
                sw.WriteLine("Merge different charge glycan:" + _MergeDifferentCharge.ToString());
                sw.WriteLine("Min length of LC Peak in minute (c):" + _minLengthOfLC.ToString());
                sw.WriteLine("Minimum abundance:" + _minAbundance.ToString());
                sw.WriteLine("Signal to noise ratio" + _peakParameter.SignalToNoiseThreshold.ToString());
                sw.WriteLine("Peak background ratio" + _peakParameter.PeakBackgroundRatio.ToString());
                sw.WriteLine("Use absolute peptide intensity" + _transformParameters.UseAbsolutePeptideIntensity.ToString());
                if (_transformParameters.UseAbsolutePeptideIntensity)
                {

                    sw.WriteLine("Absolute peptide intensity:" + _transformParameters.AbsolutePeptideIntensity.ToString());
                }
                else
                {
                    sw.WriteLine("Peptide intensity ratio:" + _transformParameters.PeptideMinBackgroundRatio.ToString());
                }

                
                sw.WriteLine("Start Time,End Time,Start Scan Num,End Scan Num,Peak Intensity,LC Peak Area,HexNac-Hex-deHex-Sia,Composition mono");

                _MergedResultList = _MergedResultList.OrderBy(x => x.GlycanComposition.NoOfHexNAc).ThenBy(x => x.GlycanComposition.NoOfHex).ThenBy(x => x.GlycanComposition.NoOfDeHex).ThenBy(x => x.GlycanComposition.NoOfSia).ThenByDescending(x => x.MonoIntensity).ToList();
                foreach (ClusteredPeak cls in _MergedResultList)
                {
                    if(_minLengthOfLC > (cls.EndTime-cls.StartTime) || cls.MonoIntensity< _minAbundance)
                    {
                        continue;
                    }

                    string export = cls.StartTime + ",";
                    if (cls.EndTime == 0)
                    {
                        export = export + cls.StartTime + ",";
                    }
                    else
                    {
                        export = export + cls.EndTime + ",";
                    }
                    export = export + cls.StartScan + ","
                                     + cls.EndScan + ","
                                     + cls.MonoIntensity.ToString() + ","
                                     + cls.PeakArea.ToString() + ",";

                    if (cls.GlycanComposition != null)
                    {
                        string Composition = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
                        export = export + Composition + "," + cls.GlycanComposition.MonoMass;
                    }
                    else
                    {
                        export = export + ",-,-";
                    }

                    sw.WriteLine(export);
                }
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
            string FullFilename;
            if (_GlycanListContainLCOrder)
            {
                try
                {
                    //With LC Order Result
                    FullFilename = _ExportFilePath + "\\" + Path.GetFileName(_ExportFilePath) + "_wLCorder.csv";
                    sw = new StreamWriter(FullFilename);
                    sw.WriteLine("Start Time,End Time,Start Scan Num,End Scan Num,Abuntance(All Isotoped Clustered),Abundance(Most Intense Peak),HexNac-Hex-deHex-Sia,Composition mono");
                    foreach (ClusteredPeak cls in _MergedResultListAfterApplyLCOrder)
                    {
                        string export = cls.StartTime + ",";
                        if (cls.EndTime == 0)
                        {
                            export = export + cls.StartTime + ",";
                        }
                        else
                        {
                            export = export + cls.EndTime + ",";
                        }
                        export = export + cls.StartScan + ","
                                        + cls.EndScan + ","
                                     + cls.IsotopicClusterIntensity.ToString() + ","
                                     + cls.MostIntenseIntensity.ToString() + ",";

                        if (cls.GlycanComposition != null)
                        {
                            string Composition = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
                            export = export + Composition + "," + cls.GlycanComposition.MonoMass;
                        }
                        else
                        {
                            export = export + ",-,-";
                        }

                        sw.WriteLine(export);
                    }
                    sw.Flush();
                    sw.Close();
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }
            Dictionary<string, List<MatchedGlycanPeak>> dictGlycans = new Dictionary<string, List<MatchedGlycanPeak>>();
            //Single Cluster in each scan
            try
            {
                FullFilename = _ExportFilePath + "\\" + Path.GetFileName(_ExportFilePath) + "_FullList.csv";
                sw = new StreamWriter(FullFilename);
                sw.WriteLine("Time,Scan Num,Abuntance,m/z,HexNac-Hex-deHex-Sia,Adduct,Composition mono");
 

                //Sort by Glycan than Time

                _MatchedPeaksInScan.Sort(delegate(MatchedGlycanPeak M1, MatchedGlycanPeak M2)
                {
                    int r = M1.GlycanKey.CompareTo(M2.GlycanKey);
                    if(r==0) r = M1.ScanTime.CompareTo(M2.ScanTime);
                    if(r==0) r=M1.Peak.MonoIntensity.CompareTo(M2.Peak.MonoIntensity);
                    return r;
                });

                foreach (MatchedGlycanPeak cls in _MatchedPeaksInScan)
                {
                    if (!dictGlycans.ContainsKey(cls.GlycanKey))
                    {
                        dictGlycans[cls.GlycanKey] = new List<MatchedGlycanPeak>();
                    }
                    dictGlycans[cls.GlycanKey].Add(cls);
                    //if (cls.IsotopicClusterIntensity == 0 || cls.Peak.MonoIntensity==0)
                    //{
                    //    continue;
                    //}
                    string export = cls.ScanTime + ","
                                    + cls.ScanNum + ",";

                        export = export + cls.Peak.MonoIntensity.ToString()  + "," + cls.Peak.MonoisotopicMZ;

                    if (cls.GlycanComposition != null)
                    {
                        string Composition = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
                        export = export + "," + Composition + "," + cls.AdductCount.ToString() + "*" + cls.Adduct + "," + cls.GlycanComposition.MonoMass;
                    }
                    else
                    {
                        export = export + ",-,-";
                    }

                    sw.WriteLine(export);
                }
                sw.Flush();
                sw.Close();
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            //Generate images.
                //CreateFolder
            string Dir = _ExportFilePath + "\\Pic";
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            foreach (string key in dictGlycans.Keys)
            {
                GetLCImage(key, dictGlycans[key]).Save(Dir+"\\"+key + ".png", System.Drawing.Imaging.ImageFormat.Png);                
            }
        }
        public void ExportParametersToExcel()
        {
            FileInfo NewFile = new FileInfo(_ExportFilePath);
            OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage(NewFile);


            OfficeOpenXml.ExcelWorksheet CurrentSheet = pck.Workbook.Worksheets.Add("Parameters");
            CurrentSheet.Column(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            CurrentSheet.Column(1).Width = 35;
            CurrentSheet.Cells[1, 1].Value = "Raw File:";
            CurrentSheet.Cells[1, 2].Value = _rawFile;
            CurrentSheet.Cells[2, 1].Value = "Range:";
            CurrentSheet.Cells[2, 2].Value = _StartScan + "~" + _EndScan;
            CurrentSheet.Cells[3, 1].Value = "Glycan List:";
            CurrentSheet.Cells[3, 2].Value = _glycanFile;
            CurrentSheet.Cells[5, 1].Value = "Experiment Section";
            CurrentSheet.Cells[6, 1].Value = "Reduced Reducing End:";
            CurrentSheet.Cells[6, 2].Value = _isReducedReducingEnd.ToString();
            CurrentSheet.Cells[7, 1].Value = "Permethylated:";
            CurrentSheet.Cells[7, 2].Value = _isPermethylated.ToString();
            CurrentSheet.Cells[8, 1].Value = "Adduct:";

            string adduct = "";
            foreach (float add in _adductLabel.Keys)
            {
                adduct = adduct + add + "(" + _adductLabel[add] + ");";
            }
            CurrentSheet.Cells[8, 2].Value = adduct;

            CurrentSheet.Cells[9, 1].Value = "Mass tolerance (PPM):";
            CurrentSheet.Cells[9, 2].Value = _massPPM.ToString();

            CurrentSheet.Cells[10, 1].Value = "Include m/z match only peak:";
            CurrentSheet.Cells[10, 2].Value = _IncludeMZMatch.ToString();

            CurrentSheet.Cells[12, 1].Value = "Merge Section";

            CurrentSheet.Cells[13, 1].Value = "Max minute in front of LC apex  (a):";
            CurrentSheet.Cells[13, 2].Value = _maxLCFrontMin.ToString();

            CurrentSheet.Cells[14, 1].Value = "Max minute in back of LC apex  (b):";
            CurrentSheet.Cells[14, 2].Value = _maxLCBackMin.ToString();

            CurrentSheet.Cells[15, 1].Value = "Merge different charge glycan:";
            CurrentSheet.Cells[15, 2].Value = _MergeDifferentCharge.ToString();

            CurrentSheet.Cells[16, 1].Value = "Min length of LC Peak in minute (c):";
            CurrentSheet.Cells[16, 2].Value = _minLengthOfLC.ToString();

            CurrentSheet.Cells[17, 1].Value = "Minimum abundance:";
            CurrentSheet.Cells[17, 2].Value = _minAbundance.ToString();

            CurrentSheet.Cells[19, 1].Value = "Peak processing parameters";
            CurrentSheet.Cells[20, 1].Value = "Signal to noise ratio:";
            CurrentSheet.Cells[20, 2].Value = _peakParameter.SignalToNoiseThreshold.ToString();
            CurrentSheet.Cells[21, 1].Value = "Peak background ratio:";
            CurrentSheet.Cells[21, 2].Value = _peakParameter.PeakBackgroundRatio.ToString();
            CurrentSheet.Cells[22, 1].Value = "Use absolute peptide intensity:";
            CurrentSheet.Cells[22, 2].Value = _transformParameters.UseAbsolutePeptideIntensity.ToString();
            if (_transformParameters.UseAbsolutePeptideIntensity)
            {

                CurrentSheet.Cells[23, 1].Value = "Absolute peptide intensity:";
                CurrentSheet.Cells[23, 2].Value = _transformParameters.AbsolutePeptideIntensity.ToString();
            }
            else
            {
                CurrentSheet.Cells[23, 1].Value = "Peptide intensity ratio:";
                CurrentSheet.Cells[23, 2].Value = _transformParameters.PeptideMinBackgroundRatio.ToString();
            }

            CurrentSheet.Cells[24, 1].Value = "Max charge";
            CurrentSheet.Cells[24, 2].Value = _transformParameters.MaxCharge;

            System.Reflection.Assembly assm = typeof(frmMainESI).Assembly;
            System.Reflection.AssemblyName assmName = assm.GetName();
            Version ver = assmName.Version;
            CurrentSheet.Cells[27, 1].Value = "Program Version:";
            CurrentSheet.Cells[27, 2].Value = ver.ToString();

            CurrentSheet.Cells[28, 1].Value = "Process Time:";
            CurrentSheet.Cells[28, 2].Value = DateTime.Now.ToString();
          
            pck.Save();            
        }
        public void ExportGlycanToExcel(string argGkey, List<MatchedGlycanPeak> argScanRecord, List<ClusteredPeak> argMergedRecord)
        {
            string Gkey = argGkey;
            int OutputRowCount = 0;
            FileInfo NewFile = new FileInfo( _ExportFilePath);
           OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage(NewFile);
           OfficeOpenXml.ExcelWorksheet CurrentSheet = pck.Workbook.Worksheets.Add(Gkey);

            var picture = CurrentSheet.Drawings.AddPicture(Gkey, GetLCImage(Gkey, argScanRecord));
            picture.SetPosition(0, 0, 9, 0);
            picture.SetSize(1320, 660);
            //CurrentSheet.Row(1).Height = 400;
            CurrentSheet.DefaultRowHeight = 50;

            OutputRowCount = 1;
            CurrentSheet.Cells[OutputRowCount, 1].Value = "Start Time";
            CurrentSheet.Cells[OutputRowCount, 2].Value = "End  Time";
            CurrentSheet.Cells[OutputRowCount, 3].Value = "Start Scan Num";
            CurrentSheet.Cells[OutputRowCount, 4].Value = "End Scan Num";
            CurrentSheet.Cells[OutputRowCount, 5].Value = "Sum Intensity";
            CurrentSheet.Cells[OutputRowCount, 6].Value = "Peak Area";
            CurrentSheet.Cells[OutputRowCount, 7].Value = "HexNac-Hex-deHex-Sia";
            OutputRowCount++;
            //Export Merge Result
            foreach (ClusteredPeak cls in argMergedRecord)
            {
                if (_minLengthOfLC > (cls.EndTime - cls.StartTime) || cls.MonoIntensity < _minAbundance)
                {
                    continue;
                }
                CurrentSheet.Cells[OutputRowCount, 1].Value = cls.StartTime;
                CurrentSheet.Cells[OutputRowCount, 2].Value = cls.EndTime;
                CurrentSheet.Cells[OutputRowCount, 3].Value = cls.StartScan;
                CurrentSheet.Cells[OutputRowCount, 4].Value = cls.EndScan;
                CurrentSheet.Cells[OutputRowCount, 5].Value = cls.MonoIntensity;
                CurrentSheet.Cells[OutputRowCount, 6].Value = cls.PeakArea;

                if (cls.GlycanComposition != null)
                {
                    CurrentSheet.Cells[OutputRowCount, 7].Value = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;

                }
                else
                {
                    CurrentSheet.Cells[OutputRowCount, 7].Value = "-";
                }
                OutputRowCount++;
            }
            CurrentSheet.Row(OutputRowCount).Height = 30; //Empty Row
            OutputRowCount++;
            CurrentSheet.Cells[OutputRowCount, 1].Value = "Time";
            CurrentSheet.Cells[OutputRowCount, 2].Value = "Scan Num";
            CurrentSheet.Cells[OutputRowCount, 3].Value = "Abundance";
            CurrentSheet.Cells[OutputRowCount, 4].Value = "m/z";
            CurrentSheet.Cells[OutputRowCount, 5].Value = "HexNac-Hex-deHex-Sia";
            CurrentSheet.Cells[OutputRowCount, 6].Value = "Adduct";
            OutputRowCount++;

            //Detail 
            //argScanRecord.Sort(delegate(MatchedGlycanPeak M1, MatchedGlycanPeak M2)
            //{
            //    int r = M1.ScanTime.CompareTo(M2.ScanTime);
            //    if (r == 0) r = M1.Adduct.CompareTo(M2.Adduct);
            //    if (r == 0) r = M1.AdductCount.CompareTo(M2.AdductCount);
            //    return r;
            //});

            //foreach (MatchedGlycanPeak cls in sortedScanRecords[Gkey])
            //{
            //    CurrentSheet.Cells[OutputRowCount, 1].Value = cls.ScanTime;
            //    CurrentSheet.Cells[OutputRowCount, 2].Value = cls.ScanNum;
            //    CurrentSheet.Cells[OutputRowCount, 3].Value = cls.Peak.MonoIntensity;
            //    CurrentSheet.Cells[OutputRowCount, 4].Value = cls.Peak.MonoisotopicMZ;
            //    if (cls.GlycanComposition != null)
            //    {
            //        CurrentSheet.Cells[OutputRowCount, 5].Value = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
            //        CurrentSheet.Cells[OutputRowCount, 6].Value = cls.Adduct + "*" + cls.AdductCount;
            //    }
            //    else
            //    {
            //        CurrentSheet.Cells[OutputRowCount, 5].Value = "-";
            //        CurrentSheet.Cells[OutputRowCount, 6].Value = "-";
            //    }
            //    OutputRowCount++;
            //}
            pck.Save();
            //pck.Dispose();
            CurrentSheet.Dispose();
            GC.Collect();
        }
        public Image GetLCImage(string argGKey, List<MatchedGlycanPeak> argMatchedScan)
        {
            string Gkey = argGKey;
            ZedGraph.ZedGraphControl zgcGlycan = new ZedGraph.ZedGraphControl();
            zgcGlycan.Width = 2400;
            zgcGlycan.Height = 1200;

            GraphPane GP = zgcGlycan.GraphPane;
            GP.Title.Text = "Glycan: " + Gkey;
            GP.XAxis.Title.Text = "Scan time (min)";
            GP.YAxis.Title.Text = "Abundance";
            GP.CurveList.Clear();
            Dictionary<string, PointPairList> dictAdductPoints = new Dictionary<string, PointPairList>();
            Dictionary<float, float> MergeIntensity = new Dictionary<float, float>();
            List<float> Time = new List<float>();
            foreach (MatchedGlycanPeak MPeak in argMatchedScan)
            {
                if (!dictAdductPoints.ContainsKey(MPeak.Adduct))
                {
                    dictAdductPoints.Add(MPeak.Adduct, new PointPairList());
                }
                dictAdductPoints[MPeak.Adduct].Add(MPeak.ScanTime, MPeak.Peak.MonoIntensity);

                if (!MergeIntensity.ContainsKey(Convert.ToSingle(MPeak.ScanTime)))
                {
                    MergeIntensity.Add(Convert.ToSingle(MPeak.ScanTime), 0);
                }
                MergeIntensity[Convert.ToSingle(MPeak.ScanTime)] = MergeIntensity[Convert.ToSingle(MPeak.ScanTime)] + MPeak.Peak.MonoIntensity;

                if (!Time.Contains(Convert.ToSingle(MPeak.ScanTime)))
                {
                    Time.Add(Convert.ToSingle(MPeak.ScanTime));
                }
            }
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
            return (Image)zgcGlycan.MasterPane.GetImage();
        }
        public void ExportToExcel()
        {
            try
            {

                _MatchedPeaksInScan.Sort(delegate(MatchedGlycanPeak M1, MatchedGlycanPeak M2)
                {
                    int r = M1.GlycanKey.CompareTo(M2.GlycanKey);
                    if (r == 0) r = M1.ScanTime.CompareTo(M2.ScanTime);
                    if (r == 0) r = M1.Peak.MonoIntensity.CompareTo(M2.Peak.MonoIntensity);
                    return r;
                });

                Dictionary<string, List<MatchedGlycanPeak>> sortedScanRecords = new Dictionary<string, List<MatchedGlycanPeak>>();
                foreach (MatchedGlycanPeak MPeak in _MatchedPeaksInScan)
                {
                    if (!sortedScanRecords.ContainsKey(MPeak.GlycanKey))
                    {
                        sortedScanRecords.Add(MPeak.GlycanKey, new List<MatchedGlycanPeak>());
                    }
                    sortedScanRecords[MPeak.GlycanKey].Add(MPeak);
                }
                
                              
                _MergedResultList.Sort(delegate(ClusteredPeak CPeak1, ClusteredPeak CPeak2)
                {
                    int r = CPeak1.GlycanKey.CompareTo(CPeak2.GlycanKey);
                    if (r == 0) r = CPeak1.StartTime.CompareTo(CPeak2.StartTime);
                    if (r == 0) r = CPeak1.MonoIntensity.CompareTo(CPeak2.MonoIntensity);
                    return r = 0;
                });
                Dictionary<string, List<ClusteredPeak>> sortedMergeRecords = new Dictionary<string, List<ClusteredPeak>>();
                foreach (ClusteredPeak CPeak in _MergedResultList)
                {
                    if (!sortedMergeRecords.ContainsKey(CPeak.GlycanKey))
                    {
                        sortedMergeRecords.Add(CPeak.GlycanKey, new List<ClusteredPeak>());
                    }
                    sortedMergeRecords[CPeak.GlycanKey].Add(CPeak);
                }


                int ColorIdx = 0;

                FileInfo NewFile = new FileInfo( _ExportFilePath);
                if (NewFile.Exists)
                {
                    File.Delete(NewFile.FullName);
                }
                OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage(NewFile);


                OfficeOpenXml.ExcelWorksheet CurrentSheet = pck.Workbook.Worksheets.Add("Parameters");
                CurrentSheet.Column(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                CurrentSheet.Column(1).Width = 35;
                CurrentSheet.Cells[1, 1].Value = "Raw File:";
                CurrentSheet.Cells[1, 2].Value = _rawFile;
                CurrentSheet.Cells[2, 1].Value = "Range:";
                CurrentSheet.Cells[2, 2].Value = _StartScan + "~" + _EndScan;
                CurrentSheet.Cells[3, 1].Value = "Glycan List:";
                CurrentSheet.Cells[3, 2].Value = _glycanFile;
                CurrentSheet.Cells[5, 1].Value = "Experiment Section";
                CurrentSheet.Cells[6, 1].Value = "Reduced Reducing End:";
                CurrentSheet.Cells[6, 2].Value = _isReducedReducingEnd.ToString();
                CurrentSheet.Cells[7, 1].Value = "Permethylated:";
                CurrentSheet.Cells[7, 2].Value = _isPermethylated.ToString();
                CurrentSheet.Cells[8, 1].Value = "Adduct:";

                string adduct = "";
                foreach (float add in _adductLabel.Keys)
                {
                    adduct = adduct + add + "("+ _adductLabel[add]+");";
                }
                CurrentSheet.Cells[8, 2].Value = adduct;

                CurrentSheet.Cells[9, 1].Value = "Mass tolerance (PPM):";
                CurrentSheet.Cells[9, 2].Value = _massPPM.ToString();

                CurrentSheet.Cells[10, 1].Value = "Include m/z match only peak:";
                CurrentSheet.Cells[10, 2].Value = _IncludeMZMatch.ToString();

                CurrentSheet.Cells[12, 1].Value = "Merge Section";

                CurrentSheet.Cells[13, 1].Value = "Max minute in front of LC apex  (a):";
                CurrentSheet.Cells[13, 2].Value = _maxLCFrontMin.ToString();

                CurrentSheet.Cells[14, 1].Value = "Max minute in back of LC apex  (b):";
                CurrentSheet.Cells[14, 2].Value = _maxLCBackMin.ToString();

                CurrentSheet.Cells[15, 1].Value = "Merge different charge glycan:";
                CurrentSheet.Cells[15, 2].Value = _MergeDifferentCharge.ToString();

                CurrentSheet.Cells[16, 1].Value = "Min length of LC Peak in minute (c):";
                CurrentSheet.Cells[16, 2].Value = _minLengthOfLC.ToString();

                CurrentSheet.Cells[17, 1].Value = "Minimum abundance:";
                CurrentSheet.Cells[17, 2].Value = _minAbundance.ToString();

                CurrentSheet.Cells[19, 1].Value = "Peak processing parameters";
                CurrentSheet.Cells[20, 1].Value = "Signal to noise ratio";
                CurrentSheet.Cells[20, 2].Value = _peakParameter.SignalToNoiseThreshold.ToString();
                CurrentSheet.Cells[21, 1].Value = "Peak background ratio";
                CurrentSheet.Cells[21, 2].Value = _peakParameter.PeakBackgroundRatio.ToString();
                CurrentSheet.Cells[22, 1].Value = "Use absolute peptide intensity";
                CurrentSheet.Cells[22, 2].Value = _transformParameters.UseAbsolutePeptideIntensity.ToString();
                if (_transformParameters.UseAbsolutePeptideIntensity)
                {
        
                    CurrentSheet.Cells[23, 1].Value = "Absolute peptide intensity";
                    CurrentSheet.Cells[23, 2].Value = _transformParameters.AbsolutePeptideIntensity.ToString();
                }
                else
                {
                    CurrentSheet.Cells[23, 1].Value = "Peptide intensity ratio";
                    CurrentSheet.Cells[23, 2].Value = _transformParameters.PeptideMinBackgroundRatio.ToString();
                }

                CurrentSheet.Cells[24, 1].Value = "Max charge";
                CurrentSheet.Cells[24, 2].Value = _transformParameters.MaxCharge;

                System.Reflection.Assembly assm = typeof(frmMainESI).Assembly;
                System.Reflection.AssemblyName assmName = assm.GetName();
                Version ver = assmName.Version;
                CurrentSheet.Cells[27, 1].Value = "Program Version:";
                CurrentSheet.Cells[27, 2].Value = ver.ToString();
               
                CurrentSheet.Cells[28, 1].Value = "Process Time:";
                CurrentSheet.Cells[28, 2].Value = DateTime.Now.ToString();

                int OutputRowCount = 0;
                pck.Save();
                CurrentSheet.Dispose();
                foreach (string Gkey in sortedMergeRecords.Keys)
                {

                    OutputRowCount = 0;
                    pck = new OfficeOpenXml.ExcelPackage(NewFile);
                    CurrentSheet = pck.Workbook.Worksheets.Add(Gkey);

                    var picture = CurrentSheet.Drawings.AddPicture(Gkey, GetLCImage(Gkey,sortedScanRecords[Gkey]));
                    picture.SetPosition(0, 0, 9, 0);
                    picture.SetSize(1320, 660);
                    CurrentSheet.Row(1).Height = 400;
                    CurrentSheet.DefaultRowHeight = 50;

                    OutputRowCount = 1;
                    CurrentSheet.Cells[OutputRowCount, 1].Value = "Start Time";
                    CurrentSheet.Cells[OutputRowCount, 2].Value = "End  Time";
                    CurrentSheet.Cells[OutputRowCount, 3].Value = "Start Scan Num";
                    CurrentSheet.Cells[OutputRowCount, 4].Value = "End Scan Num";
                    CurrentSheet.Cells[OutputRowCount, 5].Value = "Sum Intensity";
                    CurrentSheet.Cells[OutputRowCount, 6].Value = "Peak Area";
                    CurrentSheet.Cells[OutputRowCount, 7].Value = "HexNac-Hex-deHex-Sia";
                    OutputRowCount++;
                    //Export Merge Result
                    foreach (ClusteredPeak cls in sortedMergeRecords[Gkey])
                    {
                        if (_minLengthOfLC > (cls.EndTime - cls.StartTime) || cls.MonoIntensity < _minAbundance)
                        {
                            continue;
                        }
                        CurrentSheet.Cells[OutputRowCount, 1].Value = cls.StartTime;
                        CurrentSheet.Cells[OutputRowCount, 2].Value = cls.EndTime;
                        CurrentSheet.Cells[OutputRowCount, 3].Value = cls.StartScan;
                        CurrentSheet.Cells[OutputRowCount, 4].Value = cls.EndScan;
                        CurrentSheet.Cells[OutputRowCount, 5].Value = cls.MonoIntensity;
                        CurrentSheet.Cells[OutputRowCount, 6].Value = cls.PeakArea;

                        if (cls.GlycanComposition != null)
                        {
                            CurrentSheet.Cells[OutputRowCount, 7].Value = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
                         
                        }
                        else
                        {
                            CurrentSheet.Cells[OutputRowCount, 7].Value = "-";
                        }              
                        OutputRowCount++;
                    }
                    
                    //CurrentSheet.Row(OutputRowCount).Height = 30; //Empty Row
                    //OutputRowCount++;
                    //CurrentSheet.Cells[OutputRowCount, 1].Value = "Time";
                    //CurrentSheet.Cells[OutputRowCount, 2].Value = "Scan Num";
                    //CurrentSheet.Cells[OutputRowCount, 3].Value = "Abundance";
                    //CurrentSheet.Cells[OutputRowCount, 4].Value = "m/z";
                    //CurrentSheet.Cells[OutputRowCount, 5].Value = "HexNac-Hex-deHex-Sia";
                    //CurrentSheet.Cells[OutputRowCount, 6].Value = "Adduct";
                    //OutputRowCount++;
                    //sortedScanRecords[Gkey].Sort(delegate(MatchedGlycanPeak M1, MatchedGlycanPeak M2)
                    //{
                    //    int r = M1.ScanTime.CompareTo(M2.ScanTime);
                    //    if (r == 0) r = M1.Adduct.CompareTo(M2.Adduct);
                    //    if (r == 0) r = M1.AdductCount.CompareTo(M2.AdductCount);
                    //    return r;
                    //});

                    //foreach (MatchedGlycanPeak cls in sortedScanRecords[Gkey])
                    //{
                    //    CurrentSheet.Cells[OutputRowCount, 1].Value = cls.ScanTime;
                    //    CurrentSheet.Cells[OutputRowCount, 2].Value = cls.ScanNum;
                    //    CurrentSheet.Cells[OutputRowCount, 3].Value = cls.Peak.MonoIntensity;
                    //    CurrentSheet.Cells[OutputRowCount, 4].Value = cls.Peak.MonoisotopicMZ;
                    //    if (cls.GlycanComposition != null)
                    //    {
                    //        CurrentSheet.Cells[OutputRowCount, 5].Value = cls.GlycanComposition.NoOfHexNAc + "-" + cls.GlycanComposition.NoOfHex + "-" + cls.GlycanComposition.NoOfDeHex + "-" + cls.GlycanComposition.NoOfSia;
                    //        CurrentSheet.Cells[OutputRowCount, 6].Value = cls.Adduct + "*" + cls.AdductCount;
                    //    }
                    //    else
                    //    {
                    //        CurrentSheet.Cells[OutputRowCount, 5].Value = "-";
                    //        CurrentSheet.Cells[OutputRowCount, 6].Value = "-";
                    //    }
                    //    OutputRowCount++;
                    //}
                    pck.Save();
                    pck.Stream.Close();
                    CurrentSheet.Dispose();
                    pck.Dispose();
                }
            

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        private List<float> GenerateCandidatePeakList(List<GlycanCompound> argGlycanComList)
        {
            _lstCandidatePeak = new List<CandidatePeak>();
            List<float> CandidateMzList = new List<float>();
            _dicCandidatePeak = new Dictionary<float, List<CandidatePeak>>(); //CandidateMZ to Glycan
            foreach (GlycanCompound comp in argGlycanComList)
            {
                for (int i = 1; i <= _MaxCharge; i++) //Charge
                {
                    foreach (float adductMass in _adductMass)
                    {
                        for (int j = 0; j <= i; j++) //Adduct Number
                        {
                            string AdductLabel = _adductLabel[adductMass];
                            float AdductMass = adductMass;
                            CandidatePeak tmpCandidate =  new CandidatePeak(comp, i, AdductMass, j, AdductLabel);
                            if (j == 0)
                            {
                                AdductLabel = "H";
                                AdductMass = Atoms.ProtonMass;
                                tmpCandidate = new CandidatePeak(comp, i, AdductMass, 1, AdductLabel);
                            }

                            
                            //_lstCandidatePeak.Add(tmpCandidate);

                            //If candidateMZ has the same value don't add into list;
                            if (!CandidateMzList.Contains(tmpCandidate.TotalMZ))
                            {
                                CandidateMzList.Add(tmpCandidate.TotalMZ);
                            }
                            
                            //Inseet to dictionary <mz, List<candidates>>
                            if (!_dicCandidatePeak.ContainsKey(tmpCandidate.TotalMZ ))
                            {
                                _dicCandidatePeak.Add(tmpCandidate.TotalMZ, new List<CandidatePeak>());
                            }
                            bool FoundSameGlycanKey = false;
                            foreach (CandidatePeak CP in _dicCandidatePeak[tmpCandidate.TotalMZ])
                            {
                                if(CP.GlycanKey == tmpCandidate.GlycanKey) //Current Glycan already in List
                                {
                                    FoundSameGlycanKey = true;
                                    break;
                                }                                
                            }
                            if (!FoundSameGlycanKey)
                            {
                                ((List<CandidatePeak>)(_dicCandidatePeak[tmpCandidate.TotalMZ])).Add(tmpCandidate);
                            }
                        }
                    }                    
                }
            }            
            //_lstCandidatePeak.Sort();            
            CandidateMzList.Sort();
            return CandidateMzList;
        }
        private List<MatchedGlycanPeak> FindClusterWGlycanList(List<MSPeak> argPeaks, int argScanNum, double argTime)
        {
            //List<ClusteredPeak> ClsPeaks = new List<ClusteredPeak>(); //Store all cluster in this scan
            List<MatchedGlycanPeak> MatchedPeaks = new List<MatchedGlycanPeak>();
            List<MSPeak> SortedPeaks = argPeaks;
            SortedPeaks.Sort(delegate(MSPeak P1, MSPeak P2) { return Comparer<double>.Default.Compare(P1.MonoisotopicMZ, P2.MonoisotopicMZ); });

            foreach (MSPeak p in SortedPeaks)
            {
                //PeakMZ.Add(p.MonoisotopicMZ);
                //int ClosedPeakIdx = MassLib.MassUtility.GetClosestMassIdx(_candidateMzList,p.MonoisotopicMZ);                
                //List<CandidatePeak> ClosedPeaks = _dicCandidatePeak[_candidateMzList[ClosedPeakIdx]];
                //foreach (CandidatePeak ClosedPeak in ClosedPeaks)
                List<int> ClosedPeaksIdxs = MassLib.MassUtility.GetClosestMassIdxsWithinPPM(_candidateMzList, p.MonoisotopicMZ, (float)_massPPM);
                foreach (int ClosedPeakIdx in ClosedPeaksIdxs)
                {
                    List<CandidatePeak> ClosedGlycans = _dicCandidatePeak[_candidateMzList[ClosedPeakIdx]];
                    foreach (CandidatePeak ClosedGlycan in ClosedGlycans)
                    { 
                        if (p.ChargeState == ClosedGlycan.Charge &&
                            Math.Abs(MassLib.MassUtility.GetMassPPM(ClosedGlycan.TotalMZ, p.MonoisotopicMZ)) <= _massPPM)
                        {
                            MatchedGlycanPeak MatchedGlycanP = new MatchedGlycanPeak(argScanNum, argTime, p, ClosedGlycan.GlycanComposition, ClosedGlycan.AdductLabel,ClosedGlycan.AdductNo);
                            MatchedPeaks.Add(MatchedGlycanP);
                            /*ClusteredPeak tmpPeak = new ClusteredPeak(argScanNum);
                            tmpPeak.EndScan = argScanNum;
                            tmpPeak.StartTime = argTime;
                            tmpPeak.EndTime = argTime;
                            tmpPeak.Charge = ClosedPeak.Charge;
                            tmpPeak.GlycanComposition = ClosedPeak.GlycanComposition;
                            tmpPeak.Peaks.Add(p);
                            tmpPeak.Adduct = ClosedPeak.AdductLabel;
                            ClsPeaks.Add(tmpPeak);*/
                        }
                    }
                }
            }
           

            //foreach (GlycanCompound comp in _GlycanList)
            //{
            //    float[] GlycanMZ = new float[_MaxCharge + 1]; // GlycanMZ[1] = charge 1; GlycanMZ[2] = charge 2
            //    for (int i = 1; i <= _MaxCharge; i++)
            //    {
            //        GlycanMZ[i] = (float)(comp.MonoMass + MassLib.Atoms.ProtonMass * i) / (float)i;
            //    }
            //    for (int i = 1; i <= _MaxCharge; i++)
            //    {
            //        int ClosedPeak = MassLib.MassUtility.GetClosestMassIdx(PeakMZ, GlycanMZ[i]);
            //        int ChargeState = Convert.ToInt32(SortedPeaks[ClosedPeak].ChargeState);
            //        if (ChargeState == 0 || ChargeState != i ||
            //            (MassLib.MassUtility.GetClosestMassIdx(PeakMZ, GlycanMZ[i]) == 0 && PeakMZ[0] - GlycanMZ[i] > 10.0f) ||
            //            (MassLib.MassUtility.GetClosestMassIdx(PeakMZ, GlycanMZ[i]) == PeakMZ.Count - 1 && GlycanMZ[i] - PeakMZ[PeakMZ.Count - 1] > 10.0f))
            //        {
            //            continue;
            //        }
            //        else
            //        {
            //            //GetMassPPM(SortedPeaks[ClosedPeak].MonoisotopicMZ,GlycanMZ[i])> _glycanPPM
            //            /// Cluster of glycan
            //            /// Z = 1 [M+H]     [M+NH4]
            //            /// Z = 2 [M+2H]   [M+NH4+H]	    [M+2NH4]
            //            /// Z = 3 [M+3H]	[M+NH4+2H]	[M+2NH4+H] 	[M+3NH4]
            //            /// Z = 4 [M+4H]	[M+NH4+3H]	[M+2NH4+2H]	[M+3NH4+H]	[M+4NH4]
            //            if (_adductMass.Count == 0)
            //            {
            //                _adductMass.Add(0.0f);
            //            }
            //            foreach (float adductMass in _adductMass)
            //            {
            //                float[] Step = new float[ChargeState + 1];
            //                //Step[0] = GlycanMZ[i];
            //                for (int j = 0; j <= ChargeState; j++)
            //                {
            //                    Step[j] = (GlycanMZ[1] + adductMass * j) / ChargeState;
            //                }
            //                int[] PeakIdx = new int[Step.Length];
            //                for (int j = 0; j < PeakIdx.Length; j++)
            //                {
            //                    PeakIdx[j] = -1;
            //                }
            //                for (int j = 0; j < PeakIdx.Length; j++)
            //                {
            //                    int ClosedPeak2 = Convert.ToInt32(MassLib.MassUtility.GetClosestMassIdx(PeakMZ, Step[j]));
            //                    if (GetMassPPM(PeakMZ[ClosedPeak2], Step[j]) < _massPPM)
            //                    {
            //                        PeakIdx[j] = ClosedPeak2;
            //                    }
            //                }
            //                ClusteredPeak Cls = new ClusteredPeak(argScanNum);
            //                for (int j = 0; j < PeakIdx.Length; j++)
            //                {
            //                    if (PeakIdx[j] != -1)
            //                    {
            //                        Cls.Peaks.Add(SortedPeaks[PeakIdx[j]]);
            //                    }
            //                }
            //                if (Cls.Peaks.Count > 0)
            //                {
            //                    Cls.StartTime = argTime;
            //                    Cls.EndTime = argTime;
            //                    Cls.Charge = i;
            //                    Cls.GlycanCompostion = comp;
            //                    Cls.AdductMass = adductMass;
            //                    if (!ClsPeaks.Contains(Cls))
            //                    {
            //                        ClsPeaks.Add(Cls);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            return MatchedPeaks;
        }
        //private List<MatchedGlycanPeak> FindClusterWOGlycanList(List<MSPeak> argPeaks, int argScanNum, double argTime)
        //{
        //    List<MatchedGlycanPeak> ClsPeaks = new List<MatchedGlycanPeak>();
        //    List<MSPeak> SortedPeaks = argPeaks;
        //    SortedPeaks.Sort(delegate(MSPeak P1, MSPeak P2) { return Comparer<double>.Default.Compare(P1.MonoisotopicMZ, P2.MonoisotopicMZ); });

        //    if (_adductMass.Count == 0)
        //    {
        //        _adductMass.Add(0.0f);
        //    }
        //    for (int i = 0; i < SortedPeaks.Count; i++)
        //    {
        //        /// Cluster of glycan
        //        /// Z = 1 [M+H]     [M+NH4]
        //        /// Z = 2 [M+2H]   [M+NH4+H]	    [M+2NH4]
        //        /// Z = 3 [M+3H]	[M+NH4+2H]	[M+2NH4+H] 	[M+3NH4]
        //        /// Z = 4 [M+4H]	[M+NH4+3H]	[M+2NH4+2H]	[M+3NH4+H]	[M+4NH4]
        //        //Create cluster interval
        //        foreach (float adductMass in _adductMass)
        //        {                  
        //            double[] Step = new double[Convert.ToInt32(SortedPeaks[i].ChargeState) + 1];
        //            //double NH3 = MassLib.Atoms.NitrogenMass + 3 * MassLib.Atoms.HydrogenMass;
        //            Step[0] = SortedPeaks[i].MonoisotopicMZ;
        //            for (int j = 1; j <= SortedPeaks[i].ChargeState; j++)
        //            {
        //                Step[j] = Step[j - 1] + (adductMass) / SortedPeaks[i].ChargeState;
        //            }
        //            int[] PeakIdx = new int[Step.Length];
        //            PeakIdx[0] = i;
        //            for (int j = 1; j < PeakIdx.Length; j++)
        //            {
        //                PeakIdx[j] = -1;
        //            }
        //            int CurrentMatchIdx = 1;
        //            for (int j = i + 1; j < SortedPeaks.Count; j++)
        //            {
        //                if (SortedPeaks[i].ChargeState != SortedPeaks[j].ChargeState)
        //                {
        //                    continue;
        //                }
        //                for (int k = CurrentMatchIdx; k < Step.Length; k++)
        //                {
        //                    if (GetMassPPM(Step[k], SortedPeaks[j].MonoisotopicMZ) < _massPPM)
        //                    {
        //                        PeakIdx[k] = j;
        //                        CurrentMatchIdx = k + 1;
        //                        break;
        //                    }
        //                }
        //            }
        //            //FIX 
        //            //Cluster status check 
        //            //ClusteredPeak Cls = new ClusteredPeak(argScanNum);
        //            //for (int j = 0; j < PeakIdx.Length; j++)
        //            //{
        //            //    if (PeakIdx[j] != -1)
        //            //    {
        //            //        Cls.Peaks.Add(SortedPeaks[PeakIdx[j]]);
        //            //    }
        //            //}
        //            //Cls.StartTime = argTime;
        //            //Cls.EndTime = argTime;
        //            //Cls.Charge = SortedPeaks[i].ChargeState;
        //            //Cls.Adduct = _adductLabel[adductMass];
        //            //if (!ClsPeaks.Contains(Cls))
        //            //{
        //            //    ClsPeaks.Add(Cls);
        //            //}
        //        }
        //    }
        //    return ClsPeaks;
        //}
        /// <summary>
        /// Merge Multiple scan into one cluser by glycan composition and charge
        /// </summary>
        /// <param name="argDurationMin"></param>
        /// <returns></returns>
        public void MergeCluster()
        {
            //List<ClusteredPeak> MergedClusterForAllKeys = new List<ClusteredPeak>();
            _MergedResultList = new List<ClusteredPeak>();  //Store Result
            Dictionary<string, List<MatchedGlycanPeak>> dictAllPeak = new Dictionary<string, List<MatchedGlycanPeak>>();  //KEY: GlycanKey ot GlycanKey+Charge
            List<string> GlycanWProton = new List<string>(); //Store Glycan with Proton adduct
            for (int i = 0; i < _MatchedPeaksInScan.Count; i++)
            {
                string key = "";
                if (_MergeDifferentCharge)
                {
                    key = _MatchedPeaksInScan[i].GlycanKey;
                }
                else
                {
                    key = _MatchedPeaksInScan[i].GlycanKey + "-" +
                                     _MatchedPeaksInScan[i].Charge.ToString();
                }
                if (!dictAllPeak.ContainsKey(key))
                {
                    dictAllPeak.Add(key, new List<MatchedGlycanPeak>());
                }
                dictAllPeak[key].Add(_MatchedPeaksInScan[i]);
                if (_MatchedPeaksInScan[i].Adduct == "H" && !GlycanWProton.Contains(key))
                {
                    GlycanWProton.Add(key);
                }
            }
            foreach (string KEY in dictAllPeak.Keys)
            {
                if (!GlycanWProton.Contains(KEY))  //Skip identified glycans without Proton adduct;
                {
                    continue;
                }
                List<MatchedGlycanPeak> AllPeaksWithSameGlycan = dictAllPeak[KEY];
               
                //if (AllPeaksWithSameGlycan[AllPeaksWithSameGlycan.Count - 1].ScanTime - AllPeaksWithSameGlycan[0].ScanTime <= _maxLCFrontMin)
                //{   //All peaks within duration
                    //mergedPeak = (ClusteredPeak)CLSPeaks[0].Clone();
                  
                    //Sum up intensity
                    Dictionary<double, double> SumIntensity = new Dictionary<double, double>();
                    foreach (MatchedGlycanPeak MatchedPeak in AllPeaksWithSameGlycan)
                    {
                        if (!SumIntensity.ContainsKey(MatchedPeak.ScanTime))
                        {
                            SumIntensity.Add(MatchedPeak.ScanTime,0.0f);
                        }
                        SumIntensity[MatchedPeak.ScanTime] = SumIntensity[MatchedPeak.ScanTime] + MatchedPeak.MostIntenseIntensity;
                    }
                    List<MSPoint> lstMSPs = new List<MSPoint>();
                    foreach (double time in SumIntensity.Keys)
                    {
                        lstMSPs.Add(new MSPoint(Convert.ToSingle(time), Convert.ToSingle(SumIntensity[time])));
                    }
                    lstMSPs.Sort(delegate(MSPoint p1, MSPoint p2) { return p1.Mass.CompareTo(p2.Mass); });

                    //Smooth
                    List<MassLib.MSPoint> lstSmoothPnts = new List<MassLib.MSPoint>();
                    lstSmoothPnts = MassLib.Smooth.SavitzkyGolay.Smooth(lstMSPs, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);

                    //Peak Finding
                    List<MassLib.LCPeak> lcPk = null;
                    lcPk = MassLib.LCPeakDetection.PeakFinding(lstSmoothPnts, 0.1f, 0.01f);

                    //Create Result Peak
                    for (int i = 0; i < lcPk.Count; i++)
                    {
                        ClusteredPeak MergedPeak = new ClusteredPeak();
                        MergedPeak.LCPeak = lcPk[i];
                        foreach (MatchedGlycanPeak ClusterPeak in AllPeaksWithSameGlycan)
                        {
                            if (ClusterPeak.ScanTime >= lcPk[i].StartTime && ClusterPeak.ScanTime <= lcPk[i].EndTime)
                            {
                                MergedPeak.MatchedPeaksInScan.Add(ClusterPeak);
                            }
                        }      
                        if (MergedPeak.IsotopicClusterIntensity > _minAbundance &&
                            MergedPeak.TimeInterval >= _maxLCBackMin &&
                             MergedPeak.TimeInterval < _maxLCFrontMin &&
                              MergedPeak.MatchedPeaksInScan.Count >= _minLengthOfLC)
                        {
                            _MergedResultList.Add(MergedPeak);
                        }
                    }
                //}
                //else //Split into multiple clusters because exceed Max LC time
                //{
                //    //int ScanCount = 0;
                    
                //    List<string> ScanInterval = new List<string>();
                //    int StartScanIdx = 0;
                   
                //    for (int i = 1; i < AllPeaksWithSameGlycan.Count; i++)
                //    {
                //        if (AllPeaksWithSameGlycan[i].ScanTime - AllPeaksWithSameGlycan[i-1].ScanTime > 0.1)  //Merge Scan within 0.1 min
                //        {
                //            ScanInterval.Add(StartScanIdx.ToString() + "-" + (i - 1).ToString());
                //            StartScanIdx = i;
                //        }
                //        //if (MergedPeak == null)
                //        //{
                //        //    //mergedPeak = (ClusteredPeak)CLSPeaks[i].Clone();
                //        //    MergedPeak = new ClusteredPeak();
                   
                //        //    ScanCount = 1;
                //        //    continue;
                //        //}
                //        //if (CLSPeaks[i].ScanTime - mergedPeak.EndTime < 1.0)
                //        //{
                //        //    mergedPeak.EndTime = CLSPeaks[i].ScanTime;
                //        //    mergedPeak.EndScan = CLSPeaks[i].ScanNum;                          
                //        //    ScanCount++;
                //        //}
                //        //else //New Cluster
                //        //{
                //        //    double timeinterval = mergedPeak.EndTime - mergedPeak.StartTime;
                //        //    if (mergedPeak.IsotopicClusterIntensity > _minAbundance &&
                //        //        timeinterval > _maxLCBackMin &&
                //        //        timeinterval < _maxLCFrontMin &&
                //        //        ScanCount > _minLengthOfLC
                //        //        )
                //        //    {
                //        //        _MergedResultList.Add(mergedPeak);
                //        //    }
                //        //    //mergedPeak = (ClusteredPeak)CLSPeaks[i].Clone();
                //        //    mergedPeak.Adduct = CLSPeaks[i].Adduct;
                //        //    ScanCount = 1;
                //        //}
                //    }
                //    ScanInterval.Add(StartScanIdx.ToString() + "-" + (AllPeaksWithSameGlycan.Count - 1).ToString());
                //    foreach (string str in ScanInterval)
                //    {
                //        int StrScan = Convert.ToInt32(str.Split('-')[0]);
                //        int EndScan = Convert.ToInt32(str.Split('-')[1]);
                //        ClusteredPeak MergedPeak = new ClusteredPeak();
                //        for (int i = StrScan; i <= EndScan; i++)
                //        {
                //            MergedPeak.MatchedPeaksInScan.Add(AllPeaksWithSameGlycan[i]);
                //        }
                //        if (MergedPeak.IsotopicClusterIntensity > _minAbundance &&
                //             MergedPeak.TimeInterval >= _maxLCBackMin &&
                //              MergedPeak.TimeInterval < _maxLCFrontMin &&
                //              MergedPeak.MatchedPeaksInScan.Count >= _minLengthOfLC)
                //               {
                //                                    _MergedResultList.Add(MergedPeak);
                //               }
                //    }
                //    //if (_MergedResultList.Count > 1 && _MergedResultList[_MergedResultList.Count - 1] != mergedPeak) //Add last Cluster into result
                //    //{
                //    //    double timeinterval = mergedPeak.EndTime - mergedPeak.StartTime;
                //    //    if (mergedPeak.IsotopicClusterIntensity > _minAbundance &&
                //    //        timeinterval > _maxLCBackMin &&
                //    //         timeinterval < _maxLCFrontMin &&
                //    //         ScanCount > _minLengthOfLC)
                //    //    {
                //    //        _MergedResultList.Add(mergedPeak);
                //    //    }
                //    //}
                //}
            }
            //_MergedResultList = MergedCluster;
        }
        public void MergeSingleScanResultToPeak()
        {
            if (_2ndPassedPeaksInScan != null)
            {
                _MatchedPeaksInScan.AddRange(_2ndPassedPeaksInScan); 
            }
            _MergedResultList = new List<ClusteredPeak>();  //Store Result
            Dictionary<string, List<MatchedGlycanPeak>> dictAllPeak = new Dictionary<string, List<MatchedGlycanPeak>>();  //KEY: GlycanKey ot GlycanKey+Charge
            List<string> GlycanWProton = new List<string>(); //Store Glycan with Proton adduct
            

            for (int i = 0; i < _MatchedPeaksInScan.Count; i++)
            {
                string key = "";
                if (_MergeDifferentCharge)
                {
                    key = _MatchedPeaksInScan[i].GlycanKey;
                }
                else
                {
                    key = _MatchedPeaksInScan[i].GlycanKey + "-" +
                                     _MatchedPeaksInScan[i].Charge.ToString();
                }
                if (!dictAllPeak.ContainsKey(key))
                {
                    dictAllPeak.Add(key, new List<MatchedGlycanPeak>());
                }
                dictAllPeak[key].Add(_MatchedPeaksInScan[i]);
                if (_MatchedPeaksInScan[i].Adduct == "H" && !GlycanWProton.Contains(key))
                {
                    GlycanWProton.Add(key);
                }
            }
            foreach (string KEY in dictAllPeak.Keys)
            {
                if (!GlycanWProton.Contains(KEY))  //Skip identified glycans without Proton adduct;
                {
                    continue;
                }
                List<MatchedGlycanPeak> AllPeaksWithSameGlycan = dictAllPeak[KEY];
                AllPeaksWithSameGlycan.Sort(delegate(MatchedGlycanPeak Peak1, MatchedGlycanPeak Peak2)
                {
                    return Peak1.ScanTime.CompareTo(Peak2.ScanTime);
                });


                Dictionary<double, double> MergeIntensity = new Dictionary<double, double>();
                List<double> Time = new List<double>();
                foreach (MatchedGlycanPeak MGlycanPeak in AllPeaksWithSameGlycan)
                {

                    if (!MergeIntensity.ContainsKey(MGlycanPeak.ScanTime))
                    {
                        MergeIntensity.Add(MGlycanPeak.ScanTime, 0);
                    }
                    MergeIntensity[MGlycanPeak.ScanTime] = MergeIntensity[MGlycanPeak.ScanTime] + MGlycanPeak.Peak.MonoIntensity;

                    if (!Time.Contains(MGlycanPeak.ScanTime))
                    {
                        Time.Add(MGlycanPeak.ScanTime);
                    }
                }

                //Merge Intensity
                Time.Sort();
                double[] ArryIntesity = new double[Time.Count];
                double[] ArryTime = Time.ToArray();
                for (int i = 0; i < Time.Count; i++)
                {
                    ArryIntesity[i] = MergeIntensity[Time[i]];
                }


                List<double[]> PeaksTime = new List<double[]>();
                List<double[]> PeaksIntensity = new List<double[]>();

                do
                {
                    //Iter to find peak
                    int MaxIdx = FindMaxIdx(ArryIntesity);
                    int PeakStart = MaxIdx ;
                    int PeakEnd = MaxIdx;
                    //PeakStartPoint
                    while (PeakStart > 0)
                    {
                        //0.5  Two MS scan Max Interval
                        if (ArryTime[PeakStart] - ArryTime[PeakStart - 1] < 0.5 && ArryTime[MaxIdx] - ArryTime[PeakStart] < _maxLCFrontMin)
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
                        if (ArryTime[PeakEnd + 1] - ArryTime[PeakEnd] < 0.5 && ArryTime[PeakEnd] - ArryTime[MaxIdx] < _maxLCBackMin)
                        {
                            PeakEnd = PeakEnd + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    //Peak Array
                    double[] PeakTime = new double[PeakEnd - PeakStart + 1];
                    double[] PeakInt = new double[PeakEnd - PeakStart + 1];
                    Array.Copy(ArryTime, PeakStart, PeakTime, 0, PeakEnd - PeakStart + 1);
                    Array.Copy(ArryIntesity, PeakStart, PeakInt, 0, PeakEnd - PeakStart + 1);
                    //Store Peaks
                    PeaksTime.Add(PeakTime);
                    PeaksIntensity.Add(PeakInt);


                    //MergeRest
                    int SizeOfRestArray = ArryTime.Length - PeakEnd + PeakStart - 1;
                    double[] NewArryTime = new double[SizeOfRestArray];
                    double[] NewArryIntensity = new double[SizeOfRestArray];
                    Array.Copy(ArryTime, 0, NewArryTime, 0, PeakStart);
                    Array.Copy(ArryTime, PeakEnd + 1, NewArryTime, PeakStart, ArryTime.Length - 1 - PeakEnd);
                    Array.Copy(ArryIntesity, 0, NewArryIntensity, 0, PeakStart);
                    Array.Copy(ArryIntesity, PeakEnd + 1, NewArryIntensity, PeakStart, ArryTime.Length - 1 - PeakEnd);

                    ArryTime = NewArryTime;
                    ArryIntesity = NewArryIntensity;

                } while (ArryTime.Length != 0);

                List<ClusteredPeak> MergedPeaks = new List<ClusteredPeak>();
                for (int i = 0; i < PeaksTime.Count; i++)
                {
                    ClusteredPeak MergedPeak = new ClusteredPeak();
                    

                   for (int j = 0; j < PeaksTime[i].Length;j++)
                    {
                        MergedPeak.MatchedPeaksInScan.AddRange(FindGlycanIdxInGlycanList(AllPeaksWithSameGlycan,PeaksTime[i][j]));
                    }


                    if (MergedPeak.MonoIntensity >= _minAbundance &&
                         MergedPeak.TimeInterval >= _minLengthOfLC)
                    {
                        MergedPeaks.Add(MergedPeak);
                    }
                }
                _MergedResultList.AddRange(MergedPeaks);
                //ExportGlycanToExcel(KEY, AllPeaksWithSameGlycan, MergedPeaks);
            }
        
        }
        private List<MatchedGlycanPeak> FindGlycanIdxInGlycanList(List<MatchedGlycanPeak> argMatchGlycans, double argTime)
        {
            List<MatchedGlycanPeak> MatchedPeaks = new List<MatchedGlycanPeak>();
            for (int i = 0; i < argMatchGlycans.Count; i++)
            {
                if (argMatchGlycans[i].ScanTime == argTime )
                {
                    MatchedPeaks.Add(argMatchGlycans[i]);
                }
            }
            return MatchedPeaks;
        }
        private int FindMaxIdx(double[] argArry)
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
        public void Merge2PassedCluster()
        {
            if (_2ndPassedPeaksInScan == null)
            {
                return;
            }
            _MergedResultList = new List<ClusteredPeak>();  //Store Result
            Dictionary<string, List<MatchedGlycanPeak>> dictAllPeak = new Dictionary<string, List<MatchedGlycanPeak>>();  //KEY: GlycanKey ot GlycanKey+Charge
             List<string> GlycanWProton = new List<string>(); //Store Glycan with Proton adduct
            _MatchedPeaksInScan.AddRange(_2ndPassedPeaksInScan);

            for (int i = 0; i < _MatchedPeaksInScan.Count; i++)
            {
                string key = "";
                if (_MergeDifferentCharge)
                {
                    key = _MatchedPeaksInScan[i].GlycanKey;
                }
                else
                {
                    key = _MatchedPeaksInScan[i].GlycanKey + "-" +
                                     _MatchedPeaksInScan[i].Charge.ToString();
                }
                if (!dictAllPeak.ContainsKey(key))
                {
                    dictAllPeak.Add(key, new List<MatchedGlycanPeak>());
                }
                dictAllPeak[key].Add(_MatchedPeaksInScan[i]);
                if (_MatchedPeaksInScan[i].Adduct == "H" && !GlycanWProton.Contains(key))
                {
                    GlycanWProton.Add(key);
                }
            }
            foreach (string KEY in dictAllPeak.Keys)
            {
                if (!GlycanWProton.Contains(KEY))  //Skip identified glycans without Proton adduct;
                {
                    continue;
                }
                List<MatchedGlycanPeak> AllPeaksWithSameGlycan = dictAllPeak[KEY];
               
                //if (AllPeaksWithSameGlycan[AllPeaksWithSameGlycan.Count - 1].ScanTime - AllPeaksWithSameGlycan[0].ScanTime <= _maxLCFrontMin)
                //{   //All peaks within duration
                    //mergedPeak = (ClusteredPeak)CLSPeaks[0].Clone();
                  
                    //Sum up intensity
                    Dictionary<double, double> SumIntensity = new Dictionary<double, double>();
                    foreach (MatchedGlycanPeak MatchedPeak in AllPeaksWithSameGlycan)
                    {
                        if (!SumIntensity.ContainsKey(MatchedPeak.ScanTime))
                        {
                            SumIntensity.Add(MatchedPeak.ScanTime,0.0f);
                        }
                        SumIntensity[MatchedPeak.ScanTime] = SumIntensity[MatchedPeak.ScanTime] + MatchedPeak.MostIntenseIntensity;
                    }
                    List<MSPoint> lstMSPs = new List<MSPoint>();
                    foreach (double time in SumIntensity.Keys)
                    {
                        if (Convert.ToSingle(SumIntensity[time]) != 0)
                        {
                            lstMSPs.Add(new MSPoint(Convert.ToSingle(time), Convert.ToSingle(SumIntensity[time])));
                        }
                    }
                    lstMSPs.Sort(delegate(MSPoint p1, MSPoint p2) { return p1.Mass.CompareTo(p2.Mass); });

                    //Smooth
                    List<MassLib.MSPoint> lstSmoothPnts = new List<MassLib.MSPoint>();
                    lstSmoothPnts = MassLib.Smooth.SavitzkyGolay.Smooth(lstMSPs, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);

                    //Peak Finding
                    List<MassLib.LCPeak> lcPk = null;
                    lcPk = MassLib.LCPeakDetection.PeakFinding(lstSmoothPnts, 0.1f, 0.01f);

                    //Create Result Peak
                    for (int i = 0; i < lcPk.Count; i++)
                    {
                        ClusteredPeak MergedPeak = new ClusteredPeak();
                        MergedPeak.LCPeak = lcPk[i];
                        foreach (MatchedGlycanPeak ClusterPeak in AllPeaksWithSameGlycan)
                        {
                            if (ClusterPeak.ScanTime >= lcPk[i].StartTime && ClusterPeak.ScanTime <= lcPk[i].EndTime)
                            {
                                MergedPeak.MatchedPeaksInScan.Add(ClusterPeak);
                            }
                        }      
                        if (MergedPeak.IsotopicClusterIntensity > _minAbundance &&
                            MergedPeak.TimeInterval >= _maxLCBackMin &&
                             MergedPeak.TimeInterval < _maxLCFrontMin &&
                              MergedPeak.MatchedPeaksInScan.Count >= _minLengthOfLC)
                        {
                            _MergedResultList.Add(MergedPeak);
                        }
                    }
    
            }

        


            //_Merged2ndPassedResultList = new List<ClusteredPeak>();
            //Dictionary<string, List<ClusteredPeak>> dictClusteredPeak = new Dictionary<string, List<ClusteredPeak>>();
            ////Create Key
            //foreach (ClusteredPeak ClusteredPeak in _MergedResultList)
            //{
            //    string key = ClusteredPeak.GlycanKey;
            //    if (!_MergeDifferentCharge)
            //    {
            //        key = ClusteredPeak.GlycanKey + "-" +
            //                         ClusteredPeak.MatchedPeaksInScan[0].Charge.ToString();
            //    }
            //    if (!dictClusteredPeak.ContainsKey(key))
            //    {
            //        dictClusteredPeak.Add(key, new List<ClusteredPeak>());
            //    }
            //    dictClusteredPeak[key].Add(ClusteredPeak);
            //}
            //Dictionary<string, List<MatchedGlycanPeak>> dict2ndPassedPeaks = new Dictionary<string, List<MatchedGlycanPeak>>();


            //foreach (MatchedGlycanPeak MatchedPeaks in _2ndPassedPeaksInScan)
            //{
            //    string key = MatchedPeaks.GlycanKey;
            //    if (!_MergeDifferentCharge)
            //    {
            //        key = MatchedPeaks.GlycanKey + "-" +
            //                         MatchedPeaks.Charge.ToString();
            //    }
            //    if (!dict2ndPassedPeaks.ContainsKey(key))
            //    {
            //        dict2ndPassedPeaks.Add(key, new List<MatchedGlycanPeak>());
            //    }
            //    dict2ndPassedPeaks[key].Add(MatchedPeaks);
            //}
            //foreach (string key in dict2ndPassedPeaks.Keys)
            //{
            //    List<MatchedGlycanPeak> MGPeaks = dict2ndPassedPeaks[key];

            //    if (!dictClusteredPeak.ContainsKey(key))
            //    {
            //        //New Glycan
            //        Dictionary<double, double> SumIntensity = new Dictionary<double, double>();
            //        foreach (MatchedGlycanPeak MatchedPeak in MGPeaks)
            //        {
            //            if (!SumIntensity.ContainsKey(MatchedPeak.ScanTime))
            //            {
            //                SumIntensity.Add(MatchedPeak.ScanTime, 0.0f);
            //            }
            //            SumIntensity[MatchedPeak.ScanTime] = SumIntensity[MatchedPeak.ScanTime] + MatchedPeak.Peak.MonoIntensity;
            //        }
            //        List<MSPoint> lstMSPs = new List<MSPoint>();
            //        foreach (double time in SumIntensity.Keys)
            //        {
            //            lstMSPs.Add(new MSPoint(Convert.ToSingle(time), Convert.ToSingle(SumIntensity[time])));
            //        }
            //        lstMSPs.Sort(delegate(MSPoint p1, MSPoint p2) { return p1.Mass.CompareTo(p2.Mass); });

            //        //Smooth
            //        List<MassLib.MSPoint> lstSmoothPnts = new List<MassLib.MSPoint>();
            //        lstSmoothPnts = MassLib.Smooth.SavitzkyGolay.Smooth(lstMSPs, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);

            //        //Peak Finding
            //        List<MassLib.LCPeak> lcPk = null;
            //        lcPk = MassLib.LCPeakDetection.PeakFinding(lstSmoothPnts, 0.1f, 0.01f);
            //        lcPk.Sort(delegate(LCPeak p1, LCPeak p2) { return p1.StartTime.CompareTo(p2.StartTime); });


            //        ClusteredPeak tmpMergedPeak = new ClusteredPeak(MGPeaks[0]);
            //        if (lcPk.Count > 1)
            //        {
            //            tmpMergedPeak.LCPeak = lcPk[0];
            //            for (int i = 1; i < lcPk.Count; i++)
            //            {
            //                if (lcPk[i].StartTime - tmpMergedPeak.LCPeak.EndTime <= 2.5)
            //                {
            //                    tmpMergedPeak.LCPeak = MergeLCPeak(tmpMergedPeak.LCPeak, lcPk[i]);
            //                }
            //                else
            //                {
            //                    foreach (MatchedGlycanPeak ClusterPeak in MGPeaks)
            //                    {
            //                        if (ClusterPeak.ScanTime >= tmpMergedPeak.StartTime && ClusterPeak.ScanTime <= tmpMergedPeak.EndTime)
            //                        {
            //                            tmpMergedPeak.MatchedPeaksInScan.Add(ClusterPeak);
            //                        }
            //                    }
                                

            //                    _MergedResultList.Add(tmpMergedPeak);
                                

            //                    tmpMergedPeak = new ClusteredPeak();
            //                    tmpMergedPeak.LCPeak = lcPk[i];
                                
            //                }
       
            //            }
            //            foreach (MatchedGlycanPeak ClusterPeak in MGPeaks)
            //            {
            //                if (ClusterPeak.ScanTime >= tmpMergedPeak.StartTime && ClusterPeak.ScanTime <= tmpMergedPeak.EndTime)
            //                {
            //                    tmpMergedPeak.MatchedPeaksInScan.Add(ClusterPeak);
            //                }
            //            }
            //            _MergedResultList.Add(tmpMergedPeak);
            //        }
            //        ////Create Result Peak
            //        //for (int i = 0; i < lcPk.Count; i++)
            //        //{
            //        //    ClusteredPeak MergedPeak = new ClusteredPeak();
            //        //    MergedPeak.LCPeak = lcPk[i];
            //        //    foreach (MatchedGlycanPeak ClusterPeak in MGPeaks)
            //        //    {
            //        //        if (ClusterPeak.ScanTime >= lcPk[i].StartTime && ClusterPeak.ScanTime <= lcPk[i].EndTime)
            //        //        {
            //        //            MergedPeak.MatchedPeaksInScan.Add(ClusterPeak);
            //        //        }
            //        //    }
            //        //    if (MergedPeak.IsotopicClusterIntensity > _minAbundance &&
            //        //        MergedPeak.TimeInterval >= _maxLCBackMin &&
            //        //         MergedPeak.TimeInterval < _maxLCFrontMin &&
            //        //          MergedPeak.MatchedPeaksInScan.Count >= _minLengthOfLC)
            //        //    {
            //        //        _MergedResultList.Add(MergedPeak);
            //        //    }
            //        //}

            //    }
            //    else
            //    {
            //        //List<int> MergeStartAndEndIdx = new List<int>();
            //        List<float> MGPeaksTime = new List<float>();
            //        List<int> MGPeaksFirstIdx = new List<int>();
            //        for (int i = 0; i < MGPeaks.Count; i++)
            //        {
            //            if (MGPeaksTime.Count != 0)
            //            {
            //                if (Convert.ToSingle(Math.Round(MGPeaks[i].ScanTime, 5)) != MGPeaksTime[MGPeaksTime.Count - 1])
            //                {
            //                    MGPeaksTime.Add(Convert.ToSingle(Math.Round(MGPeaks[i].ScanTime, 5)));
            //                    MGPeaksFirstIdx.Add(i);
            //                }
            //            }
            //            else
            //            {
            //                MGPeaksTime.Add(Convert.ToSingle(Math.Round(MGPeaks[i].ScanTime, 5)));
            //                MGPeaksFirstIdx.Add(i);
            //            }
            //        }
                    
            //        List<ClusteredPeak> TargetPeaks = dictClusteredPeak[key];
            //        List<ClusteredPeak> MergedPeaks = new List<ClusteredPeak>();
            //        for (int i = 0; i < TargetPeaks.Count; i++)
            //        {
            //            double StartTime = TargetPeaks[i].StartTime;
            //            //ExtendFront
            //            int ExtendFrontEndIdx = 0;
            //            for (int j = 0; j < MGPeaksTime.Count; j++)
            //            {
            //                if (StartTime - MGPeaksTime[j] < 0)
            //                {
            //                    ExtendFrontEndIdx = j-1;
            //                    break;
            //                }
            //            }
            //            int ExtendFrontStartIdx = ExtendFrontEndIdx;
            //            if (ExtendFrontEndIdx <= 0)
            //            {
            //                ExtendFrontEndIdx = 0;
            //                ExtendFrontStartIdx = 0;
            //            }
            //            else
            //            {
            //                while (true)
            //                {
            //                    if (ExtendFrontStartIdx == 0)
            //                    {
            //                        ExtendFrontStartIdx = 0;
            //                        break;
            //                    }
            //                    if (MGPeaksTime[ExtendFrontStartIdx] - MGPeaksTime[ExtendFrontStartIdx - 1] < 0.25)
            //                    {
            //                        ExtendFrontStartIdx = ExtendFrontStartIdx - 1;
            //                    }
            //                    else
            //                    {
            //                        break;
            //                    }
            //                }
            //            }
            //            //ExtendBack
            //            double EndTime = TargetPeaks[i].EndTime;
            //            int ExtendBackStartIdx = 0;
            //            for (int j = 0; j < MGPeaksTime.Count; j++)
            //            {
            //                if (EndTime - MGPeaksTime[j] <= 0)
            //                {
            //                    ExtendBackStartIdx = j;
            //                    break;
            //                }
            //            }
            //            int ExtendBackEndIdx = ExtendBackStartIdx;
            //            while (ExtendBackEndIdx<MGPeaksTime.Count)
            //            {
            //                if (ExtendBackEndIdx == MGPeaksTime.Count-1)
            //                {
            //                    ExtendBackEndIdx = MGPeaksTime.Count - 1;
            //                    break;
            //                }
            //                if (MGPeaksTime[ExtendBackEndIdx + 1] - MGPeaksTime[ExtendBackEndIdx] < 0.25)
            //                {
            //                    ExtendBackEndIdx = ExtendBackEndIdx + 1;
           
            //                }
            //                else
            //                {
            //                    break;
            //                }
            //            }
            //            //Check
            //            if (i > 0) //Check Front
            //            {
            //                double ExtendStartTime = MGPeaksTime[ExtendFrontStartIdx];
            //                if (MergedPeaks[i- 1].EndTime - ExtendStartTime >= 0)
            //                {
            //                    //Revice ExtendStartIndex;
            //                    do
            //                    {
            //                        ExtendFrontStartIdx = ExtendFrontStartIdx + 1;
            //                        if (MGPeaksTime[ExtendFrontStartIdx] - MergedPeaks[i - 1].EndTime > 0)
            //                        {
            //                            break;
            //                        } 
            //                    } while (true);
            //                }
            //            }
            //            if (i != TargetPeaks.Count - 1) //Check Back
            //            {
            //                double ExtendEndTime = MGPeaksTime[ExtendBackEndIdx];
            //                if (ExtendEndTime - TargetPeaks[i + 1].StartTime >= 0)
            //                {
            //                    //Revice ExtendBackEndIndex;
            //                    do
            //                    {
            //                        ExtendBackEndIdx = ExtendBackEndIdx - 1;
            //                        if (TargetPeaks[i + 1].StartTime - MGPeaksTime[ExtendBackEndIdx] > 0)
            //                        {
            //                            break;
            //                        }
            //                    } while (true);
            //                }
            //            }
            //            //CreateNewPeaks
            //            ClusteredPeak NewClustered = new ClusteredPeak();
            //            //Add front peak
            //            for (int j = 0; j < MGPeaks.Count; j++)
            //            {
            //                if (MGPeaksTime[ExtendFrontStartIdx] < MGPeaks[j].ScanTime && MGPeaks[j].ScanTime < MGPeaksTime[ExtendFrontEndIdx])
            //                {
            //                    NewClustered.MatchedPeaksInScan.Add(MGPeaks[j]);
            //                }
            //                if (MGPeaks[j].ScanTime > MGPeaksTime[ExtendFrontEndIdx])
            //                {
            //                    break;
            //                }
            //            }
            //            //Add original peak
            //            NewClustered.MatchedPeaksInScan.AddRange(TargetPeaks[i].MatchedPeaksInScan);
            //            //Add back peak
            //            for (int j = 0; j < MGPeaks.Count; j++)
            //            {
            //                if (MGPeaksTime[ExtendBackStartIdx] <= MGPeaks[j].ScanTime && MGPeaks[j].ScanTime <= MGPeaksTime[ExtendBackEndIdx])
            //                {
            //                    NewClustered.MatchedPeaksInScan.Add(MGPeaks[j]);
            //                }
            //                if (MGPeaks[j].ScanTime > MGPeaksTime[ExtendBackEndIdx])
            //                {
            //                    break;
            //                }
            //            }
            //            MergedPeaks.Add(NewClustered);
            //        }
            //        //Merger Clusted Peak
            //        List<ClusteredPeak> MergedMergedPeak = new List<ClusteredPeak>();
            //        while(MergedPeaks.Count!=0)
            //        {
            //            int LastIdx = MergedPeaks.Count - 1;
                        
            //            if (MergedPeaks.Count > 1)
            //            {
            //                if (MergedPeaks[LastIdx].StartTime - MergedPeaks[LastIdx - 1].EndTime < 0.25)
            //                {
            //                    //Merge
            //                    MergedPeaks[LastIdx - 1].MatchedPeaksInScan.AddRange(MergedPeaks[LastIdx].MatchedPeaksInScan);
            //                    MergedPeaks.RemoveAt(LastIdx);
            //                }
            //                else //move to previous clusterpeak
            //                {
            //                    MergedMergedPeak.Add(MergedPeaks[LastIdx]);
            //                    MergedPeaks.RemoveAt(LastIdx);
            //                }
            //            }
            //            else
            //            {
            //                MergedMergedPeak.Add(MergedPeaks[0]);
            //                MergedPeaks.RemoveAt(0);
            //            }

            //        }
            //        MergedMergedPeak.Reverse();
            //        _Merged2ndPassedResultList.AddRange(MergedMergedPeak);
            //    }
            //}

        }
        private LCPeak MergeLCPeak(LCPeak Pk1, LCPeak PK2)
        {
            LCPeak First = Pk1;
            LCPeak Second = PK2;
            if (Pk1.StartTime > PK2.StartTime)
            {
                First = PK2;
                Second = Pk1;
            }
            LCPeak Merged = First;
            Merged.RawPoint.AddRange(Second.RawPoint);
            return Merged;
        }
        private void UpdateLCPeak(ref ClusteredPeak argClusterPeak)
        {
            Dictionary<double, double> dicMSPs = new Dictionary<double, double>();
            List<MSPoint> lstMSPs = new List<MSPoint>();
            foreach (MatchedGlycanPeak MatchedPeak in argClusterPeak.MatchedPeaksInScan)
            {
                if (dicMSPs.ContainsKey(MatchedPeak.ScanTime))
                {
                    dicMSPs[MatchedPeak.ScanTime] = dicMSPs[MatchedPeak.ScanTime] + MatchedPeak.Peak.MonoIntensity;
                }
                else
                {

                    dicMSPs.Add(MatchedPeak.ScanTime, MatchedPeak.Peak.MonoIntensity);
                }
            }
    
            foreach (double key in dicMSPs.Keys)
            {
                lstMSPs.Add(new MSPoint( Convert.ToSingle(key), Convert.ToSingle(dicMSPs[key])));
            }
            lstMSPs.Sort(delegate(MSPoint p1, MSPoint p2) { return p1.Mass.CompareTo(p2.Mass); });

            //Smooth
            List<MassLib.MSPoint> lstSmoothPnts = new List<MassLib.MSPoint>();
            lstSmoothPnts = MassLib.Smooth.SavitzkyGolay.Smooth(lstMSPs, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);

            //Peak Finding
            List<MassLib.LCPeak> lcPk = null;
            lcPk = MassLib.LCPeakDetection.PeakFinding(lstSmoothPnts, 0.1f, 0.01f);
            MassLib.LCPeak MergedLCPeak = new LCPeak(lcPk[0].StartTime, lcPk[lcPk.Count - 1].EndTime, lcPk[0].RawPoint);
            if (lcPk.Count != 1)
            {
                for (int i = 1; i < lcPk.Count; i++)
                {  
                    MergedLCPeak.RawPoint.AddRange(lcPk[i].RawPoint);
                }
            }
            argClusterPeak.LCPeak = MergedLCPeak;

            //for (int i = 0; i < lcPk.Count; i++)
            //{
            //    ClusteredPeak MergedPeak = new ClusteredPeak();
            //    MergedPeak.LCPeak = lcPk[i];

            //    if (MergedPeak.IsotopicClusterIntensity > _minAbundance &&
            //        MergedPeak.TimeInterval >= _maxLCBackMin &&
            //         MergedPeak.TimeInterval < _maxLCFrontMin &&
            //          MergedPeak.MatchedPeaksInScan.Count >= _minLengthOfLC)
            //    {
            //        argClusterPeak = MergedPeak;
            //    }
            //}
        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="argDurationMin"></param>
        ///// <returns></returns>
        //public static List<ClusteredPeak> MergeCluster(List<ClusteredPeak> argCLU, double argDurationMin)
        //{
        //    List<ClusteredPeak> MergedCluster = new List<ClusteredPeak>();
        //    List<ClusteredPeak> _cluPeaks = argCLU;
        //    Dictionary<string, List<ClusteredPeak>> dictAllPeak = new Dictionary<string, List<ClusteredPeak>>();
        //    Dictionary<string, double> dictPeakIntensityMax = new Dictionary<string, double>();
        //    for (int i = 0; i < _cluPeaks.Count; i++)
        //    {
        //        string key = _cluPeaks[i].GlycanCompostion.NoOfHexNAc.ToString() +"-"+
        //                            _cluPeaks[i].GlycanCompostion.NoOfHex.ToString() + "-" +
        //                            _cluPeaks[i].GlycanCompostion.NoOfDeHex.ToString() + "-" +
        //                            _cluPeaks[i].GlycanCompostion.NoOfSia.ToString() + "-" +
        //                            _cluPeaks[i].Charge.ToString();
        //        if (!dictAllPeak.ContainsKey(key))
        //        {
        //            dictAllPeak.Add(key, new List<ClusteredPeak>());
        //            dictPeakIntensityMax.Add(key, _cluPeaks[i].Intensity);
        //        }
        //        dictAllPeak[key].Add(_cluPeaks[i]);
        //        if (_cluPeaks[i].Intensity > dictPeakIntensityMax[key])
        //        {
        //            dictPeakIntensityMax[key] = _cluPeaks[i].Intensity;
        //        }
        //    }

        //    foreach (string KEY in dictAllPeak.Keys)
        //    {
        //        List<ClusteredPeak> CLSPeaks = dictAllPeak[KEY];
        //        double threshold = Math.Sqrt(dictPeakIntensityMax[KEY]);
        //        ClusteredPeak mergedPeak =null;
        //        for(int i =0 ; i< CLSPeaks.Count;i++)
        //        {
        //            if (CLSPeaks[i].Intensity < threshold)
        //            {
        //                continue;
        //            }
        //            if (mergedPeak == null)
        //            {
        //                mergedPeak = (ClusteredPeak)CLSPeaks[i].Clone();
        //                mergedPeak.MergedIntensity = CLSPeaks[i].Intensity;
        //                continue;
        //            }
        //            if (CLSPeaks[i].StartTime - mergedPeak.EndTime < 1.0)
        //            {
        //                mergedPeak.EndTime = CLSPeaks[i].StartTime;
        //                mergedPeak.EndScan = CLSPeaks[i].StartScan;
        //                mergedPeak.MergedIntensity = mergedPeak.MergedIntensity + CLSPeaks[i].Intensity;
        //            }
        //            else
        //            {
        //                MergedCluster.Add(mergedPeak);
        //                mergedPeak = (ClusteredPeak)CLSPeaks[i].Clone();
        //                mergedPeak.MergedIntensity = CLSPeaks[i].Intensity;
        //            }
        //        }
        //        if (MergedCluster[MergedCluster.Count - 1] != mergedPeak)
        //        {
        //            MergedCluster.Add(mergedPeak);
        //        }
        //    } 
        //    return MergedCluster;
        //}
        public static double GetMassPPM(double argExactMass, double argMeasureMass)
        {
            return Math.Abs(Convert.ToDouble(((argMeasureMass - argExactMass) / argExactMass) * Math.Pow(10.0, 6.0)));
        }

        public void ReadGlycanList()
        {
            _GlycanList = new List<GlycanCompound>();
            StreamReader sr;
            //Assembly assembly = Assembly.GetExecutingAssembly();
            //sr = new StreamReader(assembly.GetManifestResourceStream( "MutliNGlycanFitControls.Properties.Resources.combinations.txt"));
            int LineNumber = 0;
            sr = new StreamReader(_glycanFile);

            string tmp; // temp line for processing
            tmp = sr.ReadLine();
            LineNumber++;
            Hashtable compindex = new Hashtable(); //Glycan Type index.
            
            //Read the title
            string[] splittmp = tmp.Trim().Split(',');
            if (tmp.ToLower().Contains("order"))
            {
                _GlycanListContainLCOrder = true;
            }
            try
            {
                for (int i = 0; i < splittmp.Length; i++)
                {
                    if (splittmp[i].ToLower() == "neunac" || splittmp[i].ToLower() == "neungc" || splittmp[i].ToLower() == "sialic")
                    {
                        compindex.Add("sia", i);
                        continue;
                    }
                    if (splittmp[i].ToLower() != "hexnac" && splittmp[i].ToLower() != "hex" && splittmp[i].ToLower() != "dehex" && splittmp[i].ToLower() != "sia"  && splittmp[i].ToLower()!="order")
                    {
                        throw new Exception("Glycan list file title error. (Use:HexNAc,Hex,DeHex,Sia,NeuNAc,NeuNGc,Order)");
                    }
                    compindex.Add(splittmp[i].ToLower(), i);
                }
            }
            catch (Exception ex)
            {
                sr.Close();
                throw ex;
            }
            int processed_count = 0;

            //Read the list    
            try
            {
                do
                {
                    tmp = sr.ReadLine();
                    LineNumber++;
                    splittmp = tmp.Trim().Split(',');
                    GlycanCompound GC = new GlycanCompound(Convert.ToInt32(splittmp[(int)compindex["hexnac"]]),
                                             Convert.ToInt32(splittmp[(int)compindex["hex"]]),
                                             Convert.ToInt32(splittmp[(int)compindex["dehex"]]),
                                             Convert.ToInt32(splittmp[(int)compindex["sia"]]),
                                             _isPermethylated,
                                             false,
                                             _isReducedReducingEnd,
                                             false,
                                             true,
                                             true);
                    _GlycanList.Add(GC);
                    if (splittmp.Length == 5 && splittmp[(int)compindex["order"]]!="") // Contain order
                    {
                        _GlycanList[_GlycanList.Count - 1].GlycanLCorder = Convert.ToInt32(splittmp[(int)compindex["order"]]);
                    }
                    processed_count++;
                } while (!sr.EndOfStream);
            }
            catch (Exception ex)
            {
                throw new Exception("Glycan list file reading error on Line:" + LineNumber + ". Please check input file. (" + ex.Message + ")");
            }
            finally
            {
                sr.Close();
            }

            if (_GlycanList.Count == 0)
            {
                throw new Exception("Glycan list file reading error. Please check input file.");
            }
            _GlycanList.Sort();
        }
    }
}
