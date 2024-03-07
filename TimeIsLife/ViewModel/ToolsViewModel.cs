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
    internal class ToolsViewModel : ObservableObject
    {
        public static ToolsViewModel Instance { get; set; }
        public ToolsViewModel()
        {
            Initialize();

            //工具
            QuickUcsCommand = new RelayCommand(QuickUcs);
            ConnectLineCommand = new RelayCommand(ConnectLine);
            ConnectLinesCommand = new RelayCommand(ConnectLines);
            SetCurrentStatusCommand = new RelayCommand(SetCurrentStatus);
            AlignUcsCommand = new RelayCommand(AlignUcs);
            EquipmentAngleCommand = new RelayCommand(EquipmentAngle);
            ExplodeMInsertBlockCommand = new RelayCommand(ExplodeMInsertBlock);
            ModifyTextStyleCommand = new RelayCommand(ModifyTextStyle);
        }


        private void Initialize()
        {
            Instance = this;
        }
        
        #region 工具
        public IRelayCommand QuickUcsCommand { get; }
        void QuickUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_QuickUcs\n", true, false, true);
        }

        public IRelayCommand SetCurrentStatusCommand { get; }
        void SetCurrentStatus()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F2_SetCurrentStatus\n", true, false, true);
        }

        public IRelayCommand ConnectLineCommand { get; }
        void ConnectLine()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F1_ConnectLine\n", true, false, true);
        }

        public IRelayCommand ConnectLinesCommand { get; }
        void ConnectLines()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F3_ConnectLines\n", true, false, true);
        }

        public IRelayCommand AlignUcsCommand { get; }
        void AlignUcs()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F4_AlignUcs\n", true, false, true);
        }

        public IRelayCommand EquipmentAngleCommand { get; }
        void EquipmentAngle()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("F5_EquipmentAngle\n", true, false, true);
        }

        public IRelayCommand ExplodeMInsertBlockCommand { get; }
        void ExplodeMInsertBlock()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ExplodeMInsertBlock\n", true, false, true);
        }

        public IRelayCommand ModifyTextStyleCommand { get; }
        void ModifyTextStyle()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("FF_ModifyTextStyle\n", true, false, true);
        }
        #endregion
    }
}
