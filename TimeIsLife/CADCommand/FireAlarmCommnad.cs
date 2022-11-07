using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

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
using System.Windows.Media.Media3D;

using TimeIsLife.Helper;
using TimeIsLife.ViewModel;

using static TimeIsLife.CADCommand.FireAlarmCommnad;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.FireAlarmCommnad))]

namespace TimeIsLife.CADCommand
{
    class FireAlarmCommnad
    {
        
        #region F6_CheckBeam
        [CommandMethod("F6_CheckBeam")]
        public void F6_CheckBeam()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：通过盈建科模型生成的点位布置图，核查扣除板厚不超过600的梁腔，蓝色表示梁净高<=600";
            string s2 = "\n操作方法：运行命令";
            string s3 = "\n注意事项：";
            editor.WriteMessage(s1 + s2 + s3);

            string slab = "slab";
            string beam = "beam";

            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    List<int> slabList = new List<int>();
                    List<int> beamList = new List<int>();
                    //梁腔小于600
                    List<int> beamList1 = new List<int>();
                    //梁腔大于600
                    List<int> beamList2 = new List<int>();
                    //需要检测
                    List<int> beamList3 = new List<int>();

                    LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (var item in layerTable)
                    {
                        LayerTableRecord layerTableRecord = transaction.GetObject(item, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord == null) continue;
                        string name = layerTableRecord.Name;
                        if (name.Contains(slab))
                        {
                            int n = int.Parse(Regex.Match(name, @"\d+").Value);
                            if (0 < n && n < 1000)
                            {
                                slabList.Add(n);
                            }
                        }
                        if (name.Contains(beam))
                        {
                            int n = int.Parse(Regex.Match(name, @"\d+").Value);
                            if (0 < n && n < 5000)
                            {
                                beamList.Add(n);
                            }
                        }
                    }
                    //初步排除梁
                    foreach (var b in beamList)
                    {
                        if (b - slabList.Min() <= 600)
                        {
                            beamList1.Add(b);
                        }
                        else if (b - slabList.Max() > 600)
                        {
                            beamList2.Add(b);
                        }
                        else
                        {
                            beamList3.Add(b);
                        }
                    }


                    foreach (var item in layerTable)
                    {
                        LayerTableRecord layerTableRecord = transaction.GetObject(item, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord == null) continue;
                        string name = layerTableRecord.Name;
                        if (name.Contains(beam))
                        {
                            if (beamList1.Contains(int.Parse(Regex.Match(name, @"\d+").Value)))
                            {
                                layerTableRecord.UpgradeOpen();
                                layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                                layerTableRecord.DowngradeOpen();
                            }
                            else if (beamList2.Contains(int.Parse(Regex.Match(name, @"\d").Value)))
                            {
                                layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                            }
                            else if (beamList3.Contains(int.Parse(Regex.Match(name, @"\d+").Value)))
                            {
                                TypedValueList beams = new TypedValueList();
                                beams.Add(DxfCode.LayerName, name);
                                SelectionFilter beamFilter = new SelectionFilter(beams);
                                PromptSelectionResult beamPsr = editor.SelectAll(beamFilter);
                                if (beamPsr.Status == PromptStatus.OK)
                                {
                                    SelectionSet beamss = beamPsr.Value;
                                    foreach (var beamId in beamss.GetObjectIds())
                                    {
                                        Polyline beamPolyline = transaction.GetObject(beamId, OpenMode.ForRead) as Polyline;


                                        if (beamPolyline == null) continue;
                                        LineSegment3d lineSegment3D = beamPolyline.GetLineSegmentAt(0);
                                        Point3d point3D = lineSegment3D.MidPoint;
                                        Point3dCollection point3DCollection = GetPoint3dCollection(point3D);

                                        TypedValueList typedValues = new TypedValueList();
                                        typedValues.Add(typeof(Polyline));
                                        SelectionFilter slabFilter = new SelectionFilter(typedValues);
                                        PromptSelectionResult slabPsr = editor.SelectCrossingPolygon(point3DCollection, slabFilter);
                                        if (slabPsr.Status == PromptStatus.OK)
                                        {
                                            SelectionSet slabss = slabPsr.Value;
                                            foreach (var slabId in slabss.GetObjectIds())
                                            {
                                                Polyline slabPolyline = transaction.GetObject(beamId, OpenMode.ForRead) as Polyline;
                                                //真，梁腔高度<=600
                                                //假，梁腔高度>600
                                                bool bo = true;
                                                int a = int.Parse(Regex.Match(beamPolyline.Layer, @"\d+").Value);
                                                int b = int.Parse(Regex.Match(slabPolyline.Layer, @"\d+").Value);
                                                if (a - b > 600)
                                                {
                                                    bo = false;
                                                }
                                                if (bo)
                                                {
                                                    beamPolyline.UpgradeOpen();
                                                    beamPolyline.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                                                    beamPolyline.DowngradeOpen();
                                                }
                                                else
                                                {
                                                    beamPolyline.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }
        }

        private Point3dCollection GetPoint3dCollection(Point3d point3D)
        {
            Point3dCollection point3DCollection = new Point3dCollection();
            point3DCollection.Add(point3D + new Vector3d(-100, -100, 0));
            point3DCollection.Add(point3D + new Vector3d(100, -100, 0));
            point3DCollection.Add(point3D + new Vector3d(100, 100, 0));
            point3DCollection.Add(point3D + new Vector3d(-100, 100, 0));
            return point3DCollection;
        }
        #endregion

        #region FF_FAS
        [CommandMethod("FF_FAS")]
        public void FF_FAS()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：生成火灾自动报警系统图";
            string s2 = "\n操作方法：选择表示防火分区多段线，选择一个基点放置系统图，基点为生成系统图区域左下角点";
            string s3 = "\n注意事项：防火分区多段线需要单独图层，防火分区多段线内需要文字标注防火分区编号，文字和多段线需要同一个图层";
            editor.WriteMessage(s1 + s2 + s3);

            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string AssemblyDirectory = Path.GetDirectoryName(path);
                    string directory = Path.Combine(AssemblyDirectory, "FA");
                    string name = "";
                    Point3d basePoint3d = new Point3d();


                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(typeof(Polyline));
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
                        name = polyline.Layer;

                        PromptPointResult ppr = editor.GetPoint(new PromptPointOptions("\n请选择系统图放置点："));
                        if (ppr.Status == PromptStatus.OK) basePoint3d = ppr.Value;

                        //选择选定图上的所有多段线
                        TypedValueList typedValues1 = new TypedValueList();
                        typedValues1.Add(DxfCode.LayerName, name);
                        typedValues1.Add(typeof(Polyline));
                        SelectionFilter layerSelectionFilter = new SelectionFilter(typedValues1);
                        PromptSelectionResult psr = editor.SelectAll(layerSelectionFilter);
                        Dictionary<Polyline, KeyValuePair<DBText, Point3dCollection>> keyValuePairs = new Dictionary<Polyline, KeyValuePair<DBText, Point3dCollection>>();

                        if (psr.Status == PromptStatus.OK)
                        {
                            SelectionSet ss = psr.Value;

                            foreach (var id in ss.GetObjectIds())
                            {
                                Polyline polyline1 = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                                if (polyline1 != null)
                                {
                                    Point3dCollection point3DCollection = new Point3dCollection();
                                    for (int i = 0; i < polyline1.NumberOfVertices; i++)
                                    {
                                        point3DCollection.Add(polyline1.GetPoint3dAt(i));
                                    }

                                    List<string> names = new List<string>();
                                    List<BlockReference> blockReferences = new List<BlockReference>();

                                    TypedValueList typedValues2 = new TypedValueList();
                                    typedValues2.Add(DxfCode.LayerName, name);
                                    typedValues2.Add(typeof(DBText));
                                    SelectionFilter dbTextSelectionFilter = new SelectionFilter(typedValues2);

                                    PromptSelectionResult dbTextPromptSelectionResult = editor.SelectWindowPolygon(point3DCollection, dbTextSelectionFilter);
                                    if (dbTextPromptSelectionResult.Status == PromptStatus.OK)
                                    {
                                        SelectionSet selectionSet1 = dbTextPromptSelectionResult.Value;
                                        DBText dBText = transaction.GetObject(selectionSet1.GetObjectIds().First(), OpenMode.ForRead) as DBText;
                                        if (dBText == null)
                                        {
                                            editor.WriteMessage("防火分区编号不全！");
                                            transaction.Abort();
                                        }

                                        KeyValuePair<DBText, Point3dCollection> keyValuePair = new KeyValuePair<DBText, Point3dCollection>(dBText, point3DCollection);
                                        keyValuePairs.Add(polyline1, keyValuePair);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                        }


                        //循环防火分区
                        for (int i = 0; i < keyValuePairs.Count; i++)
                        {

                            string area = keyValuePairs.ElementAt(i).Value.Key.TextString;
                            Point3d layoutPoint3d = basePoint3d + new Vector3d(0, 2500 * i, 0);
                            Point3d tempPoint3d = new Point3d(layoutPoint3d.X, layoutPoint3d.Y, layoutPoint3d.Z);
                            List<string> blockNames = new List<string>();
                            List<BlockReference> blockReferences = new List<BlockReference>();

                            TypedValueList typedValues3 = new TypedValueList();
                            typedValues3.Add(typeof(BlockReference));
                            SelectionFilter blockReferenceSelectionFilter = new SelectionFilter(typedValues3);
                            PromptSelectionResult promptSelectionResult2 = editor.SelectWindowPolygon(keyValuePairs.ElementAt(i).Value.Value, blockReferenceSelectionFilter);
                            SelectionSet selectionSet2 = promptSelectionResult2.Value;

                            foreach (var objectId in selectionSet2.GetObjectIds())
                            {
                                BlockReference blockReference = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
                                if (blockReference != null)
                                {
                                    LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                                    if (layerTableRecord != null && layerTableRecord.IsLocked == false)
                                    {
                                        blockNames.Add(blockReference.Name);
                                        blockReferences.Add(blockReference);
                                    }

                                }
                            }

                            blockNames = blockNames.Distinct().OrderBy(b => Regex.Match(b, @"\d{2}").Value).ToList();
                            blockNames = (from blockname in blockNames
                                          where Regex.Matches(blockname, @"FA-\d{2}-").Count == 1
                                          select blockname).ToList();

                            //循环防火分区内的块，生成系统图
                            for (int j = 0; j < blockNames.Count; j++)
                            {

                                string n = (from blockReference in blockReferences
                                            where blockReference.Name == blockNames[j]
                                            select blockReference).ToList().Count.ToString();
                                string blockName = blockNames[j];
                                string file = Path.Combine(directory, blockName + ".dwg");
                                switch (blockName)
                                {
                                    case "FA-01-接线端子箱":
                                        AddElement2500(database, tempPoint3d, file, area);
                                        tempPoint3d = tempPoint3d + new Vector3d(2500, 0, 0);
                                        break;
                                    case "FA-02-带消防电话插孔的手动报警按钮":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-03-火灾报警电话机":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-04-声光警报器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-05-3W火灾警报扬声器(挂墙明装距地2.4m)":
                                        AddElement1600(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(1600, 0, 0);
                                        break;
                                    case "FA-06-3W火灾警报扬声器(吸顶安装)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-07-消火栓起泵按钮":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-08-智能型点型感烟探测器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-09-智能型点型感温探测器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-10-防火阀(70°C熔断关闭)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-11-防火阀(280°C熔断关闭)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-12-电动排烟阀(常闭)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-13-电动排烟阀(常开)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-14-常闭正压送风口":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-15-防火卷帘控制箱":
                                        AddElement2(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-16-电动挡烟垂壁控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-17-水流指示器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-18-信号阀":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-19-智能线型红外光束感烟探测器（发射端）":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-20-智能线型红外光束感烟探测器（接收端）":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-21-电动排烟窗控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-22-消防电梯控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-23-电梯控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-24-湿式报警阀组":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-25-预作用报警阀组":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-26-可燃气体探测控制器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-27-流量开关":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-28-非消防配电箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-29-消防泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-30-喷淋泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-31-消防稳压泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-32-雨淋泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-33-水幕泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-34-消防风机控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d = tempPoint3d + new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-35-就地液位显示盘":
                                        AddElement3(database, layoutPoint3d, file, n);
                                        break;
                                    case "FA-37-区域显示器":
                                        AddElement(database, layoutPoint3d, file, n, -1300);
                                        break;
                                    case "FA-38-防火门控制箱":
                                        AddElement(database, layoutPoint3d, file, n, -2600);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    transaction.Commit();

                }
            }
            catch (System.Exception)
            {

            }
        }
        /// <summary>
        /// 添加接线端子箱
        /// </summary>
        /// <param name="database"></param>
        /// <param name="editor"></param>
        /// <param name="directory"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="basePoint3d"></param>
        /// <param name="name"></param>
        /// <param name="area"></param>
        private void AddElement2500(Database database, Point3d tempPoint3d, string file, string area)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(tempPoint3d));
            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);

                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in blockTableRecord)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    dBTexts = dBTexts.OrderBy(d => d.Position.Y).ToList();
                    dBTexts[0].UpgradeOpen();
                    dBTexts[0].TextString = area;
                    dBTexts[0].DowngradeOpen();
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }



        /// <summary>
        /// 添加壁装消防广播
        /// </summary>
        /// <param name="database"></param>
        /// <param name="editor"></param>
        /// <param name="directory"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="basePoint3d"></param>
        /// <param name="name"></param>
        /// <param name="n"></param>
        private void AddElement1600(Database database, Point3d tempPoint3d, string file, string n)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(tempPoint3d));
            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);

                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in blockTableRecord)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    dBTexts = dBTexts.OrderBy(d => d.Position.Y).ToList();
                    dBTexts[0].UpgradeOpen();
                    dBTexts[0].TextString = n;
                    dBTexts[0].DowngradeOpen();
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }

        /// <summary>
        /// 添加没有模块或模块数量与设备数量一致的图元
        /// </summary>
        /// <param name="database"></param>
        /// <param name="editor"></param>
        /// <param name="directory"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="basePoint3d"></param>
        /// <param name="name"></param>
        /// <param name="n"></param>
        private void AddElement1(Database database, Point3d tempPoint3d, string file, string n)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(tempPoint3d));
            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);

                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in blockTableRecord)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    for (int i = 0; i < dBTexts.Count; i++)
                    {
                        dBTexts[i].UpgradeOpen();
                        dBTexts[i].TextString = n;
                        dBTexts[i].DowngradeOpen();
                    }
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }



        /// <summary>
        /// 添加防火卷帘
        /// </summary>
        /// <param name="database"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="file"></param>
        /// <param name="n"></param>
        private void AddElement2(Database database, Point3d tempPoint3d, string file, string n)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(tempPoint3d));
            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);

                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in blockTableRecord)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    dBTexts = dBTexts.OrderBy(d => d.Position.Y).ToList();
                    dBTexts[0].UpgradeOpen();
                    dBTexts[0].TextString = n;
                    dBTexts[0].DowngradeOpen();
                    dBTexts[1].UpgradeOpen();
                    dBTexts[1].TextString = (2 * int.Parse(n)).ToString();
                    dBTexts[1].DowngradeOpen();
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }

        /// <summary>
        /// 添加液位显示器
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layoutPoint3d"></param>
        /// <param name="file"></param>
        /// <param name="n"></param>
        private void AddElement3(Database database, Point3d layoutPoint3d, string file, string n)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(layoutPoint3d) + new Vector3d(-10000, 0, 0));

            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);
                    transaction.Commit();
                    database.Insert(matrix3D, db, false);
                }
            }
        }

        /// <summary>
        /// 添加区域显示器、防火门监控器
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layoutPoint3d"></param>
        /// <param name="file"></param>
        /// <param name="n"></param>
        /// <param name="num"></param>
        private void AddElement(Database database, Point3d layoutPoint3d, string file, string n, int distance)
        {
            Matrix3d matrix3D = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(layoutPoint3d) + new Vector3d(distance, 0, 0));

            if (File.Exists(file))
            {

                using (Database db = new Database(false, true))
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    db.ReadDwgFile(file, FileShare.Read, true, null);
                    db.CloseInput(true);

                    BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in blockTableRecord)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    for (int i = 0; i < dBTexts.Count; i++)
                    {
                        dBTexts[i].UpgradeOpen();
                        dBTexts[i].TextString = n;
                        dBTexts[i].DowngradeOpen();
                    }
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }

        #endregion

        #region FF_LoadYdbFile

        [CommandMethod("FF_LoadYdbFile")]
        public void FF_LoadYdbFile()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            string filename = GetFilePath();
            if (filename == null) return;
            string directoryName = Path.GetDirectoryName(filename);
            editor.WriteMessage("\n文件名：" + filename);

            #region 数据查询
            var conn = new SQLiteConnection($"Data Source={filename}");

            //查询标高
            string sqlFloor = "SELECT f.ID,f.LevelB,f.Height FROM tblFloor AS f WHERE f.Height != 0";
            var floors = conn.Query<Floor>(sqlFloor);

            //查询梁
            string sqlBeam = "SELECT b.ID,f.ID,f.LevelB,f.Height,bs.ID,bs.kind,bs.ShapeVal,bs.ShapeVal1,g.ID,j1.ID,j1.X,j1.Y,j2.ID,j2.X,j2.Y " +
                "FROM tblBeamSeg AS b " +
                "INNER JOIN tblFloor AS f on f.StdFlrID = b.StdFlrID " +
                "INNER JOIN tblBeamSect AS bs on bs.ID = b.SectID " +
                "INNER JOIN tblGrid AS g on g.ID = b.GridId " +
                "INNER JOIN tblJoint AS j1 on g.Jt1ID = j1.ID " +
                "INNER JOIN tblJoint AS j2 on g.Jt2ID = j2.ID";

            Func<Beam, Floor, BeamSect, Grid, Joint, Joint, Beam> mappingBeam =
                (beam, floor, beamSect, grid, j1, j2) =>
                {
                    grid.Joint1 = j1;
                    grid.Joint2 = j2;
                    beam.Grid = grid;
                    beam.Floor = floor;
                    beam.BeamSect = beamSect;
                    return beam;
                };

            var beams = conn.Query(sqlBeam, mappingBeam);

            //查询板
            string sqlSlab = "SELECT s.ID,s.GridsID,s.VertexX,s.VertexY,s.VertexZ,s.Thickness,f.ID,f.LevelB,f.Height FROM tblSlab  AS s INNER JOIN tblFloor AS f on f.StdFlrID = s.StdFlrID";
            Func<Slab, Floor, Slab> mappingSlab =
                (slab, floor) =>
                {
                    slab.Floor = floor;
                    return slab;
                };
            var slabs = conn.Query(sqlSlab, mappingSlab);

            //查询墙
            string sqlWall = "SELECT w.ID,f.ID,f.LevelB,f.Height,ws.ID,ws.kind,ws.B,g.ID,j1.ID,j1.X,j1.Y,j2.ID,j2.X,j2.Y " +
                "FROM tblWallSeg AS w " +
                "INNER JOIN tblFloor AS f on f.StdFlrID = w.StdFlrID " +
                "INNER JOIN tblWallSect AS ws on ws.ID = w.SectID " +
                "INNER JOIN tblGrid AS g on g.ID = w.GridId " +
                "INNER JOIN tblJoint AS j1 on g.Jt1ID = j1.ID " +
                "INNER JOIN tblJoint AS j2 on g.Jt2ID = j2.ID";

            Func<Wall, Floor, WallSect, Grid, Joint, Joint, Wall> mappingWall =
                (wall, floor, wallSect, grid, j1, j2) =>
                {
                    grid.Joint1 = j1;
                    grid.Joint2 = j2;
                    wall.Grid = grid;
                    wall.Floor = floor;
                    wall.WallSect = wallSect;
                    return wall;
                };
            var walls = conn.Query(sqlWall, mappingWall);
            //关闭数据库
            conn.Close();

            #endregion

            using (Database tempDb = new Database(false, true))
            using (Transaction tempTransaction = tempDb.TransactionManager.StartTransaction())
            {
                try
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    
                    //载入感烟探测器块
                    string blockPath = Path.Combine(Path.GetDirectoryName(path), "Block", "FA-08-智能型点型感烟探测器.dwg");
                    string blockName = SymbolUtilityServices.GetSymbolNameFromPathName(blockPath, "dwg");

                    ObjectId id = ObjectId.Null;
                    tempDb.ReadDwgFile(blockPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
                    tempDb.CloseInput(true);

                    //根据标高生成梁图
                    foreach (var floor in floors)
                    {
                        using (Database db = new Database())
                        using (Transaction transaction = db.TransactionManager.StartTransaction())
                        {
                            try
                            {
                                id = db.Insert(blockName, tempDb, true);

                                #region 生成板及烟感
                                foreach (var slab in slabs)
                                {
                                    bool bo = true;
                                    if (slab.Floor.LevelB != floor.LevelB) continue;

                                    SetLayer(db, $"slab-{slab.Thickness.ToString()}mm", 7);

                                    Polyline polyline = new Polyline();
                                    Point2dCollection point2Ds = GetPoint2Ds(slab);
                                    polyline.CreatePolyline(point2Ds);
                                    db.AddToModelSpace(polyline);

                                    if (slab.Thickness == 0)
                                    {
                                        if (!ElectricalViewModel.electricalViewModel.IsLayoutAtHole) bo = false;
                                    }
                                    if (!bo) continue;
                                    //在板的重心添加感烟探测器
                                    SetLayer(db, $"E-EQUIP-{slab.Thickness.ToString()}", 4);

                                    Point2d p = getCenterOfGravityPoint(point2Ds);
                                    BlockTable bt = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
                                    BlockReference blockReference = new BlockReference(p.ToPoint3d(), id);
                                    blockReference.ScaleFactors = new Scale3d(100);
                                    db.AddToModelSpace(blockReference);

                                    //设置块参照图层错误
                                    //blockReference.Layer = layerName;
                                }


                                #endregion

                                #region 生成梁
                                foreach (var beam in beams)
                                {
                                    if (beam.Floor.LevelB != floor.LevelB) continue;

                                    Point2d p1 = new Point2d(beam.Grid.Joint1.X, beam.Grid.Joint1.Y);
                                    Point2d p2 = new Point2d(beam.Grid.Joint2.X, beam.Grid.Joint2.Y);
                                    double startWidth = 0;
                                    double endWidth = 0;
                                    double height = 0;

                                    Polyline polyline = new Polyline();

                                    switch (beam.BeamSect.Kind)
                                    {
                                        case 1:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-concrete-{height.ToString()}mm", 7);
                                            break;
                                        case 2:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 7:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 13:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 22:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = Math.Min(double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]), double.Parse(beam.BeamSect.ShapeVal.Split(',')[4]));

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 26:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[5]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;

                                        default:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(db, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                    }

                                    db.AddToModelSpace(polyline);
                                }
                                #endregion

                                #region 生成墙
                                foreach (var wall in walls)
                                {
                                    if (wall.Floor.LevelB != floor.LevelB) continue;

                                    SetLayer(db, "wall", 53);

                                    Point2d p1 = new Point2d(wall.Grid.Joint1.X, wall.Grid.Joint1.Y);
                                    Point2d p2 = new Point2d(wall.Grid.Joint2.X, wall.Grid.Joint2.Y);
                                    int startWidth = wall.WallSect.B;
                                    int endWidth = wall.WallSect.B;

                                    Polyline polyline = new Polyline();
                                    polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                    polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                    db.AddToModelSpace(polyline);
                                }
                                #endregion


                                string dwgName = Path.Combine(directoryName, floor.LevelB.ToString() + ".dwg");
                                db.SaveAs(dwgName, DwgVersion.Current);
                                transaction.Commit();
                                editor.WriteMessage($"\n{dwgName}");
                            }
                            catch
                            {
                                transaction.Abort();
                                editor.WriteMessage("\n发生错误1");
                            }
                        }
                    }
                    tempTransaction.Commit();
                }
                catch
                {
                    tempTransaction.Abort();
                    editor.WriteMessage("\n发生错误2");
                }
            }
            editor.WriteMessage("\n结束");
        }

        private void SetLayer(Database db, string layerName, int colorIndex)
        {
            db.AddLayer(layerName);
            db.SetLayerColor(layerName, (short)colorIndex);
            db.SetCurrentLayer(layerName);
        }


        /// <summary>
        /// 根据已知点集合求重心
        /// </summary>
        /// <param name="mPoints">点集合</param>
        /// <returns>重心</returns>
        public Point2d getCenterOfGravityPoint(Point2dCollection mPoints)
        {
            double area = 0;//多边形面积  
            double Gx = 0, Gy = 0;// 重心的x、y  
            for (int i = 1; i <= mPoints.Count; i++)
            {
                double iLat = mPoints[i % mPoints.Count].X;
                double iLng = mPoints[i % mPoints.Count].Y;
                double nextLat = mPoints[(i - 1)].X;
                double nextLng = mPoints[(i - 1)].Y;
                double temp = (iLat * nextLng - iLng * nextLat) / 2;
                area += temp;
                Gx += temp * (iLat + nextLat) / 3;
                Gy += temp * (iLng + nextLng) / 3;
            }
            Gx = Gx / area;
            Gy = Gy / area;
            return new Point2d(Gx, Gy);
        }

        /// <summary>
        /// 获取板轮廓线点的集合
        /// </summary>
        /// <param name="slab">板</param>
        /// <returns>点集合</returns>
        private Point2dCollection GetPoint2Ds(Slab slab)
        {
            Point2dCollection points = new Point2dCollection();
            int n = slab.VertexX.Split(',').Length;
            for (int i = 0; i < n; i++)
            {
                points.Add(new Point2d(double.Parse(slab.VertexX.Split(',')[i % (n - 1)]), double.Parse(slab.VertexY.Split(',')[i % (n - 1)])));
            }

            return points;
        }

        /// <summary>
        /// 获取YDB文件
        /// </summary>
        /// <returns>返回文件路径</returns>
        private string GetFilePath()
        {
            //List<string> list = new List<string>();
            string name = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "盈建科数据库(*.ydb)|*.ydb|所有文件|*.*";
            openFileDialog.Title = "选择结构模型数据库文件";
            openFileDialog.ValidateNames = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                name = openFileDialog.FileName;
                //list.AddRange(dbPaths);
            }
            else
            {
                return null;
            }
            return name;
        }

        #region 结构类
        public class Floor
        {
            public int ID { get; set; }
            public double LevelB { get; set; }
            public double Height { get; set; }
        }

        public class BeamSect
        {
            public int ID { get; set; }
            public int Kind { get; set; }
            public string ShapeVal { get; set; }
            public string ShapeVal1 { get; set; }

        }
        public class Grid
        {
            public int ID { get; set; }
            public Joint Joint1 { get; set; }
            public Joint Joint2 { get; set; }
        }

        public class Joint
        {
            public int ID { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class Beam
        {
            public int ID { get; set; }
            public Floor Floor { get; set; }
            public BeamSect BeamSect { get; set; }
            public Grid Grid { get; set; }

        }

        public class Slab
        {
            public int ID { get; set; }
            public string GridsID { get; set; }
            public string VertexX { get; set; }
            public string VertexY { get; set; }
            public string VertexZ { get; set; }
            public int Thickness { get; set; }
            public Floor Floor { get; set; }
        }

        public class WallSect
        {
            public int ID { get; set; }
            public int Kind { get; set; }
            public int B { get; set; }
        }

        public class Wall
        {
            public int ID { get; set; }
            public Floor Floor { get; set; }
            public WallSect WallSect { get; set; }
            public Grid Grid { get; set; }
        }
        #endregion

        #endregion

        #region FF_ToHydrantAlarmButtonCommand
        [CommandMethod("FF_ToHydrantAlarmButton")]
        public void FF_ToHydrantAlarmButton()
        {
            // Put your command code here
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

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
                

            //Matrix3d GetBlockReferenceMatrix3d(BlockReference blockReference)
            //{
            //    Matrix3d blockreferenceMatrix = blockReference.BlockTransform;
            //    ObjectId ownerId = blockReference.OwnerId;
            //    if (ownerId.GetBlockName() != BlockTableRecord.ModelSpace)
            //    {
            //        BlockReference ownerBlockReference = transaction.GetObject(ownerId, OpenMode.ForRead) as BlockReference;
            //        Matrix3d ownerBlockreferenceMatrix = GetBlockReferenceMatrix3d(ownerBlockReference);
            //        Matrix3d matrix3D = blockreferenceMatrix.

            //    }
            //    return blockreferenceMatrix;
            //}
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
    }
}
