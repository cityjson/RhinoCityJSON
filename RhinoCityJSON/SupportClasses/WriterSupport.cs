using System.Collections.Generic;
using Rhino.Geometry;

namespace RhinoCityJSON
{
    class writerSupport
    {
        static public List<Rhino.Geometry.Point3d> getVerts(List<Brep> brep)
        {
            return new List<Point3d>();
        }

        static public Point3d getAnchorPoint(List<Brep> brepGroup) // lll point is seen as anchor
        {
            Point3d anchor = brepGroup[0].Vertices[0].Location ;
            bool isSet = false;
            foreach (Brep surface in brepGroup)
            {
                var vertList = surface.Vertices;
                foreach (var vert in vertList)
                {
                    Point3d location = vert.Location;
                    if (location.X < anchor.X) { anchor.X = location.X; }
                    if (location.Y < anchor.Y) { anchor.Y = location.Y; }
                    if (location.Z < anchor.Z) { anchor.Z = location.Z; }
                }
            }
            return anchor;
        }
    }
}
