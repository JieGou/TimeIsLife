using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class UserData
    {
        public string FloorLayerName { get; set; }
        public string FireAreaLayerName { get; set; }
        public string RoomLayerName { get; set; }
        public string AvoidanceAreaLayerName { get; set; }
        public string EquipmentLayerName { get; set; }
        public string WireLayerName { get; set; }
        public string YdbFileName { get; set; }
        public int SlabThickness { get; set; }

        public string TreeOrCircle { get; set; }
        // 集合属性示例
        public ObservableCollection<FireAlarmEquipment> FireAlarmEquipments { get; set; }
    }
}
