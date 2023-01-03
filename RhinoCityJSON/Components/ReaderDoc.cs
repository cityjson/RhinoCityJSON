using Grasshopper.Kernel;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace RhinoCityJSON.Components
{
    public class ReaderDoc : GH_Component
    {
        public ReaderDoc()
          : base("Document Reader", "DReader",
              "Fetches the Metadata, Textures and Materials from a CityJSON file, Autoresolves when multiple inputs",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Metadata Keys", "MdK", "Keys of the Metadata stored in the files", GH_ParamAccess.item);
            pManager.AddTextParameter("Metadata Values", "MdV", "Values of the Metadata stored in the files", GH_ParamAccess.tree);
            pManager.AddTextParameter("LoD", "L", "LoD levels", GH_ParamAccess.item);
            pManager.AddGenericParameter("test materials", "tm", "a test", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            List<string> pathList = new List<string>();
            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.offline]);
                return;
            }
            // validate the data and warn the user if invalid data is supplied.
            if (pathList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.emptyPath]);
                return;
            }
            else if (pathList[0] == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.emptyPath]);
                return;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidPath]);
                    return;
                }
            }

            List<GHMaterial> materialList = new List<GHMaterial>();
            List<string> lodLevels = new List<string>();
            var nestedMetaData = new List<Dictionary<string, string>>();

            foreach (var path in pathList)
            {
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                // Check if valid CityJSON format
                dynamic Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                    return;
                }

                // fetch metadata
                if (Jcity.metadata != null)
                {
                    foreach (Newtonsoft.Json.Linq.JProperty metaGroup in Jcity.metadata)
                    {
                        var metaValue = metaGroup.Value;
                        var metaName = metaGroup.Name;

                        if (metaValue.Count() == 0)
                        {
                            metadata.Add(metaName.ToString(), metaValue.ToString());
                        }
                        else
                        {
                            if (metaName.ToString() == "geographicalExtent" && metaValue.Count() == 6)
                            {
                                // create two string points
                                string minPoint = "{" + metaValue[0].ToString() + ", " + metaValue[1].ToString() + ", " + metaValue[2].ToString() + "}";
                                metadata.Add("geographicalExtent minPoint", minPoint);

                                string maxPoint = "{" + metaValue[3].ToString() + ", " + metaValue[4].ToString() + ", " + metaValue[5].ToString() + "}";
                                metadata.Add("geographicalExtent maxPoint", maxPoint);

                            }
                            else
                            {
                                foreach (Newtonsoft.Json.Linq.JProperty nestedMetaValue in metaValue)
                                {
                                    metadata.Add(metaName.ToString() + " " + nestedMetaValue.Name.ToString(), nestedMetaValue.Value.ToString());
                                }
                            }
                        }

                    }
                    nestedMetaData.Add(metadata);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMetaDataFound]);
                }

                // get materials
                var appearance = Jcity.appearance;
                if (appearance != null)
                {
                    var materials = appearance.materials;
                    if (materials != null)
                    {
                        int c = 0;
                        foreach (var material in materials)
                        {
                            var nPath = new Grasshopper.Kernel.Data.GH_Path(c);

                            var ghColor = new Grasshopper.Kernel.Types.GH_Colour();
                            var GH_material = new Grasshopper.Kernel.Types.GH_Material();


                            string name = "";
                            double ambientIntensity = 0;
                            double[] diffuseColor = new double[3] { -1, -1, -1 };
                            double[] emissiveColor = new double[3] { -1, -1, -1 };
                            double[] specularColor = new double[3] { -1, -1, -1 };
                            double shininess = 0;
                            double transparency = 0;
                            bool isSmooth = false;

                            if (material["name"] != null) { name = material["name"]; }
                            if (material["ambientIntensity"] != null) { ambientIntensity = material["ambientIntensity"]; }
                            if (material["diffuseColor"] != null) { diffuseColor =  new double[3]{ material["diffuseColor"][0], material["diffuseColor"][1], material["diffuseColor"][2]};}
                            if (material["emissiveColor"] != null) { diffuseColor =  new double[3]{ material["emissiveColor"][0], material["emissiveColor"][1], material["emissiveColor"][2]};}
                            if (material["specularColor"] != null) { diffuseColor =  new double[3]{ material["specularColor"][0], material["specularColor"][1], material["specularColor"][2]};}
                            if (material["shininess"] != null) { shininess = material["shininess"]; }
                            if (material["transparency"] != null) { transparency = material["transparency"]; }
                            if (material["isSmooth"] != null) { isSmooth = material["isSmooth"]; }

                            materialList.Add(
                                new GHMaterial(
                                    new Material(
                                        name,
                                        ambientIntensity,
                                        diffuseColor,
                                        emissiveColor,
                                        specularColor,
                                        shininess,
                                        transparency,
                                        isSmooth
                                        )
                                    )
                                );
                        }
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMaterialsFound]);
                    }
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMaterialsFound]);
                }

                // get LoD
                foreach (var objectGroup in Jcity.CityObjects)
                {
                    foreach (var cObject in objectGroup)
                    {
                        if (cObject.geometry == null) // parents
                        {
                            continue;
                        }

                        foreach (var boundaryGroup in cObject.geometry)
                        {
                            string currentLoD = boundaryGroup.lod;

                            if (!lodLevels.Contains(currentLoD))
                            {
                                lodLevels.Add(currentLoD);
                            }

                        }
                    }
                }
            }

            // make tree from meta data
            var dataTree = new Grasshopper.DataTree<string>();

            var metaValues = new List<string>();
            var metaKeys = new List<string>();

            foreach (var metadata in nestedMetaData)
            {
                foreach (var item in metadata)
                {
                    if (!metaKeys.Contains(item.Key))
                    {
                        metaKeys.Add(item.Key);
                    }
                }
            }

            int counter = 0;
            foreach (var metadata in nestedMetaData)
            {
                var nPath = new Grasshopper.Kernel.Data.GH_Path(counter);

                foreach (var metaKey in metaKeys)
                {
                    if (metadata.ContainsKey(metaKey))
                    {
                        dataTree.Add(metadata[metaKey], nPath);
                    }
                }
                counter++;
            }
            lodLevels.Sort();

            DA.SetDataList(0, metaKeys);
            DA.SetDataTree(1, dataTree);
            DA.SetDataList(2, lodLevels);
            DA.SetDataList(3, materialList);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.metaicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a274e16"); }
        }
    }
}
