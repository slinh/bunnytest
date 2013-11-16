using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WinformsSample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // dumps all error codes in a format that can be pasted into java
            //Twitch.ErrorCode[] arr = (Twitch.ErrorCode[])Enum.GetValues(typeof(Twitch.ErrorCode));
            //foreach (Twitch.ErrorCode ec in arr)
            //{
            //    string str = ec.ToString() + "(" + ((int)ec) + "),";
            //    System.Diagnostics.Debug.WriteLine(str);
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SampleForm());
        }
    }
}
