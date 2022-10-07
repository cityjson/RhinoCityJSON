using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace RhinoCityJSON
{
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

            // TODO put in function

            // get scalers
            double scaleX = Jcity.transform.scale[0];
            double scaleY = Jcity.transform.scale[1];
            double scaleZ = Jcity.transform.scale[2];

            DA.SetData(1, scaleZ);

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

            List<Rhino.Geometry.Brep> brepList = new List<Rhino.Geometry.Brep>();

            // create surfaces
            foreach (var objectGroup in Jcity.CityObjects)
            {
                foreach(var cObject in objectGroup)
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
                            foreach (var solid in boundaryGroup.boundaries)
                            {
                                foreach (var surface in solid)
                                {
                                    foreach (var brep in getBrepSurface(surface, vertList))
                                    {
                                        brepList.Add(brep);
                                    }
                                }
                            }
                        }
                        else if (boundaryGroup.type == "CompositeSolid" || boundaryGroup.type == "MultiSolid")
                        {
                            foreach(var composit in boundaryGroup.boundaries)
                            {
                                foreach (var solid in composit)
                                {
                                    foreach (var surface in solid)
                                    {
                                        foreach (var brep in getBrepSurface(surface, vertList))
                                        {
                                            brepList.Add(brep);
                                        }
                                    }
                                }
                            }  
                        }
                        else
                        {
                            foreach (var surface in boundaryGroup.boundaries)
                            {
                                foreach (var brep in getBrepSurface(surface, vertList))
                                {
                                    brepList.Add(brep);
                                }
                            }

                                
                        }                         
                    }
                }
            }
            if (brepList.Count > 0)
            {
                DA.SetDataList(0, brepList);
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
