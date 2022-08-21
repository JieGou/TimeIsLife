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

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.ElectricalCommand))]

namespace TimeIsLife.CADCommand
{
    class ElectricalCommand
    {
        #region 0.0 创建工具栏
        //创建面板
        static PaletteSet paletteSet;
        static CommonControl commonControl;
        static ElecSystemControl elecSystemControl;

        [CommandMethod("FF_Tools")]
        public static void FF_Tools()
        {
            if (paletteSet == null)//如果面板没有被创建
            {

                paletteSet = new PaletteSet("时间就是生命");
                paletteSet.Dock = DockSides.Left;
                
                paletteSet.AddVisual("电气", commonControl = new CommonControl());
            }
            paletteSet.Visible = true;//面板可见            
        }
        #endregion

        #region 1.1 功率求和


        //求和功率，选择多个表示功率的单行文字，文字内容由数字+“kw”组成，去除“kw”后的数字求和

        [CommandMethod("SumPower")]
        public void SumPower()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = document.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions options = new PromptSelectionOptions { MessageForAdding = "\n请选择要统计的功率:" };
                TypedValueList values = new TypedValueList { typeof(DBText) };
                PromptSelectionResult result = editor.GetSelection(options, values);

                if (result.Status == PromptStatus.OK)
                {
                    SelectionSet set = result.Value;

                    decimal d = 0;

                    foreach (var objectId in set.GetObjectIds())
                    {

                        DBText dBText = (DBText)objectId.GetObject(OpenMode.ForRead);
                        string text = dBText.TextString;
                        d += GetNumber(text);
                    }

                    elecSystemControl.elecSystemControlViewModel.Pe = (double)d;

                    PromptSelectionOptions options1 = new PromptSelectionOptions { MessageForAdding = "\n请选择要修改的功率:", SingleOnly = true };
                    PromptSelectionResult result1 = editor.GetSelection(options1, values);
                    if (result1.Status == PromptStatus.OK)
                    {
                        SelectionSet set1 = result1.Value;
                        DBText dBText = (DBText)set1.GetObjectIds().First().GetObject(OpenMode.ForWrite);
                        dBText.TextString = $"{d.ToString()}kW";
                        dBText.DowngradeOpen();
                    }
                    transaction.Commit();
                }

            }
        }

        /// <summary>
        /// 获取字符串中的数字 
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>数字</returns>
        public decimal GetNumber(string str)
        {
            decimal result = 0;
            if (str != null && str != string.Empty)
            {
                // 正则表达式剔除非数字字符（不包含小数点.）
                //str = Regex.Replace(str, @"[^/d./d]", "");
                str = Regex.Replace(str, @"[^\d.\d]", "");
                // 如果是数字，则转换为decimal类型
                if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
                {
                    result = decimal.Parse(str);
                }
            }
            return result;
        }

        #endregion

        #region 1.2 计算电流

        [CommandMethod("CalculateCurrent")]
        public void CalculateCurrent()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = document.TransactionManager.StartTransaction())
            {
                PromptPointOptions promptPointOptions = new PromptPointOptions("选择基点");
                PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);
                if (promptPointResult.Status == PromptStatus.OK)
                {
                    Point3d originalPoint3d = promptPointResult.Value;

                    Point3d point3d1 = originalPoint3d + new Vector3d(5, -7.5, 0);
                    DBText text1 = new DBText
                    {
                        Position = point3d1,
                        HorizontalMode = TextHorizontalMode.TextLeft,
                        VerticalMode = TextVerticalMode.TextBase,
                        TextString = $"Pe={elecSystemControl.elecSystemControlViewModel.Pe}kw",
                        Height = 2.5
                    };
                    text1.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

                    Point3d point3d2 = originalPoint3d + new Vector3d(5, -12.5, 0);
                    DBText text2 = new DBText
                    {
                        Position = point3d2,
                        HorizontalMode = TextHorizontalMode.TextLeft,
                        VerticalMode = TextVerticalMode.TextBase,
                        TextString = $"Kx={elecSystemControl.elecSystemControlViewModel.Kx}",
                        Height = 2.5
                    };
                    text2.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

                    Point3d point3d3 = originalPoint3d + new Vector3d(5, -17.5, 0);
                    DBText text3 = new DBText
                    {
                        Position = point3d3,
                        HorizontalMode = TextHorizontalMode.TextLeft,
                        VerticalMode = TextVerticalMode.TextBase,
                        TextString = $"cosø={elecSystemControl.elecSystemControlViewModel.Cosø}",
                        Height = 2.5
                    };
                    text3.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);

                    Point3d point3d4 = originalPoint3d + new Vector3d(5, -22.5, 0);

                    DBText text4 = new DBText
                    {
                        Position = point3d4,
                        HorizontalMode = TextHorizontalMode.TextLeft,
                        VerticalMode = TextVerticalMode.TextBase,
                        TextString = $"Ij={elecSystemControl.elecSystemControlViewModel.Ij}A",
                        Height = 2.5
                    };
                    text4.Scale(originalPoint3d, elecSystemControl.elecSystemControlViewModel.BlockReferenceScale);


                    database.AddToModelSpace(text1, text2, text3, text4);
                }
                transaction.Commit();
            }
        }

        #endregion

        #region 1.3 打断线
        [CommandMethod("CutLine")]
        public void CutLine()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = document.TransactionManager.StartTransaction())
            {
                TypedValueList values = new TypedValueList { typeof(Polyline), typeof(Line) };
                SelectionFilter selectionFilter = new SelectionFilter(values);
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                {
                    SingleOnly = true,
                    MessageForAdding = "\n 选择需要打断的直线或者多段线"
                };
                var promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);
                if (promptSelectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    ObjectId objectId = selectionSet[0].ObjectId;
                    Curve curve = objectId.GetObject(OpenMode.ForRead) as Curve;
                    if (curve != null)
                    {
                        PromptPointOptions promptPointOptions = new PromptPointOptions("\n 选择打断点");
                        PromptPointResult promptPointResult = editor.GetPoint(promptPointOptions);
                        if (promptPointResult.Status == PromptStatus.OK)
                        {
                            Point3d point3d = promptPointResult.Value;
                            double d = curve.GetParameterAtPoint(point3d);
                            Curve3d curve3d = curve.GetGeCurve();
                            double d1 = curve3d.GetParameterAtLength(d,
                                commonControl.CommonControlViewModel.Distance *
                                commonControl.CommonControlViewModel.BlockReferenceScale, true, 1e-6);
                            double d2 = curve3d.GetParameterAtLength(d,
                                commonControl.CommonControlViewModel.Distance *
                                commonControl.CommonControlViewModel.BlockReferenceScale, false, 1e-6);
                            Point3d point3d1 = curve3d.EvaluatePoint(d1);
                            Point3d point3d2 = curve3d.EvaluatePoint(d2);
                            Point3dCollection point3dCollection = new Point3dCollection() { point3d1, point3d2 };
                            if (curve is Polyline polyLine)
                            {
                                DBObjectCollection dbObjectCollection = polyLine.GetSplitCurves(point3dCollection);


                            }
                        }
                    }
                }
            }
        }
        #endregion

        [CommandMethod("TJAD_line")]
        public void TJAD_line()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string layername = "E-WIRE-1";
            short colorIndex = 1;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                LayerTableRecord layerTableRecord = new LayerTableRecord();
                if (layerTable.Has(layername))
                {
                    if (database.Clayer != layerTable[layername]) database.Clayer = layerTable[layername];
                    layerTableRecord = (LayerTableRecord)layerTable[layername].GetObject(OpenMode.ForRead);
                }
                else
                {
                    layerTableRecord.Name = layername;
                    layerTable.UpgradeOpen();
                    layerTable.Add(layerTableRecord);
                    database.TransactionManager.AddNewlyCreatedDBObject(layerTableRecord, true);
                    layerTable.DowngradeOpen();
                    database.Clayer = layerTable[layername];
                }
                layerTableRecord.UpgradeOpen();
                layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByColor, (short)(colorIndex % 256));
                layerTableRecord.DowngradeOpen();
                document.SendStringToExecute("Line ", true, false, true);
                transaction.Commit();
            }
        }

    }
}
