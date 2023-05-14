using Autodesk.AutoCAD.DatabaseServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    internal static class DatabaseHelper
    {
        public static void AddLineType2(this Database database, string lineTypeName)
        {
            using Transaction transaction = database.TransactionManager.StartTransaction();
            LinetypeTable linetypeTable = transaction.GetObject(database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
            if (!linetypeTable.Has(lineTypeName))
            {
                database.LoadLineTypeFile(lineTypeName, "acad.lin");
            }
        }
    }
}
