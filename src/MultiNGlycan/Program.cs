using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
namespace COL.MultiGlycan
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string XcaliburKeyX64 = @"CLSID\{1d23188d-53fe-4c25-b032-dc70acdbdc02}\InprocServer32";  //X64
            //string XcaliburKeyX32 = @"Wow6432Node\CLSID\{1d23188d-53fe-4c25-b032-dc70acdbdc02}\InprocServer32"; //X32
            RegistryKey X64 = Registry.ClassesRoot.OpenSubKey(XcaliburKeyX64);
            //RegistryKey X32 = Registry.ClassesRoot.OpenSubKey(XcaliburKeyX32);

            if (X64 == null)
            {
                MessageBox.Show("Xcalibur Library is not installed. Please install 32 bits MSFileReader or Xcalibur", "Library is not detected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(1);
            }
            Application.Run(new frmMainESI());
        }
    }
}
