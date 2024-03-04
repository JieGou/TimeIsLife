using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

using NetTopologySuite.Densify;

namespace TimeIsLife.Model
{
    internal class FireArea
    {
        public string Name { get; set; }
        public Polyline Polyline { get; set; }
        public string DeviceType { get; set; } = "JXX"; // 默认值为"JXX"，但可以修改

        private Device cachedDevice = null; // 缓存Device对象

        public Device Device
        {
            get
            {
                // 如果Name或DeviceType变化了，需要重新计算
                if (cachedDevice == null || !cachedDevice.DeviceType.Equals(DeviceType))
                {
                    int deviceTypeLength = DeviceType.Length;
                    int index = Name.IndexOf(DeviceType);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"Name does not contain the device type {DeviceType}.");
                    }

                    cachedDevice = new Device
                    {
                        Partition = Name.Substring(0, index),
                        DeviceType = DeviceType,
                        Floor = Name.Substring(index + deviceTypeLength)
                    };
                }
                return cachedDevice;
            }
        }
    }

}
