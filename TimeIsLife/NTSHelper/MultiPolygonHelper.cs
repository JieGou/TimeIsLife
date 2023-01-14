using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.NTSHelper
{
    internal static class MultiPolygonHelper
    {
        internal static PolylineCollection ToPolylineCollection(this MultiPolygon multiPolygon)
        {
            PolylineCollection polylineCollection = new PolylineCollection();


            return polylineCollection;
        }
    }
}
