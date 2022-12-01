using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
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

        private List<int> materialIdx_ = new List<int>();
        private List<int> cleanedMaterialIdx_ = new List<int>();
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
        public List<int> getMaterialValues() { return materialIdx_; }
        public List<int> getCMaterialValues() { return cleanedMaterialIdx_; }
        public void addCsurfaceSemantic(List<Dictionary<string, string>> surfaceSemantics)
        {
            foreach (Dictionary<string, string> surfaceSemantic in surfaceSemantics)
            {
                cleanedSurfaceSemantics_.Add(surfaceSemantic);
            }
        }

        public void addCmaterialVaues(List<int> surfaceValues)
        {
            foreach(int surfaceValue in surfaceValues)
            {
                cleanedMaterialIdx_.Add(surfaceValue);
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

        public void matchMaterials(dynamic materialData, int ind = 0)
        {
            foreach (var solidIdx in materialData)
            {
                foreach (var typeIdx in solidIdx)
                {
                    if (ind == 1)
                    {
                        if (typeIdx["values"] != null)
                        {
                            foreach (var solid in typeIdx["values"])
                            {
                                foreach (var valueIdx in solid)
                                {
                                    if (valueIdx == null)
                                    {
                                        materialIdx_.Add(-1);
                                    }
                                    else
                                    {
                                        materialIdx_.Add((int)valueIdx);
                                    }
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                        
                    }// TODO make function with railway model
                    if (ind == 0)
                    {
                        if (typeIdx["values"] != null)
                        {

                            foreach (var valueIdx in typeIdx["values"])
                            {
                                if (valueIdx == null)
                                {
                                    materialIdx_.Add(-1);
                                }
                                else
                                {
                                    materialIdx_.Add((int)valueIdx);
                                }
                            }
                        }
                        else
                        {
                           continue;
                        }
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
        /// @brief 
        static public List<Rhino.Geometry.Point3d> getVerts(
            dynamic Jcity, 
            Point3d worldOrigin, 
            double scaler,
            double rotationAngle,
            bool isFirst, bool 
            translate
            )
        {

            // coordinates of the first input
            double globalX = 0.0;
            double globalY = 0.0;
            double globalZ = 0.0;

            double originX = worldOrigin.X;
            double originY = worldOrigin.Y;
            double originZ = worldOrigin.Z;

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
                localX = Jcity.transform.translate[0] * scaler;
                localY = Jcity.transform.translate[1] * scaler;
                localZ = Jcity.transform.translate[2] * scaler;
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
                localX = Jcity.transform.translate[0] * scaler - globalX * scaler;
                localY = Jcity.transform.translate[1] * scaler - globalY * scaler;
                localZ = Jcity.transform.translate[2] * scaler - globalZ * scaler;
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
            return vertList;
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

        static public double getDocScaler()
        {
            string UnitString = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();

            if (UnitString == "Meters") { return 1; }
            else if (UnitString == "Centimeters") { return 100; }
            else if (UnitString == "Millimeters") { return 1000; }
            else if (UnitString == "Feet") { return 3.28084; }
            else if (UnitString == "Inches") { return 39.3701; }
            else { return -1; }
        }

        public static string concatonateStringList(List<string> stringList)
        {
            string conString = "";

            if (stringList.Count == 1)
            {
               return stringList[0];
            }
            else
            {
                conString = stringList[0];
                for (int i = 1; i < stringList.Count; i++)
                {
                    conString = conString + ", " + stringList[i];
                }
                return conString;
            }
        }
    }

    namespace CJT
    {
        class RingStructure // simple way to display a surface as a collection of rings
        {
            List<int> outerRing_ = new List<int>();
            List<List<int>> innerRingList_ = new List<List<int>>();

            public void setOuterRing(List<int> outerRing) { outerRing_ = outerRing; }
            public List<int> getOuterRing() { return outerRing_; }
            public void setInnerRings(List<List<int>> innerRings) { innerRingList_ = innerRings; }
            public void addInnerRing(List<int> innerRing) { innerRingList_.Add(innerRing); }
            public List<List<int>> getInnerRingList() { return innerRingList_; }
            public Rhino.Collections.CurveList getPolyStructure(List<Rhino.Geometry.Point3d> vertList)
            {
                // ring technique
                Rhino.Collections.CurveList surfaceCurves = new Rhino.Collections.CurveList();
                List<List<int>> rings = getInnerRingList();
                rings.Add(getOuterRing());

                foreach (var ring in rings)
                {
                    List<Rhino.Geometry.Point3d> curvePointsOuter = new List<Rhino.Geometry.Point3d>();
                    foreach (var vertIdx in ring)
                    {
                        curvePointsOuter.Add(vertList[vertIdx]);
                    }
                    if (curvePointsOuter.Count > 0)
                    {
                        curvePointsOuter.Add(curvePointsOuter[0]);

                        try //TODO this can be improved
                        {
                            Rhino.Geometry.Polyline polyRing = new Rhino.Geometry.Polyline(curvePointsOuter);
                            surfaceCurves.Add(polyRing);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                return surfaceCurves;
            }
        }


        class SurfaceObject
        {
            Rhino.Geometry.Brep shape_;
            int semanticValue_;

            public void setShape(Brep shape) { shape_ = shape; }
            public Brep getShape() { return shape_; }
            public void setSemanticValue(int materialValue) { semanticValue_ = materialValue; }
            public int getSemanticlValue() { return semanticValue_; } 
        }

        class GeoObject
        {
            List<SurfaceObject> boundaries_ = new List<SurfaceObject>();
            string lod_ = "-1";
            List<Dictionary<string, dynamic>> surfaceData_ = new List<Dictionary<string, dynamic>>();
            List<int> surfaceTypeValues_ = new List<int>();
            Dictionary<string, List<int>> surfaceMaterialValues_ = new Dictionary<string, List<int>>();
            string geoType_ = "";
            string GeoName_ = "";
            bool hasSurfaceData_ = false;

            public List<SurfaceObject> getBoundaries() { return boundaries_; }
            public void setBoundaries(List<SurfaceObject> boundaries) { boundaries_ = boundaries; }
            public string getLoD() { return lod_; }
            public void setLod(string lod) { lod_ = lod; }
            public List<Dictionary<string, dynamic>> getSurfaceData() { return surfaceData_; }
            public Dictionary<string, dynamic> getSurfaceData(int i) { return surfaceData_[i]; }
            public void setSurfaceData(dynamic surfaceData) 
            {
                List<Dictionary<string, dynamic>> completeSemanticColletion = new List<Dictionary<string, dynamic>>(); 
                foreach (var surfdata in surfaceData)
                {
                    Dictionary<string, dynamic> surfaceSem = new Dictionary<string, dynamic>();

                    foreach (var entry in surfdata)
                    {
                        surfaceSem.Add(entry.Name.ToString(), entry.Value);
                    }
                    completeSemanticColletion.Add(surfaceSem);
                }      
                surfaceData_ = completeSemanticColletion;

                if (completeSemanticColletion.Count > 0)
                {
                    hasSurfaceData_ = true;
                }
            }
            public List<int> getSurfaceTypeValues() { return surfaceTypeValues_; }
            public int getSurfaceTypeValue(int i) { return surfaceTypeValues_[i]; }
            public void setSurfaceTypeValues(dynamic surfaceTypeValues) { surfaceTypeValues_ = flattenValues(surfaceTypeValues); }
            public Dictionary<string, List<int>> getSurfaceMaterialValues() { return surfaceMaterialValues_; }
            public void setSurfaceMaterialValues(dynamic surfaceMaterialCollection) 
            {
                surfaceMaterialValues_ = new Dictionary<string, List<int>>();
                foreach (var surfaceMaterial in surfaceMaterialCollection)
                {
                    string valueName = surfaceMaterial.Name.ToString();
                    surfaceMaterialValues_.Add(valueName, flattenValues(surfaceMaterial.Value.values));
                }
            }
            public string getGeoType() { return geoType_; }
            public void setGeoType(string geoType) { geoType_ = geoType; }
            public string getGeoName() { return GeoName_; }
            public void setGeoName(string geoName) { GeoName_ = geoName; }

            List<int> flattenValues(dynamic nestedValues)
            {
                List<int> flatList = new List<int>();
                if (nestedValues[0].Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    foreach (int value in nestedValues)
                    {
                        flatList.Add(value);
                    }
                }
                else
                {
                    foreach (var nestedValue in nestedValues)
                    {
                        foreach (var value in flattenValues(nestedValue))
                        {
                            flatList.Add(value);
                        }
                    }
                }               
                return flatList;
            }

            public void setGeometry(dynamic JBoundaryList, List<Rhino.Geometry.Point3d> vertList, double scaler)
            {
                boundaries_ = new List<SurfaceObject>();
                List<RingStructure> ringList = boundaries2Rings(JBoundaryList);

                int counter = 0;
                foreach (var ringSet in ringList)
                {
                    List<int> outerRing = ringSet.getOuterRing();
                    if (ringSet.getInnerRingList().Count == 0 && outerRing.Count <= 4)
                    {
                        NurbsSurface nSurface;
                        if (outerRing.Count == 3)
                        {
                            nSurface = NurbsSurface.CreateFromCorners(
                                vertList[outerRing[0]],
                                vertList[outerRing[1]],
                                vertList[outerRing[2]]
                                );
                        }
                        else
                        {
                            nSurface = NurbsSurface.CreateFromCorners(
                                vertList[outerRing[0]],
                                vertList[outerRing[1]],
                                vertList[outerRing[2]],
                                vertList[outerRing[3]]
                                );
                        }
                        if (nSurface != null)
                        {
                            SurfaceObject surfaceObject = new SurfaceObject();
                            surfaceObject.setShape(nSurface.ToBrep());
                            surfaceObject.setSemanticValue(counter);
                            boundaries_.Add(surfaceObject);
                        }
                    }
                    else
                    {
                        Rhino.Collections.CurveList surfaceCurves = ringSet.getPolyStructure(vertList);

                        if (surfaceCurves.Count > 0)
                        {
                            Rhino.Geometry.Brep[] planarFace = Brep.CreatePlanarBreps(surfaceCurves, 0.1 * scaler);
                            surfaceCurves.Clear();
                            if (planarFace != null)
                            {
                                SurfaceObject surfaceObject = new SurfaceObject();
                                surfaceObject.setShape(planarFace[0]);
                                surfaceObject.setSemanticValue(counter);
                                boundaries_.Add(surfaceObject);
                            }
                        }
                    }
                    counter++;
                }
            }

            private List<RingStructure> boundaries2Rings(dynamic JBoundaryList)
            {
                List<RingStructure> ringCollection = new List<RingStructure>();
                if (JBoundaryList[0][0].Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    RingStructure ringStructure = new RingStructure();
                    int c = 0;
                    foreach (var ring in JBoundaryList)
                    {
                        List<int> ringList = new List<int>();
                        foreach (int idx in ring) { ringList.Add(idx); }
                        if (c == 0) { ringStructure.setOuterRing(ringList); }
                        else { ringStructure.addInnerRing(ringList); }
                        c++;
                    }
                    ringCollection.Add(ringStructure);
                }
                else
                {
                    foreach (var JBoundary in JBoundaryList)
                    {
                        foreach (var ringSet in boundaries2Rings(JBoundary))
                        {
                            ringCollection.Add(ringSet);
                        }
                    }
                }
                return ringCollection;
            }

            public bool hasSurfaceData() { return hasSurfaceData_; }
            public bool hasMaterialData()
            {
                if (surfaceMaterialValues_.Count() > 0)
                {
                    return true;
                }
                return false;
             }
        }

        class CityObject
        {
            string name_ = "";
            string type_ = "";

            List<GeoObject> geometry_ = new List<GeoObject>();

            bool hasGeo_ = false;
            bool isParent_ = false;
            bool isChild_ = false;
            bool hasAttributes_ = false;
            bool isFilteredOut_ = false;

            Dictionary<string, dynamic> attributes_ = new Dictionary<string, dynamic>();
            List<string> parentList_ = new List<string>();
            List<string> childList_ = new List<string>();


            public string getName() { return name_; }
            public void setName(string name) { name_ = validifyString(name); }
            public string getType() { return type_; }
            public void setType(string type) { type_ = type; }
            public List<GeoObject> getGeometry() { return geometry_; }
            public void addGeometry(GeoObject geoObject) { geometry_.Add(geoObject); }
            public Dictionary<string, dynamic> getAttributes() { return attributes_; }
            public bool hasGeo() { return hasGeo_; }
            public void setHasGeo(bool hasGeo) { hasGeo_ = hasGeo; }
            public bool isParent() { return isParent_; }
            public void setIsParent(bool isParent) { isParent_ = isParent; }
            public bool isChild() { return isChild_; }
            public void setIsChild(bool isChild) { isChild_ = isChild; }
            public bool hasAttributes() { return hasAttributes_; }
            public void setHasAttributes(bool hasAttributes) { hasAttributes_ = hasAttributes; }
            public void setIsFilteredout() { isFilteredOut_ = true; }
            public bool isFilteredout() { return isFilteredOut_; }
            public dynamic getAttribute(string key) { return attributes_[key]; }
            public void addAttribute(string key, dynamic value) { attributes_.Add(key, value); }
            public void setAttributes(dynamic jAttributeList)
            {
                attributes_ = new Dictionary<string, dynamic>();
                if (jAttributeList != null)
                {
                    setHasAttributes(true);
                    foreach (var attribute in jAttributeList)
                    {
                        addAttribute(attribute.Name, attribute.Value);
                    }
                }
            }
            public List<string> getParents() { return parentList_; }
            public void addParent(string parent) { parentList_.Add(parent); } // TODO string name check?
            public void setParents(dynamic jParentList)
            {
                parentList_ = new List<string>();
                if (jParentList != null)
                {
                    setIsChild(true); 
                    foreach (var parent in jParentList)
                    {
                        addParent(parent.ToString());
                    }
                }
            }
            public List<string> getChildren() { return childList_; }
            public void addChild(string child) { childList_.Add(child); }
            public void setChildren(dynamic jChildList)
            {
                childList_ = new List<string>();
                if (jChildList != null)
                {
                    setIsParent(true);
                    foreach (var child in jChildList)
                    {
                        addChild(child.ToString());
                    }
                }
            }

           public Dictionary<string, dynamic> getInheritancedAtt(CityCollection cityCollection)
            {
                if (parentList_.Count == 0) { return new Dictionary<string, dynamic>(); }

                Dictionary<string, dynamic> inheritedAtt = new Dictionary<string, dynamic>();
                foreach (string parentName in parentList_)
                {
                    CityObject parentObject = cityCollection.getObject(parentName);

                    foreach (var item in parentObject.getAttributes())
                    {
                        if (!inheritedAtt.ContainsKey(item.Key))
                        {
                            inheritedAtt.Add(item.Key, item.Value);
                        }
                    }
                }
                return inheritedAtt;
            }

            public string validifyString(string name)
            {
                string tName = "";
                foreach (char c in name)
                {
                    if (c != '{' && c != '}' && c != '?' && c != '@' && c != '/' && c != '\\')
                    {
                        tName += c;
                    }
                }
                return tName;
            }
        }

        class CityCollection
        {
            Dictionary<string, CityObject> objectCollection_ = new Dictionary<string, CityObject>();

            public void add(CityObject cityObject)
            {
                objectCollection_.Add(cityObject.getName(), cityObject);
            }

            public List<CityObject> getFlatColletion()
            {
                List<CityObject> collection = new List<CityObject>();
                foreach (var item in objectCollection_) {
                    var itemValues = item.Value;
                    if (!itemValues.isFilteredout())
                    {
                        collection.Add(item.Value);
                    }
                }
                return collection;
            }

            public Dictionary<string, CityObject> getCollection() { return objectCollection_; }
            public CityObject getObject(string objectName) { return objectCollection_[objectName]; }
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
    }


    public class LoDReader : GH_Component
    {
        public LoDReader()
          : base("Document Reader", "DReader",
              "Fetches the Metadata, Textures and Materials from a CityJSON file, Autoresolves when multiple inputs",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Metadata Keys", "MdK", "Keys of the Metadata stored in the files", GH_ParamAccess.item);
            pManager.AddTextParameter("Metadata Values", "MdV", "Values of the Metadata stored in the files", GH_ParamAccess.tree);
            pManager.AddTextParameter("LoD", "L", "LoD levels", GH_ParamAccess.item);
            pManager.AddTextParameter("Material Keys", "MK", "Key output representing the material list stord in the files", GH_ParamAccess.list);
            pManager.AddTextParameter("Material Values", "MV", "Color output representing the material list stord in the files", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            List<string> pathList = new List<string>();
            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }
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
            var materialsTree = new Grasshopper.DataTree<string>(); ;
            var nestedMetaData = new List<Dictionary<string, string>>();
            List<string> materialKeyList = new List<string>()
            {
                "name",
                "ambientIntensity",
                "diffuseColor",
                "emissiveColor",
                "specularColor",
                "shininess",
                "transparency",
                "isSmooth"
            };
            Dictionary<string, List<string>> materialsDict = new Dictionary<string, List<string>>();

            foreach (var path in pathList)
            {
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                // Check if valid CityJSON format
                dynamic Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }

                // fetch metadata
                if (Jcity.metadata != null)
                {
                    foreach (Newtonsoft.Json.Linq.JProperty metaGroup in Jcity.metadata)
                    {
                        var metaValue = metaGroup.Value;
                        var metaName = metaGroup.Name;

                        if (metaValue.Count() == 0)
                        {
                            metadata.Add(metaName.ToString(), metaValue.ToString());
                        }
                        else
                        {
                            if (metaName.ToString() == "geographicalExtent" && metaValue.Count() == 6)
                            {
                                // create two string points
                                string minPoint = "{" + metaValue[0].ToString() + ", " + metaValue[1].ToString() + ", " + metaValue[2].ToString() + "}";
                                metadata.Add("geographicalExtent minPoint", minPoint);

                                string maxPoint = "{" + metaValue[3].ToString() + ", " + metaValue[4].ToString() + ", " + metaValue[5].ToString() + "}";
                                metadata.Add("geographicalExtent maxPoint", maxPoint);

                            }
                            else
                            {
                                foreach (Newtonsoft.Json.Linq.JProperty nestedMetaValue in metaValue)
                                {
                                    metadata.Add(metaName.ToString() + " " + nestedMetaValue.Name.ToString(), nestedMetaValue.Value.ToString());
                                }
                            }
                        }

                    }
                    nestedMetaData.Add(metadata);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No metadata found!");
                }

                // get materials
                var appearance = Jcity.appearance;
                if (appearance != null)
                {
                    var materials = appearance.materials;
                    if (materials != null)
                    {
                        int c = 0;
                        foreach (var material in materials)
                        {
                            var nPath = new Grasshopper.Kernel.Data.GH_Path(c);

                            foreach (var mKey in materialKeyList)
                            {
                                var currentValue = material[mKey];
                                if (currentValue != null)
                                {
                                    if (currentValue.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                                    {
                                        string materialString = Math.Round((float)currentValue[0] * 256) + "," + Math.Round((float)currentValue[1] * 256) + "," + Math.Round((float)currentValue[2] * 256); 
                                        materialsTree.Add(materialString, nPath);                                
                                    }
                                    else
                                    {
                                        materialsTree.Add(material[mKey].ToString(), nPath);
                                    }
                                }
                                else
                                {
                                    materialsTree.Add("None", nPath);
                                }      
                            }
                            c++;
                        }
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No materials found!");
                        materialKeyList = new List<string>();
                    }
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No materials found!");
                    materialKeyList = new List<string>();
                }

                // get LoD
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

            // make tree from meta data
            var dataTree = new Grasshopper.DataTree<string>();

            var metaValues = new List<string>();
            var metaKeys = new List<string>();

            foreach (var metadata in nestedMetaData)
            {
                foreach (var item in metadata)
                {
                    if (!metaKeys.Contains(item.Key))
                    {
                        metaKeys.Add(item.Key);
                    }
                }
            }

            int counter = 0;
            foreach (var metadata in nestedMetaData)
            {
                var nPath = new Grasshopper.Kernel.Data.GH_Path(counter);

                foreach (var metaKey in metaKeys)
                {
                    if (metadata.ContainsKey(metaKey))
                    {
                        dataTree.Add(metadata[metaKey], nPath);
                    }
                }
                counter++;
            }
            lodLevels.Sort();
            DA.SetDataList(0, metaKeys);
            DA.SetDataTree(1, dataTree);
            DA.SetDataList(2, lodLevels);
            DA.SetDataList(3, materialKeyList);
            DA.SetDataTree(4, materialsTree);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.metaicon;
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
            pManager.AddTextParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
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

            // get scale from current session
            double scaler = ReaderSupport.getDocScaler();

            if (scaler == -1)
            {
                scaler = 1;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Rhino document scale is not supported, defaulted to unit 1");
            }

            // Parse and check if valid CityJSON format
            List<dynamic> cityJsonCollection = new List<dynamic>();
            foreach (var path in pathList)
            {  
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }
                cityJsonCollection.Add(Jcity);
            }

            bool isFirst = true;
            CJT.CityCollection ObjectCollection = new CJT.CityCollection();
            List<string> surfaceTypes = new List<string>();
            List<string> objectTypes = new List<string>();
            List<string> materialReferenceNames = new List<string>();

            foreach (var Jcity in cityJsonCollection)
            {
                // get vertices stored in a tile
                List<Rhino.Geometry.Point3d> vertList = ReaderSupport.getVerts(Jcity, worldOrigin, scaler, rotationAngle, isFirst, translate);
                isFirst = false;

                foreach (var JcityObject in Jcity.CityObjects)
                {
                    CJT.CityObject cityObject = new CJT.CityObject();
                    dynamic JCityObjectAttributes = JcityObject.Value;
                    dynamic JCityObjectAttributesAttributes = JCityObjectAttributes.attributes;

                    if (JCityObjectAttributesAttributes != null)
                    {
                        foreach (var attribue in JCityObjectAttributesAttributes) { objectTypes.Add(attribue.Name); }
                    }

                    cityObject.setName(JcityObject.Name);
                    cityObject.setType(JCityObjectAttributes.type.ToString());
                    cityObject.setParents(JCityObjectAttributes.parents);
                    cityObject.setChildren(JCityObjectAttributes.children);
                    cityObject.setAttributes(JCityObjectAttributesAttributes);

                    if (JCityObjectAttributes.geometry == null)
                    { 
                        cityObject.setIsFilteredout();
                        ObjectCollection.add(cityObject);
                        continue;
                    }
                    cityObject.setHasGeo(true);

                    int uniqueCounter = 0;
                    foreach (var jGeoObject in JCityObjectAttributes.geometry)
                    {
                        string lod = jGeoObject.lod.ToString();

                        if (setLoD)
                        {
                            if (!loDList.Contains(lod)) { continue; }
                        }


                        CJT.GeoObject geoObject = new CJT.GeoObject();
                        geoObject.setGeoName(uniqueCounter.ToString());
                        geoObject.setGeoType(jGeoObject.type.ToString());
                        geoObject.setLod(lod);

                        if (jGeoObject.semantics != null)
                        {                           
                            if (jGeoObject.semantics.surfaces != null && jGeoObject.semantics.values != null)
                            {
                                dynamic jSurfaceAttrubutes = jGeoObject.semantics.surfaces;

                                if (jSurfaceAttrubutes != null)
                                {
                                    foreach (var attribueCollection in jSurfaceAttrubutes) 
                                    {
                                        foreach (var attribue in attribueCollection) { surfaceTypes.Add(attribue.Name); }
                                    }
                                }

                                geoObject.setSurfaceData(jSurfaceAttrubutes);
                                geoObject.setSurfaceTypeValues(jGeoObject.semantics.values);
                            }
                        }
                        if (jGeoObject.material != null)
                        {
                            var materialObject = jGeoObject.material;
                            geoObject.setSurfaceMaterialValues(materialObject);
                            foreach (var surfaceMaterial in materialObject) { materialReferenceNames.Add(surfaceMaterial.Name.ToString()); }
                        }
                        geoObject.setGeometry(jGeoObject.boundaries, vertList, scaler);
                        cityObject.addGeometry(geoObject);
                        uniqueCounter++;
                    }
                    if (cityObject.getGeometry().Count == 0) { cityObject.setIsFilteredout(); }
                    ObjectCollection.add(cityObject); // data without geometry is still stored for attributes 
                }
            }

            surfaceTypes = surfaceTypes.Distinct().ToList();
            objectTypes = objectTypes.Distinct().ToList();
            materialReferenceNames = materialReferenceNames.Distinct().ToList();

            // make keyLists
            List<string> surfaceKeyList = new List<string>();
            surfaceKeyList.Add("Object Name");
            surfaceKeyList.Add("Geometry type");
            surfaceKeyList.Add("Geometry Name");
            surfaceKeyList.Add("Geometry LoD");

            foreach (var item in surfaceTypes)
            {
                surfaceKeyList.Add("Surface " + item);
            }
            foreach (var item in materialReferenceNames)
            {
                surfaceKeyList.Add("Surface Material " + item);
            }

            List<string> objectKeyList = new List<string>();
            objectKeyList.Add("Object Name");
            objectKeyList.Add("Object Type");
            objectKeyList.Add("Object Parent");
            objectKeyList.Add("Object Child");

            foreach (string item in objectTypes)
            {
                objectKeyList.Add("Object " + item);
            }

            // flatten data for grasshopper output
            List<Brep> flatSurfaceList = new List<Brep>();
            var flatSurfaceSemanticTree = new Grasshopper.DataTree<string>();
            var flatObjectSemanticTree = new Grasshopper.DataTree<string>();
            int objectCounter = 0;
            int surfaceCounter = 0;
            foreach (var cityObject in ObjectCollection.getFlatColletion())
            {
                var objectPath = new Grasshopper.Kernel.Data.GH_Path(objectCounter);

                // add native object attributes
                string objectName = cityObject.getName();
                List<string> objectParents = cityObject.getParents();
                List<string> objectChildren = cityObject.getChildren();

                flatObjectSemanticTree.Add(objectName, objectPath);
                flatObjectSemanticTree.Add(cityObject.getType(), objectPath);

                if (objectParents.Count > 0) { flatObjectSemanticTree.Add(ReaderSupport.concatonateStringList(objectParents)); }
                else { flatObjectSemanticTree.Add("None");}

                if (objectChildren.Count > 0) { flatObjectSemanticTree.Add(ReaderSupport.concatonateStringList(objectChildren)); }
                else { flatObjectSemanticTree.Add("None"); }

                // add custom object attributes
                var objectAttributes = cityObject.getAttributes();
                var inheritedAttributes = cityObject.getInheritancedAtt(ObjectCollection);

                foreach (string item in objectTypes)
                {
                    if (objectAttributes.ContainsKey(item)) 
                    {
                        flatObjectSemanticTree.Add(objectAttributes[item].ToString(), objectPath); 
                    }
                    else if (inheritedAttributes.ContainsKey(item))
                    {
                        flatObjectSemanticTree.Add(inheritedAttributes[item].ToString() + "*", objectPath);
                    }
                    else 
                    { 
                        flatObjectSemanticTree.Add("None", objectPath); 
                    }
                }

                foreach (var geoObject in cityObject.getGeometry())
                {
                    string geoType = geoObject.getGeoType();
                    string geoName = geoObject.getGeoName();
                    string geoLoD = geoObject.getLoD();

                    int boundaryCounter = 0;
                    foreach (var surface in geoObject.getBoundaries())
                    {
                        var surfacePath = new Grasshopper.Kernel.Data.GH_Path(surfaceCounter);
                        flatSurfaceList.Add(surface.getShape());

                        // add object name for aquiring object
                        flatSurfaceSemanticTree.Add(objectName, surfacePath);

                        // add geometry data
                        flatSurfaceSemanticTree.Add(geoType, surfacePath);
                        flatSurfaceSemanticTree.Add(geoName, surfacePath);
                        flatSurfaceSemanticTree.Add(geoLoD, surfacePath);

                        // add semantic surface data
                        if (geoObject.hasSurfaceData())
                        {
                            var surfaceSemantics = geoObject.getSurfaceData(geoObject.getSurfaceTypeValue(surface.getSemanticlValue()));

                            foreach (var item in surfaceTypes)
                            {
                                if (surfaceSemantics.ContainsKey(item))
                                {
                                    flatSurfaceSemanticTree.Add(surfaceSemantics[item].ToString(), surfacePath);
                                }
                                else flatSurfaceSemanticTree.Add("None", surfacePath);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < surfaceTypes.Count; i++)
                            {
                                flatSurfaceSemanticTree.Add("None", surfacePath);
                            }
                        }

                        // add material data
                        foreach (var item in materialReferenceNames)
                        {
                            if (geoObject.hasMaterialData())
                            {
                                var materialCollection = geoObject.getSurfaceMaterialValues();

                                if (materialCollection.ContainsKey(item))
                                {
                                    flatSurfaceSemanticTree.Add(materialCollection[item][surface.getSemanticlValue()].ToString(), surfacePath);
                                }
                                else
                                {
                                    flatSurfaceSemanticTree.Add("None", surfacePath);
                                }
                            }
                            else
                            {
                                flatSurfaceSemanticTree.Add("None", surfacePath);
                            }
                        }

                        surfaceCounter++;
                        boundaryCounter++;
                    }
                }
                objectCounter++;
            }

            DA.SetDataList(0, flatSurfaceList);
            DA.SetDataList(1, surfaceKeyList);
            DA.SetDataTree(2, flatSurfaceSemanticTree);
            DA.SetDataList(3, objectKeyList);
            DA.SetDataTree(4, flatObjectSemanticTree);
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


    public class RhinoGeoReader : GH_Component
    {
        public RhinoGeoReader()
          : base("RhinoCityJSONObject", "RCJObject",
              "Fetches the attributes from an object",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Geometry stored in Rhino document", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometery = new List<Grasshopper.Kernel.Types.IGH_GeometricGoo>();
            DA.GetDataList(0, geometery);

            if (geometery.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geometry input empty");
            }

            var valueCollection = new Grasshopper.DataTree<string>();
            var keyList = new List<string>();
            var rawDict = new List<Dictionary<string, string>>();

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            var l = new List<string>();
            var b = new List<string>();

            foreach (var geo in geometery)
            {
                Guid id = geo.ReferenceID;

                var obb = activeDoc.Objects.FindId(id);
                var obbAttributes = obb.Attributes;

                System.Collections.Specialized.NameValueCollection keyValues = obbAttributes.GetUserStrings();
                var localDict = new Dictionary<string, string>();

                foreach (var key in keyValues.AllKeys)
                {
                    if (!keyList.Contains(key))
                    {
                        keyList.Add(key);
                    }

                    localDict.Add(key, obbAttributes.GetUserString(key));
                }
                rawDict.Add(localDict);
            }

            int counter = 0;
            foreach (var surfacesemantic in rawDict)
            {
                var nPath = new Grasshopper.Kernel.Data.GH_Path(counter);
                foreach (var key in keyList)
                {
                    if (key == "Object Name")
                    {
                        valueCollection.Add(surfacesemantic[key] + "_LoD_" + surfacesemantic["Geometry LoD"], nPath);
                    }
                    else if (surfacesemantic.ContainsKey(key))
                    {
                        valueCollection.Add(surfacesemantic[key], nPath);
                    }
                    else
                    {
                        valueCollection.Add("None", nPath);
                    }
                }
                counter++;
            }

            DA.SetDataList(0, keyList);
            DA.SetDataTree(1, valueCollection);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.rreadericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a2754e9"); }
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
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.tree);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.list);
            pManager.AddTextParameter("Merged Surface Info Keys", "mSiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Merged Surface Info Values", "mSiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sKeys = new List<string>();
            var siTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var bKeys = new List<string>();
            var biTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var geoList = new List<Brep>();

            DA.GetDataList(0, geoList);
            DA.GetDataList(1, sKeys);
            DA.GetDataTree(2, out siTree);
            DA.GetDataList(3, bKeys);
            DA.GetDataTree(4, out biTree);

            if (bKeys.Count > 0 && biTree.DataCount == 0 ||
                bKeys.Count == 0 && biTree.DataCount > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Object info input is re1uired to be either both null, or both filled");
                return;
            }

            bool bFilter = true; // TODO make function with only surface data input
            if (bKeys.Count <= 0)
            {
                bFilter = false;
            }

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
                else // dub keys have to be removed
                {
                    ignoreBool.Add(true);
                }
            }

            // costruct a new value List
            var valueCollection = new Grasshopper.DataTree<string>();

            var sBranchCollection = siTree.Branches;
            var bBranchCollection = biTree.Branches;

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

            // Find all names and surface data
            for (int k = 0; k < sKeys.Count; k++)
            {
                for (int i = 0; i < sBranchCollection.Count; i++)
                {
                    if (bFilter)
                    {
                        if (bBranchDict.ContainsKey(sBranchCollection[i][nameIdx].ToString()))
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
                    else
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
            }

            var geoIdx = new System.Collections.Concurrent.ConcurrentBag<int>();
            Parallel.For(0, sBranchCollection.Count, i =>
            {
                var currentBranch = sBranchCollection[i];
                string branchBuildingName = currentBranch[nameIdx].ToString();
                var nPath = new Grasshopper.Kernel.Data.GH_Path(i);
                if (bFilter)
                {
                    if (bBranchDict.ContainsKey(branchBuildingName))
                    {
                        var stringPath = siTree.get_Path(i).ToString();
                        geoIdx.Add(int.Parse(stringPath.Substring(1, stringPath.Length - 2)));
                        for (int k = 1; k < bKeys.Count; k++)
                        {
                            if (!ignoreBool[k])
                            {
                                valueCollection.Add(bBranchDict[branchBuildingName][k - 1], nPath);
                            }
                        }
                    }
                }
                else
                {
                    var stringPath = siTree.get_Path(i).ToString();
                    geoIdx.Add(int.Parse(stringPath.Substring(1, stringPath.Length - 2)));
                }
            });

            var geoIdxList = geoIdx.ToList<int>();
            geoIdxList.Sort();
            var newGeoList = new List<Brep>();

            for (int i = 0; i < geoIdxList.Count; i++)
            {
                newGeoList.Add(geoList[geoIdxList[i]]);
            }
            DA.SetDataList(0, newGeoList);


            DA.SetDataList(1, keyList);
            DA.SetDataTree(2, valueCollection);
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
            //pManager.AddGenericParameter("Document Info", "Di", "Information related to the document (metadata, materials and textures", GH_ParamAccess.list);
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
                if (keyList[i].ToLower() == "geometry lod")
                {
                    lodIdx = i;
                }
                else if (keyList[i].ToLower() == "object type")
                {
                    typeIdx = i;
                }
                else if (keyList[i].ToLower() == "object name")
                {
                    nameIdx = i;
                }
                else if (keyList[i].ToLower() == "surface type")
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
                        //fullName = fullName.Substring(0, fullName.Length - BakerySupport.getPopLength(branchCollection[i][lodIdx].ToString())); 
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
              "Filters information based on a key/value pair",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Info Keys", "iK", "Keys of the information output", GH_ParamAccess.list);
            pManager.AddGenericParameter("Info Values", "iV", "Values of the information output", GH_ParamAccess.tree);
            pManager.AddTextParameter("Filter Info Key", "Fik", "Keys of the Semantic information which is used to filter on", GH_ParamAccess.list);
            pManager.AddTextParameter("Filter Info Value(s)", "FiV", "Value(s) of the semantic information  which is used to filter on", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Equals/ Not Equals", "==", "Booleans that dictates if the value should be equal, or not equal to filter input value", GH_ParamAccess.item, true);

            pManager[2].Optional = true; // origin is optional
            pManager[3].Optional = true; // origin is optional

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Filtered Info Values", "FiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
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

            var tempList = new List<string>();
            foreach (string valueString in keyStrings)
            {
                tempList.Add(valueString + "*");
            }

            foreach (string valueString in tempList)
            {
                keyStrings.Add(valueString);
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