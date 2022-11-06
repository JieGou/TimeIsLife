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
            ToHydrantAlarmButtonCommand = new RelayCommand(ToHydrantAlarmButton);


            //工具
            QuickUcsCommand = new RelayCommand(QuickUcs);
            ConnectLineCommand = new RelayCommand(ConnectLine);
            ConnectLinesCommand = new RelayCommand(ConnectLines);
            SetCurrentStatusCommand = new RelayCommand(SetCurrentStatus);
            AlignUcsCommand = new RelayCommand(AlignUcs);
            EquipmentAngleCommand = new RelayCommand(EquipmentAngle);
            ExplodeMInsertBlockCommand = new RelayCommand(ExplodeMInsertBlock);
            ModifyTextStyleCommand = new RelayCommand(ModifyTextStyle);

            //矩形布置
            GetAreaCommand = new RelayCommand(GetArea);
            RecLightingCommand = new RelayCommand(RecLighting);
            LightingCountCalculateCommand = new RelayCommand(LightingCountCalculate);
            LightingCountCalculate2Command = new RelayCommand(LightingCountCalculate2);

            //灯具沿线布置
            CurveLightingCommand = new RelayCommand(CurveLighting);

            //室形指数
            RoomIndexCalculateCommand = new RelayCommand(RoomIndexCalculate);

            //照度计算
            KongJianLiYongXiShu = 0.5;
            WeiHuXiShu = 0.8;
            IlluminanceCalculateCommand = new RelayCommand(IlluminanceCalculate);
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

            IsAlongTheLine = true;
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
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_SumPower\n", true, false, true);
        }

        public IRelayCommand AddCalculateCurrentCommand { get; }
        void AddCalculateCurrent()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_AddCalculateCurrent\n", true, false, true);
        }

        public IRelayCommand AddSumPowerCommand { get; }
        void AddSumPower()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_AddSumPower\n", true, false, true);
        }
        #endregion

        #region 火灾自动报警

        #region 属性
        //计算电流
        private bool isLayoutAtHole;
        public bool IsLayoutAtHole
        {
            get => isLayoutAtHole;
            set => SetProperty(ref isLayoutAtHole, value);
        }
        #endregion

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

        

        public IRelayCommand ToHydrantAlarmButtonCommand { get; }
        void ToHydrantAlarmButton()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ToHydrantAlarmButton\n", true, false, false);
        }
        #endregion

        #region 工具
        public IRelayCommand QuickUcsCommand { get; }
        void QuickUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_QuickUcs\n", true, false, true);
        }

        public IRelayCommand SetCurrentStatusCommand { get; }
        void SetCurrentStatus()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F2_SetCurrentStatus\n", true, false, true);
        }

        public IRelayCommand ConnectLineCommand { get; }
        void ConnectLine()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F1_ConnectLine\n", true, false, true);
        }

        public IRelayCommand ConnectLinesCommand { get; }
        void ConnectLines()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F3_ConnectLines\n", true, false, true);
        }

        public IRelayCommand AlignUcsCommand { get; }
        void AlignUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F4_AlignUcs\n", true, false, true);
        }

        public IRelayCommand EquipmentAngleCommand { get; }
        void EquipmentAngle()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F5_EquipmentAngle\n", true, false, true);
        }

        public IRelayCommand ExplodeMInsertBlockCommand { get; }
        void ExplodeMInsertBlock()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ExplodeMInsertBlock\n", true, false, true);
        }

        public IRelayCommand ModifyTextStyleCommand { get; }
        void ModifyTextStyle()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ModifyTextStyle\n", true, false, true);
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
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_GetArea\n", true, false, true);
        }

        public IRelayCommand RecLightingCommand { get; }
        void RecLighting()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_RecLighting\n", true, false, true);
        }


        public IRelayCommand LightingCountCalculateCommand { get; }
        void LightingCountCalculate()
        {
            if (lightingPower > 0)
            {
                LightingCount = Math.Round(lightingArea * lightingPowerDensity / lightingPower, 2);
            }
        }

        public IRelayCommand LightingCountCalculate2Command { get; }
        void LightingCountCalculate2()
        {
            DengJuShuLiang = LightingRow * LightingColumn;
        }
        #endregion

        #endregion

        #region 灯具沿线布置

        //灯具布置数量
        private int lightingLineCount;
        public int LightingLineCount
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
        public IRelayCommand CurveLightingCommand { get; }

        void CurveLighting()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_CurveLighting\n", true, false, true);
        }
        #endregion

        #region 照度计算

        //空间利用系数
        private double kongJianLiYongXiShu;
        public double KongJianLiYongXiShu 
        {
            get => kongJianLiYongXiShu;
            set => SetProperty(ref kongJianLiYongXiShu, value);
        }

        //维护系数
        private double weiHuXiShu;
        public double WeiHuXiShu
        {
            get => weiHuXiShu;
            set => SetProperty(ref weiHuXiShu, value);
        }

        //灯具数量
        private double dengJuShuLiang;
        public double DengJuShuLiang
        {
            get => dengJuShuLiang;
            set => SetProperty(ref dengJuShuLiang, value);
        }

        //灯具光通量
        private double dengJuGuangTongLiang;
        public double DengJuGuangTongLiang 
        {
            get => dengJuGuangTongLiang;
            set => SetProperty(ref dengJuGuangTongLiang, value);
        }

        //照度
        private double zhaoDu;
        public double ZhaoDu
        {
            get => zhaoDu;
            set => SetProperty(ref zhaoDu, value);
        }

        //照度偏差
        private string zhaoDuPianCha;
        public string ZhaoDuPianCha
        {
            get => zhaoDuPianCha;
            set => SetProperty(ref zhaoDuPianCha, value);
        }

        public IRelayCommand IlluminanceCalculateCommand { get; }
        void IlluminanceCalculate()
        {
            if (KongJianLiYongXiShu > 0 && WeiHuXiShu > 0 && DengJuShuLiang > 0 && DengJuGuangTongLiang > 0 && ZhaoDu>0 && LightingArea>0)
            {
                double a = Math.Round((Math.Round(KongJianLiYongXiShu * WeiHuXiShu * DengJuShuLiang * DengJuGuangTongLiang / LightingArea, 2) - ZhaoDu) / ZhaoDu, 2);
                ZhaoDuPianCha = $"{a:P0}";
            }
        }

        #endregion

        #region 室形指数
        //房间长
        private double roomLength;
        public double RoomLength
        {
            get => roomLength;
            set => SetProperty(ref roomLength, value);
        }

        //房间宽
        private double roomWidth;
        public double RoomWidth
        {
            get => roomWidth;
            set => SetProperty(ref roomWidth, value);
        }

        //灯具安装高度
        private double lightingHight;
        public double LightingHight
        {
            get => lightingHight;
            set => SetProperty(ref lightingHight, value);
        }

        //工作面高度
        private double workPlane;
        public double WorkPlane
        {
            get => workPlane;
            set => SetProperty(ref workPlane, value);
        }

        //室形指数
        private double roomIndex;
        public double RoomIndex
        {
            get => roomIndex;
            set => SetProperty(ref roomIndex, value);
        }

        public IRelayCommand RoomIndexCalculateCommand { get; }
        void RoomIndexCalculate()
        {
            if (roomLength > 0 && roomWidth > 0 && lightingHight > 0 && workPlane >= 0)
            {
                RoomIndex = Math.Round((roomLength * roomWidth) / ((roomLength + roomWidth) * (lightingHight - workPlane)), 2);
            }
        }
        #endregion
    }
}
