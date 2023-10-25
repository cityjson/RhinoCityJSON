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
            pManager.AddTextParameter("Source File Path", "SF", "Path at which the file is located that is to be injected", GH_ParamAccess.item);
            pManager.AddTextParameter("Target File Path", "TF", "Path at which the new file is stored", GH_ParamAccess.item);
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "Si", "information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info", "Oi", "information output related to the objects", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Force", "*", "Override existing objects", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            bool boolOn = false;
            DA.GetData(6, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            string sourcePath = "";
            string targetPath = "";
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> buildinInfo = new List<Types.GHObjectInfo>();
            bool force = false;
            var geometryList = new List<Brep>();

            DA.GetData(0, ref sourcePath);
            DA.GetData(1, ref targetPath);
            DA.GetDataList(2, geometryList);
            DA.GetDataList(3, surfaceInfo);
            DA.GetDataList(4, buildinInfo);
            DA.GetData(5, ref force);

            if (sourcePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No source filepath is supplied");
                return;
            }
            if (targetPath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No target filepath is supplied");
                return;
            }
            if (geometryList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No geometry input is supplied");
                return;
            }
            if (surfaceInfo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No surfaceInfo is supplied");
                return;
            }
            if (buildinInfo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No value tree is supplied");
                return;
            }

            // get scale from current session
            double scaler = ReaderSupport.getDocScaler();
            if (scaler == -1)
            {
                scaler = 1;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noScale]);
            }

            // hold translation value
            Vector3d sourceTranslation = new Vector3d(0, 0, 0);

            var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(sourcePath));

            // compute the translation of every object
            var firstTransformationData = Jcity.transform.translate;

            if (firstTransformationData != null)
            {
                sourceTranslation.X = -(double)firstTransformationData[0];
                sourceTranslation.Y = -(double)firstTransformationData[1];
                sourceTranslation.Z = -(double)firstTransformationData[2];
            }

            // get vertices stored in a tile
            List<Rhino.Geometry.Point3d> vertList = ReaderSupport.getVerts(Jcity, sourceTranslation, new Point3d(0, 0, 0), scaler, rotationAngle, translate);





            //TODO: find if object name is in the sourcefile
            //TODO: if found and force = false stop process
            //TODO: if found and force = true remove object from file

            //TODO: fetch the object verts
            //TODO: add verts to file verts if not present and make map of their location

            //TODO: make json geo from the input geo

            //TODO: bind semantic data to each surface
            //TODO: bind sementic data to each object

            //TODO: clean the vert list of unused verts
            //TODO: dump json

            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;
            double rotationAngle = 0;
            double scaler = ReaderSupport.getDocScaler();

            // construct the vertlist of the sourceFile
            //List<Rhino.Geometry.Point3d> sourceVertList = ReaderSupport.getVerts(Jcity, worldOrigin, scaler, rotationAngle, true, translate);

            // construct the verlist of the rhinoGeo
            List<Rhino.Geometry.Point3d> rhinoVertList = writerSupport.getVerts(geometryList);





        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.injecticon;
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
