using NetTopologySuite.Geometries;

namespace TimeIsLife.CADCommand
{
    partial class FireAlarmCommand1
    {
        public class Wall
        {
            public int ID { get; set; }
            public Floor Floor { get; set; }
            public WallSect WallSect { get; set; }
            public Grid Grid { get; set; }

            public LineString ToLineString()
            {
                Coordinate[] coordinates = new Coordinate[] { new Coordinate(this.Grid.Joint1.X, this.Grid.Joint1.Y), new Coordinate(this.Grid.Joint2.X, this.Grid.Joint2.Y) };
                return new LineString(coordinates);
            }
        }
    }
}