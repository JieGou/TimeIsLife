using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TimeIsLife.Helper;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        #region FF_FAS
        [CommandMethod("FF_FAS")]
        public void FF_FAS()
        {
            Initialize();

            string s1 = "\n作用：生成火灾自动报警系统图";
            string s2 = "\n操作方法：选择表示防火分区多段线，选择一个基点放置系统图，基点为生成系统图区域左下角点";
            string s3 = "\n注意事项：防火分区多段线需要单独图层，防火分区多段线内需要文字标注防火分区编号，文字和多段线需要同一个图层";
            editor.WriteMessage(s1 + s2 + s3);

            try
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string AssemblyDirectory = Path.GetDirectoryName(path);
                    string directory = Path.Combine(AssemblyDirectory, "FA");
                    string name = "";
                    Point3d basePoint3d = new Point3d();

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
                        name = polyline.Layer;

                        PromptPointResult ppr = editor.GetPoint(new PromptPointOptions("\n请选择系统图放置点："));
                        if (ppr.Status == PromptStatus.OK) basePoint3d = ppr.Value;

                        //选择选定图上的所有多段线
                        TypedValueList typedValues1 = new TypedValueList
                        {
                            { DxfCode.LayerName, name },
                            typeof(Polyline)
                        };
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

                            TypedValueList typedValues3 = new TypedValueList
                            {
                                typeof(BlockReference)
                            };
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
    }
}
