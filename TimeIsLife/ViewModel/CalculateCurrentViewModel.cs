using Accord.Math;

using Autodesk.AutoCAD.ApplicationServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.ViewModel
{
    internal class CalculateCurrentViewModel : ObservableObject
    {
        public static CalculateCurrentViewModel Instance { get; set; }
        public CalculateCurrentViewModel()
        {
            Initialize();

            //计算电流
            CalculateCurrentCommand = new RelayCommand(CalculateCurrent);
            SumPowerCommand = new RelayCommand(SumPower);
            AddCalculateCurrentCommand = new RelayCommand(AddCalculateCurrent);
            AddSumPowerCommand = new RelayCommand(AddSumPower);
        }

        private void Initialize()
        {
            Instance = this;

            pe = 0;
            normalOrFirePower = 0;
            normalInFirePower = 0;
            kx = 0.8;
            cosø = 0.85;
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

            Ic = Math.Round((pe * kx) / (0.38 * Math.Sqrt(3) * cosø), 2);
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
    }
}
