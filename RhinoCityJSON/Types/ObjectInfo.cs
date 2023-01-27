using Rhino.Geometry;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace RhinoCityJSON.Types
{
    public class ObjectInfo
    {
        // data for objects
        string name_ = "";
        string type_ = "";
        List<string> parents_ = new List<string>();
        List<string> children_ = new List<string>();

        // data for templates
        int templateIdx_;
        Point3d objectAnchor_;

        // data for surfaces
        string GeoName_ = "";
        string GeoType_ = "";
        string lod_ = "";
        string surfaceType_ = "";

        // data for both
        Dictionary<string, string> otherData_ = new Dictionary<string, string>();

        //splitter
        bool isSurface_ = false;
        bool isTemplate_ = false;

        public ObjectInfo() { }

        public ObjectInfo(ObjectInfo other)
        {
            name_ = other.name_;
            type_ = other.type_;
            parents_ = other.parents_;
            children_ = other.children_;
            templateIdx_ = other.templateIdx_;
            objectAnchor_ = other.objectAnchor_;
            GeoName_ = other.GeoName_;
            GeoType_ = other.GeoType_;
            lod_ = other.lod_;
            surfaceType_ = other.surfaceType_;
            otherData_ = other.otherData_;
            isSurface_ = other.isSurface_;
            isTemplate_ = other.isTemplate_;
        }

        public ObjectInfo( // normal object
            string name,
            string type,
            List<string> parents,
            List<string> children,
            Dictionary<string, string> otherData
            )
        {
            name_ = name;
            type_ = type;
            parents_ = parents;
            children_ = children;
            otherData_ = otherData;
        }

        public ObjectInfo( // template object
            string name,
            string type,
            int templateIdx,
            Point3d anchor,
            List<string> parents,
            List<string> children,
            Dictionary<string, string> otherData
            )
        {
            isTemplate_ = true;
            name_ = name;
            type_ = type;
            templateIdx_ = templateIdx;
            objectAnchor_ = anchor;
            parents_ = parents;
            children_ = children;
            otherData_ = otherData;
        }

        public ObjectInfo( // surface object
            string geoName,
            string geoType,
            string lod,
            string surfaceType,
            string parentName,
            Dictionary<string, string> otherData
            )
        {
            isSurface_ = true;
            GeoName_ = geoName;
            GeoType_ = geoType;
            lod_ = lod;
            surfaceType_ = surfaceType;
            name_ = parentName;
            otherData_ = otherData;
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
            GeoType_ = geoType;
            lod_ = lod;
        }

        public string getName() { return name_; }
        public string getGeoName() { return GeoName_; }
        public string getObjectType() { return type_; }
        public string getGeoType() { return GeoType_; }
        public string getLod() { return lod_; }
        public List<string> getParents() { return parents_; }
        public List<string> getChildren() { return children_; }
        public int getTemplateIdx() { return templateIdx_; }
        public Point3d getAnchor() { return objectAnchor_; }
        public Dictionary<string, string> getOtherData() { return otherData_; }
        public void addOtherData(string k, string s) { otherData_.Add(k, s); }
        public bool isSurface() { return isSurface_; }
        public bool isTemplate() { return isTemplate_; }
        public void removeOtherData(string k) {
            if (otherData_.ContainsKey(k))
            {
                otherData_.Remove(k);
            }
        }

        public bool isValid()
        {
            if (name_ != "" && type_ != "")
            {
                return true;
            }
            return false;
        }

        public Dictionary<int, string> fetchIdxDict()
        {
            Dictionary<int, string> filterLookup = new Dictionary<int, string>();
            if (isSurface() && isTemplate())
            {
                for (int i = 0; i < DefaultValues.surfaceTemplateKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.surfaceTemplateKeys[i]);
                }
            }
            else if (isSurface())
            {
                for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
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
            else
            {
                for (int i = 0; i < DefaultValues.objectKeys.Count; i++)
                {
                    filterLookup.Add(filterLookup.Count, DefaultValues.objectKeys[i]);
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
            if (isTemplate() && isSurface())
            {
                if (idx == 0) { return new List<string>() { getTemplateIdx().ToString() }; }
                if (idx == 1) { return new List<string>() { getGeoType() }; }
                if (idx == 2) { return new List<string>() { getLod() }; }
            }
            else if (isSurface())
            {
                if (idx == 0) { return new List<string>() { getName() }; }
                if (idx == 1) { return new List<string>() { getGeoType() }; }
                if (idx == 2) { return new List<string>() { getGeoName() }; }
                if (idx == 3) { return new List<string>() { getLod() }; }
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
            else
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

            int adjustedIdx = idx;
            if (isSurface()) { adjustedIdx = idx - DefaultValues.surfaceObjectKeysSize; }
            else if (isTemplate()) { adjustedIdx = idx - DefaultValues.templateKeysSize; }
            else {  adjustedIdx = idx - DefaultValues.objectKeysSize; }

            string[] keys = getOtherData().Keys.ToArray();
            string reqKey = keys[adjustedIdx];

            return new List<string>() { getOtherData()[reqKey] };
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
