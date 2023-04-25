using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class FireAlarmWindowState
    {
        public string FloorLayerName { get; set; }
        public string FireAlarmLayerName { get; set; }
        public string RoomLayerName { get; set; }
        public string YdbFileName { get; set; }
        public string AreaFileName { get; set; }
        public int SlabThickness { get; set; }
    }
}
