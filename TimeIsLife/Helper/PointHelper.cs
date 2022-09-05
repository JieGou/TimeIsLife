using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    internal static class PointHelper
    {
        //public static Point3d Trans(this Point3d point, CoordinateSystemCode from, CoordinateSystemCode to)
        //{
        //    return new Point3d(Trans(point.ToArray(), from, to));
        //}

        public static Point3d Ucs2Wcs(this Point3d point)
        {

            return point.TransformBy( Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem);
        }

        public static Vector3d Ucs2Wcs(this Vector3d vec)
        {
            return vec.TransformBy(Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem);
        }

        //public static Point2d Wcs2Dcs(this Point3d point, bool atPaperSpace)
        //{
        //    return new Point2d(Trans(point.ToArray(), CoordinateSystemCode.Wcs, atPaperSpace ? CoordinateSystemCode.PDcs : CoordinateSystemCode.MDcs));
        //}

        //public static Vector2d Wcs2Dcs(this Vector3d vec, bool atPaperSpace)
        //{
        //    return new Vector2d(Trans(vec.ToArray(), CoordinateSystemCode.Wcs, atPaperSpace ? CoordinateSystemCode.PDcs : CoordinateSystemCode.MDcs));
        //}

        public static Point3d Wcs2Ucs(this Point3d point)
        {
            return point.TransformBy(Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.Inverse());
        }

        public static Vector3d Wcs2Ucs(this Vector3d vec)
        {
            return vec.TransformBy(Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.Inverse());
        }
    }
}
