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

namespace TimeIsLife.ViewModel.LayoutViewModel
{
    internal class LightingLayoutSettingViewModel : ObservableObject
    {
        public static LightingLayoutSettingViewModel Instance { get; set; }
        public LightingLayoutSettingViewModel()
        {
            Initialize();
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
    }
}
