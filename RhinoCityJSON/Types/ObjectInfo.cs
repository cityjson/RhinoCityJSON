using Rhino.Geometry;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace RhinoCityJSON.Types
{
    public class ObjectInfo
    {
        // data for objects
        string objectName_ = "";
        string objectType_ = "";
        List<string> objectParents_ = new List<string>();
        List<string> objectChildren_ = new List<string>();

        string originFileName_ = "";

        // data for templates
        int templateIdx_;
        Point3d templateObjectAnchor_;

        // data for surfaces
        string surfaceGeoName_ = "";
        string surfaceGeoType_ = "";
        string surfaceLod_ = "";
        string surfaceType_ = "";

        KeyValuePair<string, string> materialIdxPair_ = new KeyValuePair<string, string>("", DefaultValues.defaultNoneValue);
        bool hasMaterial = false;

        // data for both
        Dictionary<string, string> otherData_ = new Dictionary<string, string>();

        //splitter
        bool isObject_ = false;
        bool isSurface_ = false;
        bool isTemplate_ = false;

        public ObjectInfo() { }

        public ObjectInfo(ObjectInfo other)
        {
            objectName_ = other.objectName_;
            objectType_ = other.objectType_;
            objectParents_ = other.objectParents_;
            objectChildren_ = other.objectChildren_;
            originFileName_ = other.originFileName_;
            templateIdx_ = other.templateIdx_;
            templateObjectAnchor_ = other.templateObjectAnchor_;
            surfaceGeoName_ = other.surfaceGeoName_;
            surfaceGeoType_ = other.surfaceGeoType_;
            surfaceLod_ = other.surfaceLod_;
            surfaceType_ = other.surfaceType_;
            otherData_ = new Dictionary<string, string>(other.otherData_);
            isSurface_ = other.isSurface_;
            isTemplate_ = other.isTemplate_;
            isObject_ = other.isObject_;
            materialIdxPair_ = other.materialIdxPair_;
        }

        public ObjectInfo( // normal object
            string name,
            string type,
            List<string> parents,
            List<string> children,
            string originFileName,
            Dictionary<string, string> otherData
            )
        {
            isObject_ = true;
            objectName_ = name;
            objectType_ = type;
            objectParents_ = parents;
            objectChildren_ = children;
            originFileName_ = originFileName;
            otherData_ = new Dictionary<string, string>(otherData);
        }

        public ObjectInfo( // template object
            string name,
            string type,
            int templateIdx,
            Point3d anchor,
            List<string> parents,
            List<string> children,
            string originFileName,
            Dictionary<string, string> otherData
            )
        {
            isTemplate_ = true;
            objectName_ = name;
            objectType_ = type;
            originFileName_ = originFileName;
            templateIdx_ = templateIdx;
            templateObjectAnchor_ = anchor;
            objectParents_ = parents;
            objectChildren_ = children;
            otherData_ = new Dictionary<string, string>(otherData);
        }

        public ObjectInfo( // surface object
            string geoName,
            string geoType,
            string lod,
            string surfaceType,
            string parentName,
            string originFileName,
            Dictionary<string, string> otherData
            )
        {
            isSurface_ = true;
            surfaceGeoName_ = geoName;
            surfaceGeoType_ = geoType;
            surfaceLod_ = lod;
            surfaceType_ = surfaceType;
            objectName_ = parentName;
            originFileName_ = originFileName;
            otherData_ = new Dictionary<string, string>(otherData);
        }

        public ObjectInfo( // surface template object
            int templateIdx,
            string geoType,
            string lod
            )
        {
            isSurface_ = true;
            isTemplate_ = true;
            templateIdx_ = templateIdx;
            surfaceGeoType_ = geoType;
            surfaceLod_ = lod;
        }

        public string getName() { return objectName_; }
        public void setName(string n) { objectName_ = n; }
        public string getGeoName() { return surfaceGeoName_; }
        public void setGeoName(string name) { surfaceGeoName_ = name; }
        public string getObjectType() { return objectType_; }
        public void setObjectType(string type) { objectType_ = type; }
        public string getGeoType() { return surfaceGeoType_; }
        public void setGeoType(string type) { surfaceGeoType_ = type; }
        public string getLod() { return surfaceLod_; }
        public void setLod(string lod) { surfaceLod_ = lod; } 
        public List<string> getParents() { return objectParents_; }
        public void setParents(List<string> parents) { objectParents_ = parents; }
        public List<string> getChildren() { return objectChildren_; }
        public void setChildren(List<string> children) { objectChildren_ = children; }
        public string getOriginalFileName() { return originFileName_; }
        public void setOriginalFileName(string name) { originFileName_ = name; }
        public void addMaterial(string materialName, string MaterialIdx)
        {
            materialIdxPair_ = new KeyValuePair<string, string>(materialName, MaterialIdx);
            hasMaterial = true;
        }
        public KeyValuePair<string, string> getMaterial() { return materialIdxPair_; }
        public int getTemplateIdx() { return templateIdx_; }
        public void setTemplaceIdx(int i) { templateIdx_ = i; }
        public Point3d getAnchor() { return templateObjectAnchor_; }
        public void setAnchor(Point3d p) { templateObjectAnchor_ = p; }
        public Dictionary<string, string> getOtherData() { return otherData_; }
        public void addOtherData(string k, string s) {

            if (otherData_.ContainsKey(k))
            {
                otherData_[k] = s;
                return;
            }

            otherData_.Add(k, s);
        }
        public bool isSurface() { return isSurface_; }
        public void setIsSurface(bool t) { isSurface_ = t; }
        public bool isTemplate() { return isTemplate_; }
        public bool isObject() { return isObject_; }
        public void setIsObject(bool b) { isObject_ = b; }
        public void removeOtherData(string k)
        {
            if (otherData_.ContainsKey(k))
            {
                otherData_.Remove(k);
            }
        }

        public bool isValid()
        {
            if (objectName_ != "" && objectType_ != "")
            {
                return true;
            }
            return false;
        }

        public Dictionary<int, string> fetchIdxDict()
        {
            Dictionary<int, string> filterLookup = new Dictionary<int, string>();
            if (isObject() && isSurface())
            {
                for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.objectKeys[i]);
                }
                for (int i = 1; i < DefaultValues.surfaceObjectKeys.Count; i++) // pass over 0 to aviod dublicate name call
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.surfaceObjectKeys[i]);
                }
            }
            else if (isSurface() && isTemplate())
            {
                for (int i = 0; i < DefaultValues.surfaceTemplateKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.surfaceTemplateKeys[i]);
                }
            }
            else if (isObject())
            {
                for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.objectKeys[i]);
                }
            }
            else if (isSurface())
            {
                for (int i = 0; i < DefaultValues.surfaceObjectKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.surfaceObjectKeys[i]);
                }
            }
            else if (isTemplate())
            {
                for (int i = 0; i < DefaultValues.templateKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.templateKeys[i]);
                }
            }

            string[] firstItemKeys = getOtherData().Keys.ToArray();
            for (int i = 0; i < firstItemKeys.Length; i++)
            {
                filterLookup.Add(filterLookup.Count, firstItemKeys[i]);
            }
            return filterLookup;
        }

        public List<string> getItemByIdx(int idx)
        {
            if (isObject() && isSurface())
            {
                if (idx == 0) { return new List<string>() { getName() }; }
                if (idx == 1) { return new List<string>() { getObjectType() }; }
                if (idx == 2)
                {
                    List<string> parentNames = new List<string>();
                    foreach (var parent in getParents()) { parentNames.Add(parent); }
                    return parentNames;
                }
                if (idx == 3)
                {
                    List<string> childNames = new List<string>();
                    foreach (var parent in getChildren()) { childNames.Add(parent); }
                    return childNames;
                }
                if (idx == 4) { return new List<string>() { getGeoType() }; }
                if (idx == 5) { return new List<string>() { getGeoName() }; }
                if (idx == 6) { return new List<string>() { getLod() }; }
                if (idx == 7) { return new List<string>() { getMaterial().Value }; }
            }
            else if (isTemplate() && isSurface())
            {
                if (idx == 0) { return new List<string>() { getTemplateIdx().ToString() }; }
                if (idx == 1) { return new List<string>() { getGeoType() }; }
                if (idx == 2) { return new List<string>() { getLod() }; }
                if (idx == 3) { return new List<string>() { getMaterial().Value }; }
            }
            else if (isObject())
            {
                if (idx == 0) { return new List<string>() { getName() }; }
                if (idx == 1) { return new List<string>() { getObjectType() }; }
                if (idx == 2)
                {
                    List<string> parentNames = new List<string>();
                    foreach (var parent in getParents()) { parentNames.Add(parent); }
                    return parentNames;
                }
                if (idx == 3)
                {
                    List<string> childNames = new List<string>();
                    foreach (var parent in getChildren()) { childNames.Add(parent); }
                    return childNames;
                }
            }
            else if (isSurface())
            {
                if (idx == 0) { return new List<string>() { getName() }; }
                if (idx == 1) { return new List<string>() { getGeoType() }; }
                if (idx == 2) { return new List<string>() { getGeoName() }; }
                if (idx == 3) { return new List<string>() { getLod() }; }
                if (idx == 4) { return new List<string>() { getMaterial().Value }; }
            }
            else if (isTemplate())
            {
                if (idx == 0) { return new List<string>() { getName() }; }
                if (idx == 1) { return new List<string>() { getObjectType() }; }
                if (idx == 2)
                {
                    List<string> parentNames = new List<string>();
                    foreach (var parent in getParents()) { parentNames.Add(parent); }
                    return parentNames;
                }
                if (idx == 3)
                {
                    List<string> childNames = new List<string>();
                    foreach (var parent in getChildren()) { childNames.Add(parent); }
                    return childNames;
                }
                if (idx == 4) { return new List<string>() { getTemplateIdx().ToString() }; }
                if (idx == 5) { return new List<string>() { getAnchor().ToString() }; }
            }

            int adjustedIdx = idx;
            if (isObject() && isSurface()) { adjustedIdx = idx - DefaultValues.objectKeysSize - DefaultValues.surfaceObjectKeysSize + 1; }
            else if (isObject()) { adjustedIdx = idx - DefaultValues.objectKeysSize; }
            else if (isSurface()) { adjustedIdx = idx - DefaultValues.surfaceObjectKeysSize; }
            else if (isTemplate()) { adjustedIdx = idx - DefaultValues.templateKeysSize; }
            //else return new List<string>();

            string[] keys = getOtherData().Keys.ToArray();
            string reqKey = keys[adjustedIdx];

            return new List<string>() { getOtherData()[reqKey] };
        }

        public Types.ObjectInfo removeItemByIndex(int idx)
        {
            bool found = true;
            if (isObject() && isSurface()) // TODO: add warnings when deleting things
            {
                if (idx > 8) { found = false; }
                else if (idx == 0) { }
                else if (idx == 1) { objectType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 2) { objectParents_ = new List<string>(); }
                else if (idx == 3) { objectChildren_ = new List<string>(); }
                else if (idx == 4) { objectName_ = DefaultValues.defaultNoneValue; }
                else if (idx == 5) { surfaceGeoType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 6) { surfaceGeoName_ = DefaultValues.defaultNoneValue; }
                else if (idx == 7) { surfaceLod_ = DefaultValues.defaultNoneValue; }
                else if (idx == 8) { materialIdxPair_ = new KeyValuePair<string, string>("", DefaultValues.defaultNoneValue); }
            }
            else if (isTemplate() && isSurface())
            {
                if (idx > 3) { found = false; }
                if (idx == 0) { templateIdx_ = new int(); }
                else if (idx == 1) { surfaceGeoType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 2) { surfaceLod_ = DefaultValues.defaultNoneValue; }
                else if (idx == 3) { materialIdxPair_ = new KeyValuePair<string, string>("", DefaultValues.defaultNoneValue); }
            }
            else if (isObject())
            {
                if (idx > 3) { found = false; }
                if (idx == 0) { objectName_ = DefaultValues.defaultNoneValue; }
                else if (idx == 1) { objectType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 2) { objectParents_ = new List<string>(); }
                else if (idx == 3) { objectChildren_ = new List<string>(); }
            }
            else if (isSurface())
            {
                if (idx > 4) { found = false; }
                if (idx == 0) { objectName_ = DefaultValues.defaultNoneValue; }
                else if (idx == 1) { surfaceGeoType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 2) { surfaceGeoType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 3) { surfaceLod_ = DefaultValues.defaultNoneValue; }
                else if (idx == 4) { materialIdxPair_ = new KeyValuePair<string, string>("", DefaultValues.defaultNoneValue); }
            }
            else if (isTemplate())
            {
                if (idx > 5) { found = false; }
                if (idx == 0) { objectName_ = DefaultValues.defaultNoneValue; }
                else if (idx == 1) { objectType_ = DefaultValues.defaultNoneValue; }
                else if (idx == 2) { objectParents_ = new List<string>(); }
                else if (idx == 3) { objectChildren_ = new List<string>(); }
                else if (idx == 4) { templateIdx_ = new int(); }
                else if (idx == 5) { templateObjectAnchor_ = new Point3d(); }
            }
            if (!found)
            {

                int adjustedIdx = idx;
                if (isObject() && isSurface()) { adjustedIdx = idx - DefaultValues.objectKeysSize - DefaultValues.surfaceObjectKeysSize; }
                else if (isObject()) { adjustedIdx = idx - DefaultValues.objectKeysSize; }
                else if (isSurface()) { adjustedIdx = idx - DefaultValues.surfaceObjectKeysSize; }
                else if (isTemplate()) { adjustedIdx = idx - DefaultValues.templateKeysSize; }
                //else return new List<string>();

                string[] keys = getOtherData().Keys.ToArray();
                string reqKey = keys[adjustedIdx];

                otherData_.Remove(reqKey);
            }

            return new ObjectInfo(this);
        }
    }

    public class GHObjectInfo : GH_Goo<ObjectInfo>
    {
        public GHObjectInfo(ObjectInfo objectInfoObject)
        {
            this.Value = new ObjectInfo(
                new ObjectInfo(objectInfoObject));
        }

        public GHObjectInfo(GHObjectInfo other)
        {
            this.Value = new ObjectInfo(other.Value);
        }

        public override string TypeName => "CJItemInformation";

        public override string TypeDescription => "An instance of a CityJSON Item Information";

        public override bool IsValid => Value.isValid();

        public override IGH_Goo Duplicate() => new GHObjectInfo(this);

        public override string ToString() => "CityJSON Item Information";
    }

}
