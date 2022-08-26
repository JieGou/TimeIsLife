using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TimeIsLife.ViewModel
{
    class ElectricalViewModel:ObservableObject
    {
        public static ElectricalViewModel electricalViewModel;
        public ElectricalViewModel()
        {
            Initialize();

            //计算电流
            CalculateCurrentCommand = new RelayCommand(CalculateCurrent);
            SumPowerCommand = new RelayCommand(SumPower);
            AddCalculateCurrentCommand = new RelayCommand(AddCalculateCurrent);
            AddSumPowerCommand = new RelayCommand(AddSumPower);

            //火灾自动报警
            LoadYdbFileCommand = new RelayCommand(LoadYdbFile);
            FasCommand = new RelayCommand(Fas);

            //工具
            QuickUcsCommand = new RelayCommand(QuickUcs);
            ConnectLineCommand = new RelayCommand(ConnectLine);
            ConnectLinesCommand = new RelayCommand(ConnectLines);
            SetCurrentStatusCommand = new RelayCommand(SetCurrentStatus);
            AlignUcsCommand = new RelayCommand(AlignUcs);
            EquipmentAngleCommand = new RelayCommand(EquipmentAngle);
            PlineEquallyDividedCommand = new RelayCommand(PlineEquallyDivided);
            ExplodeMInsertBlockCommand = new RelayCommand(ExplodeMInsertBlock);

            //矩形布置
            GetAreaCommand = new RelayCommand(GetArea);
            RecLightingCommand = new RelayCommand(RecLighting);
            LightingCountCalculateCommand = new RelayCommand(LightingCountCalculate);

            //灯具沿线布置
            CurLightingCommand = new RelayCommand(CurLighting);
        }

        private void Initialize()
        {
            electricalViewModel = this;

            kx = 0.8;
            cosø = 0.85;

            BlockScales = new List<int>() { 1, 25, 50, 75, 100, 150, 200, 250 };
            BlockAngles = new List<int> { 0, 90, 180, 270 };
            Distances = new List<double> { 0.0, 0.5, 1.0 };

            BlockScale = BlockScales[4];
            BlockAngle = BlockAngles[0];
            Distance = Distances[1];

            IsLengthOrCount = true;
        }

        #region 计算电流

        //额定功率
        private double pe;
        public double Pe
        {
            get => pe;
            set => SetProperty(ref pe, value);

        }

        //平时\消防功率
        private double normalOrFirePower;
        public double NormalOrFirePower
        {
            get => normalOrFirePower;
            set => SetProperty(ref normalOrFirePower, value);

        }

        //消防平时功率
        private double normalInFirePower;
        public double NormalInFirePower
        {
            get => normalInFirePower;
            set => SetProperty(ref normalInFirePower, value);
        }

        //需要系数
        private double kx;
        public double Kx
        {
            get => kx;
            set => SetProperty(ref kx, value);
        }

        //功率因数
        private double cosø;
        public double Cosø
        {
            get => cosø;
            set => SetProperty(ref cosø, value);
        }

        //计算电流
        private double ic;
        public double Ic
        {
            get => ic;
            set => SetProperty(ref ic, value);
        }


        public IRelayCommand CalculateCurrentCommand { get; }

        void CalculateCurrent()
        {
            if (pe == 0 || kx == 0 || cosø == 0) return;

            Ic = Math.Round((pe * kx) / (0.38*Math.Sqrt(3) * cosø), 2);
        }

        public IRelayCommand SumPowerCommand { get; }

        void SumPower()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_SumPower\n", true, false, false);
        }

        public IRelayCommand AddCalculateCurrentCommand { get; }
        void AddCalculateCurrent()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_AddCalculateCurrent\n", true, false, false);
        }

        public IRelayCommand AddSumPowerCommand { get; }
        void AddSumPower()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_AddSumPower\n", true, false, false);
        }
        #endregion

        #region 火灾自动报警
        public IRelayCommand LoadYdbFileCommand { get; }
        void LoadYdbFile()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_LoadYdbFile\n", true, false, false);
        }

        public IRelayCommand FasCommand { get; }
        void Fas()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_FAS\n", true, false, false);
        }
        #endregion

        #region 工具
        public IRelayCommand QuickUcsCommand { get; }
        void QuickUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_QuickUcs\n", true, false, false);
        }

        public IRelayCommand SetCurrentStatusCommand { get; }
        void SetCurrentStatus()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F2_SetCurrentStatus\n", true, false, false);
        }

        public IRelayCommand ConnectLineCommand { get; }
        void ConnectLine()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F1_ConnectLine\n", true, false, false);
        }

        public IRelayCommand ConnectLinesCommand { get; }
        void ConnectLines()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F3_ConnectLines\n", true, false, false);
        }

        public IRelayCommand AlignUcsCommand { get; }
        void AlignUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F4_AlignUcs\n", true, false, false);
        }

        public IRelayCommand EquipmentAngleCommand { get; }
        void EquipmentAngle()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F5_EquipmentAngle\n", true, false, false);
        }

        public IRelayCommand PlineEquallyDividedCommand { get; }
        void PlineEquallyDivided()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_PlineEquallyDivided\n", true, false, false);
        }

        public IRelayCommand ExplodeMInsertBlockCommand { get; }
        void ExplodeMInsertBlock()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ExplodeMInsertBlock\n", true, false, false);
        }
        #endregion

        #region 矩形布置

        #region 属性

        //布灯的区域面积
        private double lightingArea;
        public double LightingArea
        {
            get => lightingArea;
            set => SetProperty(ref lightingArea, value);
        }

        //功率密度
        private double lightingPowerDensity;
        public double LightingPowerDensity
        {
            get => lightingPowerDensity;
            set => SetProperty(ref lightingPowerDensity, value);
        }

        //灯具功率
        private double lightingPower;
        public double LightingPower
        {
            get => lightingPower;
            set => SetProperty(ref lightingPower, value);
        }

        //灯具数量
        private double lightingCount;
        public double LightingCount
        {
            get => lightingCount;
            set => SetProperty(ref lightingCount, value);
        }

        //灯具行数
        private int lightingRow;
        public int LightingRow
        {
            get => lightingRow;
            set => SetProperty(ref lightingRow, value);
        }

        //灯具列数
        private int lightingColumn;
        public int LightingColumn
        {
            get => lightingColumn;
            set => SetProperty(ref lightingColumn, value);
        }


        public List<int> BlockScales { get; set; }
        public List<int> BlockAngles { get; set; }
        public List<double> Distances { get; set; }

        //块比例

        private int blockScale;
        public int BlockScale
        {
            get => blockScale;
            set => SetProperty(ref blockScale, value);
        }
        //块角度

        private int blockAngle;
        public int BlockAngle
        {
            get => blockAngle;
            set => SetProperty(ref blockAngle, value);
        }
        //距墙距离

        private double distance;
        public double Distance
        {
            get => distance;
            set => SetProperty(ref distance, value);
        }

        #endregion

        #region 命令

        public IRelayCommand GetAreaCommand { get; }
        void GetArea()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_GetArea\n", true, false, false);
        }

        public IRelayCommand RecLightingCommand { get; }
        void RecLighting()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_RecLighting\n", true, false, false);
        }


        public IRelayCommand LightingCountCalculateCommand { get; }
        void LightingCountCalculate()
        {
            if (lightingPower > 0)
            {
                LightingCount = Math.Round(lightingArea * lightingPowerDensity / lightingPower, 2);
            }
        }


        #endregion

        #endregion


        #region 灯具沿线布置

        //灯具布置间距
        private double lightingLineCount;
        public double LightingLineCount
        {
            get => lightingLineCount;
            set => SetProperty(ref lightingLineCount, value);
        }

        //灯具布置间距
        private double lightingLength;
        public double LightingLength
        {
            get => lightingLength;
            set => SetProperty(ref lightingLength, value);
        }

        //灯具方向沿切线方向布置
        private bool isLengthOrCount;
        public bool IsLengthOrCount
        {
            get => isLengthOrCount;
            set => SetProperty(ref isLengthOrCount, value);
        }

        //灯具方向沿切线方向布置
        private bool isAlongTheLine;
        public bool IsAlongTheLine
        {
            get => isAlongTheLine;
            set => SetProperty(ref isAlongTheLine, value);
        }
        public IRelayCommand CurLightingCommand { get; }

        void CurLighting()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_CurLighting\n", true, false, false);
        }
        #endregion

    }
}
