using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class Manager : GH_Component
    {
        public Manager()
          : base("Information manager", "IManage",
              "Filters the geometry based on the input semanic data and casts the building information to the surface information format to prepare for the bakery",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.tree);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.tree);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.list);
            pManager.AddTextParameter("Merged Surface Info Keys", "mSiK", "Keys of the information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Merged Surface Info Values", "mSiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
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

            bool bFilter = true; // TODO: make function with only surface data input
            if (bKeys.Count <= 0)
            {
                bFilter = false;
            }

            // construct a new key list
            var keyList = new List<string>();
            var ignoreBool = new List<bool>();
            int nameIdx = 0;

            for (int i = 0; i < sKeys.Count; i++)
            {
                keyList.Add(sKeys[i]);
            }

            for (int i = 0; i < bKeys.Count; i++)
            {
                if (bKeys[i] == "Object Name")
                {
                    nameIdx = i;
                }

                if (!keyList.Contains(bKeys[i]) && bKeys[i] != DefaultValues.defaultNoneValue)
                {
                    keyList.Add(bKeys[i]);
                    ignoreBool.Add(false);
                }
                else // dub keys have to be removed
                {
                    ignoreBool.Add(true);
                }
            }

            // costruct a new value List
            var valueCollection = new Grasshopper.DataTree<string>();

            var sBranchCollection = siTree.Branches;
            var bBranchCollection = biTree.Branches;

            // make building dict
            Dictionary<string, List<string>> bBranchDict = new Dictionary<string, List<string>>();

            foreach (var bBranch in bBranchCollection)
            {
                List<string> templist = new List<string>();

                for (int i = 0; i < bBranch.Count; i++)
                {
                    if (i == nameIdx)
                    {
                        continue;
                    }

                    templist.Add(bBranch[i].ToString());
                }
                bBranchDict.Add(bBranch[nameIdx].ToString(), templist);
            }

            // Find all names and surface data
            for (int k = 0; k < sKeys.Count; k++)
            {
                for (int i = 0; i < sBranchCollection.Count; i++)
                {
                    if (bFilter)
                    {
                        if (bBranchDict.ContainsKey(sBranchCollection[i][nameIdx].ToString()))
                        {
                            var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                            for (int j = 0; j < sKeys.Count; j++)
                            {
                                if (keyList[k] == sKeys[j])
                                {
                                    valueCollection.Add(sBranchCollection[i][j].ToString(), nPath);
                                }
                            }
                        }
                    }
                    else
                    {
                        var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                        for (int j = 0; j < sKeys.Count; j++)
                        {
                            if (keyList[k] == sKeys[j])
                            {
                                valueCollection.Add(sBranchCollection[i][j].ToString(), nPath);
                            }
                        }
                    }

                }
            }

            var geoIdx = new System.Collections.Concurrent.ConcurrentBag<int>();
            Parallel.For(0, sBranchCollection.Count, i =>
            {
                var currentBranch = sBranchCollection[i];
                string branchBuildingName = currentBranch[nameIdx].ToString();
                var nPath = new Grasshopper.Kernel.Data.GH_Path(i);
                if (bFilter)
                {
                    if (bBranchDict.ContainsKey(branchBuildingName))
                    {
                        var stringPath = siTree.get_Path(i).ToString();
                        geoIdx.Add(int.Parse(stringPath.Substring(1, stringPath.Length - 2)));
                        for (int k = 1; k < bKeys.Count; k++)
                        {
                            if (!ignoreBool[k])
                            {
                                valueCollection.Add(bBranchDict[branchBuildingName][k - 1], nPath);
                            }
                        }
                    }
                }
                else
                {
                    var stringPath = siTree.get_Path(i).ToString();
                    geoIdx.Add(int.Parse(stringPath.Substring(1, stringPath.Length - 2)));
                }
            });

            var geoIdxList = geoIdx.ToList<int>();
            geoIdxList.Sort();
            var newGeoList = new List<Brep>();

            for (int i = 0; i < geoIdxList.Count; i++)
            {
                newGeoList.Add(geoList[geoIdxList[i]]);
            }
            DA.SetDataList(0, newGeoList);


            DA.SetDataList(1, keyList);
            DA.SetDataTree(2, valueCollection);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.divideicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }
}
