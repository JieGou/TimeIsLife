using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.NTSHelper
{
    internal static class PointHelper
    {
        internal static Point3d ToPoint3d(this Point point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
    }
}
