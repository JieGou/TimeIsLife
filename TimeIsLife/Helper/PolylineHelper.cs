using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using DotNetARX;

using NetTopologySuite.Geometries;

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
        /// <summary>
        /// 获取三维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Point3dCollection GetPoint3DCollection(this Polyline polyline)
        {
            Point3dCollection points = new Point3dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }
            return points;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Point2dCollection GetPoint2DCollection(this Polyline polyline)
        {
            Point2dCollection points = new Point2dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(new Point2d(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }
            return points;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point2d> GetPoint2Ds(this Polyline polyline)
        {
            List<Point2d> points = new List<Point2d>();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(new Point2d(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }
            return points;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Coordinate> GetCoordinates(this Polyline polyline)
        {
            List<Coordinate> points = new List<Coordinate>();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(new Coordinate(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }
            return points;
        }

        /// <summary>
        /// 判断多边形1是否在多边形2的内部，true为在内部，false为不在内部
        /// </summary>
        /// <param name="roomPolyline">多边形1</param>
        /// <param name="slabPolyline">多边形2</param>
        /// <returns></returns>
        public static bool IsPolylineInPolyline(this Polyline Polyline1, Polyline Polyline2)
        {
            bool bo = true;
            for (int i = 0; i < Polyline1.NumberOfVertices; i++)
            {
                if (!Polyline1.GetPoint2dAt(i).IsInPolygon2(Polyline2.GetPoint2DCollection().ToArray().ToList()))
                {
                    bo = false;
                }
            }
            return bo;
        }

        public static string GetXValues(this Polyline polyline)
        {
            List<double> xValues = new List<double>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {

                xValues.Add(polyline.GetPoint3dAt(i).X);
            }
            return string.Join(",", xValues.ToArray());
        }

        public static string GetYValues(this Polyline polyline)
        {
            List<double> xValues = new List<double>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {

                xValues.Add(polyline.GetPoint3dAt(i).Y);
            }
            return string.Join(",", xValues.ToArray());
        }

        public static string GetZValues(this Polyline polyline)
        {
            List<double> xValues = new List<double>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {

                xValues.Add(polyline.GetPoint3dAt(i).Z);
            }
            return string.Join(",", xValues.ToArray());
        }
    }
}
