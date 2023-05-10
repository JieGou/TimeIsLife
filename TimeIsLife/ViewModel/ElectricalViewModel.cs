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
using TimeIsLife.View;

namespace TimeIsLife.ViewModel
{
    class ElectricalViewModel : ObservableObject
    {
        public static ElectricalViewModel Instance { get; set; }
        public ElectricalViewModel()
        {
            Initialize();

        }

        private void Initialize()
        {
            Instance = this;
        }

    }
}
