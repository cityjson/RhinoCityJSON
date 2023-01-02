using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class Bakery : GH_Component
    {
        public Bakery()
          : base("Bakery", "Baking",
              "Bakes the RCJ data to Rhino",
              "RhinoCityJSON", "Baking")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry Input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "SiV", "Semantic information output related to the surfaces", GH_ParamAccess.tree);
            //pManager.AddGenericParameter("Document Info", "Di", "Information related to the document (metadata, materials and textures", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            var keyList = new List<string>();
            var brepList = new List<Brep>();

            DA.GetData(3, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            DA.GetDataList(0, brepList);
            DA.GetDataList(1, keyList);
            DA.GetDataTree(2, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);

            if (brepList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No geo input supplied");
                return;
            }

            if (brepList[0] == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No geo input supplied");
                return;
            }

            if (siTree.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface information input supplied");
                return;
            }

            var lodList = new List<string>();
            var lodTypeDictionary = new Dictionary<string, List<string>>();
            var lodSurfTypeDictionary = new Dictionary<string, List<string>>();

            IList<List<Grasshopper.Kernel.Types.IGH_Goo>> branchCollection = siTree.Branches;

            if (branchCollection.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface information input supplied");
                return;
            }
            if (brepList.Count != branchCollection.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geo and Surface information input do not comply");
                return;
            }

            // get LoD and typelist
            int lodIdx = -1;
            int typeIdx = -1;
            int nameIdx = -1;
            int surfTypeIdx = -1;
            for (int i = 0; i < keyList.Count; i++)
            {
                if (keyList[i].ToLower() == "geometry lod")
                {
                    lodIdx = i;
                }
                else if (keyList[i].ToLower() == "object type")
                {
                    typeIdx = i;
                }
                else if (keyList[i].ToLower() == "object name")
                {
                    nameIdx = i;
                }
                else if (keyList[i].ToLower() == "surface type")
                {
                    surfTypeIdx = i;
                }

            }

            if (lodIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No LoD data is supplied");
                return;
            }

            if (typeIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Object type data is supplied");
                return;
            }

            if (nameIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Object name data is supplied");
                return;
            }

            if (surfTypeIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface type data is supplied");
            }

            // create layers
            var lodId = new Dictionary<string, System.Guid>();
            var typId = new Dictionary<string, Dictionary<string, int>>();
            var surId = new Dictionary<string, Dictionary<string, int>>();

            BakerySupport.createLayers(
                "RCJ output",
                lodIdx,
                typeIdx,
                surfTypeIdx,
                branchCollection,
                ref lodId,
                ref typId,
                ref surId
                );

            // bake geo
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            var groupName = branchCollection[0][nameIdx].ToString() + branchCollection[0][lodIdx].ToString();
            activeDoc.Groups.Add("LoD: " + branchCollection[0][lodIdx].ToString() + " - " + branchCollection[0][nameIdx].ToString());
            var groupId = activeDoc.Groups.Add(groupName);
            activeDoc.Groups.FindIndex(groupId).Name = groupName;

            var potetialGroupList = new List<System.Guid>();

            for (int i = 0; i < branchCollection.Count; i++)
            {
                if (groupName != branchCollection[i][nameIdx].ToString() + branchCollection[i][lodIdx].ToString())
                {
                    if (potetialGroupList.Count > 1)
                    {
                        foreach (var groupItem in potetialGroupList)
                        {
                            activeDoc.Groups.AddToGroup(groupId, groupItem);
                        }
                    }
                    potetialGroupList.Clear();

                    groupName = branchCollection[i][nameIdx].ToString() + branchCollection[i][lodIdx].ToString();
                    groupId = activeDoc.Groups.Add("LoD: " + branchCollection[i][lodIdx].ToString() + " - " + branchCollection[i][nameIdx].ToString());
                }

                var targetBrep = brepList[i];
                string lod = branchCollection[i][lodIdx].ToString();
                string bType = BakerySupport.getParentName(branchCollection[i][typeIdx].ToString());

                string sType = "None";
                if (surfTypeIdx != -1)
                {
                    sType = branchCollection[i][surfTypeIdx].ToString();
                }

                Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                objectAttributes.Name = branchCollection[i][nameIdx].ToString() + " - " + i;

                for (int j = 0; j < branchCollection[i].Count; j++)
                {
                    string fullName = branchCollection[i][j].ToString();
                    if (j == 0)
                    {
                        //fullName = fullName.Substring(0, fullName.Length - BakerySupport.getPopLength(branchCollection[i][lodIdx].ToString())); 
                    }

                    objectAttributes.SetUserString(keyList[j], fullName);
                }

                if (bType != "Building")
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else if (sType == "None" || surfTypeIdx == -1)
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else
                {
                    objectAttributes.LayerIndex = surId[lod][sType];
                }

                potetialGroupList.Add(activeDoc.Objects.AddBrep(targetBrep, objectAttributes));
            }

            // bake final group
            if (potetialGroupList.Count > 1)
            {
                foreach (var groupItem in potetialGroupList)
                {
                    activeDoc.Groups.AddToGroup(groupId, groupItem);
                }
            }
            potetialGroupList.Clear();
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.bakeryicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e18"); }
        }
    }
}
