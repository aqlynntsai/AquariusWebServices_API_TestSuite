using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using API_TestSuite_GUI.AASreference;
using API_TestSuite_GUI.ADSreference;
using API_TestSuite_GUI.APSreference;

namespace API_TestSuite_GUI
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
            Application.Run(new TestSuite());
        }
    }
}
