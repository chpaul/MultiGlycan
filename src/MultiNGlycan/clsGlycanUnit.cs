using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COL.MultiGlycan
{
    public class clsGlycanUnit
    {
        public int GU { get; set; }
        public double EluctionTime { get; set; }
        public clsGlycanUnit(int argGU, double argEluctionTime)
        {
            GU = argGU;
            EluctionTime = argEluctionTime;
        }
    }
}
