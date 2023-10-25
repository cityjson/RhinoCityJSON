using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class ManagerReversed : GH_Component
    {
        public ManagerReversed()
          : base("Information splitter", "ISplit",
              "Filters the geometry based on the input semanic data and splits the building information from the surface information format",
              "RhinoCityJSON", DefaultValues.defaultProcessingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Merged Surface Information", "mSi", "Merged and filtered information output related to the surfaces", GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddGenericParameter("Surface Information", "Si", "Information related to the surfaces", GH_ParamAccess.item);
            pManager.AddGenericParameter("Object Information", "Oi", "Information related to the Objects", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();
            var geoList = new List<Brep>();
            DA.GetDataList(0, surfaceInfo);

            if (geoList.Count != 0)
            {
                if (geoList[0] == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noGeoFound]);
                    return;
                }

            }
            if (surfaceInfo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noSurfaceData]);
                return;
            }

            Dictionary<string, Types.GHObjectInfo> splitObjectInfoDict = new Dictionary<string, Types.GHObjectInfo>();
            List<Types.GHObjectInfo> splitSurfaceInfo = new List<Types.GHObjectInfo>();

            var newGeoList = new List<Brep>();

            foreach (var item in surfaceInfo)
            {
                Types.ObjectInfo mergedSurfaceData = item.Value;

                Dictionary<string, string> surfaceDictonary = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> otherdata in mergedSurfaceData.getOtherData())
                {
                    if (otherdata.Key.Contains(DefaultValues.defaultObjectAddition))
                    {
                        continue;
                    }
                    surfaceDictonary.Add(otherdata.Key, otherdata.Value);
                }

                Types.ObjectInfo splitsurface = new Types.ObjectInfo(
                        mergedSurfaceData.getGeoName(),
                        mergedSurfaceData.getGeoType(),
                        mergedSurfaceData.getLod(),
                        "",
                        mergedSurfaceData.getName(),
                        mergedSurfaceData.getOriginalFileName(),
                        surfaceDictonary
                    );

                splitsurface.addMaterial(mergedSurfaceData.getMaterial().Key, mergedSurfaceData.getMaterial().Value);
                

                splitSurfaceInfo.Add(
                    new Types.GHObjectInfo(
                        splitsurface
                        ));


                if (splitObjectInfoDict.ContainsKey(mergedSurfaceData.getName()))
                {
                    continue;
                }

                Dictionary<string, string> objectDictonary = new Dictionary<string, string>();

                foreach (KeyValuePair<string,string> otherdata in mergedSurfaceData.getOtherData())
                {
                    if (!otherdata.Key.Contains(DefaultValues.defaultObjectAddition))
                    {
                        continue;
                    }
                    objectDictonary.Add(otherdata.Key, otherdata.Value);
                }

                splitObjectInfoDict.Add(
                    item.Value.getName(),
                    new Types.GHObjectInfo(
                    new Types.ObjectInfo(
                        mergedSurfaceData.getName(),
                        mergedSurfaceData.getObjectType().ToString(),
                        mergedSurfaceData.getParents(),
                        mergedSurfaceData.getChildren(),
                        mergedSurfaceData.getOriginalFileName(),
                        objectDictonary
                )));

            }

            List<Types.GHObjectInfo> splitObjectInfo = new List<Types.GHObjectInfo>();
            foreach (var pair in splitObjectInfoDict)
            {
                splitObjectInfo.Add(pair.Value);
            }

            DA.SetDataList(0, newGeoList);
            DA.SetDataList(1, splitSurfaceInfo);
            DA.SetDataList(2, splitObjectInfo);
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
            get { return new Guid("b2364c3a-18ae-4eb5-aeb3-f76e8a274e40"); }
        }
    }
}
