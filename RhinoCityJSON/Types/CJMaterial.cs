using System.Linq;
using Grasshopper.Kernel.Types;

namespace RhinoCityJSON
{
    public class Material
    {
        string name_ = "";
        double ambientIntensity_ = 0;
        double[] diffuseColor_ = new double[3] { -1, -1, -1 };
        double[] emissiveColor_ = new double[3] { -1, -1, -1 };
        double[] specularColor_ = new double[3] { -1, -1, -1 };
        double shininess_ = 0;
        double transparency_ = 0;
        bool isSmooth_ = false;

        public Material() { }

        public Material(string name)
        {
            name_ = name;
        }

        public Material(Material other)
        {
            name_ = other.name_;
            ambientIntensity_ = other.ambientIntensity_;
            diffuseColor_ = other.diffuseColor_;
            emissiveColor_ = other.emissiveColor_;
            specularColor_ = other.specularColor_;
            shininess_ = other.shininess_;
            transparency_ = other.transparency_;
            isSmooth_ = other.isSmooth_;
        }

        public Material(
            string materialName,
            double ambientInternsity,
            double[] diffuseColor,
            double[] emissiveColor,
            double[] specularColor,
            double shininess,
            double transparency,
            bool isSmooth
            )
        {
            name_ = materialName;
            ambientIntensity_ = ambientInternsity;
            diffuseColor_ = diffuseColor;
            emissiveColor_ = emissiveColor;
            specularColor_ = specularColor;
            shininess_ = shininess;
            transparency_ = transparency;
            isSmooth_ = isSmooth;
        }

        public string getName() { return name_; }
        public double getAmbient() { return ambientIntensity_; }
        public double[] getDifColor() { return diffuseColor_; }
        public double[] getemColor() { return emissiveColor_; }
        public double[] getspeColor() { return specularColor_; }
        public double getshine() { return shininess_; }
        public double getTransparency() { return transparency_; }

        public bool getIsSmooth() { return isSmooth_; }

        public bool isValid()
        {
            if (name_ == "") { return false;}

            if (ambientIntensity_ > 1 || ambientIntensity_ < 0) { return false; }

            if (diffuseColor_.Count() > 3 || emissiveColor_.Count() > 3 || specularColor_.Count() > 3) { return false; }

            for (int i = 0; i < 3; i++)
            {
                if (diffuseColor_[i] < 0 || diffuseColor_[i] > 1) { return false; }
            }

            int negEm = 0;
            int negSp = 0;
            for (int i = 0; i < 3; i++)
            {
                if (emissiveColor_[i] == -1) { negEm++; }
                if (specularColor_[i] == -1) { negSp++; }
            }

            if (negEm != 0 && negEm != 3 || negSp !=0 && negSp != 3) { return false; }

            if (negEm > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (emissiveColor_[i] < 0 || emissiveColor_[i] > 1) { return false; }
                }
            }

            if (negSp > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (specularColor_[i] < 0 || specularColor_[i] > 1) { return false; }
                }
            }

            if (shininess_ < 0 || shininess_ > 1) { return false; }

            if (transparency_ < 0 || transparency_ > 1) { return false; }

            return true;
        }
    }


    public class GHMaterial : GH_Goo<Material>
    {
        public GHMaterial(Material materialObject)
        {
            this.Value = new Material(
                new Material(materialObject));
        }

        public GHMaterial(GHMaterial other)
        {
            this.Value = new Material(other.Value);
        }

        public override string TypeName => "CJMaterial";

        public override string TypeDescription => "An instance of a CityJSON material";

        public override bool IsValid => Value.isValid();

        public override IGH_Goo Duplicate() => new GHMaterial(this);

        public override string ToString() => "CityJSON Material";
    }

    


}
