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
using COL.GlycoLib;

namespace COL.MultiGlycan
{
    public partial class frmBatchGenImg : Form
    {
        private List<string> ResultFiles;
        private bool isIndividualImg;
        private bool isQuantImg;
        private int CompletedCount;
        private string currentFile;
        private string ExportFilePath;
        private enumGlycanLabelingMethod LabelingMethod;
        public frmBatchGenImg(List<string>  argResultFiles, string argExportRootPath, enumGlycanLabelingMethod argLabelingMethod, bool argIndividualImg, bool argQuantImg)
        {
            InitializeComponent();
            ResultFiles = argResultFiles;
            isIndividualImg = argIndividualImg;
            isQuantImg = argQuantImg;
            CompletedCount = 0;
            ExportFilePath = argExportRootPath;
            LabelingMethod = argLabelingMethod;
            bgWorkerGenerateImages.RunWorkerAsync();

        }

        private void bgWorkerGenerateImages_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (string resultFile in ResultFiles)
            {
                currentFile = resultFile;
                bgWorkerGenerateImages.ReportProgress(0);
                string FullListFile = resultFile.Replace(".csv", "_FullList.csv");
                string QuantFile = resultFile.Replace(".csv", "_Quant.csv");
                //Get individual image
                if (isIndividualImg && File.Exists(FullListFile))
                {
                    GenerateImages.GenGlycanLcImg(
                        FullListFile,
                        ExportFilePath + "\\" + Path.GetFileNameWithoutExtension(resultFile));
                }
                //Get Quant Image
                if (isQuantImg && File.Exists(QuantFile))
                {
                    GenerateImages.GenQuantImg(
                        QuantFile,
                        LabelingMethod,
                        ExportFilePath + "\\" + Path.GetFileNameWithoutExtension(resultFile));
                }
                CompletedCount = CompletedCount + 1;
                bgWorkerGenerateImages.ReportProgress(0);
            }
        }

        private void bgWorkerGenerateImages_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = Convert.ToInt32( CompletedCount/(float) ResultFiles.Count* 100);
            lblPercentage.Text = (CompletedCount/(float) ResultFiles.Count*100).ToString("00") + "%";
            lblFileName.Text = "FileName:" + currentFile + "\t(" + CompletedCount.ToString() + "/" +ResultFiles.Count.ToString() + ")";
        }

        private void bgWorkerGenerateImages_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }
    }
}
