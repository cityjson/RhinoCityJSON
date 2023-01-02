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
            int lodIdx = -1;
            int typeIdx = -1;
            int surfTypeIdx = -1;

            for (int i = 0; i < sKeys.Count; i++)
            {
                if (sKeys[i].ToLower() == "template idx")
                {
                    tempIdxTempIdx = i;
                }
                else if (sKeys[i].ToLower() == "geometry lod")
                {
                    lodIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i].ToLower() == "template idx")
                {
                    tempIdxObIdx = i;
                }
                else if (bKeys[i].ToLower() == "object anchor")
                {
                    anchorIdx = i;
                }
                else if (bKeys[i].ToLower() == "object name")
                {
                    nameIdx = i;
                }
                else if (bKeys[i].ToLower() == "object type")
                {
                    typeIdx = i;
                }
            }

            if (lodIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.noLod]);
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

            if (typeIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.noBType]);
            }

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            // create layers
            var lodId = new Dictionary<string, System.Guid>();
            var typId = new Dictionary<string, Dictionary<string, int>>();
            var surId = new Dictionary<string, Dictionary<string, int>>();
            var blockId = new Dictionary<string, int>();
            var parentID = activeDoc.Layers.FindIndex(BakerySupport.makeParentLayer("RCJ Template output"));
            var typColor = BakerySupport.getTypeColor();

            // find the blocks are already present
            var blockList = Rhino.RhinoDoc.ActiveDoc.InstanceDefinitions.GetList(true);

            foreach (var block in blockList)
            {
                blockId.Add(block.Name, block.Index);
            }

            // copy geometry 
            var obBranchCollection = biTree.Branches;
            var surfBranchCollection = siTree.Branches;

            // get unique template indxList
            List<string> uniqueIdx = new List<string>();
            foreach (var item in surfBranchCollection)
            {
                uniqueIdx.Add(item[tempIdxTempIdx].ToString());
            }
            uniqueIdx = uniqueIdx.Distinct().ToList();

            foreach (string tempIdx in uniqueIdx)
            {
                List<Brep> blockTemplateList = new List<Brep>();
                List<Rhino.DocObjects.ObjectAttributes> attributeList = new List<Rhino.DocObjects.ObjectAttributes>();

                string templateLoD = "";
                for (int i = 0; i < surfBranchCollection.Count; i++)
                {
                    if (surfBranchCollection[i][tempIdxTempIdx].ToString() == tempIdx)
                    {
                        templateLoD = surfBranchCollection[i][lodIdx].ToString();
                        blockTemplateList.Add(geoList[i]);
                        Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                        objectAttributes.Name = "Template " + tempIdx;

                        for (int j = 0; j < sKeys.Count; j++)
                        {
                            objectAttributes.SetUserString(sKeys[j], surfBranchCollection[i][j].ToString());
                        }
                        attributeList.Add(objectAttributes);

                    }
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

                foreach (var objectSem in obBranchCollection)
                {
                    string cleanedTemplateType = BakerySupport.getParentName(objectSem[typeIdx].ToString());
                    if (objectSem[tempIdxObIdx].ToString() == tempIdx)
                    {
                        Point3d location = new Point3d();
                        Point3d.TryParse(objectSem[anchorIdx].ToString(), out location);

                        Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
                        objectAttributes.Name = objectSem[nameIdx].ToString();

                        for (int j = 0; j < bKeys.Count; j++)
                        { 
                            objectAttributes.SetUserString(
                                bKeys[j], 
                                objectSem[j].ToString()); 
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
