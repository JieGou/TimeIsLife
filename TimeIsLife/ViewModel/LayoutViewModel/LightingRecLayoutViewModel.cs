using Autodesk.AutoCAD.ApplicationServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.ViewModel.LayoutViewModel
{
    internal class LightingRecLayoutViewModel : ObservableObject
    {
        public static LightingRecLayoutViewModel Instance { get; set; }
        public LightingRecLayoutViewModel()
        {
            Initialize();

            //矩形布置
            GetAreaCommand = new RelayCommand(GetArea);
            RecLightingCommand = new RelayCommand(RecLighting);
            LightingCountCalculateCommand = new RelayCommand(LightingCountCalculate);
            LightingCountCalculate2Command = new RelayCommand(LightingCountCalculate2);

            //室形指数
            RoomIndexCalculateCommand = new RelayCommand(RoomIndexCalculate);

            //照度计算            
            IlluminanceCalculateCommand = new RelayCommand(IlluminanceCalculate);
        }

        private void Initialize()
        {
            Instance = this;
            KongJianLiYongXiShu = 0.5;
            WeiHuXiShu = 0.8;
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
