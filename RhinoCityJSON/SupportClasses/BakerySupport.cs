using System.Collections.Generic;
using Rhino.Geometry;

namespace RhinoCityJSON
{
    class BakerySupport
    {
        static public string getParentName(string Childname)
        {
            if (
                Childname == "BridgePart" ||
                Childname == "BridgeInstallation" ||
                Childname == "BridgeConstructiveElement" ||
                Childname == "BrideRoom" ||
                Childname == "BridgeFurniture"
                )
            {
                return "Bridge";
            }
            else if (
                Childname == "BuildingPart" ||
                Childname == "BuildingInstallation" ||
                Childname == "BuildingConstructiveElement" ||
                Childname == "BuildingFurniture" ||
                Childname == "BuildingStorey" ||
                Childname == "BuildingRoom" ||
                Childname == "BuildingUnit"
                )
            {
                return "Building";
            }
            else if (
                Childname == "TunnelPart" ||
                Childname == "TunnelInstallation" ||
                Childname == "TunnelConstructiveElement" ||
                Childname == "TunnelHollowSpace" ||
                Childname == "TunnelFurniture"
                )
            {
                return "Tunnel";
            }
            else
            {
                return Childname;
            }
        }

        static public Dictionary<string, System.Drawing.Color> getTypeColor()
        {
            return new Dictionary<string, System.Drawing.Color>
            {
                { "Bridge", System.Drawing.Color.Gray },
                { "Building", System.Drawing.Color.LightBlue },
                { "CityFurniture", System.Drawing.Color.Red },
                { "LandUse", System.Drawing.Color.FloralWhite },
                { "OtherConstruction", System.Drawing.Color.White },
                { "PlantCover", System.Drawing.Color.Green },
                { "SolitaryVegetationObject", System.Drawing.Color.Green },
                { "TINRelief", System.Drawing.Color.LightYellow},
                { "TransportationSquare", System.Drawing.Color.Gray},
                { "Road", System.Drawing.Color.Gray},
                { "Tunnel", System.Drawing.Color.Gray},
                { "WaterBody", System.Drawing.Color.MediumBlue},
                { "+GenericCityObject", System.Drawing.Color.White},
                { "Railway", System.Drawing.Color.DarkGray},
            };
        }

        static public Dictionary<string, System.Drawing.Color> getSurfColor()
        {
            return new Dictionary<string, System.Drawing.Color>{
                { "GroundSurface", System.Drawing.Color.Gray },
                { "WallSurface", System.Drawing.Color.LightBlue },
                { "RoofSurface", System.Drawing.Color.Red }
            };
        }

        static public Point3d getAnchorPoint(List<Brep> brepGroup) // lll point is seen as anchor
        {
            Point3d anchor = brepGroup[0].Vertices[0].Location;
            bool isSet = false;
            foreach (Brep surface in brepGroup)
            {
                var vertList = surface.Vertices;
                foreach (var vert in vertList)
                {
                    Point3d location = vert.Location;
                    if (location.X < anchor.X) { anchor.X = location.X; }
                    if (location.Y < anchor.Y) { anchor.Y = location.Y; }
                    if (location.Z < anchor.Z) { anchor.Z = location.Z; }
                }
            }
            return anchor;
        }

        static public int makeParentLayer(string layerBaseName)
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            // create a new unique master layer name
            Rhino.DocObjects.Layer parentlayer = new Rhino.DocObjects.Layer();
            parentlayer.Name = layerBaseName;
            parentlayer.Color = System.Drawing.Color.Red;
            parentlayer.Index = 100;

            // if the layer already exists find a new name
            int count = 0;
            if (activeDoc.Layers.FindName(layerBaseName) != null)
            {
                while (true)
                {
                    if (activeDoc.Layers.FindName(layerBaseName + " - " + count.ToString()) == null)
                    {
                        parentlayer.Name = layerBaseName + " - " + count.ToString();
                        parentlayer.Index = parentlayer.Index + count;
                        break;
                    }
                    count++;
                }
            }

            return activeDoc.Layers.Add(parentlayer);
        }

        static public List<string> getUniqueLoDList(
            int lodIdx,
            IList<List<Grasshopper.Kernel.Types.IGH_Goo>> branchCollection
            )
        {
            var lodList = new List<string>();
            for (int i = 0; i < branchCollection.Count; i++)
            {
                // get LoD
                string lod = branchCollection[i][lodIdx].ToString();

                if (!lodList.Contains(lod))
                {
                    lodList.Add(lod);
                }
            }
            return lodList;
        }

        static public Dictionary<string, List<string>> getUniqueTypeLod(
            int lodIdx,
            int typeIdx,
            IList<List<Grasshopper.Kernel.Types.IGH_Goo>> branchCollection
            )
        {
            var lodTypeDictionary = new Dictionary<string, List<string>>();
            for (int i = 0; i < branchCollection.Count; i++)
            {
                // get building types present in input per LoD
                string lod = branchCollection[i][lodIdx].ToString();
                string bType = branchCollection[i][typeIdx].ToString();

                if (!lodTypeDictionary.ContainsKey(lod))
                {
                    lodTypeDictionary.Add(lod, new List<string>());
                    lodTypeDictionary[lod].Add(bType);

                }
                else if (!lodTypeDictionary[lod].Contains(bType))
                {
                    lodTypeDictionary[lod].Add(bType);
                }
            }
            return lodTypeDictionary;
        }

        static public Dictionary<string, List<string>> getUniqueSurfaceLod(
            int lodIdx,
            int surfTypeIdx,
            IList<List<Grasshopper.Kernel.Types.IGH_Goo>> branchCollection
            )
        {
            var lodSurfTypeDictionary = new Dictionary<string, List<string>>();
            for (int i = 0; i < branchCollection.Count; i++)
            {
                string lod = branchCollection[i][lodIdx].ToString();

                // get surface types present in input
                string sType = branchCollection[i][surfTypeIdx].ToString();

                if (!lodSurfTypeDictionary.ContainsKey(lod))
                {
                    lodSurfTypeDictionary.Add(lod, new List<string>());
                    lodSurfTypeDictionary[lod].Add(sType);

                }
                else if (!lodSurfTypeDictionary[lod].Contains(sType))
                {
                    lodSurfTypeDictionary[lod].Add(sType);
                }
            }
            return lodSurfTypeDictionary;
        }

       
        static public void createLayers(
            string layerBaseName,
            int lodIdx,
            int typeIdx,
            int surfTypeIdx,
            IList<List<Grasshopper.Kernel.Types.IGH_Goo>> branchCollection,
            ref Dictionary<string, System.Guid> lodIdLookup,
            ref Dictionary<string, Dictionary<string, int>> typIdLookup,
            ref Dictionary<string, Dictionary<string, int>> surIdLookup
            )
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            // get the lod lvls and types and surface types present in the surfaces 
            var lodTypeDictionary = getUniqueTypeLod(lodIdx, typeIdx, branchCollection);
            var lodList = getUniqueLoDList(lodIdx, branchCollection);
            var lodSurfTypeDictionary = getUniqueSurfaceLod(lodIdx, surfTypeIdx, branchCollection);
            var parentID = activeDoc.Layers.FindIndex(makeParentLayer(layerBaseName));

            // create the layers
            createLoDLayers(
                lodList,
                ref lodIdLookup,
                ref typIdLookup,
                ref surIdLookup,
                parentID
                );

            createTypeLayers(
                ref lodIdLookup,
                ref typIdLookup,
                ref lodTypeDictionary,
                BakerySupport.getTypeColor()
                );

            if (surfTypeIdx != -1)
            {
                createSurfaceLayers(
                    lodSurfTypeDictionary,
                    ref typIdLookup,
                    ref surIdLookup,
                    BakerySupport.getSurfColor()
                    );
            }
        }

        static public void createLodLayer(
            string lodString,
            ref Dictionary<string, System.Guid> lodIdLookup,
            ref Dictionary<string, Dictionary<string, int>> typIdLookup,
            ref Dictionary<string, Dictionary<string, int>> surIdLookup,
            Rhino.DocObjects.Layer parentLayer
            )
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;
            Rhino.DocObjects.Layer lodLayer = new Rhino.DocObjects.Layer();
            lodLayer.Name = "LoD " + lodString;
            lodLayer.Color = System.Drawing.Color.DarkRed;
            lodLayer.Index = 200 + lodIdLookup.Count;
            lodLayer.ParentLayerId = parentLayer.Id;

            var id = activeDoc.Layers.Add(lodLayer);
            var idx = activeDoc.Layers.FindIndex(id).Id;
            lodIdLookup.Add(lodString, idx);
            typIdLookup.Add(lodString, new Dictionary<string, int>());
            surIdLookup.Add(lodString, new Dictionary<string, int>());
        }

        static public void createLoDLayers(
            List<string> lodList,
            ref Dictionary<string, System.Guid> lodIdLookup,
            ref Dictionary<string, Dictionary<string, int>> typIdLookup,
            ref Dictionary<string, Dictionary<string, int>> surIdLookup,
            Rhino.DocObjects.Layer parentLayer
            )
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            for (int i = 0; i < lodList.Count; i++)
            {
                createLodLayer(
                    lodList[i],
                    ref lodIdLookup,
                    ref typIdLookup,
                    ref surIdLookup,
                    parentLayer
                    );
            }
        }

        static public List<string> getCleanedTypes(List<string> objectTypeList)
        {
            var cleanedTypeList = new List<string>();
            foreach (var bType in objectTypeList)
            {
                var filteredName = BakerySupport.getParentName(bType);

                if (!cleanedTypeList.Contains(filteredName))
                {
                    cleanedTypeList.Add(filteredName);
                }
            }
            return cleanedTypeList;
        }

        // Creates the type layers in the rhino file and creates a lookup dictionary
        static public void createTypeLayers(
            ref Dictionary<string, System.Guid> lodIdLookup,
            ref Dictionary<string, Dictionary<string, int>> typIdLookup,
            ref Dictionary<string, List<string>> lodTypeDictionary,
            Dictionary<string, System.Drawing.Color> typColor
            )
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            foreach (var lodTypeLink in lodTypeDictionary)
            {
                var targeLId = lodIdLookup[lodTypeLink.Key];
                var cleanedTypeList = getCleanedTypes(lodTypeLink.Value);

                foreach (var bType in cleanedTypeList)
                {
                    Rhino.DocObjects.Layer typeLayer = new Rhino.DocObjects.Layer();
                    typeLayer.Name = bType;

                    System.Drawing.Color lColor = System.Drawing.Color.DarkRed;
                    try
                    {
                        lColor = typColor[bType];
                    }
                    catch
                    {
                        continue;
                    }

                    typeLayer.Color = lColor;
                    typeLayer.ParentLayerId = targeLId;

                    var idx = activeDoc.Layers.Add(typeLayer);
                    typIdLookup[lodTypeLink.Key].Add(bType, idx);
                }
            }
        }

        // Creates the surface layers in the rhino file and creates a lookup dictionary
        static public void createSurfaceLayers(
              Dictionary<string, List<string>> lodSurfTypeDictionary,
              ref Dictionary<string, Dictionary<string, int>> typIdLookup,
              ref Dictionary<string, Dictionary<string, int>> surIdLookup,
              Dictionary<string, System.Drawing.Color> surfColor
        )
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            foreach (var lodTypeLink in lodSurfTypeDictionary)
            {
                var targeLId = activeDoc.Layers.FindIndex(typIdLookup[lodTypeLink.Key]["Building"]).Id;
                var cleanedSurfTypeList = getCleanedTypes(lodTypeLink.Value);

                foreach (var sType in cleanedSurfTypeList)
                {
                    Rhino.DocObjects.Layer surfTypeLayer = new Rhino.DocObjects.Layer();
                    surfTypeLayer.Name = sType;

                    System.Drawing.Color lColor = System.Drawing.Color.DarkRed;
                    try
                    {
                        lColor = surfColor[sType];
                    }
                    catch
                    {
                        continue;
                    }

                    surfTypeLayer.Color = lColor;
                    surfTypeLayer.ParentLayerId = targeLId;

                    var idx = activeDoc.Layers.Add(surfTypeLayer);
                    surIdLookup[lodTypeLink.Key].Add(sType, idx);
                }
            }
        }
    }
}
