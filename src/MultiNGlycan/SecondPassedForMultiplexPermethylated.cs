using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using COL.GlycoLib;
using COL.MassLib;

namespace COL.MultiGlycan
{
    public static class SecondPassedForMultiplexPermethylated
    {
        public static void Processing(ref MultiGlycanESI argMultiGlycan)
        {
            GlycanCompound processed;
            try
            {
                foreach (GlycanCompound g in argMultiGlycan.IdentifiedGlycanCompounds)
                {
                    processed = g;
                    //Search Peak in front of the peak 
                    ThermoRawReader raw = (ThermoRawReader)argMultiGlycan.RawReader;
                    List<int> identifedScans = argMultiGlycan.MatchedPeakInScan.Where(x => x.GlycanComposition == g).Select(y => y.ScanNum).ToList();
                    for (int i = 0; i < identifedScans.Count; i++)
                    {
                        int identifiedScanNum = identifedScans[i];
                        while (true)
                        {
                            identifiedScanNum -= 1;
                            if (identifiedScanNum < 1 || raw.GetRetentionTime(identifedScans[i]) - raw.GetRetentionTime(identifiedScanNum) > argMultiGlycan.MaxLCFrontMin)
                            {
                                break;
                            }
                            if (raw.GetMsLevel((identifiedScanNum)) != 1)
                            {
                                continue;
                            }
                            MSScan scan = raw.ReadScan(identifiedScanNum);
                            int PeakIdx = MassUtility.GetClosestMassIdx(scan.MZs, (float)g.MZ);
                            if (MassUtility.GetMassPPM(scan.MZs[PeakIdx], g.MZ) > argMultiGlycan.MassPPM)
                            {
                                continue;
                            }
                            //Find isotope cluster 
                            List<MSPoint> lstIsotopes = new List<MSPoint>();
                            lstIsotopes.Add(new MSPoint(scan.MZs[PeakIdx],scan.Intensities[PeakIdx]));
                            float targetMZ = (float)g.MZ;
                            do
                            {
                                targetMZ += 1.0f/g.Charge;
                                PeakIdx = MassUtility.GetClosestMassIdx(scan.MZs, targetMZ);
                                if (MassUtility.GetMassPPM(scan.MZs[PeakIdx], targetMZ) <= argMultiGlycan.MassPPM)
                                {
                                    lstIsotopes.Add(new MSPoint(scan.MZs[PeakIdx],scan.Intensities[PeakIdx]));
                                }
                                else
                                {
                                    break;
                                }
                            } while (true);
                            if (lstIsotopes.Count < argMultiGlycan.MininumIsotopePeakCount)
                            {
                                continue;
                            }
                            MatchedGlycanPeak mPeak = new MatchedGlycanPeak(scan.ScanNo, scan.Time, scan.MSPeaks[PeakIdx], g);
                            mPeak.MSPoints = lstIsotopes;
                            if (!argMultiGlycan.MatchedPeakInScan.Contains(mPeak))
                            {
                                argMultiGlycan.MatchedPeakInScan.Add(mPeak);
                            }
                        }
                        identifiedScanNum = identifedScans[i];
                        while (true)
                        {
                            identifiedScanNum += 1;
                            if (identifiedScanNum > raw.NumberOfScans || raw.GetRetentionTime(identifiedScanNum) - raw.GetRetentionTime(identifedScans[i]) > argMultiGlycan.MaxLCBackMin)
                            {
                                break;
                            }
                            if (raw.GetMsLevel((identifiedScanNum)) != 1)
                            {
                                continue;
                            }
                            MSScan scan = raw.ReadScan(identifiedScanNum);
                            int PeakIdx = MassUtility.GetClosestMassIdx(scan.MZs, (float)g.MZ);
                            if (MassUtility.GetMassPPM(scan.MZs[PeakIdx], g.MZ) > argMultiGlycan.MassPPM)
                            {
                                continue;
                            }
                            //Find isotope cluster 
                            List<MSPoint> lstIsotopes = new List<MSPoint>();
                            lstIsotopes.Add(new MSPoint(scan.MZs[PeakIdx], scan.Intensities[PeakIdx]));
                            float targetMZ = (float)g.MZ;
                            do
                            {
                                targetMZ += 1.0f / g.Charge;
                                PeakIdx = MassUtility.GetClosestMassIdx(scan.MZs, targetMZ);
                                if (MassUtility.GetMassPPM(scan.MZs[PeakIdx], targetMZ) <= argMultiGlycan.MassPPM)
                                {
                                    lstIsotopes.Add(new MSPoint(scan.MZs[PeakIdx], scan.Intensities[PeakIdx]));
                                }
                                else
                                {
                                    break;
                                }
                            } while (true);
                            if (lstIsotopes.Count < argMultiGlycan.MininumIsotopePeakCount)
                            {
                                continue;
                            }
                            MatchedGlycanPeak mPeak = new MatchedGlycanPeak(scan.ScanNo, scan.Time, scan.MSPeaks[PeakIdx], g);
                            mPeak.MSPoints = lstIsotopes;
                            if (!argMultiGlycan.MatchedPeakInScan.Contains(mPeak))
                            {
                                argMultiGlycan.MatchedPeakInScan.Add(mPeak);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
                throw ex;
            }
         
        }
    }
}
