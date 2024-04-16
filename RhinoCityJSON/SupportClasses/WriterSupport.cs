using System;
using Rhino.Geometry;

namespace RhinoCityJSON
{
    class writerSupport
    {
        static public Newtonsoft.Json.Linq.JArray rhinoSurfacesToJarray(CJT.GeoObject geoObject, Newtonsoft.Json.Linq.JArray vertsJArray, double[] scalerArray)
        {
            Newtonsoft.Json.Linq.JArray outputArray = new Newtonsoft.Json.Linq.JArray();

            foreach (CJT.SurfaceObject geoBoundary in geoObject.getBoundaries())
            {
                Brep brepBoundary = geoBoundary.getShape();
                Newtonsoft.Json.Linq.JArray ringJArray = new Newtonsoft.Json.Linq.JArray();
                foreach (BrepLoop surfaceLoop in brepBoundary.Loops)
                {
                    Newtonsoft.Json.Linq.JArray vertJArray = new Newtonsoft.Json.Linq.JArray();
                    foreach (BrepTrim surfaceTrim in surfaceLoop.Trims)
                    {
                        Point3d surfaceVertex = surfaceTrim.StartVertex.Location;

                        int[] roundedCoordinate = new int[] {
                                                (int)Math.Floor(surfaceVertex.X/scalerArray[0]),
                                                (int)Math.Floor(surfaceVertex.Y/scalerArray[1]),
                                                (int)Math.Floor(surfaceVertex.Z/scalerArray[2])
                                            };

                        int vertIndx = vertsJArray.Count;

                        for (int j = 0; j < vertsJArray.Count; j++)
                        {
                            Newtonsoft.Json.Linq.JArray jsonVert = vertsJArray[j].ToObject<Newtonsoft.Json.Linq.JArray>();

                            if (
                                (int)jsonVert[0] != roundedCoordinate[0] ||
                                (int)jsonVert[1] != roundedCoordinate[1] ||
                                (int)jsonVert[2] != roundedCoordinate[2]
                                )
                            {
                                continue;
                            }
                            vertIndx = j;
                            break;
                        }

                        Newtonsoft.Json.Linq.JArray roundedCoordinateJArray = new Newtonsoft.Json.Linq.JArray();

                        foreach (var coordinateValue in roundedCoordinate)
                        {
                            roundedCoordinateJArray.Add(coordinateValue);
                        }

                        if (vertIndx == vertsJArray.Count) { vertsJArray.Add(roundedCoordinateJArray); }
                        vertJArray.Add(vertIndx);
                    }
                    ringJArray.Add(vertJArray);
                }
                outputArray.Add(ringJArray);
            }
            return outputArray;
        }
    }
}
