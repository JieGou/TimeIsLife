using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestApp
{
    internal class MainWindowViewModel: ObservableObject
    {
        public MainWindowViewModel()
        {
            CalculateCurrentCommand = new RelayCommand(CalculateCurrent);
            SumPowerCommand = new RelayCommand(SumPower);
        }
        #region 计算电流

        //额定功率
        private double pe;
        public double Pe
        {
            get => pe;
            set => SetProperty(ref pe, value);

        }

        //是否显示明细
        private Visibility isVisibility;
        public Visibility IsVisibility
        {
            get => isVisibility;
            set => SetProperty(ref isVisibility, value);

        }

        //消防功率
        private double firePower;
        public double FirePower
        {
            get => firePower;
            set => SetProperty(ref firePower, value);

        }

        //非消防功率
        private double nFirePower;
        public double NFirePower
        {
            get => nFirePower;
            set => SetProperty(ref nFirePower, value);

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

        
        public IRelayCommand CalculateCurrentCommand { get;  }

        void CalculateCurrent()
        {
            if (pe == 0 || kx == 0 || cosø == 0) return;

            Ic = Math.Round((pe* kx) / (Math.Sqrt(3) * cosø), 2);
        }

        public IRelayCommand SumPowerCommand { get; }

        void SumPower()
        {
            firePower = 0;
            nFirePower = 0;
            if (nFirePower == 0) return;

            firePower = 0;
            nFirePower = 0;
        }
        #endregion
    }
}
