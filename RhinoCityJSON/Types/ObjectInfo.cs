
using System.Collections.Generic;
using Grasshopper.Kernel.Types;

namespace RhinoCityJSON.Types
{
    public class ObjectInfo
    {
        // data for objects
        string name_ = "";
        string type_ = "";
        List<string> parents_ = new List<string>();
        List<string> children_ = new List<string>();

        // data for surfaces
        string GeoName_ = "";
        string GeoType_ = "";
        string lod_ = "";
        string surfaceType_ = "";

        // data for both
        Dictionary<string, string> otherData_ = new Dictionary<string, string>();

        //splitter
        bool isSurface_ = false;

        public ObjectInfo() { }

        public ObjectInfo(ObjectInfo other)
        {
            name_ = other.name_;
            type_ = other.type_;
            parents_ = other.parents_;
            children_ = other.children_;
            GeoName_ = other.GeoName_;
            GeoType_ = other.GeoType_;
            lod_ = other.lod_;
            surfaceType_ = other.surfaceType_;
            otherData_ = other.otherData_;
            isSurface_ = other.isSurface_;
        }

        public ObjectInfo(
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

        public ObjectInfo(
            string geoName,
            string geoType,
            string lod,
            string surfaceType,
            string parentName,
            Dictionary<string, string> otherData
            )
        {
            GeoName_ = geoName;
            GeoType_ = geoType;
            lod_ = lod;
            surfaceType_ = surfaceType;
            name_ = parentName;
            otherData_ = otherData;
            isSurface_ = true;
        }

        public string getName() { return name_; }
        public string getGeoName() { return GeoName_; }
        public string getObjectType() { return type_; }
        public string getGeoType() { return GeoType_; }
        public string getLod() { return lod_; }
        public List<string> getParents() { return parents_; }
        public List<string> getChildren() { return children_; }
        public Dictionary<string, string> getOtherData() { return otherData_; }
        public void addOtherData(string k, string s) { otherData_.Add(k, s); }
        public bool isSurface() { return isSurface_; }
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
