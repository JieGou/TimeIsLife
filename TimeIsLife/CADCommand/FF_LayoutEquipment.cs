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
using System.Windows.Shapes;
using Polygon = NetTopologySuite.Geometries.Polygon;
using Path = System.IO.Path;
using NetTopologySuite.Operation.Valid;
using NetTopologySuite.Utilities;
using System.Windows.Media.Media3D;
using System.Numerics;
using NetTopologySuite.Precision;
using Accord.Math.Geometry;
using NetTopologySuite.Operation.Distance;
using System.Drawing;
using NetTopologySuite.Geometries.Utilities;
using Accord.Collections;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.FireAlarmCommand1))]

namespace TimeIsLife.CADCommand
{
    partial class TilCommand
    {
        
        //#region _FF_SaveAreaFile
        //[CommandMethod("_FF_SaveAreaFile")]
        //public void _FF_SaveAreaFile()
        //{
        //    Initialize();

        //    const string createFloorTableSql = @"
        //        CREATE TABLE IF NOT EXISTS Floor (
        //            ID INTEGER PRIMARY KEY,
        //            Name TEXT,
        //            Level REAL,
        //            X REAL,
        //            Y REAL,
        //            Z REAL
        //        )";
        //    const string createAreaTableSql = @"
        //        CREATE TABLE IF NOT EXISTS Area (
        //            ID INTEGER PRIMARY KEY,
        //            FloorID INTEGER NOT NULL,
        //            VertexX TEXT NOT NULL,
        //            VertexY TEXT NOT NULL,
        //            VertexZ TEXT NOT NULL,
        //            Kind INTEGER NOT NULL,
        //            Note TEXT,
        //            FOREIGN KEY(FloorID) REFERENCES Floor(ID)
        //        )";
        //    const string createBasePointTableSql = @"
        //        CREATE TABLE IF NOT EXISTS BasePoint (
        //            ID INTEGER PRIMARY KEY,
        //            Name TEXT,
        //            Level REAL NOT NULL,
        //            X REAL NOT NULL,
        //            Y REAL NOT NULL,
        //            Z REAL NOT NULL
        //        )";
        //    const string insertBasePointSql = @"
        //        INSERT INTO BasePoint (Name, Level, X, Y, Z) 
        //        VALUES (@name, @level, @x, @y, @z)";
        //    const string insertFloorSql = "INSERT INTO Floor (Name, Level, X, Y, Z) VALUES (@Name, @Level, @X, @Y, @Z)";
        //    const string insertAreaSql = "INSERT INTO Area (FloorID, VertexX, VertexY, VertexZ, Kind, Note) VALUES (@FloorID, @X, @Y, @Z, @Kind, @Note)";

        //    using (Transaction transaction = database.TransactionManager.StartTransaction())
        //    {
        //        //获取块表
        //        BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
        //        //获取模型空间
        //        BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        //        //获取图纸空间
        //        BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

        //        try
        //        {
        //            if (File.Exists(FireAlarmWindowViewModel.Instance.AreaFileName))
        //            {
        //                File.Delete(FireAlarmWindowViewModel.Instance.AreaFileName);
        //            }

        //            SQLiteConnection.CreateFile(FireAlarmWindowViewModel.Instance.AreaFileName);
        //            SQLiteHelper sqliteHelper = new SQLiteHelper($"Data Source={FireAlarmWindowViewModel.Instance.AreaFileName};Version=3;");
        //            sqliteHelper.Execute(createFloorTableSql);
        //            sqliteHelper.Execute(createAreaTableSql);
        //            sqliteHelper.Execute(createBasePointTableSql);

        //            sqliteHelper.Insert<int>(insertBasePointSql,
        //                new
        //                {
        //                    FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Name,
        //                    FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Level,
        //                    FireAlarmWindowViewModel.Instance.ReferenceBasePoint.X,
        //                    FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Y,
        //                    FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Z
        //                });

        //            foreach (var areaFloor in FireAlarmWindowViewModel.Instance.AreaFloors)
        //            {
        //                sqliteHelper.Insert<int>(insertFloorSql, areaFloor);
        //            }

        //            foreach (var area in FireAlarmWindowViewModel.Instance.Areas)
        //            {
        //                // 从Floor表中查询指定标高的ID
        //                int floorId = sqliteHelper.Query<int>("SELECT ID FROM Floor WHERE Level = @level", new { level = area.Floor.Level }).FirstOrDefault();

        //                if (floorId > 0)
        //                {
        //                    // 构建插入语句并插入到Area表中                            
        //                    sqliteHelper.Execute(insertAreaSql, new { FloorID = floorId, X = area.VertexX, Y = area.VertexY, Z = area.VertexZ, Kind = area.Kind, Note = area.Note });
        //                }

        //                //过滤器
        //                TypedValueList typedValues = new TypedValueList();
        //                typedValues.Add(typeof(Polyline));
        //                SelectionFilter selectionFilter1 = new SelectionFilter(typedValues);
        //                Point3dCollection point3DCollection1 = area.Point3dCollection.TransformBy(ucsToWcsMatrix3d.Inverse());
        //                SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter1, point3DCollection1);

        //                if (selectionSet.Count == 0) continue;
        //                foreach (var id in selectionSet.GetObjectIds())
        //                {
        //                    Polyline polyline = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
        //                    if (polyline == null) continue;
        //                    if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName || polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
        //                    {
        //                        TypedValueList dbTextTypedValues = new TypedValueList
        //                        {
        //                            { DxfCode.LayerName, polyline.Layer },
        //                            typeof(DBText)
        //                        };
        //                        SelectionFilter selectionFilter2 = new SelectionFilter(dbTextTypedValues);
        //                        SelectionSet dbTextSelectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter2, polyline.GetPoint3dCollection(ucsToWcsMatrix3d.Inverse()));
        //                        List<string> notes = new List<string>();
        //                        foreach (var textId in dbTextSelectionSet.GetObjectIds())
        //                        {
        //                            DBText dBText = transaction.GetObject(textId, OpenMode.ForRead) as DBText;
        //                            if (dBText == null) return;
        //                            notes.Add(dBText.TextString);
        //                        }

        //                        Model.Area newArea = null;
        //                        if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName)
        //                        {
        //                            newArea = new Model.Area
        //                            {
        //                                Kind = 1,
        //                                Note = string.Join(",", notes.ToArray()),
        //                                VertexX = polyline.GetXValues(),
        //                                VertexY = polyline.GetYValues(),
        //                                VertexZ = polyline.GetZValues()
        //                            };
        //                        }
        //                        else if (polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
        //                        {
        //                            newArea = new Model.Area
        //                            {
        //                                Kind = 2,
        //                                Note = string.Join(",", notes.ToArray()),
        //                                VertexX = polyline.GetXValues(),
        //                                VertexY = polyline.GetYValues(),
        //                                VertexZ = polyline.GetZValues()
        //                            };
        //                        }
        //                        if (newArea != null)
        //                        {
        //                            sqliteHelper.Execute(insertAreaSql, new { FloorID = floorId, X = newArea.VertexX, Y = newArea.VertexY, Z = newArea.VertexZ, Kind = newArea.Kind, Note = newArea.Note });
        //                        }
        //                    }
        //                }
        //            }
        //            transaction.Commit();
        //            FireAlarmWindow.Instance.ShowDialog();
        //        }
        //        catch
        //        {
        //            transaction.Abort();
        //            FireAlarmWindow.Instance.ShowDialog();
        //        }
        //    }
        //}
        ////#endregion

        #region _FF_LayoutEquipment

        [CommandMethod("FF_LayoutEquipment")]
        public void FF_LayoutEquipment()
        {
            bool? windowResult = FireAlarmWindow.Instance.ShowDialog();
            if (windowResult != true) return;

            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

            #region 数据库相关常量
            //const string createFloorTableSql = @"
            //    CREATE TABLE IF NOT EXISTS Floor (
            //        ID INTEGER PRIMARY KEY,
            //        Name TEXT,
            //        Level REAL,
            //        X REAL,
            //        Y REAL,
            //        Z REAL
            //    )";
            //const string createAreaTableSql = @"
            //    CREATE TABLE IF NOT EXISTS Area (
            //        ID INTEGER PRIMARY KEY,
            //        FloorID INTEGER NOT NULL,
            //        VertexX TEXT NOT NULL,
            //        VertexY TEXT NOT NULL,
            //        VertexZ TEXT NOT NULL,
            //        Kind INTEGER NOT NULL,
            //        Note TEXT,
            //        FOREIGN KEY(FloorID) REFERENCES Floor(ID)
            //    )";
            //const string createBasePointTableSql = @"
            //    CREATE TABLE IF NOT EXISTS BasePoint (
            //        ID INTEGER PRIMARY KEY,
            //        Name TEXT,
            //        Level REAL NOT NULL,
            //        X REAL NOT NULL,
            //        Y REAL NOT NULL,
            //        Z REAL NOT NULL
            //    )";
            //const string insertBasePointSql = @"
            //    INSERT INTO BasePoint (Name, Level, X, Y, Z) 
            //    VALUES (@name, @level, @x, @y, @z)";
            //const string insertFloorSql = "INSERT INTO Floor (Name, Level, X, Y, Z) VALUES (@Name, @Level, @X, @Y, @Z)";
            //const string insertAreaSql = "INSERT INTO Area (FloorID, VertexX, VertexY, VertexZ, Kind, Note) VALUES (@FloorID, @X, @Y, @Z, @Kind, @Note)";
            #endregion

            List<Beam> beams;
            List<Floor> floors;
            List<Slab> slabs;
            List<Wall> walls;
            
            ObjectId smokeDetectorID;
            ObjectId temperatureDetectorID;
            BasePoint basePoint;

            //NTS
            PrecisionModel precisionModel = new PrecisionModel(1000d);
            GeometryPrecisionReducer precisionReducer = new GeometryPrecisionReducer(precisionModel);
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
                (
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                precisionModel,
                4326
                );
            GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(precisionModel);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                //获取块表
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                //获取模型空间
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                //获取图纸空间
                BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;
                try
                {
                    #region YDB数据查询
                    // YDB数据查询
                    var ydbHelper = new SQLiteHelper($"Data Source={FireAlarmWindowViewModel.Instance.YdbFileName}");
                    using (var ydbConn = ydbHelper.GetConnection())
                    {
                        string sqlFloor = "SELECT f.ID,f.Name,f.LevelB,f.Height FROM tblFloor AS f WHERE f.Height != 0";
                        floors = ydbConn.Query<Floor>(sqlFloor).ToList();

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
                        beams = ydbConn.Query(sqlBeam, mappingBeam).ToList();

                        string sqlSlab = "SELECT s.ID,s.GridsID,s.VertexX,s.VertexY,s.VertexZ,s.Thickness,f.ID,f.LevelB,f.Height FROM tblSlab  AS s INNER JOIN tblFloor AS f on f.StdFlrID = s.StdFlrID";
                        Func<Slab, Floor, Slab> mappingSlab =
                            (slab, floor) =>
                            {
                                slab.Floor = floor;
                                return slab;
                            };
                        slabs = ydbConn.Query<Slab, Floor, Slab>(sqlSlab, mappingSlab).ToList();

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
                        walls = ydbConn.Query(sqlWall, mappingWall).ToList();
                    }
                    #endregion

                    #region 保存区域数据

                    ////获取块表
                    //BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    ////获取模型空间
                    //BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    ////获取图纸空间
                    //BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                    ////判断区域数据文件是否存在，存在则删除区域数据库
                    //if (File.Exists(FireAlarmWindowViewModel.Instance.AreaFileName))
                    //{
                    //    File.Delete(FireAlarmWindowViewModel.Instance.AreaFileName);
                    //}

                    ////创建区域数据库
                    //SQLiteConnection.CreateFile(FireAlarmWindowViewModel.Instance.AreaFileName);
                    //SQLiteHelper sqliteHelper = new SQLiteHelper($"Data Source={FireAlarmWindowViewModel.Instance.AreaFileName};Version=3;");
                    //sqliteHelper.Execute(createFloorTableSql);
                    //sqliteHelper.Execute(createAreaTableSql);
                    //sqliteHelper.Execute(createBasePointTableSql);

                    ////插入基点数据
                    //sqliteHelper.Insert<int>(insertBasePointSql,
                    //    new
                    //    {
                    //        FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Name,
                    //        FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Level,
                    //        FireAlarmWindowViewModel.Instance.ReferenceBasePoint.X,
                    //        FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Y,
                    //        FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Z
                    //    });

                    ////插入楼层数据
                    //foreach (var areaFloor in FireAlarmWindowViewModel.Instance.AreaFloors)
                    //{
                    //    sqliteHelper.Insert<int>(insertFloorSql, areaFloor);
                    //}


                    //foreach (var area in FireAlarmWindowViewModel.Instance.Areas)
                    //{
                    //    // 从Floor表中查询指定标高的ID
                    //    int floorId = sqliteHelper.Query<int>("SELECT ID FROM Floor WHERE Level = @level", new { level = area.Floor.Level }).FirstOrDefault();

                    //    if (floorId > 0)
                    //    {
                    //        // 构建插入语句并插入到Area表中                            
                    //        sqliteHelper.Execute(insertAreaSql, new { FloorID = floorId, X = area.VertexX, Y = area.VertexY, Z = area.VertexZ, area.Kind, area.Note });
                    //    }

                    //    //过滤器
                    //    TypedValueList typedValues = new TypedValueList
                    //    {
                    //        typeof(Polyline)
                    //    };
                    //    SelectionFilter selectionFilter1 = new SelectionFilter(typedValues);
                    //    Point3dCollection point3DCollection1 = area.Point3dCollection.TransformBy(ucsToWcsMatrix3d.Inverse());
                    //    SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter1, point3DCollection1);

                    //    if (selectionSet.Count == 0) continue;
                    //    foreach (var id in selectionSet.GetObjectIds())
                    //    {
                    //        Polyline polyline = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                    //        if (polyline == null) continue;
                    //        if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName || polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
                    //        {
                    //            TypedValueList dbTextTypedValues = new TypedValueList
                    //                {
                    //                    { DxfCode.LayerName, polyline.Layer },
                    //                    typeof(DBText)
                    //                };
                    //            SelectionFilter selectionFilter2 = new SelectionFilter(dbTextTypedValues);
                    //            SelectionSet dbTextSelectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter2, polyline.GetPoint3dCollection(ucsToWcsMatrix3d.Inverse()));
                    //            List<string> notes = new List<string>();
                    //            if (dbTextSelectionSet == null || dbTextSelectionSet.Count == 0)
                    //            {
                    //                continue;
                    //            }
                    //            foreach (var textId in dbTextSelectionSet.GetObjectIds())
                    //            {
                    //                DBText dBText = transaction.GetObject(textId, OpenMode.ForRead) as DBText;
                    //                if (dBText == null) continue;
                    //                notes.Add(dBText.TextString);
                    //            }

                    //            Model.Area newArea = null;
                    //            if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName)
                    //            {
                    //                newArea = new Model.Area
                    //                {
                    //                    Kind = 1,
                    //                    Note = string.Join(",", notes.ToArray()),
                    //                    VertexX = polyline.GetXValues(),
                    //                    VertexY = polyline.GetYValues(),
                    //                    VertexZ = polyline.GetZValues()
                    //                };
                    //            }
                    //            else if (polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
                    //            {
                    //                newArea = new Model.Area
                    //                {
                    //                    Kind = 2,
                    //                    Note = string.Join(",", notes.ToArray()),
                    //                    VertexX = polyline.GetXValues(),
                    //                    VertexY = polyline.GetYValues(),
                    //                    VertexZ = polyline.GetZValues()
                    //                };
                    //            }
                    //            if (newArea != null)
                    //            {
                    //                sqliteHelper.Execute(insertAreaSql, new { FloorID = floorId, X = newArea.VertexX, Y = newArea.VertexY, Z = newArea.VertexZ, newArea.Kind, newArea.Note });
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion

                    #region 区域数据查询
                    //// 区域数据查询            
                    //var areaHelper = new SQLiteHelper($"Data Source={FireAlarmWindowViewModel.Instance.AreaFileName}");
                    //using (var areaConn = areaHelper.GetConnection())
                    //{
                    //    string selectBasePointSql = "SELECT Name, Level, X, Y, Z FROM BasePoint";
                    //    basePoint = areaConn.Query<BasePoint>(selectBasePointSql).ToList().FirstOrDefault();

                    //    string sqlFloorArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexZ,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z FROM Area AS a INNER JOIN Floor AS f on f.ID = a.FloorID WHERE kind=0";
                    //    string sqlFireArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexZ,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z FROM Area AS a INNER JOIN Floor AS f on f.ID = a.FloorID WHERE kind=1";
                    //    string sqlRoomArea = "SELECT a.ID,a.FloorID,a.VertexX,a.VertexY,a.VertexZ,a.kind,a.Note,f.ID,f.Name,f.Level,f.X,f.Y,f.Z FROM Area AS a INNER JOIN Floor AS f on f.ID = a.FloorID WHERE kind=2";
                    //    Func<Model.Area, AreaFloor, Model.Area> mappingArea =
                    //        (area, areaFloor) =>
                    //        {
                    //            area.Floor = areaFloor;
                    //            return area;
                    //        };
                    //    floorAreas = areaConn.Query(sqlFloorArea, mappingArea).ToList();
                    //    fireAreas = areaConn.Query(sqlFireArea, mappingArea).ToList();
                    //    roomAreas = areaConn.Query(sqlRoomArea, mappingArea).ToList();
                    //}
                    #endregion

                    smokeDetectorID = LoadBlockIntoDatabase("FA-08-智能型点型感烟探测器.dwg");
                    temperatureDetectorID = LoadBlockIntoDatabase("FA-09-智能型点型感温探测器.dwg");

                    basePoint = new BasePoint
                    {
                        Name = FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Name,
                        Level = FireAlarmWindowViewModel.Instance.SelectedAreaFloor.Level,
                        X = FireAlarmWindowViewModel.Instance.ReferenceBasePoint.X,
                        Y = FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Y,
                        Z = FireAlarmWindowViewModel.Instance.ReferenceBasePoint.Z
                    };

                    Model.Area baseArea = FireAlarmWindowViewModel.Instance.Areas.Where(f => f.Level == basePoint.Level).FirstOrDefault();
                    Vector3d baseVector = baseArea.BasePoint.GetVectorTo(basePoint.Point3d);

                    #region 根据房间边界及板轮廓生成烟感

                    foreach (var curFloorArea in FireAlarmWindowViewModel.Instance.Areas)
                    {
                        List<Model.Area> fireAreas = new List<Model.Area>();
                        List<Model.Area> roomAreas = new List<Model.Area>();
                        //过滤器
                        TypedValueList typedValues = new TypedValueList
                    {
                        typeof(Polyline)
                    };
                        SelectionFilter selectionFilter1 = new SelectionFilter(typedValues);
                        Point3dCollection point3DCollection1 = curFloorArea.Point3dCollection.TransformBy(ucsToWcsMatrix3d.Inverse());
                        SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter1, point3DCollection1);

                        if (selectionSet.Count == 0) continue;
                        foreach (var id in selectionSet.GetObjectIds())
                        {
                            Polyline polyline = transaction.GetObject(id, OpenMode.ForRead) as Polyline;
                            if (polyline == null) continue;
                            if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName || polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
                            {
                                TypedValueList dbTextTypedValues = new TypedValueList
                                {
                                    { DxfCode.LayerName, polyline.Layer },
                                    typeof(DBText)
                                };
                                SelectionFilter selectionFilter2 = new SelectionFilter(dbTextTypedValues);
                                SelectionSet dbTextSelectionSet = editor.GetSelectionSet(SelectString.SelectCrossingPolygon, null, selectionFilter2, polyline.GetPoint3dCollection(ucsToWcsMatrix3d.Inverse()));
                                List<string> notes = new List<string>();
                                if (dbTextSelectionSet == null || dbTextSelectionSet.Count == 0) continue;
                                foreach (var textId in dbTextSelectionSet.GetObjectIds())
                                {
                                    DBText dBText = transaction.GetObject(textId, OpenMode.ForRead) as DBText;
                                    if (dBText == null) continue;
                                    notes.Add(dBText.TextString);
                                }

                                Model.Area newArea = null;
                                if (polyline.Layer == FireAlarmWindowViewModel.Instance.FireAreaLayerName)
                                {
                                    newArea = new Model.Area
                                    {
                                        Kind = 1,
                                        Note = string.Join(",", notes.ToArray()),
                                        VertexX = polyline.GetXValues(),
                                        VertexY = polyline.GetYValues(),
                                        VertexZ = polyline.GetZValues()
                                    };
                                    if (newArea != null)
                                    {
                                        fireAreas.Add(newArea);
                                    }
                                }
                                else if (polyline.Layer == FireAlarmWindowViewModel.Instance.RoomAreaLayerName)
                                {
                                    newArea = new Model.Area
                                    {
                                        Kind = 2,
                                        Note = string.Join(",", notes.ToArray()),
                                        VertexX = polyline.GetXValues(),
                                        VertexY = polyline.GetYValues(),
                                        VertexZ = polyline.GetZValues()
                                    };
                                    if (newArea != null)
                                    {
                                        roomAreas.Add(newArea);
                                    }
                                }
                            }
                        }

                        SetLayer(database, $"E-EQUIP", 4);
                        List<Beam> curFloorBeams = beams.Where(beam => beam.Floor.LevelB == curFloorArea.Level).ToList();
                        List<Slab> curFloorSlabs = slabs.Where(slab => slab.Floor.LevelB == curFloorArea.Level).ToList();
                        List<Wall> curFloorWalls = walls.Where(wall => wall.Floor.LevelB == curFloorArea.Level).ToList();
                        List<BlockReference> blockReferences = new List<BlockReference>();
                        Vector3d vector3D = Point3d.Origin.GetVectorTo(curFloorArea.BasePoint) + baseVector;

                        foreach (var roomArea in roomAreas)
                        {
                            //筛选本层房间，true继续，false跳过
                            //if (roomArea.Level != curFloorArea.Level) continue;
                            //判断房间类型
                            string areaNote = roomArea.Note;
                            HashSet<string> resultSet = GetRoomType(areaNote);
                            //HashSet< DetectorInfo > detectorInfos = new HashSet< DetectorInfo >();
                            if (resultSet.Count == 0) continue;
                            foreach (string result in resultSet)
                            {
                                DetectorInfo detectorInfo = new DetectorInfo();
                                switch (result)
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
                                //detectorInfos.Add(detectorInfo);

                                Coordinate[] roomCoordinates = GetAreaCoordinates(roomArea);
                                Polygon roomPolygon = geometryFactory.CreatePolygon(roomCoordinates);
                                Dictionary<Geometry, double> geometriyDictionary = new Dictionary<Geometry, double>();
                                foreach (var slab in curFloorSlabs)
                                {
                                    //筛选本层板，true继续，false跳过
                                    if (slab.Floor.LevelB != curFloorArea.Level) continue;
                                    if (string.IsNullOrEmpty(slab.VertexX) || string.IsNullOrEmpty(slab.VertexY) || string.IsNullOrEmpty(slab.VertexZ)) continue;
                                    slab.TranslateVertices(vector3D);
                                    Coordinate[] slabCoordinates = GetSlabCoordinates(slab);
                                    Polygon slabPolygon = geometryFactory.CreatePolygon(slabCoordinates);

                                    //检查 roomPolygon 是否与 slabPolygon 相交。
                                    //如果 roomPolygon 或 slabPolygon 无效，则使用 NetTopologySuite GeometryFixer 进行修复。
                                    //计算两个多边形的相交几何体。
                                    //如果相交几何体是非空多边形，并且板的厚度在指定范围内，则将相交几何体和对应的厚度添加到一个字典（geometriyDictionary）中。
                                    if (roomPolygon.Intersects(slabPolygon))
                                    {
                                        if (roomPolygon.IsValid == false)
                                        {
                                            roomPolygon = (Polygon)NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(roomPolygon);
                                        }
                                        if (slabPolygon.IsValid == false)
                                        {
                                            slabPolygon = (Polygon)NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(slabPolygon);
                                        }

                                        //返回结果不正确
                                        var intersectionGeometry = roomPolygon.Intersection(slabPolygon);



                                        if (intersectionGeometry.OgcGeometryType == OgcGeometryType.Polygon)
                                        {
                                            if (intersectionGeometry.IsEmpty) continue;
                                            if (slab.Thickness > 0 && slab.Thickness < 9999)
                                            {
                                                geometriyDictionary.Add(intersectionGeometry, slab.Thickness);
                                            }
                                            else
                                            {
                                                geometriyDictionary.Add(intersectionGeometry, FireAlarmWindowViewModel.Instance.SlabThickness);
                                            }
                                        }
                                    }

                                    slab.TranslateVertices(-vector3D);
                                }


                                //对多段线集合按照面积进行排序
                                var orderedGeometriyDictionary = geometriyDictionary.OrderByDescending(g => g.Key.Area).ToDictionary(x => x.Key, x => x.Value);
                                List<Coordinate> coordinates = new List<Coordinate>();
                                List<Geometry> tempGeometries = new List<Geometry>();
                                //根据多段线集合生成探测器
                                foreach (var geometryItem in orderedGeometriyDictionary)
                                {
                                    if (CanOffestDistance(geometryItem.Key, -600)) continue;
                                    tempGeometries.Add(geometryItem.Key);
                                }
                                foreach (var geometryItem in orderedGeometriyDictionary)
                                {
                                    if (tempGeometries.Contains(geometryItem.Key)) continue;
                                    tempGeometries.Add(geometryItem.Key);
                                    //多边形内布置一个或多个点位
                                    if ((geometryItem.Key.Area / 1000000) > detectorInfo.Area4)
                                    {
                                        //如果为假，超出保护范围，需要切分为n个子区域
                                        // 定义最大迭代次数以避免无限循环
                                        const int maxIterations = 10;

                                        if (IsProtected(geometryFactory, geometryItem.Key, detectorInfo.Radius, curFloorBeams))
                                        {
                                            coordinates.Add(geometryItem.Key.Centroid.Coordinate);
                                        }
                                        else
                                        {
                                            int n = 2;
                                            for (int i = 0; i < maxIterations; i++)
                                            {
                                                List<Geometry> splitGeometries = SplitPolygon(geometryFactory, geometryItem.Key, n, 100.0);
                                                bool isAllProtected = true;

                                                foreach (var item in splitGeometries)
                                                {
                                                    if (!IsProtected(geometryFactory, item, detectorInfo.Radius, curFloorBeams))
                                                    {
                                                        isAllProtected = false;
                                                        break;
                                                    }
                                                }

                                                if (isAllProtected)
                                                {
                                                    foreach (var item in splitGeometries)
                                                    {
                                                        coordinates.Add(item.Centroid.Coordinate);
                                                    }
                                                    break;
                                                }

                                                n++;
                                            }
                                        }

                                    }
                                    else
                                    {
                                        //如果为假，超出保护范围，需要切分为n个子区域
                                        if (IsProtected(geometryFactory, geometryItem.Key, detectorInfo.Radius, curFloorBeams))
                                        {
                                            List<Geometry> beam600Geometries = new List<Geometry>();
                                            List<Geometry> beam200Geometries = new List<Geometry>();
                                            //保护范围内是否有其他区域
                                            foreach (var item in orderedGeometriyDictionary)
                                            {
                                                if (tempGeometries.Contains(item.Key)) continue;
                                                if (!IsProtected(geometryFactory, geometryItem.Key, detectorInfo.Radius, item.Key)) continue;
                                                if (!(geometryItem.Key.Intersection(item.Key) is LineString lineString)) continue;
                                                List<LineString> lineStrings = new List<LineString>();
                                                foreach (var beam in curFloorBeams)
                                                {

                                                    LineString beamLineString = beam.ToLineString(geometryFactory, precisionReducer);

                                                    bool isPointOnLine1 = new DistanceOp(lineString.StartPoint, beamLineString).Distance() <= 1e-3;
                                                    bool isPointOnLine2 = new DistanceOp(lineString.EndPoint, beamLineString).Distance() <= 1e-3;
                                                    if (isPointOnLine1 && isPointOnLine2)
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
                                                            beam600Geometries.Add(item.Key);
                                                            break;
                                                        }
                                                        else if (0 < height && height < 200)
                                                        {
                                                            beam200Geometries.Add(item.Key);
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (beam200Geometries.Count == 0 && beam600Geometries.Count == 0)
                                            {
                                                coordinates.Add(geometryItem.Key.Centroid.Coordinate);
                                                continue;
                                            }

                                            double area = geometryItem.Key.Area / 1000000;
                                            if (beam200Geometries.Count > 0)
                                            {
                                                foreach (var item in beam200Geometries)
                                                {
                                                    area += item.Area / 1000000;
                                                    tempGeometries.Add(item);
                                                }
                                            }

                                            if (detectorInfo.Area3 < area && area <= detectorInfo.Area4)
                                            {
                                                int count = 2;
                                                IsProtectedByGeometry(tempGeometries, beam600Geometries, count);
                                            }
                                            else if (detectorInfo.Area2 < area && area <= detectorInfo.Area3)
                                            {
                                                int count = 3;
                                                IsProtectedByGeometry(tempGeometries, beam600Geometries, count);
                                            }
                                            else if (detectorInfo.Area1 < area && area <= detectorInfo.Area2)
                                            {
                                                int count = 4;
                                                IsProtectedByGeometry(tempGeometries, beam600Geometries, count);
                                            }
                                            else if (area < detectorInfo.Area1)
                                            {
                                                int count = 5;
                                                IsProtectedByGeometry(tempGeometries, beam600Geometries, count);
                                            }
                                            Coordinate coordinate = geometryItem.Key.Centroid.Coordinate;
                                            coordinates.Add(coordinate);

                                        }
                                        else
                                        {
                                            int n = 2;
                                            while (true)
                                            {
                                                List<Geometry> splitGeometries = SplitPolygon(geometryFactory, geometryItem.Key, n, 100.0);
                                                bool b = true;
                                                foreach (var item in splitGeometries)
                                                {
                                                    if (!IsProtected(geometryFactory, item, detectorInfo.Radius, curFloorBeams))
                                                    {
                                                        b = false;
                                                        break;
                                                    }
                                                }
                                                if (b)
                                                {
                                                    foreach (var item in splitGeometries)
                                                    {
                                                        coordinates.Add(item.Centroid.Coordinate);
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
                                    }
                                }
                                //去除距梁边小于500mm的布置点
                                if (coordinates.Count == 0)
                                {
                                    coordinates.Add(roomPolygon.Centroid.Coordinate);
                                }
                                ObjectId id;
                                if (detectorInfo.Radius == 3600 || detectorInfo.Radius == 4400)
                                {
                                    id = temperatureDetectorID;
                                }
                                else
                                {
                                    id = smokeDetectorID;
                                }
                                foreach (var coordinate in coordinates)
                                {
                                    BlockReference blockReference = new BlockReference(new Point3d(coordinate.X, coordinate.Y, 0), id);
                                    blockReferences.Add(blockReference);
                                    blockReference.ScaleFactors = new Scale3d(100);
                                    database.AddToModelSpace(blockReference);
                                }


                            }
                        }

                        SetLayer(database, "E-WIRE", 3);
                        foreach (var curFireArea in fireAreas)
                        {
                            //if (curFireArea.Level != curFloorArea.Level) continue;
                            Coordinate[] coordinates = GetAreaCoordinates(curFireArea);
                            Polygon fireAreaPolygon = geometryFactory.CreatePolygon(coordinates);

                            List<BlockReference> fireAreaBlockReferences = new List<BlockReference>();
                            List<LineString> lineStrings = new List<LineString>();
                            const double Tolerance = 1e-3;

                            foreach (var blockReference in blockReferences)
                            {
                                Point point = geometryFactory.CreatePoint(new Coordinate(blockReference.Position.X,blockReference.Position.Y));
                                if (fireAreaPolygon.Contains(point))
                                {
                                    fireAreaBlockReferences.Add(blockReference);
                                }
                            }
                            if (FireAlarmWindowViewModel.Instance.IsCircularConnectionSelected)
                            {
                                lineStrings = CreateOptimalRingConnections(fireAreaBlockReferences);
                            }
                            else
                            {
                                lineStrings = CreateMinimumSpanningTreeConnections(fireAreaBlockReferences,geometryFactory);
                            }

                            modelSpace.UpgradeOpen();

                            foreach (var lineString in lineStrings)
                            {
                                var startPoint = new Point3d(lineString.Coordinates[0].X, lineString.Coordinates[0].Y, 0);
                                var endPoint = new Point3d(lineString.Coordinates[1].X, lineString.Coordinates[1].Y, 0);

                                BlockReference br1 = fireAreaBlockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - startPoint.X) < Tolerance && Math.Abs(b.Position.Y - startPoint.Y) < Tolerance);
                                BlockReference br2 = fireAreaBlockReferences.FirstOrDefault(b => Math.Abs(b.Position.X - endPoint.X) < Tolerance && Math.Abs(b.Position.Y - endPoint.Y) < Tolerance);

                                List<Point3d> point3DList1 = GetPoint3DCollection(br1);
                                List<Point3d> point3DList2 = GetPoint3DCollection(br2);

                                var closestPair = (from p1 in point3DList1
                                                   from p2 in point3DList2
                                                   orderby p1.DistanceTo(p2)
                                                   select new { Point1 = p1, Point2 = p2 }).First();

                                var wire = new Autodesk.AutoCAD.DatabaseServices.Line(closestPair.Point1, closestPair.Point2);

                                modelSpace.AppendEntity(wire);
                                transaction.AddNewlyCreatedDBObject(wire, true);
                            }

                            modelSpace.DowngradeOpen();

                        }

                        #region 生成梁
                        foreach (var beam in curFloorBeams)
                        {
                            Point2d p1 = new Point2d(beam.Grid.Joint1.X, beam.Grid.Joint1.Y);
                            Point2d p2 = new Point2d(beam.Grid.Joint2.X, beam.Grid.Joint2.Y);
                            double startWidth = 0;
                            double endWidth = 0;
                            double height = 0;

                            Polyline polyline = new Polyline();

                            string[] shapeVals = beam.BeamSect.ShapeVal.Split(',');
                            startWidth = endWidth = double.Parse(shapeVals[1]);
                            height = double.Parse(shapeVals[2]);
                            string layerName = $"beam-steel-{beam.BeamSect.Kind}-{height}mm";

                            switch (beam.BeamSect.Kind)
                            {
                                case 1:
                                    layerName = $"beam-concrete-{height}mm";
                                    break;
                                case 22:
                                    height = Math.Min(double.Parse(shapeVals[3]), double.Parse(shapeVals[4]));
                                    break;
                                case 26:
                                    startWidth = endWidth = double.Parse(shapeVals[5]);
                                    break;
                            }

                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                            SetLayer(database, layerName, 7);

                            polyline.TransformBy(Matrix3d.Displacement(vector3D));
                            database.AddToModelSpace(polyline);
                        }

                        #endregion

                        #region 生成墙
                        foreach (var wall in curFloorWalls)
                        {
                            SetLayer(database, "wall", 53);

                            Point2d p1 = new Point2d(wall.Grid.Joint1.X, wall.Grid.Joint1.Y);
                            Point2d p2 = new Point2d(wall.Grid.Joint2.X, wall.Grid.Joint2.Y);
                            int startWidth = wall.WallSect.B;
                            int endWidth = wall.WallSect.B;

                            Polyline polyline = new Polyline();
                            polyline.AddVertexAt(0, p1, 0, startWidth, endWidth);
                            polyline.AddVertexAt(1, p2, 0, startWidth, endWidth);
                            polyline.TransformBy(Matrix3d.Displacement(vector3D));
                            database.AddToModelSpace(polyline);
                        }
                        #endregion
                    }
                    #endregion
                }
                catch
                {
                    transaction.Abort();
                    editor.WriteMessage("\n布置点位时发生错误");
                }
                transaction.Commit();
            }
            editor.WriteMessage("\n结束");
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

        public List<LineString> CreateOptimalRingConnections(List<BlockReference> blocks)
        {
            if (blocks.Count < 3)
                throw new ArgumentException("至少需要3个块来形成环形连线。");

            var points = blocks.Select(b => new Coordinate(b.Position.X, b.Position.Y)).ToList();
            var sortedPoints = NearestNeighborAlgorithm(points);
            var connections = new List<LineString>();

            for (int i = 0; i < sortedPoints.Count; i++)
            {
                Coordinate start = sortedPoints[i];
                Coordinate end = sortedPoints[(i + 1) % sortedPoints.Count];
                connections.Add(new LineString(new[] { start, end }));
            }

            return connections;
        }

        private List<Coordinate> NearestNeighborAlgorithm(List<Coordinate> points)
        {
            var remainingPoints = new List<Coordinate>(points);
            var path = new List<Coordinate>();
            var currentPoint = remainingPoints[0];
            path.Add(currentPoint);
            remainingPoints.Remove(currentPoint);

            while (remainingPoints.Any())
            {
                currentPoint = FindNearestPoint(currentPoint, remainingPoints);
                path.Add(currentPoint);
                remainingPoints.Remove(currentPoint);
            }

            return path;
        }

        private Coordinate FindNearestPoint(Coordinate current, List<Coordinate> points)
        {
            Coordinate nearest = null;
            double minDistance = double.MaxValue;

            foreach (var point in points)
            {
                double distance = Distance(current, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        private double Distance(Coordinate p1, Coordinate p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }


        public List<LineString> CreateMinimumSpanningTreeConnections(List<BlockReference> blocks,GeometryFactory geometryFactory)
        {
            if (blocks.Count < 2)
                throw new ArgumentException("至少需要2个块来形成树形连线。");

            var points = blocks.Select(b => geometryFactory.CreatePoint(new Coordinate(b.Position.X, b.Position.Y))).ToList();

            return Kruskal.FindMinimumSpanningTree(points, geometryFactory);
        }
        private ObjectId LoadBlockIntoDatabase(string blockName)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Database blockDatabase = new Database(false, true))
            {
                using (Transaction transaction = blockDatabase.TransactionManager.StartTransaction())
                {
                    try
                    {
                        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                        UriBuilder uri = new UriBuilder(codeBase);
                        string path = Uri.UnescapeDataString(uri.Path);
                        string blockPath = Path.Combine(Path.GetDirectoryName(path), "Block", blockName);
                        string blockSymbolName = SymbolUtilityServices.GetSymbolNameFromPathName(blockPath, "dwg");
                        blockDatabase.ReadDwgFile(blockPath, FileOpenMode.OpenForReadAndReadShare, allowCPConversion: true, null);
                        blockDatabase.CloseInput(true);
                        transaction.Commit();
                        return database.Insert(blockSymbolName, blockDatabase, true);
                    }
                    catch
                    {
                        transaction.Abort();
                        editor.WriteMessage("\n加载AutoCAD图块发生错误");
                        return ObjectId.Null;
                    }
                }
            }
        }


        private HashSet<string> GetRoomType(string input)
        {
            HashSet<string> results = new HashSet<string>();
            string pattern = "A[12]H[23]";

            MatchCollection matches = Regex.Matches(input, pattern);
            foreach (Match match in matches)
            {
                results.Add(match.Value);
            }

            return results;
        }

        public static bool IsPointOnSegment(Coordinate point, Coordinate segmentStart, Coordinate segmentEnd, float tolerance)
        {
            Vector2 segment = new Vector2((float)(segmentEnd.X - segmentStart.X), (float)(segmentEnd.Y - segmentStart.Y));
            Vector2 pointToStart = new Vector2((float)(point.X - segmentStart.X), (float)(point.Y - segmentStart.Y));
            Vector2 pointToEnd = new Vector2((float)(point.X - segmentEnd.X), (float)(point.Y - segmentEnd.Y));

            float dotProduct = Vector2.Dot(segment, pointToStart);
            if (dotProduct < 0) return false;

            float squaredLength = segment.LengthSquared();
            if (dotProduct > squaredLength) return false;

            float crossProduct = segment.X * pointToEnd.Y - segment.Y * pointToEnd.X;
            return Math.Abs(crossProduct) <= tolerance;
        }

        public static bool IsSegmentAOverlappingOrPartOfSegmentB(LineString A, LineString B, double tolerance)
        {
            Coordinate AStart = A.CoordinateSequence.GetCoordinate(0);
            Coordinate AEnd = A.CoordinateSequence.GetCoordinate(1);
            Coordinate BStart = B.CoordinateSequence.GetCoordinate(0);
            Coordinate BEnd = B.CoordinateSequence.GetCoordinate(1);

            return (IsPointOnSegment(AStart, BStart, BEnd, (float)tolerance) && IsPointOnSegment(AEnd, BStart, BEnd, (float)tolerance));
        }


        private bool CanOffestDistance(Geometry geometry, double distance)
        {
            //缓冲区参数
            var bufferParam = new BufferParameters
            {
                IsSingleSided = true,
                JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre,
            };

            Geometry bufferGeometry = geometry.Buffer(distance, bufferParam);

            if (bufferGeometry.Area > 0)
            {
                return true;
            }
            else
            {
                return false;
            }



        }

        private static void IsProtectedByGeometry(List<Geometry> tempGeometries, List<Geometry> beam600Geometries, int count)
        {
            int n = beam600Geometries.Count;
            if (n > 0)
            {
                var orderedBeam600Geometries = beam600Geometries.OrderByDescending(a => a.Area);
                if (n >= count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        tempGeometries.Add(beam600Geometries[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        tempGeometries.Add(beam600Geometries[i]);
                    }
                }
            }
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

        private void SetLayer(Database database, string layerName, int colorIndex)
        {
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(database.LayerTableId, OpenMode.ForRead);

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
                database.Clayer = layerId;

                tr.Commit();
            }
        }


        /// <summary>
        /// 判断几何是否在指定半径及以几何质心为圆心的圆的保护范围内
        /// </summary>
        /// <param name="geometry">几何</param>
        /// <param name="radius">半径</param>
        /// <returns>true：在保护范围内；false:不在保护范围内</returns>
        private bool IsProtected(GeometryFactory geometryFactory, Geometry geometry, double radius, List<Beam> beams)
        {
            MinimumBoundingCircle mbc = new MinimumBoundingCircle(geometry);
            Geometry circlePolygon = mbc.GetCircle();

            // 计算几何质心与最小外接圆边界上的一个点之间的距离作为最小外接圆半径
            Point centerPoint = circlePolygon.Centroid;
            Coordinate[] circleCoordinates = circlePolygon.Coordinates;
            double minRadius = centerPoint.Coordinate.Distance(circleCoordinates[0]);

            return minRadius <= radius;
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
        private bool IsProtected(GeometryFactory geometryFactory, Geometry geometry, double radius, Geometry touchGeometry)
        {
            Point centerPoint = geometry.Centroid;
            bool b = true;
            for (int i = 0; i < touchGeometry.NumPoints - 1; i++)
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
            string[] vertexX = slab.VertexX.Split(',');
            string[] vertexY = slab.VertexY.Split(',');
            int n = vertexX.Length;

            Coordinate[] coordinates = new Coordinate[n + 1];
            for (int i = 0; i < n + 1; i++)
            {

                coordinates[i] = new Coordinate(double.Parse(vertexX[i % n]), double.Parse(vertexY[i % n]));
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
        private Coordinate[] GetAreaCoordinates(Model.Area roomAear)
        {
            int n = roomAear.VertexX.Split(',').Length;
            Coordinate[] coordinates = new Coordinate[n + 1];
            for (int i = 0; i < n + 1; i++)
            {
                coordinates[i] = new Coordinate(Math.Round(double.Parse(roomAear.VertexX.Split(',')[i % n]), 3), Math.Round(double.Parse(roomAear.VertexY.Split(',')[i % n]), 3));
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
                    Point point2D = geometryFactory.CreatePoint(new Coordinate(x, y));

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

        List<Geometry> SplitPolygon(GeometryFactory geometryFactory, Geometry geometry, int count, double interval)
        {
            // 质心点集
            Coordinate maxPoint = geometry.Max();
            Coordinate minPoint = geometry.Min();

            // 构建网格点并判断点是否在多边形内部
            List<double[]> gridPoints = new List<double[]>();
            for (double x = minPoint.X; x <= maxPoint.X; x += interval)
            {
                for (double y = minPoint.Y; y <= maxPoint.Y; y += interval)
                {
                    Point point2D = geometryFactory.CreatePoint(new Coordinate(x, y));

                    if (point2D.Within(geometry))
                    {
                        gridPoints.Add(new double[2] { x, y });
                    }
                }
            }

            // 利用EKmeans 获取分组和簇的质心
            Accord.Math.Random.Generator.Seed = 0;
            KMeans kMeans = new KMeans(count);
            KMeansClusterCollection clusters = kMeans.Learn(gridPoints.ToArray());
            double[][] centerPoints = clusters.Centroids;
            List<Coordinate> coords = new List<Coordinate>();
            foreach (var c in centerPoints)
            {
                coords.Add(new Coordinate(c[0], c[1]));
            }

            // 构建泰森多边形
            VoronoiDiagramBuilder voronoiDiagramBuilder = new VoronoiDiagramBuilder();
            Envelope clipEnvelpoe = new Envelope(minPoint, maxPoint);
            voronoiDiagramBuilder.ClipEnvelope = clipEnvelpoe;
            voronoiDiagramBuilder.SetSites(coords);
            GeometryCollection geometryCollection = voronoiDiagramBuilder.GetDiagram(geometryFactory);

            // 利用封闭面切割泰森多边形
            List<Geometry> geometries = new List<Geometry>();
            for (int i = 0; i < geometryCollection.NumGeometries; i++)
            {
                Geometry vorGeometry = geometryCollection.GetGeometryN(i);
                geometries.Add(vorGeometry.Intersection(geometry));
            }
            return geometries;
        }

        #endregion
    }
}