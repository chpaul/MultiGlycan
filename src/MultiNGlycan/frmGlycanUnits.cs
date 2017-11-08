using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace COL.MultiGlycan
{
    public partial class frmGlycanUnits : Form
    {
        List<clsGlycanUnit> lstGU;
        public frmGlycanUnits(ref List<clsGlycanUnit> argGUs)
        {
            InitializeComponent();
            lstGU = argGUs;

            dgvGU.Columns.Add("GU", "GU");
            dgvGU.Columns.Add("EluctionTime", "EluctionTime(mins)");
          
            if(lstGU.Count!=0)
            {
                foreach (clsGlycanUnit gu in lstGU.OrderBy(x => x.GU))
                    dgvGU.Rows.Add(gu.GU, gu.EluctionTime);              
            }
        }

        private void btnGUSave_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Overwrite GUs?", "Save GUs", MessageBoxButtons.YesNo)== DialogResult.Yes)
            {
                lstGU.Clear();
                foreach(DataGridViewRow row in dgvGU.Rows)
                {
                    if(Convert.ToInt32(row.Cells[0].Value)!=0)
                        lstGU.Add(new clsGlycanUnit(Convert.ToInt32(row.Cells[0].Value), 
                                                Convert.ToDouble(row.Cells[1].Value)));
                }
                this.Close();
            }
        }
    }
}
