using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
