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
              "RhinoCityJSON", DefaultValues.defaultbakingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry Input", GH_ParamAccess.list);
            pManager.AddGenericParameter("Merged Surface Info", "mSi", "Semantic information", GH_ParamAccess.list);
            pManager.AddGenericParameter("Materials", "M", "Material information", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Activate", "A", "Activate bakery", GH_ParamAccess.item, false);
           // pManager.AddBooleanParameter("Layer per File", "LF", "Make a new layer cluster per source file", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            //pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            bool splitLayers = false;
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();
            var brepList = new List<Brep>();
            var materialList = new List<Types.GHMaterial>();

            DA.GetData(3, ref boolOn);
            //DA.GetData(4, ref splitLayers);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            DA.GetDataList(0, brepList);
            DA.GetDataList(1, surfaceInfo);
            DA.GetDataList(2, materialList);

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
            if (surfaceInfo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Surface information input supplied");
                return;
            }
            if (!BakerySupport.hasBuildingData(surfaceInfo))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Building data can be detected, make sure you have used the info manager to merge data");
                //return;
            }

            var lodList = new List<string>();
            var lodTypeDictionary = new Dictionary<string, List<string>>();
            var lodSurfTypeDictionary = new Dictionary<string, List<string>>();

            // create layers
            var lodId = new Dictionary<string, System.Guid>();
            var typId = new Dictionary<string, Dictionary<string, int>>();
            var surId = new Dictionary<string, Dictionary<string, int>>();
            splitLayers = false;

            BakerySupport.createLayers(
                "RCJ output",
                surfaceInfo,
                ref lodId,
                ref typId,
                ref surId,
                splitLayers
                );

            // create materials
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            List<int> materialIdx = new List<int>();
            foreach (var materialObject in materialList)
            {
                int dubIdx = activeDoc.Materials.Find(materialObject.Value.getName(), false);            
                if (dubIdx == -1)
                {
                    materialIdx.Add(BakerySupport.createRhinoMaterial(materialObject, activeDoc.Materials));
                    continue;
                }
                materialIdx.Add(dubIdx);
            }

            // bake geo
            var groupName = surfaceInfo[0].Value.getName() + surfaceInfo[0].Value.getLod();
            activeDoc.Groups.Add("LoD: " + surfaceInfo[0].Value.getName() + " - " + surfaceInfo[0].Value.getLod());
            var groupId = activeDoc.Groups.Add(groupName);
            activeDoc.Groups.FindIndex(groupId).Name = groupName;

            var potetialGroupList = new List<System.Guid>();

            for (int i = 0; i < surfaceInfo.Count; i++)
            {
                if (groupName != surfaceInfo[i].Value.getName() + surfaceInfo[i].Value.getLod())
                {
                    if (potetialGroupList.Count > 1)
                    {
                        foreach (var groupItem in potetialGroupList)
                        {
                            activeDoc.Groups.AddToGroup(groupId, groupItem);
                        }
                    }
                    potetialGroupList.Clear();

                    groupName = surfaceInfo[i].Value.getName() + surfaceInfo[i].Value.getLod();
                    groupId = activeDoc.Groups.Add("LoD: " + surfaceInfo[i].Value.getName() + " - " + surfaceInfo[i].Value.getLod());
                }

                var targetBrep = brepList[i];
                string lod = surfaceInfo[i].Value.getLod();
                string bType = BakerySupport.getParentName(surfaceInfo[i].Value.getObjectType());


                string sType = "None";
                if (surfaceInfo[i].Value.getOtherData().ContainsKey("Surface type"))
                {
                    sType = surfaceInfo[i].Value.getOtherData()["Surface type"];
                }

                // create object attributes
                Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                var surfaceValues = surfaceInfo[i].Value;
                objectAttributes.Name = surfaceValues.getName() + " - " + i;
                objectAttributes.SetUserString("Object Name", surfaceValues.getName());
                objectAttributes.SetUserString("Object Type", surfaceValues.getObjectType());
                objectAttributes.SetUserString("LoD", surfaceValues.getLod());
                objectAttributes.SetUserString("Geometry Super Name", surfaceValues.getSuperName());
                objectAttributes.SetUserString("Geometry Name", surfaceValues.getGeoName());
                objectAttributes.SetUserString("Geometry Type", surfaceValues.getGeoType());
                objectAttributes.SetUserString("File Source", surfaceValues.getOriginalFileName());

                if (surfaceInfo[i].Value.getParents().Count() > 0)
                {
                    string combinatedString = "";
                    for (int j = 0; j < surfaceInfo[i].Value.getParents().Count(); j++)
                    {
                        if (surfaceInfo[i].Value.getParents().Count() - 1 == j)
                        {
                            combinatedString += surfaceValues.getParents()[j];
                            continue;
                        }
                        combinatedString += surfaceValues.getParents()[j] + ", ";
                    }
                    objectAttributes.SetUserString("Parents", combinatedString);
                }

                if (surfaceInfo[i].Value.getChildren().Count() > 0)
                {
                    string combinatedString = "";
                    for (int j = 0; j < surfaceInfo[i].Value.getChildren().Count(); j++)
                    {
                        if (surfaceInfo[i].Value.getChildren().Count() - 1 == j)
                        {
                            combinatedString += surfaceValues.getChildren()[j];
                            continue;
                        }
                        combinatedString += surfaceValues.getChildren()[j] + ", ";
                    }
                    objectAttributes.SetUserString("Children", combinatedString);
                }

                if (materialIdx.Count > 0)
                {
                    KeyValuePair<string, string> materialPair = surfaceInfo[i].Value.getMaterial();
                    string materialString = materialPair.Value;
                    if (materialString != DefaultValues.defaultNoneValue)
                    {
                        int materalNum = Int32.Parse(materialString);
                        objectAttributes.MaterialIndex = materialIdx[materalNum];
                        objectAttributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
                    }
                }
                

                var otherData = surfaceInfo[i].Value.getOtherData();

                foreach (var pair in otherData)
                {
                    objectAttributes.SetUserString(pair.Key, pair.Value);
                }

                if (bType != "Building")
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else if (sType == "None")
                {
                    objectAttributes.LayerIndex = typId[lod][bType];
                }
                else if (surId[lod].ContainsKey(sType))
                {
                    objectAttributes.LayerIndex = surId[lod][sType];
                }
                else
                {
                    if (!surId[lod].ContainsKey(sType))
                    {
                        Rhino.DocObjects.Layer otherTypeLayer = new Rhino.DocObjects.Layer();
                        otherTypeLayer.Name = sType;

                        Random rnd = new Random();
                        int aValue = rnd.Next(0, 255);
                        int rValue = rnd.Next(0, 255);
                        int bValue = rnd.Next(0, 255);        

                        otherTypeLayer.Color = System.Drawing.Color.FromArgb(aValue, rValue, bValue);
                        otherTypeLayer.ParentLayerId = activeDoc.Layers.FindIndex(typId[lod]["Building"]).Id;
                        var idx = activeDoc.Layers.Add(otherTypeLayer);
                        surId[lod].Add(sType, idx);
                    }
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
