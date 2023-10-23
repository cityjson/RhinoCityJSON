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
            pManager.AddGenericParameter("Surface Info", "Si", "Information related to the template surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info", "Oi", "Information related to the templated objects", GH_ParamAccess.list);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddGenericParameter("Surface Information", "Si", "Information related to the surfaces", GH_ParamAccess.item);
            pManager.AddGenericParameter("Object Information", "Oi", "Information related to the Objects", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Types.GHObjectInfo> surfaceInfoList = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> objectInfoList = new List<Types.GHObjectInfo>();
            var geoList = new List<Brep>();

            DA.GetDataList(0, geoList);
            DA.GetDataList(1, surfaceInfoList);
            DA.GetDataList(2, objectInfoList);

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
            }

            Dictionary<int, List<Brep>> geometryLookup =  new Dictionary<int, List<Brep>>();
            Dictionary<int, List<Types.GHObjectInfo>> dataLookup =  new Dictionary<int, List<Types.GHObjectInfo>>();

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

                if (dataLookup.ContainsKey(surfaceInfo.Value.getTemplateIdx()))
                {
                    dataLookup[surfaceInfo.Value.getTemplateIdx()].Add(surfaceInfo);
                    geometryLookup[surfaceInfo.Value.getTemplateIdx()].Add(geoList[i]);
                }
                else
                {
                    dataLookup.Add(surfaceInfo.Value.getTemplateIdx(), new List<Types.GHObjectInfo>());
                    geometryLookup.Add(surfaceInfo.Value.getTemplateIdx(), new List<Brep>());
                    dataLookup[surfaceInfo.Value.getTemplateIdx()].Add(surfaceInfo);
                    geometryLookup[surfaceInfo.Value.getTemplateIdx()].Add(geoList[i]);
                }
                
            }

            List<Brep> newGeoList = new List<Brep>();
            List<Types.GHObjectInfo> newObjectList = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> newSurfaceList = new List<Types.GHObjectInfo>();

            int uniqueCounter = 0;
            foreach (var objectInfo in objectInfoList)
            {
                if (!dataLookup.ContainsKey(objectInfo.Value.getTemplateIdx())) { continue;  }
                Vector3d translation = new Vector3d(objectInfo.Value.getAnchor());

                var geoLookupValue = geometryLookup[objectInfo.Value.getTemplateIdx()];
                var surfLookupValue = dataLookup[objectInfo.Value.getTemplateIdx()];

                for (int i = 0; i < geoLookupValue.Count; i++)
                {
                    var geoInfo = geoLookupValue[i];
                    var surfInfo = surfLookupValue[i];

                    string surfaceName = objectInfo.Value.getName() + "-" + uniqueCounter;

                    Brep geoInfoCopy = geoInfo.DuplicateBrep();
                    geoInfoCopy.Translate(translation);
                    geoInfoCopy.SetUserString("_Geoname", surfaceName);
                    geoInfoCopy.SetUserString("_ObjName", objectInfo.Value.getName());

                    newGeoList.Add(geoInfoCopy);

                    var objectInfoObject =
                            new Types.ObjectInfo(
                           objectInfo.Value.getName() + "-" + uniqueCounter,
                           surfInfo.Value.getGeoType(),
                           surfInfo.Value.getLod(),
                           "",
                           objectInfo.Value.getName(),
                           objectInfo.Value.getOriginalFileName(),
                           surfInfo.Value.getOtherData()
                           );

                    newSurfaceList.Add(new Types.GHObjectInfo(objectInfoObject));
                    uniqueCounter++;
                }

                newObjectList.Add(new Types.GHObjectInfo(
                    new Types.ObjectInfo(
                    objectInfo.Value.getName(),
                    objectInfo.Value.getObjectType(),
                    objectInfo.Value.getParents(),
                    objectInfo.Value.getChildren(),
                    objectInfo.Value.getOriginalFileName(),
                    objectInfo.Value.getOtherData()
                ))) ;
            }
            DA.SetDataList(0, newGeoList);
            DA.SetDataList(1, newSurfaceList);
            DA.SetDataList(2, newObjectList);
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
