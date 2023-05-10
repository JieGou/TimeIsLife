using Accord.Math;

using Autodesk.AutoCAD.ApplicationServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Windows.Forms.MonthCalendar;

namespace TimeIsLife.ViewModel
{
    internal class LightingLayoutViewModel : ObservableObject
    {
        public static LightingLayoutViewModel Instance { get; set; }
        public LightingLayoutViewModel()
        {
            Initialize();

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
            Instance = this;

            BlockScales = new List<int>() { 1, 25, 50, 75, 100, 150, 200, 250 };
            BlockAngles = new List<int> { 0, 90, 180, 270 };
            Distances = new List<double> { 0.0, 0.5, 1.0 };

            BlockScale = BlockScales[4];
            BlockAngle = BlockAngles[0];
            Distance = Distances[1];

            IsAlongTheLine = true;
            IsLengthOrCount = true;
        }


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
            if (KongJianLiYongXiShu > 0 && WeiHuXiShu > 0 && DengJuShuLiang > 0 && DengJuGuangTongLiang > 0 && ZhaoDu > 0 && LightingArea > 0)
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
