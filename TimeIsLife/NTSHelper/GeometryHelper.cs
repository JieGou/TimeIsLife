using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.NTSHelper
{
    internal static class GeometryHelper
    {

        internal static Polyline ToPolyline(this Geometry geometry)
        {
            Polyline polyline = new Polyline();
            for (int i = 0; i < geometry.NumPoints; i++)
            {
                polyline.AddVertexAt(i, geometry.Coordinates[i].ToPoint2d(), 0, 0, 0);
            }
            return polyline;
        }
    }
}
