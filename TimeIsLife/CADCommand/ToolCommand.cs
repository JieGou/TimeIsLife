using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using DotNetARX;

using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;
using NetTopologySuite;

using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using TimeIsLife.Jig;
using TimeIsLife.Model;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Point = NetTopologySuite.Geometries.Point;
using TimeIsLife.Helper;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.ToolCommand))]

namespace TimeIsLife.CADCommand
{
    class ToolCommand
    {

        private Document document;
        private Database database;
        private Editor editor;
        private Matrix3d ucsToWcsMatrix3d;

        void Initialize()
        {
            document = Application.DocumentManager.CurrentDocument;
            database = document.Database;
            editor = document.Editor;
            ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
        }

        /// <summary>
        /// 获取NTS指定精度和标准坐标系的GeometryFactory实例
        /// </summary>
        /// <returns>GeometryFactory实例</returns>
        private GeometryFactory CreateGeometryFactory()
        {
            //NTS
            var precisionModel = new PrecisionModel(1000d);
            GeometryPrecisionReducer precisionReducer = new GeometryPrecisionReducer(precisionModel);
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
                (
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                precisionModel,
                4326
                );
            return NtsGeometryServices.Instance.CreateGeometryFactory(precisionModel);
        }

        #region FF_QuickUcs
        [CommandMethod("FF_QuickUcs")]
        public void FF_QuickUcs()
        {
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

            Initialize();

            document.SendStringToExecute("UCSICON\nN\n", true, false, false);
            document.SendStringToExecute("UCS\nOB\n", true, false, false);
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
            Initialize();

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
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    List<List<Point3d>> point3Ds1 = new List<List<Point3d>>();
                    Dictionary<List<Point3d>, double> keyValuePairs = new Dictionary<List<Point3d>, double>();

                    Point3dCollection point3DCollection1 = new Point3dCollection();
                    Point3dCollection point3DCollection2 = new Point3dCollection();
                    BlockReference blockReference1 = null;
                    BlockReference blockReference2 = null;

                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                    };
                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(BlockReference));
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);

                    PromptSelectionResult promptSelectionResult1 = editor.GetSelection(promptSelectionOptions, selectionFilter);
                    if (promptSelectionResult1.Status == PromptStatus.OK)
                    {
                        SelectionSet selectionSet = promptSelectionResult1.Value;
                        foreach (var id in selectionSet.GetObjectIds())
                        {
                            blockReference1 = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (blockReference1 == null) continue;

                            Matrix3d matrix3D = blockReference1.BlockTransform;
                            BlockTableRecord btr1 = transaction.GetObject(blockReference1.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr1 == null) continue;
                            foreach (var objectId in btr1)
                            {
                                DBPoint dBPoint = transaction.GetObject(objectId, OpenMode.ForRead) as DBPoint;
                                if (dBPoint == null) continue;
                                point3DCollection1.Add(dBPoint.Position.TransformBy(matrix3D));
                            }
                        }
                    }
                    if (point3DCollection1.Count == 0) break;
                    blockReference1.Highlight();

                    PromptSelectionResult promptSelectionResult2 = editor.GetSelection(promptSelectionOptions, selectionFilter);
                    if (promptSelectionResult2.Status == PromptStatus.OK)
                    {
                        SelectionSet selectionSet = promptSelectionResult2.Value;
                        foreach (var id in selectionSet.GetObjectIds())
                        {
                            blockReference2 = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (blockReference2 == null) continue;
                            Matrix3d matrix3D = blockReference2.BlockTransform;
                            BlockTableRecord btr2 = transaction.GetObject(blockReference2.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr2 == null) continue;
                            foreach (var objectId in btr2)
                            {
                                DBPoint dBPoint = transaction.GetObject(objectId, OpenMode.ForRead) as DBPoint;
                                if (dBPoint == null) continue;
                                point3DCollection2.Add(dBPoint.Position.TransformBy(matrix3D));
                            }
                        }
                    }
                    if (point3DCollection2.Count == 0) break;
                    blockReference1.Unhighlight();

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
                    modelSpace.UpgradeOpen();
                    modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    modelSpace.DowngradeOpen();
                    transaction.Commit();
                }
                catch (System.Exception)
                {
                    transaction.Abort();
                }
            }
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
            Initialize();

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

            Initialize();
            GeometryFactory geometryFactory = CreateGeometryFactory();

            string s1 = "\n作用：多个块按照最近距离自动连线。";
            string s2 = "\n操作方法：框选对象";
            string s3 = "\n注意事项：块不能锁定";
            editor.WriteMessage(s1 + s2 + s3);

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            Point3d startPoint3D = new Point3d();
            Point3d endPoint3D = new Point3d();

            Matrix3d matrix3D = editor.CurrentUserCoordinateSystem;
            PromptPointOptions promptPointOptions;

            promptPointOptions = new PromptPointOptions("\n 请选择第一个角点：");
            PromptPointResult ppr = editor.GetPoint(promptPointOptions);

            if (ppr.Status == PromptStatus.OK)
            {
                startPoint3D = ppr.Value;
            }
            else
            {
                transaction.Abort();
                return;
            }

            database.AddLineType2("DASHED");

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
            TypedValueList typedValues = new TypedValueList
                    {
                        typeof(BlockReference),
                        { DxfCode.LayerName, "E-EQUIP" }
                    };
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
                if (GetPoint3DCollection(blockReference).Count == 0) continue;
                blockReferences.Add(blockReference);
            }

            var points = ConvertCoordinatesToPoints(geometryFactory, blockReferences);
            List<LineString> tree = Kruskal.FindMinimumSpanningTree(points, geometryFactory);
            SetLayer(database, "E-WIRE", 1);
            BlockReference br1;
            BlockReference br2;
            const double Tolerance = 1e-3;

            foreach (var line in tree)
            {
                var startPoint = new Point3d(line.Coordinates[0].X, line.Coordinates[0].Y, 0);
                var endPoint = new Point3d(line.Coordinates[1].X, line.Coordinates[1].Y, 0);

                br1 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - startPoint.X) < Tolerance && Math.Abs(b.Position.Y - startPoint.Y) < Tolerance);
                br2 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - endPoint.X) < Tolerance && Math.Abs(b.Position.Y - endPoint.Y) < Tolerance);

                List<Point3d> point3DList1 = GetPoint3DCollection(br1);
                List<Point3d> point3DList2 = GetPoint3DCollection(br2);

                var closestPair = (from p1 in point3DList1
                                   from p2 in point3DList2
                                   orderby p1.DistanceTo(p2)
                                   select new { Point1 = p1, Point2 = p2 }).First();

                Line wire = new Line(closestPair.Point1, closestPair.Point2);

                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                btr.UpgradeOpen();
                btr.AppendEntity(wire);
                transaction.AddNewlyCreatedDBObject(wire, true);
                btr.DowngradeOpen();
            }

            transaction.Commit();
        }

        private void SetLayer(Database db, string layerName, int colorIndex)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

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
                    tr.AddNewlyCreatedDBObject(layerTableRecord, add: true);
                    layerTable.DowngradeOpen();
                }
                else
                {
                    layerId = layerTable[layerName];
                }

                // Set layer color
                LayerTableRecord layerTableRecordToModify = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                layerTableRecordToModify.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);

                // Set current layer
                db.Clayer = layerId;

                tr.Commit();
            }
        }

        public List<Point> ConvertCoordinatesToPoints(GeometryFactory geometryFactory, List<BlockReference> blockReferences)
        {
            var points = new List<Point>();
            foreach (var blockReference in blockReferences)
            {
                points.Add(geometryFactory.CreatePoint(new Coordinate(blockReference.Position.X, blockReference.Position.Y)));
            }
            return points;
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

        private List<Point3d> GetPoint3DCollection(BlockReference blockReference)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            List<Point3d> point3DList = new List<Point3d>();

            using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                Matrix3d matrix3D = blockReference.BlockTransform;

                BlockTableRecord btr = transaction.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return point3DList;

                foreach (DBPoint dBPoint in btr.OfType<ObjectId>().Select(id => transaction.GetObject(id, OpenMode.ForRead)).OfType<DBPoint>())
                {
                    point3DList.Add(dBPoint.Position.TransformBy(matrix3D));
                }
            }

            return point3DList;
        }
        #endregion

        #region F4_AlignUcs
        [CommandMethod("F4_AlignUcs")]
        public void F4_AlignUcs()
        {
            Initialize();

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
                    database.AddLineType2("DASHED");

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
            Initialize();

            string s1 = "\n作用：多个对象在ucs坐标系下，设置对象的旋转角度";
            string s2 = "\n操作方法：框选对象，设置旋转角度（默认ucs的x轴为0度，逆时针选择为正）";
            string s3 = "\n注意事项：";
            editor.WriteMessage(s1 + s2 + s3);

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();

            Point3d startPoint3D = new Point3d();
            Point3d endPoint3D = new Point3d();

            PromptPointOptions promptPointOptions = new PromptPointOptions($"\n 请选择第一个角点:");
            PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);

            if (promptPointResult.Status == PromptStatus.OK)
            {
                startPoint3D = promptPointResult.Value;
            }
            else
            {
                transaction.Abort();
                return;
            }

            database.AddLineType2("DASHED");
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
                endPoint3D = ucsSelectJig.endPoint3d.TransformBy(ucsToWcsMatrix3d.Inverse());
            }
            else
            {
                transaction.Abort();
                return;
            }

            Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, ucsToWcsMatrix3d);

            TypedValueList typedValues = new TypedValueList
            {
                typeof(BlockReference)
            };
            SelectionFilter selectionFilter = new SelectionFilter(typedValues);
            PromptSelectionResult promptSelectionResult = editor.SelectCrossingPolygon(point3DCollection, selectionFilter);
            List<BlockReference> blockReferences = new List<BlockReference>();
            SelectionSet selectionSet = promptSelectionResult.Value;
            if (selectionSet == null)
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
            foreach (BlockReference blockReference in blockReferences)
            {
                blockReference.UpgradeOpen();
                blockReference.TransformBy(ucsToWcsMatrix3d.Inverse());
                blockReference.Rotation = 0;
                blockReference.TransformBy(ucsToWcsMatrix3d);
                blockReference.DowngradeOpen();
            }
            transaction.Commit();
        }
        #endregion

        #region FF_ExplodeMInsertBlock
        [CommandMethod("FF_ExplodeMInsertBlock")]
        public void FF_ExplodeMInsertBlock()
        {
            Initialize();

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

        #region FF_ModifyTextStyle

        /// 文字样式校正
        /// </summary>
        [CommandMethod("FF_ModifyTextStyle")]
        public void FF_ModifyTextStyle()
        {
            Initialize();

            string sysFontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);//windows系统字体目录
            DirectoryInfo sysDirInfo = new DirectoryInfo(sysFontsPath);//Windows系统字体文件夹

            using (Transaction transaction = document.TransactionManager.StartOpenCloseTransaction())
            {
                TextStyleTable textStyleTable = (TextStyleTable)transaction.GetObject(database.TextStyleTableId, OpenMode.ForRead, false);
                foreach (ObjectId id in textStyleTable)
                {
                    using TextStyleTableRecord textStyleTableRecord = (TextStyleTableRecord)transaction.GetObject(id, OpenMode.ForWrite, false);
                    #region 校正windows系统字体
                    if (textStyleTableRecord.Font.TypeFace != string.Empty)
                    {
                        string fontFileFullName = string.Empty;

                        FileInfo[] fis = sysDirInfo.GetFiles(textStyleTableRecord.FileName);
                        if (fis.Length > 0)
                        {
                            fontFileFullName = fis[0].FullName;
                        }
                        else
                        {
                            fontFileFullName = FindFontFile(database, textStyleTableRecord.FileName);
                        }

                        if (fontFileFullName != string.Empty)
                        {
                            using (PrivateFontCollection privateFontCollection = new PrivateFontCollection())
                            {
                                try
                                {
                                    privateFontCollection.AddFontFile(fontFileFullName);

                                    //更正文字样式的字体名
                                    if (privateFontCollection.Families[0].Name != textStyleTableRecord.Font.TypeFace)
                                    {
                                        textStyleTableRecord.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                                            privateFontCollection.Families[0].Name, textStyleTableRecord.Font.Bold, textStyleTableRecord.Font.Italic,
                                            textStyleTableRecord.Font.CharacterSet, textStyleTableRecord.Font.PitchAndFamily
                                            );
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    editor.WriteMessage($"\n***错误***：{fontFileFullName}-{e.Message}");
                                }
                            }
                        }
                        else
                        {
                            //字体缺失,则用宋体代替
                            textStyleTableRecord.FileName = "SimSun.ttf";
                            textStyleTableRecord.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("宋体", false, false, 134, 2);
                        }
                    }
                    #endregion
                    #region 校正shx字体
                    else
                    {
                        if (!textStyleTableRecord.IsShapeFile &&
                            FindFontFile(database, textStyleTableRecord.FileName) == string.Empty)
                        {
                            textStyleTableRecord.FileName = "romans.shx";//用romans.shx代替
                        }

                        if (textStyleTableRecord.BigFontFileName != string.Empty &&
                            FindFontFile(database, textStyleTableRecord.BigFontFileName) == string.Empty)
                        {
                            textStyleTableRecord.BigFontFileName = "hztxt.shx";//用gbcbig.shx代替
                        }
                    }
                    #endregion
                }

                transaction.Commit();
            }

            editor.Regen();
            editor.UpdateScreen();
        }

        private static string FindFontFile(Database db, string name)
        {
            var hostapp = HostApplicationServices.Current;

            if (name == "") return string.Empty;

            string fullname = string.Empty;
            try
            {
                fullname = hostapp.FindFile(name, db, FindFileHint.FontFile);
            }
            catch { }

            return fullname;
        }
        #endregion
    }
}
