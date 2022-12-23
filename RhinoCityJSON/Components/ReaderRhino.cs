using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noGeoFound]);
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
                        valueCollection.Add(surfacesemantic[key], nPath);
                    }
                    else if (surfacesemantic.ContainsKey(key))
                    {
                        valueCollection.Add(surfacesemantic[key], nPath);
                    }
                    else
                    {
                        valueCollection.Add(DefaultValues.defaultNoneValue, nPath);
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
}
