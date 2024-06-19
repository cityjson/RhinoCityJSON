using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace RhinoCityJSON.Components
{
    public class attributePainter : GH_Component
    {
        public attributePainter()
          : base("Attribute Painter", "AP",
              "generates color output to color attributes with the preview component",
              "RhinoCityJSON", DefaultValues.defaultManagerFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Surface Info", "I", "Information to be displayed", GH_ParamAccess.list);
            pManager.AddColourParameter("Color", "C", "Colors to be used for display", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Display Color", "DC", "Display color", GH_ParamAccess.list);
        }

        Color interColor(Color c1, Color c2, double value)
        {
            int red = (int) Math.Floor(c1.R + ((c2.R - c1.R) * value));
            int green = (int)Math.Floor(c1.R + ((c2.G - c1.G) * value));
            int blue = (int)Math.Floor(c1.B + ((c2.B - c1.B) * value));
            return Color.FromArgb(red, green, blue);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> infoList = new List<Types.GHObjectInfo>();
            List<Color> colorList = new List<Color>();

            DA.GetDataList(0, infoList);
            DA.GetDataList(1, colorList);

            if (infoList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noData]);
                return;
            }
            if (colorList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noColorData]);
                return;
            }
            if (colorList.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.fewColorData]);
            }

        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.paintAttribute;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-77ae-4eb3-efb3-f76e8a274e90"); }
        }
    }
}