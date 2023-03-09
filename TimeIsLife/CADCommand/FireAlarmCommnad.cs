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

using static TimeIsLife.CADCommand.FireAlarmCommnad;

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

        #region _FF_GetFloorAreaLayerName
        [CommandMethod("_FF_GetFloorAreaLayerName")]
        public void _FF_GetFloorAreaLayerName()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    //选择选项
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions();
                    promptSelectionOptions.SingleOnly = true;
                    promptSelectionOptions.RejectObjectsOnLockedLayers = true;
                    promptSelectionOptions.MessageForAdding = $"选择表示楼层区域的闭合多段线";

                    //过滤器
                    TypedValueList typedValues = new TypedValueList()
                    {
                        //类型
                        new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                        //图层名称
                        //new TypedValue((int)DxfCode.LayerName,""),
                        //块名
                        //new TypedValue((int)DxfCode.BlockName,"")
                    };

                    SelectionSet selectionSet = editor.GetSelectionSet(SelectString.GetSelection, promptSelectionOptions, typedValues, null);
                    Polyline polyline = transaction.GetObject(selectionSet.GetObjectIds().FirstOrDefault(), OpenMode.ForRead) as Polyline;
                    if (polyline == null) 
                    {
                        MessageBox.Show("选择的对象不是多段线！");
                        FireAlarmWindow.instance.ShowDialog();
                        return; 
                    }
                    FireAlarmWindowViewModel.instance.FloorAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.instance.ShowDialog();
        }
        #endregion

        #region _FF_GetFireAreaLayerName
        [CommandMethod("_FF_GetFireAreaLayerName")]
        public void _FF_GetFireAreaLayerName()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    //选择选项
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions();
                    promptSelectionOptions.SingleOnly = true;
                    promptSelectionOptions.RejectObjectsOnLockedLayers = true;
                    promptSelectionOptions.MessageForAdding = $"选择表示防火分区的闭合多段线";

                    //过滤器
                    TypedValueList typedValues = new TypedValueList()
                {
                    //类型
                    new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                    //图层名称
                    //new TypedValue((int)DxfCode.LayerName,""),
                    //块名
                    //new TypedValue((int)DxfCode.BlockName,"")
                };

                    SelectionSet selectionSet = editor.GetSelectionSet(SelectString.GetSelection, promptSelectionOptions, typedValues, null);
                    Polyline polyline = transaction.GetObject(selectionSet.GetObjectIds().FirstOrDefault(), OpenMode.ForRead) as Polyline;
                    if (polyline == null)
                    {
                        MessageBox.Show("选择的对象不是多段线！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    FireAlarmWindowViewModel.instance.FireAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.instance.ShowDialog();
        }
        #endregion

        #region _FF_GetRoomAreaLayerName
        [CommandMethod("_FF_GetRoomAreaLayerName")]
        public void _FF_GetRoomAreaLayerName()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    //选择选项
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions();
                    promptSelectionOptions.SingleOnly = true;
                    promptSelectionOptions.RejectObjectsOnLockedLayers = true;
                    promptSelectionOptions.MessageForAdding = $"选择表示房间区域的闭合多段线";

                    //过滤器
                    TypedValueList typedValues = new TypedValueList()
                {
                    //类型
                    new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                    //图层名称
                    //new TypedValue((int)DxfCode.LayerName,""),
                    //块名
                    //new TypedValue((int)DxfCode.BlockName,"")
                };

                    SelectionSet selectionSet = editor.GetSelectionSet(SelectString.GetSelection, promptSelectionOptions, typedValues, null);
                    Polyline polyline = transaction.GetObject(selectionSet.GetObjectIds().FirstOrDefault(), OpenMode.ForRead) as Polyline;
                    if (polyline == null)
                    {
                        MessageBox.Show("选择的对象不是多段线！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    FireAlarmWindowViewModel.instance.RoomAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.instance.ShowDialog();
        }
        #endregion

        #region _FF_GetFloorArea
        [CommandMethod("_FF_GetFloorArea")]
        public void _FF_GetFloorArea()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            if (FireAlarmWindowViewModel.instance.FloorAreaLayerName.IsNullOrWhiteSpace())
            {
                MessageBox.Show("请先完成楼层图层的选择！");
                FireAlarmWindow.instance.ShowDialog();
                return;
            }

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    //选择选项
                    PromptEntityOptions promptEntityOptions = new PromptEntityOptions("\n选择楼层");
                    PromptEntityResult result = editor.GetEntity(promptEntityOptions);
                    if (result.Status != PromptStatus.OK)
                    {
                        MessageBox.Show("重新选择楼层！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    Polyline polyline = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;
                    if (polyline == null)
                    {
                        MessageBox.Show("选择对象不是多段线！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    FireAlarmWindowViewModel.instance.SelectedAreaFloor.X = polyline.GeometricExtents.MinPoint.X;
                    FireAlarmWindowViewModel.instance.SelectedAreaFloor.Y = polyline.GeometricExtents.MinPoint.Y;
                    FireAlarmWindowViewModel.instance.SelectedAreaFloor.Z = polyline.GeometricExtents.MinPoint.Z;

                    //过滤器
                    TypedValueList typedValues = new TypedValueList();
                    typedValues.Add(DxfCode.LayerName, FireAlarmWindowViewModel.instance.FloorAreaLayerName);
                    typedValues.Add(typeof(DBText));

                    SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectWindowPolygon, null, 
                        new SelectionFilter(typedValues), polyline.GetPoint3dCollection());
                    if (selectionSet.Count != 1)
                    {
                        MessageBox.Show("包含多个楼层名称！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    DBText dBText = transaction.GetObject(selectionSet.GetObjectIds()[0], OpenMode.ForRead) as DBText;
                    if (dBText == null)
                    {
                        MessageBox.Show("未包含楼层名称！");
                        FireAlarmWindow.instance.ShowDialog();
                        return;
                    }
                    FireAlarmWindowViewModel.instance.SelectedAreaFloor.Name = dBText.TextString;
                    Model.Area area = new Model.Area
                    {
                        Floor = FireAlarmWindowViewModel.instance.SelectedAreaFloor,
                        Kind = 0,
                        X = polyline.GetXValues(),
                        Y = polyline.GetYValues(),
                        Z = polyline.GetZValues()
                    };
                    FireAlarmWindowViewModel.instance.Areas.Add(area);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.instance.ShowDialog();
        }
        #endregion

        #region _FF_GetBasePoint
        [CommandMethod("_FF_GetBasePoint")]
        public void _FF_GetBasePoint()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    if (FireAlarmWindowViewModel.instance.SelectedAreaFloor == null)
                    {
                        System.Windows.Forms.MessageBox.Show("未选择基点对应的楼层！");
                        FireAlarmWindow.instance.ShowDialog();
                        transaction.Abort();
                    }
                    var ydbConn = new SQLiteConnection($"Data Source={FireAlarmWindowViewModel.instance.YdbFileName}");

                    //查询梁
                    string sqlBeam = "SELECT b.ID,f.ID,f.LevelB,f.Height,bs.ID,bs.kind,bs.ShapeVal,bs.ShapeVal1,g.ID,j1.ID,j1.X,j1.Y,j2.ID,j2.X,j2.Y " +
                        "FROM tblBeamSeg AS b " +
                        "INNER JOIN tblFloor AS f on f.StdFlrID = b.StdFlrID " +
                        "INNER JOIN tblBeamSect AS bs on bs.ID = b.SectID " +
                        "INNER JOIN tblGrid AS g on g.ID = b.GridId " +
                        "INNER JOIN tblJoint AS j1 on g.Jt1ID = j1.ID " +
                        "INNER JOIN tblJoint AS j2 on g.Jt2ID = j2.ID " +
                        "WHERE f.LevelB = @LevelB"; // add a WHERE clause to filter by f.LevelB

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

                    List<Beam> beams = ydbConn.Query(sqlBeam, mappingBeam, 
                        new { LevelB = FireAlarmWindowViewModel.instance.SelectedAreaFloor.Level }).ToList();

                    //查询墙
                    string sqlWall = "SELECT w.ID,f.ID,f.LevelB,f.Height,ws.ID,ws.kind,ws.B,g.ID,j1.ID,j1.X,j1.Y,j2.ID,j2.X,j2.Y " +
                        "FROM tblWallSeg AS w " +
                        "INNER JOIN tblFloor AS f on f.StdFlrID = w.StdFlrID " +
                        "INNER JOIN tblWallSect AS ws on ws.ID = w.SectID " +
                        "INNER JOIN tblGrid AS g on g.ID = w.GridId " +
                        "INNER JOIN tblJoint AS j1 on g.Jt1ID = j1.ID " +
                        "INNER JOIN tblJoint AS j2 on g.Jt2ID = j2.ID " +
                        "WHERE f.LevelB = @LevelB";

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
                    List<Wall> walls = ydbConn.Query(sqlWall, mappingWall, 
                        new { LevelB = FireAlarmWindowViewModel.instance.SelectedAreaFloor.Level }).ToList();
                    //关闭数据库
                    ydbConn.Close();

                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;


                    List<Polyline> polylines = new List<Polyline>();
                    #region 生成梁
                    foreach (var beam in beams)
                    {
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
                                break;
                            case 2:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);
                                height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;
                            case 7:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;
                            case 13:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;
                            case 22:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                height = Math.Min(double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]), double.Parse(beam.BeamSect.ShapeVal.Split(',')[4]));

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;
                            case 26:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[5]);
                                height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;

                            default:
                                startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                break;
                        }
                        polylines.Add(polyline);
                    }
                    #endregion

                    #region 生成墙
                    foreach (var wall in walls)
                    {
                        Point2d p1 = new Point2d(wall.Grid.Joint1.X, wall.Grid.Joint1.Y);
                        Point2d p2 = new Point2d(wall.Grid.Joint2.X, wall.Grid.Joint2.Y);
                        int startWidth = wall.WallSect.B;
                        int endWidth = wall.WallSect.B;

                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                        polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                        polylines.Add(polyline);
                    }
                    #endregion

                    BasePointJig basePointJig = new BasePointJig(polylines);
                    PromptResult promptResult = editor.Drag(basePointJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        FireAlarmWindowViewModel.instance.ReferenceBasePoint = basePointJig._point;
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.instance.ShowDialog();
        }
        #endregion

        #region _FF_SaveAreaFile
        [CommandMethod("_FF_SaveAreaFile")]
        public void _FF_SaveAreaFile()
        {
            foreach (var areaFloor in FireAlarmWindowViewModel.instance.AreaFloors)
            {
                if (areaFloor.Name.IsNullOrWhiteSpace())
                {
                    MessageBox.Show("楼层信息缺失！");
                    FireAlarmWindow.instance.ShowDialog();
                    return;
                }
            }

            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取块表
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //获取模型空间
                    BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    //获取图纸空间
                    BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    if (File.Exists(FireAlarmWindowViewModel.instance.AreaFileName))
                    {
                        File.Delete(FireAlarmWindowViewModel.instance.AreaFileName);

                    }
                    SQLiteConnection.CreateFile(FireAlarmWindowViewModel.instance.AreaFileName);
                    SQLiteHelper helper = new SQLiteHelper($"Data Source={FireAlarmWindowViewModel.instance.AreaFileName};Version=3;");
                    helper.Execute(@"
                        CREATE TABLE IF NOT EXISTS Floor (
                            ID INTEGER PRIMARY KEY,
                            Name TEXT,
                            Level REAL NOT NULL,
                            X REAL NOT NULL,
                            Y REAL NOT NULL,
                            Z REAL NOT NULL
                        )
                    ");

                    helper.Execute(@"
                        CREATE TABLE IF NOT EXISTS Area (
                            ID INTEGER PRIMARY KEY,
                            FloorID INTEGER NOT NULL,
                            VertexX TEXT NOT NULL,
                            VertexY TEXT NOT NULL,
                            VertexZ TEXT NOT NULL,
                            Kind INTEGER NOT NULL,
                            Note TEXT,
                            FOREIGN KEY(FloorID) REFERENCES Floor(ID)
                        )
                    ");

                    helper.Execute(@"
                        CREATE TABLE IF NOT EXISTS BasePoint (
                            ID INTEGER PRIMARY KEY,
                            Name TEXT,
                            Level REAL NOT NULL,
                            X REAL NOT NULL,
                            Y REAL NOT NULL,
                            Z REAL NOT NULL
                        )
                    ");

                    helper.Insert<int>("INSERT INTO BasePoint (Name,Level,X, Y, Z) VALUES ( @name,@level,@x, @y, @z)",
                        new
                        {
                            FireAlarmWindowViewModel.instance.SelectedAreaFloor.Name,
                            FireAlarmWindowViewModel.instance.SelectedAreaFloor.Level,
                            FireAlarmWindowViewModel.instance.ReferenceBasePoint.X,
                            FireAlarmWindowViewModel.instance.ReferenceBasePoint.Y,
                            FireAlarmWindowViewModel.instance.ReferenceBasePoint.Z
                        });

                    //插入的值不能为空
                    foreach (var areaFloor in FireAlarmWindowViewModel.instance.AreaFloors)
                    {
                        helper.Insert<int>("Floor", areaFloor);
                    }

                    foreach (var area in FireAlarmWindowViewModel.instance.Areas)
                    {
                        // 从Floor表中查询对应的FloorID
                        int floorId = helper.Query<int>("SELECT ID FROM Floor WHERE Level = @level", new { level = area.Floor.Level }).FirstOrDefault();

                        if (floorId > 0)
                        {
                            // 构建插入语句并插入到Area表中
                            string insertSql = "INSERT INTO Area (FloorID, VertexX, VertexY, VertexZ, Kind, Note) VALUES (@floorId, @x, @y, @z, @kind, @note)";
                            helper.Execute(insertSql, new { floorId, area.X, area.Y, area.Z, area.Kind, area.Note });
                        }

                        //过滤器
                        TypedValueList typedValues = new TypedValueList()
                        {
                            //类型
                            new TypedValue((int)DxfCode.Start,"Polyline"),
                            //图层名称
                            new TypedValue((int)DxfCode.LayerName,FireAlarmWindowViewModel.instance.FireAreaLayerName),
                            new TypedValue((int)DxfCode.LayerName,FireAlarmWindowViewModel.instance.RoomAreaLayerName),
                            //块名
                            //new TypedValue((int)DxfCode.BlockName,"")
                        };

                        SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectAll, null, typedValues, area.Point3dCollection);
                        foreach (var id in selectionSet.GetObjectIds())
                        {
                            Polyline polyline = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                            if (polyline == null) continue;
                            if (polyline.Layer == FireAlarmWindowViewModel.instance.FireAreaLayerName)
                            {
                                //过滤器
                                TypedValueList dbTextTypedValues = new TypedValueList()
                                {
                                    //类型
                                    new TypedValue((int)DxfCode.Start,"DBText"),
                                    //图层名称
                                    new TypedValue((int)DxfCode.LayerName,FireAlarmWindowViewModel.instance.FireAreaLayerName),
                                    //块名
                                    //new TypedValue((int)DxfCode.BlockName,"")
                                };

                                SelectionSet dbTextSelectionSet = editor.GetSelectionSet(SelectString.SelectWindowPolygon, null, dbTextTypedValues, polyline.GetPoint3dCollection());
                                DBText dBText = transaction.GetObject(dbTextSelectionSet.GetObjectIds()[0], OpenMode.ForRead) as DBText;
                                if (dBText == null) return;
                                Model.Area fireArea = new Model.Area
                                {
                                    Kind = 1,
                                    Note = dBText.ToString(),
                                    X = polyline.GetXValues(),
                                    Y = polyline.GetYValues(),
                                    Z = polyline.GetZValues()
                                };
                                string insertSql = "INSERT INTO Area (FloorID, VertexX, VertexY, VertexZ, Kind, Note) VALUES (@floorId, @x, @y, @z, @kind, @note)";
                                helper.Execute(insertSql, new { floorId, fireArea.X, fireArea.Y, fireArea.Z, fireArea.Kind, fireArea.Note });
                            }
                            else if (polyline.Layer == FireAlarmWindowViewModel.instance.RoomAreaLayerName)
                            {
                                //过滤器
                                TypedValueList dbTextTypedValues = new TypedValueList()
                                {
                                    //类型
                                    new TypedValue((int)DxfCode.Start,"DBText"),
                                    //图层名称
                                    new TypedValue((int)DxfCode.LayerName,FireAlarmWindowViewModel.instance.RoomAreaLayerName),
                                    //块名
                                    //new TypedValue((int)DxfCode.BlockName,"")
                                };

                                SelectionSet dbTextSelectionSet = editor.GetSelectionSet(SelectString.SelectWindowPolygon, null, dbTextTypedValues, polyline.GetPoint3dCollection());
                                DBText dBText = transaction.GetObject(dbTextSelectionSet.GetObjectIds()[0], OpenMode.ForRead) as DBText;
                                if (dBText == null) return;
                                Model.Area roomArea = new Model.Area
                                {
                                    Kind = 2,
                                    Note = dBText.ToString(),
                                    X = polyline.GetXValues(),
                                    Y = polyline.GetYValues(),
                                    Z = polyline.GetZValues()
                                };
                                string insertSql = "INSERT INTO Area (FloorID, VertexX, VertexY, VertexZ, Kind, Note) VALUES (@floorId, @x, @y, @z, @kind, @note)";
                                helper.Execute(insertSql, new { floorId, roomArea.X, roomArea.Y, roomArea.Z, roomArea.Kind, roomArea.Note });
                            }
                            //// 插入一条记录
                            //var newId = helper.Insert<int>("INSERT INTO MyTable (Name) VALUES (@Name)", new { Name = "John" });

                            //// 更新一条记录
                            //var rowsAffected = helper.Update("UPDATE MyTable SET Name = @Name WHERE Id = @Id", new { Id = newId, Name = "Jack" });

                            //// 查询记录
                            //var result = helper.Query<MyModel>("SELECT * FROM MyTable WHERE Name = @Name", new { Name = "Jack" });

                            //// 删除记录
                            //var rowsDeleted = helper.Delete("DELETE FROM MyTable WHERE Id = @Id", new { Id = newId });
                        }
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                }
            }
        }
        #endregion

        #region _FF_GeneratingEquipment

        [CommandMethod("_FF_GeneratingEquipment")]
        public void _FF_GeneratingEquipment()
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            //NTS
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
                (
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                new PrecisionModel(0.001d),
                4326
                );
            GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();

            //载入YDB数据库
            string ydbDB = GetYdbFilePath();
            if (ydbDB == null) return;
            string directoryName = Path.GetDirectoryName(ydbDB);
            editor.WriteMessage("\n文件名：" + ydbDB);

            //载入区域数据库
            string areaDB = GetAreaFilePath();
            if (areaDB == null) return;
            editor.WriteMessage("\n文件名：" + areaDB);

            #region YDB数据查询
            var ydbConn = new SQLiteConnection($"Data Source={ydbDB}");

            //查询标高
            string sqlFloor = "SELECT f.ID,f.Name,f.LevelB,f.Height FROM tblFloor AS f WHERE f.Height != 0";
            var floors = ydbConn.Query<Floor>(sqlFloor);

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

            var beams = ydbConn.Query(sqlBeam, mappingBeam).ToList();

            //查询板
            string sqlSlab = "SELECT s.ID,s.GridsID,s.VertexX,s.VertexY,s.VertexZ,s.Thickness,f.ID,f.LevelB,f.Height FROM tblSlab  AS s INNER JOIN tblFloor AS f on f.StdFlrID = s.StdFlrID";
            Func<Slab, Floor, Slab> mappingSlab =
                (slab, floor) =>
                {
                    slab.Floor = floor;
                    return slab;
                };
            var slabs = ydbConn.Query(sqlSlab, mappingSlab);

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
            var walls = ydbConn.Query(sqlWall, mappingWall);
            //关闭数据库
            ydbConn.Close();

            #endregion

            #region 区域数据查询
            var areaConn = new SQLiteConnection($"Data Source={areaDB}");
            //查询标高
            string sqlFloorArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexY,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z" +
                "FROM Area AS a " +
                "INNER JOIN Floor As f " +
                "WHERE kind=0";
            string sqlFireArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexY,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z" +
                "FROM Area AS a " +
                "INNER JOIN Floor As f " +
                "WHERE kind=1";
            string sqlRoomArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexY,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z" +
                "FROM Area AS a " +
                "INNER JOIN Floor As f " +
                "WHERE kind=2";
            Func<Model.Area, AreaFloor, Model.Area> mappingArea = (area, areaFloor) =>
            {
                area.Floor = areaFloor;
                return area;
            };
            var floorAreas = areaConn.Query<Model.Area>(sqlFloorArea, mappingArea);
            var fireAreas = areaConn.Query<Model.Area>(sqlFireArea, mappingArea);
            var roomAreas = areaConn.Query<Model.Area>(sqlRoomArea, mappingArea);
            areaConn.Close();
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
                    string smokeDetectorPath = Path.Combine(Path.GetDirectoryName(path), "Block", "FA-08-智能型点型感烟探测器.dwg");
                    string temperatureDetectorPath = Path.Combine(Path.GetDirectoryName(path), "Block", "FA-09-智能型点型感温探测器.dwg");
                    string smokeDetector = SymbolUtilityServices.GetSymbolNameFromPathName(smokeDetectorPath, "dwg");
                    string temperatureDetector = SymbolUtilityServices.GetSymbolNameFromPathName(temperatureDetectorPath, "dwg");

                    tempDb.ReadDwgFile(smokeDetectorPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
                    tempDb.ReadDwgFile(temperatureDetectorPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
                    tempDb.CloseInput(true);

                    //根据标高生成梁图
                    foreach (var floor in floors)
                    {
                        using (Database newDatabase = new Database())
                        using (Transaction newTransaction = newDatabase.TransactionManager.StartTransaction())
                        {
                            try
                            {
                                ObjectId smokeDetectorID = newDatabase.Insert(smokeDetector, tempDb, true);
                                ObjectId temperatureDetectorID = newDatabase.Insert(temperatureDetector, tempDb, true);


                                #region 根据房间边界及板轮廓生成烟感
                                foreach (var roomArea in roomAreas)
                                {
                                    //筛选本层房间，true继续，false跳过
                                    if (roomArea.Level != floor.LevelB) continue;
                                    //判断房间类型
                                    string areaNote = roomArea.Note;
                                    if (areaNote.IsNullOrWhiteSpace() || areaNote.Contains("A1H1") || areaNote.Contains("A2H1") || areaNote.Contains("A4")) continue;
                                    DetectorInfo detectorInfo = new DetectorInfo();
                                    switch (areaNote)
                                    {
                                        case "A1H2":
                                            detectorInfo.Radius = 6700;
                                            detectorInfo.Area1 = 16;
                                            detectorInfo.Area2 = 24;
                                            detectorInfo.Area3 = 32;
                                            detectorInfo.Area4 = 48;
                                            break;
                                        case "A1H3":
                                            detectorInfo.Radius = 5800;
                                            detectorInfo.Area1 = 12;
                                            detectorInfo.Area2 = 18;
                                            detectorInfo.Area3 = 24;
                                            detectorInfo.Area4 = 36;
                                            break;
                                        case "A2H2":
                                            detectorInfo.Radius = 4400;
                                            detectorInfo.Area1 = 6;
                                            detectorInfo.Area2 = 9;
                                            detectorInfo.Area3 = 12;
                                            detectorInfo.Area4 = 18;
                                            break;
                                        case "A2H3":
                                            detectorInfo.Radius = 3600;
                                            detectorInfo.Area1 = 4;
                                            detectorInfo.Area2 = 6;
                                            detectorInfo.Area3 = 8;
                                            detectorInfo.Area4 = 12;
                                            break;
                                    }

                                    Coordinate[] roomCoordinates = GetRoomCoordinates(roomArea);
                                    Polygon roomPolygon = geometryFactory.CreatePolygon(roomCoordinates);
                                    //Polyline roomPolyline = new Polyline();
                                    //roomPolyline.CreatePolyline(roomCoordinates);
                                    //房间外形轮廓
                                    //Extents3d roomExtents3d = roomPolyline.GeometricExtents;

                                    //房间范围内的闭合区域
                                    //List<Polyline> polylines = new List<Polyline>();
                                    //房间范围关联的板
                                    //List<Slab> slabs1 = new List<Slab>();
                                    Dictionary<Geometry, double> geometries = new Dictionary<Geometry, double>();
                                    foreach (var slab in slabs)
                                    {
                                        //筛选本层板，true继续，false跳过
                                        if (slab.Floor.LevelB != floor.LevelB) continue;
                                        Coordinate[] slabCoordinates = GetSlabCoordinates(slab);
                                        Polygon slabPolygon = geometryFactory.CreatePolygon(slabCoordinates);
                                        //Polyline slabPolyline = new Polyline();
                                        //slabPolyline.CreatePolyline(slabPoint2Ds);
                                        //板外形轮廓
                                        //Extents3d slabExtents3d = slabPolyline.GeometricExtents;
                                        //判断多段线的矩形轮廓是否相交，相交继续，不相交跳过
                                        //if (!CheckCross(roomExtents3d, slabExtents3d)) continue;


                                        //不相交或相邻，相交，包含，属于
                                        if (roomPolygon.Intersects(slabPolygon))
                                        {
                                            Geometry geometry = roomPolygon.Intersection(slabPolygon);
                                            if (geometry.OgcGeometryType == OgcGeometryType.Polygon)
                                            {
                                                geometries.Add(geometry, slab.Thickness);
                                            }
                                        }
                                        else if (roomPolygon.Contains(slabPolygon))
                                        {
                                            geometries.Add((Geometry)slabPolygon, slab.Thickness);
                                        }
                                        else if (roomPolygon.Within(slabPolygon))
                                        {
                                            geometries.Add((Geometry)roomPolygon, slab.Thickness);
                                        }
                                    }

                                    SetLayer(newDatabase, $"E-EQUIP", 4);

                                    //对多段线集合按照面积进行排序
                                    geometries.OrderByDescending(g => g.Key.Area);
                                    List<Point> points = new List<Point>();
                                    List<Geometry> tempGeometries = new List<Geometry>();
                                    //根据多段线集合生成探测器
                                    foreach (var geometry in geometries)
                                    {
                                        if (tempGeometries.Contains(geometry.Key)) continue;
                                        //多边形内布置一个或多个点位
                                        if (geometry.Key.Area > detectorInfo.Area4)
                                        {
                                            //如果为假，超出保护范围，需要切分为n个子区域
                                            if (IsProtected(geometry.Key, detectorInfo.Radius, geometryFactory, beams, floor))
                                            {
                                                points.Add(geometry.Key.Centroid);
                                            }
                                            else
                                            {
                                                int n = 2;
                                                while (true)
                                                {
                                                    List<Geometry> splitGeometries = SplitPolygon(geometryFactory, geometry.Key, n, 100);
                                                    bool b = true;
                                                    foreach (var item in splitGeometries)
                                                    {
                                                        if (!IsProtected(item, detectorInfo.Radius, geometryFactory, beams, floor))
                                                        {
                                                            b = false;
                                                            break;
                                                        }
                                                    }
                                                    if (b)
                                                    {
                                                        foreach (var item in splitGeometries)
                                                        {
                                                            points.Add(item.Centroid);
                                                            //BlockReference blockReference = new BlockReference(item.Centroid.ToPoint3d(), smokeDetectorID)
                                                            //{
                                                            //    ScaleFactors = new Scale3d(100)
                                                            //};
                                                            //newDatabase.AddToModelSpace(blockReference);
                                                        }
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        n++;
                                                        continue;
                                                    }
                                                }
                                            }
                                            tempGeometries.Add(geometry.Key);
                                        }
                                        else
                                        {
                                            //如果为假，超出保护范围，需要切分为n个子区域
                                            if (IsProtected(geometry.Key, detectorInfo.Radius, geometryFactory, beams, floor))
                                            {
                                                List<Geometry> beam600 = new List<Geometry>();
                                                List<Geometry> beam200 = new List<Geometry>();
                                                //保护范围内是否有其他区域
                                                foreach (var item in geometries)
                                                {
                                                    if (tempGeometries.Contains(item.Key)) continue;
                                                    if (!IsProtected(geometry.Key, detectorInfo.Radius, item.Key)) continue;
                                                    if (!(geometry.Key.Intersection(item.Key) is LineString lineString)) continue;

                                                    //foreach (var wall in walls)
                                                    //{
                                                    //    if (wall.Floor.LevelB != floor.LevelB) continue;
                                                    //    if (wall.ToLineString().Contains(lineString) || wall.ToLineString().Equals(lineString))
                                                    //    {
                                                    //        break;
                                                    //    }
                                                    //}

                                                    foreach (var beam in beams)
                                                    {
                                                        if (beam.Floor.LevelB != floor.LevelB) continue;
                                                        if (beam.ToLineString().Contains(lineString) || beam.ToLineString().Equals(lineString))
                                                        {
                                                            double height = 0;
                                                            if (beam.IsConcrete)
                                                            {
                                                                height = beam.Height - item.Value;
                                                            }
                                                            else
                                                            {
                                                                height = beam.Height;
                                                            }

                                                            if (height > 600)
                                                            {
                                                                break;
                                                            }
                                                            else if (height >= 200 && height <= 600)
                                                            {
                                                                beam600.Add(item.Key);
                                                                break;
                                                            }
                                                            else if (height < 200)
                                                            {
                                                                beam200.Add(item.Key);
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                                double area = geometry.Key.Area;
                                                if (beam200.Count > 0)
                                                {
                                                    foreach (var item in beam200)
                                                    {
                                                        area += item.Area;
                                                        tempGeometries.Add(item);
                                                    }
                                                }

                                                if (detectorInfo.Area3 < area && area <= detectorInfo.Area4)
                                                {
                                                    int count = 2;
                                                    if (beam600.Count > 0)
                                                    {
                                                        beam600.OrderByDescending(a => a.Area);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            tempGeometries.Add(beam600[i]);
                                                        }
                                                    }
                                                }
                                                else if (detectorInfo.Area2 < area && area <= detectorInfo.Area3)
                                                {
                                                    int count = 3;
                                                    if (beam600.Count > 0)
                                                    {
                                                        beam600.OrderByDescending(a => a.Area);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            tempGeometries.Add(beam600[i]);
                                                        }
                                                    }

                                                }
                                                else if (detectorInfo.Area1 < area && area <= detectorInfo.Area2)
                                                {
                                                    int count = 4;
                                                    if (beam600.Count > 0)
                                                    {
                                                        beam600.OrderByDescending(a => a.Area);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            tempGeometries.Add(beam600[i]);
                                                        }
                                                    }

                                                }
                                                else if (area < detectorInfo.Area1)
                                                {
                                                    int count = 5;
                                                    if (beam600.Count > 0)
                                                    {
                                                        beam600.OrderByDescending(a => a.Area);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            tempGeometries.Add(beam600[i]);
                                                        }
                                                    }

                                                }
                                                points.Add(geometry.Key.Centroid);

                                            }
                                            else
                                            {
                                                int n = 2;
                                                while (true)
                                                {
                                                    List<Geometry> splitGeometries = SplitPolygon(geometryFactory, geometry.Key, n, 100);
                                                    bool b = true;
                                                    foreach (var item in splitGeometries)
                                                    {
                                                        if (!IsProtected(item, detectorInfo.Radius, geometryFactory, beams, floor))
                                                        {
                                                            b = false; break;
                                                        }
                                                    }
                                                    if (b)
                                                    {
                                                        foreach (var item in splitGeometries)
                                                        {
                                                            points.Add(item.Centroid);
                                                        }
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        n++;
                                                        continue;
                                                    }
                                                }
                                            }
                                            tempGeometries.Add(geometry.Key);
                                        }
                                    }
                                    //去除距梁边小于500mm的布置点
                                }

                                //foreach (var slab in slabs)
                                //{
                                //    bool bo = true;
                                //    if (slab.Floor.LevelB != floor.LevelB) continue;

                                //    SetLayer(newDatabase, $"slab-{slab.Thickness.ToString()}mm", 7);

                                //    Polyline polyline = new Polyline();
                                //    Point2dCollection point2Ds = GetSlabCoordinates(slab);
                                //    polyline.CreatePolyline(point2Ds);
                                //    newDatabase.AddToModelSpace(polyline);

                                //    if (slab.Thickness == 0)
                                //    {
                                //        if (!ElectricalViewModel.electricalViewModel.IsLayoutAtHole) bo = false;
                                //    }
                                //    if (!bo) continue;
                                //    //在板的重心添加感烟探测器
                                //    SetLayer(newDatabase, $"E-EQUIP-{slab.Thickness.ToString()}", 4);

                                //    Point2d p = getCenterOfGravityPoint(point2Ds);
                                //    BlockTable bt = (BlockTable)newTransaction.GetObject(newDatabase.BlockTableId, OpenMode.ForRead);
                                //    BlockReference blockReference = new BlockReference(p.ToPoint3d(), smokeDetectorID);
                                //    blockReference.ScaleFactors = new Scale3d(100);
                                //    newDatabase.AddToModelSpace(blockReference);

                                //    //设置块参照图层错误
                                //    //blockReference.Layer = layerName;
                                //}


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
                                            SetLayer(newDatabase, $"beam-concrete-{height.ToString()}mm", 7);
                                            break;
                                        case 2:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 7:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 13:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 22:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = Math.Min(double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]), double.Parse(beam.BeamSect.ShapeVal.Split(',')[4]));

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                        case 26:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[5]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[3]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;

                                        default:
                                            startWidth = endWidth = double.Parse(beam.BeamSect.ShapeVal.Split(',')[1]);
                                            height = double.Parse(beam.BeamSect.ShapeVal.Split(',')[2]);

                                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                            SetLayer(newDatabase, $"beam-steel-{beam.BeamSect.Kind.ToString()}-{height.ToString()}mm", 7);
                                            break;
                                    }

                                    newDatabase.AddToModelSpace(polyline);
                                }
                                #endregion

                                #region 生成墙
                                foreach (var wall in walls)
                                {
                                    if (wall.Floor.LevelB != floor.LevelB) continue;

                                    SetLayer(newDatabase, "wall", 53);

                                    Point2d p1 = new Point2d(wall.Grid.Joint1.X, wall.Grid.Joint1.Y);
                                    Point2d p2 = new Point2d(wall.Grid.Joint2.X, wall.Grid.Joint2.Y);
                                    int startWidth = wall.WallSect.B;
                                    int endWidth = wall.WallSect.B;

                                    Polyline polyline = new Polyline();
                                    polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                                    polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                                    newDatabase.AddToModelSpace(polyline);
                                }
                                #endregion


                                string dwgName = Path.Combine(directoryName, floor.LevelB.ToString() + ".dwg");
                                newDatabase.SaveAs(dwgName, DwgVersion.Current);
                                newTransaction.Commit();
                                editor.WriteMessage($"\n{dwgName}");
                            }
                            catch
                            {
                                newTransaction.Abort();
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

        private string GetAreaFilePath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "区域数据库(*.area)|*.area|所有文件|*.*",
                Title = "选择区域数据库文件",
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            string name;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                name = openFileDialog.FileName;
            }
            else
            {
                return null;
            }
            return name;
        }


        /// <summary>
        /// 判断几何是否在指定半径及以几何质心为圆心的圆的保护范围内
        /// </summary>
        /// <param name="geometry">几何</param>
        /// <param name="radius">半径</param>
        /// <returns>true：在保护范围内；false:不在保护范围内</returns>
        private bool IsProtected(Geometry geometry, double radius, GeometryFactory geometryFactory, List<Beam> beams, Floor floor)
        {
            Point centerPoint = geometry.Centroid;
            bool b = true;
            if (geometry is Polygon polygon)
            {
                //缓冲区参数

                var bufferParam = new BufferParameters
                {
                    IsSingleSided = true,
                    JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre,
                };

                int n = polygon.NumPoints;
                LineString[] list = new LineString[n];
                for (int i = 0; i < n; i++)
                {

                    double d = centerPoint.Coordinate.Distance(polygon.Coordinates[i]);
                    if (d > radius)
                    {
                        b = false;
                        break;
                    }
                    LineString lineString = (LineString)geometryFactory.CreateLineString
                        (new[] { polygon.Coordinates[i % n], polygon.Coordinates[(i + 1) % n] });
                    foreach (var beam in beams)
                    {
                        if (beam.Floor.LevelB != floor.LevelB) continue;
                        if (beam.ToLineString().Contains(lineString) || beam.ToLineString().Equals(lineString))
                        {
                            list[i] = (LineString)lineString.Buffer(-beam.Width / 2, bufferParam);
                            break;
                        }
                    }
                }

                double distance = -500;
                GeometryCollection geometryCollection = new GeometryCollection(list, geometryFactory);
                geometryCollection.Union();
                Polygonizer polygonizer = new Polygonizer();
                foreach (var item in geometryCollection)
                {
                    polygonizer.Add(item);
                }
                var polygons = polygonizer.GetPolygons();
                switch (polygons.Count)
                {
                    case 0:
                        b = false;
                        break;
                    case 1:
                        break;
                    default:
                        Geometry g = polygons.OrderByDescending(p => p.Area).FirstOrDefault().Buffer(distance, bufferParam);
                        if (g.IsValid)
                        {
                            break;
                        }
                        else
                        {
                            b = false;
                            break;
                        }
                }
            }
            return b;
        }

        public Geometry Validate(Geometry geom)
        {
            if (geom.OgcGeometryType == OgcGeometryType.Polygon)
            {
                if (geom.IsValid)
                {
                    geom.Normalize();
                    return geom;
                }
                Polygonizer polygonizer = new Polygonizer();
                AddPolygon((Polygon)geom, polygonizer);
                return ToPolygonGeometry(polygonizer.GetPolygons(), geom.Factory);
            }
            else if (geom.OgcGeometryType == OgcGeometryType.MultiPolygon)
            {
                if (geom.IsValid)
                {
                    geom.Normalize(); // validate does not pick up rings in the wrong order - this will fix that
                    return geom; // If the multipolygon is valid just return it
                }
                Polygonizer polygonizer = new Polygonizer();
                for (int i = 0; i < geom.NumGeometries; i++)
                {
                    AddPolygon((Polygon)geom.GetGeometryN(i), polygonizer);
                }

                return ToPolygonGeometry(polygonizer.GetPolygons(), geom.Factory);
            }
            else
            {
                return geom; // In my case, I only care about polygon / multipolygon geometries
            }
        }

        /**
          * Add all line strings from the polygon given to the polygonizer given
          * @param polygon polygon from which to extract line strings
          * @param polygonizer polygonizer
          */
        private void AddPolygon(Polygon polygon, Polygonizer polygonizer)
        {
            //添加外边界线
            AddLineString(polygon.ExteriorRing, polygonizer);
            //添加内边界线
            foreach (var line in polygon.InteriorRings)
            {
                AddLineString(line, polygonizer);
            }
        }

        /**
         * Add the linestring given to the polygonizer
         * @param linestring line string
         * @param polygonizer polygonizer
         */
        private void AddLineString(LineString lineString, Polygonizer polygonizer)
        {
            // LinearRings are treated differently to line strings : we need a LineString NOT a LinearRing
            lineString = lineString.Factory.CreateLineString(lineString.CoordinateSequence);
            // unioning the linestring with the point makes any self intersections explicit.

            Geometry toAdd = null;
            Point point = lineString.Factory.CreatePoint(lineString.GetCoordinateN(0));
            toAdd = lineString.Union(point);
            //for (int i = 0; i < lineString.NumPoints; i++)
            //{
            //    IPoint point = lineString.Factory.CreatePoint(lineString.GetCoordinateN(i));
            //    toAdd = lineString.Union(point);
            //}

            //Add result to polygonizer
            polygonizer.Add(toAdd);

            return;

        }

        /**
         * Get a geometry from a collection of polygons.
         * 
         * @param polygons collection
         * @param factory factory to generate MultiPolygon if required
         * @return null if there were no polygons, the polygon if there was only one, or a MultiPolygon containing all polygons otherwise
         */
        private Geometry ToPolygonGeometry(ICollection<Geometry> polygons, GeometryFactory factory)
        {
            switch (polygons.Count)
            {
                case 0:
                    return null; // No valid polygons!
                case 1:
                    return polygons.ElementAt(0); // single polygon - no need to wrap
                default:
                    return factory.CreateMultiPolygon(polygons.Select(p => p as Polygon).ToArray()); // multiple polygons - wrap them
            }
        }


        /// <summary>
        /// 判断区域是否在相邻区域探测器的保护范围内
        /// </summary>
        /// <param name="geometry">相邻的区域</param>
        /// <param name="radius">保护半径</param>
        /// <param name="touchGeometry">被判断的区域</param>
        /// <returns>true：在保护范围内；false:不在保护范围内</returns>
        private bool IsProtected(Geometry geometry, double radius, Geometry touchGeometry)
        {
            Point centerPoint = geometry.Centroid;
            bool b = true;
            for (int i = 0; i < touchGeometry.NumPoints; i++)
            {

                double d = centerPoint.Coordinate.Distance(touchGeometry.Coordinates[i]);
                if (d > radius)
                {
                    b = false;
                }
            }
            return b;
        }

        /// <summary>
        /// 获取板轮廓线点的集合
        /// </summary>
        /// <param name="slab">板</param>
        /// <returns>点集合</returns>
        private Coordinate[] GetSlabCoordinates(Slab slab)
        {
            int n = slab.VertexX.Split(',').Length;
            Coordinate[] coordinates = new Coordinate[n];
            for (int i = 0; i < n; i++)
            {
                coordinates[i] = new Coordinate(double.Parse(slab.VertexX.Split(',')[i % (n - 1)]), double.Parse(slab.VertexY.Split(',')[i % (n - 1)]));
            }

            return coordinates;
        }

        /// <summary>
        /// 获取YDB文件
        /// </summary>
        /// <returns>返回文件路径</returns>
        private string GetYdbFilePath()
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


        /// <summary>
        /// 获取房间轮廓线点的集合
        /// </summary>
        /// <param name="slab">板</param>
        /// <returns>点集合</returns>
        private Coordinate[] GetRoomCoordinates(Model.Area roomAear)
        {
            int n = roomAear.X.Split(',').Length;
            Coordinate[] coordinates = new Coordinate[n];
            for (int i = 0; i < n; i++)
            {
                coordinates[i] = new Coordinate(double.Parse(roomAear.X.Split(',')[i % (n - 1)]), double.Parse(roomAear.Y.Split(',')[i % (n - 1)]));
            }
            return coordinates;
        }

        /// <summary>
        /// 判断两个矩形是否相交和包含
        /// </summary>
        /// <param name="roomExtents3d">房间外轮廓</param>
        /// <param name="slabExtents3d">板外轮廓</param>
        /// <returns></returns>
        private bool CheckCross(Extents3d roomExtents3d, Extents3d slabExtents3d)
        {
            Point2d roomMaxPoint = roomExtents3d.MaxPoint.ToPoint2d();
            Point2d roomMinPoint = roomExtents3d.MinPoint.ToPoint2d();

            Point2d slabMaxPoint = slabExtents3d.MaxPoint.ToPoint2d();
            Point2d slabMinPoint = slabExtents3d.MinPoint.ToPoint2d();

            return ((Math.Abs(roomMaxPoint.X + roomMinPoint.X - slabMaxPoint.X - slabMinPoint.X) < roomMaxPoint.X - roomMinPoint.X + slabMaxPoint.X - slabMinPoint.X)
                && Math.Abs(roomMaxPoint.Y + roomMinPoint.Y - slabMaxPoint.Y - slabMinPoint.Y) < roomMaxPoint.Y - roomMinPoint.Y + slabMaxPoint.Y - slabMinPoint.Y);
        }

        /// <summary>
        /// 判断两个多边形是否有交点
        /// </summary>
        /// <param name="roomPolyline"></param>
        /// <param name="slabPolyline"></param>
        /// <returns></returns>
        private Point3dCollection GetIntersectPoint3d(Polyline roomPolyline, Polyline slabPolyline)
        {
            Point3dCollection intPoints3d = new Point3dCollection();
            for (int i = 0; i < slabPolyline.NumberOfVertices; i++)
            {
                if (i < slabPolyline.NumberOfVertices - 1 || slabPolyline.Closed)
                {
                    SegmentType slabSegmentType = slabPolyline.GetSegmentType(i);
                    if (slabSegmentType == SegmentType.Line)
                    {
                        LineSegment2d slabLine = slabPolyline.GetLineSegment2dAt(i);
                        PolyIntersectWithLine(roomPolyline, slabLine, 0.0001, ref intPoints3d);
                    }
                    else if (slabSegmentType == SegmentType.Arc)
                    {

                    }
                }
            }
            return intPoints3d;
        }


        /// <summary>
        /// 多段线和直线求交点
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="geLine"></param>
        /// <param name="tol"></param>
        /// <param name="points"></param>
        private void PolyIntersectWithLine(Polyline poly, LineSegment2d geLine, double tol, ref Point3dCollection points)
        {
            Point2dCollection intPoints2d = new Point2dCollection();

            // 获得直线对应的几何类
            //LineSegment2d geLine = new LineSegment2d(ToPoint2d(line.StartPoint), ToPoint2d(line.EndPoint));

            // 每一段分别计算交点
            Tolerance tolerance = new Tolerance(tol, tol);
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                if (i < poly.NumberOfVertices - 1 && poly.Closed)
                {
                    SegmentType st = poly.GetSegmentType(i);
                    if (st == SegmentType.Line)
                    {
                        LineSegment2d geLineSeg = poly.GetLineSegment2dAt(i);
                        Point2d[] pts = geLineSeg.IntersectWith(geLine, tolerance);
                        if (pts != null)
                        {
                            for (int j = 0; j < pts.Length; j++)
                            {
                                if (FindPointIn(intPoints2d, pts[j], tol) < 0)
                                {
                                    intPoints2d.Add(pts[j]);
                                }
                            }
                        }
                    }
                    else if (st == SegmentType.Arc)
                    {
                        CircularArc2d geArcSeg = poly.GetArcSegment2dAt(i);
                        Point2d[] pts = geArcSeg.IntersectWith(geLine, tolerance);
                        if (pts != null)
                        {
                            for (int j = 0; j < pts.Length; j++)
                            {
                                if (FindPointIn(intPoints2d, pts[j], tol) < 0)
                                {
                                    intPoints2d.Add(pts[j]);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < intPoints2d.Count; i++)
            {
                points.Add(ToPoint3d(intPoints2d[i]));
            }
        }


        // 点是否在集合中
        private int FindPointIn(Point2dCollection points, Point2d pt, double tol)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Math.Abs(points[i].X - pt.X) < tol && Math.Abs(points[i].Y - pt.Y) < tol)
                {
                    return i;
                }
            }

            return -1;
        }

        // 三维点转二维点
        private static Point2d ToPoint2d(Point3d point3d)
        {
            return new Point2d(point3d.X, point3d.Y);
        }

        // 二维点转三维点
        private static Point3d ToPoint3d(Point2d point2d)
        {
            return new Point3d(point2d.X, point2d.Y, 0);
        }

        private static Point3d ToPoint3d(Point2d point2d, double elevation)
        {
            return new Point3d(point2d.X, point2d.Y, elevation);
        }


        /// <summary>
        /// 多边形点集排序
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Point3dCollection SortPolyPoints(Point3dCollection points)
        {

            if (points == null || points.Count == 0) return null;
            //计算重心
            Point3d center = new Point3d();
            double X = 0, Y = 0;
            for (int i = 0; i < points.Count; i++)
            {
                X += points[i].X;
                Y += points[i].Y;
            }
            center = new Point3d((int)X / points.Count, (int)Y / points.Count, points[0].Z);
            //冒泡排序
            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int j = 0; j < points.Count - i - 1; j++)
                {
                    if (PointCmp(points[j], points[j + 1], center))
                    {
                        Point3d tmp = points[j];
                        points[j] = points[j + 1];
                        points[j + 1] = tmp;
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// 若点a大于点b,即点a在点b顺时针方向,返回true,否则返回false
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        private bool PointCmp(Point3d a, Point3d b, Point3d center)
        {
            if (a.X >= 0 && b.X < 0)
                return true;
            else if (a.X == 0 && b.X == 0)
                return a.Y > b.Y;
            //向量OA和向量OB的叉积
            double det = (a.X - center.X) * (b.Y - center.Y) - (b.X - center.X) * (a.Y - center.Y);
            if (det < 0)
                return true;
            if (det > 0)
                return false;
            //向量OA和向量OB共线，以距离判断大小
            double d1 = (a.X - center.X) * (a.X - center.X) + (a.Y - center.Y) * (a.Y - center.Y);
            double d2 = (b.X - center.X) * (b.X - center.X) + (b.Y - center.Y) * (b.Y - center.Y);
            return d1 > d2;
        }

        /// <summary>
        /// 按照指定数量切分多边形
        /// </summary>
        /// <param name="geometry">多边形</param>
        /// <param name="count">平分份数</param>
        /// <param name="n">点集数量（kmeans算法用，越多越精确但速度越慢）</param>
        /// <param name="detectorInfo">探测器</param>
        /// <returns>符合条件的平分多边形的质心点集</returns>
        List<Geometry> SplitPolygon(GeometryFactory geometryFactory, Geometry geometry, int count, int n)
        {
            Random random = new Random();
            //质心点集
            //List<Point2d> Points = new List<Point2d>();
            Coordinate maxPoint = geometry.Max();
            Coordinate minPoint = geometry.Min();

            //构建随机点并判断点是否在多边形内部
            double[][] randomPoints = new double[n][];
            for (int i = 0; i < n; i++)
            {
                while (true)
                {
                    double x = minPoint.X + random.NextDouble() * (maxPoint.X - minPoint.X);
                    double y = minPoint.Y + random.NextDouble() * (maxPoint.Y - minPoint.Y);
                    Point point2D = new Point(x, y);

                    if (point2D.Within(geometry))
                    {
                        randomPoints[i] = new double[2] { x, y };
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            //利用EKmeans 获取分组和簇的质心
            Accord.Math.Random.Generator.Seed = 0;
            KMeans kMeans = new KMeans(count);
            KMeansClusterCollection clusters = kMeans.Learn(randomPoints);
            double[][] centerPoints = clusters.Centroids;
            List<Coordinate> coords = new List<Coordinate>();
            foreach (var c in centerPoints)
            {
                coords.Add(new Coordinate(c[0], c[1]));
            }
            //构建泰森多边形
            VoronoiDiagramBuilder voronoiDiagramBuilder = new VoronoiDiagramBuilder();
            Envelope clipEnvelpoe = new Envelope(minPoint, maxPoint);
            voronoiDiagramBuilder.ClipEnvelope = clipEnvelpoe;
            voronoiDiagramBuilder.SetSites(coords);
            GeometryCollection geometryCollection = voronoiDiagramBuilder.GetDiagram(geometryFactory);

            // 4. 利用封闭面切割泰森多边形
            List<Geometry> geometries = new List<Geometry>();
            for (int i = 0; i < geometryCollection.NumGeometries; i++)
            {
                Geometry vorGeometry = geometryCollection.GetGeometryN(i);
                geometries.Add(vorGeometry.Intersection(geometry));
            }
            return geometries;
        }


        #region 结构类
        public class Floor
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public double LevelB { get; set; }
            public double Height { get; set; }
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

            public bool IsConcrete
            {
                get
                {
                    if (BeamSect.Kind == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public double Height
            {
                get
                {
                    var height = BeamSect.Kind switch
                    {
                        1 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        2 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        7 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        13 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        22 => Math.Min(double.Parse(this.BeamSect.ShapeVal.Split(',')[3]),
                                                        double.Parse(this.BeamSect.ShapeVal.Split(',')[4])),
                        26 => double.Parse(this.BeamSect.ShapeVal.Split(',')[3]),
                        _ => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                    };
                    return height;
                }
            }
            public LineString ToLineString()
            {
                Coordinate[] coordinates = new Coordinate[] { new Coordinate(this.Grid.Joint1.X, this.Grid.Joint1.Y), new Coordinate(this.Grid.Joint2.X, this.Grid.Joint2.Y) };
                return new LineString(coordinates);
            }

            public double Width
            {
                get
                {
                    var width = BeamSect.Kind switch
                    {
                        1 => double.Parse(this.BeamSect.ShapeVal.Split(',')[1]),
                        _ => 0.0,
                    };
                    return width;
                }
            }
        }
        public class BeamSect
        {
            public int ID { get; set; }
            public int Kind { get; set; }
            public string ShapeVal { get; set; }
            public string ShapeVal1 { get; set; }

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

            public LineString ToLineString()
            {
                Coordinate[] coordinates = new Coordinate[] { new Coordinate(this.Grid.Joint1.X, this.Grid.Joint1.Y), new Coordinate(this.Grid.Joint2.X, this.Grid.Joint2.Y) };
                return new LineString(coordinates);
            }
        }

        public class DetectorInfo
        {
            public int Radius { get; set; }
            public double Area1 { get; set; }
            public double Area2 { get; set; }
            public double Area3 { get; set; }
            public double Area4 { get; set; }
        }
        #endregion

        #endregion
    }
}