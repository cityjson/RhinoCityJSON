using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace RhinoCityJSON
{
    namespace CJT
    {
        class RingStructure // simple way to display a surface as a collection of rings
        {
            List<int> outerRing_ = new List<int>();
            List<List<int>> innerRingList_ = new List<List<int>>();

            public void setOuterRing(List<int> outerRing) { outerRing_ = outerRing; }
            public List<int> getOuterRing() { return outerRing_; }
            public void setInnerRings(List<List<int>> innerRings) { innerRingList_ = innerRings; }
            public void addInnerRing(List<int> innerRing) { innerRingList_.Add(innerRing); }
            public List<List<int>> getInnerRingList() { return innerRingList_; }
            public Rhino.Collections.CurveList getPolyStructure(List<Rhino.Geometry.Point3d> vertList)
            {
                // ring technique
                Rhino.Collections.CurveList surfaceCurves = new Rhino.Collections.CurveList();
                List<List<int>> rings = getInnerRingList();
                rings.Add(getOuterRing());

                foreach (var ring in rings)
                {
                    List<Rhino.Geometry.Point3d> curvePointsOuter = new List<Rhino.Geometry.Point3d>();
                    foreach (var vertIdx in ring)
                    {
                        curvePointsOuter.Add(vertList[vertIdx]);
                    }
                    if (curvePointsOuter.Count > 0)
                    {
                        curvePointsOuter.Add(curvePointsOuter[0]);

                        try //TODO: this can be improved
                        {
                            Rhino.Geometry.Polyline polyRing = new Rhino.Geometry.Polyline(curvePointsOuter);
                            surfaceCurves.Add(polyRing);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                return surfaceCurves;
            }
        }

        class SurfaceObject
        {
            Rhino.Geometry.Brep shape_;
            int semanticValue_;

            public void setShape(Brep shape) { shape_ = shape; }
            public Brep getShape() { return shape_; }
            public void setSemanticValue(int materialValue) { semanticValue_ = materialValue; }
            public int getSemanticlValue() { return semanticValue_; }
        }

        class TemplateObject
        {
            int template_ = 0;
            Point3d anchor_ = new Point3d(0, 0, 0);
            //Matrix transformation_ = new Matrix(4,4);
            // TODO: implement transformation


            public TemplateObject(
                int template,
                Point3d anchor
               )
            {
                template_ = template;
                anchor_ = anchor;
            }

            public Point3d getAnchor() { return anchor_; }
            public int getTemplate() { return template_; }
        }

        class GeoObject
        {
            List<SurfaceObject> boundaries_ = new List<SurfaceObject>();
            string lod_ = "-1";
            List<Dictionary<string, dynamic>> surfaceData_ = new List<Dictionary<string, dynamic>>();
            List<int> surfaceTypeValues_ = new List<int>();
            Dictionary<string, List<int>> surfaceMaterialValues_ = new Dictionary<string, List<int>>();
            string geoType_ = "";
            string GeoName_ = "";
            string GeoSuperName_ = "";
            bool hasSurfaceData_ = false;
            bool hasGeometry_ = false;

            public List<SurfaceObject> getBoundaries() { return boundaries_; }
            public void setBoundaries(List<SurfaceObject> boundaries) { boundaries_ = boundaries; }
            public void addBoundary(SurfaceObject boundary) { boundaries_.Add(boundary); }
            public string getLoD() { return lod_; }
            public void setLod(string lod) { lod_ = lod; }
            public List<Dictionary<string, dynamic>> getSurfaceData() { return surfaceData_; }
            public Dictionary<string, dynamic> getSurfaceData(int i) { return surfaceData_[i]; }
            public bool hasGeometry() { return hasGeometry_; }
            public void setSurfaceData(dynamic surfaceData)
            {
                List<Dictionary<string, dynamic>> completeSemanticColletion = new List<Dictionary<string, dynamic>>();
                foreach (var surfdata in surfaceData)
                {
                    Dictionary<string, dynamic> surfaceSem = new Dictionary<string, dynamic>();

                    foreach (var entry in surfdata)
                    {
                        surfaceSem.Add(entry.Name.ToString(), entry.Value);
                    }
                    completeSemanticColletion.Add(surfaceSem);
                }
                surfaceData_ = completeSemanticColletion;

                if (completeSemanticColletion.Count > 0)
                {
                    hasSurfaceData_ = true;
                }
            }
            public void addSurfaceData(Dictionary<string, dynamic> surfaceDataItem) { surfaceData_.Add(surfaceDataItem); }
            public List<int> getSurfaceTypeValues() { return surfaceTypeValues_; }
            public int getSurfaceTypeValue(int i) { return surfaceTypeValues_[i]; }
            public void setSurfaceTypeValues(dynamic surfaceTypeValues) { surfaceTypeValues_ = flattenValues(surfaceTypeValues); }
            public Dictionary<string, List<int>> getSurfaceMaterialValues() { return surfaceMaterialValues_; }
            public void setSurfaceMaterialValues(dynamic surfaceMaterialCollection)
            {
                surfaceMaterialValues_ = new Dictionary<string, List<int>>();
                foreach (var surfaceMaterial in surfaceMaterialCollection)
                {
                    string valueName = surfaceMaterial.Name.ToString();
                    dynamic materialValues = surfaceMaterial.Value.values;
                    if (materialValues != null)
                    {
                        surfaceMaterialValues_.Add(valueName, flattenValues(surfaceMaterial.Value.values));
                    } // TODO: make single value exceptions


                }
            }
            public string getGeoType() { return geoType_; }
            public void setGeoType(string geoType) { geoType_ = geoType; }
            public string getGeoName() { return GeoName_; }
            public void setGeoName(string geoName) { GeoName_ = geoName; }
            public string getSuperName() { return GeoSuperName_; }
            public void setSuperName(string geoName) { GeoSuperName_ = geoName; }

            List<int> flattenValues(dynamic nestedValues)
            {
                List<int> flatList = new List<int>();
                if (nestedValues[0] == null || nestedValues[0].Type == Newtonsoft.Json.Linq.JTokenType.Integer) // TODO: find issue here
                {
                    for (int i = 0; i < nestedValues.Count; i++)
                    {
                        var nestedValue = nestedValues[i];
                        if (nestedValue == null)
                        {
                            flatList.Add(-1);
                        }
                        else
                        {
                            flatList.Add((int)nestedValue);
                        }
                    }
                }
                else
                {
                    foreach (var nestedValue in nestedValues)
                    {
                        foreach (var value in flattenValues(nestedValue))
                        {
                            flatList.Add(value);
                        }
                    }
                }
                return flatList;
            }

            public void setGeometry(dynamic JBoundaryList, List<Rhino.Geometry.Point3d> vertList, double scaler)
            {
                boundaries_ = new List<SurfaceObject>();
                List<RingStructure> ringList = boundaries2Rings(JBoundaryList);

                int counter = 0;
                foreach (var ringSet in ringList)
                {
                    List<int> outerRing = ringSet.getOuterRing();
                    if (ringSet.getInnerRingList().Count == 0 && outerRing.Count <= 4)
                    {
                        NurbsSurface nSurface = null;
                        if (outerRing.Count == 3)
                        {
                            nSurface = NurbsSurface.CreateFromCorners(
                                vertList[outerRing[0]],
                                vertList[outerRing[1]],
                                vertList[outerRing[2]]
                                );
                        }
                        else if(outerRing.Count == 3)
                        {
                            nSurface = NurbsSurface.CreateFromCorners(
                                vertList[outerRing[0]],
                                vertList[outerRing[1]],
                                vertList[outerRing[2]],
                                vertList[outerRing[3]]
                                );
                        }
                        if (nSurface != null)
                        {
                            SurfaceObject surfaceObject = new SurfaceObject();
                            surfaceObject.setShape(nSurface.ToBrep());
                            surfaceObject.setSemanticValue(counter);
                            boundaries_.Add(surfaceObject);
                        }
                    }
                    else
                    {
                        Rhino.Collections.CurveList surfaceCurves = ringSet.getPolyStructure(vertList);
                        if (surfaceCurves.Count > 0)
                        {
                            Rhino.Geometry.Brep[] planarFace = Brep.CreatePlanarBreps(surfaceCurves, 1e-2*scaler);
                            surfaceCurves.Clear();
                            if (planarFace != null)
                            {
                                SurfaceObject surfaceObject = new SurfaceObject();
                                surfaceObject.setShape(planarFace[0]);
                                surfaceObject.setSemanticValue(counter);
                                boundaries_.Add(surfaceObject);
                            }
                        }
                    }
                    counter++;
                }
                if (boundaries_.Count() > 0) { hasGeometry_ = true; }
            }

            private List<RingStructure> boundaries2Rings(dynamic JBoundaryList)
            {
                List<RingStructure> ringCollection = new List<RingStructure>();
                if (JBoundaryList[0][0].Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                {
                    RingStructure ringStructure = new RingStructure();
                    int c = 0;
                    foreach (var ring in JBoundaryList)
                    {
                        List<int> ringList = new List<int>();
                        foreach (int idx in ring) { ringList.Add(idx); }
                        if (c == 0) { ringStructure.setOuterRing(ringList); }
                        else { ringStructure.addInnerRing(ringList); }
                        c++;
                    }
                    ringCollection.Add(ringStructure);
                }
                else
                {
                    foreach (var JBoundary in JBoundaryList)
                    {
                        foreach (var ringSet in boundaries2Rings(JBoundary))
                        {
                            ringCollection.Add(ringSet);
                        }
                    }
                }
                return ringCollection;
            }

            public bool hasSurfaceData() { return hasSurfaceData_; }
            public bool hasMaterialData()
            {
                if (surfaceMaterialValues_.Count() > 0)
                {
                    return true;
                }
                return false;
            }
        }

        class CityObject
        {
            string name_ = "";
            string type_ = "";

            List<GeoObject> geometry_ = new List<GeoObject>();
            bool isTemplated_ = false;
            TemplateObject templateObb_; // TODO: allow for multiple templates 

            string originFileName_ = "";

            bool hasGeo_ = false;
            bool isParent_ = false;
            bool isChild_ = false;
            bool hasAttributes_ = false;
            bool isFilteredOut_ = false;

            Dictionary<string, dynamic> attributes_ = new Dictionary<string, dynamic>();
            List<string> parentList_ = new List<string>();
            List<string> childList_ = new List<string>();

            public string getName() { return name_; }
            public void setName(string name) { name_ = validifyString(name); }
            public string getType() { return type_; }
            public void setType(string type) { type_ = type; }
            public List<GeoObject> getGeometry() { return geometry_; }
            public void addGeometry(GeoObject geoObject) { geometry_.Add(geoObject); }
            public void addTemplate(int templateIdx, Point3d anchor)
            {
                isTemplated_ = true;
                templateObb_ = new TemplateObject(templateIdx, anchor);
            }

            public TemplateObject getTemplate() { return templateObb_; }
            public void setOriginalFileName(string path)
            {
                originFileName_ = System.IO.Path.GetFileName(path);
            }
            public string getOriginalFileName() { return originFileName_; }
            public Dictionary<string, dynamic> getAttributes() { return attributes_; }
            public bool hasGeo() { return hasGeo_; }
            public void setHasGeo(bool hasGeo) { hasGeo_ = hasGeo; }
            public bool isParent() { return isParent_; }
            public void setIsParent(bool isParent) { isParent_ = isParent; }
            public bool isChild() { return isChild_; }
            public void setIsChild(bool isChild) { isChild_ = isChild; }
            public bool hasAttributes() { return hasAttributes_; }
            public void setHasAttributes(bool hasAttributes) { hasAttributes_ = hasAttributes; }
            public void setIsFilteredout() { isFilteredOut_ = true; }
            public bool isFilteredout() { return isFilteredOut_; }
            public void setIsTemplated() { isTemplated_ = true; }
            public bool isTemplated() { return isTemplated_; }
            public dynamic getAttribute(string key) { return attributes_[key]; }
            public void addAttribute(string key, dynamic value) { attributes_.Add(key, value); }
            public void setAttributes(dynamic jAttributeList)
            {
                attributes_ = new Dictionary<string, dynamic>();
                if (jAttributeList != null)
                {
                    setHasAttributes(true);
                    foreach (var attribute in jAttributeList)
                    {
                        addAttribute(attribute.Name, attribute.Value);
                    }
                }
            }
            public List<string> getParents() { return parentList_; }
            public void addParent(string parent) { parentList_.Add(parent); } // TODO: string name check?
            public void setParents(dynamic jParentList)
            {
                parentList_ = new List<string>();
                if (jParentList != null)
                {
                    setIsChild(true);
                    foreach (var parent in jParentList)
                    {
                        addParent(parent.ToString());
                    }
                }
            }
            public List<string> getChildren() { return childList_; }
            public void addChild(string child) { childList_.Add(child); }
            public void setChildren(dynamic jChildList)
            {
                childList_ = new List<string>();
                if (jChildList != null)
                {
                    setIsParent(true);
                    foreach (var child in jChildList)
                    {
                        addChild(child.ToString());
                    }
                }
            }

            public Dictionary<string, dynamic> getInheritancedAtt(CityCollection cityCollection)
            {
                if (parentList_.Count == 0) { return new Dictionary<string, dynamic>(); }

                Dictionary<string, dynamic> inheritedAtt = new Dictionary<string, dynamic>();
                foreach (string parentName in parentList_)
                {
                    CityObject parentObject = cityCollection.getObject(parentName);

                    foreach (var item in parentObject.getAttributes())
                    {
                        if (!inheritedAtt.ContainsKey(item.Key))
                        {
                            inheritedAtt.Add(item.Key, item.Value);
                        }
                    }
                }
                return inheritedAtt;
            }

            public string validifyString(string name)
            {
                string tName = "";
                foreach (char c in name)
                {
                    if (c != '{' && c != '}' && c != '?' && c != '@' && c != '/' && c != '\\')
                    {
                        tName += c;
                    }
                }
                return tName;
            }
        }

        class CityCollection
        {
            Dictionary<string, CityObject> objectCollection_ = new Dictionary<string, CityObject>();

            public void add(CityObject cityObject)
            {
                objectCollection_.Add(cityObject.getName(), cityObject);
            }

            public List<CityObject> getFlatColletion()
            {
                List<CityObject> collection = new List<CityObject>();
                foreach (var item in objectCollection_)
                {
                    var itemValues = item.Value;
                    if (!itemValues.isFilteredout())
                    {
                        collection.Add(item.Value);
                    }
                }
                return collection;
            }

            public Dictionary<string, CityObject> getCollection() { return objectCollection_; }
            public CityObject getObject(string objectName) { return objectCollection_[objectName]; }
        }
    }
}
