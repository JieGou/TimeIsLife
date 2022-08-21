// (C) Copyright 2022 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using System;

// 该行不是必需的，但是可以提高加载性能
[assembly: ExtensionApplication(typeof(TimeIsLife.MyPlugin))]

namespace TimeIsLife
{
    // 该类由AutoCAD实例化一次，并在会话期间保持有效。 如果您一次都没有进行初始化，则应删除此类。
    public class MyPlugin : IExtensionApplication
    {

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

            // 在此处初始化您的插件应用程序
        }

        void IExtensionApplication.Terminate()
        {
            // 在这里清理插件应用程序
        }

    }

}
