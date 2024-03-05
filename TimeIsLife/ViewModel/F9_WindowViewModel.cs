using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

using TimeIsLife.Helper;
using TimeIsLife.Model;
using TimeIsLife.View;
using MessageBox = System.Windows.MessageBox;

namespace TimeIsLife.ViewModel
{
    internal class F9_WindowViewModel: ObservableObject
    {
        public static F9_WindowViewModel Instance { get; set; }
        public F9_WindowViewModel()
        {
            Initialize();
            GetFireAreaLayerNameCommand = new RelayCommand(GetFireAreaLayerName);
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
            OpenFileDialogCommand1 = new RelayCommand<Object>(OpenFileDialog1);
            OpenFileDialogCommand2 = new RelayCommand<Object>(OpenFileDialog2);
        }
        private void Initialize()
        {
            Instance = this;
            FireAlarmEquipments = new ObservableCollection<FireAlarmEquipment>();
        }


        public IRelayCommand GetFireAreaLayerNameCommand { get; }
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand<Object> OpenFileDialogCommand1 { get; }
        public IRelayCommand<Object> OpenFileDialogCommand2 { get; }
        public bool Result { get; private set; }



        //防火分区
        private string fireAreaLayerName;
        public string FireAreaLayerName
        {
            get => fireAreaLayerName;
            set => SetProperty(ref fireAreaLayerName, value);
        }

        //防火分区
        private string deviceType;
        public string DeviceType
        {
            get => deviceType;
            set => SetProperty(ref deviceType, value);
        }

        private ObservableCollection<FireAlarmEquipment> fireAlarmEquipments;
        public ObservableCollection<FireAlarmEquipment> FireAlarmEquipments
        {
            get => fireAlarmEquipments;
            set => SetProperty(ref fireAlarmEquipments, value);
        }

        private void GetFireAreaLayerName()
        {
            F9_Window.Instance.Hide();
            string message = @"选择表示防火分区的闭合多段线";
            string layerName = GetLayerName(message);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                F9_Window.Instance.ShowDialog();
                return;
            }
            Instance.FireAreaLayerName = layerName;
            F9_Window.Instance.ShowDialog();
        }

        private void Confirm()
        {
            Result = true;
            F9_Window.Instance.Close();
        }
        private void Cancel()
        {
            Result = false;
            F9_Window.Instance.Close();
        }
        public void SaveState()
        {

            MyPlugin.CurrentUserData.FireAreaLayerName = FireAreaLayerName;
            MyPlugin.CurrentUserData.FireAlarmEquipments = FireAlarmEquipments;
            MyPlugin.CurrentUserData.DeviceType = DeviceType;
        }
        public void LoadState()
        {
            FireAreaLayerName = MyPlugin.CurrentUserData.FireAreaLayerName;
            FireAlarmEquipments = MyPlugin.CurrentUserData.FireAlarmEquipments;
            DeviceType = MyPlugin.CurrentUserData.DeviceType;
        }

        private void OpenFileDialog1(object obj)
        {
            // obj 是绑定的 DataGrid 行数据
            var fireAlarmEquipment = obj as FireAlarmEquipment;
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            string blockDirectory = Path.Combine(assemblyDirectory, "Block", "FA");
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = blockDirectory;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新路径
                fireAlarmEquipment.BlockPath = openFileDialog.FileName;
                // 或者 distributionBox.SchematicBlockPath = openFileDialog.FileName; 根据实际情况
            }
        }

        private void OpenFileDialog2(object obj)
        {
            // obj 是绑定的 DataGrid 行数据
            var fireAlarmEquipment = obj as FireAlarmEquipment;
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            string schematicBlockDirectory = Path.Combine(assemblyDirectory, "SchematicBlock", "FA");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = schematicBlockDirectory;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新路径
                fireAlarmEquipment.SchematicBlockPath = openFileDialog.FileName;
                // 或者 distributionBox.SchematicBlockPath = openFileDialog.FileName; 根据实际情况
            }
        }
        private string GetLayerName(string message)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                //选择选项
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions
                {
                    SingleOnly = true,
                    RejectObjectsOnLockedLayers = true,
                    MessageForAdding = message
                };

                SelectionSet selectionSet = editor.GetSelectionSet(SelectString.GetSelection, promptSelectionOptions, null, null);
                if (selectionSet == null) return null;
                Entity entity = transaction.GetObject(selectionSet.GetObjectIds().FirstOrDefault(), OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    MessageBox.Show(@"对象图层锁定！");
                    return null;
                }
                return entity.Layer;
            }
        }
    }
}
