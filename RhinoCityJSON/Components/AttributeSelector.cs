using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoCityJSON.Components
{
    public class AttributeSelector : GH_Component
    {
        public AttributeSelector()
          : base("Attribute Selector", "Selector",
              "Fetches the required information form the object data",
              "RhinoCityJSON", DefaultValues.defaultManagerFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Information Objects", "Io", "The information output of a reader object", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Attribute Name", "An", "Attribute name of which the values are desired", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Attribbutes", "A", "Values related to the desired attribute", GH_ParamAccess.tree);

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
            if (values.Count < 0)
            {
                return;
            }

            var doc = Grasshopper.Instances.ActiveCanvas.Document;

            bool redraw = true;
            Types.ObjectInfo firstItem = values[0].Value;
            Dictionary<int, string> filterLookup = firstItem.fetchIdxDict();
            bool updatedList = false;

            IGH_Param oldSurfaceKeySelector = null;

            if (filterKey.Count > 0)
            {
                // get the connected filter selector
                System.Guid connectedListGuid = this.Params.Input[1].ComponentGuid;
                var firstInput = this.Params.Input[1];
                oldSurfaceKeySelector = firstInput.Sources[0];
                Grasshopper.Kernel.Special.GH_ValueList oldVallist = (Grasshopper.Kernel.Special.GH_ValueList)oldSurfaceKeySelector;

                // check if the connected filter selector is connected to more than one other component
                if (oldVallist.Recipients.Count() > 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Filter selector is allowed to be only connected to one object");
                    return;
                } 

                var valueCollection = oldVallist.ListItems;

                if (oldSurfaceKeySelector.GetType().ToString() != "Grasshopper.Kernel.Special.GH_ValueList") //if not correct type
                {
                    // TODO: make this work
                }
                else if (valueCollection.Count() != filterLookup.Count()) // if object count is not equal to old filter intput
                {
                    var tempList = RhinoCityJSON.FilterSupport.ReplaceValueList(oldVallist, firstItem, filterLookup);
                    this.Params.Input[1].AddSource(tempList, 0);
                    tempList.ExpireSolution(true);
                }
                else // check if all filter inputs are the same as the possible ones
                {
                    for (int i = 0; i < valueCollection.Count(); i++)
                    {
                        if (valueCollection[i].Name.ToString() != filterLookup[i].ToString())
                        {
                            var tempList = RhinoCityJSON.FilterSupport.ReplaceValueList(oldVallist, firstItem, filterLookup);
                            this.Params.Input[1].AddSource(tempList, 0);
                            tempList.ExpireSolution(true);
                            break;
                        }
                    }
                }
            }

            DA.GetDataList(1, filterKey); // Find again if deleted

            if (filterKey.Count == 0)
            {
                updatedList = true;

               var vallist = RhinoCityJSON.FilterSupport.CreateValueList(this, this, firstItem, filterLookup);
                Grasshopper.Instances.ActiveCanvas.Document.AddObject(vallist, false);
                this.Params.Input[1].AddSource(vallist, 0);
                vallist.ExpireSolution(true);
            }
            if (!updatedList)
            {
                // filter process
                var outputTree = new Grasshopper.DataTree<string>();
                for (int i = 0; i < values.Count(); i++)
                {
                    var nPath = new Grasshopper.Kernel.Data.GH_Path(i);
                    List<string> answer = values[i].Value.getItemByIdx(filterKey[0]);
                    foreach (var item in answer)
                    {
                        outputTree.Add(item, nPath);
                    }
                }
                DA.SetDataTree(0, outputTree);
            }
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.keyselectoricon;
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
