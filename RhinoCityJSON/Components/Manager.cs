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
              "RhinoCityJSON", DefaultValues.defaultProcessingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry input", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "Si", "information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info", "Oi", "information output related to the objects", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.list);
            pManager.AddGenericParameter("Merged Surface Information", "mSi", "Merged and filtered information output related to the surfaces", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> buildinInfo = new List<Types.GHObjectInfo>();
            var geoList = new List<Brep>();

            DA.GetDataList(0, geoList);
            DA.GetDataList(1, surfaceInfo);
            DA.GetDataList(2, buildinInfo);

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
            if (buildinInfo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noBuildingData]);
                return;
            }

            // costruct a new value List
            List<Types.GHObjectInfo> mergedSurfaceInfo = new List<Types.GHObjectInfo>();

            // make building dict
            Dictionary<string, Types.ObjectInfo> buildingDict = new Dictionary<string, Types.ObjectInfo>();

            for (int i = 0; i < buildinInfo.Count; i++) { buildingDict.Add(buildinInfo[i].Value.getName(), buildinInfo[i].Value); }

            for (int i = 0; i < surfaceInfo.Count; i++)
            {
                var sourceSurfaceDataObject = surfaceInfo[i].Value;
                var currentSurfaceDataObject = new Types.ObjectInfo(sourceSurfaceDataObject);

                if (!buildingDict.ContainsKey(currentSurfaceDataObject.getName()))
                {
                    continue;
                }

                var currentBuildingDataObject = buildingDict[currentSurfaceDataObject.getName()];

                currentSurfaceDataObject.setObjectType(currentBuildingDataObject.getObjectType());
                currentSurfaceDataObject.setParents(currentBuildingDataObject.getParents());
                currentSurfaceDataObject.setChildren(currentBuildingDataObject.getChildren());
                currentSurfaceDataObject.setOriginalFileName(currentBuildingDataObject.getOriginalFileName());
                currentSurfaceDataObject.setIsObject(true);

                var otherBuildingData = currentBuildingDataObject.getOtherData();

                foreach (var otherPair in otherBuildingData)
                {
                    currentSurfaceDataObject.addOtherData(otherPair.Key, otherPair.Value);
                }
                mergedSurfaceInfo.Add(new Types.GHObjectInfo(currentSurfaceDataObject));
            }

            // make a geoList
            var newGeoList = new List<Brep>();
            var filterSurface = true;


            Dictionary<string, Types.GHObjectInfo> surfDict = new Dictionary<string, Types.GHObjectInfo>();

            for (int i = 0; i < mergedSurfaceInfo.Count; i++) { surfDict.Add(mergedSurfaceInfo[i].Value.getGeoName(), mergedSurfaceInfo[i]); }

            if (geoList.Count == surfaceInfo.Count)
            {
                filterSurface = false;
            }

            for (int i = 0; i < geoList.Count; i++)
            {

                if (surfDict.ContainsKey(geoList[i].GetUserString("_Geoname")))
                {
                    newGeoList.Add(geoList[i]);
                }
            }
            DA.SetDataList(0, newGeoList);
            DA.SetDataList(1, mergedSurfaceInfo);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.mergeicon;
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
