using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using NetTopologySuite.Geometries;
using TimeIsLife.Helper;
using TimeIsLife.Jig;
using TimeIsLife.Model;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        [CommandMethod("F3_ConnectMultiLines")]
        public void F3_ConnectMultiLines()
        {

            // 获取当前文档和数据库的引用
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

			GeometryFactory geometryFactory = CreateGeometryFactory();

            string s1 = "\n作用：多个块按照最近距离自动连线。";
            string s2 = "\n操作方法：框选对象";
            string s3 = "\n注意事项：块不能锁定";
            editor.WriteMessage(s1 + s2 + s3);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                //获取块表
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                //获取模型空间
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                //获取图纸空间
                BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;
				Point3d endPoint3D;

                PromptPointResult ppr = editor.GetPoint(new PromptPointOptions("\n 请选择第一个角点："));

                if (ppr.Status != PromptStatus.OK) return;
                var startPoint3D = ppr.Value;

                database.LoadSysLineType(SystemLinetype.DASHED);

                // 初始化矩形
                Polyline polyLine = new Polyline
                {
                    Closed = true,
                    Linetype = SystemLinetype.DASHED.ToString(),
                    Transparency = new Transparency(128),
                    ColorIndex = 31,
                    LinetypeScale = 1000 / database.Ltscale
                };
                for (int i = 0; i < 4; i++)
                {
                    polyLine.AddVertexAt(i, new Point2d(0, 0), 0, 0, 0);
                }

				UcsSelectJig ucsSelectJig = new UcsSelectJig(startPoint3D, polyLine);
                PromptResult promptResult = editor.Drag(ucsSelectJig);
                if (promptResult.Status != PromptStatus.OK) return;

                endPoint3D = ucsSelectJig.endPoint3d.TransformBy(ucsToWcsMatrix3d.Inverse());

				Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, ucsToWcsMatrix3d);
                TypedValueList typedValues = new TypedValueList
                {
                    typeof(BlockReference),
                    { DxfCode.LayerName, "E-EQUIP" }
                };
                List<BlockReference> blockReferences = new List<BlockReference>();

				SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                PromptSelectionResult promptSelectionResult =
                    editor.SelectCrossingPolygon(point3DCollection, selectionFilter);
                if (promptSelectionResult.Status != PromptStatus.OK) return;

                foreach (var id in promptSelectionResult.Value.GetObjectIds())
                {
                    BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (blockReference == null || blockReference.GetConnectionPoints().Count == 0) continue;
					LayerTableRecord layerTableRecord =
                        transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (layerTableRecord != null && layerTableRecord.IsLocked) continue;
                    blockReferences.Add(blockReference);
                }

                var points = GetNtsPointsFromBlockreference(geometryFactory, blockReferences);
                List<LineString> tree = Kruskal.FindMinimumSpanningTree(points, geometryFactory);
                SetCurrentLayer(database, "E-WIRE", 1);
                const double tolerance = 1e-3;
                modelSpace.UpgradeOpen();
                foreach (var line in tree)
                {
                    var startPoint = new Point3d(line.Coordinates[0].X, line.Coordinates[0].Y, 0);
                    var endPoint = new Point3d(line.Coordinates[1].X, line.Coordinates[1].Y, 0);

                    var br1 = blockReferences.FirstOrDefault(b =>
                        Math.Abs(b.Position.X - startPoint.X) < tolerance &&
                        Math.Abs(b.Position.Y - startPoint.Y) < tolerance);
                    var br2 = blockReferences.FirstOrDefault(b =>
                        Math.Abs(b.Position.X - endPoint.X) < tolerance &&
                        Math.Abs(b.Position.Y - endPoint.Y) < tolerance);

                    Line connectline = GetBlockreferenceConnectline(br1, br2);

                    modelSpace.AppendEntity(connectline);
                    transaction.AddNewlyCreatedDBObject(connectline, true);
				}
                modelSpace.DowngradeOpen();
                transaction.Commit();
            }
        }

        public List<Point> GetNtsPointsFromBlockreference(GeometryFactory geometryFactory, List<BlockReference> blockReferences)
        {
            var points = new List<Point>();
            foreach (var blockReference in blockReferences)
            {
                points.Add(geometryFactory.CreatePoint(new Coordinate(blockReference.Position.X, blockReference.Position.Y)));
            }
            return points;
        }
    }
}
