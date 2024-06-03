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

            bool bo = true;
            while (bo) 
            {
                switch (MyPlugin.CurrentUserData.EntityOrNestedEntity)
                {
                    case "E":
                        // 选择整体实体的逻辑
                        PromptEntityOptions promptEntityOptions = new PromptEntityOptions("\n选择一个实体或[嵌套实体(N)/世界(W)]：");
                        promptEntityOptions.Keywords.Add("N");
                        promptEntityOptions.Keywords.Add("W");
                        promptEntityOptions.AppendKeywordsToMessage = false;
                        PromptEntityResult promptEntityResult = editor.GetEntity(promptEntityOptions);

                        if (promptEntityResult.Status == PromptStatus.OK)
                        {
                            SetUcsBasedOnEntity(promptEntityResult.ObjectId, document);
                            bo = false;
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
                                    bo = false;
                                    break;
                            }
                        }
                        else
                        {
                            bo = false;
                        }
                        break;
                    case "N":
                        // 提示用户选择一个嵌套实体
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
                            SetUcsBasedOnEntity(promptNestedEntityResult.ObjectId, document, promptNestedEntityResult.PickedPoint);
                            bo = false;
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
                                    bo = false;
                                    break;
                            }
                        }
                        else
                        {
                            bo = false;
                        }
                        break;
                }
            };            
        }

        private void SetUcsBasedOnEntity(ObjectId entityId, Document document, Point3d? pickedPoint = null)
        {
            Editor editor = document.Editor;
            using Transaction transaction = document.Database.TransactionManager.StartTransaction();
            Entity entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;

            if (entity is BlockReference br)
            {
                Point3d origin = br.Position;
                double angle = br.Rotation;

                Vector3d xAxis = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
                Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis);

                Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                editor.CurrentUserCoordinateSystem = ucs;

                // 处理嵌套块引用
                BlockTableRecord blockTableRecord = transaction.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (blockTableRecord != null)
                {
                    foreach (ObjectId nestedId in blockTableRecord)
                    {
                        Entity nestedEntity = transaction.GetObject(nestedId, OpenMode.ForRead) as Entity;
                        if (nestedEntity is BlockReference nestedBlockReference)
                        {
                            SetUcsBasedOnEntity(nestedBlockReference.ObjectId, document, pickedPoint);
                        }
                    }
                }
            }
            else if (entity is Curve curve)
            {
                if (curve is Polyline polyline && pickedPoint.HasValue)
                {
                    int segmentIndex = -1;
                    double minDistance = double.MaxValue;

                    for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                    {
                        SegmentType segmentType = polyline.GetSegmentType(i);
                        Curve segmentCurve = null;

                        if (segmentType == SegmentType.Line)
                        {
                            segmentCurve = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                        }

                        if (segmentCurve != null)
                        {
                            Point3d closestPoint = segmentCurve.GetClosestPointTo(pickedPoint.Value, false);
                            double distance = closestPoint.DistanceTo(pickedPoint.Value);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                segmentIndex = i;
                            }
                        }
                    }

                    if (segmentIndex != -1)
                    {
                        Curve selectedSegment = null;
                        SegmentType segmentType = polyline.GetSegmentType(segmentIndex);

                        if (segmentType == SegmentType.Line)
                        {
                            selectedSegment = new Line(polyline.GetPoint3dAt(segmentIndex), polyline.GetPoint3dAt(segmentIndex + 1));
                        }

                        if (selectedSegment != null)
                        {
                            Point3d origin = pickedPoint.Value;
                            Vector3d xAxis = selectedSegment.GetFirstDerivative(origin).GetNormal();
                            Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis).GetNormal();

                            Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                            editor.CurrentUserCoordinateSystem = ucs;
                        }
                    }
                }
                else
                {
                    Point3d origin = curve.StartPoint;
                    Vector3d xAxis = curve.GetFirstDerivative(origin).GetNormal();
                    Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis).GetNormal();

                    Matrix3d ucs = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, origin, xAxis, yAxis, Vector3d.ZAxis);
                    editor.CurrentUserCoordinateSystem = ucs;
                }
            }
            else if (entity is DBText text)
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



        private void SetUcsToWcs()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document.Editor;

            // 重置UCS为WCS
            Matrix3d ucsToWcsMatrix = Matrix3d.Identity;
            editor.CurrentUserCoordinateSystem = ucsToWcsMatrix;
            editor.Command("_.UCSICON", "N");
        }
    }
}
