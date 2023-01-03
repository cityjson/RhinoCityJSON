using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoCityJSON.Components
{
    public class ExplodeMaterial : GH_Component
    {
        public ExplodeMaterial()
          : base("Explode Material", "!Material",
              "Decomposits a material to its components",
              "RhinoCityJSON", "Processing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Materials", "M", "materials stored in the files", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("ambientIntensity", "kA", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("diffuseColor", "kD", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("emissiveColor", "kE", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("specularColor", "kS", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("shininess", "S", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("transparency", "T", "Name of the material", GH_ParamAccess.list);
            pManager.AddTextParameter("isSmooth", "iS", "Name of the material", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var materialList = new List<GHMaterial>();
            DA.GetDataList(0, materialList);

            List<string> nameList = new List<string>();
            List<string> ambientList = new List<string>();
            List<string> diffuseList = new List<string>();
            List<string> emissiveList = new List<string>();
            List<string> specularList = new List<string>();
            List<string> shinyList = new List<string>();
            List<string> transparencyList = new List<string>();
            List<string> smoothList = new List<string>();

            foreach (var materialObject in materialList)
            {
                var materialValue = materialObject.Value;

                nameList.Add(materialValue.getName());

                double ambientIntensity = materialValue.getAmbient();
                if (ambientIntensity == -1) { ambientList.Add(DefaultValues.defaultNoneValue); }
                else { ambientList.Add(ambientIntensity.ToString()); }

                diffuseList.Add(ReaderSupport.arrayToString(materialValue.getDifColor()));
                emissiveList.Add(ReaderSupport.arrayToString(materialValue.getemColor()));
                specularList.Add(ReaderSupport.arrayToString(materialValue.getspeColor()));

                double shiny = materialValue.getshine();
                if (shiny == -1) { shinyList.Add(DefaultValues.defaultNoneValue); }
                else { shinyList.Add(shiny.ToString()); }

                double transparency = materialValue.getTransparency();
                if (transparency == -1) { transparencyList.Add(DefaultValues.defaultNoneValue); }
                else { transparencyList.Add(transparency.ToString()); }

                smoothList.Add(materialValue.getIsSmooth().ToString());
            }

            DA.SetDataList(0, nameList);
            DA.SetDataList(1, ambientList);
            DA.SetDataList(2, diffuseList);
            DA.SetDataList(3, emissiveList);
            DA.SetDataList(4, specularList);
            DA.SetDataList(5, shinyList);
            DA.SetDataList(6, transparencyList);
            DA.SetDataList(7, smoothList);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return RhinoCityJSON.Properties.Resources.ematerialicon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2384c9a-18ae-4eb3-aeb3-f76e8a274e40"); }
        }
    }
}
