using Autodesk.AutoCAD.Geometry;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.NTSHelper
{
    internal static class CoordinateHelper
    {
        internal static Point3d ToPoint3d(this Coordinate coordinate)
        {
            return new Point3d(coordinate.X, coordinate.Y, coordinate.Z);
        }

        internal static Point2d ToPoint2d(this Coordinate coordinate)
        {
            return new Point2d(coordinate.X, coordinate.Y);
        }
    }
}
