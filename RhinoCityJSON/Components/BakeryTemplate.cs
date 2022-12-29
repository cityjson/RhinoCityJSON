using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class BakeryTemplate : GH_Component
    {
        public BakeryTemplate()
          : base("TemplateBakery", "TBakery",
              "Bakes the template data to Rhino",
              "RhinoCityJSON", "Baking")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Template Geometry", "TG", "Geometry Input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "TSiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "TSiV", "Semantic information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddTextParameter("Object Info Keys", "TOiK", "Keys of the information output related to the Objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info", "TOiV", "Semantic information output related to the Objects", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sKeys = new List<string>();
            var siTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var bKeys = new List<string>();
            var biTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var geoList = new List<Brep>();
            bool boolOn = false;

            DA.GetDataList(0, geoList);
            DA.GetDataList(1, sKeys);
            DA.GetDataTree(2, out siTree);
            DA.GetDataList(3, bKeys);
            DA.GetDataTree(4, out biTree);
            DA.GetData(5, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            if (bKeys.Count > 0 && biTree.DataCount == 0 ||
                bKeys.Count == 0 && biTree.DataCount > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.unevenFilterInput]);
                return;
            }

            // find locations of crucial data
            int tempIdxTempIdx = -1;
            int tempIdxObIdx = -1;
            int anchorIdx = -1;
            int nameIdx = -1;
            for (int i = 0; i < sKeys.Count; i++)
            {
                if (sKeys[i] == "Template Idx")
                {
                    tempIdxTempIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i] == "Template Idx")
                {
                    tempIdxObIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i] == "Object Anchor")
                {
                    anchorIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i] == "Object Name")
                {
                    nameIdx = i;
                }
            }

            if (tempIdxTempIdx == -1 || tempIdxObIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Template Idx data can not be found");
                return;
            }

            if (anchorIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Template Anchor data can not be found");
                return;
            }

            if (nameIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No object names can be found");
                return;
            }

            // copy geometry 
            var obBranchCollection = biTree.Branches;
            var surfBranchCollection = siTree.Branches;
            var newGeoList = new List<Brep>();
            var surfValueCollection = new Grasshopper.DataTree<string>();
            var obValueCollection = new Grasshopper.DataTree<string>();
            int offset = 0;

            // get unique template indxList
            List<string> uniqueIdx = new List<string>();
            foreach (var item in surfBranchCollection)
            {
                uniqueIdx.Add(item[tempIdxTempIdx].ToString());
            }
            uniqueIdx = uniqueIdx.Distinct().ToList();

            foreach (string tempIdx in uniqueIdx)
            {
                Point3d blockAnchor = new Point3d();
                bool setAnchor = false;
                List<Brep> blockTemplateList = new List<Brep>();
                List<Rhino.DocObjects.ObjectAttributes> attributeList = new List<Rhino.DocObjects.ObjectAttributes>();
                for (int i = 0; i < surfBranchCollection.Count; i++)
                {
                    if (surfBranchCollection[i][tempIdxTempIdx].ToString() == tempIdx)
                    {
                        blockTemplateList.Add(geoList[i]);
                        Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                        objectAttributes.Name = "Template " + tempIdx;

                        for (int j = 0; j < sKeys.Count; j++)
                        { objectAttributes.SetUserString(sKeys[j], surfBranchCollection[i][j].ToString());}
                        attributeList.Add(objectAttributes);

                    }
                }
                int instanceIdx = Rhino.RhinoDoc.ActiveDoc.InstanceDefinitions.Add("Template " + tempIdx, "test", writerSupport.getAnchorPoint(blockTemplateList), blockTemplateList, attributeList);

                foreach (var objectSem in obBranchCollection)
                {
                    if (objectSem[tempIdxObIdx].ToString() == tempIdx)
                    {
                        Point3d location = new Point3d();
                        Point3d.TryParse(objectSem[anchorIdx].ToString(), out location);

                        Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                        objectAttributes.Name = objectSem[nameIdx].ToString();

                        for (int j = 0; j < bKeys.Count; j++)
                        { objectAttributes.SetUserString(bKeys[j], objectSem[j].ToString()); }

                        Rhino.RhinoDoc.ActiveDoc.Objects.AddInstanceObject(instanceIdx, Transform.Translation(location.X, location.Y, location.Z), objectAttributes); 
                    }
                }
            }

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.templatebakeryicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a284e18"); }
        }
    }
}
