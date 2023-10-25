using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoCityJSON.Components
{
    public class addAtrribute : GH_Component
    {
        public addAtrribute()
          : base("Atribute Add", "Add",
              "Adds attribute to the object info",
              "RhinoCityJSON", DefaultValues.defaultManagerFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Information Objects", "Io", "Information output of a reader object", GH_ParamAccess.list);
            pManager.AddTextParameter("Attribute name", "An", "The name of the new attribute", GH_ParamAccess.list);
            pManager.AddTextParameter("Attribute value", "Av", "The value of the new attributes", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("output Information Objects", "oIo", "The updated information object list", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> values = new List<Types.GHObjectInfo>();
            List<string> attributeNameList = new List<string>();
            //var attributeValeList = new Grasshopper.DataTree<string>();

            DA.GetDataList(0, values);
            DA.GetDataList(1, attributeNameList);
            DA.GetDataTree(2, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String> attributeValeList);

            if (values.Count() != attributeValeList.Branches.Count())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "object count and Attribute value count is not maching");
                return;
            }
            if (attributeNameList.Count > 1)
            {
                return;
            }

            bool isSingular = true;
            foreach (var attributeList in attributeValeList.Branches)
            {
                if (attributeList.Count() > 1)
                {
                    isSingular = false;
                    break;
                }
            }

            string attributeName = attributeNameList[0];

            if (attributeName == "Object Name")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.setName(attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Object Type")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.setObjectType(attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Object Parent")
            {

            }
            else if (attributeName == "Object Child")
            {

            }
            else if (attributeName == "Geometry Type")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.setGeoType(attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Geometry Name")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.setGeoName(attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Geometry LoD")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.setLod(attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Surface Material")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++) { values[j].Value.addMaterial("", attributeValeList.Branches[j][0].ToString()); }
            }
            else if (attributeName == "Template Idx")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                int n;
                bool isNumeric = int.TryParse("123", out n);

                if (!isNumeric)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Template Idx can only be integer");
                    return;
                }

                for (int j = 0; j < values.Count; j++) { values[j].Value.setTemplaceIdx(n); }
            }
            else if (attributeName == "Object Anchor")
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }
                for (int j = 0; j < values.Count; j++)
                {
                    string[] components = attributeValeList.Branches[j][0].ToString().Split(' ');

                    if (components.Length == 3 && double.TryParse(components[0], out double x) && double.TryParse(components[1], out double y) && double.TryParse(components[2], out double z))
                    {
                        Point3d point = new Point3d(x, y, z);
                        values[j].Value.setAnchor(point);
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "point entry required");
                        return;
                    }
                }
            }
            else
            {
                if (!isSingular)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No multiple entries allowed for this attibute");
                    return;
                }

                for (int j = 0; j < values.Count; j++) { values[j].Value.addOtherData(attributeName, attributeValeList.Branches[j][0].ToString()); }
            }

            DA.SetDataList(0, values);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.addAttribute;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-18ae-4eb3-efb3-f76e8a274e18"); }
        }
    }
}