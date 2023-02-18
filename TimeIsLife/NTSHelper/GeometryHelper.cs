using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

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

        internal static Coordinate Max(this Geometry geometry)
        {            
            double x = geometry.Coordinates[0][0];
            double y = geometry.Coordinates[0][1];
            for (int i = 0; i < geometry.NumPoints; i++)
            {
                if (x< geometry.Coordinates[i][0])
                {
                    x= geometry.Coordinates[i][0];
                }

                if (y< geometry.Coordinates[i][1])
                {
                    y= geometry.Coordinates[i][1];
                }
            }
            
            return new Coordinate(x,y);
        }

        internal static Coordinate Min(this Geometry geometry)
        {
            double x = geometry.Coordinates[0][0];
            double y = geometry.Coordinates[0][1];
            for (int i = 0; i < geometry.NumPoints; i++)
            {
                if (x > geometry.Coordinates[i][0])
                {
                    x = geometry.Coordinates[i][0];
                }

                if (y > geometry.Coordinates[i][1])
                {
                    y = geometry.Coordinates[i][1];
                }
            }

            return new Coordinate(x, y);
        }
    }
}
