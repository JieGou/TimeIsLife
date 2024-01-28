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

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        #region FF_EquipmentConnectLine
        [CommandMethod("FF_EquipmentConnectLine")]
        public void FF_EquipmentConnectLine()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
            GeometryFactory geometryFactory = CreateGeometryFactory();

            DisplayInstructions(editor);

            // 添加用户输入来选择连线类型
            PromptKeywordOptions keywordOptions = new PromptKeywordOptions("\n选择连线类型 [环形(Circle)/树形(Tree)]: ");
            keywordOptions.Keywords.Add("Circle");
            keywordOptions.Keywords.Add("Tree");
            keywordOptions.Keywords.Default = "Tree";
            keywordOptions.AllowNone = false;

            PromptResult keywordResult = editor.GetKeywords(keywordOptions);
            if (keywordResult.Status != PromptStatus.OK) return;

            bool isCircle = keywordResult.StringResult == "Circle";

            using Transaction transaction = database.TransactionManager.StartTransaction();
            try
            {
                BlockTableRecord modelSpace = GetModelSpace();

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
                PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);
                if (promptSelectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    Polyline polyline = transaction.GetObject(selectionSet.GetObjectIds().First(), OpenMode.ForRead) as Polyline;
                    if (polyline == null) transaction.Abort();
                    string name = polyline.Layer;

                    //选择选定图上的所有多段线
                    TypedValueList fireAreaTypedValues = new TypedValueList
                        {
                            { DxfCode.LayerName, name },
                            typeof(Polyline)
                        };
                    SelectionFilter fireAreaSelectionFilter = new SelectionFilter(fireAreaTypedValues);
                    PromptSelectionResult fireAreaSelection = editor.SelectAll(fireAreaSelectionFilter);

                    SetLayer(database, "E-WIRE", 3);
                    if (fireAreaSelection.Status == PromptStatus.OK)
                    {
                        SelectionSet fireAreas = fireAreaSelection.Value;

                        //迭代防火分区
                        foreach (var fireAreaPlineId in fireAreas.GetObjectIds())
                        {
                            List<BlockReference> blockReferences = new List<BlockReference>();

                            Polyline fireArea = transaction.GetObject(fireAreaPlineId, OpenMode.ForRead) as Polyline;
                            if (fireArea == null) continue;
                            Point3dCollection point3DCollection = new Point3dCollection();
                            for (int i = 0; i < fireArea.NumberOfVertices; i++)
                            {
                                point3DCollection.Add(fireArea.GetPoint3dAt(i));
                            }

                            TypedValueList blockReferenceTypedValues = new TypedValueList
                            {
                                typeof(BlockReference),
                                { DxfCode.LayerName, "E-EQUIP" }
                            };
                            SelectionFilter blockReferenceSelectionFilter = new SelectionFilter(blockReferenceTypedValues);
                            PromptSelectionResult blockReferenceSelectionResult = editor.SelectCrossingPolygon(point3DCollection, blockReferenceSelectionFilter);
                            if (blockReferenceSelectionResult.Status != PromptStatus.OK)
                            {
                                transaction.Abort();
                                return;
                            }
                            SelectionSet blockReferenceSelection = blockReferenceSelectionResult.Value;
                            foreach (var blockReferenceId in blockReferenceSelection.GetObjectIds())
                            {
                                BlockReference blockReference = transaction.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                                if (blockReference == null) continue;
                                LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                                if (layerTableRecord.IsLocked == true) continue;
                                if (GetPoint3DCollection(blockReference).Count == 0) continue;
                                blockReferences.Add(blockReference);
                            }

                            List<LineString> lineStrings = new List<LineString>();
                            const double Tolerance = 1e-3;

                            if (isCircle)
                            {
                                lineStrings = CreateOptimalRingConnections(blockReferences);
                            }
                            else
                            {
                                lineStrings = CreateMinimumSpanningTreeConnections(blockReferences, geometryFactory);
                            }

                            transaction.GetObject(modelSpace.Id, OpenMode.ForWrite);

                            foreach (var lineString in lineStrings)
                            {
                                var startPoint = new Point3d(lineString.Coordinates[0].X, lineString.Coordinates[0].Y, 0);
                                var endPoint = new Point3d(lineString.Coordinates[1].X, lineString.Coordinates[1].Y, 0);

                                BlockReference br1 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - startPoint.X) < Tolerance && Math.Abs(b.Position.Y - startPoint.Y) < Tolerance);
                                BlockReference br2 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - endPoint.X) < Tolerance && Math.Abs(b.Position.Y - endPoint.Y) < Tolerance);

                                List<Point3d> point3DList1 = GetPoint3DCollection(br1);
                                List<Point3d> point3DList2 = GetPoint3DCollection(br2);

                                var closestPair = (from p1 in point3DList1
                                                   from p2 in point3DList2
                                                   orderby p1.DistanceTo(p2)
                                                   select new { Point1 = p1, Point2 = p2 }).First();

                                var wire = new Line(closestPair.Point1, closestPair.Point2);

                                modelSpace.AppendEntity(wire);
                                transaction.AddNewlyCreatedDBObject(wire, true);
                            }
                        }
                    }
                }
                transaction.Commit();
            }
            catch (System.Exception)
            {

            }
        }
        #endregion

        private void DisplayInstructions(Editor editor)
        {
            string instructions = "\n作用：选择连线形式对设备进行连线" +
                                  "\n操作方法：根据命令提示，选择连线形式" +
                                  "\n注意事项：防火分区多段线需要单独图层，防火分区多段线内需要文字标注防火分区编号，文字和多段线需要同一个图层";
            editor.WriteMessage(instructions);
        }

        private BlockTableRecord GetModelSpace()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            using Transaction transaction = database.TransactionManager.StartTransaction();
            BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            return transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        }


        #region 环形方法6：Google OR-Tools
        public List<LineString> CreateOptimalRingConnections(List<BlockReference> blocks)
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



        #endregion
        public List<LineString> CreateMinimumSpanningTreeConnections(List<BlockReference> blocks, GeometryFactory geometryFactory)
        {
            if (blocks.Count < 2)
                throw new ArgumentException("至少需要2个块来形成树形连线。");

            var points = blocks.Select(b => geometryFactory.CreatePoint(new Coordinate(b.Position.X, b.Position.Y))).ToList();

            return Kruskal.FindMinimumSpanningTree(points, geometryFactory);
        }

    }
}
