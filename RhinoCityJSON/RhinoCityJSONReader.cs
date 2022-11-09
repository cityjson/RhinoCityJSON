using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace RhinoCityJSON
{ 
    class CJTempate
    {
        private int idx_ = 0;
        private string lod_ = "none";
        private bool hasError_ = false;

        private List<Brep> brepList_ = new List<Rhino.Geometry.Brep>();

        public CJTempate(int idx)
        {
            idx_ = idx;
        }

        public string getLod() { return lod_; }
        public void setLod(string lod) { lod_ = lod; }
        public bool getError() { return hasError_; }
        public void setError(bool hasError) { hasError_ = hasError; }
        public List<Rhino.Geometry.Brep> getBrepList() { return brepList_; }
        public void setBrepList(List<Rhino.Geometry.Brep> brepList) { brepList_ = brepList; }
        public int getBrepCount() { return brepList_.Count; }
    }


    class CJObject
    {
        private string name_ = "None";
        private string lod_ = "None";
        private string parentName_ = "None";
        private string geometryType_ = "None";
        private double scaler_ = 1;
        private bool hasError_ = false;

        private List<string> internalizedSurfTypes_ = new List<string>();
        private List<Dictionary<string, string>> surfaceSemantics_ = new List<Dictionary<string, string>>();
        private List<Dictionary<string, string>> cleanedSurfaceSemantics_ = new List<Dictionary<string, string>>();
        private List<Rhino.Geometry.Brep> brepList_ = new List<Rhino.Geometry.Brep>();

        public CJObject(string name){ name_ = name; }
        public string getName() { return name_; }
        public void setName(string name) { name_ = name; }
        public string getLod() { return lod_; }
        public void setLod(string lod) { lod_ = lod; }
        public string getParendName() { return parentName_; }
        public void setParendName(string parentName) { parentName_ = parentName; }
        public string getGeometryType() { return geometryType_; }
        public void setGeometryType(string geometryType) { geometryType_ = geometryType; }
        public double getScalar() { return scaler_; }
        public void setScaler(double scaler) { scaler_ = scaler; }
        public bool getError() { return hasError_; }
        public void setError(bool hasError) { hasError_ = hasError; }
        public List<string> getPresentTypes() { return internalizedSurfTypes_; }
        public List<Dictionary<string, string>> getSurfaceSemantics() { return surfaceSemantics_; }
        public List<Dictionary<string, string>> getCSurfaceSemantics() { return cleanedSurfaceSemantics_; }
        public void addCsurfaceSemantic(List<Dictionary<string, string>> surfaceSemantics)
        {
            foreach (Dictionary<string, string> surfaceSemantic in surfaceSemantics)
            {
                cleanedSurfaceSemantics_.Add(surfaceSemantic);
            }
        }

        public List<Rhino.Geometry.Brep> getBrepList() { return brepList_; }
        public void setBrepList(List<Rhino.Geometry.Brep> brepList) { brepList_ = brepList; }
        public int getBrepCount() { return brepList_.Count; }

        public void matchSemantics(dynamic semanticData, int ind = 0)
        {
            List<string> typeList = new List<string>();
            var surfaceSemantics = new List<Dictionary<string, string>>();

            foreach (Newtonsoft.Json.Linq.JObject type in semanticData.surfaces)
            {
                var localDict = new Dictionary<string, string>();
                foreach (var t in type)
                {
                    localDict.Add(t.Key, t.Value.ToString());
                    if (!internalizedSurfTypes_.Contains(t.Key))
                    {
                        internalizedSurfTypes_.Add(t.Key);
                    }                  
                }
                surfaceSemantics.Add(localDict);
            }

            if (ind == 0)
            {
                foreach (int typeIdx in semanticData.values)
                {
                    surfaceSemantics_.Add(surfaceSemantics[typeIdx]);        
                }
            }
            if (ind == 1)
            {
                foreach (var solididx in semanticData.values)
                {
                    foreach (int typeIdx in solididx)
                    {
                        surfaceSemantics_.Add(surfaceSemantics[typeIdx]);
                    }
                }
            }
        }

        public void joinSimple()
        {
            var joinedBrep = Brep.JoinBreps(brepList_, 0.2);
            brepList_.Clear();

            foreach (Brep brep in joinedBrep)
            {
                brepList_.Add(brep);
            }
        }

        public void joinSmart(IGH_DataAccess DA)
        {

        }
    }

    static class ErrorCollection // TODO put all the errors centrally 
    {
        static public int surfaceCreationErrorCode = 00001;
        static public int emptyPathErrorCode = 00002;

        static public Dictionary<int, string> errorCollection = new Dictionary<int, string>()
        {
            {surfaceCreationErrorCode, "Not all surfaces have been correctly created"},
            {emptyPathErrorCode, "Path is empty"}
        };
    }

    class ReaderSupport
    {
        static public List<int> getSematicValues(dynamic boundaryGroup)
        {
            List<int> semanticValues = new List<int>();
            foreach (int sVaule in boundaryGroup.semantics.values)
            {
                semanticValues.Add(sVaule);
            }
            if (semanticValues.Count == 0)
            {
                foreach (var boundary in boundaryGroup.boundaries)
                {
                    semanticValues.Add(0);
                }
            }
            return semanticValues;
        }

        static public bool CheckValidity(dynamic file)
        {
            if (file.CityObjects == null || file.type != "CityJSON" || file.version == null ||
                file.transform == null || file.transform.scale == null || file.transform.translate == null ||
                file.vertices == null)
            {
                return false;
            }
            else if (file.version != "1.1" && file.version != "1.0")
            {
                return false;
            }
            return true;
        }

        static public Tuple<List<Rhino.Geometry.Brep>, bool> getBrepSurface(dynamic surface, List<Rhino.Geometry.Point3d> vertList, double scalar = 1)
        {
            bool isTriangle = false;

            List<Rhino.Geometry.Brep> brepList = new List<Rhino.Geometry.Brep>();
            bool hasError = false;

            // this is one complete surface (surface + holes)
            Rhino.Collections.CurveList surfaceCurves = new Rhino.Collections.CurveList();

            // check if is triangle
            if (surface.Count == 1)
            {
                if (surface[0].Count < 5 && surface[0].Count > 2)
                {
                    isTriangle = true;
                }
            }

            if (isTriangle)
            {
                List<int> currentSurf = surface[0].ToObject<List<int>>();
                NurbsSurface nSurface;

                if (currentSurf.Count == 3)
                {
                    nSurface = NurbsSurface.CreateFromCorners(
                        vertList[currentSurf[0]],
                        vertList[currentSurf[1]],
                        vertList[currentSurf[2]]
                        );
                }
                else
                {
                    nSurface = NurbsSurface.CreateFromCorners(
                        vertList[currentSurf[0]],
                        vertList[currentSurf[1]],
                        vertList[currentSurf[2]],
                        vertList[currentSurf[3]]
                        );
                }

                if (nSurface != null)
                {
                    brepList.Add(nSurface.ToBrep());
                    return Tuple.Create(brepList, false);
                }
            }


            for (int i = 0; i < surface.Count; i++)
            {
                // one ring 
                List<Rhino.Geometry.Point3d> curvePoints = new List<Rhino.Geometry.Point3d>();
                foreach (int vertIdx in surface[i])
                {
                    curvePoints.Add(vertList[vertIdx]);
                }
                if (curvePoints.Count > 0)
                {
                    curvePoints.Add(curvePoints[0]);

                    try
                    {
                        Rhino.Geometry.Polyline ring = new Rhino.Geometry.Polyline(curvePoints);
                        surfaceCurves.Add(ring);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            if (surfaceCurves.Count > 0)
            {
                Rhino.Geometry.Brep[] planarFace = Brep.CreatePlanarBreps(surfaceCurves, 0.1 * scalar); //TODO scale
                surfaceCurves.Clear();
                try
                {
                    brepList.Add(planarFace[0]);
                }
                catch (Exception)
                {
                    hasError = true;
                }
            }
            return Tuple.Create(brepList, hasError);
        }


        static public CJObject getBrepShape(dynamic solid, List<Rhino.Geometry.Point3d> vertList, CJObject cjobject = null, bool advanced = false)
        {
            CJObject localCJObject = new CJObject("");
            double scaler = cjobject.getScalar();

            if (cjobject != null)
            {
                localCJObject = cjobject;
            }

            int idx = localCJObject.getBrepCount();
            var surfaceSemantics = localCJObject.getSurfaceSemantics();
            List<string> filteredSurfaceNames = new List<string>();
            var filteredSurfaceSemantics = new List<Dictionary<string, string>>();

            List<Rhino.Geometry.Brep> localBreps = new List<Brep>();
            bool hasError = false;
            int count = 0;

            foreach (var surface in solid)
            {
                var readersurf = ReaderSupport.getBrepSurface(surface, vertList, scaler);
                if (readersurf.Item2)
                {
                    hasError = true;
                }
                foreach (var brep in readersurf.Item1)
                {
                    localBreps.Add(brep);

                    if (!advanced)
                    {
                        continue;
                    }

                    if (surfaceSemantics.Count > 0)
                    {
                        filteredSurfaceSemantics.Add(surfaceSemantics[count]);
                    }
                }

                count++;
            }

            if (advanced)
            {
                localCJObject.addCsurfaceSemantic(filteredSurfaceSemantics);
            }


            List<Brep> brepList = localCJObject.getBrepList();

            foreach (var brep in localBreps)
            {
                brepList.Add(brep);
            }

            localCJObject.setBrepList(brepList);
            localCJObject.setError(hasError);

            return localCJObject;
        }


        static public List<CJTempate> getTemplateGeo(dynamic Jcity, bool setLoD, List<string> loDList, double scaler = 1)
        {
            List<Rhino.Geometry.Point3d> vertListTemplate = new List<Rhino.Geometry.Point3d>();
            var templateGeoList = new List<CJTempate>();

            bool hasError = false;

            if (Jcity["geometry-templates"] != null)
            {

                foreach (var jsonvert in Jcity["geometry-templates"]["vertices-templates"])
                {
                    double x = jsonvert[0] * scaler;
                    double y = jsonvert[1] * scaler;
                    double z = jsonvert[2] * scaler;

                    Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(x, y, z);
                    vertListTemplate.Add(vert);
                }
                foreach (var template in Jcity["geometry-templates"]["templates"])
                {
                    var templateGeo = new List<Brep>();
                    if (setLoD && !loDList.Contains((string)template.lod))
                    {
                        continue;
                    }

                    // this is all the geometry in one shape with info
                    else if (template.type == "Solid")
                    {
                        foreach (var solid in template.boundaries)
                        {
                            CJObject readershape = new CJObject("");
                            readershape.setScaler(scaler);
                            readershape = ReaderSupport.getBrepShape(solid, vertListTemplate, readershape);

                            if (readershape.getError())
                            {
                                hasError = true;
                            }

                            readershape.joinSimple();

                            foreach (var brep in readershape.getBrepList())
                            {
                                templateGeo.Add(brep);
                            }
                        }
                    }
                    else if (template.type == "CompositeSolid" || template.type == "MultiSolid")
                    {
                        foreach (var composit in template.boundaries)
                        {
                            foreach (var solid in composit)
                            {
                                CJObject readershape = new CJObject("");
                                readershape.setScaler(scaler);
                                readershape = ReaderSupport.getBrepShape(solid, vertListTemplate, readershape);

                                if (readershape.getError())
                                {
                                    hasError = true;
                                }

                                readershape.joinSimple();

                                foreach (var brep in readershape.getBrepList())
                                {
                                    templateGeo.Add(brep);
                                }
                            }
                        }
                    }
                    else
                    {
                        CJObject readershape = new CJObject("");
                        readershape.setScaler(scaler);
                        readershape = ReaderSupport.getBrepShape(template.boundaries, vertListTemplate, readershape);

                        if (readershape.getError())
                        {
                            hasError = true;
                        }

                        readershape.joinSimple();

                        foreach (var brep in readershape.getBrepList())
                        {
                            templateGeo.Add(brep);
                        }
                    }

                    CJTempate newTemplate = new CJTempate(templateGeoList.Count);
                    newTemplate.setBrepList(templateGeo);
                    newTemplate.setLod((string)template.lod);
                    newTemplate.setError(hasError);

                    templateGeoList.Add(newTemplate);
                }
                return templateGeoList;
            }

            var emptyList = new List<CJTempate>();

            return emptyList;
        }


        static public void populateKeyList(ref List<string> keyList, Dictionary<string, dynamic> sourceDict)
        {
            foreach (KeyValuePair<string, dynamic> buildingInfoPair in sourceDict)
            {
                foreach (dynamic attributePair in buildingInfoPair.Value)
                {
                    string key = attributePair.Name;
                    if (!keyList.Contains(key))
                    {
                        keyList.Add(key);
                    }
                }
            }
        }

        static public string stripString(string name)
        {
            string tName = "";
            foreach (char c in name)
            {
                if (c != '{' && c != '}' && c != '?' && c != '@' && c != '/' && c != '\\' )
                {
                    tName += c;
                }
            }
            return tName;
        }
    }


    class BakerySupport
    {
        static public string getParentName(string Childname)
        {
            if (
                Childname == "BridgePart" ||
                Childname == "BridgeInstallation" ||
                Childname == "BridgeConstructiveElement" ||
                Childname == "BrideRoom" ||
                Childname == "BridgeFurniture"
                )
            {
                return "Bridge";
            }
            else if (
                Childname == "BuildingPart" ||
                Childname == "BuildingInstallation" ||
                Childname == "BuildingConstructiveElement" ||
                Childname == "BuildingFurniture" ||
                Childname == "BuildingStorey" ||
                Childname == "BuildingRoom" ||
                Childname == "BuildingUnit"
                )
            {
                return "Building";
            }
            else if (
                Childname == "TunnelPart" ||
                Childname == "TunnelInstallation" ||
                Childname == "TunnelConstructiveElement" ||
                Childname == "TunnelHollowSpace" ||
                Childname == "TunnelFurniture"
                )
            {
                return "Tunnel";
            }
            else
            {
                return Childname;
            }
        }

        static public Dictionary<string, System.Drawing.Color> getTypeColor()
        {
            return new Dictionary<string, System.Drawing.Color>
            {
                { "Bridge", System.Drawing.Color.Gray },
                { "Building", System.Drawing.Color.LightBlue },
                { "CityFurniture", System.Drawing.Color.Red },
                { "LandUse", System.Drawing.Color.FloralWhite },
                { "OtherConstruction", System.Drawing.Color.White },
                { "PlantCover", System.Drawing.Color.Green },
                { "SolitaryVegetationObject", System.Drawing.Color.Green },
                { "TINRelief", System.Drawing.Color.LightYellow},
                { "TransportationSquare", System.Drawing.Color.Gray},
                { "Road", System.Drawing.Color.Gray},
                { "Tunnel", System.Drawing.Color.Gray},
                { "WaterBody", System.Drawing.Color.MediumBlue},
                { "+GenericCityObject", System.Drawing.Color.White},
                { "Railway", System.Drawing.Color.DarkGray},
            };
        }

        static public Dictionary<string, System.Drawing.Color> getSurfColor()
        {
            return new Dictionary<string, System.Drawing.Color>{
                { "GroundSurface", System.Drawing.Color.Gray },
                { "WallSurface", System.Drawing.Color.LightBlue },
                { "RoofSurface", System.Drawing.Color.Red }
            };
        }

        static public int getPopLength(string lod)
        {
            if (lod == "0" || lod == "1" || lod == "2" || lod == "3")
            {
                return 6;
            }
            else if (lod == "0.0" || lod == "0.1" || lod == "0.2" || lod == "0.3" ||
                     lod == "1.0" || lod == "1.1" || lod == "1.2" || lod == "1.3" ||
                     lod == "2.0" || lod == "2.1" || lod == "2.2" || lod == "2.3" ||
                     lod == "3.0" || lod == "3.1" || lod == "3.2" || lod == "3.3")
            {
                return 8;
            }
            else
            {
                return 0;
            }
        }
    }


    public class LoDReader : GH_Component
    {
        public LoDReader()
          : base("LoDReader", "LReader",
              "Fetches the Lod levels stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("LoD", "L", "LoD levels", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> pathList = new List<string>();
            if (!DA.GetDataList(0, pathList)) return;

            // validate the data and warn the user if invalid data is supplied.
            if (pathList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[ErrorCollection.surfaceCreationErrorCode]);
                return;
            }
            else if (pathList[0] == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[ErrorCollection.surfaceCreationErrorCode]);
                return;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid filepath found");
                    return;
                }
            }

            List<string> lodLevels = new List<string>();

            foreach (var path in pathList)
            {
                // Check if valid CityJSON format
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }
                foreach (var objectGroup in Jcity.CityObjects)
                {
                    foreach (var cObject in objectGroup)
                    {
                        if (cObject.geometry == null) // parents
                        {
                            continue;
                        }

                        foreach (var boundaryGroup in cObject.geometry)
                        {
                            string currentLoD = boundaryGroup.lod;

                            if (!lodLevels.Contains(currentLoD))
                            {
                                lodLevels.Add(currentLoD);
                            }

                        }
                    }
                }
            }
            lodLevels.Sort();
            DA.SetDataList(0, lodLevels);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.lodicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e16"); }
        }
    }


    public class ReaderSettings : GH_Component
    {
        public ReaderSettings()
          : base("ReaderSettings", "RSettings",
              "Sets the additional configuration for the SReader and reader",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Translate", "T", "Translate according to the stored translation vector", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Model origin", "O", "The Origin of the model. This coordiante will be set as the {0,0,0} point for the imported JSON", GH_ParamAccess.list);
            pManager.AddNumberParameter("True north", "Tn", "The direction of the true north", GH_ParamAccess.list, 0.0);
            pManager.AddTextParameter("LoD", "L", "Desired Lod, keep empty for all", GH_ParamAccess.list, "");

            pManager[1].Optional = true; // origin is optional
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Settings", "S", "Set settings", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool translate = false;
            var p = new Rhino.Geometry.Point3d(0, 0, 0);
            bool setP = false;
            var pList = new List<Rhino.Geometry.Point3d>();
            var north = 0.0;
            var northList = new List<double>();
            var loDList = new List<string>();

            DA.GetData(0, ref translate);
            DA.GetDataList(1, pList);
            DA.GetDataList(2, northList);
            DA.GetDataList(3, loDList);

            if (pList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Multiple true origin points submitted");
                return;
            }
            else if (pList != null && pList.Count == 1)
            {
                setP = true;
                p = pList[0];
            }

            if (northList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Multiple true north angles submitted");
                return;
            }
            else if (northList[0] != 0 && !setP)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "True north rotation only functions if origin is given");
                return;
            }
            else
            {
                north = northList[0];
            }

            if (north < -360 || north > 360)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "True north rotation is larger than 360 degrees");
            }

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    if (lod == "0" || lod == "0.0" || lod == "0.1" || lod == "0.2" || lod == "0.3" ||
                        lod == "1" || lod == "1.0" || lod == "1.1" || lod == "1.2" || lod == "1.3" ||
                        lod == "2" || lod == "2.0" || lod == "2.1" || lod == "2.2" || lod == "2.3" ||
                        lod == "3" || lod == "3.0" || lod == "3.1" || lod == "3.2" || lod == "3.3")
                    {
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid lod input found");
                        return;
                    }
                }

            }

            var settingsTuple = Tuple.Create(translate, p, setP, north, loDList);
            DA.SetData(0, new Grasshopper.Kernel.Types.GH_ObjectWrapper(settingsTuple));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.settingsicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a275e15"); }
        }

    }



    public class SimpleRhinoCityJSONReader : GH_Component
    {
        public SimpleRhinoCityJSONReader()
          : base("SimpleRCJReader", "SReader",
              "Reads the Geometry related data stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Settings", "S", "Settings coming from the RSettings component", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGroupParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> pathList = new List<string>();

            var settingsList = new List<Grasshopper.Kernel.Types.GH_ObjectWrapper>();
            var readSettingsList = new List<Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>>();

            bool boolOn = false;


            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);
            DA.GetDataList(2, settingsList);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }
            else if (settingsList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only a single settings input allowed");
                return;
            }
            // validate the data and warn the user if invalid data is supplied.
            else if (pathList[0] == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is empty");
                return;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid filepath found");
                    return;
                }
            }

            // get the settings
            List<string> loDList = new List<string>();

            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;

            double rotationAngle = 0;

            if (settingsList.Count > 0)
            {
                // extract settings
                foreach (Grasshopper.Kernel.Types.GH_ObjectWrapper objWrap in settingsList)
                {
                    readSettingsList.Add(objWrap.Value as Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>);
                }

                Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>> settings = readSettingsList[0];
                translate = settings.Item1;
                rotationAngle = Math.PI * settings.Item4 / 180.0;

                if (settings.Item3) // if world origin is set
                {
                    worldOrigin = settings.Item2;
                }
                loDList = settings.Item5;
            }
            // check lod validity
            bool setLoD = false;

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    if (lod == "0" || lod == "0.0" || lod == "0.1" || lod == "0.2" || lod == "0.3" ||
                        lod == "1" || lod == "1.0" || lod == "1.1" || lod == "1.2" || lod == "1.3" ||
                        lod == "2" || lod == "2.0" || lod == "2.1" || lod == "2.2" || lod == "2.3" ||
                        lod == "3" || lod == "3.0" || lod == "3.1" || lod == "3.2" || lod == "3.3")
                    {
                        setLoD = true;
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid lod input found");
                        return;
                    }
                }

            }

            Dictionary<string, List<Brep>> lodNestedBreps = new Dictionary<string, List<Brep>>();

            // coordinates of the first input
            double globalX = 0.0;
            double globalY = 0.0;
            double globalZ = 0.0;

            bool isFirst = true;

            double originX = worldOrigin.X;
            double originY = worldOrigin.Y;
            double originZ = worldOrigin.Z;

            foreach (var path in pathList)
            {
                // Check if valid CityJSON format
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }

                // get scalers
                double scaleX = Jcity.transform.scale[0];
                double scaleY = Jcity.transform.scale[1];
                double scaleZ = Jcity.transform.scale[2];

                // translation vectors
                double localX = 0.0;
                double localY = 0.0;
                double localZ = 0.0;

                // get location
                if (translate)
                {
                    localX = Jcity.transform.translate[0];
                    localY = Jcity.transform.translate[1];
                    localZ = Jcity.transform.translate[2];
                }
                else if (isFirst && !translate)
                {
                    isFirst = false;
                    globalX = Jcity.transform.translate[0];
                    globalY = Jcity.transform.translate[1];
                    globalZ = Jcity.transform.translate[2];
                }
                else if (!isFirst && !translate)
                {
                    localX = Jcity.transform.translate[0] - globalX;
                    localY = Jcity.transform.translate[1] - globalY;
                    localZ = Jcity.transform.translate[2] - globalZ;
                }

                // ceate vertlist
                var jsonverts = Jcity.vertices;
                List<Rhino.Geometry.Point3d> vertList = new List<Rhino.Geometry.Point3d>();
                foreach (var jsonvert in jsonverts)
                {
                    double x = jsonvert[0];
                    double y = jsonvert[1];
                    double z = jsonvert[2];

                    double tX = x * scaleX + localX - originX;
                    double tY = y * scaleY + localY - originY;
                    double tZ = z * scaleZ + localZ - originZ;

                    Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(
                        tX * Math.Cos(rotationAngle) - tY * Math.Sin(rotationAngle),
                        tY * Math.Cos(rotationAngle) + tX * Math.Sin(rotationAngle),
                        tZ
                        );
                    vertList.Add(vert);
                }

                // create template vertlist and templates
                List<CJTempate> templateGeoList = ReaderSupport.getTemplateGeo(Jcity, setLoD, loDList);

                foreach (CJTempate template in templateGeoList)
                {
                    if (template.getError())
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                        break;
                    }
                }

                // create surfaces
                foreach (var objectGroup in Jcity.CityObjects)
                {
                    foreach (var cObject in objectGroup)
                    {
                        if (cObject.geometry == null) // parents
                        {
                            continue;
                        }

                        foreach (var boundaryGroup in cObject.geometry)
                        {
                            string loD = (string)boundaryGroup.lod;
                            CJObject lodBuilding = new CJObject(objectGroup.Name + "-" + loD);

                            if (setLoD && !loDList.Contains((string)boundaryGroup.lod))
                            {
                                continue;
                            }

                            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();

                            if (boundaryGroup.template != null)
                            {

                                CJTempate shapeTemplate = templateGeoList[(int)boundaryGroup.template];
                                loD = shapeTemplate.getLod();

                                List<Brep> shapeList = shapeTemplate.getBrepList();
                                var anchorPoint = vertList[(int)boundaryGroup.boundaries[0]];

                                foreach (Brep shape in shapeList)
                                {
                                    double x = anchorPoint[0];
                                    double y = anchorPoint[1];
                                    double z = anchorPoint[2];

                                    Brep transShape = shape.DuplicateBrep();

                                    transShape.Translate(x, y, z);

                                    breps.Add(transShape);
                                }
                            }

                            // this is all the geometry in one shape with info
                            else if (boundaryGroup.type == "Solid")
                            {
                                foreach (var solid in boundaryGroup.boundaries)
                                {
                                    lodBuilding = ReaderSupport.getBrepShape(solid, vertList, lodBuilding);

                                    if (lodBuilding.getError())
                                    {
                                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                    }
                                }
                            }
                            else if (boundaryGroup.type == "CompositeSolid" || boundaryGroup.type == "MultiSolid")
                            {
                                foreach (var composit in boundaryGroup.boundaries)
                                {
                                    foreach (var solid in composit)
                                    {
                                        lodBuilding = ReaderSupport.getBrepShape(solid, vertList, lodBuilding);

                                        if (lodBuilding.getError())
                                        {
                                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                lodBuilding = ReaderSupport.getBrepShape(boundaryGroup.boundaries, vertList, lodBuilding);

                                if (lodBuilding.getError())
                                {
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                }
                            }

                            lodBuilding.joinSimple();

                            foreach (var brep in lodBuilding.getBrepList())
                            {
                                breps.Add(brep);
                            }

                            try
                            {
                                lodNestedBreps.Add(loD, breps);
                            }
                            catch (ArgumentException)
                            {
                                foreach (var brep in breps)
                                {
                                    lodNestedBreps[loD].Add(brep);
                                }
                            }
                        }
                    }
                }
            }

            var outputList = new List<Grasshopper.Kernel.Types.GH_GeometryGroup>();

            foreach (KeyValuePair<string, List<Brep>> entry in lodNestedBreps)
            {
                Grasshopper.Kernel.Types.GH_GeometryGroup loDGroup = new Grasshopper.Kernel.Types.GH_GeometryGroup();

                foreach (var brep in entry.Value)
                {
                    loDGroup.Objects.Add(GH_Convert.ToGeometricGoo(brep));
                }
                outputList.Add(loDGroup);
            }

            if (outputList.Count > 0)
            {
                DA.SetDataList(0, outputList);
            }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.sreadericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a2754e8"); }
        }
    }

    public class RhinoCityJSONReader : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RhinoCityJSONReader()
          : base("RCJReader", "Reader",
              "Reads the complete data stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Settings", "S", "Settings coming from the RSettings component", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Vales", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> pathList = new List<string>();
            var settingsList = new List<Grasshopper.Kernel.Types.GH_ObjectWrapper>();
            var readSettingsList = new List<Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>>();
            bool boolOn = false;

            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);
            DA.GetDataList(2, settingsList);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }
            else if (settingsList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only a single settings input allowed");
                return;
            }
            // validate the data and warn the user if invalid data is supplied.
            else if (pathList[0] == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is empty");
                return;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid filepath found");
                    return;
                }
            }

            // get the settings
            List<string> loDList = new List<string>();
            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;
            double rotationAngle = 0;

            if (settingsList.Count > 0)
            {
                // extract settings
                foreach (Grasshopper.Kernel.Types.GH_ObjectWrapper objWrap in settingsList)
                {
                    readSettingsList.Add(objWrap.Value as Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>);
                }

                Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>> settings = readSettingsList[0];
                translate = settings.Item1;
                rotationAngle = Math.PI * settings.Item4 / 180.0;

                if (settings.Item3) // if world origin is set
                {
                    worldOrigin = settings.Item2;
                }
                loDList = settings.Item5;
            }
            // check lod validity
            bool setLoD = false;

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    if (lod == "0" || lod == "0.0" || lod == "0.1" || lod == "0.2" || lod == "0.3" ||
                        lod == "1" || lod == "1.0" || lod == "1.1" || lod == "1.2" || lod == "1.3" ||
                        lod == "2" || lod == "2.0" || lod == "2.1" || lod == "2.2" || lod == "2.3" ||
                        lod == "3" || lod == "3.0" || lod == "3.1" || lod == "3.2" || lod == "3.3")
                    {
                        setLoD = true;
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid lod input found");
                        return;
                    }
                }

            }

            // set DataCollection
            var surfaceSemanticTypes = new List<string>();
            var geoSurfaceInfo = new Dictionary<string, List<Brep>>();
            var simpleNameDict = new Dictionary<string, string>();
            var semanticSurfaceInfo = new Dictionary<string, List<Dictionary<string, string>>>();
            var semanticBuildingInfo = new Dictionary<string, dynamic>();
            var semanticParentInfo = new Dictionary<string, dynamic>();

            // set data output list
            var dataTree = new Grasshopper.DataTree<string>();


            // get scale from current session
            double scaler = 1;
            string UnitString = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();

            if (UnitString == "Meters")
            {
                scaler = 1;
            }
            else if (UnitString == "Centimeters")
            {
                scaler = 100;
            }
            else if (UnitString == "Millimeters")
            {
                scaler = 1000;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Rhino document scale is not supported, defaulted to unit 1");
            }


            // coordinates of the first input
            double globalX = 0.0;
            double globalY = 0.0;
            double globalZ = 0.0;

            bool isFirst = true;

            double originX = worldOrigin.X * scaler;
            double originY = worldOrigin.Y * scaler;
            double originZ = worldOrigin.Z * scaler;

            foreach (var path in pathList)
            {
                // Check if valid CityJSON format
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }

                // get scalers
                double scaleX = Jcity.transform.scale[0] * scaler;
                double scaleY = Jcity.transform.scale[1] * scaler;
                double scaleZ = Jcity.transform.scale[2] * scaler;

                // translation vectors
                double localX = 0.0;
                double localY = 0.0;
                double localZ = 0.0;

                // get location
                if (translate)
                {
                    localX = Jcity.transform.translate[0];
                    localY = Jcity.transform.translate[1];
                    localZ = Jcity.transform.translate[2];
                }
                else if (isFirst && !translate)
                {
                    isFirst = false;
                    globalX = Jcity.transform.translate[0];
                    globalY = Jcity.transform.translate[1];
                    globalZ = Jcity.transform.translate[2];
                }
                else if (!isFirst && !translate)
                {
                    localX = Jcity.transform.translate[0] - globalX;
                    localY = Jcity.transform.translate[1] - globalY;
                    localZ = Jcity.transform.translate[2] - globalZ;
                }

                // ceate vertlist
                var jsonverts = Jcity.vertices;
                List<Rhino.Geometry.Point3d> vertList = new List<Rhino.Geometry.Point3d>();
                foreach (var jsonvert in jsonverts)
                {
                    double x = jsonvert[0];
                    double y = jsonvert[1];
                    double z = jsonvert[2];

                    double tX = x * scaleX + localX - originX;
                    double tY = y * scaleY + localY - originY;
                    double tZ = z * scaleZ + localZ - originZ;

                    Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(
                        tX * Math.Cos(rotationAngle) - tY * Math.Sin(rotationAngle),
                        tY * Math.Cos(rotationAngle) + tX * Math.Sin(rotationAngle),
                        tZ
                        );
                    vertList.Add(vert);
                }

                // create template vertlist and templates
                List<CJTempate> templateGeoList = ReaderSupport.getTemplateGeo(Jcity, setLoD, loDList, scaler);

                foreach (CJTempate template in templateGeoList)
                {
                    if (template.getError())
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                        break;
                    }
                }

                // create surfaces
                foreach (var objectGroup in Jcity.CityObjects)
                {
                    var oName = ReaderSupport.stripString(objectGroup.Name);
                    foreach (var cObject in objectGroup)
                    {
                        string buildingType = cObject.type;
                        var parent = cObject.parents;
                        var attributes = cObject.attributes;
                        var children = cObject.children;

                        if (children != null) // parents
                        {
                            if (attributes != null)
                            {
                                foreach (string child in children)
                                {
                                    foreach (var boundaryGroup in cObject.geometry)
                                    {
                                        semanticParentInfo.Add(child, attributes);
                                    }                                    
                                }
                            }
                        }

                        if (cObject.geometry == null)
                        {
                            continue;
                        }

                        foreach (var boundaryGroup in cObject.geometry)
                        {
                            CJObject lodBuilding = new CJObject(oName);
                            string groupLoD = boundaryGroup.lod;

                            lodBuilding.setLod(groupLoD);
                            lodBuilding.setGeometryType(buildingType);
                            lodBuilding.setScaler(scaler);

                            if (parent != null)
                            {
                                foreach (string item in parent)
                                {
                                    lodBuilding.setParendName(item);
                                    break;
                                }
                            }

                            if (setLoD && !loDList.Contains(groupLoD))
                            {
                                continue;
                            }

                            if (boundaryGroup.template != null)
                            {

                                CJTempate shapeTemplate = templateGeoList[(int)boundaryGroup.template];
                                groupLoD = shapeTemplate.getLod();
                                lodBuilding.setLod(shapeTemplate.getLod());

                                if (boundaryGroup.semantics != null)
                                {
                                    lodBuilding.matchSemantics(boundaryGroup.semantics, 1);
                                }

                                List<Brep> shapeList = shapeTemplate.getBrepList();
                                var anchorPoint = vertList[(int)boundaryGroup.boundaries[0]];

                                var localBrepList = new List<Brep>();

                                foreach (Brep shape in shapeList)
                                {
                                    double x = anchorPoint[0];
                                    double y = anchorPoint[1];
                                    double z = anchorPoint[2];

                                    Brep transShape = shape.DuplicateBrep();

                                    transShape.Translate(x, y, z);

                                    localBrepList.Add(transShape);
                                }
                                lodBuilding.setBrepList(localBrepList);
                            }
                            // this is all the geometry in one shape with info
                            else if (boundaryGroup.type == "Solid")
                            {
                                if (boundaryGroup.semantics != null)
                                {
                                    lodBuilding.matchSemantics(boundaryGroup.semantics, 1);
                                }

                                foreach (var solid in boundaryGroup.boundaries)
                                {
                                    lodBuilding = ReaderSupport.getBrepShape(solid, vertList, lodBuilding, true);

                                    if (lodBuilding.getError())
                                    {
                                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                    }
                                }
                            }
                            else if (boundaryGroup.type == "CompositeSolid" || boundaryGroup.type == "MultiSolid")
                            {
                                foreach (var composit in boundaryGroup.boundaries)
                                {
                                    foreach (var solid in composit)
                                    {
                                        lodBuilding = ReaderSupport.getBrepShape(solid, vertList, lodBuilding, true);

                                        if (lodBuilding.getError())
                                        {
                                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (boundaryGroup.semantics != null)
                                {
                                    lodBuilding.matchSemantics(boundaryGroup.semantics);
                                }

                                lodBuilding = ReaderSupport.getBrepShape(boundaryGroup.boundaries, vertList, lodBuilding, true);

                                if (lodBuilding.getError())
                                {
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                }
                            }

                            // check if attributes have to be stored
                            if (attributes != null)
                            {
                                if (!setLoD)
                                {
                                    semanticBuildingInfo.Add(oName + "_LoD_" + groupLoD, attributes);
                                }
                                else
                                {

                                    if (loDList.Contains((string)boundaryGroup.lod))
                                    {
                                        semanticBuildingInfo.Add(oName + "_LoD_" + groupLoD, attributes);
                                    }

                                }
                            }
                            else
                            {
                                if (!setLoD)
                                {
                                    semanticBuildingInfo.Add(oName + "_LoD_" + groupLoD, new Newtonsoft.Json.Linq.JObject());
                                }
                                else
                                {

                                    if (loDList.Contains((string)boundaryGroup.lod))
                                    {
                                        semanticBuildingInfo.Add(oName + "_LoD_" + groupLoD, new Newtonsoft.Json.Linq.JObject());

                                    }
                                }
                            }

                            //lodBuilding.joinSimple();

                            var surfaceSemanticPairedList = new List<Dictionary<string, string>>();
                            var brepList = lodBuilding.getBrepList();
                            List<Dictionary<string, string>> allSemantic = lodBuilding.getCSurfaceSemantics();
                            var name = lodBuilding.getName();
                            var bType = lodBuilding.getGeometryType();
                            var parentName = lodBuilding.getParendName();
                            var endLoD = lodBuilding.getLod();

                            // get keys
                            foreach (string typeKey in lodBuilding.getPresentTypes())
                            {
                                if (!surfaceSemanticTypes.Contains(typeKey))
                                {
                                    surfaceSemanticTypes.Add(typeKey);
                                }                   
                            }

                            for (int i = 0; i < brepList.Count; i++)
                            {
                                var semData = new Dictionary<string, string>
                                {
                                    { "Object Name", name +"_LoD_" + endLoD },
                                    { "Object Parent Name", parentName },
                                    { "Object Type", bType },
                                    { "Object LoD", endLoD },
                                };

                                if (allSemantic.Count != 0)
                                {
                                    foreach (KeyValuePair<string, string> item in allSemantic[i])
                                    {
                                        semData.Add("Surface " + char.ToUpper(item.Key[0]) + item.Key.Substring(1), item.Value);
                                    }
                                }
                                else{
                                    semData.Add("none", "none");
                                }
                                surfaceSemanticPairedList.Add(semData);
                            }

                            var complexName = name + "_LoD_" + endLoD;
                            simpleNameDict.Add(complexName, name);

                            semanticSurfaceInfo.Add(complexName, surfaceSemanticPairedList);
                            geoSurfaceInfo.Add(complexName, brepList);
                        }
                    }
                }
            }

            // make s keylist
            var sKeyList = new List<string>
            {
                "Object Name",
                "Object Parent Name",
                "Object Type",
                "Object LoD"
            };

            // add variable keys
            foreach (string variableKey in surfaceSemanticTypes)
            {
                sKeyList.Add("Surface " + char.ToUpper(variableKey[0]) + variableKey.Substring(1));
            }

            int counter = 0;
            foreach (var cObject in semanticSurfaceInfo)
            {
                foreach (var cSurf in cObject.Value)
                {
                    var nPath = new Grasshopper.Kernel.Data.GH_Path(counter);
                    var currentPair = cSurf;

                    foreach (string keyString in sKeyList)
                    {
                        try
                        {
                            dataTree.Add(currentPair[keyString], nPath);
                        }
                        catch (Exception)
                        {
                            dataTree.Add("None", nPath);
                        }
                    }
                    counter++;
                } 
            }

            // make b keylist
            var bKeyList = new List<string>();
            var correctedBkey = new List<string>();

            // check objects for keys
            ReaderSupport.populateKeyList(ref bKeyList, semanticBuildingInfo);
            ReaderSupport.populateKeyList(ref bKeyList, semanticParentInfo);            
            bKeyList.Sort();

            // make b value tree
            var bValueTree = new Grasshopper.DataTree<string>();
            if (bKeyList.Count != 0)
            {
                int pathCount = 0;
                foreach (KeyValuePair<string, dynamic> buildingAttPair in semanticBuildingInfo)
                {
                    var nPath = new Grasshopper.Kernel.Data.GH_Path(pathCount);
                    bool hasParent = false;
                    dynamic parentAtt = null;
                    dynamic buildingAdd = buildingAttPair.Value;
                    var currentName = buildingAttPair.Key;
                    var simpleName = simpleNameDict[currentName];

                    if (semanticParentInfo.ContainsKey(simpleName))
                    {
                        parentAtt = semanticParentInfo[simpleName];
                        hasParent = true;
                    }

                    bValueTree.Add(currentName, nPath);
                    foreach (var bKey in bKeyList)
                    {
                        if (buildingAdd[bKey] != null)
                        {
                            bValueTree.Add(buildingAdd[bKey].ToString(), nPath);
                        }
                        else
                        {
                            if (hasParent)
                            {
                                if (parentAtt[bKey] != null)
                                {
                                    bValueTree.Add(parentAtt[bKey].ToString() + "*", nPath);
                                }
                                else
                                {
                                    bValueTree.Add("None", nPath);
                                }
                            }
                            else
                            {
                                bValueTree.Add("None", nPath);
                            }
                        }
                    }

                    pathCount++;
                }
                bKeyList.Insert(0, "Name");
            }
            else
            {
                bKeyList.Insert(0, "None");
                bValueTree.Add("None");
            }

            foreach (string bKey in bKeyList)
            {
                correctedBkey.Add("Object " + bKey);
            }

            var linearBrepist = new List<Brep>();

            foreach (List<Brep> brepList in geoSurfaceInfo.Values)
            {
                foreach (Brep surf in brepList)
                {
                    linearBrepist.Add(surf);
                }

            }

            if (geoSurfaceInfo.Count > 0)
            {
                DA.SetDataList(0, linearBrepist);
                DA.SetDataList(1, sKeyList);
                DA.SetDataTree(2, dataTree);
                DA.SetDataList(3, correctedBkey);
                DA.SetDataTree(4, bValueTree);
            }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.readericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a2754e7"); }
        }
    }


    public class BuildingToSurfaceInfo : GH_Component
    {
        public BuildingToSurfaceInfo()
          : base("Information manager", "IManage",
              "Filters the geometry based on the input semanic data and casts the building information to the surface information format to prepare for the bakery",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info Vales", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddTextParameter("Merged Surface Info Keys", "mSiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Merged Surface Info Vales", "mSiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sKeys = new List<string>();
            var siTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var bKeys = new List<string>();
            var biTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();

            DA.GetDataList(0, sKeys);
            DA.GetDataTree(1, out siTree);
            DA.GetDataList(2, bKeys);
            DA.GetDataTree(3, out biTree);

            // construct a new key list
            var keyList = new List<string>();
            var ignoreBool = new List<bool>();
            int nameIdx = 0;

            for (int i = 0; i < sKeys.Count; i++)
            {
                keyList.Add(sKeys[i]);
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i] == "Object Name")
                {
                    nameIdx = i;
                }

                if (!keyList.Contains(bKeys[i]) && bKeys[i] != "None")
                {
                    keyList.Add(bKeys[i]);
                    ignoreBool.Add(false);
                }
                else
                {
                    ignoreBool.Add(true);
                }
            }

            // costruct a new value List
            var valueCollection = new Grasshopper.DataTree<string>();

            var sBranchCollection = siTree.Branches;
            var bBranchCollection = biTree.Branches;

            // Find all names and surface data
            for (int k = 0; k < sKeys.Count; k++)
            {
                for (int i = 0; i < sBranchCollection.Count; i++)
                {
                    var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                    for (int j = 0; j < sKeys.Count; j++)
                    {
                        if (keyList[k] == sKeys[j])
                        {
                            valueCollection.Add(sBranchCollection[i][j].ToString(), nPath);
                        }
                    }
                }
            }

            // make building dict
            Dictionary<string, List<string>> bBranchDict = new Dictionary<string, List<string>>();

            foreach (var bBranch in bBranchCollection)
            {
                List<string> templist = new List<string>();

                for (int i = 0; i < bBranch.Count; i++)
                {
                    if (i == nameIdx)
                    {
                        continue;
                    }
                    
                    templist.Add(bBranch[i].ToString());
                }
                bBranchDict.Add(bBranch[nameIdx].ToString(), templist);
            }

            Parallel.For(0, sBranchCollection.Count, i =>
            {
                var currentBranch = sBranchCollection[i];
                string branchBuildingName = currentBranch[nameIdx].ToString();
                var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                for (int k = 1; k < bKeys.Count; k++)
                {
                    if (!ignoreBool[k])
                    {
                        valueCollection.Add(bBranchDict[branchBuildingName][k-1], nPath);
                    }
                }
            });

            DA.SetDataList(0, keyList);
            DA.SetDataTree(1, valueCollection);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.divideicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }

    public class Bakery : GH_Component
    {
        public Bakery()
          : base("RCJBakery", "Bakery",
              "Bakes the RCJ data to Rhino",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry Input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "SiV", "Semantic information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            var keyList = new List<string>();
            var brepList = new List<Brep>();

            DA.GetData(3, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }

            DA.GetDataList(0, brepList);
            DA.GetDataList(1, keyList);
            DA.GetDataTree(2, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);

            if (brepList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No geo input supplied");
                return;
            }

            if (brepList[0] == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No geo input supplied");
                return;
            }

            if (siTree.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface information input supplied");
                return;
            }

            var lodList = new List<string>();
            var lodTypeDictionary = new Dictionary<string, List<string>>();
            var lodSurfTypeDictionary = new Dictionary<string, List<string>>();

            var branchCollection = siTree.Branches;

            if (branchCollection.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface information input supplied");
                return;
            }
            if (brepList.Count != branchCollection.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geo and Surface information input do not comply");
                return;
            }

            // get LoD and typelist
            int lodIdx = -1;
            int typeIdx = -1;
            int nameIdx = -1;
            int surfTypeIdx = -1;
            for (int i = 0; i < keyList.Count; i++)
            {
                if (keyList[i] == "Object LoD")
                {
                    lodIdx = i;
                }
                else if (keyList[i] == "Object Type")
                {
                    typeIdx = i;
                }
                else if (keyList[i] == "Object Name")
                {
                    nameIdx = i;
                }
                else if (keyList[i] == "Surface Type")
                {
                    surfTypeIdx = i;
                }

            }

            if (lodIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No LoD data is supplied");
                return;
            }

            if (typeIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Object type data is supplied");
                return;
            }

            if (nameIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Object name data is supplied");
                return;
            }

            if (surfTypeIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface type data is supplied");
            }

            for (int i = 0; i < branchCollection.Count; i++)
            {
                // get LoD
                string lod = branchCollection[i][lodIdx].ToString();

                if (!lodList.Contains(lod))
                {
                    lodList.Add(lod);
                }

                // get building types present in input
                string bType = branchCollection[i][typeIdx].ToString();

                if (!lodTypeDictionary.ContainsKey(lod))
                {
                    lodTypeDictionary.Add(lod, new List<string>());
                    lodTypeDictionary[lod].Add(bType);

                }
                else if (!lodTypeDictionary[lod].Contains(bType))
                {
                    lodTypeDictionary[lod].Add(bType);
                }
            }

            if (surfTypeIdx != -1)
            {
                for (int i = 0; i < branchCollection.Count; i++)
                {
                    string lod = branchCollection[i][lodIdx].ToString();

                    // get surface types present in input
                    string sType = branchCollection[i][surfTypeIdx].ToString();

                    if (!lodSurfTypeDictionary.ContainsKey(lod))
                    {
                        lodSurfTypeDictionary.Add(lod, new List<string>());
                        lodSurfTypeDictionary[lod].Add(sType);

                    }
                    else if (!lodSurfTypeDictionary[lod].Contains(sType))
                    {
                        lodSurfTypeDictionary[lod].Add(sType);
                    }
                }
            }

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            // create a new unique master layer name
            Rhino.DocObjects.Layer parentlayer = new Rhino.DocObjects.Layer();
            parentlayer.Name = "RCJ output";
            parentlayer.Color = System.Drawing.Color.Red;
            parentlayer.Index = 100;

            // if the layer already exists find a new name
            int count = 0;
            if (activeDoc.Layers.FindName("RCJ output") != null)
            {
                while (true)
                {
                    if (activeDoc.Layers.FindName("RCJ output - " + count.ToString()) == null)
                    {
                        parentlayer.Name = "RCJ output - " + count.ToString();
                        parentlayer.Index = parentlayer.Index + count;
                        break;
                    }
                    count++;
                }
            }

            activeDoc.Layers.Add(parentlayer);
            var parentID = activeDoc.Layers.FindName(parentlayer.Name).Id;

            // create LoD layers
            var lodId = new Dictionary<string, System.Guid>();
            var typId = new Dictionary<string, Dictionary<string, int>>();
            var surId = new Dictionary<string, Dictionary<string, int>>();
            var typColor = BakerySupport.getTypeColor();
            var surfColor = BakerySupport.getSurfColor();

            for (int i = 0; i < lodList.Count; i++)
            {
                Rhino.DocObjects.Layer lodLayer = new Rhino.DocObjects.Layer();
                lodLayer.Name = "LoD " + lodList[i];
                lodLayer.Color = System.Drawing.Color.DarkRed;
                lodLayer.Index = 200 + i;
                lodLayer.ParentLayerId = parentID;

                var id = activeDoc.Layers.Add(lodLayer);
                var idx = activeDoc.Layers.FindIndex(id).Id;
                lodId.Add(lodList[i], idx);
                typId.Add(lodList[i], new Dictionary<string, int>());
                surId.Add(lodList[i], new Dictionary<string, int>());
            }

            foreach (var lodTypeLink in lodTypeDictionary)
            {
                var targeLId = lodId[lodTypeLink.Key];
                var cleanedTypeList = new List<string>();

                foreach (var bType in lodTypeLink.Value)
                {
                    var filteredName = BakerySupport.getParentName(bType);

                    if (!cleanedTypeList.Contains(filteredName))
                    {
                        cleanedTypeList.Add(filteredName);
                    }
                }

                foreach (var bType in cleanedTypeList)
                {
                    Rhino.DocObjects.Layer typeLayer = new Rhino.DocObjects.Layer();
                    typeLayer.Name = bType;

                    System.Drawing.Color lColor = System.Drawing.Color.DarkRed;
                    try
                    {
                        lColor = typColor[bType];
                    }
                    catch
                    {
                        continue;
                    }

                    typeLayer.Color = lColor;
                    typeLayer.ParentLayerId = targeLId;

                    var idx = activeDoc.Layers.Add(typeLayer);
                    typId[lodTypeLink.Key].Add(bType, idx);
                }
            }

            if (surfTypeIdx != -1)
            {
                foreach (var lodTypeLink in lodSurfTypeDictionary)
                {
                    var targeLId = activeDoc.Layers.FindIndex(typId[lodTypeLink.Key]["Building"]).Id;
                    var cleanedSurfTypeList = new List<string>();

                    foreach (var bType in lodTypeLink.Value)
                    {
                        var filteredName = BakerySupport.getParentName(bType);

                        if (!cleanedSurfTypeList.Contains(filteredName))
                        {
                            cleanedSurfTypeList.Add(filteredName);
                        }
                    }

                    foreach (var sType in cleanedSurfTypeList)
                    {
                        Rhino.DocObjects.Layer surfTypeLayer = new Rhino.DocObjects.Layer();
                        surfTypeLayer.Name = sType;

                        System.Drawing.Color lColor = System.Drawing.Color.DarkRed;
                        try
                        {
                            lColor = surfColor[sType];
                        }
                        catch
                        {
                            continue;
                        }

                        surfTypeLayer.Color = lColor;
                        surfTypeLayer.ParentLayerId = targeLId;

                        var idx = activeDoc.Layers.Add(surfTypeLayer);
                        surId[lodTypeLink.Key].Add(sType, idx);
                    }
                }
            }

            // bake geo
            var groupName = branchCollection[0][nameIdx].ToString() + branchCollection[0][lodIdx].ToString();
            activeDoc.Groups.Add("LoD: " + branchCollection[0][lodIdx].ToString() + " - " + branchCollection[0][nameIdx].ToString());
            var groupId = activeDoc.Groups.Add(groupName);
            activeDoc.Groups.FindIndex(groupId).Name = groupName;

            var potetialGroupList = new List<System.Guid>();

            for (int i = 0; i < branchCollection.Count; i++)
            {
                if (groupName != branchCollection[i][nameIdx].ToString() + branchCollection[i][lodIdx].ToString())
                {
                    if (potetialGroupList.Count > 1)
                    {
                        foreach (var groupItem in potetialGroupList)
                        {
                            activeDoc.Groups.AddToGroup(groupId, groupItem);
                        }
                    }
                    potetialGroupList.Clear();

                    groupName = branchCollection[i][nameIdx].ToString() + branchCollection[i][lodIdx].ToString();
                    groupId = activeDoc.Groups.Add("LoD: " + branchCollection[i][lodIdx].ToString() + " - " + branchCollection[i][nameIdx].ToString());
                }

                var targetBrep = brepList[i];
                string lod = branchCollection[i][lodIdx].ToString();
                string bType = BakerySupport.getParentName(branchCollection[i][typeIdx].ToString());

                string sType = "None";
                if (surfTypeIdx != -1)
                {
                    sType = branchCollection[i][surfTypeIdx].ToString();
                }

                Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                objectAttributes.Name = branchCollection[i][nameIdx].ToString() + " - " + i;

                for (int j = 0; j < branchCollection[i].Count; j++)
                {
                    string fullName = branchCollection[i][j].ToString();
                    if (j == 0)
                    {
                        fullName = fullName.Substring(0, fullName.Length - BakerySupport.getPopLength(branchCollection[i][lodIdx].ToString())); // TODO make this smart
                    }

                    objectAttributes.SetUserString(keyList[j], fullName);
                }

                if (bType != "Building")
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else if (sType == "None" || surfTypeIdx == -1)
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else
                {
                    objectAttributes.LayerIndex = surId[lod][sType];
                }

                potetialGroupList.Add(activeDoc.Objects.AddBrep(targetBrep, objectAttributes));
            }

            // bake final group
            if (potetialGroupList.Count > 1)
            {
                foreach (var groupItem in potetialGroupList)
                {
                    activeDoc.Groups.AddToGroup(groupId, groupItem);
                }
            }
            potetialGroupList.Clear();
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.bakeryicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e18"); }
        }
    }

    public class Filter : GH_Component
    {
        public Filter()
          : base("Filter", "Filter",
              "Filters the geometry based on the input semanic data and casts the building information to the surface information format to prepare for the bakery",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Info Keys", "iK", "Keys of the information output", GH_ParamAccess.list);
            pManager.AddGenericParameter("Info Vales", "iV", "Values of the information output", GH_ParamAccess.tree);
            pManager.AddTextParameter("Filter Info Keys", "Fik", "Keys of the Semantic information which is used to filter on", GH_ParamAccess.list);
            pManager.AddTextParameter("Filter Info Value(s)", "FiV", "Value(s) of the semantic information  which is used to filter on", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Equals/ Not Equals", "==", "Booleans that dictates if the value should be equal, or not equal to filter input value", GH_ParamAccess.item, true);

            pManager[2].Optional = true; // origin is optional
            pManager[3].Optional = true; // origin is optional

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Merged Surface Info Vales", "mSiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var keyList = new List<string>();
            var keyFilter = new List<string>();
            var keyStrings = new List<string>();
            bool equals = true;

            DA.GetDataList(0, keyList);
            DA.GetDataTree(1, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);
            DA.GetDataList(2, keyFilter);
            DA.GetDataList(3, keyStrings);
            DA.GetData(4, ref equals);

            if (keyFilter.Count == 0 && keyStrings.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter Key can not be empty when Filter values is not empty");
                return;
            }
            if (keyFilter.Count != 0 && keyStrings.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter values can not be empty when Filter Key is not empty");
                return;
            }
            if (keyFilter.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Can only filter on a single key");
                return;
            }
            if (keyFilter.Count == 0 && keyStrings.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No filter input");
                DA.SetDataTree(0, siTree);
                return;
            }

            if (keyList.Count != siTree.Branches[0].Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Info keys and values do not comply");
                return;
            }

            // get the indx of the filter key
            int keyIdx = -1;

            for (int i = 0; i < keyList.Count; i++)
            {
                if (keyList[i] == keyFilter[0])
                {
                    keyIdx = i;
                }
            }

            if (keyIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter key can not be found in the info key list");
                return;
            }


            // find the complying branches
            var dataTree = new Grasshopper.DataTree<string>();
            var branchCollection = siTree.Branches;

            if (equals)
            {
                for (int i = 0; i < branchCollection.Count; i++)
                {
                    for (int j = 0; j < keyStrings.Count; j++)
                    {
                        if (keyStrings[j] == branchCollection[i][keyIdx].ToString())
                        {
                            var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                            foreach (var item in branchCollection[i])
                            {
                                dataTree.Add(item.ToString(), nPath);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < branchCollection.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < keyStrings.Count; j++)
                    {
                        if (keyStrings[j] == branchCollection[i][keyIdx].ToString())
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                        foreach (var item in branchCollection[i])
                        {
                            dataTree.Add(item.ToString(), nPath);
                        }
                    }
                }
            }


            if (dataTree.BranchCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No matching values could be found");
            }

            DA.SetDataTree(0, dataTree);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.filtericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }

}