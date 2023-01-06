using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RhinoCityJSON.Components
{
    public class RhinoCityJSONReader : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RhinoCityJSONReader()
          : base("RCJReader", "Reader",
              "Reads the object data stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
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
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Keys", "SiK", "Keys of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Surface Info Values", "SiV", "Values of the information output related to the surfaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Keys", "Oik", "Keys of the Semantic information output related to the objects", GH_ParamAccess.item);
            pManager.AddTextParameter("Object Info Values", "OiV", "Values of the semantic information output related to the objects", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> pathList = new List<string>();
            List<Types.GHReaderSettings> settingsList = new List<Types.GHReaderSettings>();

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

            if (settingsList.Count() > 0)
            {
                ReaderSupport.getSettings(
                                settingsList[0],
                                ref loDList,
                                ref setLoD,
                                ref worldOrigin,
                                ref translate,
                                ref rotationAngle);
            }

            

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

            foreach (var Jcity in cityJsonCollection)
            {
                // get vertices stored in a tile
                List<Rhino.Geometry.Point3d> vertList = ReaderSupport.getVerts(Jcity, worldOrigin, scaler, rotationAngle, isFirst, translate);
                isFirst = false;

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

                    int uniqueCounter = 0;
                    foreach (var jGeoObject in JCityObjectAttributes.geometry)
                    {
                        if (jGeoObject.type == "GeometryInstance") { continue; }

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
                        cityObject.addGeometry(geoObject);
                        uniqueCounter++;
                    }
                    if (cityObject.getGeometry().Count == 0) { cityObject.setIsFilteredout(); }
                    ObjectCollection.add(cityObject); // data without geometry is still stored for attributes 
                }
            }

            // make keyLists
            surfaceTypes = surfaceTypes.Distinct().ToList();
            objectTypes = objectTypes.Distinct().ToList();
            materialReferenceNames = materialReferenceNames.Distinct().ToList();

            List<string> surfaceKeyList = new List<string>();
            ReaderSupport.populateSurfaceKeys(ref surfaceKeyList, surfaceTypes, materialReferenceNames);

            List<string> objectKeyList = new List<string>();
            ReaderSupport.populateObjectKeys(ref objectKeyList, objectTypes);

            // flatten data for grasshopper output
            List<Brep> flatSurfaceList = new List<Brep>();
            var flatSurfaceSemanticTree = new Grasshopper.DataTree<string>();
            var flatObjectSemanticTree = new Grasshopper.DataTree<string>();
            int objectCounter = 0;
            int surfaceCounter = 0;

            foreach (var cityObject in ObjectCollection.getFlatColletion())
            {
                ReaderSupport.populateFlatSemanticTree(ref flatObjectSemanticTree, cityObject, ObjectCollection, objectTypes, objectCounter);

                foreach (var geoObject in cityObject.getGeometry())
                {
                    string geoType = geoObject.getGeoType();
                    string geoName = geoObject.getGeoName();
                    string geoLoD = geoObject.getLoD();

                    foreach (var surface in geoObject.getBoundaries())
                    {
                        flatSurfaceList.Add(surface.getShape());

                        ReaderSupport.populateFlatSurfSemanticTree(
                            ref flatSurfaceSemanticTree,
                            surfaceTypes,
                            materialReferenceNames,
                            cityObject, geoObject,
                            surface,
                            geoType,
                            geoName,
                            geoLoD,
                            surfaceCounter
                            );

                        surfaceCounter++;
                    }
                }
                objectCounter++;
            }

            DA.SetDataList(0, flatSurfaceList);
            DA.SetDataList(1, surfaceKeyList);
            DA.SetDataTree(2, flatSurfaceSemanticTree);
            DA.SetDataList(3, objectKeyList);
            DA.SetDataTree(4, flatObjectSemanticTree);
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.readericon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2364c3a-18ae-4eb3-aeb3-f76e8a2754e7"); }
        }
    }
}
