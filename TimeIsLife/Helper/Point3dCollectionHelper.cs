using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    public static class Point3dCollectionHelper
    {
        public static Point3dCollection TransformBy(this Point3dCollection point3DCollection, Matrix3d matrix3d)
        {
            var result = new Point3dCollection();
            foreach (var point in point3DCollection)
            {
                result.Add(((Point3d)point).TransformBy(matrix3d));
            }
            return result;
        }
    }
}
