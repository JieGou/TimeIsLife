using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public static class GiftWrapping
    {
        public static List<LineString> CreateNonCrossingLines(List<Point> points, GeometryFactory geometry)
        {
            int count = points.Count;
            var result = new List<LineString>();
            List<Point> orderedPoints = new List<Point>();

            int leftMostIndex = 0;
            for (int i = 1; i < count; i++)
            {
                if (points[i].X < points[leftMostIndex].X)
                {
                    leftMostIndex = i;
                }
            }

            int currentIndex = leftMostIndex;
            int nextIndex;
            do
            {
                orderedPoints.Add(points[currentIndex]);
                nextIndex = (currentIndex + 1) % count;

                for (int i = 0; i < count; i++)
                {
                    double direction = (points[nextIndex].X - points[currentIndex].X) * (points[i].Y - points[currentIndex].Y) -
                                       (points[nextIndex].Y - points[currentIndex].Y) * (points[i].X - points[currentIndex].X);

                    if (direction < 0 || (direction == 0 && (points[i].X - points[currentIndex].X) > (points[nextIndex].X - points[currentIndex].X)))
                    {
                        nextIndex = i;
                    }
                }
                currentIndex = nextIndex;
            } while (currentIndex != leftMostIndex);

            for (int i = 0; i < orderedPoints.Count - 1; i++)
            {
                result.Add(geometry.CreateLineString(new[]
                {
                new Coordinate(orderedPoints[i].X, orderedPoints[i].Y),
                new Coordinate(orderedPoints[i + 1].X, orderedPoints[i + 1].Y)
            }));
            }

            // 添加从终点到起点的最后一条线
            result.Add(geometry.CreateLineString(new[]
            {
            new Coordinate(orderedPoints[orderedPoints.Count - 1].X, orderedPoints[orderedPoints.Count - 1].Y),
            new Coordinate(orderedPoints[0].X, orderedPoints[0].Y)
        }));

            return result;
        }
    }
}
