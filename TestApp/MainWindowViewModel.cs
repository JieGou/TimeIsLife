using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace TestApp
{
    internal class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            AutoLoadDlls();
            isCurrentUser = true;
            FlagLOADCTRLS = 2;

            FileBrowseCommand = new RelayCommand(FileBrowse);
            AddRegistryKeyCommand = new RelayCommand(AddRegistryKey);
            RemoveRegistryKeyCommand = new RelayCommand(RemoveRegistryKey);
        }


        //当前AutoCAD版本
        private string cadName;
        public string CadName
        {
            get => cadName;
            set => SetProperty(ref cadName, value);
        }

        private string dllPath;
        public string DllPath
        {
            get => dllPath;
            set => SetProperty(ref dllPath, value);
        }

        private string appName;
        public string AppName
        {
            get => appName;
            set => SetProperty(ref appName, value);
        }

        private string appDesc;
        public string AppDesc
        {
            get => appDesc;
            set => SetProperty(ref appDesc, value);
        }
        
        private int flagLOADCTRLS;
        public int FlagLOADCTRLS
        {
            get => flagLOADCTRLS;
            set => SetProperty(ref flagLOADCTRLS, value);
        }

        private bool isCurrentUser;
        public bool IsCurrentUser
        {
            get => isCurrentUser;
            set => SetProperty(ref isCurrentUser, value);
        }

        private bool isOverWrite;
        public bool IsOverWrite
        {
            get => isOverWrite;
            set => SetProperty(ref isOverWrite, value);
        }

        public ObservableCollection<CadDll> CadDlls { get; } = new ObservableCollection<CadDll>();

        public IRelayCommand FileBrowseCommand { get; }

        void FileBrowse()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            //判断用户是否通过文件对话框选择了.NET程序集
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //选择的DLL文件的文件名（含路径）
                DllPath = openFileDialog.FileName;
                //选择的DLL文件的文件名（不含路径与后缀），作为应用程序名
                AppName = System.IO.Path.GetFileNameWithoutExtension(DllPath);
                //应用程序描述设置成程序名
                AppDesc = AppName;
            }
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

        private void AutoLoadDlls()
        {
            //获取当前AutoCAD的注册表键名
            string cadKeyName = GetAutoCADKeyName();
            //打开HKEY_LOCAL_MACHINE下当前AutoCAD的注册表键以获得版本号
            RegistryKey keyCAD = Registry.LocalMachine.OpenSubKey(cadKeyName);
            //设置文本框显示当前AutoCAD版本号
            string cadName = keyCAD.GetValue("ProductName").ToString();
            this.CadName = cadName;
            //打开HKEY_CURRENT_USER下当前AutoCAD的Applications注册表键以显示已加载的.NET程序
            RegistryKey keyApplications = Registry.CurrentUser.CreateSubKey(cadKeyName + "\\" + "Applications");
            //遍历Applications下的注册表项
            foreach (var subKeyNameApp in keyApplications.GetSubKeyNames())
            {
                //打开注册表键
                RegistryKey keyApplication = keyApplications.OpenSubKey(subKeyNameApp);

                //如果是.NET程序
                if (keyApplication.GetValue("MANAGED") != null)
                {
                    //在列表框中添加.NET程序的名字和程序路径
                    //CadDll cadDll = new CadDll(keyApplication.GetValue("DESCRIPTION").ToString(), keyApplication.GetValue("LOADER").ToString());
                    CadDll cadDll = new CadDll()
                    {
                        Name = keyApplication.GetValue("DESCRIPTION").ToString(),
                        Path = keyApplication.GetValue("LOADER").ToString()
                    };
                    CadDlls.Add(cadDll);
                }
            }
        }


        public IRelayCommand AddRegistryKeyCommand { get; }

        void AddRegistryKey()
        {
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
            return;
        }

        public IRelayCommand RemoveRegistryKeyCommand { get; }
        void RemoveRegistryKey()
        {
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
            return;
        }

    }

    public class CadDll: ObservableObject
    {

        //public CadDll(string name, string path)
        //{
        //    Name = name; 
        //    Path = path;
        //}

        //private string name;
        //public string Name
        //{
        //    get => name;
        //    set => SetProperty(ref name, value);
        //}

        //private string path;
        //public string Path
        //{
        //    get => path;
        //    set => SetProperty(ref path, value);
        //}

        public string Name { get; set; }
        public string Path { get; set; }

    }
}
