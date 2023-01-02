using System.Collections.Generic;

namespace RhinoCityJSON
{
    public enum errorCodes
    {
        oversizedAngle,
        noError,
        offline,
        multipleInputSettings,
        multipleOrigins,
        multipleNorth,
        surfaceCreation,
        emptyPath,
        invalidPath,
        invalidLod,
        noLod,
        noBType,
        noScale,
        invalidJSON,
        noTeamplateFound,
        noMetaDataFound,
        noMaterialsFound,
        noGeoFound,
        requiresNorth,
        unevenFilterInput
    }

    static class ErrorCollection // TODO: put all the errors centrally 
    {
        static public Dictionary<errorCodes, string> errorCollection = new Dictionary<errorCodes, string>()
        {
            {errorCodes.oversizedAngle, "True north rotation is larger than 360 degrees"},
            {errorCodes.offline, "Node is offline"},
            {errorCodes.multipleInputSettings, "Only a single settings input allowed"},
            {errorCodes.multipleNorth, "Only a single settings input allowed"},
            {errorCodes.multipleOrigins, "Multiple true north angles submitted"},
            {errorCodes.surfaceCreation, "Not all surfaces have been correctly created"},
            {errorCodes.emptyPath, "Path is empty"},
            {errorCodes.invalidPath, "No valid filepath found"},
            {errorCodes.invalidLod, "Invalid lod input found"},
            {errorCodes.noLod, "No lod data is supplied"},
            {errorCodes.noBType, "No Object type data is supplied"},
            {errorCodes.noScale, "Rhino document scale is not supported, defaulted to unit 1"},
            {errorCodes.invalidJSON, "Invalid CityJSON file"},
            {errorCodes.noTeamplateFound, "No templated objects were found"},
            {errorCodes.noMetaDataFound, "No metadata found"},
            {errorCodes.noMaterialsFound, "No materials found"},
            {errorCodes.noGeoFound, "Geometry input empty"},
            {errorCodes.requiresNorth, "True north rotation only functions if origin is given"},
            {errorCodes.unevenFilterInput, "Object info input is required to be either both null, or both filled"}
        };
    }

    static class DefaultValues // TODO: put all the default values here
    {
        static public string defaultSurfaceAddition = "Surface ";
        static public string defaultObjectAddition = "Object ";
        static public string defaultInheritanceAddition = "*";
        static public string defaultNoneValue = "None";

        static public List<string> surfaceObjectKeys = new List<string>()
        {
            "Object Name",
            "Geometry Type",
            "Geometry Name",
            "Geometry LoD"
        };

        static public List<string> surfaceTemplateKeys = new List<string>()
        {
            "Template Idx",
            "Geometry Type",
            "Geometry LoD"
        };

        static public List<string> objectKeys = new List<string>()
        {
            "Object Name",
            "Object Type",
            "Object Parent",
            "Object Child"
        };

        static public List<string> templateKeys = new List<string>()
        {
            "Object Name",
            "Object Type",
            "Object Parent",
            "Object Child",
            "Template Idx",
            "Object Anchor"
        };

    }
}
