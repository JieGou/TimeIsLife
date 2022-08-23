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
        public ElectricalViewModel()
        {
            CalculateCurrentCommand = new RelayCommand(CalculateCurrent);
            SumPowerCommand = new RelayCommand(SumPower);
            Mothed1Command = new RelayCommand(Mothed1);
            Mothed2Command = new RelayCommand(Mothed2);
        }

        #region 计算电流

        //额定功率
        private double pe;
        public double Pe
        {
            get => pe;
            set => SetProperty(ref pe, value);

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


        public IRelayCommand CalculateCurrentCommand { get; }

        void CalculateCurrent()
        {
            if (pe == 0 || kx == 0 || cosø == 0) return;

            Ic = Math.Round((pe * kx) / (Math.Sqrt(3) * cosø), 2);
        }

        public IRelayCommand SumPowerCommand { get; }

        void SumPower()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_SumPower\n", true, false, false);
        }

        public IRelayCommand Mothed1Command { get; }
        void Mothed1()
        {

        }

        public IRelayCommand Mothed2Command { get; }
        void Mothed2()
        {

        }
        #endregion
    }
}
