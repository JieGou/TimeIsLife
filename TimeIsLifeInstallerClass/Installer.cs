using Microsoft.Win32;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            
            bool IsCurrentUser = true;
            bool IsOverWrite = true;
            string AppName = "TimeIsLife";
            string AppDesc = AppName;
            int FlagLOADCTRLS = 2;
            string DllPath = Path.Combine(path, AppName+".dll");

            //获取AutoCAD所属的注册表键名
            var autoCADKeyName = GetAutoCADKeyName();
            //确定是HKEY_CURRENT_USER还是HKEY_LOCAL_MACHINE
            RegistryKey keyRoot = IsCurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
            // 由于某些AutoCAD版本的HKEY_CURRENT_USER可能不包括Applications键值，因此要创建该键值
            // 如果已经存在该鍵，无须担心可能的覆盖操作问题，因为CreateSubKey函数会以写的方式打开它而不会执行覆盖操作
            RegistryKey keyApp = keyRoot.CreateSubKey(autoCADKeyName + "\\" + "Applications");
            //若存在同名的程序且选择不覆盖则返回
            if (!IsOverWrite && keyApp.GetSubKeyNames().Contains(AppName)) return;
            //创建相应的键并设置自动加载应用程序的选项
            RegistryKey keyUserApp = keyApp.CreateSubKey(AppName);
            keyUserApp.SetValue("DESCRIPTION", AppDesc, RegistryValueKind.String);
            keyUserApp.SetValue("LOADCTRLS", FlagLOADCTRLS, RegistryValueKind.DWord);
            keyUserApp.SetValue("LOADER", DllPath, RegistryValueKind.String);
            keyUserApp.SetValue("MANAGED", 1, RegistryValueKind.DWord);
        }

        public string GetAutoCADKeyName()
        {
            // 获取HKEY_CURRENT_USER键
            RegistryKey keyCurrentUser = Registry.CurrentUser;
            // 打开AutoCAD所属的注册表键:HKEY_CURRENT_USER\Software\Autodesk\AutoCAD
            RegistryKey keyAutoCAD = keyCurrentUser.OpenSubKey("Software\\Autodesk\\AutoCAD");
            //获得表示当前的AutoCAD版本的注册表键值:R18.2
            string valueCurAutoCAD = keyAutoCAD.GetValue("CurVer").ToString();
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
            bool IsCurrentUser = true;

            string AppName = "TimeIsLife";

            try
            {
                // 获取AutoCAD所属的注册表键名
                string cadName = GetAutoCADKeyName();
                // 确定是HKEY_CURRENT_USER还是HKEY_LOCAL_MACHINE
                RegistryKey keyRoot = IsCurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
                // 以写的方式打开Applications注册表键
                RegistryKey keyApp = keyRoot.OpenSubKey(cadName + "\\" + "Applications", true);
                //删除指定名称的注册表键
                keyApp.DeleteSubKeyTree(AppName);
            }
            catch
            {

            }
        }

        //回滚
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
    }
}
