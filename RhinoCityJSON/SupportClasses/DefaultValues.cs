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
        multipleDomain,
        surfaceCreation,
        emptyPath,
        invalidPath,
        invalidLod,
        noLod,
        noBType,
        noScale,
        noSurfaceData,
        noBuildingData,
        noSurfaceBuildingMatch,
        invalidJSON,
        noTeamplateFound,
        noMetaDataFound,
        noMaterialsFound,
        noGeoFound,
        requiresNorth,
        unevenFilterInput,
        incorrectSetComponent,
        largeFile,
        noObject,
        unevenPathInput,
        outputDirNotReal,
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
            {errorCodes.multipleDomain, "Multiple domain shapes submitted"},
            {errorCodes.surfaceCreation, "Not all surfaces have been correctly created"},
            {errorCodes.emptyPath, "Path is empty"},
            {errorCodes.invalidPath, "No valid filepath found"},
            {errorCodes.invalidLod, "Invalid lod input found"},
            {errorCodes.noLod, "No lod data is supplied"},
            {errorCodes.noBType, "No Object type data is supplied"},
            {errorCodes.noScale, "Rhino document scale is not supported, defaulted to unit 1"},
            {errorCodes.noSurfaceData, "No surface data could be found"},
            {errorCodes.noBuildingData, "No building data could be found"},
            {errorCodes.noSurfaceBuildingMatch, "No matching parent building of surface could be found"},
            {errorCodes.invalidJSON, "Invalid CityJSON file"},
            {errorCodes.noTeamplateFound, "No templated objects were found"},
            {errorCodes.noMetaDataFound, "No metadata found"},
            {errorCodes.noMaterialsFound, "No materials found"},
            {errorCodes.noGeoFound, "Geometry input is empty"},
            {errorCodes.requiresNorth, "True north rotation only functions if origin is given"},
            {errorCodes.unevenFilterInput, "Object info input is required to be either both null, or both filled"},
            {errorCodes.incorrectSetComponent, "Incorrect settings component is used for this process"},
            {errorCodes.largeFile, "+10k objects attempted to be opened, if desired enable 'allow extremely large files' via the settings component"},
            {errorCodes.noObject, "Object name can not be found in the target file"},
            {errorCodes.unevenPathInput, "Path lists do not have the same lenght"},
            {errorCodes.outputDirNotReal, "output folder does not exist"}
        };
    }

    static class DefaultValues // TODO: put all the default values here
    {
        static public string defaultSurfaceAddition = "Surface ";
        static public string defaultObjectAddition = "Object ";
        static public string defaultMaterialAddition = "Material ";
        static public string defaultInheritanceAddition = "*";
        static public string defaultNoneValue = "None";

        static public string defaultReaderFolder = "0: Reading";
        static public string defaultWritingFolder = "1: Writing";
        static public string defaultbakingFolder = "2: Baking";
        static public string defaultManagerFolder = "3: Attribute managing";
        static public string defaultProcessingFolder = "4: Processing";

        static public List<string> surfaceObjectKeys = new List<string>()
        {
            "Object Name",
            "Geometry Type",
            "Geometry Super Name",
            "Geometry Name",
            "Geometry LoD",
            "Surface Material"
        };

        static public List<string> surfaceTemplateKeys = new List<string>()
        {
            "Template Idx",
            "Geometry Type",
            "Geometry LoD",
            "Surface Material"
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

        static public int surfaceObjectKeysSize = surfaceObjectKeys.Count;
        static public int surfaceTemplateKeysSize = surfaceTemplateKeys.Count;
        static public int objectKeysSize = objectKeys.Count;
        static public int templateKeysSize = templateKeys.Count;

    }
}
