using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace RhinoCityJSON
{
    class CJObject
    {
        private string name_ = "None";
        private string lod_ = "None";
        private string parentName_ = "None";

        private string geometryType_ = "None";

        private List<string> surfaceNames_ = new List<string>();
        private List<Rhino.Geometry.Brep> brepList_ = new List<Rhino.Geometry.Brep>();

        public CJObject(string name)
        {
            name_ = name;
        }

        public string getName() { return name_; }
        public void setName(string name) { name_ = name; }
        public string getLod() { return lod_; }
        public void setLod(string lod) { lod_ = lod; }
        public string getParendName() { return parentName_; }
        public void setParendName(string parentName) { parentName_ = parentName; }
        public string getGeometryType() { return geometryType_; }
        public void setGeometryType(string geometryType) { geometryType_ = geometryType; }
        public List<string> getSurfaceNames() { return surfaceNames_; }
        public void setSurfaceNames(List<string> surfaceTypes) { surfaceNames_ = surfaceTypes; }
        public List<Rhino.Geometry.Brep> getBrepList() { return brepList_; }
        public void setBrepList(List<Rhino.Geometry.Brep> brepList) { brepList_ = brepList; }
        public int getBrepCount() { return brepList_.Count; }


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
          : base("RhinoCityJSONReader", "Reader",
              "Construct an Archimedean, or arithmetic, spiral given its radii and number of turns.",
              "RhinoCityJSON", "Reading")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {            
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Translate", "T", "Translate according to CityJSON data", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddTextParameter("Info", "I", "Semantic information output", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            bool boolOn = false;
            if (!DA.GetData(0, ref path)) return;
            DA.GetData(2, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }

            // validate the data and warn the user if invalid data is supplied.
            if (path == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is empty");
                return;
            }
            if (!System.IO.File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid filepath found");
                return;
            }

            // Check if valid CityJSON format
            var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
            if (!CheckValidity(Jcity))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                return;
            }

            // collect semantic data
            List<CJObject> cjObjects = new List<CJObject>();

            // TODO put in function
            List<string> surfacenames = new List<string>();
            List<string> boundaryLODs = new List<string>();
            List<string> surfaceTypes = new List<string>();

            // get scalers
            double scaleX = Jcity.transform.scale[0];
            double scaleY = Jcity.transform.scale[1];
            double scaleZ = Jcity.transform.scale[2];

            // ceate vertlist
            var jsonverts = Jcity.vertices;
            List<Rhino.Geometry.Point3d> vertList = new List<Rhino.Geometry.Point3d>();
            foreach (var jsonvert in jsonverts)
            {
                double x = jsonvert[0];
                double y = jsonvert[1];
                double z = jsonvert[2];
                Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(x * scaleX, y * scaleY, z * scaleZ);
                vertList.Add(vert);                
            }

           

            // create surfaces
            foreach (var objectGroup in Jcity.CityObjects)
            {
                // getObject name
                CJObject obb = new CJObject(objectGroup.Name);


                string objectname = objectGroup.Name; // TODO remove
                foreach (var cObject in objectGroup)
                {
                    if (cObject.geometry == null) // parents
                    {
                        continue;
                    }

                    foreach (var boundaryGroup in cObject.geometry)
                    {
                        // this is all the geometry in one shape with info
                        if (boundaryGroup.type == "Solid")
                        {

                            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();
                            foreach (var solid in boundaryGroup.boundaries)
                            {
                                foreach (var surface in solid)
                                {
                                    foreach (var brep in getBrepSurface(surface, vertList))
                                    {
                                        breps.Add(brep);
                                    }
                                }
                            }

                            obb.setBrepList(breps);
                            obb = fetchSematicData(obb, boundaryGroup);

                        }
                        else if (boundaryGroup.type == "CompositeSolid" || boundaryGroup.type == "MultiSolid")
                        {
                            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();
                            foreach (var composit in boundaryGroup.boundaries)
                            {
                                foreach (var solid in composit)
                                {
                                    foreach (var surface in solid)
                                    {
                                        foreach (var brep in getBrepSurface(surface, vertList))
                                        {
                                            breps.Add(brep);
                                        }
                                    }
                                }
                            }

                            obb.setBrepList(breps);
                            obb = fetchSematicData(obb, boundaryGroup);
                        }
                        else
                        {
                            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();
                            foreach (var surface in boundaryGroup.boundaries)
                            {
                                foreach(var brep in getBrepSurface(surface, vertList))
                                {
                                    breps.Add(brep);
                                }
                            }

                            obb.setBrepList(breps);
                            obb = fetchSematicData(obb, boundaryGroup);

                        }
                    }
                }
                cjObjects.Add(obb);
            }




            if (cjObjects.Count > 0)
            {
                List<Rhino.Geometry.Brep> brepList = new List<Rhino.Geometry.Brep>();
                List<Tuple<string, string, string>> semanticDataList = new List<Tuple<string, string, string>>();
                foreach (CJObject cjObject in cjObjects)
                {
                    foreach (var brep in cjObject.getBrepList())
                    {
                        brepList.Add(brep);
                    }

                    foreach (var name in cjObject.getSurfaceNames())
                    {
                        semanticDataList.Add(Tuple.Create<string, string, string>(cjObject.getName(), cjObject.getLod(), name));
                    }
                }


                DA.SetDataList(0, brepList);
                DA.SetDataList(1, semanticDataList);
                //DA.SetDataList(1, semanticTypes);
            }
        }


        private bool CheckValidity(dynamic file)
        {
            if (file.CityObjects == null || file.type != "CityJSON" || file.version == null || 
                file.transform == null || file.transform.scale == null || file.transform.translate == null || 
                file.vertices == null)
            {
                return false;
            }
            else if (file.version != "1.1")
            {
                return false;
            }
            return true;
        }

        private List<Rhino.Geometry.Brep> getBrepSurface(dynamic surface, dynamic vertList)
        {
            List<Rhino.Geometry.Brep> brepList = new List<Rhino.Geometry.Brep>();

            // this is one complete surface (surface + holes)
            Rhino.Collections.CurveList surfaceCurves = new Rhino.Collections.CurveList();

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
                Rhino.Geometry.Brep[] planarFace = Brep.CreatePlanarBreps(surfaceCurves, 0.25); //TODO monior value
                surfaceCurves.Clear();
                try
                {
                    brepList.Add(planarFace[0]);
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                }
            } 
            return brepList;
        }


        private List<string> getSurfaceTypes(dynamic boundaryGroup)
        {
            List<string> surfacetypes = new List<string>();

            foreach (Newtonsoft.Json.Linq.JObject surfacetype in boundaryGroup.semantics.surfaces)
            {
                surfacetypes.Add(forcefullKeyStringStrip(surfacetype.ToString(Formatting.None)));
            }
            return surfacetypes;
        }

        private List<int> getSematicValues(dynamic boundaryGroup)
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

        private string forcefullKeyStringStrip(string inputString)
        {
            int c = 0;
            string strippedString = "";
            bool valueString = false;

            foreach (char lttr in inputString)
            {
                if (valueString && lttr != '"')
                {
                    strippedString += lttr;
                }

                else if (lttr == '"' && c == 2)
                {
                    if (lttr == '"' && strippedString.Length > 0)
                    {
                        break;
                    }

                    valueString = true;                    
                }
                else if (lttr == '"')
                {
                    c++;
                }
            }

            return strippedString;
        }


        CJObject fetchSematicData(CJObject obb, dynamic boundaryGroup)
        {

            List<string> surfacenames = new List<string>();
            List<string> boundaryLODs = new List<string>();
            List<string> surfaceTypes = new List<string>();

            obb.setLod((string)boundaryGroup.lod);

            bool hasSemanticSurf = true;
            int brepCount = obb.getBrepCount();

            if (boundaryGroup.semantics is null)
            {
                hasSemanticSurf = false;
            }

            // semantic data
            List<string> semanticTypes = new List<string>();
            List<int> semanticValues = new List<int>();

            if (hasSemanticSurf)
            {
                semanticTypes = getSurfaceTypes(boundaryGroup);
                semanticValues = getSematicValues(boundaryGroup);
            }

            if (semanticValues.Count == 0)
            {
                for (int i = 0; i < brepCount; i++)
                {
                    surfaceTypes.Add("none");
                }
            }
            else
            {
                for (int i = 0; i < brepCount; i++)
                {
                    surfaceTypes.Add(semanticTypes[semanticValues[i]]);
                }
            }

            obb.setSurfaceNames(surfaceTypes);

            return obb;
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
                return null;
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
}
