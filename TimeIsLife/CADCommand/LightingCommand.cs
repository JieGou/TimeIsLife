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
using TimeIsLife.Tools;
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



        [CommandMethod("FF_CurveLighting")]
        public void FF_CurveLighting()
        {
            // Put your command code here
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            Matrix3d matrixd = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;

            try
            {
                double curveLength;
                Curve curve;
                BlockReference baseBlockReference;

                //选择块
                while (true)
                {
                    PromptEntityResult result = editor.GetEntity("\n 拾取块：");
                    if (result.Status == PromptStatus.OK)
                    {
                        ObjectId id = result.ObjectId;
                        baseBlockReference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
                        if (baseBlockReference == null)
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

                //选择Curve，判断是否为Arc，Line，Polyline，获取长度
                while (true)
                {
                    PromptEntityResult result = editor.GetEntity("\n 拾取圆弧或直线或多段线：");
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
                        else
                        {
                            editor.WriteMessage("\n请选择圆弧或直线或多段线！");
                            continue;
                        }
                        break;
                    }
                    else if (result.Status == PromptStatus.Cancel)
                    {
                        return;
                    }
                }

                string layer = baseBlockReference.Layer;
                string blockName = baseBlockReference.Name;
                Point3d point3d = new Point3d();
                Scale3d scale3D = baseBlockReference.ScaleFactors;
                double rotateAngle = 0;
                Dictionary<string,string> attNameValues = new Dictionary<string,string>();
                BlockTableRecord blockTableRecord = transaction.GetObject(baseBlockReference.BlockTableRecord,OpenMode.ForRead) as BlockTableRecord;
                if (blockTableRecord == null) return;
                if (blockTableRecord.HasAttributeDefinitions)
                {
                    foreach (var attri in baseBlockReference.AttributeCollection)
                    {
                        AttributeReference ar = null;
                        if (attri is ObjectId objectId)
                        {
                            ar = transaction.GetObject(objectId, OpenMode.ForWrite) as AttributeReference;
                        }
                        else if (attri is AttributeReference attributeReference)
                        {
                            ar = attributeReference;
                        }

                        if (ar != null)
                        {
                            attNameValues.Add(ar.Tag.ToUpper(), ar.TextString);
                        }
                    }
                }

                switch (ElectricalViewModel.electricalViewModel.IsLengthOrCount)
                {
                    //固定数量
                    case true:
                        int m = ElectricalViewModel.electricalViewModel.LightingLineCount;
                        while (m <= 0)
                        {
                            PromptIntegerOptions promptIntegerOptions = new PromptIntegerOptions("请输入灯具数量：");
                            promptIntegerOptions.DefaultValue = m;
                            promptIntegerOptions.UseDefaultValue = true;
                            var result = editor.GetInteger(promptIntegerOptions);
                            if (result.Status == PromptStatus.OK)
                            {
                                ElectricalViewModel.electricalViewModel.LightingLineCount = m = result.Value;
                            }
                            else
                            {
                                return;
                            }
                        }
                        double unitLength = curveLength / m;
                        for (int i = 0; i < m; i++)
                        {
                            point3d = curve.GetPointAtDist(unitLength * (i + ElectricalViewModel.electricalViewModel.Distance));
                            switch (ElectricalViewModel.electricalViewModel.IsAlongTheLine)
                            {
                                //是否沿线方向
                                case true:
                                    var vector3d = curve.GetFirstDerivative(point3d);
                                    rotateAngle = vector3d.GetAngleTo(new Vector3d(1, 0, 0), new Vector3d(0, 0, -1)) + ElectricalViewModel.electricalViewModel.BlockAngle * Math.PI / 180;
                                    break;
                                case false:
                                    rotateAngle = ElectricalViewModel.electricalViewModel.BlockAngle * Math.PI / 180;
                                    break;
                            };
                                                        
                            InsertBlockReference(database, transaction, modelSpace, baseBlockReference, layer, point3d, scale3D, rotateAngle, attNameValues, blockTableRecord);
                        }
                        break;
                    //固定距离
                    case false:
                        double l = ElectricalViewModel.electricalViewModel.LightingLength;
                        while (l <= 0)
                        {
                            PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("请输入灯具间距：");
                            promptDoubleOptions.DefaultValue = l;
                            promptDoubleOptions.UseDefaultValue = true;
                            var result = editor.GetDouble(promptDoubleOptions);
                            if (result.Status == PromptStatus.OK)
                            {
                                ElectricalViewModel.electricalViewModel.LightingLength = l = result.Value;
                            }
                            else
                            {
                                return;
                            }
                        }
                        int j = 0;
                        while (true)
                        {
                            double tempLength = (j + ElectricalViewModel.electricalViewModel.Distance) * ElectricalViewModel.electricalViewModel.LightingLength;
                            if (tempLength > curveLength) break;

                            point3d = curve.GetPointAtDist(tempLength);
                            switch (ElectricalViewModel.electricalViewModel.IsAlongTheLine)
                            {
                                //是否沿线方向
                                case true:
                                    var vector3d = curve.GetFirstDerivative(point3d);
                                    rotateAngle = vector3d.GetAngleTo(new Vector3d(1, 0, 0), new Vector3d(0, 0, -1)) + ElectricalViewModel.electricalViewModel.BlockAngle * Math.PI / 180;
                                    break;
                                case false:
                                    rotateAngle = ElectricalViewModel.electricalViewModel.BlockAngle * Math.PI / 180;
                                    break;
                            };

                            
                            InsertBlockReference(database, transaction, modelSpace, baseBlockReference, layer, point3d, scale3D, rotateAngle, attNameValues, blockTableRecord);
                            j++;
                        }
                        break;
                };

                modelSpace.DowngradeOpen();
                transaction.Commit();

            }
            catch
            {
                transaction.Abort();
            }
        }

        private static void InsertBlockReference(Database database, Transaction transaction, BlockTableRecord modelSpace, BlockReference baseBlockReference, string layer, Point3d point3d, Scale3d scale3D, double rotateAngle, Dictionary<string, string> attNameValues, BlockTableRecord blockTableRecord)
        {
            BlockReference newBlockReference = new BlockReference(point3d, baseBlockReference.BlockTableRecord)
            {
                Layer = layer,
                ScaleFactors = scale3D,
                Rotation = rotateAngle
            };

            modelSpace.AppendEntity(newBlockReference);
            transaction.AddNewlyCreatedDBObject(newBlockReference, true);

            if (blockTableRecord.HasAttributeDefinitions)
            {
                foreach (var id in blockTableRecord)
                {
                    AttributeDefinition attributeDefinition = transaction.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                    if (attributeDefinition != null)
                    {
                        //创建一个新的属性对象
                        AttributeReference attrbuteReference = new AttributeReference();
                        //从属性定义获得属性对象的对象特性
                        attrbuteReference.SetAttributeFromBlock(attributeDefinition, newBlockReference.BlockTransform);
                        //设置属性对象的其它特性
                        attrbuteReference.Position = attributeDefinition.Position.TransformBy(newBlockReference.BlockTransform);
                        attrbuteReference.Rotation = rotateAngle;
                        attrbuteReference.AdjustAlignment(database);
                        //判断是否包含指定的属性名称
                        if (attNameValues.ContainsKey(attributeDefinition.Tag.ToUpper()))
                        {
                            //设置属性值
                            attrbuteReference.TextString = attNameValues[attributeDefinition.Tag.ToUpper()].ToString();
                        }
                        //向块参照添加属性对象
                        newBlockReference.AttributeCollection.AppendAttribute(attrbuteReference);
                        transaction.AddNewlyCreatedDBObject(attrbuteReference, true);
                    }
                }
            }
        }
    }
}
