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
            BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
            Matrix3d matrixd = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;

            //E-配电箱-竖向干线
            string blockName1 = "E-配电箱-竖向干线";
            //E-配电箱-电气火灾监控器
            string blockName2 = "E-配电箱-电气火灾监控器";
            Point3d point3D2 = new Point3d();
            //E-配电箱-消防设备电源监控系统图
            string blockName3 = "E-配电箱-消防设备电源监控系统图";
            Point3d point3D3 = new Point3d();
            //E-配电箱-电力能量监控系统
            string blockName4 = "E-配电箱-电力能量监控系统";
            PromptPointOptions pointOptions = new PromptPointOptions("\n请选择起始点：");
            PromptPointResult pointResult = editor.GetPoint(pointOptions);
            if (pointResult.Status != PromptStatus.OK) return;
            Point3d point3D4 = pointResult.Value;

            //在第几层敷设主干路由
            string pathLevel = "B1";

            //获取Block文件夹路径
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string AssemblyDirectory = Path.GetDirectoryName(path);
            string directory = Path.Combine(AssemblyDirectory, "Block");

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

            if (diagramPanels.Count == 0) return;
            List<DiagramPanel> ALPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][L][^TE]")).ToList();
            List<DiagramPanel> ALTPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][L][T]")).ToList();
            List<DiagramPanel> ALEPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][L][E]")).ToList();
            List<DiagramPanel> APPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][P][^TE]")).ToList();
            List<DiagramPanel> APTPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][P][T]")).ToList();
            List<DiagramPanel> APEPanels = diagramPanels.Where(p => Regex.IsMatch(p.Name, "[A][P][E]")).ToList();

            //获取配电划分区域
            List<string> tempAreas = new List<string>();
            foreach (var item in diagramPanels)
            {
                tempAreas.Add(Regex.Match(item.Name, "^[0-9]").Value);
            }
            List<string> areas = tempAreas.Distinct().ToList();
            areas.Sort();

            //获取楼层集合并排序
            List<string> tempFloors = new List<string>();
            foreach (var item in ALPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][L]\\w*\\d*").Value.Substring(2));
            }
            foreach (var item in ALTPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][L][T]\\w*\\d*").Value.Substring(3));
            }
            foreach (var item in ALEPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][L][E]\\w*\\d*").Value.Substring(3));
            }
            foreach (var item in APPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][P]\\w*\\d*").Value.Substring(2));
            }
            foreach (var item in APTPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][P][T]\\w*\\d*").Value.Substring(3));
            }
            foreach (var item in APEPanels)
            {
                tempFloors.Add(Regex.Match(item.Name, "[A][P][E]\\w*\\d*").Value.Substring(3));
            }

            List<string> floors = tempFloors.Distinct().ToList();
            floors.Sort();

            for (int i = 0; i < floors.Count-1; i++)
            {
                for (int j = i+1; j < floors.Count-i; j++)
                {
                    string temp = null;
                    string a = floors[i];
                    string b = floors[j];
                    if (a.Contains("B") && b.Contains("B"))
                    {
                        string a1 = Regex.Match(a, "[B]\\d*").Value.Substring(1);
                        string b1 = Regex.Match(b, "[B]\\d*").Value.Substring(1);

                        string a2 = Regex.Match(a, "[B][A-Za-z]").Value.Substring(1);
                        string b2 = Regex.Match(b, "[B][A-Za-z]").Value.Substring(1);

                        if (!string.IsNullOrEmpty(a1) && !string.IsNullOrEmpty(b1))
                        {
                            if (int.Parse(a1) < int.Parse(b1))
                            {
                                temp = a;
                                a = b;
                                b = temp;
                            }
                        }
                        else if(string.IsNullOrEmpty(a1) && !string.IsNullOrEmpty(b1))
                        {
                            temp = a;
                            a = b;
                            b = temp;
                        }
                        else if(!string.IsNullOrEmpty(a2) && string.IsNullOrEmpty(b2))
                        {
                            temp = a;
                            a = b;
                            b = temp;
                        }
                        else if (!string.IsNullOrEmpty(a2) && !string.IsNullOrEmpty(b1))
                        {
                            temp = a;
                            a = b;
                            b = temp;
                        }
                    }
                    else if(!a.Contains("B") && b.Contains("B"))
                    {
                        temp = a;
                        a = b;
                        b = temp;
                    }
                    else if(!a.Contains("B") && !b.Contains("B"))
                    {
                        string a1 = Regex.Match(a, "\\d*").Value;
                        string b1 = Regex.Match(b, "\\d*").Value;

                        string a2 = Regex.Match(a, "[A-Za-z]*").Value;
                        string b2 = Regex.Match(b, "[A-Za-z]*").Value;

                        if (!string.IsNullOrEmpty(a1) && !string.IsNullOrEmpty(b1))
                        {
                            if (int.Parse(a1) > int.Parse(b1))
                            {
                                temp = a;
                                a = b;
                                b = temp;
                            }
                        }
                        else if (string.IsNullOrEmpty(a1))
                        {
                            temp = a;
                            a = b;
                            b = temp;
                        }
                        else if (!string.IsNullOrEmpty(a2) && string.IsNullOrEmpty(b2))
                        {
                            temp = a;
                            a = b;
                            b = temp;
                        }
                    }
                }
            }

            string layerName = "E-ANNO-TEXT";
            int colorIndex = 7;
            //SetLayer(database, "E-ANNO-TEXT", 7);
            LayerTable layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            LayerTableRecord layerTableRecord = new LayerTableRecord();
            if (!layerTable.Has(layerName))
            {
                
                layerTableRecord.Name = layerName;
                layerTable.UpgradeOpen();
                layerTable.Add(layerTableRecord);
                transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
                layerTable.DowngradeOpen();
            }

            
            layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
            

            ObjectId layerId = layerTable[layerName];
            if (database.Clayer != layerId)
            {
                database.Clayer = layerId;
            }


            //生成电力能量系统图
            string file1 = Path.Combine(directory, "多功能表2.dwg");
            if (!File.Exists(file1)) return;
            string file2 = Path.Combine(directory, "电力能量监控系统图.dwg");
            if (!File.Exists(file2)) return;

            AddElectricalPowerSystem(database, point3D4, file2);

            Point3d xMaxPoint3d = GetStartPoint(point3D4);
            for (int i = 0; i < areas.Count; i++)
            {
                Point3d areaStartPoint = GetAreaStartPoint(point3D4, xMaxPoint3d);
                for (int j = 0; j <= floors.Count; j++)
                {
                    if (floors[j] == pathLevel)
                    {
                        j++;
                    }
                    Point3d floorStartPoint = areaStartPoint + new Vector3d(0, 2000 * j, 0);
                    int l = 0;
                    for (int k = 0; k < ALPanels.Count; k++)
                    {
                        string name = ALPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][L]\\w*\\d*").Value.Substring(2))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X> xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                    for (int k = 0; k < ALTPanels.Count; k++)
                    {
                        string name = ALTPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][L][T]\\w*\\d*").Value.Substring(3))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X > xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                    for (int k = 0; k < ALEPanels.Count; k++)
                    {
                        string name = ALEPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][L][E]\\w*\\d*").Value.Substring(3))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X > xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                    for (int k = 0; k < APPanels.Count; k++)
                    {
                        string name = APPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][P]\\w*\\d*").Value.Substring(2))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X > xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                    for (int k = 0; k < APTPanels.Count; k++)
                    {
                        string name = APTPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][P][T]\\w*\\d*").Value.Substring(3))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X > xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                    for (int k = 0; k < APEPanels.Count; k++)
                    {
                        string name = APEPanels[k].Name;
                        if (areas[i] == Regex.Match(name, "^[0-9]").Value && floors[j] == Regex.Match(name, "[A][L][E]\\w*\\d*").Value.Substring(3))
                        {
                            Point3d tempPoint3d = floorStartPoint + new Vector3d(l * 2100, 0, 0);
                            if (tempPoint3d.X > xMaxPoint3d.X)
                            {
                                xMaxPoint3d = tempPoint3d;
                            }
                            l++;
                            //绘制图元
                            AddPanel(database, tempPoint3d, name, path);
                        }
                    }
                }
            }

            //绘制楼层线
            string linetypeName = "DASHED";
            LinetypeTable linetypeTable = (LinetypeTable)database.LinetypeTableId.GetObject(OpenMode.ForRead);
            if (!linetypeTable.Has(linetypeName))
            {
                database.LoadLineTypeFile(linetypeName, "acad.lin");
            }

            modelSpace.UpgradeOpen();
            for (int i = 0; i <= floors.Count; i++)
            {
                if (floors[i] == pathLevel)
                {
                    i++;
                }
                Point3d startPoint = point3D4 + new Vector3d(18150, 4000, 0) + new Vector3d(0, 2000 * i, 0);
                Point3d endPoint = new Point3d(xMaxPoint3d.X, startPoint.Y, startPoint.Z) + new Vector3d(4000, 0, 0);
                Line line = new Line(startPoint, endPoint);
                line.Linetype = "DASHED";
                line.ColorIndex = 8;
                modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line,true); ;
            }
            modelSpace.DowngradeOpen();

            transaction.Commit();
        }

        void AddElectricalPowerSystem(Database database, Point3d point, string file)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(point));
            using (Database tmepDatabase = new Database(false, true))
            using (Transaction tempTransaction = tmepDatabase.TransactionManager.StartOpenCloseTransaction())
            {
                tmepDatabase.ReadDwgFile(file, FileShare.Read, true, null);
                tmepDatabase.CloseInput(true);
                database.Insert(matrix3D, tmepDatabase, false);
                tempTransaction.Commit();
            }
        }

        void AddPanel(Database database,Point3d point, string name, string file)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(point));
            using (Database tmepDatabase = new Database(false, true))
            using (Transaction tempTransaction = tmepDatabase.TransactionManager.StartOpenCloseTransaction())
            {
                tmepDatabase.ReadDwgFile(file, FileShare.Read, true, null);
                tmepDatabase.CloseInput(true);

                BlockTable tempBlockTable = tempTransaction.GetObject(tmepDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord tempModelSpace = tempTransaction.GetObject(tempBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                List<DBText> dBTexts = new List<DBText>();
                foreach (var item in tempModelSpace)
                {
                    DBText dBText = tempTransaction.GetObject(item, OpenMode.ForRead) as DBText;
                    if (dBText != null)
                    {
                        dBTexts.Add(dBText);
                    }
                }
                dBTexts = dBTexts.OrderByDescending(d => d.Position.Y).ToList();
                dBTexts[0].UpgradeOpen();
                dBTexts[0].TextString = name;
                dBTexts[0].DowngradeOpen();
                tempTransaction.Commit();

                database.Insert(matrix3D, tmepDatabase, false);
            }
        }

        Point3d GetStartPoint(Point3d startPoint)
        {
            return startPoint + new Vector3d(18150, 4000, 0) + new Vector3d(2000, 500, 0);
        }

        //i区起始点
        Point3d GetAreaStartPoint(Point3d startPoint, Point3d tempMaxPoint)
        {
            Point3d tempPoint = GetStartPoint(startPoint);
            return new Point3d(tempMaxPoint.X, tempPoint.Y, tempPoint.Z) + new Vector3d(4000, 0, 0);
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
            transaction.Commit();
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

        void SetLayer(Database db, string layerName, int colorIndex)
        {
            db.AddLayer(layerName);
            db.SetLayerColor(layerName, (short)colorIndex);
            db.SetCurrentLayer(layerName);
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
