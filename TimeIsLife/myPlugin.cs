// (C) Copyright 2022 by  
//

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;

using WinApp = System.Windows.Application;
using System;
using System.Windows;
using TimeIsLife.CADCommand;
using System.Windows.Input;


// 该行不是必需的，但是可以提高加载性能
[assembly: ExtensionApplication(typeof(TimeIsLife.MyPlugin))]
[assembly: CommandClass(typeof(ElectricalCommand))]
[assembly: CommandClass(typeof(FireAlarmCommand1))]
[assembly: CommandClass(typeof(LightingCommand))]
[assembly: CommandClass(typeof(ToolCommand))]

namespace TimeIsLife
{
    // 该类由AutoCAD实例化一次，并在会话期间保持有效。 如果您一次都没有进行初始化，则应删除此类。
    public class MyPlugin : IExtensionApplication
    {
        private InputGestureCollection _gestures;

        void IExtensionApplication.Initialize()
        {
            // 在此处添加一次初始化一种常见的情况是在此处设置一个回调函数，供非托管代码调用。
            // 这样做:
            // 1. 从采用函数指针的非托管代码中导出函数，并将传入的值存储在全局变量中。
            // 2. 在此函数传递委托中调用此导出的函数。
            // 3. 当非托管代码需要此托管模块的服务时，您只需调用acrxLoadApp（）即可，直到acrxLoadApp返回全局函数指针时，它都会初始化为指向C＃委托。
            // 有关更多信息，请参见：
            // http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
            // http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
            // http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
            // 以及一些现有的AutoCAD托管应用程序。

            // 在此处初始化您的插件应用程序+
            Autodesk.Windows.ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
        }

        void IExtensionApplication.Terminate()
        {
            // 在这里清理插件应用程序
        }

        //组件管理器初始化
        public void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
        {
            if (Autodesk.Windows.ComponentManager.Ribbon != null)
            {
                CreateRibbon();//添加ribbon菜单的函数
                Autodesk.Windows.ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
            }
        }

        //根据XAML文件//添加ribbon菜单的函数
        public void CreateRibbon()
        {
            Uri uri = new Uri("/TimeIsLife;component/Resources/RibbonDictionary.xaml", UriKind.Relative);
            ResourceDictionary resourceDictionary = (ResourceDictionary)WinApp.LoadComponent(uri);
            RibbonTab tab = resourceDictionary["TimeIsLife"] as RibbonTab;

            //查找Ribbon按钮并添加命令事件
            RibbonItemCollection items = tab.Panels[0].Source.Items;
            foreach (RibbonItem item in items)
            {
                if (item is RibbonButton)
                    ((RibbonButton)item).CommandHandler = new RibbonCommandHandler();
                else if (item is RibbonRowPanel)
                {
                    RibbonRowPanel row = (RibbonRowPanel)item;
                    foreach (RibbonItem rowItem in row.Items)
                    {
                        if (rowItem is RibbonButton)
                            ((RibbonButton)rowItem).CommandHandler = new RibbonCommandHandler();
                    }
                }
            }
            RibbonControl ribbonControl = ComponentManager.Ribbon;//获取Ribbon界面
            ribbonControl.Tabs.Add(tab);//将选项卡添加到Ribbon界面中
            //ribbonControl.ActiveTab = tab;//设置当前活动选项卡
        }
    }
}
