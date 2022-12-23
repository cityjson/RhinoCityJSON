using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RhinoCityJSON.Components
{
    public class RhinoTemplateJSONReader : GH_Component
    {
        public RhinoTemplateJSONReader()
          : base("RCJTemplateReader", "TReader",
              "Reads the template data stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Settings", "S", "Settings coming from the RSettings component", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Template Geometry", "TG", "Geometry output", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Keys", "TSiK", "Keys of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Values", "TSiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Keys", "TOik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Values", "TOiV", "Values of the semantic information output related to the objects", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> pathList = new List<string>();
            var settingsList = new List<Grasshopper.Kernel.Types.GH_ObjectWrapper>();

            bool boolOn = false;

            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);
            DA.GetDataList(2, settingsList);

            errorCodes inputError = ReaderSupport.checkInput(
                boolOn,
                settingsList,
                pathList
                );

            if (inputError != errorCodes.noError)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[inputError]);
                return;
            }

            // get the settings
            List<string> loDList = new List<string>();
            bool setLoD = false;
            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;
            double rotationAngle = 0;

            ReaderSupport.getSettings(
                settingsList,
                ref loDList,
                ref setLoD,
                ref worldOrigin,
                ref translate,
                ref rotationAngle);

            // get scale from current session
            double scaler = ReaderSupport.getDocScaler();

            if (scaler == -1)
            {
                scaler = 1;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noScale]);
            }

            // Parse and check if valid CityJSON format
            List<dynamic> cityJsonCollection = new List<dynamic>();
            foreach (var path in pathList)
            {
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));

                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ErrorCollection.errorCollection[errorCodes.invalidJSON]);
                    return;
                }
                cityJsonCollection.Add(Jcity);
            }

            bool isFirst = true;
            CJT.CityCollection ObjectCollection = new CJT.CityCollection();
            List<string> surfaceTypes = new List<string>();
            List<string> objectTypes = new List<string>();
            List<string> materialReferenceNames = new List<string>();
            List<CJT.GeoObject> templateGeoList = new List<CJT.GeoObject>();

            foreach (var Jcity in cityJsonCollection)
            {
                // get vertices stored in a tile
                List<Rhino.Geometry.Point3d> LocationList = ReaderSupport.getVerts(Jcity, worldOrigin, scaler, rotationAngle, isFirst, translate);
                List<Rhino.Geometry.Point3d> vertList = ReaderSupport.getVerts(Jcity, new Point3d(0, 0, 0), scaler, 0.0, isFirst, false, true);

                isFirst = false;

                if (vertList.Count == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.noTeamplateFound]);
                }

                // create template objects
                int uniqueCounter = 0;
                foreach (var jGeoObject in Jcity["geometry-templates"]["templates"])
                {
                    string lod = jGeoObject.lod.ToString();

                    if (setLoD)
                    {
                        if (!loDList.Contains(lod)) { continue; }
                    }


                    CJT.GeoObject geoObject = new CJT.GeoObject();
                    geoObject.setGeoName(uniqueCounter.ToString());
                    geoObject.setGeoType(jGeoObject.type.ToString());
                    geoObject.setLod(lod);

                    if (jGeoObject.semantics != null)
                    {
                        if (jGeoObject.semantics.surfaces != null && jGeoObject.semantics.values != null)
                        {
                            dynamic jSurfaceAttrubutes = jGeoObject.semantics.surfaces;

                            if (jSurfaceAttrubutes != null)
                            {
                                foreach (var attribueCollection in jSurfaceAttrubutes)
                                {
                                    foreach (var attribue in attribueCollection) { surfaceTypes.Add(attribue.Name); }
                                }
                            }

                            geoObject.setSurfaceData(jSurfaceAttrubutes);
                            geoObject.setSurfaceTypeValues(jGeoObject.semantics.values);
                        }
                    }
                    if (jGeoObject.material != null)
                    {
                        var materialObject = jGeoObject.material;
                        geoObject.setSurfaceMaterialValues(materialObject);
                        foreach (var surfaceMaterial in materialObject) { materialReferenceNames.Add(surfaceMaterial.Name.ToString()); }
                    }
                    geoObject.setGeometry(jGeoObject.boundaries, vertList, scaler);
                    templateGeoList.Add(geoObject);
                    uniqueCounter++;
                }

                foreach (var JcityObject in Jcity.CityObjects)
                {
                    CJT.CityObject cityObject = new CJT.CityObject();
                    dynamic JCityObjectAttributes = JcityObject.Value;
                    dynamic JCityObjectAttributesAttributes = JCityObjectAttributes.attributes;

                    if (JCityObjectAttributesAttributes != null)
                    {
                        foreach (var attribue in JCityObjectAttributesAttributes) { objectTypes.Add(attribue.Name); }
                    }

                    cityObject.setName(JcityObject.Name);
                    cityObject.setType(JCityObjectAttributes.type.ToString());
                    cityObject.setParents(JCityObjectAttributes.parents);
                    cityObject.setChildren(JCityObjectAttributes.children);
                    cityObject.setAttributes(JCityObjectAttributesAttributes);

                    if (JCityObjectAttributes.geometry == null)
                    {
                        cityObject.setIsFilteredout();
                        ObjectCollection.add(cityObject);
                        continue;
                    }
                    cityObject.setHasGeo(true);

                    foreach (var jGeoObject in JCityObjectAttributes.geometry)
                    {
                        if (jGeoObject.type != "GeometryInstance") { continue; }
                        int templateIdx = jGeoObject["template"];
                        int pointIdx = jGeoObject["boundaries"][0];
                        cityObject.addTemplate(templateIdx, LocationList[pointIdx]);
                    }
                    ObjectCollection.add(cityObject); // data without geometry is still stored for attributes 
                }
            }

            surfaceTypes = surfaceTypes.Distinct().ToList();
            objectTypes = objectTypes.Distinct().ToList();
            materialReferenceNames = materialReferenceNames.Distinct().ToList();

            List<string> surfaceKeyList = new List<string>();
            ReaderSupport.populateSurfaceKeys(
                ref surfaceKeyList,
                surfaceTypes,
                materialReferenceNames,
                true);

            List<string> objectKeyList = new List<string>();
            ReaderSupport.populateObjectKeys(
                ref objectKeyList,
                objectTypes,
                true);


            List<Brep> flatSurfaceList = new List<Brep>();
            var flatSurfaceSemanticTree = new Grasshopper.DataTree<string>();
            var flatObjectSemanticTree = new Grasshopper.DataTree<string>();
            int objectCounter = 0;
            int surfaceCounter = 0;

            foreach (var geoObject in templateGeoList)
            {
                string geoType = geoObject.getGeoType();
                int geoNum = objectCounter;
                string geoLoD = geoObject.getLoD();
                var nPath = new Grasshopper.Kernel.Data.GH_Path(objectCounter);

                foreach (var surface in geoObject.getBoundaries())
                {
                    var nPath2 = new Grasshopper.Kernel.Data.GH_Path(surfaceCounter);
                    flatSurfaceList.Add(surface.getShape());
                    flatSurfaceSemanticTree.Add(geoNum.ToString(), nPath2);
                    flatSurfaceSemanticTree.Add(geoType, nPath2);
                    flatSurfaceSemanticTree.Add(geoLoD, nPath2);

                    ReaderSupport.addMatSurfValue(
                        ref flatSurfaceSemanticTree,
                        materialReferenceNames,
                        geoObject,
                        surface,
                        nPath2);

                    surfaceCounter++;
                }
                objectCounter++;
            }

            objectCounter = 0;
            foreach (var cityObject in ObjectCollection.getFlatColletion())
            {
                if (cityObject.isTemplated())
                {
                    ReaderSupport.populateFlatSemanticTree(ref flatObjectSemanticTree, cityObject, ObjectCollection, objectTypes, objectCounter);
                    objectCounter++;
                }
            }


            DA.SetDataList(0, flatSurfaceList);
            DA.SetDataList(1, surfaceKeyList);
            DA.SetDataTree(2, flatSurfaceSemanticTree);
            DA.SetDataList(3, objectKeyList);
            DA.SetDataTree(4, flatObjectSemanticTree);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.sreadericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb5-f76e8a275e15"); }
        }

    }
}
