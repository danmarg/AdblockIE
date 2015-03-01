using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Configuration.Install;
using System.Runtime.InteropServices;

namespace Adblock
{
    [RunInstaller(true)]
    public partial class RegasmInstaller : Installer
    {
        public RegasmInstaller()
        {

        }
        public override void Install(IDictionary stateSaver)
        {

            base.Install(stateSaver);
            Regasm("/codebase");
        }
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            Regasm("/u");
        }
        public override void Uninstall(IDictionary savedState)
        {
            base.Rollback(savedState); Regasm("/u");
        }
        private void Regasm(string parameters)
        {
            //Debugger.Launch();
            string regasmPath = RuntimeEnvironment.GetRuntimeDirectory() + @"regasm.exe";
            string dllPath = this.GetType().Assembly.Location;
            if (!File.Exists(regasmPath))
                throw new InstallException("Registering assembly failed");
            if (!File.Exists(dllPath))
                throw new InstallException("Registering assembly failed");
            Process process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false; // Hides console window     
            process.StartInfo.FileName = regasmPath;
            process.StartInfo.Arguments = string.Format("{0} \"{1}\"", parameters, dllPath);
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InstallException("Registering assembly failed");
        }
    }
}
