using Accord.MachineLearning;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.ToolPalette;

using Dapper;


using DotNetARX;
using NetTopologySuite;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Polygonize;
using NetTopologySuite.Triangulate;

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

using TimeIsLife.Helper;
using TimeIsLife.NTSHelper;
using TimeIsLife.ViewModel;
using TimeIsLife.Model;

using static TimeIsLife.CADCommand.FireAlarmCommand1;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Geometry = NetTopologySuite.Geometries.Geometry;
using GeometryCollection = NetTopologySuite.Geometries.GeometryCollection;
using Accord.Math;
using Accord;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Windows.Data;
using Autodesk.AutoCAD.Windows.Features.PointCloud.PointCloudColorMapping;
using static System.Net.Mime.MediaTypeNames;
using System.Collections;
using System.Windows.Interop;
using System.Windows;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using TimeIsLife.View;
using Point = NetTopologySuite.Geometries.Point;
using TimeIsLife.Jig;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Data.Entity;
using Database = Autodesk.AutoCAD.DatabaseServices.Database;
using NetTopologySuite.Precision;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.OverlayNG;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.FireAlarmCommand2))]

namespace TimeIsLife.CADCommand
{
    internal class FireAlarmCommand2
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

        #region FF_ToHydrantAlarmButtonCommand
        [CommandMethod("FF_ToHydrantAlarmButton")]
        public void FF_ToHydrantAlarmButton()
        {
            Initialize();

            using (Database tempDatabase = new Database(false, true))
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    Matrix3d matrixd = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;

                    #region 获取文件路径，载入消火栓起泵按钮块
                    string blockFullName = "FA-07-消火栓起泵按钮.dwg";
                    ObjectId btrId = InsertBlock(database, tempDatabase, blockFullName);
                    #endregion

                    string name = "";

                    PromptSelectionOptions promptSelectionOptions1 = new PromptSelectionOptions()
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                    };
                    TypedValueList typedValues1 = new TypedValueList();
                    typedValues1.Add(typeof(BlockReference));
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues1);

                    PromptSelectionResult promptSelectionResult1 = editor.GetSelection(promptSelectionOptions1, selectionFilter);
                    if (promptSelectionResult1.Status == PromptStatus.OK)
                    {
                        SelectionSet selectionSet1 = promptSelectionResult1.Value;
                        foreach (var id in selectionSet1.GetObjectIds())
                        {
                            BlockReference blockReference1 = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (blockReference1 == null) continue;
                            name = blockReference1.Name;
                        }
                    }

                    if (name.IsNullOrWhiteSpace()) return;

                    List<BlockReference> blockReferences = new List<BlockReference>();

                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(BlockReference));
                    SelectionFilter blockReferenceSelectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionResult promptSelectionResult = editor.SelectAll(blockReferenceSelectionFilter);
                    SelectionSet selectionSet = promptSelectionResult.Value;

                    foreach (ObjectId blockReferenceId in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                        if (blockReference.Name != name || blockReference == null) continue;
                        blockReference.UpgradeOpen();
                        Scale3d scale3D = blockReference.ScaleFactors;
                        blockReference.ScaleFactors = blockReference.GetUnitScale3d(100);
                        Matrix3d blockreferenceMatrix = blockReference.BlockTransform;
                        blockReference.ScaleFactors = scale3D;
                        blockReference.DowngradeOpen();

                        SetLayer(database, $"E-EQUIP", 4);
                        BlockReference newBlockReference = new BlockReference(Point3d.Origin, btrId);
                        newBlockReference.TransformBy(blockreferenceMatrix);
                        database.AddToModelSpace(newBlockReference);
                    }
                }
                catch
                {
                    transaction.Abort();
                    return;
                }

                transaction.Commit();
            }
        }

        private static ObjectId InsertBlock(Database database, Database tempDatabase, string blockFullName)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string blockPath = Path.Combine(Path.GetDirectoryName(path), "Block", blockFullName);
            string blockName = SymbolUtilityServices.GetSymbolNameFromPathName(blockPath, "dwg");

            ObjectId btrId = ObjectId.Null;
            tempDatabase.ReadDwgFile(blockPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
            tempDatabase.CloseInput(true);

            btrId = database.Insert(blockName, tempDatabase, true);
            return btrId;
        }


        #endregion

        #region FF_FireAlarmCeiling
        [CommandMethod("FF_FireAlarmCeiling")]
        public void FF_FireAlarmCeiling()
        {
            Initialize();
            GeometryFactory geometryFactory = CreateGeometryFactory();

            using Transaction transaction = database.TransactionManager.StartTransaction();
            try
            {
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    RejectObjectsOnLockedLayers = true,
                };
                TypedValueList typedValues = new TypedValueList
                {
                    typeof(Polyline)
                };
                SelectionFilter selectionFilter = new SelectionFilter(typedValues);

                SelectionSet selectionSet = editor.GetSelectionSet(SelectString.GetSelection, promptSelectionOptions, selectionFilter, null);
                if (selectionSet == null)
                {
                    transaction.Abort();
                    return;
                }
                List<Polygon> polygons = new List<Polygon>();
                // 提前获取 Radius 的值
                var radius = 5800;

                foreach (var id in selectionSet.GetObjectIds())
                {
                    Polyline polyline = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                    if (polyline == null) continue;
                    polygons.Add(polyline.ToPolygon(geometryFactory));
                }
                SetLayer(database, $"E-EQUIP", 4);
                List<Coordinate> coordinates = new List<Coordinate>();
                foreach (var polygon in polygons)
                {
                    if (polygon.IsEmpty) continue;
                    if (IsProtected(polygon, radius))
                    {
                        coordinates.Add(polygon.Centroid.Coordinate);
                    }
                    else
                    {
                        int n = 2;
                        while (true)
                        {
                            List<Geometry> splitGeometries = SplitPolygon(geometryFactory, polygon, n, 100.0);
                            if (splitGeometries.All(item => IsProtected(item, radius)))
                            {
                                foreach (var item in splitGeometries)
                                {
                                    coordinates.Add(item.Centroid.Coordinate);
                                }
                                break;
                            }
                            n++;
                        }
                    }
                }

                //去除距梁边小于500mm的布置点
                if (coordinates.Count == 0)
                {
                    editor.WriteMessage("/n 未布置探测器！");
                    transaction.Abort();
                    return;
                }

                // 提前加载并缓存模块ID
                var smokeDetectorID = LoadBlockIntoDatabase(database, "FA-08-智能型点型感烟探测器.dwg");
                var temperatureDetectorID = LoadBlockIntoDatabase(database, "FA-09-智能型点型感温探测器.dwg");


                ObjectId blockReferenceId = (radius == 3600 || radius == 4400) ? temperatureDetectorID : smokeDetectorID;

                foreach (var coordinate in coordinates)
                {
                    BlockReference blockReference = new BlockReference(new Point3d(coordinate.X, coordinate.Y, 0), blockReferenceId);
                    blockReference.ScaleFactors = new Scale3d(100);
                    database.AddToModelSpace(blockReference);
                }
            }
            catch
            {
                transaction.Abort();
                return;
            }
            transaction.Commit();
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

        /// <summary>
        /// 输入半径是否小于几何的最小外接圆的半径
        /// </summary>
        /// <param name="geometry">几何</param>
        /// <param name="radius">半径</param>
        /// <returns>小于等于为真，大于为假</returns>
        private bool IsProtected(Geometry geometry, double radius)
        {
            MinimumBoundingCircle mbc = new MinimumBoundingCircle(geometry);
            Geometry circlePolygon = mbc.GetCircle();

            // 计算几何质心与最小外接圆边界上的一个点之间的距离作为最小外接圆半径
            Point centerPoint = circlePolygon.Centroid;
            Coordinate[] circleCoordinates = circlePolygon.Coordinates;
            double minRadius = centerPoint.Coordinate.Distance(circleCoordinates[0]);

            return minRadius <= radius;
        }

        /// <summary>
        /// 根据输入的值和采样精度拆分几何
        /// </summary>
        /// <param name="geometryFactory"></param>
        /// <param name="geometry">几何</param>
        /// <param name="count">数量</param>
        /// <param name="interval">采样精度</param>
        /// <returns>几何的集合</returns>
        List<Geometry> SplitPolygon(GeometryFactory geometryFactory, Geometry geometry, int count, double interval)
        {
            // 质心点集
            Coordinate maxPoint = geometry.Max();
            Coordinate minPoint = geometry.Min();

            // 构建网格点并判断点是否在多边形内部
            List<double[]> gridPoints = new List<double[]>();
            for (double x = minPoint.X; x <= maxPoint.X; x += interval)
            {
                for (double y = minPoint.Y; y <= maxPoint.Y; y += interval)
                {
                    Point point2D = geometryFactory.CreatePoint(new Coordinate(x, y));

                    if (point2D.Within(geometry))
                    {
                        gridPoints.Add(new double[2] { x, y });
                    }
                }
            }

            // 利用EKmeans 获取分组和簇的质心
            Accord.Math.Random.Generator.Seed = 0;
            KMeans kMeans = new KMeans(count);
            KMeansClusterCollection clusters = kMeans.Learn(gridPoints.ToArray());
            double[][] centerPoints = clusters.Centroids;
            List<Coordinate> coords = new List<Coordinate>();
            foreach (var c in centerPoints)
            {
                coords.Add(new Coordinate(c[0], c[1]));
            }

            // 构建泰森多边形
            VoronoiDiagramBuilder voronoiDiagramBuilder = new VoronoiDiagramBuilder();
            Envelope clipEnvelpoe = new Envelope(minPoint, maxPoint);
            voronoiDiagramBuilder.ClipEnvelope = clipEnvelpoe;
            voronoiDiagramBuilder.SetSites(coords);
            GeometryCollection geometryCollection = voronoiDiagramBuilder.GetDiagram(geometryFactory);

            // 利用封闭面切割泰森多边形
            List<Geometry> geometries = new List<Geometry>();
            for (int i = 0; i < geometryCollection.NumGeometries; i++)
            {
                Geometry vorGeometry = geometryCollection.GetGeometryN(i);
                geometries.Add(vorGeometry.Intersection(geometry));
            }
            return geometries;
        }

        /// <summary>
        /// 加载指定名称块
        /// </summary>
        /// <param name="database">加载块的数据库</param>
        /// <param name="blockName">块名</param>
        /// <returns>载入块ID</returns>
        private ObjectId LoadBlockIntoDatabase(Database database, string blockName)
        {
            using (Database tempDb = new Database(false, true))
            {
                using (Transaction tempTransaction = tempDb.TransactionManager.StartTransaction())
                {
                    try
                    {
                        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                        UriBuilder uri = new UriBuilder(codeBase);
                        string path = Uri.UnescapeDataString(uri.Path);
                        string blockPath = Path.Combine(Path.GetDirectoryName(path), "Block", blockName);
                        string blockSymbolName = SymbolUtilityServices.GetSymbolNameFromPathName(blockPath, "dwg");
                        tempDb.ReadDwgFile(blockPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
                        tempDb.CloseInput(true);
                        tempTransaction.Commit();
                        return database.Insert(blockSymbolName, tempDb, true);
                    }
                    catch
                    {
                        tempTransaction.Abort();
                        editor.WriteMessage("\n加载AutoCAD图块发生错误");
                        return ObjectId.Null;
                    }
                }
            }
        }
        #endregion

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
    }
}
