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

using TimeIsLife.Jig;
using TimeIsLife.ViewModel;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.LightingCommand))]

namespace TimeIsLife.CADCommand
{
    class LightingCommand
    {

        [CommandMethod("FF_GetArea")]
        public void FF_GetArea()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document.Editor;
            Database database = document.Database;

            using Transaction transaction = document.TransactionManager.StartTransaction();
            try
            {
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    SingleOnly = true,
                    RejectObjectsOnLockedLayers = true,
                };
                PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions);
                if (promptSelectionResult.Status != PromptStatus.OK) return;

                SelectionSet selectionSet = promptSelectionResult.Value;

                Polyline polyLine = (Polyline)transaction.GetObject(selectionSet.GetObjectIds()[0], OpenMode.ForRead);
                if (polyLine == null) return;

                double polyLineArea = polyLine.Area;
                ElectricalViewModel.electricalViewModel.LightingArea = Math.Round(polyLineArea * 1e-6, 2);
                transaction.Commit();
            }
            catch
            {
                transaction.Abort();
            }
        }

        [CommandMethod("FF_RecLighting")]
        public void FF_RecLighting()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document.Editor;
            Database database = document.Database;

            Matrix3d matrix3d = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;

            using (document.LockDocument())
            {
                Transaction transaction = document.TransactionManager.StartTransaction();
                BlockTable blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = ((BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead));

                try
                {
                    if (ElectricalViewModel.electricalViewModel.LightingRow == 0 || ElectricalViewModel.electricalViewModel.LightingColumn == 0)
                    {
                        return;
                    }

                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = "\n请选择块："
                    };
                    PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions);
                    if (promptSelectionResult.Status != PromptStatus.OK) return;
                    SelectionSet selectionSet = promptSelectionResult.Value;

                    BlockReference blockReference = (BlockReference)transaction.GetObject(selectionSet.GetObjectIds()[0], OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)blockReference.BlockTableRecord.GetObject(OpenMode.ForRead);
                    if (btr == null) return;

                    PromptPointOptions pointOptions = new PromptPointOptions("\n请选择起始点：");
                    PromptPointResult pointResult = editor.GetPoint(pointOptions);
                    if (pointResult.Status != PromptStatus.OK) return;
                    Point3d basePoint3d = pointResult.Value;

                    string linetypeName = "DASHED";
                    LinetypeTable linetypeTable = (LinetypeTable)database.LinetypeTableId.GetObject(OpenMode.ForRead);
                    if (!linetypeTable.Has(linetypeName))
                    {
                        database.LoadLineTypeFile(linetypeName, "acad.lin");
                    }

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


                    LightingLayoutJig lightingLayoutJig = new LightingLayoutJig(blockReference, basePoint3d, polyLine);
                    PromptResult resJig = editor.Drag(lightingLayoutJig);
                    if (resJig.Status != PromptStatus.OK) return;

                    modelSpace.UpgradeOpen();
                    foreach (var reference in lightingLayoutJig.blockReferences)
                    {
                        modelSpace.AppendEntity(reference);
                        transaction.AddNewlyCreatedDBObject(reference, true);

                        //if (btr.HasAttributeDefinitions)
                        //{
                        //    foreach (var attri in reference.AttributeCollection)
                        //    {
                        //        AttributeReference ar = null;
                        //        if (attri is ObjectId)
                        //        {
                        //            ar = transaction.GetObject((ObjectId)attri,OpenMode.ForWrite) as AttributeReference;
                        //        }
                        //        else if(attri is AttributeReference)
                        //        {
                        //            ar = (AttributeReference)attri;
                        //        }

                        //        if (ar != null)
                        //        {
                        //            //ar.UpgradeOpen();
                        //            //reference.UpgradeOpen();
                        //            bool b1  = ar.IsWriteEnabled;
                        //            bool b2 = reference.IsWriteEnabled;


                        //            Point3d point3D = blockReference.Position;
                        //            //ar.TransformBy(matrixd.Inverse());
                        //            //reference.TransformBy(matrixd.Inverse());
                        //            ar.TransformBy(Matrix3d.Rotation(blockReference.Rotation, Vector3d.ZAxis, point3D.TransformBy(matrixd)).Inverse());

                        //            ar.TransformBy(Matrix3d.Displacement(point3D.TransformBy(matrixd).GetVectorTo(reference.Position)));
                        //            //ar.TransformBy(Matrix3d.Rotation((BlockReferenceAngle * Math.PI) / 180.0, Vector3d.ZAxis, reference.Position));


                        //            //ar.TransformBy(matrixd);
                        //            //reference.TransformBy(matrixd);
                        //            //ar.DowngradeOpen();
                        //            //reference.DowngradeOpen();
                        //        }
                        //    }
                        //}

                    }
                    modelSpace.DowngradeOpen();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                    return;
                }
            }
        }



        [CommandMethod("FF_CurLighting")]
        public void FF_CurLighting()
        {
            // Put your command code here
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();

            BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord space = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            try
            {
                double curveLength;
                Curve curve;
                BlockReference blockReference;

                //选择Curve，判断是否为Arc，Line，Polyline，
                while (true)
                {
                    PromptEntityResult result = editor.GetEntity("\n 拾取圆弧或直线或多段线或样条曲线：");
                    if (result.Status == PromptStatus.OK)
                    {
                        ObjectId id = result.ObjectId;
                        curve = (Curve)transaction.GetObject(id, OpenMode.ForRead);
                        if (curve is Arc arc)
                        {
                            curveLength = arc.Length;
                        }
                        else if (curve is Line line)
                        {
                            curveLength = line.Length;
                        }
                        else if (curve is Polyline polyline)
                        {
                            curveLength = polyline.Length;
                        }
                        else if (curve is Spline spline)
                        {
                            curveLength = ((Polyline)spline.ToPolylineWithPrecision(99)).Length;
                        }
                        else
                        {
                            editor.WriteMessage("\n请选择圆弧或直线或多段线或样条曲线！");
                            continue;
                        }
                        break;
                    }
                    else if (result.Status == PromptStatus.Cancel)
                    {
                        return;
                    }
                }

                //选择块
                while (true)
                {
                    PromptEntityResult result = editor.GetEntity("\n 拾取块：");
                    if (result.Status == PromptStatus.OK)
                    {
                        ObjectId id = result.ObjectId;
                        blockReference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
                        if (blockReference == null)
                        {
                            editor.WriteMessage("\n请选择块！");
                            continue;
                        }
                        break;
                    }
                    else if (result.Status == PromptStatus.Cancel)
                    {
                        return;
                    }
                }

                var blockTableRecord = blockReference.BlockTableRecord;
                if (blockTableRecord == null) return;
                if (ElectricalViewModel.electricalViewModel.LightingLength <= 0) return;

                int n = (int)Math.Ceiling(curveLength / ElectricalViewModel.electricalViewModel.LightingLength);

                for (int i = 0; i < n; i++)
                {
                    Point3d point3D = curve.GetPointAtDist((i + 0.5) * curveLength / n);
                    var vector3d = curve.GetFirstDerivative(point3D);
                    BlockReference br = new BlockReference(point3D, blockTableRecord);
                    br.ScaleFactors = blockReference.ScaleFactors;
                    br.Layer = blockReference.Layer;
                    br.Rotation = vector3d.GetAngleTo(new Vector3d(1, 0, 0), new Vector3d(0, 0, -1));
                    space.AppendEntity(br);
                    transaction.AddNewlyCreatedDBObject(br, true);
                }
                space.DowngradeOpen();
                transaction.Commit();

                //while (true)
                //{
                //    PromptIntegerOptions promptIntegerOptions = new PromptIntegerOptions(c);
                //    promptIntegerOptions.DefaultValue = 3000;
                //    promptIntegerOptions.UseDefaultValue = true;
                //    var result = editor.GetInteger(promptIntegerOptions);
                //    if (result.Status == PromptStatus.OK)
                //    {
                //        d = result.Value;
                //        break;
                //    }
                //    else if (result.Status == PromptStatus.Cancel)
                //    {
                //        return;
                //    }
                //    else
                //    {
                //        editor.WriteMessage("\n 请输入正确的间距");
                //    }
                //}

            }
            catch
            {
                transaction.Abort();
            }
        }
    }
}
