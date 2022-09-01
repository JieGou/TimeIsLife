using Microsoft.Win32;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            string DllPath = Path.Combine(path, AppName+".dll");

            Debugger.Launch();
            Dispatcher.CurrentDispatcher.Invoke(new Action(() => {RegisterWindow registerWindow = new RegisterWindow(DllPath);registerWindow.ShowDialog(); }));

            
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
