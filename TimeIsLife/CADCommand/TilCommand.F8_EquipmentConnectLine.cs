using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Google.OrTools.ConstraintSolver;


using TimeIsLife.Model;
using TimeIsLife.ViewModel;
using Google.Protobuf.WellKnownTypes;
using TimeIsLife.Helper;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        [CommandMethod("F8_EquipmentConnectLine")]
        public void F8_EquipmentConnectLine()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
            GeometryFactory geometryFactory = CreateGeometryFactory();

            string message = "\n作用：选择连线形式对设备进行连线" +
                                  "\n操作方法：根据命令提示，选择连线形式" +
                                  "\n注意事项：防火分区多段线需要单独图层，防火分区多段线内需要文字标注防火分区编号，文字和多段线需要同一个图层";
            editor.WriteMessage(message);

            // 添加用户输入来选择连线类型
            PromptKeywordOptions keywordOptions = new PromptKeywordOptions("\n选择连线类型 [环形(Circle)/树形(Tree)]: ");
            keywordOptions.Keywords.Add("Circle");
            keywordOptions.Keywords.Add("Tree");
            keywordOptions.Keywords.Default = "Tree";
            keywordOptions.AllowNone = false;

            PromptResult keywordResult = editor.GetKeywords(keywordOptions);
            if (keywordResult.Status != PromptStatus.OK) return;

            bool isCircle = keywordResult.StringResult == "Circle";

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;
                    Point3d endPoint3D;

                    //选取防火分区多段线
                    TypedValueList typedValues = new TypedValueList
                    {
                        typeof(Polyline)
                    };
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = "\n请选择防火分区："
                    };

                    PromptSelectionResult promptSelectionResult =
                        editor.GetSelection(promptSelectionOptions, selectionFilter);
                    if (promptSelectionResult.Status != PromptStatus.OK) return;

                    Polyline polyline =
                        transaction.GetObject(promptSelectionResult.Value.GetObjectIds().First(), OpenMode.ForRead) as Polyline;
                    if (polyline == null) return;

                    //选择选定图上的所有多段线
                    TypedValueList fireAreaTypedValues = new TypedValueList
                        {
                            { DxfCode.LayerName, polyline.Layer },
                            typeof(Polyline)
                        };
                    SelectionFilter fireAreaSelectionFilter = new SelectionFilter(fireAreaTypedValues);
                    PromptSelectionResult fireAreaSelection = editor.SelectAll(fireAreaSelectionFilter);

                    SetCurrentLayer(database, "E-WIRE", 3);

                    if (fireAreaSelection.Status != PromptStatus.OK) return;

                    //迭代防火分区
                    foreach (var id in fireAreaSelection.Value.GetObjectIds())
                    {
                        List<BlockReference> blockReferences = new List<BlockReference>();

                        Polyline fireArea =
                            transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                        if (fireArea == null) continue;
                        Point3dCollection point3DCollection = fireArea.GetPoint3dCollection();

                        TypedValueList blockReferenceTypedValues = new TypedValueList
                                {
                                    typeof(BlockReference),
                                    { DxfCode.LayerName, "E-EQUIP" }
                                };
                        SelectionFilter blockReferenceSelectionFilter =
                            new SelectionFilter(blockReferenceTypedValues);
                        PromptSelectionResult blockReferenceSelectionResult =
                            editor.SelectCrossingPolygon(point3DCollection, blockReferenceSelectionFilter);
                        if (blockReferenceSelectionResult.Status != PromptStatus.OK) continue;

                        foreach (var blockReferenceId in blockReferenceSelectionResult.Value.GetObjectIds())
                        {
                            BlockReference blockReference =
                                transaction.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                            if (blockReference == null) continue;
                            LayerTableRecord layerTableRecord =
                                transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as
                                    LayerTableRecord;
                            if (layerTableRecord != null && layerTableRecord.IsLocked) continue;
                            if (blockReference.GetConnectionPoints().Count == 0) continue;
                            blockReferences.Add(blockReference);
                        }

                        List<LineString> lineStrings;
                        const double tolerance = 1e-3;

                        if (isCircle)
                        {
                            lineStrings = CreateOptimalRingConnections(blockReferences);
                        }
                        else
                        {
                            lineStrings =
                                CreateMinimumSpanningTreeConnections(blockReferences, geometryFactory);
                        }

                        foreach (var lineString in lineStrings)
                        {
                            var startPoint = new Point3d(lineString.Coordinates[0].X,
                                lineString.Coordinates[0].Y, 0);
                            var endPoint = new Point3d(lineString.Coordinates[1].X, lineString.Coordinates[1].Y,
                                0);

                            BlockReference br1 = blockReferences.FirstOrDefault(b =>
                                Math.Abs(b.Position.X - startPoint.X) < tolerance &&
                                Math.Abs(b.Position.Y - startPoint.Y) < tolerance);
                            BlockReference br2 = blockReferences.FirstOrDefault(b =>
                                Math.Abs(b.Position.X - endPoint.X) < tolerance &&
                                Math.Abs(b.Position.Y - endPoint.Y) < tolerance);

                            Line connectline = GetBlockreferenceConnectline(br1, br2);


                            modelSpace.AppendEntity(connectline);
                            transaction.AddNewlyCreatedDBObject(connectline, true);
                        }
                        
                    }
                    transaction.Commit();
                }
                catch (System.Exception)
                {
                    // ignored
                }
            }
        }

        private List<LineString> CreateOptimalRingConnections(List<BlockReference> blocks)
        {
            if (blocks.Count < 3)
                throw new ArgumentException("至少需要3个块来形成环形连线。");

            // 将blocks转换为坐标点集
            var points = blocks.Select(b => new Coordinate(b.Position.X, b.Position.Y)).ToList();

            // OR-Tools 求解器的初始化
            RoutingIndexManager manager = new RoutingIndexManager(points.Count, 1, 0);
            RoutingModel routing = new RoutingModel(manager);

            // 距离计算回调
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // 转换索引为用户定义的点索引
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                Coordinate p1 = points[fromNode];
                Coordinate p2 = points[toNode];
                return (long)Math.Round(Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
            });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // 设置搜索参数
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // 解决问题
            Assignment solution = routing.SolveWithParameters(searchParameters);

            // 获取解决方案并创建LineString连接
            List<Coordinate> sortedPoints = new List<Coordinate>();
            long index = routing.Start(0);
            while (routing.IsEnd(index) == false)
            {
                sortedPoints.Add(points[manager.IndexToNode(index)]);
                index = solution.Value(routing.NextVar(index));
            }
            sortedPoints.Add(sortedPoints[0]); // 闭合环路

            var connections = new List<LineString>();
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                Coordinate start = sortedPoints[i];
                Coordinate end = sortedPoints[i + 1];
                connections.Add(new LineString(new[] { start, end }));
            }

            return connections;
        }


        private List<LineString> CreateMinimumSpanningTreeConnections(List<BlockReference> blocks, GeometryFactory geometryFactory)
        {
            if (blocks.Count < 2)
                throw new ArgumentException("至少需要2个块来形成树形连线。");

            var points = blocks.Select(b => geometryFactory.CreatePoint(new Coordinate(b.Position.X, b.Position.Y))).ToList();

            return Kruskal.FindMinimumSpanningTree(points, geometryFactory);
        }

    }
}
