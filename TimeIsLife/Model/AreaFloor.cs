using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class AreaFloor: ObservableObject
    {
        public int ID { get; set; }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private double level;
        public double Level
        {
            get => level;
            set => SetProperty(ref level, value);
        }

        private double x;
        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }
        
        private double y;
        public double Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }

        private double z;
        public double Z
        {
            get => z; set => SetProperty(ref z, value);
        }
    }
}
