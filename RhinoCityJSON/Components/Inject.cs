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
              "RhinoCityJSON", "Writing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Source File Path", "SF", "Path at which the file is located that is to be injected", GH_ParamAccess.item);
            pManager.AddTextParameter("Target File Path", "TF", "Path at which the new file is stored", GH_ParamAccess.item);
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Force", "*", "Override existing objects", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string sourcePath = "";
            string targetPath = "";
            var keyList = new List<string>();
            var keyStrings = new List<string>();
            bool force = false;
            var geometryList = new List<Brep>();

            DA.GetData(0, ref sourcePath);
            DA.GetData(1, ref targetPath);
            DA.GetDataList(2, geometryList);
            DA.GetDataList(3, keyList);
            DA.GetDataTree(4, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);
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
            if (keyList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No keylist is supplied");
                return;
            }
            if (siTree.Branches.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No value tree is supplied");
                return;
            }

            var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(sourcePath));
            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;
            double rotationAngle = 0;
            double scaler = ReaderSupport.getDocScaler();

            // construct the vertlist of the sourceFile
            List<Rhino.Geometry.Point3d> sourceVertList = ReaderSupport.getVerts(Jcity, worldOrigin, scaler, rotationAngle, true, translate);

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
