using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace RhinoCityJSON
{
    public class RhinoCityJSONInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "RhinoCityJSON";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("12fcfe82-dac9-478f-98c4-2b8f81bb6e78");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
