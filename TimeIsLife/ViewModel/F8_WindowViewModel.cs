using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Dapper;

using DotNetARX;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using TimeIsLife.Helper;
using TimeIsLife.Jig;
using TimeIsLife.Model;
using TimeIsLife.View;

using TimeIsLife.View;

namespace TimeIsLife.ViewModel
{
    internal class F8_WindowViewModel : ObservableObject
    {

        public static F8_WindowViewModel Instance { get; set; }
        public F8_WindowViewModel()
        {
            Initialize();
            GetFireAreaLayerNameCommand = new RelayCommand(GetFireAreaLayerName);
            GetAvoidanceAreaLayerNameCommand = new RelayCommand(GetAvoidanceAreaLayerName);
            GetEquipmentLayerNameCommand = new RelayCommand(GetEquipmentLayerName);
            GetWireLayerNameCommand = new RelayCommand(GetWireLayerName);
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }
        private void Initialize()
        {
            Instance = this;
        }


        public IRelayCommand GetFireAreaLayerNameCommand { get; }
        public IRelayCommand GetAvoidanceAreaLayerNameCommand { get; }
        public IRelayCommand GetEquipmentLayerNameCommand { get; }
        public IRelayCommand GetWireLayerNameCommand { get; }
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public bool Result { get; private set; }



        //防火分区
        private string fireAreaLayerName;
        public string FireAreaLayerName
        {
            get => fireAreaLayerName;
            set => SetProperty(ref fireAreaLayerName, value);
        }


        //禁止布线区域
        private string avoidanceAreaLayerName;
        public string AvoidanceAreaLayerName
        {
            get => avoidanceAreaLayerName;
            set => SetProperty(ref avoidanceAreaLayerName, value);
        }

        //设备图层
        private string equipmentLayerName;
        public string EquipmentLayerName
        {
            get => equipmentLayerName;
            set => SetProperty(ref equipmentLayerName, value);
        }

        //设备图层
        private string wireLayerName;
        public string WireLayerName
        {
            get => wireLayerName;
            set => SetProperty(ref wireLayerName, value);
        }
        private void GetFireAreaLayerName()
        {
            F8_Window.Instance.Hide();
            string message = @"选择表示防火分区的闭合多段线";
            string layerName = GetLayerName(message);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                F8_Window.Instance.ShowDialog();
                return;
            }
            Instance.FireAreaLayerName = layerName;
            F8_Window.Instance.ShowDialog();
        }

        private void GetAvoidanceAreaLayerName()
        {
            F8_Window.Instance.Hide();
            string message = @"选择表示禁止布线区域的闭合多段线";
            string layerName = GetLayerName(message);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                F8_Window.Instance.ShowDialog();
                return;
            }
            Instance.AvoidanceAreaLayerName = layerName;
            F8_Window.Instance.ShowDialog();
        }
        private void GetEquipmentLayerName()
        {
            F8_Window.Instance.Hide();
            string message = @"选择设备图层";
            string layerName = GetLayerName(message);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                F8_Window.Instance.ShowDialog();
                return;
            }
            Instance.EquipmentLayerName = layerName;
            F8_Window.Instance.ShowDialog();
        }
        private void GetWireLayerName()
        {
            F8_Window.Instance.Hide();
            string message = @"选择线缆图层";
            string layerName = GetLayerName(message);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                F8_Window.Instance.ShowDialog();
                return;
            }
            Instance.WireLayerName = layerName;
            F8_Window.Instance.ShowDialog();
        }
        private void Confirm()
        {
            Result = true;
            F8_Window.Instance.Close();
        }
        private void Cancel()
        {
            Result = false;
            F8_Window.Instance.Close();
        }
        public void SaveState()
        {

            MyPlugin.CurrentUserData.FireAreaLayerName = FireAreaLayerName;
            MyPlugin.CurrentUserData.AvoidanceAreaLayerName = AvoidanceAreaLayerName;
            MyPlugin.CurrentUserData.EquipmentLayerName = EquipmentLayerName;
            MyPlugin.CurrentUserData.WireLayerName = WireLayerName;
        }
        public void LoadState()
        {
            FireAreaLayerName = MyPlugin.CurrentUserData.FireAreaLayerName;
            AvoidanceAreaLayerName = MyPlugin.CurrentUserData.AvoidanceAreaLayerName;
            EquipmentLayerName = MyPlugin.CurrentUserData.EquipmentLayerName;
            WireLayerName = MyPlugin.CurrentUserData.WireLayerName;
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
