using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Precision;

namespace TimeIsLife.Model
{
    public class AvoidanceArea
    {
        public Polygon Area { get; private set; }

        public AvoidanceArea(IEnumerable<Coordinate> coordinates)
        {
            var geometryFactory = new GeometryFactory();
            Area = geometryFactory.CreatePolygon(coordinates.ToArray());
        }

        // 检测给定的线段是否穿过避开区域
        public bool Intersects(LineString line)
        {
            return Area.Intersects(line);
        }
    }
}
