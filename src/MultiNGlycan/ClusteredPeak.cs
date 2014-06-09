﻿using System;
using System.Collections.Generic;
using System.Text;
using COL.MassLib;
using COL.GlycoLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace COL.MultiGlycan
{
    //[Serializable]
    public class ClusteredPeak// : ICloneable
    {
        private List<MatchedGlycanPeak> _MatchedPeaksInScan;
        private LCPeak _LCPeak;
        private List<MSPoint> _RawPoint;
        //private List<MSPeak> _MSPeak;
        //private double _StatrTime;
        //private double _EndTime;
        //private int _StartScan;
        //private int _EndScan;
        //private double _charge;
        //private GlycanCompound _glycanComposition;
        //private double _MergedIntensity;
        //private string _adduct;
        public ClusteredPeak()
        {
            _MatchedPeaksInScan = new List<MatchedGlycanPeak>();
        }
        public ClusteredPeak(MatchedGlycanPeak argMatchedGlycanPeak)
        {
            _MatchedPeaksInScan = new List<MatchedGlycanPeak>();
            _MatchedPeaksInScan.Add(argMatchedGlycanPeak);
        }
        public List<MatchedGlycanPeak> MatchedPeaksInScan
        {
            get { return _MatchedPeaksInScan; }
            set { _MatchedPeaksInScan = value; }
        }
        public LCPeak LCPeak
        {
            get { return _LCPeak; }
            set { _LCPeak = value; }
        }
        /// <summary>
        /// Sum of Intensity Value: All isotoped peaks are included.
        /// </summary>
        public double IsotopicClusterIntensity
        {
            get
            {
                double _IsotopicClusterIntensity = 0.0f;
                foreach (MatchedGlycanPeak P in _MatchedPeaksInScan)
                {
                    _IsotopicClusterIntensity = _IsotopicClusterIntensity + P.Peak.ClusterIntensity;
                }                
                return _IsotopicClusterIntensity;
            }
        }
        /// <summary>
        /// Sum of Intensity Value: The most intense isotoped peak is included only.
        /// </summary>
        public double MostIntenseIntensity
        {
            get
            {
                if (_LCPeak != null)
                {
                    return _LCPeak.SumOfIntensity;
                }
                else
                {
                    double _MostIntenseIntensity = 0;
                    foreach (MatchedGlycanPeak P in _MatchedPeaksInScan)
                    {
                        _MostIntenseIntensity = _MostIntenseIntensity + P.Peak.MostIntenseIntensity;
                    }
                    return _MostIntenseIntensity;
                }
            }
        }
        public double MonoIntensity
        {
            get
            {
                double MonotIntenseIntensity = 0;
                foreach (MatchedGlycanPeak P in _MatchedPeaksInScan)
                {
                    MonotIntenseIntensity = MonotIntenseIntensity + P.Peak.MonoIntensity;
                }
                return MonotIntenseIntensity;
            }
        }
        //public double MergedIntensity
        //{
        //    get { return _MergedIntensity; }
        //    set { _MergedIntensity = value; }
        //}
        public double StartTime
        {
            get
            {
                
                return Math.Round(_MatchedPeaksInScan[0].ScanTime,5);
            }  
        }
        public double EndTime
        {
            get
            {
         
                return Math.Round( _MatchedPeaksInScan[_MatchedPeaksInScan.Count-1].ScanTime,5);
            }
        }
        public void CalcLCPeak()
        {
            List<MSPoint> lstMSPs = new List<MSPoint>();
            foreach (MatchedGlycanPeak Peak in _MatchedPeaksInScan)
            {
                lstMSPs.Add(new MSPoint(Convert.ToSingle(Math.Round(Peak.ScanTime,5)), Peak.Peak.MonoIntensity));
            }
            lstMSPs.Sort(delegate(MSPoint p1, MSPoint p2) { return p1.Mass.CompareTo(p2.Mass); });


            
                List<MassLib.MSPoint> lstSmoothPnts = new List<MassLib.MSPoint>();
                lstSmoothPnts = MassLib.Smooth.SavitzkyGolay.Smooth(lstMSPs, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);

                //Peak Finding
                List<MassLib.LCPeak> lcPk = null;
                lcPk = MassLib.LCPeakDetection.PeakFinding(lstSmoothPnts, 0.1f, 0.01f);
                MassLib.LCPeak MergedLCPeak = null;
                if (lcPk.Count > 0)
                {
                    MergedLCPeak = new LCPeak(lcPk[0].StartTime, lcPk[lcPk.Count - 1].EndTime, lcPk[0].RawPoint);
                    if (lcPk.Count > 1)
                    {
                        for (int i = 1; i < lcPk.Count; i++)
                        {
                            MergedLCPeak.RawPoint.AddRange(lcPk[i].RawPoint);
                        }
                    }                  
                }
                else
                {
                    MergedLCPeak = new LCPeak(lstSmoothPnts[0].Mass,lstSmoothPnts[lstSmoothPnts.Count-1].Mass, lstSmoothPnts);
                }
                _LCPeak = MergedLCPeak;
            
        }
        public int StartScan
        {
            get { return _MatchedPeaksInScan[0].ScanNum; }
        }
        public int EndScan
        {
            get { return _MatchedPeaksInScan[_MatchedPeaksInScan.Count-1].ScanNum; }
        }
        //public double mz
        //{
        //    get { return _MSPeak[0].MonoisotopicMZ; }
        //}
        //public double Charge
        //{
        //    get {return _charge;}
        //}
        //public List<MSPeak> Peaks
        //{
        //    get { return _MSPeak; }
        //    set { _MSPeak = value; }
        //}
        //public double ClusterMono
        //{
        //    get { return _MSPeak[0].MonoMass; }
        //}
        public GlycanCompound GlycanComposition
        {
            get { return _MatchedPeaksInScan[0].GlycanComposition; }
        }
        public string GlycanKey
        {
            get
            {
                return _MatchedPeaksInScan[0].GlycanComposition.NoOfHexNAc.ToString() + "-" +
                              _MatchedPeaksInScan[0].GlycanComposition.NoOfHex.ToString() + "-" +
                              _MatchedPeaksInScan[0].GlycanComposition.NoOfDeHex.ToString() + "-" +
                              _MatchedPeaksInScan[0].GlycanComposition.NoOfSia.ToString();
            }
        }
        public double TimeInterval
        {
            get { return _MatchedPeaksInScan[_MatchedPeaksInScan.Count - 1].ScanTime - _MatchedPeaksInScan[0].ScanTime; }
        }
        public double PeakArea
        {            
            get {
                CalcLCPeak();
                return _LCPeak.PeakArea;
              
            }
        }

        //private double CalculatePeakArea()
        //{
        //    double peakArea = 0.0;
        //    Dictionary<string, double> PeakAreaByAdduct = new Dictionary<string, double>();
        //    Dictionary<string, string> Mz2Adduct = new Dictionary<string, string>();
        //    Dictionary<string, List<MatchedGlycanPeak>> ClusterByMZ = new Dictionary<string, List<MatchedGlycanPeak>>();
        //    foreach (MatchedGlycanPeak MatchedPeak in _MatchedPeaksInScan)
        //    {
        //        string MatchedMonoKey = MatchedPeak.Peak.MonoMass.ToString("0.00");
        //        if (!ClusterByMZ.ContainsKey(MatchedMonoKey))
        //        {
        //            ClusterByMZ.Add(MatchedMonoKey, new List<MatchedGlycanPeak>());
        //            Mz2Adduct.Add(MatchedMonoKey, MatchedPeak.Adduct);
        //        }
        //        ClusterByMZ[MatchedMonoKey].Add(MatchedPeak);                    
        //    }
        //    foreach (string MonoKey in ClusterByMZ.Keys)
        //    {                
        //        List<MSPeak> Peaks = new List<MSPeak>();
        //        foreach (MatchedGlycanPeak GPeak in ClusterByMZ[MonoKey])
        //        {
        //            //ConvertTo MSPeak    
        //            Peaks.Add(new MSPeak(Convert.ToSingle(GPeak.ScanTime),Convert.ToSingle(GPeak.Peak.MonoIntensity)));
        //        }
        //        List<MSPeak> SmoothedPeaks = MassLib.Smooth.SavitzkyGolay.Smooth(Peaks, MassLib.Smooth.SavitzkyGolay.FILTER_WIDTH.FILTER_WIDTH_7);
        //        double CalcedPeakArea = CalcPeakArea(SmoothedPeaks);
        //        if (!PeakAreaByAdduct.ContainsKey(Mz2Adduct[MonoKey]))
        //        {
        //            PeakAreaByAdduct.Add(Mz2Adduct[MonoKey], 0);
        //        }
        //        PeakAreaByAdduct[Mz2Adduct[MonoKey]] = PeakAreaByAdduct[Mz2Adduct[MonoKey]] + CalcedPeakArea;
        //        peakArea = peakArea + CalcedPeakArea;
        //    }
        //    return peakArea;
        //}
        //private double CalcPeakArea(List<MSPeak> argPeaks)
        //{
        //    double AreaOfCurve = 0.0;

        //    for (int i = 0; i < argPeaks.Count - 1; i++)
        //    {
        //        AreaOfCurve = AreaOfCurve + ((argPeaks[i + 1].MonoMass - argPeaks[i].MonoMass) * ((argPeaks[i + 1].MonoIntensity + argPeaks[i].MonoIntensity) / 2));

        //    }            
        //    return AreaOfCurve;
        //}
        //public object Clone() // ICloneable implementation
        //{
        //    ClusteredPeak ClusPeaksClone = new ClusteredPeak(this.StartScan);
        //        //ClusPeaksClone.MergedIntensity = this.MergedIntensity;
        //    ClusPeaksClone.Peaks = this.Peaks;
        //    return ClusPeaksClone;
        //}
        //public override bool Equals(object obj)
        //{
        //    //obj is null 
        //    if (obj == null)
        //    {
        //        return false;
        //    }
        //    if (obj is ClusteredPeak)
        //    {
        //        ClusteredPeak ClsPaek = obj as ClusteredPeak;

        //        if( ClsPaek._charge == this._charge &&
        //             ClsPaek._EndScan == this._EndScan &&
        //            ClsPaek._EndTime == this._EndTime &&
        //            ClsPaek._glycanComposition == this._glycanComposition &&
        //            //ClsPaek._MSPeak == this._MSPeak &&
        //            ClsPaek._StartScan == this._StartScan &&
        //            ClsPaek._StatrTime == this._StatrTime )
        //        {
        //            return true;
        //        }                
        //    }
        //    return false; 
        //}
        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
    }
}
