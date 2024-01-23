using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Dapper;

using DotNetARX;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using TimeIsLife.Helper;
using TimeIsLife.Jig;
using TimeIsLife.Model;
using TimeIsLife.View;

using static TimeIsLife.CADCommand.FireAlarmCommand1;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Database = Autodesk.AutoCAD.DatabaseServices.Database;

namespace TimeIsLife.ViewModel
{
    internal class FireAlarmWindowViewModel : ObservableObject
    {
        public static FireAlarmWindowViewModel Instance { get; set; }
        public FireAlarmWindowViewModel()
        {
            Initialize();
            GetFloorAreaLayerNameCommand = new RelayCommand(GetFloorAreaLayerName);
            GetFireAreaLayerNameCommand = new RelayCommand(GetFireAreaLayerName);
            GetRoomAreaLayerNameCommand = new RelayCommand(GetRoomAreaLayerName);
            GetYdbFileNameCommand = new RelayCommand(GetYdbFileName);
            GetBasePointCommand = new RelayCommand(GetBasePoint);
            GetFloorAreaCommand = new RelayCommand(GetFloorArea);
            //SaveAreaFileCommand = new RelayCommand(SaveAreaFile);
            //ApplyCommand = new RelayCommand(Apply);
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        private void Initialize()
        {
            Instance = this;
            AreaFloors = new ObservableCollection<AreaFloor>();
            Areas = new ObservableCollection<Area>();
        }

        #region 属性
        //楼层区域
        private string floorAreaLayerName;
        public string FloorAreaLayerName
        {
            get => floorAreaLayerName;
            set => SetProperty(ref floorAreaLayerName, value);
        }
        //防火分区
        private string fireAreaLayerName;
        public string FireAreaLayerName
        {
            get => fireAreaLayerName;
            set => SetProperty(ref fireAreaLayerName, value);
        }

        //房间区域
        private string roomAreaLayerName;
        public string RoomAreaLayerName
        {
            get => roomAreaLayerName;
            set => SetProperty(ref roomAreaLayerName, value);
        }

        //YDB数据文件
        private string ydbFileName;
        public string YdbFileName
        {
            get => ydbFileName;
            set => SetProperty(ref ydbFileName, value);
        }

        ////区域数据文件
        //private string areaFileName;
        //public string AreaFileName
        //{
        //    get => areaFileName;
        //    set => SetProperty(ref areaFileName, value);
        //}

        private ObservableCollection<AreaFloor> areaFloors;
        public ObservableCollection<AreaFloor> AreaFloors
        {
            get => areaFloors;
            set => SetProperty(ref areaFloors, value);
        }


        private AreaFloor selectedAreaFloor;
        public AreaFloor SelectedAreaFloor
        {
            get => selectedAreaFloor;
            set => SetProperty(ref selectedAreaFloor, value);
        }

        private ObservableCollection<Area> areas;
        public ObservableCollection<Area> Areas
        {
            get => areas;
            set => SetProperty(ref areas, value);
        }

        private Point3d referenceBasePoint;
        public Point3d ReferenceBasePoint
        {
            get => referenceBasePoint;
            set => SetProperty(ref referenceBasePoint, value);
        }

        public FireAlarmWindowState CurrentState { get; set; } = new FireAlarmWindowState();

        private int slabThickness;
        public int SlabThickness
        {
            get => slabThickness;
            set => SetProperty(ref slabThickness, value);
        }

        private bool sCircularConnectionSelected;
        public bool IsCircularConnectionSelected
        {
            get => sCircularConnectionSelected;
            set => SetProperty(ref sCircularConnectionSelected, value);
        }
        
        #endregion

        #region 委托
        public IRelayCommand GetFloorAreaLayerNameCommand { get; }
        public IRelayCommand GetFireAreaLayerNameCommand { get; }
        public IRelayCommand GetRoomAreaLayerNameCommand { get; }
        public IRelayCommand GetYdbFileNameCommand { get; }
        public IRelayCommand SaveAreaFileCommand { get; }
        public IRelayCommand GetBasePointCommand { get; }
        public IRelayCommand GetFloorAreaCommand { get; }
        //public IRelayCommand ApplyCommand { get; }
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public DialogResult DialogResult { get; private set; }
        #endregion

        #region 方法      
        public void GetFloorAreaLayerName()
        {
            FireAlarmWindow.Instance.Hide();

            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
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
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = $"选择表示楼层区域的闭合多段线"
                    };

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
                        FireAlarmWindow.Instance.ShowDialog();
                        return;
                    }
                    Instance.FloorAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("发生错误: " + ex.Message); // 提供详细的错误信息
                    transaction.Abort();
                }
            }
            FireAlarmWindow.Instance.ShowDialog();
        }
        public void GetFireAreaLayerName()
        {
            FireAlarmWindow.Instance.Hide();

            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
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
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = $"选择表示防火分区的闭合多段线"
                    };

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
                        FireAlarmWindow.Instance.ShowDialog();
                        return;
                    }
                    Instance.FireAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("发生错误: " + ex.Message); // 提供详细的错误信息
                    transaction.Abort();
                }
            }
            FireAlarmWindow.Instance.ShowDialog();

        }
        public void GetRoomAreaLayerName()
        {
            FireAlarmWindow.Instance.Hide();
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
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
                    PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                    {
                        SingleOnly = true,
                        RejectObjectsOnLockedLayers = true,
                        MessageForAdding = $"选择表示房间区域的闭合多段线"
                    };

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
                        FireAlarmWindow.Instance.ShowDialog();
                        return;
                    }
                    Instance.RoomAreaLayerName = polyline.Layer;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("发生错误: " + ex.Message); // 提供详细的错误信息
                    transaction.Abort();
                }
            }
            FireAlarmWindow.Instance.ShowDialog();
        }
        public void GetYdbFileName()
        {
            HideFireAlarmWindow();

            if (!TryOpenFileDialog(out string ydbFileName))
            {
                ShowErrorAndRetry();
                return;
            }

            LoadYdbDatabase(ydbFileName);
            ShowFireAlarmWindow();
        }
        private void HideFireAlarmWindow()
        {
            FireAlarmWindow.Instance.Hide();
            AreaFloors.Clear();
        }
        private bool TryOpenFileDialog(out string ydbFileName)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "盈建科数据库(*.ydb)|*.ydb|所有文件|*.*",
                Title = "选择结构模型数据库文件",
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ydbFileName = openFileDialog.FileName;
                return true;
            }

            ydbFileName = null;
            return false;
        }
        private void ShowErrorAndRetry()
        {
            MessageBox.Show("请重新选择YDB文件！");
            FireAlarmWindow.Instance.ShowDialog();
        }
        public void LoadYdbDatabase(string ydbFileName)
        {
            using SQLiteConnection ydbConn = new SQLiteConnection($"Data Source={ydbFileName}");
            string sqlFloor = "SELECT f.ID,f.Name,f.LevelB,f.Height FROM tblFloor AS f WHERE f.Height != 0";
            IEnumerable<Floor> floors = ydbConn.Query<Floor>(sqlFloor);

            foreach (var floor in floors)
            {
                AreaFloor area = new AreaFloor
                {
                    Name = floor.Name,
                    Level = floor.LevelB
                };
                AreaFloors.Add(area);
            }
        }
        private void ShowFireAlarmWindow()
        {
            FireAlarmWindow.Instance.ShowDialog();
        }
        private void GetFloorArea() 
        {
            if (SelectedAreaFloor == null)
            {
                MessageBox.Show("未选择楼层！");
                return;
            }

            if (Instance.FloorAreaLayerName.IsNullOrWhiteSpace())
            {
                MessageBox.Show("请先完成楼层图层的选择！");
                return;
            }

            FireAlarmWindow.Instance.Hide();

            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

            using Transaction transaction = database.TransactionManager.StartTransaction();
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
                    FireAlarmWindow.Instance.ShowDialog();
                    return;
                }
                Polyline polyline = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;
                if (polyline == null)
                {
                    MessageBox.Show("选择对象不是多段线！");
                    FireAlarmWindow.Instance.ShowDialog();
                    return;
                }
                Instance.SelectedAreaFloor.X = polyline.GeometricExtents.MinPoint.X;
                Instance.SelectedAreaFloor.Y = polyline.GeometricExtents.MinPoint.Y;
                Instance.SelectedAreaFloor.Z = polyline.GeometricExtents.MinPoint.Z;
                //FireAlarmWindowViewModel.instance.SelectedAreaFloor.MinPoint3d = polyline.GeometricExtents.MinPoint;
                //FireAlarmWindowViewModel.instance.SelectedAreaFloor.MaxPoint3d = polyline.GeometricExtents.MaxPoint;

                //过滤器
                TypedValueList typedValues = new TypedValueList
                    {
                        { DxfCode.LayerName, Instance.FloorAreaLayerName },
                        typeof(DBText)
                    };

                SelectionSet selectionSet = editor.GetSelectionSet(SelectString.SelectWindowPolygon, null,
                    new SelectionFilter(typedValues), polyline.GetPoint3dCollection(ucsToWcsMatrix3d));
                if (selectionSet == null)
                {
                    MessageBox.Show("缺少楼层名称！");
                    FireAlarmWindow.Instance.ShowDialog();
                    return;
                }
                else if (selectionSet.Count != 1)
                {
                    MessageBox.Show("包含多个楼层名称！");
                    FireAlarmWindow.Instance.ShowDialog();
                    return;
                }

                DBText dBText = transaction.GetObject(selectionSet.GetObjectIds()[0], OpenMode.ForRead) as DBText;
                if (dBText == null)
                {
                    MessageBox.Show("未包含楼层名称！");
                    FireAlarmWindow.Instance.ShowDialog();
                    return;
                }
                Instance.SelectedAreaFloor.Name = dBText.TextString;
                Area area = new Model.Area
                {
                    Floor = Instance.SelectedAreaFloor,
                    Kind = 0,
                    VertexX = polyline.GetXValues(),
                    VertexY = polyline.GetYValues(),
                    VertexZ = polyline.GetZValues()
                };
                Instance.Areas.Add(area);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误: {ex.Message}");
                transaction.Abort();
            }
            finally
            {
                FireAlarmWindow.Instance.ShowDialog();
            }
        }
        private void GetBasePoint()
        {
            if (Instance.SelectedAreaFloor == null)
            {
                MessageBox.Show("未选择基点对应的楼层！");
                return;
            }

            FireAlarmWindow.Instance.Hide();

            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

            

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    List<Beam> beams = new List<Beam>();
                    List<Wall> walls = new List<Wall>();

                    using (var ydbConn = new SQLiteConnection($"Data Source={Instance.YdbFileName}"))
                    {
                        ydbConn.Open();

                        // 查询梁的逻辑
                        beams = QueryBeams(ydbConn, Instance.SelectedAreaFloor.Level);

                        // 查询墙的逻辑
                        walls = QueryWalls(ydbConn, Instance.SelectedAreaFloor.Level);

                        ydbConn.Close();
                    }

                    List<Polyline> polylines = GeneratePolylines(beams, walls);                    

                    BasePointJig basePointJig = new BasePointJig(polylines);
                    PromptResult promptResult = editor.Drag(basePointJig);
                    if (promptResult.Status == PromptStatus.OK)
                    {
                        Instance.ReferenceBasePoint = basePointJig._point;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发生错误: {ex.Message}");
                }
                finally
                {
                    transaction.Abort();
                }
            }
            FireAlarmWindow.Instance.ShowDialog();
        }
        private List<Beam> QueryBeams(SQLiteConnection ydbConn, double level)
        {
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

            return ydbConn.Query(sqlBeam, mappingBeam,
                new { LevelB = level }).ToList();
        }
        private List<Wall> QueryWalls(SQLiteConnection ydbConn, double level)
        {
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
            return ydbConn.Query(sqlWall, mappingWall,
                new { LevelB = level }).ToList();
        }
        private List<Polyline> GeneratePolylines(List<Beam> beams, List<Wall> walls)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            List<Polyline> polylines = new List<Polyline>();

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                //获取块表
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                //获取模型空间
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                //获取图纸空间
                BlockTableRecord paperSpace = transaction.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

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

            }
            return polylines;
        }

        //public void SaveAreaFile()
        //{
        //    FireAlarmWindow.Instance.Hide();
        //    SaveFileDialog saveFileDialog = new SaveFileDialog
        //    {
        //        Filter = "区域数据库文件 (*.Area)|*.Area|所有文件 (*.*)|*.*",
        //        Title = "文件另存为..."
        //    };
        //    if (saveFileDialog.ShowDialog() == DialogResult.OK)
        //    {
        //        Instance.AreaFileName = saveFileDialog.FileName;
        //    }
        //    FireAlarmWindow.Instance.ShowDialog();
        //}

        //public void Apply()
        //{
        //    Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_FF_SaveAreaFile\n", true, false, true);
        //    FireAlarmWindow.Instance.Hide();
        //}

        public void Confirm()
        {
            DialogResult = DialogResult.OK;
            FireAlarmWindow.Instance.Close();
        }
        public void Cancel()
        {
            DialogResult = DialogResult.Cancel;
            FireAlarmWindow.Instance.Close();
        }
        public void SaveState()
        {
            CurrentState.FloorLayerName = this.FloorAreaLayerName;
            CurrentState.FireAlarmLayerName = this.FireAreaLayerName;
            CurrentState.RoomLayerName = this.RoomAreaLayerName;
            CurrentState.YdbFileName = this.YdbFileName;
            //CurrentState.AreaFileName = this.AreaFileName;
            CurrentState.SlabThickness = this.SlabThickness;
            // 将CurrentState对象序列化为JSON字符串
            string json = JsonConvert.SerializeObject(CurrentState);

            // 将JSON字符串写入到本地文件中
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appDataPath, "TimeIsLife", "UserData");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filePath = Path.Combine(directory, "FireAlarmWindowState.json");
            File.WriteAllText(filePath, json);
        }
        public void LoadState()
        {
            // 读取本地文件中的JSON字符串
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appDataPath, "TimeIsLife", "UserData");
            string filePath = Path.Combine(directory, "FireAlarmWindowState.json");
            if (!File.Exists(filePath)) return;
            string json = File.ReadAllText(filePath);

            // 将JSON字符串反序列化为State对象
            CurrentState = JsonConvert.DeserializeObject<FireAlarmWindowState>(json);
            FloorAreaLayerName = CurrentState.FloorLayerName;
            FireAreaLayerName = CurrentState.FireAlarmLayerName;
            RoomAreaLayerName = CurrentState.RoomLayerName;
            YdbFileName = CurrentState.YdbFileName;
            //this.AreaFileName = CurrentState.AreaFileName;
            SlabThickness = CurrentState.SlabThickness;
        }
        #endregion
    }
}
