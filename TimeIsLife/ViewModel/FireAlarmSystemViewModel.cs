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
// ReSharper disable InconsistentNaming

namespace TimeIsLife.ViewModel
{
    internal class FireAlarmSystemViewModel : ObservableObject
    {
        public FireAlarmSystemViewModel()
        {
            //火灾自动报警
            F6_Command = new RelayCommand(F6);
            F7_Command = new RelayCommand(F7);
            F8_Command = new RelayCommand(F8);
            F9_Command = new RelayCommand(F9);
        }
        
        public IRelayCommand F6_Command { get; }
        void F6()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F6_LoadYdbFile\n", true, false, false);
        }

        public IRelayCommand F7_Command { get; }
        void F7()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F7_LayoutEquipment\n", true, false, false);
        }


        public IRelayCommand F8_Command { get; }
        void F8()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F8_EquipmentConnectLine\n", true, false, false);
        }

        public IRelayCommand F9_Command { get; }
        void F9()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F9_FAS\n", true, false, false);
        }

    }
}
