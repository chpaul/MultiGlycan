using System;
using System.Collections.Generic;
using System.Text;
using COL.MassLib;
using COL.GlycoLib;
namespace COL.MultiGlycan
{
    public class MatchedGlycanPeak
    {
        private MSPeak _MSPeak;
        private double _Time;
        private int _ScanNum;
        private double _charge;
        private GlycanCompound _glycanComposition;
        //private double _MergedIntensity;
        private string _adduct;
        private int _adductCount;
        public MatchedGlycanPeak(int argScanNum,double argTime, MSPeak argPeak, GlycanCompound argGlycanComp, string argAddcut, int argAdductCount)
        {
            _ScanNum = argScanNum;
            _Time = argTime;
            _MSPeak = argPeak;
            _glycanComposition = argGlycanComp;
            _adduct = argAddcut;
            _adductCount = argAdductCount;
        }
        public string Adduct
        {
            get { return _adduct; }
        }
        public int AdductCount
        {
            get { return _adductCount; }
        }
        public int Charge
        {
            get { return (int) _MSPeak.ChargeState; }
        }
        /// <summary>
        /// Sum of Intensity Value: All isotoped peaks are included.
        /// </summary>
        public double IsotopicClusterIntensity
        {
            get{ return _MSPeak.ClusterIntensity;}
        }
        /// <summary>
        /// Sum of Intensity Value: The most intens isotoped peak is included only.
        /// </summary>
        public double MostIntenseIntensity
        {
            get{return _MSPeak.MostIntenseIntensity;}
        }
        public double ScanTime
        {
            get { return _Time; }
        }
        public MSPeak Peak
        {
            get {return _MSPeak;}
        }
        public int ScanNum
        {
            get { return _ScanNum; }
        }
        public GlycanCompound GlycanComposition
        {
            get { return _glycanComposition; }
            set { _glycanComposition = value; }
        }  
        public string GlycanKey
        {
            get
            {
                return _glycanComposition.NoOfHexNAc.ToString() + "-" +
                              _glycanComposition.NoOfHex.ToString() + "-" +
                              _glycanComposition.NoOfDeHex.ToString() + "-" +
                              _glycanComposition.NoOfSia.ToString();
            }
        }
    }
}
