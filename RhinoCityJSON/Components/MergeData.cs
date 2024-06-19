using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class MergeData : GH_Component
    {
        public MergeData()
          : base("Merge Data", "MergeDXF",
              "Gets building object attributes from the layer in which the source geometry is placed and merge into information object, usefull for when using DXF inmports from QGIS",
              "RhinoCityJSON", DefaultValues.defaultProcessingFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Source Geometry", "SG", "Geometry from rhino with additional information", GH_ParamAccess.list);
            pManager.AddGenericParameter("Surface Info", "Si", "information output related to the surfaces", GH_ParamAccess.list);
            pManager.AddGenericParameter("Object Info", "Oi", "information output related to the objects", GH_ParamAccess.list);
            pManager.AddTextParameter("Geometry ID", "Gid", "The ID input related to the geometry", GH_ParamAccess.list);
            pManager.AddTextParameter("Attribute Name", "Na", "Name of the attribute", GH_ParamAccess.list);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object Information", "Oi", "Information related to the Objects", GH_ParamAccess.item);
            pManager.AddGeometryParameter("t", "tt", "Information related to the Objects", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometeryIdList = new List<string>();
            var convertedGeometry = new List<Brep>();
            List<Types.GHObjectInfo> surfaceInfo = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> buildinInfo = new List<Types.GHObjectInfo>();
            List<string> attributeNameList = new List<string>();

            DA.GetDataList(0, convertedGeometry);
            DA.GetDataList(1, surfaceInfo);
            DA.GetDataList(2, buildinInfo);
            DA.GetDataList(3, geometeryIdList);
            DA.GetDataList(4, attributeNameList);

            List<Types.GHObjectInfo> buildingInfoNewList = new List<Types.GHObjectInfo>();

            //get the filteridx 
            for (int i = 0; i < buildinInfo.Count(); i++)
            {
                buildingInfoNewList.Add(new Types.GHObjectInfo(buildinInfo[i]));
            }

            if (geometeryIdList.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noGeoFound]);
            }
            if (convertedGeometry.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noGeoFound]);
            }

            if (attributeNameList.Count == 0)
            {
                //TODO: add error
                return;
            }

            if (attributeNameList.Count > 1)
            {
                //TODO: add error
                return;
            }


            string attributeName = attributeNameList[0];

            Dictionary<Point3d, List<string>> point2DataDict = new Dictionary<Point3d, List<string>>();

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            foreach (var geometeryIdString in geometeryIdList)
            {
                // get the geometry center
                Guid geometeryId = new Guid(geometeryIdString);
                var obb = activeDoc.Objects.FindId(geometeryId);
                var obbGeo = obb.Geometry;

                var test = obbGeo.GetBoundingBox(true);
                Point3d bBoxCenter = test.Center;

                // get the layer name of the geom
                var obbAttributes = obb.Attributes;
                Rhino.DocObjects.Layer obbLayer = activeDoc.Layers.FindIndex(obbAttributes.LayerIndex);
                string layerName = obbLayer.Name.ToString();

                List<string> nameList = new List<string>();
                nameList.Add(layerName);

                if (point2DataDict.ContainsKey(bBoxCenter))
                {
                    //TODO: add warning
                    continue;
                }
                point2DataDict.Add(bBoxCenter, nameList);
            }

            List<string> processedObjects = new List<string>();

            for (int i = 0; i < surfaceInfo.Count; i++)
            {
                bool isFound = false;

                Types.GHObjectInfo currentInfoObject = surfaceInfo[i];
                string superName = currentInfoObject.Value.getName();

                if (processedObjects.Contains(superName))  { continue; }

                List<string> surfaceTypeList = currentInfoObject.Value.getItemByName("Surface type");

              

                bool willEval = false;

                if (currentInfoObject.Value.getLod() == "0" ||
                    currentInfoObject.Value.getLod() == "0.0" ||
                    currentInfoObject.Value.getLod() == "0,0"
                   )
                {
                    willEval = true;
                }

                if (!willEval)
                {
                    if (surfaceTypeList.Count == 0) { continue; }
                    if (surfaceTypeList[0] != "GroundSurface") { continue; }
                }

                processedObjects.Add(superName);
                Point3d bbCenter = convertedGeometry[i].GetBoundingBox(true).Center;

                foreach (KeyValuePair<Point3d, List<string>> pointNamePair in point2DataDict)
                {
                    Point3d otherCenter = pointNamePair.Key;

                    double distance = Math.Sqrt(
                        Math.Pow(bbCenter.X - otherCenter.X, 2) +
                        Math.Pow(bbCenter.Y - otherCenter.Y, 2)
                    );
                    if (distance > 0.2)
                    {
                        continue;
                    }

                    foreach (Types.GHObjectInfo buildinInfoNew in buildingInfoNewList)
                    {
                        if (buildinInfoNew.Value.getName() == superName)
                        {
                            buildinInfoNew.Value.addOtherData(attributeName, pointNamePair.Value[0]);
                        }


                    }
                    isFound = true;
                    break;
                }

                if (!isFound)
                {
                    foreach (Types.GHObjectInfo buildinInfoNew in buildingInfoNewList)
                    {
                        if (buildinInfoNew.Value.getName() == superName)
                        {
                            buildinInfoNew.Value.addOtherData(attributeName, DefaultValues.defaultNoneValue);
                        }
                    }
                }
            }
            DA.SetDataList(0, buildingInfoNewList);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.mergeData;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb6-ceb1-f71e8a275e15"); }
        }

    }
}
