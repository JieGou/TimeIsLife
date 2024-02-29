using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    internal static class BlockReferenceHelper
    {
        /// <summary>
        /// 获取与BlockReference关联的单位缩放因子。
        /// </summary>
        /// <param name="blockReference">要获取其单位缩放因子的BlockReference对象。</param>
        /// <param name="n">用于调整缩放因子的正整数。如果小于1，将被视为1。</param>
        /// <returns>返回Scale3d对象，其X、Y和Z分量被调整为单位长度的n倍，同时保留原始缩放因子的符号。</returns>
        public static Scale3d GetUnitScale3d(this BlockReference blockReference, int n)
        {
            // 确保n的值至少为1
            if (n < 1)
            {
                n = 1;
            }

            // 获取BlockReference的缩放因子
            Scale3d scale3D = blockReference.ScaleFactors;

            // 调整缩放因子：使其长度为单位长度的n倍，同时保持原始缩放方向（正负符号）
            return new Scale3d(
                (scale3D.X / Math.Abs(scale3D.X)) * n, // 计算X方向的单位缩放因子，并乘以n
                (scale3D.Y / Math.Abs(scale3D.Y)) * n, // 计算Y方向的单位缩放因子，并乘以n
                (scale3D.Z / Math.Abs(scale3D.Z)) * n  // 计算Z方向的单位缩放因子，并乘以n
            );
        }


        /// <summary>
        /// 获取与指定块引用相关联的连接点。
        /// </summary>
        /// <param name="blockRef">要从中提取连接点的BlockReference对象。</param>
        /// <returns>包含在世界坐标系中的连接点的Point3dCollection对象。</returns>
        public static Point3dCollection GetConnectionPoints(this BlockReference blockRef)
        {
            // 获取块引用所在的数据库
            Database database = blockRef.Database;

            // 创建一个新的点集合用于存储连接点
            Point3dCollection connectionPoints = new Point3dCollection();

            // 获取块引用的变换矩阵，用于将点从块坐标系转换到世界坐标系
            Matrix3d transform = blockRef.BlockTransform;

            // 开始事务以读取块引用
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                // 获取块引用指向的块记录（BlockTableRecord）
                BlockTableRecord btr = transaction.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                // 如果块记录无效，则直接返回空的连接点集合
                if (btr == null) return connectionPoints;

                // 遍历块记录中的每个对象
                foreach (ObjectId objId in btr)
                {
                    // 尝试将对象转换为DBPoint（表示连接点）
                    DBPoint dbPoint = transaction.GetObject(objId, OpenMode.ForRead) as DBPoint;

                    // 如果对象是DBPoint，则将其位置转换到世界坐标系，并添加到连接点集合中
                    if (dbPoint != null)
                    {
                        connectionPoints.Add(dbPoint.Position.TransformBy(transform));
                    }
                }
            }

            // 返回包含连接点的集合
            return connectionPoints;
        }
    }
}
