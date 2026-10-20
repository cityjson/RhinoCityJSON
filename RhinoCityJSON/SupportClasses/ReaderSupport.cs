using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Newtonsoft.Json;

namespace RhinoCityJSON
{
    class ReaderSupport
    {
        static public string arrayToString(double[] doubleArray)
        {
            if (doubleArray[0] == -1)  {  return DefaultValues.defaultNoneValue; }

            return Math.Round(doubleArray[0] * 255, 0).ToString() + ", " + Math.Round(doubleArray[1] * 255, 0).ToString() + ", " + Math.Round(doubleArray[2] * 255, 0).ToString();
        }


        /// @brief 
        static public List<Rhino.Geometry.Point3d> getVerts(
            dynamic Jcity,
            Vector3d firstTranslation,
            Point3d worldCenter,
            double docScaler,
            double rotationAngle,
            RhinoCityJSON.SupportClasses.ProjImplementation projInstance,
            string currentCRS = "",
            string targedCrs = "",
            bool isTemplateSurf = false
            )
        {

            // get the cityJSON document data
            var translationCityData = Jcity.transform.translate;
            Vector3d translationCityVector;
            if (isTemplateSurf) { translationCityVector = new Vector3d(0, 0, 0); }
            else { translationCityVector = new Vector3d((double)translationCityData[0], (double)translationCityData[1], (double)translationCityData[2]); }

            var scalingCityData = Jcity.transform.scale;
            Vector3d scaleCityVector;
            if (isTemplateSurf) {  scaleCityVector = new Vector3d(1, 1, 1); }
            else { scaleCityVector = new Vector3d((double)scalingCityData[0], (double)scalingCityData[1], (double)scalingCityData[2]); }

            if (!isTemplateSurf && !string.IsNullOrWhiteSpace(targedCrs))
            {
                projInstance.initTransform(currentCRS, targedCrs);
            }

            // ceate vertlist
            dynamic jsonverts;
            if (isTemplateSurf)
            {
                dynamic geoTemplates = Jcity["geometry-templates"];
                if (geoTemplates == null) { return new List<Rhino.Geometry.Point3d>(); }

                jsonverts = Jcity["geometry-templates"]["vertices-templates"];
                if (jsonverts == null) { return new List<Rhino.Geometry.Point3d>(); }
            }
            else { jsonverts = Jcity.vertices; }

            List<Rhino.Geometry.Point3d> vertList = new List<Rhino.Geometry.Point3d>();
            foreach (var jsonvert in jsonverts)
            {
                List<double> vertCoords = new List<double> {0,0,0 };

                // get the cityjson document vert coordinate
                vertCoords[0] = (double)jsonvert[0] * scaleCityVector[0] + translationCityVector[0];
                vertCoords[1] = (double)jsonvert[1] * scaleCityVector[1] + translationCityVector[1];
                vertCoords[2] = (double)jsonvert[2] * scaleCityVector[2] + translationCityVector[2];

                //TODO: compute the reprojected vert coordinate
                if (!isTemplateSurf && !string.IsNullOrWhiteSpace(targedCrs))
                {
                    vertCoords = projInstance.ReprojectPoint(vertCoords);
                }

                // compute the local rhino document coordinate
                vertCoords[0] = docScaler * vertCoords[0] + firstTranslation.X - worldCenter.X;
                vertCoords[1] = docScaler * vertCoords[1] + firstTranslation.Y - worldCenter.Y;
                vertCoords[2] = docScaler * vertCoords[2] + firstTranslation.Z - worldCenter.Z;

                Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(
                     vertCoords[0] * Math.Cos(rotationAngle) - vertCoords[1] * Math.Sin(rotationAngle),
                     vertCoords[1] * Math.Cos(rotationAngle) + vertCoords[0] * Math.Sin(rotationAngle),
                     vertCoords[2]
                     );
                
                 vertList.Add(vert);
            }

            if (!isTemplateSurf && !string.IsNullOrWhiteSpace(targedCrs))
            {
                projInstance.discardTransform();
            }

            return vertList;
        }

        static public bool CheckInDomain(dynamic boundaries, List<Point3d> pointList, double scaler, Brep domainBox)
        {
            if (boundaries[0][0].Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                foreach (var ring in boundaries)
                {
                    foreach (int idx in ring)
                    {
                        Point3d p = pointList[idx];

                        if (domainBox.IsPointInside(p, 0.01, true))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                foreach (var boundary in boundaries)
                {
                    if (CheckInDomain(boundary, pointList, scaler, domainBox))
                    {
                        return true;
                    }
                   
                }
            }

             return false;
        }


        static public bool CheckValidity(dynamic file)
        {
            if (file.CityObjects == null || file.type != "CityJSON" || file.version == null ||
                file.transform == null || file.transform.scale == null || file.transform.translate == null ||
                file.vertices == null)
            {
                return false;
            }
            else if (file.version != "1.1" && file.version != "1.0" && file.version != "2.0")
            {
                return false; //TODO: adress this
            }
            return true;
        }

        static public double getDocScaler()
        {
            string UnitString = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();

            if (UnitString == "Meters") { return 1; }
            else if (UnitString == "Centimeters") { return 100; }
            else if (UnitString == "Millimeters") { return 1000; }
            else if (UnitString == "Feet") { return 3.28084; }
            else if (UnitString == "Inches") { return 39.3701; }
            else { return -1; }
        }

        public static string concatonateStringList(List<string> stringList)
        {
            string conString = "";

            if (stringList.Count == 1)
            {
                return stringList[0];
            }
            else
            {
                conString = stringList[0];
                for (int i = 1; i < stringList.Count; i++)
                {
                    conString = conString + ", " + stringList[i];
                }
                return conString;
            }
        }
        public static errorCodes getSettings(
          Types.GHReaderSettings ghSettings,
          ref Point3d worldOrigin,
          ref bool translate,
          ref double rotationAngle,
          ref bool allowLargeFile
          )
        {
            var CJSettings = ghSettings.Value;
            translate = CJSettings.getTranslate();
            rotationAngle = Math.PI * CJSettings.getTrueNorth() / 180.0;
            worldOrigin = CJSettings.getModelOrigin();
            allowLargeFile = CJSettings.getAllowLargeFile();
            return errorCodes.noError;
        }

        public static errorCodes getSettings(
           Types.GHReaderSettings ghSettings,
           ref List<string> loDList,
           ref bool setLoD,
           ref Point3d worldOrigin,
           ref bool translate,
           ref double rotationAngle,
           ref Brep domainbox,
           ref string tCrs,
           ref bool allowLargeFile
           )
        {
            getSettings(ghSettings,
                ref worldOrigin,
                ref translate,
                ref rotationAngle,
                ref allowLargeFile);

            var CJSettings = ghSettings.Value;
            domainbox = CJSettings.getDomain();

            loDList = CJSettings.getLoDList();

            tCrs = CJSettings.getTCrs();

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    setLoD = true;
                }
            }
            return errorCodes.noError;
        }

        public static void populateSurfaceKeys(
            ref List<string> keyList,
            List<string> surfaceTypes,
            List<string> materialReferenceNames,
            bool isTemplate = false
            )
        {
            // populate with default values
            if (isTemplate) { keyList = new List<string>(DefaultValues.surfaceTemplateKeys); }
            else { keyList = new List<string>(DefaultValues.surfaceObjectKeys); }

            foreach (var item in surfaceTypes)
            {
                keyList.Add(DefaultValues.defaultSurfaceAddition + item);
            }
            foreach (var item in materialReferenceNames)
            {
                keyList.Add(DefaultValues.defaultSurfaceAddition + "Material " + item);
            }
        }


        public static void populateObjectKeys(
            ref List<string> keyList,
            List<string> objectTypes,
            bool isTemplate = false
            )
        {
            // populate with default values
            if (isTemplate) { keyList = new List<string>(DefaultValues.templateKeys); }
            else { keyList = new List<string>(DefaultValues.objectKeys); }

            foreach (string item in objectTypes)
            {
                keyList.Add(DefaultValues.defaultObjectAddition + item);
            }
        }


        public static void populateObjectOtherDataDict(
           ref Dictionary<string, string> flatObjectSemanticTree,
           CJT.CityObject cityObject,
           CJT.CityCollection ObjectCollection,
           List<string> objectTypes
           )
        {

            // add custom object attributes
            var objectAttributes = cityObject.getAttributes();

            var inheritedAttributes = cityObject.getInheritancedAtt(ObjectCollection);

            foreach (string item in objectTypes)
            {
                if (objectAttributes.ContainsKey(item))
                {
                    flatObjectSemanticTree.Add(DefaultValues.defaultObjectAddition + item, objectAttributes[item].ToString());
                }
                else if (inheritedAttributes.ContainsKey(item))
                {
                    flatObjectSemanticTree.Add(DefaultValues.defaultObjectAddition + item, inheritedAttributes[item].ToString() + DefaultValues.defaultInheritanceAddition);
                }
                else
                {
                    flatObjectSemanticTree.Add(DefaultValues.defaultObjectAddition + item, DefaultValues.defaultNoneValue);
                }
            }
        }

        public static void populateFlatSemanticTree(
            ref Grasshopper.DataTree<string> flatObjectSemanticTree,
            CJT.CityObject cityObject,
            CJT.CityCollection ObjectCollection,
            List<string> objectTypes,
            int pathCounter
            )
        {
            var objectPath = new Grasshopper.Kernel.Data.GH_Path(pathCounter);

            // add native object attributes
            string objectName = cityObject.getName();
            List<string> objectParents = cityObject.getParents();
            List<string> objectChildren = cityObject.getChildren();

            flatObjectSemanticTree.Add(objectName, objectPath);
            flatObjectSemanticTree.Add(cityObject.getType(), objectPath);

            if (objectParents.Count > 0) { flatObjectSemanticTree.Add(ReaderSupport.concatonateStringList(objectParents)); }
            else { flatObjectSemanticTree.Add(DefaultValues.defaultNoneValue); }

            if (objectChildren.Count > 0) { flatObjectSemanticTree.Add(ReaderSupport.concatonateStringList(objectChildren)); }
            else { flatObjectSemanticTree.Add(DefaultValues.defaultNoneValue); }

            if (cityObject.isTemplated())
            {
                flatObjectSemanticTree.Add(cityObject.getTemplate().getTemplate().ToString(), objectPath);
                flatObjectSemanticTree.Add(cityObject.getTemplate().getAnchor().ToString(), objectPath);
            }

            // add custom object attributes
            var objectAttributes = cityObject.getAttributes();
            var inheritedAttributes = cityObject.getInheritancedAtt(ObjectCollection);

            foreach (string item in objectTypes)
            {
                if (objectAttributes.ContainsKey(item))
                {
                    flatObjectSemanticTree.Add(objectAttributes[item].ToString(), objectPath);
                }
                else if (inheritedAttributes.ContainsKey(item))
                {
                    flatObjectSemanticTree.Add(inheritedAttributes[item].ToString() + DefaultValues.defaultInheritanceAddition, objectPath);
                }
                else
                {
                    flatObjectSemanticTree.Add(DefaultValues.defaultNoneValue, objectPath);
                }
            }
        }


        public static void populateSurfaceOtherDataDict(
           ref Dictionary<string, string> flatSurfaceSemanticTree,
           List<string> surfaceTypes,
           List<string> materialReferenceNames,
           CJT.GeoObject geoObject,
           CJT.SurfaceObject surface
           )
        {

            // add semantic surface data
            if (geoObject.hasSurfaceData())
            {
                var surfaceSemantics = geoObject.getSurfaceData(geoObject.getSurfaceTypeValue(surface.getSemanticlValue()));

                foreach (var item in surfaceTypes)
                {
                    if (surfaceSemantics.ContainsKey(item))
                    {
                        flatSurfaceSemanticTree.Add(DefaultValues.defaultSurfaceAddition + item ,surfaceSemantics[item].ToString());
                    }
                    else flatSurfaceSemanticTree.Add(DefaultValues.defaultSurfaceAddition + item, DefaultValues.defaultNoneValue);
                }
            }
            else
            {
                foreach (var item in surfaceTypes)
                {
                    flatSurfaceSemanticTree.Add(DefaultValues.defaultSurfaceAddition + item, DefaultValues.defaultNoneValue);
                }
            }
        }


        public static errorCodes checkInput(
            bool boolOn,
            List<Types.GHReaderSettings> ghSettings,
            List<string> pathList
            )
        {
            if (!boolOn)
            {
                return errorCodes.offline;
            }
            else if (ghSettings.Count > 1)
            {
                return errorCodes.multipleInputSettings;
            }
            // validate the data and warn the user if invalid data is supplied.
            else if (pathList[0] == "")
            {
                return errorCodes.emptyPath;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    return errorCodes.invalidPath;
                }
            }
            return errorCodes.noError;
        }

        public static List<string> getCRS(ref List<dynamic> cityCollection)
        {
            List<string> crsList = new List<string>();

            for (int i = 0; i < cityCollection.Count; i++)
            {
                var Jcity = cityCollection[i];
                var metaData = Jcity.metadata;

                if (metaData == null)
                {
                    crsList.Add("");
                    continue;
                }

                var crs = metaData.referenceSystem;

                if (crs == null)
                {
                    crsList.Add("");
                    continue;
                }
                crsList.Add(crs.ToString());
            }
            return crsList;
        }
    }
}
