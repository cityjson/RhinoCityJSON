using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RhinoCityJSON
{
    class FilterSupport
    {
        static public Grasshopper.Kernel.Special.GH_ValueList CreateValueList(GH_DocumentObject GHdocument, GH_Component gH_Component, Types.ObjectInfo firstItem, Dictionary<int, string> filterLookup)
        {  

            Grasshopper.Kernel.Special.GH_ValueList vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.CreateAttributes();

            if (firstItem.isSurface())
            {
                vallist.Name = "Surface keys";
                vallist.NickName = "Surface Filter:";
            }
            else
            {
                vallist.Name = "Object keys";
                vallist.NickName = "Object Filter:";
            }


            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

            int inputcount = 2; //TODO: make smarter
            vallist.Attributes.Pivot = new System.Drawing.PointF((float)GHdocument.Attributes.DocObject.Attributes.Bounds.Left - vallist.Attributes.Bounds.Width - 5, (float)gH_Component.Params.Input[1].Attributes.Bounds.Y + inputcount * 30);
            vallist.ListItems.Clear();

            for (int i = 0; i < filterLookup.Count(); i++)
            {
                vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(filterLookup[i].ToString(), '"' + filterLookup[i].ToString() + '"'));
            }

            vallist.Description = vallist.ListItems.Count.ToString() + "types were found in the input";

            return vallist;

        }


        static public Grasshopper.Kernel.Special.GH_ValueList ReplaceValueList(Grasshopper.Kernel.Special.GH_ValueList vallist, Types.ObjectInfo firstItem, Dictionary<int, string> filterLookup)
        {
            var oldPivot = vallist.Attributes.Pivot;
            if (firstItem.isSurface())
            {
                vallist.Name = "Surface keys";
                vallist.NickName = "Surface Filter:";
            }
            else
            {
                vallist.Name = "Object keys";
                vallist.NickName = "Object Filter:";
            }


            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;
            vallist.Attributes.Pivot = oldPivot;
            vallist.ListItems.Clear();

            for (int i = 0; i < filterLookup.Count(); i++)
            {
                vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(filterLookup[i].ToString(), '"' + filterLookup[i].ToString() + '"'));
            }

            vallist.Description = vallist.ListItems.Count.ToString() + "types were found int the input";
            return vallist;

            //Grasshopper.Instances.ActiveCanvas.Document.AddObject(vallist, false);
        }
    }
}


