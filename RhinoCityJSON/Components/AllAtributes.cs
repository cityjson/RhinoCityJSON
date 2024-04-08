using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoCityJSON.Components
{
    public class allAtributes : GH_Component
    {
        public allAtributes()
          : base("All Atributes", "AAtt",
              "exposes all the attribues",
              "RhinoCityJSON", DefaultValues.defaultManagerFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Information Objects", "Io", "Information output of a reader object", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Atributes", "A", "Information related to the surfaces", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> values = new List<Types.GHObjectInfo>();
            DA.GetDataList(0, values);
            Types.ObjectInfo firstItem = values[0].Value;
            Dictionary<int, string> filterLookup = firstItem.fetchIdxDict();

            List<string> atributes = new List<string>();
            foreach (KeyValuePair<int, string> flterEntry in filterLookup)
            {
                atributes.Add(flterEntry.Value);
            }

            DA.SetDataList(0, atributes);

        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.attribute;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-77ae-4eb3-efb3-f76e8a274e18"); }
        }
    }
}