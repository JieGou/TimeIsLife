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


            
        }

        private void Initialize()
        {
            kx = 0.8;
            cosø = 0.85;
            normalOrFirePower = 50;
            normalInFirePower = 30;
            pe = 50;
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
    }
}
