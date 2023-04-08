using Autodesk.AutoCAD.Geometry;

using System.Collections.Generic;
using System.Linq;

namespace TimeIsLife.CADCommand
{
    partial class FireAlarmCommand1
    {
        public class Slab
        {
            public int ID { get; set; }
            public string GridsID { get; set; }
            public string VertexX { get; set; }
            public string VertexY { get; set; }
            public string VertexZ { get; set; }
            public int Thickness { get; set; }
            public Floor Floor { get; set; }

            public void TranslateVertices(Vector3d translationVector)
            {
                List<double> xValues = ParseValues(VertexX);
                List<double> yValues = ParseValues(VertexY);
                List<double> zValues = ParseValues(VertexZ);

                xValues = xValues.Select(x => x + translationVector.X).ToList();
                yValues = yValues.Select(y => y + translationVector.Y).ToList();
                zValues = zValues.Select(z => z + translationVector.Z).ToList();

                VertexX = FormatValues(xValues);
                VertexY = FormatValues(yValues);
                VertexZ = FormatValues(zValues);
            }

            private List<double> ParseValues(string valueString)
            {
                if (valueString.EndsWith(","))
                {
                    valueString = valueString.TrimEnd(',');
                }
                return valueString.Split(',').Select(double.Parse).ToList();
            }

            private string FormatValues(List<double> values)
            {
                return string.Join(",", values);
            }
        }
    }
}