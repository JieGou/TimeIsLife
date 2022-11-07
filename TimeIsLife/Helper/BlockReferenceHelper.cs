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
        public static Scale3d GetUnitScale3d(this BlockReference blockReference,int n)
        {
            if (n < 1)
            {
                n=1;
            }
            Scale3d scale3D = blockReference.ScaleFactors;
            return new Scale3d((scale3D.X / Math.Abs(scale3D.X))*n , scale3D.Y / (Math.Abs(scale3D.Y))*n , (scale3D.Z / Math.Abs(scale3D.Z)) * n );            
        }
    }
}
