using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using DotNetARX;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using TimeIsLife.Jig;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.ToolCommand))]

namespace TimeIsLife.CADCommand
{
    class ToolCommand
    {
        #region FF_QuickUcs
        [CommandMethod("FF_QuickUcs")]
        public void FF_QuickUcs()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            //using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            //try
            //{
            //    ViewportTableRecord viewportTableRecord = (ViewportTableRecord)transaction.GetObject(database.CurrentViewportTableRecordId, OpenMode.ForWrite);
            //    if (viewportTableRecord == null) return;
            //    editor.WriteMessage($"{viewportTableRecord.IconAtOrigin}");
            //    viewportTableRecord.IconAtOrigin = !viewportTableRecord.IconAtOrigin;
            //    //ViewTableRecord viewTableRecord = editor.GetCurrentView();
            //    //if (viewTableRecord == null) return;
            //    //viewTableRecord.UcsIconAtOrigin = true;

            //    //UcsTableRecord ucsTableRecord = (UcsTableRecord)transaction.GetObject(viewTableRecord.UcsName, OpenMode.ForWrite);
            //    //if (ucsTableRecord == null) return;
            //    //ucsTableRecord.Origin = Point3d.Origin;
            //    //ucsTableRecord.DowngradeOpen();
            //    transaction.Commit();
            //}
            //catch
            //{
            //    transaction.Abort();
            //}
            document.SendStringToExecute("UCSICON\nN\n", true, false, true);
            document.SendStringToExecute("UCS\nOB\n", true, false, true);


        }
        #endregion

        #region F1_ConnectLine
        /// <summary>
        /// 作用：两个块之间连线。
        /// 操作方法：块内需要用点表示连接点，运行命令，依次点选两个块，块最近的连接点之间连线。
        /// </summary>
        [CommandMethod("F1_ConnectLine")]
        public void F1_ConnectLine()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：两个块之间连线。";
            string s2 = "\n操作方法：运行命令，依次点选两个块，块最近的连接点之间连线。";
            string s3 = "\n注意事项：块内需要用点表示连接点。";
            editor.WriteMessage(s1 + s2 + s3);
            while (true)
            {
                using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
                try
                {
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    List<List<Point3d>> point3Ds1 = new List<List<Point3d>>();
                    Dictionary<List<Point3d>, double> keyValuePairs = new Dictionary<List<Point3d>, double>();
                    Point3dCollection point3DCollection1 = GetPoint3DCollection();
                    if (point3DCollection1.Count == 0) break;
                    Point3dCollection point3DCollection2 = GetPoint3DCollection();
                    if (point3DCollection2.Count == 0) break;

                    foreach (Point3d item1 in point3DCollection1)
                    {
                        foreach (Point3d item2 in point3DCollection2)
                        {
                            List<Point3d> point3Ds = new List<Point3d>();
                            point3Ds.Add(item1);
                            point3Ds.Add(item2);
                            keyValuePairs.Add(point3Ds, item1.DistanceTo(item2));
                        }
                    }
                    Line line = null;
                    double d = keyValuePairs.Min(p => p.Value);
                    foreach (var item in keyValuePairs)
                    {
                        if (item.Value.Equals(d))
                        {
                            line = new Line(item.Key[0], item.Key[1]);
                        }
                    }
                    if (line == null) continue;
                    btr.UpgradeOpen();
                    btr.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    btr.DowngradeOpen();
                    transaction.Commit();
                }
                catch (System.Exception)
                {

                }
            }
        }

        private Point3dCollection GetPoint3DCollection()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Point3dCollection point3DCollection = new Point3dCollection();

            using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    SingleOnly = true,
                    RejectObjectsOnLockedLayers = true,
                };

                TypedValueList typedValues = new TypedValueList();
                typedValues.Add(typeof(BlockReference));
                SelectionFilter selectionFilter = new SelectionFilter(typedValues);

                PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);

                if (promptSelectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null) continue;
                        Matrix3d matrix3D = blockReference.BlockTransform;

                        BlockTableRecord btr = transaction.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (btr == null) continue;
                        foreach (var objectId in btr)
                        {
                            DBPoint dBPoint = transaction.GetObject(objectId, OpenMode.ForRead) as DBPoint;
                            if (dBPoint == null) continue;
                            point3DCollection.Add(dBPoint.Position.TransformBy(matrix3D));
                        }
                    }
                }
            }

            return point3DCollection;
        }
        #endregion

        #region F2_SetCurrentStatus
        /// <summary>
        /// 作用：根据选择对象的图层、颜色、线型、线型比例设置默认的图层、颜色、线型、线型比例
        /// 操作方法：运行命令，选择对象
        /// </summary>
        [CommandMethod("F2_SetCurrentStatus")]
        public void F2_SetCurrentStatus()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：根据选择对象的图层、颜色、线型、线型比例设置默认的图层、颜色、线型、线型比例。";
            string s2 = "\n操作方法：运行命令，选择对象。";
            editor.WriteMessage(s1 + s2);
            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                    };

                    PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions);

                    if (promptSelectionResult.Status == PromptStatus.OK)
                    {
                        SelectionSet selectionSet = promptSelectionResult.Value;
                        foreach (var id in selectionSet.GetObjectIds())
                        {
                            Entity entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                            if (entity == null) continue;
                            database.Cecolor = entity.Color;
                            database.Clayer = entity.LayerId;
                            database.Celtype = entity.LinetypeId;
                            database.Celtscale = entity.LinetypeScale;
                        }
                    }
                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }

        }
        #endregion

        static bool bo = true;

        #region F3_ConnectLines

        /// <summary>
        /// 作用：在F1的基础上，多个块之间连线。
        /// 操作方法：框选对象，设置连接方向（默认ucs的x轴）
        /// </summary>
        [CommandMethod("F3_ConnectLines")]
        public void F3_ConnectLines()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：在F1的基础上，多个块之间连线。";
            string s2 = "\n操作方法：框选对象，设置连接方向（默认ucs的x轴）";
            string s3 = "\n注意事项：块内需要用点表示连接点。";
            editor.WriteMessage(s1 + s2 + s3);
            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    Point3d startPoint3D = new Point3d();
                    Point3d endPoint3D = new Point3d();
                    Vector3d xVector3D = database.Ucsxdir;
                    Vector3d yVector3D = database.Ucsydir;

                    Matrix3d matrix3D = editor.CurrentUserCoordinateSystem;
                    PromptPointOptions promptPointOptionsX;
                    PromptPointOptions promptPointOptionsY;
                    PromptPointResult ppr;

                    if (bo)
                    {
                        promptPointOptionsX = new PromptPointOptions("\n 请选择第一个角点：[x轴(X)/y轴(Y)]<X>");
                        promptPointOptionsX.Keywords.Add("X");
                        promptPointOptionsX.Keywords.Add("Y");
                        promptPointOptionsX.Keywords.Default = "X";
                        promptPointOptionsX.AppendKeywordsToMessage = false;
                        ppr = editor.GetPoint(promptPointOptionsX);
                    }
                    else
                    {
                        promptPointOptionsY = new PromptPointOptions("\n 请选择第一个角点：[x轴(X)/y轴(Y)]<Y>");
                        promptPointOptionsY.Keywords.Add("X");
                        promptPointOptionsY.Keywords.Add("Y");
                        promptPointOptionsY.Keywords.Default = "X";
                        promptPointOptionsY.AppendKeywordsToMessage = false;
                        ppr = editor.GetPoint(promptPointOptionsY);
                    }

                    while (ppr.Status == PromptStatus.Keyword)
                    {
                        switch (ppr.StringResult)
                        {
                            case "X":
                                promptPointOptionsX = new PromptPointOptions("\n 请选择第一个角点：[x轴(X)/y轴(Y)]<X>");
                                promptPointOptionsX.Keywords.Add("X");
                                promptPointOptionsX.Keywords.Add("Y");
                                promptPointOptionsX.Keywords.Default = "X";
                                promptPointOptionsX.AppendKeywordsToMessage = false;
                                ppr = editor.GetPoint(promptPointOptionsX);
                                bo = true;
                                break;
                            case "Y":
                                promptPointOptionsY = new PromptPointOptions("\n 请选择第一个角点：[x轴(X)/y轴(Y)]<Y>");
                                promptPointOptionsY.Keywords.Add("X");
                                promptPointOptionsY.Keywords.Add("Y");
                                promptPointOptionsY.Keywords.Default = "X";
                                promptPointOptionsY.AppendKeywordsToMessage = false;
                                ppr = editor.GetPoint(promptPointOptionsY);
                                bo = false;
                                break;
                            default:
                                break;
                        }
                    }

                    if (ppr.Status == PromptStatus.OK)
                    {
                        startPoint3D = ppr.Value;
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    try
                    {
                        database.LoadLineTypeFile("DASHED", "acad.lin");
                    }
                    catch { }

                    // 初始化矩形
                    Polyline polyLine = new Polyline();
                    for (int i = 0; i < 4; i++)
                    {
                        polyLine.AddVertexAt(i, new Point2d(0, 0), 0, 0, 0);
                    }
                    polyLine.Closed = true;
                    polyLine.Linetype = "DASHED";
                    polyLine.Transparency = new Transparency(128);
                    polyLine.ColorIndex = 31;
                    polyLine.LinetypeScale = 1000 / database.Ltscale;

                    UcsSelectJig ucsSelectJig = new UcsSelectJig(startPoint3D, polyLine);
                    PromptResult promptResult = editor.Drag(ucsSelectJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        endPoint3D = ucsSelectJig.endPoint3d.TransformBy(matrix3D.Inverse());
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, matrix3D);
                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(BlockReference));
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionResult promptSelectionResult = editor.SelectCrossingPolygon(point3DCollection, selectionFilter);
                    if (promptSelectionResult.Status != PromptStatus.OK)
                    {
                        transaction.Abort();
                        return;
                    }
                    List<BlockReference> blockReferences = new List<BlockReference>();
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null) continue;
                        LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord.IsLocked == true) continue;
                        blockReferences.Add(blockReference);
                    }
                    if (bo)
                    {
                        blockReferences = blockReferences.OrderBy(b => b.Position.TransformBy(matrix3D.Inverse()).X).ToList();
                    }
                    else
                    {
                        blockReferences = blockReferences.OrderBy(b => b.Position.TransformBy(matrix3D.Inverse()).Y).ToList();
                    }

                    for (int i = 0; i < blockReferences.Count - 1; i++)
                    {

                        BlockReference br1 = blockReferences[i];
                        BlockReference br2 = blockReferences[i + 1];
                        //editor.WriteMessage($"\n br1-{br1.Position.X.ToString()},{br1.Position.Y.ToString()},{br1.Position.Z.ToString()};br2-{br2.Position.X.ToString()},{br2.Position.Y.ToString()},{br2.Position.Z.ToString()}");
                        Point3dCollection point3DCollection1 = GetPoint3DCollection(br1);
                        if (point3DCollection1.Count == 0) transaction.Abort();
                        Point3dCollection point3DCollection2 = GetPoint3DCollection(br2);
                        if (point3DCollection2.Count == 0) transaction.Abort();

                        BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                        List<List<Point3d>> point3Ds1 = new List<List<Point3d>>();
                        Dictionary<List<Point3d>, double> keyValuePairs = new Dictionary<List<Point3d>, double>();

                        foreach (Point3d item1 in point3DCollection1)
                        {
                            foreach (Point3d item2 in point3DCollection2)
                            {
                                List<Point3d> point3Ds = new List<Point3d>();
                                point3Ds.Add(item1);
                                point3Ds.Add(item2);
                                keyValuePairs.Add(point3Ds, item1.DistanceTo(item2));
                            }
                        }
                        Line line = null;
                        double d = keyValuePairs.Min(p => p.Value);
                        foreach (var item in keyValuePairs)
                        {
                            if (item.Value.Equals(d))
                            {
                                line = new Line(item.Key[0], item.Key[1]);
                            }
                        }
                        if (line == null) continue;
                        btr.UpgradeOpen();
                        btr.AppendEntity(line);
                        transaction.AddNewlyCreatedDBObject(line, true);
                        btr.DowngradeOpen();
                    }
                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }
        }

        private Point3dCollection GetPoint3DCollection(Point3d up1, Point3d up2, Matrix3d matrix3D)
        {
            //    Point3d p1 = up1.TransformBy(matrix3D.Inverse());
            //    Point3d p2 = up2.TransformBy(matrix3D.Inverse());
            Point3dCollection point3DCollection = new Point3dCollection();
            if (up1.X < up2.X && up1.Y < up2.Y)
            {
                var leftDownPoint = up1;
                var leftUpPoint = new Point3d(up1.X, up2.Y, 0);
                var rightUpPoint = up2;
                var rightDownPoint = new Point3d(up2.X, up1.Y, 0);
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else if (up1.X < up2.X && up1.Y > up2.Y)
            {
                var leftDownPoint = new Point3d(up1.X, up2.Y, 0);
                var leftUpPoint = up1;
                var rightUpPoint = new Point3d(up2.X, up1.Y, 0);
                var rightDownPoint = up2;
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else if (up1.X > up2.X && up1.Y > up2.Y)
            {
                var leftDownPoint = up2;
                var leftUpPoint = new Point3d(up2.X, up1.Y, 0);
                var rightUpPoint = up1;
                var rightDownPoint = new Point3d(up1.X, up2.Y, 0);
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else
            {
                var leftDownPoint = new Point3d(up2.X, up1.Y, 0);
                var leftUpPoint = up2;
                var rightUpPoint = new Point3d(up1.X, up2.Y, 0);
                var rightDownPoint = up1;
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            return point3DCollection;
        }

        private Point3dCollection GetPoint3DCollection(Point3dCollection point3DCollection, Point3d leftDownPoint, Point3d rightDownPoint, Point3d rightUpPoint, Point3d leftUpPoint, Matrix3d matrix3D)
        {
            //    point3DCollection.Add(leftDownPoint.TransformBy(matrix3D.Inverse()));
            //    point3DCollection.Add(rightDownPoint.TransformBy(matrix3D.Inverse()));
            //    point3DCollection.Add(rightUpPoint.TransformBy(matrix3D.Inverse()));
            //    point3DCollection.Add(leftUpPoint.TransformBy(matrix3D.Inverse()));

            point3DCollection.Add(leftDownPoint);
            point3DCollection.Add(rightDownPoint);
            point3DCollection.Add(rightUpPoint);
            point3DCollection.Add(leftUpPoint);

            return point3DCollection;
        }

        private Point3dCollection GetPoint3DCollection(BlockReference blockReference)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Point3dCollection point3DCollection = new Point3dCollection();

            using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                Matrix3d matrix3D = blockReference.BlockTransform;

                BlockTableRecord btr = transaction.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return point3DCollection;
                foreach (var objectId in btr)
                {
                    DBPoint dBPoint = transaction.GetObject(objectId, OpenMode.ForRead) as DBPoint;
                    if (dBPoint == null) continue;
                    point3DCollection.Add(dBPoint.Position.TransformBy(matrix3D));
                }
            }

            return point3DCollection;
        }
        #endregion

        #region F4_AlignUcs
        [CommandMethod("F4_AlignUcs")]
        public void F4_AlignUcs()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：多个对象在ucs坐标系下，沿x轴或者y轴对齐";
            string s2 = "\n操作方法：框选对象，设置对齐方向（默认ucs的x轴），选择基准对齐对象";
            string s3 = "\n注意事项：";
            editor.WriteMessage(s1 + s2 + s3);
            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    Point3d startPoint3D = new Point3d();
                    Point3d endPoint3D = new Point3d();
                    Matrix3d matrix3D = editor.CurrentUserCoordinateSystem;
                    PromptPointOptions promptPointOptionsX = null;
                    PromptPointOptions promptPointOptionsY = null;
                    PromptPointResult ppr;
                    if (bo)
                    {
                        promptPointOptionsX = new PromptPointOptions("\n 请选择第一个角点：[x轴对齐(X)/y轴对齐(Y)]<X>");
                        promptPointOptionsX.Keywords.Add("X");
                        promptPointOptionsX.Keywords.Add("Y");
                        promptPointOptionsX.Keywords.Default = "X";
                        promptPointOptionsX.AppendKeywordsToMessage = false;
                        ppr = editor.GetPoint(promptPointOptionsX);
                    }
                    else
                    {
                        promptPointOptionsY = new PromptPointOptions("\n 请选择第一个角点：[x轴对齐(X)/y轴对齐(Y)]<Y>");
                        promptPointOptionsY.Keywords.Add("X");
                        promptPointOptionsY.Keywords.Add("Y");
                        promptPointOptionsY.Keywords.Default = "X";
                        promptPointOptionsY.AppendKeywordsToMessage = false;
                        ppr = editor.GetPoint(promptPointOptionsY);
                    }

                    while (ppr.Status == PromptStatus.Keyword)
                    {
                        switch (ppr.StringResult)
                        {
                            case "X":
                                promptPointOptionsX = new PromptPointOptions("\n 请选择第一个角点：[x轴对齐(X)/y轴对齐(Y)]<X>");
                                promptPointOptionsX.Keywords.Add("X");
                                promptPointOptionsX.Keywords.Add("Y");
                                promptPointOptionsX.Keywords.Default = "X";
                                promptPointOptionsX.AppendKeywordsToMessage = false;
                                ppr = editor.GetPoint(promptPointOptionsX);
                                bo = true;
                                break;
                            case "Y":
                                promptPointOptionsY = new PromptPointOptions("\n 请选择第一个角点：[x轴对齐(X)/y轴对齐(Y)]<Y>");
                                promptPointOptionsY.Keywords.Add("X");
                                promptPointOptionsY.Keywords.Add("Y");
                                promptPointOptionsY.Keywords.Default = "X";
                                promptPointOptionsY.AppendKeywordsToMessage = false;
                                ppr = editor.GetPoint(promptPointOptionsY);
                                bo = false;
                                break;
                            default:
                                break;
                        }
                    }

                    if (ppr.Status == PromptStatus.OK)
                    {
                        startPoint3D = ppr.Value;
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    //获取第二个点
                    try
                    {
                        database.LoadLineTypeFile("DASHED", "acad.lin");
                    }
                    catch { }

                    // 初始化矩形
                    Polyline polyLine = new Polyline();
                    for (int i = 0; i < 4; i++)
                    {
                        polyLine.AddVertexAt(i, new Point2d(0, 0), 0, 0, 0);
                    }
                    polyLine.Closed = true;
                    polyLine.Linetype = "DASHED";
                    polyLine.Transparency = new Transparency(128);
                    polyLine.ColorIndex = 31;
                    polyLine.LinetypeScale = 1000 / database.Ltscale;

                    UcsSelectJig ucsSelectJig = new UcsSelectJig(startPoint3D, polyLine);
                    PromptResult promptResult = editor.Drag(ucsSelectJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        endPoint3D = ucsSelectJig.endPoint3d.TransformBy(matrix3D.Inverse());
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, matrix3D);
                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(BlockReference));
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionResult promptSelectionResult = editor.SelectCrossingPolygon(point3DCollection, selectionFilter);

                    List<BlockReference> blockReferences = new List<BlockReference>();
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    if (selectionSet.Count == 0)
                    {
                        transaction.Abort();
                        return;
                    }
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null) continue;
                        LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord.IsLocked == true) continue;
                        blockReferences.Add(blockReference);
                    }

                    //选择基准对齐块参照
                    Point3d basePoint = new Point3d();
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = "\n 请选择对齐的基准图元："
                    };

                    PromptSelectionResult psr = editor.GetSelection(promptSelectionOptions, selectionFilter);

                    if (psr.Status == PromptStatus.OK)
                    {
                        SelectionSet ss = psr.Value;
                        if (ss.Count == 0)
                        {
                            transaction.Abort();
                            return;
                        }
                        BlockReference blockReference = transaction.GetObject(ss.GetObjectIds().First(), OpenMode.ForRead) as BlockReference;
                        if (blockReference == null)
                        {
                            transaction.Abort();
                            return;
                        }
                        basePoint = blockReference.Position;
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    foreach (BlockReference blockReference in blockReferences)
                    {
                        blockReference.UpgradeOpen();
                        //blockReference.TransformBy(matrix3D.Inverse());
                        //blockReference.Rotation = 0;
                        //blockReference.TransformBy(matrix3D);
                        Point3d bp = basePoint.TransformBy(matrix3D.Inverse());
                        if (bo)
                        {
                            Point3d tempPoint = blockReference.Position.TransformBy(matrix3D.Inverse());
                            Vector3d vector3D = blockReference.Position.GetVectorTo(new Point3d(tempPoint.X, bp.Y, tempPoint.Z).TransformBy(matrix3D));
                            Matrix3d mt = Matrix3d.Displacement(vector3D);
                            blockReference.TransformBy(mt);
                        }
                        else
                        {
                            Point3d tempPoint = blockReference.Position.TransformBy(matrix3D.Inverse());
                            Vector3d vector3D = blockReference.Position.GetVectorTo(new Point3d(bp.X, tempPoint.Y, tempPoint.Z).TransformBy(matrix3D));
                            Matrix3d mt = Matrix3d.Displacement(vector3D);
                            blockReference.TransformBy(mt);
                        }

                        blockReference.DowngradeOpen();
                    }
                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }
        }
        #endregion

        #region F5_EquipmentAngle
        [CommandMethod("F5_EquipmentAngle")]
        public void F5_EquipmentAngle()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：多个对象在ucs坐标系下，设置对象的旋转角度";
            string s2 = "\n操作方法：框选对象，设置旋转角度（默认ucs的x轴为0度，逆时针选择为正）";
            string s3 = "\n注意事项：";
            editor.WriteMessage(s1 + s2 + s3);
            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    double angle = 0;
                    Point3d startPoint3D = new Point3d();
                    Point3d endPoint3D = new Point3d();
                    Matrix3d matrix3D = editor.CurrentUserCoordinateSystem;

                    PromptPointOptions promptPointOptions1 = new PromptPointOptions($"\n 请选择第一个角点：[块的角度(A)]<{angle}>");
                    promptPointOptions1.Keywords.Add("A");
                    promptPointOptions1.AppendKeywordsToMessage = false;

                    PromptPointResult promptPointResult1;
                    promptPointResult1 = editor.GetPoint(promptPointOptions1);
                    while (promptPointResult1.Status == PromptStatus.Keyword)
                    {
                        switch (promptPointResult1.StringResult)
                        {
                            case "A":
                                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions($"\n 请输入块角度<{angle}>：");
                                PromptDoubleResult promptDoubleResult = editor.GetDouble(promptDoubleOptions);
                                if (promptDoubleResult.Status == PromptStatus.OK)
                                {
                                    angle = promptDoubleResult.Value;
                                }
                                PromptPointOptions promptPointOptions = new PromptPointOptions($"\n 请选择第一个角点：[块的角度(A)]<{angle}>");
                                promptPointOptions.Keywords.Add("A");
                                promptPointOptions.AppendKeywordsToMessage = false;
                                promptPointResult1 = editor.GetPoint(promptPointOptions);
                                break;
                            default:
                                break;
                        }
                    }

                    if (promptPointResult1.Status == PromptStatus.OK)
                    {
                        startPoint3D = promptPointResult1.Value;
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    try
                    {
                        database.LoadLineTypeFile("DASHED", "acad.lin");
                    }
                    catch { }
                    // 初始化矩形
                    Polyline polyLine = new Polyline();
                    for (int i = 0; i < 4; i++)
                    {
                        polyLine.AddVertexAt(i, new Point2d(0, 0), 0, 0, 0);
                    }
                    polyLine.Closed = true;
                    polyLine.Linetype = "DASHED";
                    polyLine.Transparency = new Transparency(128);
                    polyLine.ColorIndex = 31;
                    polyLine.LinetypeScale = 1000 / database.Ltscale;

                    UcsSelectJig ucsSelectJig = new UcsSelectJig(startPoint3D, polyLine);
                    PromptResult promptResult = editor.Drag(ucsSelectJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        endPoint3D = ucsSelectJig.endPoint3d.TransformBy(matrix3D.Inverse());
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, matrix3D);

                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(BlockReference));
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionResult promptSelectionResult = editor.SelectCrossingPolygon(point3DCollection, selectionFilter);

                    List<BlockReference> blockReferences = new List<BlockReference>();
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    if (selectionSet.Count == 0)
                    {
                        transaction.Abort();
                    }
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null) continue;
                        LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord.IsLocked == true) continue;
                        blockReferences.Add(blockReference);
                    }
                    foreach (BlockReference blockReference in blockReferences)
                    {
                        blockReference.UpgradeOpen();
                        blockReference.TransformBy(matrix3D.Inverse());
                        blockReference.Rotation = Math.PI / 180 * angle;
                        blockReference.TransformBy(matrix3D);
                        blockReference.DowngradeOpen();
                    }
                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }
        }
        #endregion

        #region FF_ExplodeMInsertBlock
        [CommandMethod("FF_ExplodeMInsertBlock")]
        public void FF_ExplodeMInsertBlock()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            try
            {

                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    SingleOnly = true,
                    RejectObjectsOnLockedLayers = true,
                };

                TypedValueList typedValues = new TypedValueList();
                typedValues.Add(typeof(BlockReference));
                SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);

                if (promptSelectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        MInsertBlock mInsertBlock = transaction.GetObject(id, OpenMode.ForWrite) as MInsertBlock;
                        if (mInsertBlock == null) continue;
                        mInsertBlock.ExplodeToOwnerSpace();
                        mInsertBlock.Erase();
                    }
                }

                transaction.Commit();
            }
            catch
            {
            }

        }
        #endregion
    }
}
