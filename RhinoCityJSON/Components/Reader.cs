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
          : base("Reader Objects", "OReader",
              "Reads the object data stored in a CityJSON file",
              "RhinoCityJSON", DefaultValues.defaultReaderFolder)
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
            pManager.AddGenericParameter("Surface Information", "Si", "Information related to the surfaces", GH_ParamAccess.item);
            pManager.AddGenericParameter("Object Information", "Oi", "Information related to the Objects", GH_ParamAccess.item);
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

            var activeDoc = Rhino.RhinoDoc.ActiveDoc;


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
            Brep domainBox = new Brep();
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
                                ref loDList,
                                ref setLoD,
                                ref worldOrigin,
                                ref translate,
                                ref rotationAngle,
                                ref domainBox,
                                ref allowLargeFile);
            }

            // find out if domain has to be filtered
            bool filterDomain = true;
            if (domainBox.GetArea() == 0) { filterDomain = false; }

            // hold translation value
            Vector3d firstTranslation = new Vector3d(0, 0, 0);

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

            // object Count
            int totalCount = 0;
            List<Newtonsoft.Json.Linq.JObject> jCityObjectCollection = new List<Newtonsoft.Json.Linq.JObject>();

            for (int i = 0; i < cityJsonCollection.Count; i++)
            {
                var Jcity = cityJsonCollection[i];
                Newtonsoft.Json.Linq.JObject jCityObjectList = Jcity.CityObjects;
                totalCount += jCityObjectList.Count;
                if (totalCount > 10000 && !allowLargeFile)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ErrorCollection.errorCollection[errorCodes.largeFile]);
                    return;
                }
                jCityObjectCollection.Add(jCityObjectList);
            }

            bool isFirst = true;
            CJT.CityCollection ObjectCollection = new CJT.CityCollection();
            List<string> surfaceTypes = new List<string>();
            List<string> objectTypes = new List<string>();
            List<string> materialReferenceNames = new List<string>();

            for (int i = 0; i < cityJsonCollection.Count; i++)
            {
                var Jcity = cityJsonCollection[i];
                
                if (isFirst)
                { // compute the translation of every object
                    var firstTransformationData =  Jcity.transform.translate;

                    if (firstTransformationData != null)
                    {
                        isFirst = false;
                        firstTranslation.X = -(double)firstTransformationData[0];
                        firstTranslation.Y = -(double)firstTransformationData[1];
                        firstTranslation.Z = -(double)firstTransformationData[2];
                    }
                }

                // get vertices stored in a tile
                List<Rhino.Geometry.Point3d> vertList = ReaderSupport.getVerts(Jcity, firstTranslation, worldOrigin, scaler, rotationAngle, translate);
                Newtonsoft.Json.Linq.JObject jCityObjectList = jCityObjectCollection[i];

                foreach (var JcityObject in jCityObjectList)
                {
                    // check if name is present
                    if (ObjectCollection.getCollection().ContainsKey(JcityObject.Key)) {  continue; }

                    CJT.CityObject cityObject = new CJT.CityObject();
                    dynamic JCityObjectAttributes = JcityObject.Value;
                    dynamic JCityObjectAttributesAttributes = JCityObjectAttributes.attributes;

                    if (JCityObjectAttributesAttributes != null)
                    {
                        foreach (var attribue in JCityObjectAttributesAttributes) { objectTypes.Add(attribue.Name); }
                    }

                    cityObject.setName(JcityObject.Key);
                    cityObject.setType(JCityObjectAttributes.type.ToString());
                    cityObject.setParents(JCityObjectAttributes.parents);
                    cityObject.setChildren(JCityObjectAttributes.children);
                    cityObject.setOriginalFileName(pathList[i]);
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
                        // check if any vertex falls in domain
                        if (filterDomain)
                        {
                            if (!ReaderSupport.CheckInDomain(jGeoObject.boundaries, vertList, scaler, domainBox))
                            {
                                continue;
                            }
                        }

                        // immidiately set geometry to allow for cancelling if object is not withing range
                        CJT.GeoObject geoObject = new CJT.GeoObject();
                        geoObject.setGeometry(jGeoObject.boundaries, vertList, scaler);

                        // pass if object has no geometry
                        if (!geoObject.hasGeometry()) { continue; }
                        geoObject.setGeoName(JcityObject.Key + "-" + uniqueCounter.ToString());
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

            // flatten data for grasshopper output
            List<Brep> flatSurfaceList = new List<Brep>();
            List<Types.GHObjectInfo> objectDataList = new List<Types.GHObjectInfo>();
            List<Types.GHObjectInfo> surfaceDataList = new List<Types.GHObjectInfo>();

            foreach (var cityObject in ObjectCollection.getFlatColletion())
            {
                Dictionary<string, string> additionalObjectData = new Dictionary<string, string>();
                ReaderSupport.populateObjectOtherDataDict(ref additionalObjectData, cityObject, ObjectCollection, objectTypes);

                objectDataList.Add(new Types.GHObjectInfo(
                    new Types.ObjectInfo(
                    cityObject.getName(),
                    cityObject.getType(),
                    cityObject.getParents(),
                    cityObject.getChildren(),
                    cityObject.getOriginalFileName(),
                    additionalObjectData
                )));

                foreach (var geoObject in cityObject.getGeometry())
                {
                    string geoType = geoObject.getGeoType();
                    string geoName = geoObject.getGeoName();
                    string geoLoD = geoObject.getLoD();

                    int counter = 0;
                    foreach (var surface in geoObject.getBoundaries())
                    {
                        string surfaceName = geoName + "-" + counter;

                        surface.getShape().SetUserString("_Geoname", surfaceName);
                        surface.getShape().SetUserString("_ObjName", cityObject.getName());
                        flatSurfaceList.Add(surface.getShape());
                        Dictionary<string, string> additionalSurfaceData = new Dictionary<string, string>();

                        ReaderSupport.populateSurfaceOtherDataDict(
                            ref additionalSurfaceData, 
                            surfaceTypes, 
                            materialReferenceNames, 
                            geoObject, 
                            surface)
                            ;


                        var objectInfoObject =
                             new Types.ObjectInfo(
                            surfaceName,
                            geoType,
                            geoLoD,
                            "",               
                            cityObject.getName(),
                            cityObject.getOriginalFileName(),
                            additionalSurfaceData
                            );

                        // add material data
                        foreach (var item in materialReferenceNames)
                        {
                            if (geoObject.hasMaterialData())
                            {
                                var materialCollection = geoObject.getSurfaceMaterialValues();

                                if (materialCollection.ContainsKey(item))
                                {
                                    var matNum = materialCollection[item][surface.getSemanticlValue()];
                                    if (matNum >= 0)
                                    {
                                        objectInfoObject.addMaterial(item, matNum.ToString());
                                        break; // currently only single materials are supported
                                    }
                                }
                            }
                        }
                        surfaceDataList.Add(new Types.GHObjectInfo(objectInfoObject));
                        counter++;
                    }
                }
            }

            DA.SetDataList(0, flatSurfaceList);
            DA.SetDataList(1, surfaceDataList);
            DA.SetDataList(2, objectDataList);
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
