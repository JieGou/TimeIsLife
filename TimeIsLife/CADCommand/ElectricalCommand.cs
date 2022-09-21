using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

using Dapper;

using DotNetARX;

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using TimeIsLife.Helper;
using TimeIsLife.View;
using TimeIsLife.ViewModel;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using CommandFlags = Autodesk.AutoCAD.Runtime.CommandFlags;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.ElectricalCommand))]

namespace TimeIsLife.CADCommand
{
    class ElectricalCommand
    {
        #region FF_Tools
        //创建面板
        static PaletteSet paletteSet;
        static ElectricalView electricalView;
        static ElectricalViewModel electricalViewModel;

        [CommandMethod("FF_Tools")]
        public static void FF_Tools()
        {
            if (paletteSet == null)//如果面板没有被创建
            {

                paletteSet = new PaletteSet("TimeIsLife");
                paletteSet.DockEnabled = DockSides.Left;
                paletteSet.TitleBarLocation = PaletteSetTitleBarLocation.Left;
                paletteSet.Dock = DockSides.Left;

                ElementHost host = new ElementHost();
                host.AutoSize = true;
                host.Dock = DockStyle.Fill;
                host.Child = electricalView = new ElectricalView();
                electricalViewModel = (ElectricalViewModel)electricalView.DataContext;
                paletteSet.Add("电气", host);
            }
            paletteSet.Visible = true;//面板可见

            
        }
        #endregion

        #region FF_SumPower
        //求和功率，选择多个表示功率的单行文字，文字内容由数字+“kw”组成，去除“kw”后的数字求和

        [CommandMethod("FF_SumPower")]
        public void FF_SumPower()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            electricalViewModel.Pe = 0;
            electricalViewModel.NormalOrFirePower = 0;
            electricalViewModel.NormalInFirePower = 0;

            try
            {
                using Transaction transaction = document.TransactionManager.StartTransaction();
                PromptSelectionOptions options = new PromptSelectionOptions { MessageForAdding = "\n请选择要统计的功率:" };
                TypedValueList values = new TypedValueList { typeof(DBText) };
                PromptSelectionResult result = editor.GetSelection(options, values);

                if (result.Status == PromptStatus.OK)
                {
                    SelectionSet set = result.Value;

                    List<double[]> list = new List<double[]>();

                    foreach (var objectId in set.GetObjectIds())
                    {

                        DBText dBText = (DBText)objectId.GetObject(OpenMode.ForRead);
                        string text = dBText.TextString;
                        list.Add(GetPower(text));
                    }

                    //消防/非消防
                    double a = 0;
                    //消防平时负荷
                    double b = 0;
                    
                    foreach (var item in list)
                    {
                        a += item[0];
                        b += item[1];
                    }

                    electricalViewModel.NormalOrFirePower = a;
                    electricalViewModel.NormalInFirePower = b;
                    electricalViewModel.Pe = Math.Max(a, b);

                    transaction.Commit();


                    //PromptSelectionOptions options1 = new PromptSelectionOptions { MessageForAdding = "\n请选择要修改的功率:", SingleOnly = true };
                    //PromptSelectionResult result1 = editor.GetSelection(options1, values);
                    //if (result1.Status == PromptStatus.OK)
                    //{
                    //    SelectionSet set1 = result1.Value;
                    //    DBText dBText = (DBText)set1.GetObjectIds().First().GetObject(OpenMode.ForWrite);
                    //    dBText.TextString = $"{d.ToString()}kW";
                    //    dBText.DowngradeOpen();
                    //}
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 获取字符串中的数字 
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>数字</returns>
        public double[] GetPower(string str)
        {
            double[] arr = new double[] { 0, 0 };
            if (str != null && str != string.Empty)
            {
                // 正则表达式剔除非数字字符（不包含小数点.）
                //str = Regex.Replace(str, @"[^/d./d]", "");
                MatchCollection matchCollection = Regex.Matches(str, "[0-9]+[.]?[0-9]*");
                if (matchCollection.Count == 1)
                {
                    arr[0] = double.Parse(matchCollection[0].Value);
                }
                else if (matchCollection.Count == 2)
                {
                    arr[0] = double.Parse(matchCollection[0].Value);
                    arr[1] = double.Parse(matchCollection[1].Value);
                }
            }
            return arr;
        }

        #endregion

        #region FF_AddCalculateCurrent
        [CommandMethod("FF_AddCalculateCurrent")]
        public void FF_AddCalculateCurrent()
        {
            MessageBox.Show("还未实现");
        }
        #endregion

        #region FF_AddSumPower
        [CommandMethod("FF_AddSumPower")]
        public void FF_AddSumPower()
        {
            MessageBox.Show("还未实现");
        }

        #endregion

        #region FF_GenerateMonitorSystem
        [CommandMethod("FF_GenerateMonitorSystem", CommandFlags.UsePickSet)]
        public void FF_GenerateMonitorSystem()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            Matrix3d matrixd = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;

            //E-配电箱-竖向干线
            string blockName1 = "E-配电箱-竖向干线";
            //E-配电箱-电气火灾监控器
            string blockName2 = "E-配电箱-电气火灾监控器";
            //E-配电箱-消防设备电源监控系统图
            string blockName3 = "E-配电箱-消防设备电源监控系统图";
            //E-配电箱-电力能量监控系统
            string blockName4 = "E-配电箱-电力能量监控系统";

            List<BlockReference> blockReferences1 = new List<BlockReference>();

            PromptSelectionResult psr = editor.SelectImplied();
            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet selectionSet = psr.Value;
                if (selectionSet.Count > 0)
                {
                    foreach (var objectId in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference =transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null || blockReference.Name != blockName1) continue;
                        blockReferences1.Add(blockReference);
                    }
                }
            }
            else
            {
                TypedValueList typedValues = new TypedValueList();
                typedValues.Add(typeof(BlockReference));
                SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                PromptSelectionResult promptSelectionResult = editor.GetSelection(selectionFilter);
                if (promptSelectionResult.Status != PromptStatus.OK) return;
                SelectionSet selectionSet = promptSelectionResult.Value;
                foreach (var objectId in selectionSet.GetObjectIds())
                {
                    BlockReference blockReference = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
                    if (blockReference == null || blockReference.Name != blockName1) continue;
                    blockReferences1.Add(blockReference);
                }
            }

            List<DiagramPanel> diagramPanels = new List<DiagramPanel>();
            foreach (var blockReference1 in blockReferences1)
            {
                diagramPanels.Add(GetDiagramPanel(blockReference1));
            }

            editor.WriteMessage("ok!");
        }

        DiagramPanel GetDiagramPanel(BlockReference blockReference)
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            

            DiagramPanel diagramPanel = new DiagramPanel();

            Matrix3d matrix3D = blockReference.BlockTransform;
            BlockTableRecord blockTableRecord = transaction.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            Extents3d extents3D = new Extents3d();
            foreach (var objectId in blockTableRecord)
            {
                Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                if (entity == null) continue;
                extents3D.AddExtents(entity.GeometricExtents);
            }
            Point3d leftButtomPoint = extents3D.MinPoint;
            Point3d rightUpPoint = extents3D.MaxPoint;
            Point3d rightButtomPoint = new Point3d(rightUpPoint.X, leftButtomPoint.Y, 0);
            Point3d leftUpPoint = new Point3d(leftButtomPoint.X, rightUpPoint.Y, 0);
            Point3d leftMidPoint = new Point3d(leftButtomPoint.X, leftButtomPoint.Y + (leftUpPoint.Y - leftButtomPoint.Y) / 2, 0);
            Point3d rightMidPoint = new Point3d(rightUpPoint.X, rightButtomPoint.Y + (rightUpPoint.Y - rightButtomPoint.Y) / 2, 0);

            Point3dCollection point3DCollection1 = new Point3dCollection();
            point3DCollection1.Add(leftButtomPoint);
            point3DCollection1.Add(rightButtomPoint);
            point3DCollection1.Add(rightMidPoint);
            point3DCollection1.Add(leftMidPoint);
            Polyline polyline1 = GetTextArea(matrix3D, point3DCollection1);

            Point3dCollection point3DCollection2 = new Point3dCollection();
            point3DCollection2.Add(leftMidPoint);
            point3DCollection2.Add(rightMidPoint);
            point3DCollection2.Add(rightUpPoint);
            point3DCollection2.Add(leftUpPoint);
            Polyline polyline2 = GetTextArea(matrix3D, point3DCollection2);

            TypedValueList typedValues = new TypedValueList();
            typedValues.Add(typeof(DBText));
            SelectionFilter selectionFilter = new SelectionFilter(typedValues);

            PromptSelectionResult promptSelectionResult1 = editor.SelectWindowPolygon(polyline1.GetPoint3DCollection(), selectionFilter);
            if (promptSelectionResult1.Status != PromptStatus.OK) return null;
            SelectionSet selectionSet1 = promptSelectionResult1.Value;
            DBText dBText1 = transaction.GetObject(selectionSet1.GetObjectIds()[0], OpenMode.ForRead) as DBText;
            if (dBText1 == null) return null;
            diagramPanel.Load = dBText1.TextString;

            PromptSelectionResult promptSelectionResult2 = editor.SelectWindowPolygon(polyline2.GetPoint3DCollection(), selectionFilter);
            if (promptSelectionResult2.Status != PromptStatus.OK) return null;
            SelectionSet selectionSet2 = promptSelectionResult2.Value;
            DBText dBText2 = transaction.GetObject(selectionSet2.GetObjectIds()[0], OpenMode.ForRead) as DBText;
            if (dBText2 == null) return null;
            diagramPanel.Name = dBText2.TextString;

            return diagramPanel;
        }

        private Polyline GetTextArea(Matrix3d matrix3D, Point3dCollection point3DCollection)
        {
            Polyline polyline = new Polyline();
            for (int i = 0; i < point3DCollection.Count; i++)
            {
                polyline.AddVertexAt(i, new Point2d(0, 0), 0, 0, 0);
            }
            polyline.Closed = true;
            polyline.Normal = Vector3d.ZAxis;
            polyline.Elevation = 0;
            for (int i = 0; i < point3DCollection.Count; i++)
            {
                polyline.SetPointAt(i, point3DCollection[i].ToPoint2d());
            }
            polyline.TransformBy(matrix3D);
            return polyline;
        }

        #endregion


        //#region 1.2 计算电流

        //[CommandMethod("CalculateCurrent")]
        //public void CalculateCurrent()
        //{
        //    Document document = Application.DocumentManager.MdiActiveDocument;
        //    Database database = document.Database;
        //    Editor editor = document.Editor;

        //    using (Transaction transaction = document.TransactionManager.StartTransaction())
        //    {
        //        PromptPointOptions promptPointOptions = new PromptPointOptions("选择基点");
        //        PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);
        //        if (promptPointResult.Status == PromptStatus.OK)
        //        {
        //            Point3d originalPoint3d = promptPointResult.Value;

        //            Point3d point3d1 = originalPoint3d + new Vector3d(5, -7.5, 0);
        //            DBText text1 = new DBText
        //            {
        //                Position = point3d1,
        //                HorizontalMode = TextHorizontalMode.TextLeft,
        //                VerticalMode = TextVerticalMode.TextBase,
        //                TextString = $"Pe={elecSystemControl.elecSystemControlViewModel.Pe}kw",
        //                Height = 2.5
        //            };
        //            text1.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

        //            Point3d point3d2 = originalPoint3d + new Vector3d(5, -12.5, 0);
        //            DBText text2 = new DBText
        //            {
        //                Position = point3d2,
        //                HorizontalMode = TextHorizontalMode.TextLeft,
        //                VerticalMode = TextVerticalMode.TextBase,
        //                TextString = $"Kx={elecSystemControl.elecSystemControlViewModel.Kx}",
        //                Height = 2.5
        //            };
        //            text2.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

        //            Point3d point3d3 = originalPoint3d + new Vector3d(5, -17.5, 0);
        //            DBText text3 = new DBText
        //            {
        //                Position = point3d3,
        //                HorizontalMode = TextHorizontalMode.TextLeft,
        //                VerticalMode = TextVerticalMode.TextBase,
        //                TextString = $"cosø={elecSystemControl.elecSystemControlViewModel.Cosø}",
        //                Height = 2.5
        //            };
        //            text3.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

        //            Point3d point3d4 = originalPoint3d + new Vector3d(5, -22.5, 0);

        //            DBText text4 = new DBText
        //            {
        //                Position = point3d4,
        //                HorizontalMode = TextHorizontalMode.TextLeft,
        //                VerticalMode = TextVerticalMode.TextBase,
        //                TextString = $"Ij={elecSystemControl.elecSystemControlViewModel.Ij}A",
        //                Height = 2.5
        //            };
        //            text4.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);


        //            database.AddToModelSpace(text1, text2, text3, text4);
        //        }
        //        transaction.Commit();
        //    }
        //}

        //#endregion

        //#region 1.3 打断线
        //[CommandMethod("CutLine")]
        //public void CutLine()
        //{
        //    Document document = Application.DocumentManager.MdiActiveDocument;
        //    Database database = document.Database;
        //    Editor editor = document.Editor;

        //    using (Transaction transaction = document.TransactionManager.StartTransaction())
        //    {
        //        TypedValueList values = new TypedValueList { typeof(Polyline), typeof(Line) };
        //        SelectionFilter selectionFilter = new SelectionFilter(values);
        //        PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
        //        {
        //            SingleOnly = true,
        //            MessageForAdding = "\n 选择需要打断的直线或者多段线"
        //        };
        //        var promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);
        //        if (promptSelectionResult.Status == PromptStatus.OK)
        //        {
        //            SelectionSet selectionSet = promptSelectionResult.Value;
        //            ObjectId objectId = selectionSet[0].ObjectId;
        //            Curve curve = objectId.GetObject(OpenMode.ForRead) as Curve;
        //            if (curve != null)
        //            {
        //                PromptPointOptions promptPointOptions = new PromptPointOptions("\n 选择打断点");
        //                PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);
        //                if (promptPointResult.Status == PromptStatus.OK)
        //                {
        //                    Point3d point3d = promptPointResult.Value;
        //                    double d = curve.GetParameterAtPoint(point3d);
        //                    Curve3d curve3d = curve.GetGeCurve();
        //                    double d1 = curve3d.GetParameterAtLength(d,
        //                        commonControl.CommonControlViewModel.Distance *
        //                        commonControl.CommonControlViewModel.BlockReferenceScale, true, 1e-6);
        //                    double d2 = curve3d.GetParameterAtLength(d,
        //                        commonControl.CommonControlViewModel.Distance *
        //                        commonControl.CommonControlViewModel.BlockReferenceScale, false, 1e-6);
        //                    Point3d point3d1 = curve3d.EvaluatePoint(d1);
        //                    Point3d point3d2 = curve3d.EvaluatePoint(d2);
        //                    Point3dCollection point3dCollection = new Point3dCollection() { point3d1, point3d2 };
        //                    if (curve is Polyline polyLine)
        //                    {
        //                        DBObjectCollection dbObjectCollection = polyLine.GetSplitCurves(point3dCollection);


        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //#endregion

        //[CommandMethod("TJAD_line")]
        //public void TJAD_line()
        //{
        //    Document document = Application.DocumentManager.CurrentDocument;
        //    Database database = document.Database;
        //    Editor editor = document.Editor;
        //    string layername = "E-WIRE-1";
        //    short colorIndex = 1;

        //    using (Transaction transaction = database.TransactionManager.StartTransaction())
        //    {
        //        LayerTable layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
        //        LayerTableRecord layerTableRecord = new LayerTableRecord();
        //        if (layerTable.Has(layername))
        //        {
        //            if (database.Clayer != layerTable[layername]) database.Clayer = layerTable[layername];
        //            layerTableRecord = (LayerTableRecord)layerTable[layername].GetObject(OpenMode.ForRead);
        //        }
        //        else
        //        {
        //            layerTableRecord.Name = layername;
        //            layerTable.UpgradeOpen();
        //            layerTable.Add(layerTableRecord);
        //            database.TransactionManager.AddNewlyCreatedDBObject(layerTableRecord, true);
        //            layerTable.DowngradeOpen();
        //            database.Clayer = layerTable[layername];
        //        }
        //        layerTableRecord.UpgradeOpen();
        //        layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByColor, (short)(colorIndex % 256));
        //        layerTableRecord.DowngradeOpen();
        //        document.SendStringToExecute("Line ", true, false, true);
        //        transaction.Commit();
        //    }
        //}

    }
}
