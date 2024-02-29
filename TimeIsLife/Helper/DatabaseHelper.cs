using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeIsLife.Model;

namespace TimeIsLife.Helper
{
    internal static class DatabaseHelper
    {
        public static void LoadSysLineType(this Database database, SystemLinetype lineTypeName)
        {
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LinetypeTable linetypeTable =
                    transaction.GetObject(database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                if (linetypeTable != null && linetypeTable.Has(lineTypeName.ToString())) return;
                database.LoadLineTypeFile(lineTypeName.ToString(), "acad.lin");
                transaction.Commit();
            }
        }

        public static void NewLayer(this Database database, string layerName, int colorIndex)
        {
            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            LayerTable layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);

            // Create new layer if it doesn't exist
            ObjectId layerId;
            if (!layerTable.Has(layerName))
            {
                LayerTableRecord layerTableRecord = new LayerTableRecord
                {
                    Name = layerName
                };

                layerTable.UpgradeOpen();
                layerId = layerTable.Add(layerTableRecord);
                transaction.AddNewlyCreatedDBObject(layerTableRecord, add: true);
                layerTable.DowngradeOpen();
            }
            else
            {
                layerId = layerTable[layerName];
            }

            // Set layer color
            LayerTableRecord layerTableRecordToModify = (LayerTableRecord)transaction.GetObject(layerId, OpenMode.ForWrite);
            layerTableRecordToModify.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
            layerTableRecordToModify.DowngradeOpen();
            // Set current layer
            database.Clayer = layerId;
            transaction.Commit();
        }
    }
}
