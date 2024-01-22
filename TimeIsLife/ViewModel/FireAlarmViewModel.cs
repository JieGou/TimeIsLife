using Accord.Math;

using Autodesk.AutoCAD.ApplicationServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeIsLife.View;

namespace TimeIsLife.ViewModel
{
    internal class FireAlarmViewModel : ObservableObject
    {
        public static FireAlarmViewModel Instance { get; set; }
        public FireAlarmViewModel()
        {
            Initialize();

            //火灾自动报警
            LoadYdbFileCommand = new RelayCommand(LoadYdbFile);
            FasCommand = new RelayCommand(Fas);
            AutomaticConnectionCommand = new RelayCommand(AutomaticConnection);
            ToHydrantAlarmButtonCommand = new RelayCommand(ToHydrantAlarmButton);
            OpenFireAlarmWindowCommand = new RelayCommand(OpenFireAlarmWindow);
            FireAlarmCeilingCommand = new RelayCommand(FireAlarmCeiling);
        }

        private void Initialize()
        {
            Instance = this;

            MaxValue = 100;
            isTreeConnection = true;
            RadiusList = new List<int> { 3600, 4400, 5800, 6700 };
            Radius = 5800;
        }

        #region 属性
        //是否在洞口布置
        private bool isLayoutAtHole;
        public bool IsLayoutAtHole
        {
            get => isLayoutAtHole;
            set => SetProperty(ref isLayoutAtHole, value);
        }

        //最短路径阈值
        private int maxValue;
        public int MaxValue
        {
            get => maxValue;
            set => SetProperty(ref maxValue, value);
        }

        //半径
        public List<int> RadiusList { get; set; }

        private int radius;
        public int Radius
        {
            get => radius;
            set => SetProperty(ref radius, value);
        }

        //算法选择
        private bool isTreeConnection;
        public bool IsTreeConnection
        {
            get => isTreeConnection;
            set => SetProperty(ref isTreeConnection, value);
        }

        private bool isCircularConnection1;
        public bool IsCircularConnection1
        {
            get => isCircularConnection1;
            set => SetProperty(ref isCircularConnection1, value);
        }

        private bool isCircularConnection2;
        public bool IsCircularConnection2
        {
            get => isCircularConnection2;
            set => SetProperty(ref isCircularConnection2, value);
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


        public IRelayCommand AutomaticConnectionCommand { get; }
        void AutomaticConnection()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_AutomaticConnection\n", true, false, false);
        }

        public IRelayCommand ToHydrantAlarmButtonCommand { get; }
        void ToHydrantAlarmButton()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ToHydrantAlarmButton\n", true, false, false);
        }

        public IRelayCommand OpenFireAlarmWindowCommand { get; }
        void OpenFireAlarmWindow()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_LayoutEquipment\n", true, false, false);
        }

        public IRelayCommand FireAlarmCeilingCommand { get; }
        void FireAlarmCeiling()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_FireAlarmCeiling\n", true, false, false);
        }
    }
}
