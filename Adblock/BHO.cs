using System;
using System.Runtime.InteropServices;
using SHDocVw;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using MSHTML;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace af0.Adblock
{
	[	
	ComVisible(true),
    Guid("90EFF544-3981-4d46-85C9-C0361D0931D6"), 
	ClassInterface(ClassInterfaceType.None)
	]
	public class BHO : IObjectWithSite
	{
		// The web browser object for the host ie
		WebBrowser _webBrowser;

		public BHO()
		{
            //Debugger.Launch();
#if TRACE
            TextWriterTraceListener listener = new TextWriterTraceListener(Path.Combine(Path.GetTempPath(), "adblock.log"));
            Trace.AutoFlush = true;
            //Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);
#endif
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

		#region IObjectWithSite Members

        private void Filter()
        {
            try
            {
                HTMLDocument doc = _webBrowser.Document as HTMLDocument;
                if (doc != null)
                {
                    AdblockEngine.Instance.Filter(doc, doc.url);
                }
            }
            catch (Exception e)
            {
                Trace.Fail(e.Message, e.StackTrace);
            }
        }

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

                _webBrowser.DownloadComplete += new DWebBrowserEvents2_DownloadCompleteEventHandler(Filter);
                //_webBrowser.NavigateComplete2 += new DWebBrowserEvents2_NavigateComplete2EventHandler((object x, ref object y) => AdblockEngine.Instance.PreFilter((x as IWebBrowser2).Document as HTMLDocument, y as string));
                //_webBrowser.DocumentComplete += new DWebBrowserEvents2_DocumentCompleteEventHandler((object x, ref object y) => AdblockEngine.Instance.Filter((x as IWebBrowser2).Document as HTMLDocument, y as string));
			}
			else
            {
                _webBrowser.DownloadComplete -= new DWebBrowserEvents2_DownloadCompleteEventHandler(Filter);
                //_webBrowser.NavigateComplete2 -= new DWebBrowserEvents2_NavigateComplete2EventHandler((object x, ref object y) => _adblocker.PreFilter((x as IWebBrowser2).Document as HTMLDocument, y as string));
                //_webBrowser.DocumentComplete -= new DWebBrowserEvents2_DocumentCompleteEventHandler((object x, ref object y) => _adblocker.Filter((x as IWebBrowser2).Document as HTMLDocument, y as string));

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
