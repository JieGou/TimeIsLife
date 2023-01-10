using Autodesk.AutoCAD.DatabaseServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    internal static class DBTextHelper
    {
        public static void EditContent(this DBText dBText,string text)
        {
            dBText.UpgradeOpen();
            dBText.TextString = text;
            dBText.DowngradeOpen();
        }
    }
}
