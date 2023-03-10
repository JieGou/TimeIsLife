using Autodesk.AutoCAD.ApplicationServices;
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
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using TimeIsLife.Model;
using TimeIsLife.View;

using static TimeIsLife.CADCommand.FireAlarmCommnad;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TimeIsLife.ViewModel
{
    internal class FireAlarmWindowViewModel : ObservableObject
    {
        public static FireAlarmWindowViewModel instance;
        public FireAlarmWindowViewModel()
        {
            Initialize();
            GetFloorAreaLayerNameCommand = new RelayCommand(GetFloorAreaLayerName);
            GetFireAreaLayerNameCommand = new RelayCommand(GetFireAreaLayerName);
            GetRoomAreaLayerNameCommand = new RelayCommand(GetRoomAreaLayerName);
            GetYdbFileNameCommand = new RelayCommand(GetYdbFileName);
            GetBasePointCommand = new RelayCommand(GetBasePoint);
            GetFloorAreaCommand = new RelayCommand(GetFloorArea);
            SaveAreaFileCommand = new RelayCommand(SaveAreaFile);
            ApplyCommand = new RelayCommand(Apply);
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        private void Initialize()
        {
            instance = this;
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

        //区域数据文件
        private string areaFileName;
        public string AreaFileName
        {
            get => areaFileName;
            set => SetProperty(ref areaFileName, value);
        }

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
        #endregion

        #region 委托
        public IRelayCommand GetFloorAreaLayerNameCommand { get; }
        public IRelayCommand GetFireAreaLayerNameCommand { get; }
        public IRelayCommand GetRoomAreaLayerNameCommand { get; }
        public IRelayCommand GetYdbFileNameCommand { get; }
        public IRelayCommand SaveAreaFileCommand { get; }
        public IRelayCommand GetBasePointCommand { get; }
        public IRelayCommand GetFloorAreaCommand { get; }
        public IRelayCommand ApplyCommand { get; }
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        #endregion

        #region 方法
        public void GetFloorAreaLayerName()
        {
            string cmd = "_FF_GetFloorAreaLayerName\n";
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd, true, false, true);
            FireAlarmWindow.instance.Hide();

        }

        public void GetFireAreaLayerName()
        {
            string cmd = "_FF_GetFireAreaLayerName\n";
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd, true, false, true);
            FireAlarmWindow.instance.Hide();

        }
        public void GetRoomAreaLayerName()
        {
            string cmd = "_FF_GetRoomAreaLayerName\n";
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd, true, false, true);
            FireAlarmWindow.instance.Hide();

        }

        public void GetYdbFileName()
        {
            FireAlarmWindow.instance.Hide();
            if (AreaFloors.Count > 0) AreaFloors.Clear();
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
                YdbFileName = openFileDialog.FileName;
            }

            //载入YDB数据库
            if (YdbFileName == null)
            {
                MessageBox.Show("请重新选择YDB文件！");
                FireAlarmWindow.instance.ShowDialog();
                return;
            }
            SQLiteConnection ydbConn = new SQLiteConnection($"Data Source={YdbFileName}");
            //查询标高
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
            FireAlarmWindow.instance.ShowDialog();
        }


        private void GetFloorArea()
        {
            if (SelectedAreaFloor == null)
            {
                MessageBox.Show("未选择楼层！");
            }
            else
            {
                Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_FF_GetFloorArea\n", true, false, true);
                FireAlarmWindow.instance.Hide();

            }
        }

        private void GetBasePoint()
        {
            string cmd = "_FF_GetBasePoint\n";
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd, true, false, true);
            FireAlarmWindow.instance.Hide();

        }

        public void SaveAreaFile()
        {
            FireAlarmWindow.instance.Hide();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "区域数据库文件 (*.Area)|*.Area|所有文件 (*.*)|*.*",
                Title = "文件另存为..."
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FireAlarmWindowViewModel.instance.AreaFileName = saveFileDialog.FileName;
            }
            FireAlarmWindow.instance.ShowDialog();
        }

        public void Apply()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_FF_SaveAreaFile\n", true, false, true);
            FireAlarmWindow.instance.Hide();
        }

        public void Confirm()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_FF_SaveAreaFile\n", true, false, true);
            FireAlarmWindow.instance.Close();
        }

        public void Cancel()
        {
            FireAlarmWindow.instance.Close();
        }

        public void SaveState()
        {
            CurrentState.FloorLayerName = this.FloorAreaLayerName;
            CurrentState.FireAlarmLayerName = this.FireAreaLayerName;
            CurrentState.RoomLayerName = this.RoomAreaLayerName;
            CurrentState.YdbFileName = this.YdbFileName;
            CurrentState.AreaFileName = this.AreaFileName;
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
            this.FloorAreaLayerName = CurrentState.FloorLayerName;
            this.FireAreaLayerName = CurrentState.FireAlarmLayerName;
            this.RoomAreaLayerName = CurrentState.RoomLayerName;
            this.YdbFileName = CurrentState.YdbFileName;
            this.AreaFileName = CurrentState.AreaFileName;
        }

        #endregion
    }
}
