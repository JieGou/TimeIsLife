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

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.FireAlarmCommand2))]

namespace TimeIsLife.CADCommand
{
    internal class FireAlarmCommand2
    {

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
                        List<string> areas = new List<string>();
                        for (int i = 0; i < keyValuePairs.Count; i++)
                        {
                            string area = keyValuePairs.ElementAt(i).Value.Key.TextString;
                            if (areas.Contains(area)) continue;
                            areas.Add(area);
                            Point3d layoutPoint3d = basePoint3d + new Vector3d(0, 2500 * i, 0);
                            Point3d tempPoint3d = new Point3d(layoutPoint3d.X, layoutPoint3d.Y, layoutPoint3d.Z) + new Vector3d(12100, 0, 0);
                            List<string> blockNames = new List<string>();
                            List<BlockReference> blockReferences = new List<BlockReference>();

                            TypedValueList typedValues3 = new TypedValueList();
                            typedValues3.Add(typeof(BlockReference));
                            SelectionFilter blockReferenceSelectionFilter = new SelectionFilter(typedValues3);

                            for (int j = i + 1; j < keyValuePairs.Count; j++)
                            {
                                if (area == keyValuePairs.ElementAt(j).Value.Key.TextString)
                                {
                                    PromptSelectionResult promptSelectionResult3 = editor.SelectWindowPolygon(keyValuePairs.ElementAt(j).Value.Value, blockReferenceSelectionFilter);
                                    SelectionSet selectionSet3 = promptSelectionResult3.Value;

                                    foreach (var objectId in selectionSet3.GetObjectIds())
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
                                }
                            }

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
                                        int n1 = 0, n2 = 0;
                                        //获取短路隔离器数量n1
                                        //获取接线端子箱数量n2
                                        foreach (var item in blockReferences)
                                        {
                                            if (item.Name == "FA-总线短路隔离器")
                                            {
                                                item.UpgradeOpen();
                                                foreach (ObjectId id in item.AttributeCollection)
                                                {
                                                    AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                                    if (attref != null)
                                                    {
                                                        n1 += int.Parse(attref.TextString);
                                                    }
                                                }
                                                item.DowngradeOpen();
                                            }
                                            if (item.Name == "FA-01-接线端子箱")
                                            {
                                                item.UpgradeOpen();
                                                foreach (ObjectId id in item.AttributeCollection)
                                                {
                                                    AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                                    if (attref != null)
                                                    {
                                                        n2 += int.Parse(attref.TextString);
                                                    }
                                                }
                                                item.DowngradeOpen();
                                            }
                                        }

                                        AddElement2500(database, layoutPoint3d, file, area, n1.ToString(), n2.ToString());
                                        break;
                                    case "FA-02-带消防电话插孔的手动报警按钮":
                                        AddElement1(database, layoutPoint3d + new Vector3d(2500, 0, 0), file, n);
                                        break;
                                    case "FA-03-火灾报警电话机":
                                        AddElement1(database, layoutPoint3d + new Vector3d(3200, 0, 0), file, n);
                                        break;
                                    case "FA-04-声光警报器":
                                        AddElement1(database, layoutPoint3d + new Vector3d(3900, 0, 0), file, n);
                                        break;
                                    case "FA-05-3W火灾警报扬声器(挂墙明装距地2.4m)":
                                        AddElement1600(database, layoutPoint3d + new Vector3d(4800, 0, 0), file, n);
                                        break;
                                    case "FA-06-3W火灾警报扬声器(吸顶安装)":
                                        AddElement1(database, layoutPoint3d + new Vector3d(6400, 0, 0), file, n);
                                        break;
                                    case "FA-07-消火栓起泵按钮":
                                        AddElement1(database, layoutPoint3d + new Vector3d(7100, 0, 0), file, n);
                                        break;
                                    case "FA-08-智能型点型感烟探测器":
                                        AddElement1(database, layoutPoint3d + new Vector3d(7800, 0, 0), file, n);
                                        break;
                                    case "FA-09-智能型点型感温探测器":
                                        AddElement1(database, layoutPoint3d + new Vector3d(8700, 0, 0), file, n);
                                        break;
                                    case "FA-10-防火阀(70°C熔断关闭)":
                                        AddElement1(database, layoutPoint3d + new Vector3d(9400, 0, 0), file, n);
                                        break;
                                    case "FA-11-防火阀(280°C熔断关闭)":
                                        AddElement1(database, layoutPoint3d + new Vector3d(10300, 0, 0), file, n);
                                        break;
                                    case "FA-12-电动排烟阀(常闭)":
                                        AddElement1(database, layoutPoint3d + new Vector3d(11200, 0, 0), file, n);
                                        break;
                                    case "FA-13-电动排烟阀(常开)":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-14-常闭正压送风口":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-15-防火卷帘控制箱":
                                        AddElement2(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-16-电动挡烟垂壁控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-17-水流指示器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-18-信号阀":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-19-智能线型红外光束感烟探测器（发射端）":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-20-智能线型红外光束感烟探测器（接收端）":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-21-电动排烟窗控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-22-消防电梯控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-23-电梯控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-24-湿式报警阀组":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-25-预作用报警阀组":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-26-可燃气体探测控制器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-27-流量开关":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-28-非消防配电箱":
                                        //获取非消防配电箱数量n
                                        int n3 = 0;
                                        foreach (var item in blockReferences)
                                        {
                                            if (item.Name == "FA-28-非消防配电箱")
                                            {
                                                item.UpgradeOpen();
                                                foreach (ObjectId id in item.AttributeCollection)
                                                {
                                                    AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                                    if (attref != null)
                                                    {
                                                        n3 += int.Parse(attref.TextString);
                                                    }
                                                }
                                                item.DowngradeOpen();
                                            }
                                        }
                                        AddElement4(database, tempPoint3d, file, n3.ToString());
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-29-消防泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-30-喷淋泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-31-消防稳压泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-32-雨淋泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-33-水幕泵控制箱":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-34-消防风机控制箱":
                                        //获取消防风机控制箱数量n4，消防风机数量n5
                                        int n4 = 0, n5 = 0;
                                        foreach (var item in blockReferences)
                                        {
                                            if (item.Name == "FA-34-消防风机控制箱")
                                            {
                                                n4++;
                                                item.UpgradeOpen();
                                                foreach (ObjectId id in item.AttributeCollection)
                                                {
                                                    AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                                    if (attref != null)
                                                    {
                                                        n5 += int.Parse(attref.TextString);
                                                    }
                                                }
                                                item.DowngradeOpen();
                                            }
                                        }
                                        AddElement5(database, tempPoint3d, file, n4.ToString(), n5.ToString());
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-35-就地液位显示盘":
                                        AddElement3(database, layoutPoint3d, file, n);
                                        break;
                                    case "FA-37-区域显示器":
                                        AddElement1(database, layoutPoint3d + new Vector3d(-1300, 0, 0), file, n);
                                        break;
                                    case "FA-38-常闭防火门监控模块":
                                        AddElement1(database, layoutPoint3d + new Vector3d(-2600, 0, 0), file, n);
                                        break;
                                    case "FA-39-常开防火门监控模块":
                                        AddElement1(database, layoutPoint3d + new Vector3d(-2600, 0, 0), file, n);
                                        break;
                                    case "FA-40-压力开关":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
                                        break;
                                    case "FA-41-火焰探测器":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(700, 0, 0);
                                        break;
                                    case "FA-42-电磁阀":
                                        AddElement1(database, tempPoint3d, file, n);
                                        tempPoint3d += new Vector3d(900, 0, 0);
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
        /// <param name="database">当前文件数据库</param>
        /// <param name="tempPoint3d">插入基点</param>
        /// <param name="file">插入的DWG文件路径</param>
        /// <param name="area">防火分区编号</param>
        /// <param name="n1">短路隔离器数量</param>
        /// <param name="n2">接线端子箱数量</param>
        private void AddElement2500(Database database, Point3d tempPoint3d, string file, string area, string n1, string n2)
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
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in modelSpace)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }

                        BlockReference blockReference = transaction.GetObject(item, OpenMode.ForRead) as BlockReference;
                        if (blockReference != null && blockReference.Name.Equals("FA-总线短路隔离器"))
                        {
                            blockReference.UpgradeOpen();
                            foreach (ObjectId id in blockReference.AttributeCollection)
                            {
                                AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                if (attref != null)
                                {
                                    attref.UpgradeOpen();
                                    //设置属性值
                                    attref.TextString = n1;

                                    attref.DowngradeOpen();
                                }
                            }
                            blockReference.DowngradeOpen();
                        }
                    }
                    dBTexts = dBTexts.OrderBy(d => d.Position.X).ToList();
                    dBTexts[0].EditContent(area);
                    dBTexts[1].EditContent(n2);
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
        /// 添加非消防强切点位
        /// </summary>
        /// <param name="database"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="file"></param>
        /// <param name="n">非消防强切点位</param>
        private void AddElement4(Database database, Point3d tempPoint3d, string file, string n)
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
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in modelSpace)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBText.UpgradeOpen();
                            dBText.TextString = n;
                            dBText.DowngradeOpen();
                        }

                        BlockReference blockReference = transaction.GetObject(item, OpenMode.ForRead) as BlockReference;
                        if (blockReference != null && blockReference.Name.Equals("FA-28-非消防配电箱"))
                        {
                            blockReference.UpgradeOpen();
                            foreach (ObjectId id in blockReference.AttributeCollection)
                            {
                                AttributeReference attref = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
                                if (attref != null)
                                {
                                    attref.UpgradeOpen();
                                    //设置属性值
                                    attref.TextString = n;

                                    attref.DowngradeOpen();
                                }
                            }
                            blockReference.DowngradeOpen();
                        }
                    }
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }

        /// <summary>
        /// 添加消防风机控制箱
        /// </summary>
        /// <param name="database"></param>
        /// <param name="tempPoint3d"></param>
        /// <param name="file"></param>
        /// <param name="n1">消防风机控制箱数量</param>
        /// <param name="n2">消防风机数量</param>
        private void AddElement5(Database database, Point3d tempPoint3d, string file, string n1, string n2)
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
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    List<DBText> dBTexts = new List<DBText>();
                    foreach (var item in modelSpace)
                    {
                        DBText dBText = transaction.GetObject(item, OpenMode.ForRead) as DBText;
                        if (dBText != null)
                        {
                            dBTexts.Add(dBText);
                        }
                    }
                    dBTexts = dBTexts.OrderBy(d => d.Position.Y).ToList();
                    dBTexts[0].EditContent(n1);
                    dBTexts[1].EditContent(n2);
                    transaction.Commit();

                    database.Insert(matrix3D, db, false);
                }
            }
        }
        #endregion

        #region FF_AutomaticConnection
        [CommandMethod("FF_AutomaticConnection")]
        public void FF_AutomaticConnection()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            string s1 = "\n作用：多个块按照最近距离自动连线。";
            string s2 = "\n操作方法：框选对象";
            string s3 = "\n注意事项：块不能锁定";
            editor.WriteMessage(s1 + s2 + s3);

            //NTS
            var precisionModel = new PrecisionModel(1000d);
            GeometryPrecisionReducer precisionReducer = new GeometryPrecisionReducer(precisionModel);
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
                (
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                precisionModel,
                4326
                );
            GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(precisionModel);

            try
            {
                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    Point3d startPoint3D = new Point3d();
                    Point3d endPoint3D = new Point3d();

                    Matrix3d matrix3D = editor.CurrentUserCoordinateSystem;
                    PromptPointOptions promptPointOptions;

                    promptPointOptions = new PromptPointOptions("\n 请选择第一个角点：");
                    PromptPointResult ppr = editor.GetPoint(promptPointOptions);

                    if (ppr.Status == PromptStatus.OK)
                    {
                        startPoint3D = ppr.Value;
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    using (Transaction loadLineTypeTransaction = database.TransactionManager.StartTransaction())
                    {
                        LinetypeTable linetypeTable = loadLineTypeTransaction.GetObject(database.LinetypeTableId, OpenMode.ForWrite) as LinetypeTable;

                        try
                        {
                            database.LoadLineTypeFile("DASHED", "acad.lin");
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception)
                        {
                            // Handle the exception
                        }

                        loadLineTypeTransaction.Commit();
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

                    UcsSelectJig ucsSelectJig = new UcsSelectJig(startPoint3D, polyLine);
                    PromptResult promptResult = editor.Drag(ucsSelectJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        endPoint3D = ucsSelectJig.endPoint3d.TransformBy(matrix3D.Inverse());
                    }
                    else
                    {
                        transaction.Abort();
                        return;
                    }

                    Point3dCollection point3DCollection = GetPoint3DCollection(startPoint3D, endPoint3D, matrix3D);
                    TypedValueList typedValues = new TypedValueList
                    {
                        typeof(BlockReference),
                        { DxfCode.LayerName, "E-EQUIP" }
                    };
                    SelectionFilter selectionFilter = new SelectionFilter(typedValues);
                    PromptSelectionResult promptSelectionResult = editor.SelectCrossingPolygon(point3DCollection, selectionFilter);
                    if (promptSelectionResult.Status != PromptStatus.OK)
                    {
                        transaction.Abort();
                        return;
                    }
                    List<BlockReference> blockReferences = new List<BlockReference>();
                    SelectionSet selectionSet = promptSelectionResult.Value;
                    foreach (var id in selectionSet.GetObjectIds())
                    {
                        BlockReference blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blockReference == null) continue;
                        LayerTableRecord layerTableRecord = transaction.GetObject(blockReference.LayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerTableRecord.IsLocked == true) continue;
                        if (GetPoint3DCollection(blockReference).Count == 0) continue;
                        blockReferences.Add(blockReference);
                    }

                    var points = ConvertCoordinatesToPoints(geometryFactory, blockReferences);

                    List<LineString> tree;

                    if (ElectricalViewModel.electricalViewModel.IsTreeConnection)
                    {
                        tree = Kruskal.FindMinimumSpanningTree(points, geometryFactory);
                    }
                    else if (ElectricalViewModel.electricalViewModel.IsCircularConnection1)
                    {
                        tree = ConnectPointsNonCrossing(points, geometryFactory);
                    }
                    else if (ElectricalViewModel.electricalViewModel.IsCircularConnection2)
                    {
                        tree = ConnectPointsNonCrossingMST(points, geometryFactory);
                    }
                    else
                    {
                        // 如果没有选中任何 RadioButton，您可以在这里处理该情况
                        tree = new List<LineString>();
                    }

                    SetLayer(database, "E-WIRE", 1);
                    BlockReference br1;
                    BlockReference br2;
                    const double Tolerance = 1e-3;

                    foreach (var line in tree)
                    {
                        var startPoint = new Point3d(line.Coordinates[0].X, line.Coordinates[0].Y, 0);
                        var endPoint = new Point3d(line.Coordinates[1].X, line.Coordinates[1].Y, 0);

                        br1 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - startPoint.X) < Tolerance && Math.Abs(b.Position.Y - startPoint.Y) < Tolerance);
                        br2 = blockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - endPoint.X) < Tolerance && Math.Abs(b.Position.Y - endPoint.Y) < Tolerance);

                        List<Point3d> point3DList1 = GetPoint3DCollection(br1);
                        List<Point3d> point3DList2 = GetPoint3DCollection(br2);

                        var closestPair = (from p1 in point3DList1
                                           from p2 in point3DList2
                                           orderby p1.DistanceTo(p2)
                                           select new { Point1 = p1, Point2 = p2 }).First();

                        Line wire = new Line(closestPair.Point1, closestPair.Point2);

                        BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                        btr.UpgradeOpen();
                        btr.AppendEntity(wire);
                        transaction.AddNewlyCreatedDBObject(wire, true);
                        btr.DowngradeOpen();
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception)
            {

            }
        }

        public static List<LineString> ConnectPointsNonCrossingMST(List<Point> points, GeometryFactory geometry)
        {
            int count = points.Count;
            var result = new List<LineString>();

            if (count < 2)
            {
                return result;
            }

            // Step 1: Construct the minimum spanning tree
            var mst = Kruskal.FindMinimumSpanningTree(points, geometry);

            // Step 2: Perform depth-first search on the minimum spanning tree
            var adjacencyList = new Dictionary<Point, List<Point>>();
            foreach (var line in mst)
            {
                var p1 = line.StartPoint;
                var p2 = line.EndPoint;

                if (!adjacencyList.ContainsKey(p1))
                {
                    adjacencyList[p1] = new List<Point>();
                }
                adjacencyList[p1].Add(p2);

                if (!adjacencyList.ContainsKey(p2))
                {
                    adjacencyList[p2] = new List<Point>();
                }
                adjacencyList[p2].Add(p1);
            }

            var visited = new HashSet<Point>();
            var dfsOrder = new List<Point>();

            void DFS(Point current)
            {
                visited.Add(current);
                dfsOrder.Add(current);

                foreach (var neighbor in adjacencyList[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        DFS(neighbor);
                    }
                }
            }

            DFS(points[0]);

            // Step 3: Connect the points in the DFS order
            for (int i = 0; i < count - 1; i++)
            {
                result.Add(geometry.CreateLineString(new[] {
            new Coordinate(dfsOrder[i].X, dfsOrder[i].Y),
            new Coordinate(dfsOrder[i + 1].X, dfsOrder[i + 1].Y)
        }));
            }

            // Close the cycle by connecting the last point to the first one
            result.Add(geometry.CreateLineString(new[] {
        new Coordinate(dfsOrder[count - 1].X, dfsOrder[count - 1].Y),
        new Coordinate(dfsOrder[0].X, dfsOrder[0].Y)
    }));

            return result;
        }


        public static List<LineString> ConnectPointsNonCrossing(List<Point> points, GeometryFactory geometry)
        {
            int count = points.Count;
            var result = new List<LineString>();

            if (count < 2)
            {
                return result;
            }

            var remainingPoints = new List<Point>(points);
            var currentPoint = remainingPoints[0];
            remainingPoints.RemoveAt(0);

            while (remainingPoints.Count > 0)
            {
                int nearestPointIndex = -1;
                double minDist = double.PositiveInfinity;

                for (int i = 0; i < remainingPoints.Count; i++)
                {
                    double dist = Distance(currentPoint, remainingPoints[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestPointIndex = i;
                    }
                }

                var nextPoint = remainingPoints[nearestPointIndex];
                remainingPoints.RemoveAt(nearestPointIndex);

                result.Add(geometry.CreateLineString(new[] {
            new Coordinate(currentPoint.X, currentPoint.Y),
            new Coordinate(nextPoint.X, nextPoint.Y)
        }));

                currentPoint = nextPoint;
            }

            // Close the cycle by connecting the last point to the first one
            result.Add(geometry.CreateLineString(new[] {
        new Coordinate(currentPoint.X, currentPoint.Y),
        new Coordinate(points[0].X, points[0].Y)
    }));

            return result;
        }

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }


        public List<Point> ConvertCoordinatesToPoints(GeometryFactory geometryFactory, List<BlockReference> blockReferences)
        {
            var points = new List<Point>();
            foreach (var blockReference in blockReferences)
            {
                points.Add(geometryFactory.CreatePoint(new Coordinate(blockReference.Position.X, blockReference.Position.Y)));
            }
            return points;
        }

        private Point3dCollection GetPoint3DCollection(Point3d startPoint3D, Point3d endPoint3D, Matrix3d matrix3D)
        {
            //    Point3d p1 = up1.TransformBy(matrix3D.Inverse());
            //    Point3d p2 = up2.TransformBy(matrix3D.Inverse());
            Point3dCollection point3DCollection = new Point3dCollection();
            if (startPoint3D.X < endPoint3D.X && startPoint3D.Y < endPoint3D.Y)
            {
                var leftDownPoint = startPoint3D;
                var leftUpPoint = new Point3d(startPoint3D.X, endPoint3D.Y, 0);
                var rightUpPoint = endPoint3D;
                var rightDownPoint = new Point3d(endPoint3D.X, startPoint3D.Y, 0);
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else if (startPoint3D.X < endPoint3D.X && startPoint3D.Y > endPoint3D.Y)
            {
                var leftDownPoint = new Point3d(startPoint3D.X, endPoint3D.Y, 0);
                var leftUpPoint = startPoint3D;
                var rightUpPoint = new Point3d(endPoint3D.X, startPoint3D.Y, 0);
                var rightDownPoint = endPoint3D;
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else if (startPoint3D.X > endPoint3D.X && startPoint3D.Y > endPoint3D.Y)
            {
                var leftDownPoint = endPoint3D;
                var leftUpPoint = new Point3d(endPoint3D.X, startPoint3D.Y, 0);
                var rightUpPoint = startPoint3D;
                var rightDownPoint = new Point3d(startPoint3D.X, endPoint3D.Y, 0);
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            else
            {
                var leftDownPoint = new Point3d(endPoint3D.X, startPoint3D.Y, 0);
                var leftUpPoint = endPoint3D;
                var rightUpPoint = new Point3d(startPoint3D.X, endPoint3D.Y, 0);
                var rightDownPoint = startPoint3D;
                point3DCollection = GetPoint3DCollection(point3DCollection, leftDownPoint, rightDownPoint, rightUpPoint, leftUpPoint, matrix3D);
            }
            return point3DCollection;
        }

        private Point3dCollection GetPoint3DCollection(Point3dCollection point3DCollection, Point3d leftDownPoint, Point3d rightDownPoint, Point3d rightUpPoint, Point3d leftUpPoint, Matrix3d matrix3D)
        {

            point3DCollection.Add(leftDownPoint);
            point3DCollection.Add(rightDownPoint);
            point3DCollection.Add(rightUpPoint);
            point3DCollection.Add(leftUpPoint);

            return point3DCollection;
        }

        private List<Point3d> GetPoint3DCollection(BlockReference blockReference)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            List<Point3d> point3DList = new List<Point3d>();

            using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                Matrix3d matrix3D = blockReference.BlockTransform;

                BlockTableRecord btr = transaction.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return point3DList;

                foreach (DBPoint dBPoint in btr.OfType<ObjectId>().Select(id => transaction.GetObject(id, OpenMode.ForRead)).OfType<DBPoint>())
                {
                    point3DList.Add(dBPoint.Position.TransformBy(matrix3D));
                }
            }

            return point3DList;
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
