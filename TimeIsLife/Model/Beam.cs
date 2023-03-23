using NetTopologySuite.Geometries;

using System;

namespace TimeIsLife.CADCommand
{
    partial class FireAlarmCommand1
    {
        #region 结构类

        public class Beam
        {
            public int ID { get; set; }
            public Floor Floor { get; set; }
            public BeamSect BeamSect { get; set; }
            public Grid Grid { get; set; }

            public bool IsConcrete
            {
                get
                {
                    if (BeamSect.Kind == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public double Height
            {
                get
                {
                    var height = BeamSect.Kind switch
                    {
                        1 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        2 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        7 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        13 => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                        22 => Math.Min(double.Parse(this.BeamSect.ShapeVal.Split(',')[3]),
                                                        double.Parse(this.BeamSect.ShapeVal.Split(',')[4])),
                        26 => double.Parse(this.BeamSect.ShapeVal.Split(',')[3]),
                        _ => double.Parse(this.BeamSect.ShapeVal.Split(',')[2]),
                    };
                    return height;
                }
            }
            public LineString ToLineString()
            {
                Coordinate[] coordinates = new Coordinate[] { new Coordinate(this.Grid.Joint1.X, this.Grid.Joint1.Y), new Coordinate(this.Grid.Joint2.X, this.Grid.Joint2.Y) };
                return new LineString(coordinates);
            }

            public double Width
            {
                get
                {
                    var width = BeamSect.Kind switch
                    {
                        1 => double.Parse(this.BeamSect.ShapeVal.Split(',')[1]),
                        _ => 0.0,
                    };
                    return width;
                }
            }
        }
        #endregion

    }
}