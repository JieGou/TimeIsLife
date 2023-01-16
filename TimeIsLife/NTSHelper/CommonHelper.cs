using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.NTSHelper
{
    internal class CommonHelper
    {
        /// <summary>
        /// 返回由起点、中点、终点定义的圆弧的凸度
        /// </summary>
        /// <param name="startPoint">圆弧起点</param>
        /// <param name="middlePoint">圆弧中点</param>
        /// <param name="endPoint">圆弧终点</param>
        /// <returns></returns>
        private static double GetBulgeBy3Point(Point2d startPoint,Point2d middlePoint,Point2d endPoint)
        {
            //middlePoint到endPoint的向量的角度
            double angle1 = middlePoint.GetVectorTo(endPoint).Angle;
            //startPoint到middlePoint的向量的角度
            double angle2 = startPoint.GetVectorTo(middlePoint).Angle;
            //
            double bulge = Math.Tan((angle1 - angle2) / 2);
            return bulge;
        }
    }
}
