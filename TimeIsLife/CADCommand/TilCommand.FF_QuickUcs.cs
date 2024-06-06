using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using System;
using System.Windows.Documents;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

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

            bool bo = true;
            while (bo)
            {
                switch (MyPlugin.CurrentUserData.EntityOrNestedEntity)
                {
                    case "E":
                        bo = HandleEntitySelection(editor, document);
                        break;
                    case "N":
                        bo = HandleNestedEntitySelection(editor, document);
                        break;
                }
            }
        }

        private bool HandleEntitySelection(Editor editor, Document document)
        {
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
            PromptEntityOptions promptEntityOptions = new PromptEntityOptions("\n选择一个实体或[嵌套实体(N)/世界(W)]：");
            promptEntityOptions.Keywords.Add("N");
            promptEntityOptions.Keywords.Add("W");
            promptEntityOptions.AppendKeywordsToMessage = false;
            PromptEntityResult promptEntityResult = editor.GetEntity(promptEntityOptions);

            if (promptEntityResult.Status == PromptStatus.OK)
            {
                SetUcsBasedOnEntity(promptEntityResult.ObjectId, document,promptEntityResult.PickedPoint.TransformBy(ucsToWcsMatrix3d));
                return false;
            }
            else if (promptEntityResult.Status == PromptStatus.Keyword)
            {
                switch (promptEntityResult.StringResult)
                {
                    case "N":
                        MyPlugin.CurrentUserData.EntityOrNestedEntity = promptEntityResult.StringResult;
                        break;
                    case "W":
                        SetUcsToWcs();
                        return false;
                }
            }

            return true;
        }

        private bool HandleNestedEntitySelection(Editor editor, Document document)
        {
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
            PromptNestedEntityOptions promptNestedEntityOptions = new PromptNestedEntityOptions("\n选择一个嵌套实体[实体(E)/世界(W)]：")
            {
                AppendKeywordsToMessage = false,
                UseNonInteractivePickPoint = false,
                NonInteractivePickPoint = default,
                AllowNone = false
            };
            promptNestedEntityOptions.Keywords.Add("E");
            promptNestedEntityOptions.Keywords.Add("W");
            promptNestedEntityOptions.AppendKeywordsToMessage = false;
            PromptNestedEntityResult promptNestedEntityResult = editor.GetNestedEntity(promptNestedEntityOptions);

            if (promptNestedEntityResult.Status == PromptStatus.OK)
            {
                SetUcsBasedOnEntity(promptNestedEntityResult.ObjectId, document, promptNestedEntityResult.PickedPoint.TransformBy(ucsToWcsMatrix3d));
                return false;
            }
            else if (promptNestedEntityResult.Status == PromptStatus.Keyword)
            {
                switch (promptNestedEntityResult.StringResult)
                {
                    case "E":
                        MyPlugin.CurrentUserData.EntityOrNestedEntity = promptNestedEntityResult.StringResult;
                        break;
                    case "W":
                        SetUcsToWcs();
                        return false;
                }
            }

            return true;
        }

        private void SetUcsBasedOnEntity(ObjectId entityId, Document document, Point3d pickedPoint)
        {
            Editor editor = document.Editor;
            using Transaction transaction = document.Database.TransactionManager.StartTransaction();
            Entity entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;

            if (entity is BlockReference br)
            {
                SetUcsForBlockReference(br, editor, pickedPoint);
            }
            else if (entity is Curve curve)
            {
                SetUcsForCurve(curve, document, pickedPoint);
            }
            else if (entity is DBText text)
            {
                SetUcsForText(text, editor);
            }
            else if (entity is MText mtext)
            {
                SetUcsForMText(mtext, editor);
            }

            transaction.Commit();
        }

        private void SetUcsForBlockReference(BlockReference br, Editor editor, Point3d pickedPoint)
        {
            Point3d origin = br.Position;
            double angle = br.Rotation;

            Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
            SetUcs(editor, origin, xAxis);
        }

        private void SetUcsForCurve(Curve curve, Document document, Point3d pickedPoint)
        {
            LineSegment3d[] lineSegments = GetPickBoxLineSegments(pickedPoint);

            if (curve is Polyline polyline)
            {
                // 找到和拾取框相交的线段
                for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                {
                    if (polyline.GetSegmentType(i) == SegmentType.Line)
                    {
                        LineSegment3d segment = polyline.GetLineSegmentAt(i);
                        //外部参照中的多段线碰撞检测不成功，有可能是拾取框像素转换为实际长度太小导致
                        if (lineSegments[0].IntersectWith(segment) != null || lineSegments[1].IntersectWith(segment) != null || lineSegments[2].IntersectWith(segment) != null || lineSegments[3].IntersectWith(segment) != null)
                        {
                            Point3d startPoint = segment.StartPoint;
                            Point3d endPoint = segment.EndPoint;
                            Point3d origin = pickedPoint.DistanceTo(startPoint) < pickedPoint.DistanceTo(endPoint) ? startPoint : endPoint;
                            Vector3d xAxis = (endPoint - startPoint).GetNormal();

                            // 如果原点是endPoint，则反向xAxis
                            if (origin == endPoint)
                            {
                                xAxis = -xAxis;
                            }

                            SetUcs(document.Editor, origin, xAxis);
                            return;
                        }
                    }
                }
            }
            else if (curve is Line line)
            {
                // 获取最近的端点
                Point3d startPoint = line.StartPoint;
                Point3d endPoint = line.EndPoint;
                Point3d origin = startPoint.DistanceTo(pickedPoint) < endPoint.DistanceTo(pickedPoint) ? startPoint : endPoint;
                Vector3d xAxis = (endPoint - startPoint).GetNormal();

                // 如果原点是endPoint，则反向xAxis
                if (origin == endPoint)
                {
                    xAxis = -xAxis;
                }

                SetUcs(document.Editor, origin, xAxis);
            }
        }
        
        private void SetUcsForText(DBText text, Editor editor)
        {
            Point3d origin = text.Position;
            double angle = text.Rotation;

            Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
            SetUcs(editor, origin, xAxis);
        }

        private void SetUcsForMText(MText mtext, Editor editor)
        {
            Point3d origin = mtext.Location;
            double angle = mtext.Rotation;

            // 获取当前UCS
            Matrix3d currentUcs = editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d ucs = currentUcs.CoordinateSystem3d;

            // 计算MText的x轴方向，加入当前UCS的旋转角度
            double currentUcsAngle = ucs.Xaxis.GetAngleTo(Vector3d.XAxis);
            double totalAngle = angle + currentUcsAngle;
            Vector3d xAxis = new Vector3d(Math.Cos(totalAngle), Math.Sin(totalAngle), 0);

            SetUcs(editor, origin, xAxis);
        }

        private void SetUcsToWcs()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document.Editor;

            // 重置UCS为WCS
            Matrix3d ucsToWcsMatrix = Matrix3d.Identity;
            editor.CurrentUserCoordinateSystem = ucsToWcsMatrix;
            editor.Command("_.UCSICON", "N");
        }

        private void SetUcs(Editor editor, Point3d origin, Vector3d xAxis)
        {
            Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
            Matrix3d ucs = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                origin, xAxis, yAxis, Vector3d.ZAxis);

            editor.CurrentUserCoordinateSystem = ucs;
        }

        //方法：根据点获取拾取框四条边的集合
        private LineSegment3d[] GetPickBoxLineSegments(Point3d pickedPoint)
        {
            // 获取拾取框大小
            int pickBoxSize = System.Convert.ToInt32(Application.GetSystemVariable("PICKBOX"));
            // 获取当前视图
            ViewTableRecord currentView = Application.DocumentManager.MdiActiveDocument.Editor.GetCurrentView();

            // 获取视点宽高（单位是图形文件的单位）
            double viewWidth = currentView.Width;
            double viewHeight = currentView.Height;

            // 获取当前视图的像素大小
            //优化：使用Manager获取设备独立显示大小
            double pixelWidth, pixelHeight;
            Manager manager = Application.DocumentManager.MdiActiveDocument.GraphicsManager;
            pixelWidth = manager.DeviceIndependentDisplaySize.Width;
            pixelHeight = manager.DeviceIndependentDisplaySize.Height;

            // 计算屏幕一个像素在图形文件中的长度
            double lengthPerPixelX = viewWidth / pixelWidth * pickBoxSize * 2;
            double lengthPerPixelY = viewHeight / pixelHeight * pickBoxSize * 2;

            // 计算拾取框四个角点
            Point3d pickBoxPoint1 = new Point3d(pickedPoint.X - lengthPerPixelX / 2, pickedPoint.Y - lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint2 = new Point3d(pickedPoint.X + lengthPerPixelX / 2, pickedPoint.Y - lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint3 = new Point3d(pickedPoint.X + lengthPerPixelX / 2, pickedPoint.Y + lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint4 = new Point3d(pickedPoint.X - lengthPerPixelX / 2, pickedPoint.Y + lengthPerPixelY / 2, 0);

            LineSegment3d lineSegment1 = new LineSegment3d(pickBoxPoint1, pickBoxPoint2);
            LineSegment3d lineSegment2 = new LineSegment3d(pickBoxPoint2, pickBoxPoint3);
            LineSegment3d lineSegment3 = new LineSegment3d(pickBoxPoint3, pickBoxPoint4);
            LineSegment3d lineSegment4 = new LineSegment3d(pickBoxPoint4, pickBoxPoint1);
            return new LineSegment3d[] { lineSegment1, lineSegment2, lineSegment3, lineSegment4 };
        }
    }
}
