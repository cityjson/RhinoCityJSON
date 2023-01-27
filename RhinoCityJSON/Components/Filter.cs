using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoCityJSON.Components
{
    public class Filter : GH_Component
    {
        public Filter()
          : base("Filter", "Filter",
              "Filters information based on a key/value pair",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Info Keys", "iK", "Keys of the information output", GH_ParamAccess.list);
            pManager.AddGenericParameter("Info Values", "iV", "Values of the information output", GH_ParamAccess.tree);
            pManager.AddTextParameter("Filter Info Key", "Fik", "Keys of the Semantic information which is used to filter on", GH_ParamAccess.list);
            pManager.AddTextParameter("Filter Info Value(s)", "FiV", "Value(s) of the semantic information  which is used to filter on", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Equals/ Not Equals", "==", "Booleans that dictates if the value should be equal, or not equal to filter input value", GH_ParamAccess.item, true);

            pManager[2].Optional = true;
            pManager[3].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Filtered Info Values", "FiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var keyList = new List<string>();
            var keyFilter = new List<string>();
            var keyStrings = new List<string>();
            bool equals = true;

            DA.GetDataList(0, keyList);
            DA.GetDataTree(1, out Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> siTree);
            DA.GetDataList(2, keyFilter);
            DA.GetDataList(3, keyStrings);
            DA.GetData(4, ref equals);

            if (keyFilter.Count == 0 && keyStrings.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter Key can not be empty when Filter values is not empty");
                return;
            }
            if (keyFilter.Count != 0 && keyStrings.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter values can not be empty when Filter Key is not empty");
                return;
            }
            if (keyFilter.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Can only filter on a single key");
                return;
            }
            if (keyFilter.Count == 0 && keyStrings.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No filter input");
                DA.SetDataTree(0, siTree);
                return;
            }

            if (keyList.Count != siTree.Branches[0].Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Info keys and values do not comply");
                return;
            }

            // get the indx of the filter key
            int keyIdx = -1;

            for (int i = 0; i < keyList.Count; i++)
            {
                if (keyList[i] == keyFilter[0])
                {
                    keyIdx = i;
                }
            }

            if (keyIdx == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filter key can not be found in the info key list");
                return;
            }

            var tempList = new List<string>();
            foreach (string valueString in keyStrings)
            {
                tempList.Add(valueString + "*");
            }

            foreach (string valueString in tempList)
            {
                keyStrings.Add(valueString);
            }

            // find the complying branches
            var dataTree = new Grasshopper.DataTree<string>();
            var branchCollection = siTree.Branches;

            if (equals)
            {
                for (int i = 0; i < branchCollection.Count; i++)
                {
                    for (int j = 0; j < keyStrings.Count; j++)
                    {
                        if (keyStrings[j] == branchCollection[i][keyIdx].ToString())
                        {
                            var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                            foreach (var item in branchCollection[i])
                            {
                                dataTree.Add(item.ToString(), nPath);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < branchCollection.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < keyStrings.Count; j++)
                    {
                        if (keyStrings[j] == branchCollection[i][keyIdx].ToString())
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var nPath = new Grasshopper.Kernel.Data.GH_Path(i);

                        foreach (var item in branchCollection[i])
                        {
                            dataTree.Add(item.ToString(), nPath);
                        }
                    }
                }
            }


            if (dataTree.BranchCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No matching values could be found");
            }

            DA.SetDataTree(0, dataTree);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.filtericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }

    public class Filter2 : GH_Component
    {
        public Filter2()
          : base("Filter2", "Filter2",
              "Filters information based on a key/value pair",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Equals/ Not Equals", "==", "Booleans that dictates if the value should be equal, or not equal to filter input value", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Filter Key", "Fk", "key to filter", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Attribbutes", "A", "Values of the information output related to the surfaces", GH_ParamAccess.tree);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> values = new List<Types.GHObjectInfo>();
            List<int> filterKey = new List<int>();

            DA.GetDataList(0, values);
            DA.GetDataList(1, filterKey);

            if (filterKey.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only a single filter is allowed");
                return;
            }

            Dictionary<int, string> filterLookup = new Dictionary<int, string>();
            bool updatedList = false;

            // update extension line
            if (values.Count > 0 && filterKey.Count == 0)
            {
                updatedList = true;
                if (this.Params.Input[1].Sources.Count != 0)
                {
                    //TODO: check if update is needed
                    //TODO: check if original selected value is present
                    //TODO: preserve origina connections
                }
                else
                {
                    var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
                    vallist.CreateAttributes();
                    vallist.Name = "Filter keys"; //TODO: make smarter
                    vallist.NickName = "Filter:"; //TODO: make smarter
                    vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

                    int inputcount = 2; //TODO: make smarter
                    vallist.Attributes.Pivot = new System.Drawing.PointF((float)this.Attributes.DocObject.Attributes.Bounds.Left - vallist.Attributes.Bounds.Width - 5, (float)this.Params.Input[1].Attributes.Bounds.Y + inputcount * 30);

                    vallist.ListItems.Clear();

                    int filterIdx = 0;
                    Types.ObjectInfo firstItem = values[0].Value;

                    string[] firstItemKeys = firstItem.getOtherData().Keys.ToArray();

                    if (firstItem.isSurface())
                    {
                        for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                        {
                            filterLookup.Add(filterIdx, DefaultValues.surfaceObjectKeys[i]);
                            vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(DefaultValues.surfaceObjectKeys[i], filterIdx.ToString()));
                            filterIdx++;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                        {
                            filterLookup.Add(filterIdx, DefaultValues.objectKeys[i]);
                            vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(DefaultValues.objectKeys[i], filterIdx.ToString()));
                            filterIdx++;
                        }
                    }
                    for (int i = 0; i < firstItemKeys.Length; i++)
                    {
                        filterLookup.Add(filterIdx, firstItemKeys[i]);
                        vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(firstItemKeys[i], filterIdx.ToString()));
                        filterIdx++;
                    }

                    vallist.Description = vallist.ListItems.Count.ToString() + "types were found int the input";
                    Grasshopper.Instances.ActiveCanvas.Document.AddObject(vallist, false);

                    this.Params.Input[1].AddSource(vallist, 0);
                    vallist.ExpireSolution(true);
                }
            }
            if (!updatedList)
            {
                // filter process
                var outputTree = new Grasshopper.DataTree<string>();
                Types.ObjectInfo refItem = values[0].Value;
                bool found = false;

                if (refItem.isSurface())
                {
                    if (filterKey[0] == 0)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getName(), objectPath);
                        }
                    }
                    if (filterKey[0] == 1)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getGeoType(), objectPath);
                        }
                    }
                    if (filterKey[0] == 2)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getGeoName(), objectPath);
                        }
                    }
                    if (filterKey[0] == 3)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getLod(), objectPath);
                        }
                    }
                }
                else
                {
                    if (filterKey[0] == 0)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getName(), objectPath);
                        }
                    }
                    if (filterKey[0] == 1)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                            outputTree.Add(values[i].Value.getObjectType(), objectPath);
                        }
                    }
                    if (filterKey[0] == 2)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);

                            foreach (var parent in values[i].Value.getParents())
                            {
                                outputTree.Add(parent, objectPath);
                            }
                        }
                    }
                    if (filterKey[0] == 3)
                    {
                        found = true;
                        for (int i = 0; i < values.Count(); i++)
                        {
                            var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);

                            foreach (var child in values[i].Value.getChildren())
                            {
                                outputTree.Add(child, objectPath);
                            }
                        }
                    }
                }

                if (!found)
                {
                    int idx = filterKey[0];
                    if (refItem.isSurface())
                    {
                        idx = idx - DefaultValues.surfaceObjectKeysSize;
                    }
                    else
                    {
                        idx = idx - DefaultValues.objectKeysSize;
                    }

                    string[] keys = refItem.getOtherData().Keys.ToArray();
                    string reqKey = keys[idx];

                    for (int i = 0; i < values.Count(); i++)
                    {
                        var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                        outputTree.Add(values[i].Value.getOtherData()[reqKey], objectPath);
                    }

                }

                DA.SetDataTree(0, outputTree);
            }

            

/*            if (filterLookup.Count == 0)
            {
                string[] firstItemKeys = refItem.getOtherData().Keys.ToArray();
                int filterIdx = 0;
                if (refItem.isSurface())
                {
                    for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                    {
                        filterLookup.Add(filterIdx, DefaultValues.surfaceObjectKeys[i]);
                        filterIdx++;
                    }
                }
                else
                {
                    for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                    {
                        filterLookup.Add(filterIdx, DefaultValues.objectKeys[i]);
                        filterIdx++;
                    }
                }
                for (int i = 0; i < firstItemKeys.Length; i++)
                {
                    filterLookup.Add(filterIdx, firstItemKeys[i]);
                    filterIdx++;
                }
            }

            string filterString = filterLookup[filterKey[0]];

            if (refItem.getOtherData().ContainsKey(filterString))
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    var objectPath = new Grasshopper.Kernel.Data.GH_Path(i);
                    outputTree.Add(values[i].Value.getOtherData()[filterString], objectPath);
                }
            }*/



        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.filtericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c9a-18ae-4eb3-efb3-f76e8a274e40"); }
        }
    }

}
