using Autodesk.AutoCAD.Geometry;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class Area
    {
        private Point3d point3D;

        public int ID { get; }
        public double Level { get { return Floor.Level; } }
        public AreaFloor Floor { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string Z { get; set; }

        //0为楼层区域，1为防火分区，2为房间区域
        public int Kind { get; set; }
        public string Note { get; set; }
        public Point3dCollection Point3dCollection
        {
            get
            {
                int n = X.Split(',').Length;
                Point3dCollection point3DCollection = new Point3dCollection();
                for (int i = 0; i < n; i++)
                {
                    point3DCollection.Add(new Point3d(double.Parse(X.Split(',')[i % (n - 1)]), double.Parse(Y.Split(',')[i % (n - 1)]), double.Parse(Z.Split(',')[i % (n - 1)])));
                }
                return point3DCollection;
            }
        }

        public Point3d BasePoint
        {
            get
            {
                return new Point3d(Floor.X, Floor.Y, Floor.Z);
            }
        }
    }
}
