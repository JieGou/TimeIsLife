using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace TimeIsLife.Helper
{
    internal static class PolylineHelper
    {
        public static Point3dCollection GetPoint3DCollection(this Polyline polyline)
        {
            Point3dCollection points = new Point3dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }
            return points;
        }
    }
}
