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
        }

        private void Initialize()
        {
            Instance = this;
        }
    }
}
