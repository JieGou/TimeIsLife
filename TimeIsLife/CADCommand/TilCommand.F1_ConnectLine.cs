using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using DotNetARX;
using Google.Protobuf.Reflection;
using TimeIsLife.Helper;


namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        /// <summary>
        /// 作用：两个块之间连线。
        /// 操作方法：块内需要用点表示连接点，运行命令，依次点选两个块，块最近的连接点之间连线。
        /// </summary>
        [CommandMethod("F1_ConnectSingleLine", CommandFlags.Modal)]
        // 原始方法被重构，现在只处理用户输入和调用 GetBlockreferenceConnectline 方法
        public void F1_ConnectSingleLine()
        {
            // 初始化操作
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            // 输出操作说明到 AutoCAD 命令行
            const string s1 = "\n作用：两个块之间连线。";
            const string s2 = "\n操作方法：运行命令，依次点选两个块，块最近的连接点之间连线。";
            const string s3 = "\n注意事项：块内需要用点表示连接点。";
            editor.WriteMessage(s1 + s2 + s3);
            while (true)
            {
                BlockReference firstBlockReference = PromptForBlockReference("选择第一个块", editor, database);
                if (firstBlockReference == null) return;
                firstBlockReference.Highlight();
                BlockReference secondBlockReference = PromptForBlockReference("选择第二个块", editor, database);
                firstBlockReference.Unhighlight();
                if (secondBlockReference == null) return;

                var connectline = GetBlockreferenceConnectline(firstBlockReference, secondBlockReference);

                // 使用事务将线添加到模型空间
                using Transaction transaction = database.TransactionManager.StartTransaction();
                BlockTableRecord modelSpace =
                    transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                if (modelSpace == null) throw new ArgumentNullException(nameof(modelSpace));
                modelSpace.AppendEntity(connectline);
                transaction.AddNewlyCreatedDBObject(connectline, true);
                transaction.Commit();
            }
        }

        // 辅助方法：提示用户选择一个块并返回 BlockReference
        private BlockReference PromptForBlockReference(string message, Editor editor, Database database)
        {
            PromptSelectionOptions promptOptions = new PromptSelectionOptions()
            {
                MessageForAdding = message,
                SingleOnly = true,
                RejectObjectsOnLockedLayers = true,
            };
            TypedValueList filterList = new TypedValueList { typeof(BlockReference) };
            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionResult selectionResult = editor.GetSelection(promptOptions, filter);

            if (selectionResult.Status != PromptStatus.OK)
                return null;

            using Transaction transaction = database.TransactionManager.StartTransaction();
            ObjectId objectId = selectionResult.Value.GetObjectIds().First();
            return transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
        }
        private Line GetBlockreferenceConnectline(BlockReference firstBlock, BlockReference secondBlock)
        {
            Database database = firstBlock.Database;
            Line connectline = new Line();

            // 事务用于访问数据库对象
            using Transaction transaction = database.TransactionManager.StartTransaction();
            // 获取第一个块的连接点
            Point3dCollection firstBlockPoints = firstBlock.GetConnectionPoints();
            // 获取第二个块的连接点
            Point3dCollection secondBlockPoints = secondBlock.GetConnectionPoints();

            // 如果两个块中至少有一个没有连接点，则不继续执行
            if (firstBlockPoints.Count == 0 || secondBlockPoints.Count == 0)
            {
                return connectline;
            }

            // 计算最近的点对
            double minDistance = double.MaxValue;
            Point3d closestPointFromFirstBlock = Point3d.Origin;
            Point3d closestPointFromSecondBlock = Point3d.Origin;

            foreach (Point3d pointFromFirstBlock in firstBlockPoints)
            {
                foreach (Point3d pointFromSecondBlock in secondBlockPoints)
                {
                    double distance = pointFromFirstBlock.DistanceTo(pointFromSecondBlock);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPointFromFirstBlock = pointFromFirstBlock;
                        closestPointFromSecondBlock = pointFromSecondBlock;
                    }
                }
            }

            // 创建一条连接这两个最近点的线
            if (minDistance < double.MaxValue)
            {
                connectline = new Line(closestPointFromFirstBlock, closestPointFromSecondBlock);
            }

            return connectline;
        }
    }
}