using Microsoft.Win32;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

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

            process.WaitForExit();


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

        public string GetAutoCADKeyName()
        {
            // 获取HKEY_CURRENT_USER键
            RegistryKey keyCurrentUser = Registry.CurrentUser;
            // 打开AutoCAD所属的注册表键:HKEY_CURRENT_USER\Software\Autodesk\AutoCAD
            RegistryKey keyAutoCAD = keyCurrentUser.OpenSubKey("Software\\Autodesk\\AutoCAD");
            //获得表示当前的AutoCAD版本的注册表键值:R18.2
            string valueCurAutoCAD = (string)keyAutoCAD.GetValue("CurVer");
            if (valueCurAutoCAD == null) return "";//如果未安装AutoCAD，则返回
            //获取当前的AutoCAD版本的注册表键:HKEY_LOCAL_MACHINE\Software\Autodesk\AutoCAD\R18.2
            RegistryKey keyCurAutoCAD = keyAutoCAD.OpenSubKey(valueCurAutoCAD);
            //获取表示AutoCAD当前语言的注册表键值:ACAD-a001:804
            string language = keyCurAutoCAD.GetValue("CurVer").ToString();
            //获取AutoCAD当前语言的注册表键:HKEY_LOCAL_MACHINE\Software\Autodesk\AutoCAD\R18.2\ACAD-a001:804
            RegistryKey keyLanguage = keyCurAutoCAD.OpenSubKey(language);
            //返回去除HKEY_LOCAL_MACHINE前缀的当前AutoCAD注册表项的键名:Software\Autodesk\AutoCAD\R18.2\ACAD-a001:804
            return keyLanguage.Name.Substring(keyCurrentUser.Name.Length + 1);
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
