using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace RhinoCityJSON.Components
{
    public class RhinoGeoReader : GH_Component
    {
        public RhinoGeoReader()
          : base("RhinoCityJSONObject", "RCJObject",
              "Fetches the attributes from a rhino geo object",
              "RhinoCityJSON", DefaultValues.defaultReaderFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Geometry stored in Rhino document", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Merged Surface Information", "mSi", "Merged and filtered information output related to the surfaces", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometery = new List<Grasshopper.Kernel.Types.IGH_GeometricGoo>();
            DA.GetDataList(0, geometery);

            if (geometery.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noGeoFound]);
            }

            List<Types.GHObjectInfo> mergedSurfaceInfo = new List<Types.GHObjectInfo>();

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            foreach (var surface in geometery)
            {
                var currentSurfaceObjectInfo = new Types.ObjectInfo();
                currentSurfaceObjectInfo.setIsObject(true);
                currentSurfaceObjectInfo.setIsSurface(true);

                // get the attributes
                Guid id = surface.ReferenceID;
                var obb = activeDoc.Objects.FindId(id);
                var obbAttributes = obb.Attributes;
                System.Collections.Specialized.NameValueCollection userKeyStringList = obbAttributes.GetUserStrings();

                foreach (var userKeyString in userKeyStringList.AllKeys)
                {
                    string userValueString = obbAttributes.GetUserString(userKeyString);

                    if (userKeyString == "Object Name")
                    {
                        currentSurfaceObjectInfo.setName(userValueString);
                        continue;
                    } 
                    if (userKeyString == "File Source")
                    {
                        currentSurfaceObjectInfo.setOriginalFileName(userValueString);
                        continue;
                    }
                    if (userKeyString == "Geometry Super Name")
                    {
                        currentSurfaceObjectInfo.setSuperName(userValueString);
                        continue;
                    }
                    if (userKeyString == "Geometry Name")
                    {
                        currentSurfaceObjectInfo.setGeoName(userValueString);
                        continue;
                    }
                    if (userKeyString == "Geometry Type")
                    {
                        currentSurfaceObjectInfo.setGeoType(userValueString);
                        continue;
                    }
                    if (userKeyString == "LoD")
                    {
                        currentSurfaceObjectInfo.setLod(userValueString);
                        continue;
                    }
                    if (userKeyString == "Object Type")
                    {
                        currentSurfaceObjectInfo.setObjectType(userValueString);
                        continue;
                    }
                    if (userKeyString == "Parents")
                    {
                        currentSurfaceObjectInfo.setParents(new List<string>(userValueString.Split(',')));
                        continue;
                    }
                    if (userKeyString == "Children")
                    {
                        currentSurfaceObjectInfo.setChildren(new List<string>(userValueString.Split(',')));
                        continue;
                    }
                    else
                    {
                        currentSurfaceObjectInfo.addOtherData(userKeyString, userValueString);
                    }
                }
                mergedSurfaceInfo.Add(new Types.GHObjectInfo(currentSurfaceObjectInfo));
            }
            DA.SetDataList(0, mergedSurfaceInfo);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.rreadericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a2754e9"); }
        }

    }
}
