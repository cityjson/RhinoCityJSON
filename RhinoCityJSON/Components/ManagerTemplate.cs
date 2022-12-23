using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class Template2Object : GH_Component
    {
        public Template2Object()
          : base("Template2Object", "T2O",
              "Convert template data to normal object data",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Template Geometry", "TG", "Geometry input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.list);
            pManager.AddTextParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sKeys = new List<string>();
            var siTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var bKeys = new List<string>();
            var biTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            var geoList = new List<Brep>();

            DA.GetDataList(0, geoList);
            DA.GetDataList(1, sKeys);
            DA.GetDataTree(2, out siTree);
            DA.GetDataList(3, bKeys);
            DA.GetDataTree(4, out biTree);

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
            for (int i = 0; i < sKeys.Count(); i++)
            {
                if (sKeys[i] == "Template Idx")
                {
                    tempIdxTempIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count(); i++)
            {
                if (bKeys[i] == "Template Idx")
                {
                    tempIdxObIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count(); i++)
            {
                if (bKeys[i] == "Object Anchor")
                {
                    anchorIdx = i;
                }
            }

            for (int i = 0; i < bKeys.Count(); i++)
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

            for (int i = 0; i < obBranchCollection.Count(); i++)
            {
                var obSematics = obBranchCollection[i];

                bool found = false;
                string idxNum = obSematics[tempIdxObIdx].ToString();
                Point3d anchorPoint = new Point3d(0, 0, 0);
                if (!Point3d.TryParse(obSematics[anchorIdx].ToString(), out anchorPoint)) { continue; }

                for (int j = 0; j < surfBranchCollection.Count(); j++)
                {
                    var surfSemantics = surfBranchCollection[j];

                    if (idxNum != surfSemantics[tempIdxTempIdx].ToString()) // TODO: make a dictionary in advance
                    {
                        continue;
                    }

                    found = true;

                    var nPath = new Grasshopper.Kernel.Data.GH_Path(offset);
                    offset++;

                    var surface = geoList[j].DuplicateBrep();
                    surface.Translate(anchorPoint.X, anchorPoint.Y, anchorPoint.Z);
                    newGeoList.Add(surface);

                    surfValueCollection.Add(obSematics[nameIdx].ToString(), nPath);
                    for (int k = 0; k < surfSemantics.Count(); k++)
                    {
                        if (k == tempIdxTempIdx)
                        {
                            continue;
                        }

                        surfValueCollection.Add(surfSemantics[k].ToString(), nPath);
                    }

                }
                if (found)
                {
                    var obPath = new Grasshopper.Kernel.Data.GH_Path(i);
                    for (int j = 0; j < obSematics.Count; j++)
                    {
                        if (j == anchorIdx || j == tempIdxObIdx)
                        {
                            continue;
                        }

                        obValueCollection.Add(obSematics[j].ToString(), obPath);
                    }
                }
            }

            // clean key lists
            sKeys.RemoveAt(tempIdxTempIdx);
            sKeys.Insert(0, "Object Name");

            if (anchorIdx > tempIdxTempIdx)
            {
                bKeys.RemoveAt(anchorIdx);
                bKeys.RemoveAt(tempIdxObIdx);
            }
            else
            {
                bKeys.RemoveAt(tempIdxObIdx);
                bKeys.RemoveAt(anchorIdx);
            }

            DA.SetDataList(0, newGeoList);
            DA.SetDataList(1, sKeys);
            DA.SetDataTree(2, surfValueCollection);
            DA.SetDataList(3, bKeys);
            DA.SetDataTree(4, obValueCollection);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.t2oicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-ceb3-f76e8a275e15"); }
        }

    }
}
