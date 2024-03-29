﻿using Grasshopper.Kernel;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rhino.Geometry;


namespace RhinoCityJSON.Components
{
    public class ReaderDoc : GH_Component
    {
        public ReaderDoc()
          : base("Reader Documents", "DReader",
              "Fetches the Metadata, Textures and Materials from a CityJSON file, Autoresolves when multiple inputs",
              "RhinoCityJSON", DefaultValues.defaultReaderFolder)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Settings", "S", "Settings coming from the RSettings component", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Metadata Information", "Mi", "Information related to the document", GH_ParamAccess.item);
            pManager.AddTextParameter("LoD", "L", "LoD levels", GH_ParamAccess.item);
            pManager.AddGenericParameter("Materials", "m", "Materials stored in the files", GH_ParamAccess.list);
            pManager.AddBoxParameter("Domain", "D", "Full spacial domain of the file", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool boolOn = false;
            List<string> pathList = new List<string>();
            List<Types.GHReaderSettings> settingsList = new List<Types.GHReaderSettings>();
            DA.GetDataList(2, settingsList);

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

            // get the settings
            bool setLoD = false;
            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;
            double rotationAngle = 0;
            bool allowLargeFile = false;

            if (settingsList.Count() > 0)
            {
                if (settingsList[0].Value.isDocSetting())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.incorrectSetComponent]);
                    return;
                }

                ReaderSupport.getSettings(
                                settingsList[0],
                                ref worldOrigin,
                                ref translate,
                                ref rotationAngle,
                                ref allowLargeFile
                                );
            }

            List<Types.GHMaterial> materialList = new List<Types.GHMaterial>();
            List<string> lodLevels = new List<string>();
            var nestedMetaData = new List<Dictionary<string, string>>();

            var domainList = new List<Rhino.Geometry.Box>();

            // get scale from current session
            double scaler = ReaderSupport.getDocScaler();
            if (scaler == -1)
            {
                scaler = 1;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noScale]);
            }


            // hold translation value
            Rhino.Geometry.Vector3d lllExtend = new Rhino.Geometry.Vector3d(0, 0, 0);
            bool isFirst = true;


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

                // get LoD (this is not part of metadata and can still be generated without valid metadata
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
                            string currentLoD = boundaryGroup.lod.ToString();

                            if (!lodLevels.Contains(currentLoD))
                            {
                                Rhino.RhinoApp.WriteLine(currentLoD);
                                Rhino.RhinoApp.WriteLine(" ");

                                lodLevels.Add(currentLoD);
                            }

                        }
                    }
                }

                lodLevels.Sort();
                DA.SetDataList(1, lodLevels);

                // fetch metadata
                if (Jcity.metadata == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMetaDataFound]);
                    continue;
                }

                if (!Jcity.ContainsKey("transform"))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMetaDataFound]);
                    return;
                }
                var translationObject = Jcity.transform;

                if (!translationObject.ContainsKey("translate"))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMetaDataFound]);
                    return;
                }

                var translationtransObject = translationObject.translate;
                var metaData = Jcity.metadata;

                if (!metaData.ContainsKey("geographicalExtent"))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noMetaDataFound]);
                    return;
                }

                var geoExtendObject = metaData.geographicalExtent;

                Rhino.Geometry.Box domain = new Rhino.Geometry.Box();
                if (!translate)
                {
                    if (isFirst)
                    { // compute the translation of every object
                        isFirst = false;
                        lllExtend.X = -(double)translationtransObject[0];
                        lllExtend.Y = -(double)translationtransObject[1];
                        lllExtend.Z = -(double)translationtransObject[2];
                    }

                    Rhino.Geometry.BoundingBox bbox = new Rhino.Geometry.BoundingBox(
                        ((double)geoExtendObject[0] + lllExtend.X) * scaler,
                        ((double)geoExtendObject[1] + lllExtend.Y) * scaler,
                        ((double)geoExtendObject[2] + lllExtend.Z) * scaler,
                        ((double)geoExtendObject[3] + lllExtend.X) * scaler,
                        ((double)geoExtendObject[4] + lllExtend.Y) * scaler,
                        ((double)geoExtendObject[5] + lllExtend.Z) * scaler
                        );
                    domain = new Rhino.Geometry.Box(bbox);
                }
                else
                {
                    Rhino.Geometry.BoundingBox bbox = new Rhino.Geometry.BoundingBox(
                        (double)geoExtendObject[0] * scaler,
                        (double)geoExtendObject[1] * scaler,
                        (double)geoExtendObject[2] * scaler,
                        (double)geoExtendObject[3] * scaler,
                        (double)geoExtendObject[4] * scaler,
                        (double)geoExtendObject[5] * scaler
                        );
                    domain = new Rhino.Geometry.Box(bbox);
                }

                Transform translateTransform = Transform.Translation(new Vector3d(-worldOrigin.X, -worldOrigin.Y, -worldOrigin.Z));
                Transform rotateTransform = Transform.Rotation(rotationAngle, new Point3d(0, 0, 0));

                domain.Transform(translateTransform);
                domain.Transform(rotateTransform);

                domainList.Add(domain);

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
                        if (metaValue.Type == Newtonsoft.Json.Linq.JTokenType.Array) { continue; }
                        foreach (Newtonsoft.Json.Linq.JProperty nestedMetaValue in metaValue)
                        {
                            metadata.Add(metaName.ToString() + " " + nestedMetaValue.Name.ToString(), nestedMetaValue.Value.ToString());
                        }
                    }
                }

                nestedMetaData.Add(metadata);

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
                            if (material["emissiveColor"] != null) { emissiveColor =  new double[3]{ material["emissiveColor"][0], material["emissiveColor"][1], material["emissiveColor"][2]};}
                            if (material["specularColor"] != null) { specularColor =  new double[3]{ material["specularColor"][0], material["specularColor"][1], material["specularColor"][2]};}
                            if (material["shininess"] != null) { shininess = material["shininess"]; }
                            if (material["transparency"] != null) { transparency = material["transparency"]; }
                            if (material["isSmooth"] != null) { isSmooth = material["isSmooth"]; }

                            materialList.Add(
                                new Types.GHMaterial(
                                    new Types.Material(
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

                
            }

            // make tree from meta data
            var dataTree = new Grasshopper.DataTree<string>();
            var domainTree = new Grasshopper.DataTree<Rhino.Geometry.Box>();
            List<Types.GHObjectInfo> objectDataList = new List<Types.GHObjectInfo>();

            int counter = 0;
            foreach (var metadata in nestedMetaData)
            {
                Types.ObjectInfo objectData = new Types.ObjectInfo();
                foreach (var pair in metadata)
                {
                    objectData.addOtherData(pair.Key, pair.Value);
                }

                objectDataList.Add(new Types.GHObjectInfo(objectData));

            }

            counter = 0;

            foreach(var domain in domainList)
            {
                var nPath = new Grasshopper.Kernel.Data.GH_Path(counter);

                domainTree.Add(domain, nPath);
            }

            DA.SetDataList(0, objectDataList);
            DA.SetDataList(2, materialList);
            DA.SetDataTree(3, domainTree);
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
