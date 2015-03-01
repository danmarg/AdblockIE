using System;
using System.Runtime.InteropServices;
using SHDocVw;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using mshtml;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace af0.Adblock.PerfTest
{
	[	
	ComVisible(true),
    Guid("9F7D09BE-704E-4bf3-995D-B73ECADD8D5F"),
	ClassInterface(ClassInterfaceType.None)
	]
	public class BHO : IObjectWithSite
	{
		// The web browser object for the host ie
		WebBrowser _webBrowser;
        DateTime start;
        StreamWriter log;

		public BHO()
		{
            log = new StreamWriter(Path.Combine(Path.GetTempPath(), "perftest.log"));
        }
		
		public static string BHOKEYNAME = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Browser Helper Objects";

		[ComRegisterFunction]
		public static void RegisterBHO(Type t)
		{
			RegistryKey key = Registry.LocalMachine.OpenSubKey(BHOKEYNAME, true);

			if (key == null)
				key = Registry.LocalMachine.CreateSubKey(BHOKEYNAME);

			string guidString = t.GUID.ToString("B");
			RegistryKey bhoKey = key.OpenSubKey(guidString);
			
			if (bhoKey == null)
				bhoKey = key.CreateSubKey(guidString);

			key.Close();
			bhoKey.Close();
		}
		
		[ComUnregisterFunction]
		public static void UnregisterBHO(Type t)
		{
			RegistryKey key = Registry.LocalMachine.OpenSubKey(BHOKEYNAME, true);
			string guidString = t.GUID.ToString("B");
			
			if (key != null)
				key.DeleteSubKey(guidString, false);
		}

        void DocumentBegin(object a, ref object b, ref object c, ref object d, ref object e, ref object f, ref bool g)
        {
            start = DateTime.Now;
        }
        void DocumentComplete(object a, ref object b)
        {
            string url = b as string;
            log.WriteLine(String.Format("{0},{1}", url, (DateTime.Now - start).TotalMilliseconds));
            log.Flush();
            start = DateTime.MinValue;
        }

		#region IObjectWithSite Members

		public int SetSite(object site)
		{
            //Debugger.Launch();

            if (site != null)
            {
                _webBrowser = (WebBrowser)site;
                if (!_webBrowser.FullName.ToUpper().EndsWith("IEXPLORE.EXE"))
                {
                    _webBrowser = null;
                    return 0;
                }

                _webBrowser.BeforeNavigate2 += new DWebBrowserEvents2_BeforeNavigate2EventHandler(DocumentBegin);
                _webBrowser.DocumentComplete += new DWebBrowserEvents2_DocumentCompleteEventHandler(DocumentComplete);

			}
			else
            {
                _webBrowser.BeforeNavigate2 -= new DWebBrowserEvents2_BeforeNavigate2EventHandler(DocumentBegin);
                _webBrowser.DocumentComplete -= new DWebBrowserEvents2_DocumentCompleteEventHandler(DocumentComplete);

				_webBrowser = null;
			}

			return 0;
		}

		public int GetSite(ref Guid guid, out IntPtr ppvSite)
		{
			IntPtr punk = Marshal.GetIUnknownForObject(_webBrowser);
			int hr = Marshal.QueryInterface(punk, ref guid, out ppvSite);		
			Marshal.Release(punk);
			
			return hr;
		}

		#endregion
	}
}
