using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class ReaderSettings : GH_Component
    {
        public ReaderSettings()
          : base("Settings Reader", "RSettings",
              "Sets the additional configuration for the Template and Document reader",
              "RhinoCityJSON", DefaultValues.defaultReaderFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Translate", "T", "Translate according to the stored translation vector", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Model origin", "O", "The origin of the model. This coordiante will be set as the {0,0,0} point for the imported JSON", GH_ParamAccess.list);
            pManager.AddNumberParameter("True north", "Tn", "The direction of the true north", GH_ParamAccess.list, 0.0);
            pManager.AddGeometryParameter("Domain", "D", "The domain within objects should be located to be loaded (disabled for Document Reader)", GH_ParamAccess.list);
            pManager.AddTextParameter("LoD", "L", "Desired Lod, keep empty for all (disabled for Document Reader)", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Large files", "Alf", "Allow extremely large files", GH_ParamAccess.item, false);

            pManager[1].Optional = true; // origin is optional
            pManager[3].Optional = true; // origin is optional
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Settings", "S", "Set settings", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool translate = false;
            bool largeFile = false;
            var p = new Rhino.Geometry.Point3d(0, 0, 0);
            bool setP = false;
            var pList = new List<Rhino.Geometry.Point3d>();
            var north = 0.0;
            var northList = new List<double>();
            var loDList = new List<string>();
            var domainList = new List<Rhino.Geometry.Brep>();
            var domain = new Rhino.Geometry.Brep();

            DA.GetData(0, ref translate);
            DA.GetDataList(1, pList);
            DA.GetDataList(2, northList);
            DA.GetDataList(3, domainList);
            DA.GetDataList(4, loDList);
            DA.GetData(5, ref largeFile);

            if (pList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.multipleOrigins]);
                return;
            }
            else if (pList != null && pList.Count == 1)
            {
                setP = true;
                p = pList[0];
            }
            if (northList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.multipleNorth]);
                return;
            }
            else
            {
                north = northList[0];
            }

            if (north < -360 || north > 360)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.oversizedAngle]);
            }
            if (domainList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.multipleDomain]);
            }
            else if (domainList.Count == 1)
            {
                domain = domainList[0];
            }

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    string simpleLoD = lod.Trim();

                    if (simpleLoD == "0" || simpleLoD == "0.0" || simpleLoD == "0.1" || simpleLoD == "0.2" || simpleLoD == "0.3" ||
                        simpleLoD == "1" || simpleLoD == "1.0" || simpleLoD == "1.1" || simpleLoD == "1.2" || simpleLoD == "1.3" ||
                        simpleLoD == "2" || simpleLoD == "2.0" || simpleLoD == "2.1" || simpleLoD == "2.2" || simpleLoD == "2.3" ||
                        simpleLoD == "3" || simpleLoD == "3.0" || simpleLoD == "3.1" || simpleLoD == "3.2" || simpleLoD == "3.3")
                    {
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidLod]);
                        return;
                    }
                }

            }

            Types.GHReaderSettings readerSettings = new Types.GHReaderSettings(
                    new Types.ReaderSettings(
                    translate,
                    p,
                    north,
                    domain,
                    loDList,
                    largeFile
                    )
                );

            DA.SetData(0, readerSettings);
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
}
