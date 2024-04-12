using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace RhinoCityJSON
{     
    public class Inject : GH_Component
    {
        public Inject()
          : base("Inject", "Inject",
              "Adds information to existing CityJSON files",
              "RhinoCityJSON", DefaultValues.defaultWritingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Source Path", "sP", "path of source JSON file", GH_ParamAccess.list, "");
            pManager.AddTextParameter("Target Path", "tP", "(new) path of the target JSON file", GH_ParamAccess.list, "");
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Information", "Si", "Information related to the surfaces", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Append", "A", "If true appends instead of replacing the original geometry", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {           
            // activate component
            bool boolOn = false;
            DA.GetData(5, ref boolOn);
            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            // get and check file path(s)
            List<String> sourcePathList = new List<string>();
            List<String> targetPathList = new List<string>();
            if (!DA.GetDataList(0, sourcePathList)) return;
            if (!DA.GetDataList(1, targetPathList)) return;

            if (sourcePathList.Count != targetPathList.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.unevenPathInput]);
                return;
            }

            foreach (var path in sourcePathList) // check if sourcefiles Exist
            {
                if (path.Length == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.emptyPath]);
                    return;
                }

                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidPath]);
                    return;
                }
            }

            foreach (var path in targetPathList) // check if target folders exist
            {
                if (path.Length == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.emptyPath]);
                    return;
                }

                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.outputDirNotReal]);
                    return;
                }
            }

            // get and check geo and its semantics
            var brepList = new List<Brep>();
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();

            DA.GetDataList(2, brepList);
            DA.GetDataList(3, surfaceInfo);

            if (brepList.Count != surfaceInfo.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.outputDirNotReal]);
                return;
            }

            for (int i = 0; i < surfaceInfo.Count; i++)
            {
                var surfaceInfoItem = surfaceInfo[i].Value;
                if (surfaceInfoItem.getName() == "")
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Surface Info object with Indx " + i.ToString() + "is missing an object name");
                    return;
                }

                if (surfaceInfoItem.getGeoType() == "")
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Surface Info object with Indx " + i.ToString() + "is missing an geo type");
                    return;
                }

                if (surfaceInfoItem.getLod() == "")
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Surface Info object with Indx " + i.ToString() + "is missing an LoD");
                    return;
                }

                if (surfaceInfoItem.getSuperName() == "")
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Surface Info object with Indx " + i.ToString() + "is missing a supertype");
                    return;
                }
            }

            // collect and group all the surfaces as a geoobject
            Dictionary<string, CJT.GeoObject> GeometeryNameLookup = new Dictionary<string, CJT.GeoObject>();
            for (int i = 0; i < brepList.Count; i++)
            {
                Brep currentBrep = brepList[i];
                Types.ObjectInfo currentObjectInfo = surfaceInfo[i].Value;

                if (!GeometeryNameLookup.ContainsKey(currentObjectInfo.getSuperName()))
                {
                    CJT.GeoObject geoObject = new CJT.GeoObject();
                    geoObject.setGeoName(currentObjectInfo.getSuperName());
                    geoObject.setGeoType(currentObjectInfo.getGeoType());
                    geoObject.setLod(currentObjectInfo.getLod());
                    GeometeryNameLookup.Add(currentObjectInfo.getSuperName(), geoObject);
                }

                CJT.GeoObject currentObject = GeometeryNameLookup[currentObjectInfo.getSuperName()];

                Dictionary<string, dynamic> translationDictionary = new Dictionary<string, dynamic>();
                foreach (KeyValuePair<string, string> entry in currentObjectInfo.getOtherData())
                {
                    translationDictionary.Add(entry.Key, entry.Value);
                }
                currentObject.addSurfaceData(translationDictionary); //TODO: cast correctly to string

                CJT.SurfaceObject currentSurface = new CJT.SurfaceObject();
                currentSurface.setShape(currentBrep);
                currentObject.addBoundary(currentSurface);
            }

            bool append = true;
            DA.GetData(4, ref append);

            // get the surfaces objectname
            HashSet<string> injectedObjectNameList = new HashSet<string>();
            foreach (var surfaceInfoPiece in surfaceInfo) { injectedObjectNameList.Add(surfaceInfoPiece.Value.getName()); }


            bool objectFound = false;
            for (int i = 0; i < sourcePathList.Count; i++)
            {
                var path = sourcePathList[i];
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));

                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                    return;
                }

                // get the scale item of the file to properly compress the data
                double[] scalerArray = new double[] { 1, 1, 1 };
                if (Jcity.transform != null && Jcity.transform.scale != null)
                {
                    Newtonsoft.Json.Linq.JArray scalerObject = Jcity.transform.scale;
                    scalerArray[0] = ((double)scalerObject[0]);
                    scalerArray[1] = ((double)scalerObject[1]);
                    scalerArray[2] = ((double)scalerObject[2]);
                }

                // get the vert list
                if (Jcity.vertices == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                    return;
                }

                Newtonsoft.Json.Linq.JArray vertsJArray = Jcity["vertices"];

                // get the city objects
                if (Jcity.CityObjects == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                    return;
                }

                Newtonsoft.Json.Linq.JObject jCityObjectList = Jcity["CityObjects"] as Newtonsoft.Json.Linq.JObject;
                HashSet<string> removedObjectHash = new HashSet<string>();

                foreach (var JcityObject in jCityObjectList)
                {
                    foreach (string objectName in injectedObjectNameList)
                    {
                        if (objectName != JcityObject.Key) { continue; }
                        dynamic JCityObjectAttributes = JcityObject.Value;

                        if (!append && !removedObjectHash.Contains(objectName)) //remove geo
                        {
                            Newtonsoft.Json.Linq.JArray oldGeo = JCityObjectAttributes["geometry"];
                            JCityObjectAttributes["geometry"] = new Newtonsoft.Json.Linq.JArray(); // all LoD of an object is removed if no appending
                            removedObjectHash.Add(objectName);
                        }

                        foreach (var geoNamePair in GeometeryNameLookup)
                        {
                            if (!geoNamePair.Key.Contains(objectName)) {  continue; } // check for partial key

                            CJT.GeoObject currentGeoObject = geoNamePair.Value;
                            Newtonsoft.Json.Linq.JObject geoJObject = new Newtonsoft.Json.Linq.JObject();
                            Newtonsoft.Json.Linq.JArray boundariesJArray = new Newtonsoft.Json.Linq.JArray();

                            // generate geometry
                            string geotype = currentGeoObject.getGeoType();

                            if (geotype == "MultiSurface")
                            {
                                foreach (CJT.SurfaceObject geoBoundary in currentGeoObject.getBoundaries())
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
                                                Newtonsoft.Json.Linq.JArray jsonVert = vertsJArray[j].ToObject< Newtonsoft.Json.Linq.JArray>();

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

                                            if (vertIndx == vertsJArray.Count) { vertsJArray.Add(roundedCoordinateJArray);  }
                                            vertJArray.Add(vertIndx);
                                        }
                                        ringJArray.Add(vertJArray);
                                    }
                                    boundariesJArray.Add(ringJArray);
                                }
                            }

                            Newtonsoft.Json.Linq.JArray GeoArray = JCityObjectAttributes["geometry"];
                            geoJObject.Add("boundaries", boundariesJArray);
                            int intLod;
                            bool lodIsInt = int.TryParse(currentGeoObject.getLoD(), out intLod);

                            if (!lodIsInt)
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                                return;
                            }

                            geoJObject.Add("lod", intLod);
                            geoJObject.Add("type", geotype);
                            GeoArray.Add(geoJObject);
                            //TODO: inject the data
                        }
                    }
                }

                if (removedObjectHash.Count > 0) // clean unused verts if object has been removed
                {
                    // TODO: make vert mapping
                    // TODO: remove verts
                    // TODO: remap geo verts
                }



                objectFound = true;

                string json = JsonConvert.SerializeObject(Jcity);

                string filePath = targetPathList[i];

                System.IO.File.WriteAllText(filePath, json);

            }

            if (!objectFound)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noObject]);
                return;
            }


        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.injectGeoicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2365c9a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }
}
