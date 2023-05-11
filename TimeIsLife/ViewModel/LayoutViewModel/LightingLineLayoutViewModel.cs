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
    internal class LightingLineLayoutViewModel : ObservableObject
    {
        public static LightingLineLayoutViewModel Instance { get; set; }
        public LightingLineLayoutViewModel()
        {
            Initialize();

            //灯具沿线布置
            CurveLightingCommand = new RelayCommand(CurveLighting);

        }

        private void Initialize()
        {
            Instance = this;

            IsAlongTheLine = true;
            IsLengthOrCount = true;
        }

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

    }
}
