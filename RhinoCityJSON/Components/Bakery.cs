using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

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
            pManager.AddGenericParameter("Materials", "M", "The material information", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            var keyList = new List<string>();
            var brepList = new List<Brep>();
            var materialList = new List<GHMaterial>();

            DA.GetData(4, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            DA.GetDataList(0, brepList);
            DA.GetDataList(1, keyList);
            DA.GetDataTree(2, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);
            DA.GetDataList(3, materialList);

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
            List<int> materialNames = new List<int>();
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
                else if (keyList[i].ToLower().Split(' ')[0] == "surface" && keyList[i].ToLower().Split(' ')[1] == "material")
                {
                    materialNames.Add(i);
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

            // create materials
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            List<int> materialIdx = new List<int>();
            foreach (var materialObject in materialList)
            {
                materialIdx.Add(BakerySupport.createRhinoMaterial(materialObject));
            }

            // bake geo
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

                // create object attributes
                Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                objectAttributes.Name = branchCollection[i][nameIdx].ToString() + " - " + i;

                // bind material to object
                if (materialNames.Count > 0 && materialIdx.Count > 0)
                {
                    string materialString = branchCollection[i][materialNames[0]].ToString();
                    if (materialString != DefaultValues.defaultNoneValue)
                    {
                        int materalNum = Int32.Parse(branchCollection[i][materialNames[0]].ToString());
                        objectAttributes.MaterialIndex = materialIdx[materalNum];
                        objectAttributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
                    }
                }

                for (int j = 0; j < branchCollection[i].Count; j++)
                {
                    string fullName = branchCollection[i][j].ToString();
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
                else if (surId[lod].ContainsKey(sType))
                {
                    objectAttributes.LayerIndex = surId[lod][sType];
                }
                else
                {
                    if (!surId[lod].ContainsKey("Other"))
                    {
                        Rhino.DocObjects.Layer otherTypeLayer = new Rhino.DocObjects.Layer();
                        otherTypeLayer.Name = "Other";
                        otherTypeLayer.Color = System.Drawing.Color.Gray;
                        otherTypeLayer.ParentLayerId = activeDoc.Layers.FindIndex(typId[lod]["Building"]).Id;
                        var idx = activeDoc.Layers.Add(otherTypeLayer);
                        surId[lod].Add("Other", idx);
                    }
                    objectAttributes.LayerIndex = surId[lod]["Other"];
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
