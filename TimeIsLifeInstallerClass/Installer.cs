using Microsoft.Win32;

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace TimeIsLifeInstallerClass
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }

        //安装前
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);
        }

        //安装
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }

        #region 安装后

        
        //安装后
        protected override void OnAfterInstall(IDictionary savedState)
        {
            string path = this.Context.Parameters["targetdir"];
            path = Path.GetDirectoryName(path);

            string AppName = "TimeIsLife";
            string AppDesc = AppName;
            string dllPath = Path.Combine(path, AppName + ".dll");

            //Debugger.Launch();

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Combine(path, "TimeIsLifeRegister.exe");
            info.Arguments = dllPath;
            info.UseShellExecute = true;
            info.WorkingDirectory = path;
            info.CreateNoWindow = false;
            info.WindowStyle = ProcessWindowStyle.Normal;
            Process process = Process.Start(info);
            //Thread.Sleep(100);

            //process.WaitForExit();


            //Thread thread = new Thread(() =>
            //{
            //    RegisterWindow registerWindow = new RegisterWindow(DllPath); 
            //    registerWindow.Show();
            //    registerWindow.Closed += (sender, e) => registerWindow.Dispatcher.InvokeShutdown();
            //    System.Windows.Threading.Dispatcher.Run();
            //});

            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();

            //Dispatcher.CurrentDispatcher.Invoke(new Action(() => {RegisterWindow registerWindow = new RegisterWindow(DllPath);registerWindow.Show(); }));


        }

        #endregion
















        //卸载前
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            
        }

        //卸载
        public override void Uninstall(IDictionary savedState)
        {
            
        }

        //卸载后
        protected override void OnAfterUninstall(IDictionary savedState)
        {
            
        }

        //回滚
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
    }
}
