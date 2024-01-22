using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class BasePoint
    {
        public string Name { get; set; }
        public double Level { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Point3d Point3d
        {
            get
            {
                return new Point3d(X, Y, Z);
            }
        }
    }
}
