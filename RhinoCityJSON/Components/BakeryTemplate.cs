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
          : base("Template Bakery", "TBakery",
              "Bakes the template data to Rhino",
              "RhinoCityJSON", DefaultValues.defaultbakingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Information", "Si", "Information related to the template surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Information", "Oi", "Information related to the templated Objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Materials", "M", "Material information", GH_ParamAccess.list);
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
            bool boolOn = false;
            List<Types.GHObjectInfo> surfaceInfoList = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> objectInfoList = new List<Types.GHObjectInfo>();
            var brepList = new List<Brep>();
            var materialList = new List<Types.GHMaterial>();

            DA.GetData(4, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }

            DA.GetDataList(0, brepList);
            DA.GetDataList(1, surfaceInfoList);
            DA.GetDataList(2, objectInfoList);
            DA.GetDataList(3, materialList);

            Dictionary<int, List<Brep>> geometryLookup = new Dictionary<int, List<Brep>>();
            Dictionary<int, List<Types.GHObjectInfo>> dataSurfLookup = new Dictionary<int, List<Types.GHObjectInfo>>();
            Dictionary<int, List<Types.GHObjectInfo>> dataObjLookup = new Dictionary<int, List<Types.GHObjectInfo>>();

            //check if template data
            foreach (var objectInfo in objectInfoList)
            {
                if (objectInfo.Value.isSurface())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all input objects are objects");
                    return;
                }
                if (!objectInfo.Value.isTemplate())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all input is a template object");
                    return;
                }
                if (dataObjLookup.ContainsKey(objectInfo.Value.getTemplateIdx()))
                {
                    dataObjLookup[objectInfo.Value.getTemplateIdx()].Add(objectInfo);
                }
                else
                {
                    dataObjLookup.Add(objectInfo.Value.getTemplateIdx(), new List<Types.GHObjectInfo>());
                    dataObjLookup[objectInfo.Value.getTemplateIdx()].Add(objectInfo);
                }
            }

            for (int i = 0; i < surfaceInfoList.Count; i++)
            {
                var surfaceInfo = surfaceInfoList[i];

                if (!surfaceInfo.Value.isSurface())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all input surfaces are surfaces");
                    return;
                }
                if (!surfaceInfo.Value.isTemplate())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all input is a template surface");
                    return;
                }

                if (dataSurfLookup.ContainsKey(surfaceInfo.Value.getTemplateIdx()))
                {
                    dataSurfLookup[surfaceInfo.Value.getTemplateIdx()].Add(surfaceInfo);
                    geometryLookup[surfaceInfo.Value.getTemplateIdx()].Add(brepList[i]);
                }
                else
                {
                    dataSurfLookup.Add(surfaceInfo.Value.getTemplateIdx(), new List<Types.GHObjectInfo>());
                    geometryLookup.Add(surfaceInfo.Value.getTemplateIdx(), new List<Brep>());
                    dataSurfLookup[surfaceInfo.Value.getTemplateIdx()].Add(surfaceInfo);
                    geometryLookup[surfaceInfo.Value.getTemplateIdx()].Add(brepList[i]);
                }

            }

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            // create layers
            var lodId = new Dictionary<string, System.Guid>();
            var typId = new Dictionary<string, Dictionary<string, int>>();
            var surId = new Dictionary<string, Dictionary<string, int>>();
            var blockId = new Dictionary<string, int>();
            var parentID = activeDoc.Layers.FindIndex(BakerySupport.makeParentLayer("RCJ Template output"));
            var typColor = BakerySupport.getTypeColor();

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

            // find the blocks are already present
            var blockList = Rhino.RhinoDoc.ActiveDoc.InstanceDefinitions.GetList(true);
            foreach (var block in blockList) { blockId.Add(block.Name, block.Index); }

            // get unique Idx numbers
            List<int> uniqueIdx = new List<int>();
            foreach (var item in dataObjLookup)
            {
                if (uniqueIdx.Contains(item.Key)) { continue; }
                uniqueIdx.Add(item.Key);
            }
            foreach (var item in dataSurfLookup)
            {
                if (uniqueIdx.Contains(item.Key)) { continue; }
                uniqueIdx.Add(item.Key);
            }


            foreach (int tempIdx in uniqueIdx)
            {
                List<Brep> blockTemplateList = new List<Brep>();
                List<Rhino.DocObjects.ObjectAttributes> attributeList = new List<Rhino.DocObjects.ObjectAttributes>();

                string templateLoD = "";

                // surface data
                for (int i = 0; i < dataSurfLookup[tempIdx].Count; i++)
                {
                    var surfInfo = dataSurfLookup[tempIdx][i];
                    templateLoD = surfInfo.Value.getLod();
                    blockTemplateList.Add(geometryLookup[tempIdx][i]);

                    Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                    objectAttributes.Name = "Template " + tempIdx;

                    var surfaceValues = surfInfo.Value;
                    objectAttributes.Name = surfaceValues.getName() + " - " + i;
                    objectAttributes.SetUserString("LoD", surfaceValues.getLod());
                    objectAttributes.SetUserString("Geometry Super Name", surfaceValues.getSuperName());
                    objectAttributes.SetUserString("Geometry Name", surfaceValues.getGeoName());
                    objectAttributes.SetUserString("Geometry Type", surfaceValues.getGeoType());

                    /*                       // bind material to object
                                           if (materialNames.Count > 0 && materialIdx.Count > 0)
                                           {
                                               string materialString = surfBranchCollection[i][materialNames[0]].ToString();
                                               if (materialString != DefaultValues.defaultNoneValue)
                                               {
                                                   int materalNum = Int32.Parse(surfBranchCollection[i][materialNames[0]].ToString());
                                                   objectAttributes.MaterialIndex = materialIdx[materalNum];
                                                   objectAttributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
                                               }
                                           }*/

                    attributeList.Add(objectAttributes);
                }

                // check if data is available in the layer index
                if (!lodId.ContainsKey(templateLoD))
                {
                    BakerySupport.createLodLayer(
                        templateLoD,
                        ref lodId,
                        ref typId,
                        ref surId,
                        parentID
                        );
                }

                string blockName = "Template " + tempIdx;

                if (blockId.ContainsKey(blockName))
                {
                    int counter = 0;
                    while (true)
                    {
                        blockName = "Template " + tempIdx + " - " + counter.ToString();

                        if (!blockId.ContainsKey(blockName))
                        {
                            break;
                        }
                        counter++;
                    }
                }

                int instanceIdx = Rhino.RhinoDoc.ActiveDoc.InstanceDefinitions.Add(blockName, "test", BakerySupport.getAnchorPoint(blockTemplateList), blockTemplateList, attributeList);
                blockId.Add(blockName, instanceIdx);

                for (int i = 0; i < dataObjLookup[tempIdx].Count; i++)
                {
                    var objInfo = dataObjLookup[tempIdx][i];
                    string cleanedTemplateType = BakerySupport.getParentName(objInfo.Value.getObjectType());

                    Point3d location = objInfo.Value.getAnchor();
                    Rhino.RhinoApp.WriteLine(location.ToString());
                    Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                    objectAttributes.Name = objInfo.Value.getName();

                    objectAttributes.SetUserString("Object Name", objInfo.Value.getName());
                    objectAttributes.SetUserString("Object Type", objInfo.Value.getObjectType());
                    objectAttributes.SetUserString("LoD", objInfo.Value.getLod());

                    if (objInfo.Value.getParents().Count() > 0)
                    {
                        string combinatedString = "";
                        for (int j = 0; j < objInfo.Value.getParents().Count(); j++)
                        {
                            if (objInfo.Value.getParents().Count() - 1 == j)
                            {
                                combinatedString += objInfo.Value.getParents()[j];
                                continue;
                            }
                            combinatedString += objInfo.Value.getParents()[j] + ", ";
                        }
                        objectAttributes.SetUserString("Parents", combinatedString);
                    }

                    if (objInfo.Value.getChildren().Count() > 0)
                    {
                        string combinatedString = "";
                        for (int j = 0; j < objInfo.Value.getChildren().Count(); j++)
                        {
                            if (objInfo.Value.getChildren().Count() - 1 == j)
                            {
                                combinatedString += objInfo.Value.getChildren()[j];
                                continue;
                            }
                            combinatedString += objInfo.Value.getChildren()[j] + ", ";
                        }
                        objectAttributes.SetUserString("Children", combinatedString);
                    }

                    objectAttributes.SetUserString("File Source", objInfo.Value.getOriginalFileName());

                    foreach (var pair in objInfo.Value.getOtherData())
                    {
                        objectAttributes.SetUserString(pair.Key, pair.Value);
                    }

                    // check if data is available in the layer index
                    if (!typId[templateLoD].ContainsKey(cleanedTemplateType)) // TODO: make function with building surf subs (can test after injector implementation)
                    {
                        Rhino.DocObjects.Layer typeLayer = new Rhino.DocObjects.Layer();
                        typeLayer.Name = cleanedTemplateType;
                        System.Drawing.Color lColor = System.Drawing.Color.DarkRed;

                        if (typColor.ContainsKey(cleanedTemplateType))
                        {
                            lColor = typColor[cleanedTemplateType];
                        }

                        typeLayer.Color = lColor;
                        typeLayer.ParentLayerId = lodId[templateLoD];

                        var idx = activeDoc.Layers.Add(typeLayer);
                        typId[templateLoD].Add(cleanedTemplateType, idx);
                    }

                    objectAttributes.LayerIndex = typId[templateLoD][cleanedTemplateType];
                    Rhino.RhinoDoc.ActiveDoc.Objects.AddInstanceObject(instanceIdx, Transform.Translation(location.X, location.Y, location.Z), objectAttributes);
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
