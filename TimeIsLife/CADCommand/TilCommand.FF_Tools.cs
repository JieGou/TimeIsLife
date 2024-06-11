using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Forms;

using TimeIsLife.View;

namespace TimeIsLife.CADCommand
{
    
    internal partial class TilCommand
    {
        // 创建一个私有的PaletteSet字段
        private PaletteSet paletteSet;

        [CommandMethod("FF_Tools")]
        public void FF_Tools()
        {
            if (paletteSet == null) // 判断面板是否创建；true:未创建；false:已创建；
            {
                // 创建面板
                paletteSet = new PaletteSet("TimeIsLife") // 创建一个新的PaletteSet实例，并设置其标题为"TimeIsLife"
                {
                    DockEnabled = DockSides.Left, // 设置PaletteSet的DockEnabled属性为DockSides.Left，使其可以停靠在窗口的左侧
                    TitleBarLocation = PaletteSetTitleBarLocation.Left // 设置标题栏位置

                };
                ElementHost host = new ElementHost // 创建一个ElementHost实例
                {
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Child = new ElectricalView() // 将ElectricalView实例分配给ElementHost的Child属性
                };
                paletteSet.Add("电气", host); // 将ElementHost实例添加到PaletteSet中，并将其命名为"电气"
            }
            paletteSet.Visible = true; // 设置面板可见
            paletteSet.Dock = DockSides.Left; // 设置PaletteSet的Dock属性
        }
    }
}
