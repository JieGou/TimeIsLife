using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {

        [CommandMethod("FF_QuickUcs", CommandFlags.Modal)]
        public void FF_QuickUcs()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

            // 提供关键字选项给用户
            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("\n选择选项 [实体(Entity)/嵌套实体(NestedEntity)]");
            promptKeywordOptions.Keywords.Add("Entity");
            promptKeywordOptions.Keywords.Add("NestedEntity");
            promptKeywordOptions.Keywords.Default = MyPlugin.CurrentUserData.EntityOrNestedEntity;
            PromptResult keywords = editor.GetKeywords(promptKeywordOptions);
            if (keywords.Status != PromptStatus.OK) return; // 用户取消或输入无效
            MyPlugin.CurrentUserData.EntityOrNestedEntity = keywords.StringResult;
            switch (keywords.StringResult)
            {
                case "Entity":
                    // 选择整体实体的逻辑
                    PromptEntityOptions promptEntityOptions = new PromptEntityOptions("\n选择一个实体：")
                    {
                        AllowNone = false 
                    };
                    PromptEntityResult promptEntityResult = editor.GetEntity(promptEntityOptions);

                    if (promptEntityResult.Status != PromptStatus.OK) return;
                    SetUcsBasedOnEntity(promptEntityResult.ObjectId, document);
                    break;
                case "NestedEntity":
                    // 提示用户选择一个嵌套实体
                    PromptNestedEntityOptions promptNestedEntityOptions = new PromptNestedEntityOptions("\n选择一个嵌套实体：")
                    {
                        AppendKeywordsToMessage = false,
                        UseNonInteractivePickPoint = false,
                        NonInteractivePickPoint = default,
                        AllowNone = false
                    };
                    PromptNestedEntityResult promptNestedEntityResult = editor.GetNestedEntity(promptNestedEntityOptions);

                    if (promptNestedEntityResult.Status != PromptStatus.OK) return;
                    SetUcsBasedOnEntity(promptNestedEntityResult.ObjectId, document, promptNestedEntityResult.PickedPoint);
                    break;
            }
        }

        private void SetUcsBasedOnEntity(ObjectId entityId, Document document, Point3d? pickedPoint = null)
        {
            Editor editor = document.Editor;
            using Transaction transaction = document.Database.TransactionManager.StartTransaction();
            Entity entity;
            if (pickedPoint.HasValue)
            {
                entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
            }
            else
            {
                // 直接从ObjectId获取实体
                entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
            }

            // 在这里根据选择的实体或子实体设置UCS
            // 示例：根据直线实体设置UCS
            if (entity is Line line)
            {
                // 根据直线设置UCS的逻辑
            }
            // 其他实体类型处理...
            if (entity is BlockReference br)
            {
                Point3d origin = br.Position;
                double angle = br.Rotation;

                Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
                Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis);

                Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                editor.CurrentUserCoordinateSystem = ucs;
            }
            if (entity is Curve curve)
            {
                Point3d origin = curve.StartPoint;
                Vector3d xAxis = curve.GetFirstDerivative(origin);
                Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis).GetNormal();

                Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                editor.CurrentUserCoordinateSystem = ucs;
            }
            if (entity is DBText text)
            {
                Point3d origin = text.Position;
                double angle = text.Rotation;

                Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
                Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis);

                Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                editor.CurrentUserCoordinateSystem = ucs;
            }
            else if (entity is MText mtext)
            {
                Point3d origin = mtext.Location;
                double angle = mtext.Rotation;

                Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
                Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis);

                Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                editor.CurrentUserCoordinateSystem = ucs;
            }

            transaction.Commit();
        }

    }
}
