# RhinoCityJSONReader
[![GitHub license](https://img.shields.io/github/license/jaspervdv/RhinoCityJSON?style=for-the-badge)](https://github.com/jaspervdv/RhinoCityJSON/blob/master/LICENSE)

A Rhino/Grasshopper plugin allowing the CityJSON format (https://www.cityjson.org/) to be directly used in Rhino 3D and Grasshopper without the loss of semantic information. 
The primary focus of the plugin was to function on the 3D BAG data (https://3dbag.nl/en/viewer). However, the plugin functions as well with other CityJSON files.
The plugin consists out of multiple Grasshopper components exposing data in a format that is directly usable in grasshopper, and allows all this data to be baked into Rhino.

![Example of the semantic data that can be imported to Rhino](https://raw.githubusercontent.com/jaspervdv/RhinoCityJSON/master/Images/Overview_1.jpg)

Currently only supports CityJSON 1.1 and 1.2.

## How to install
### Via Food4Rhino
URL: https://www.food4rhino.com/en/app/rhino-cityjson

This is the easiest way to install the plugin. However newer features/updates will be released via that outlet only when deemed completely stable and finished.
This means that updates will be less regular, less versions will be available and experimental features will not be available.

### Build/Compile locally
If a certain version is desired that is not available via Food4Rhino or if it is desired to edit the code it is possible to compile the code yourself. 
If working with Visual Studio loading the RhinoCityJSON.sln should automatically resolve most of the issues. 
However, the build output path has to be set to function correctly. 
It is recommended to set this to the Grasshopper component directory path, or a subfolder of the Grasshopper component directry path. 
This path can be easily found by opening Grasshopper, going to File->Special Folders->Component Folder.

## The GH components
A simple summary of the plugin's component.

### Simple Reader
The Simple Reader only processes data related to geometry. 

Inputs:
* Path. A path to a CityJSON file. Multiple file paths are optional, based on the data stored in the file they will be placed correctly related to each other. 
* Activate. A boolean dictating if the component is active or not.
* Settings. A settings string that can be supplied by the Settings component.

Outputs:
* Geometry. A grouping of Breps. The grouping is based on the LoD of the objects

### Reader
The Reader processes all the data supplied by a CityJSON.

Inputs:
* Path. A path to a CityJSON file. Multiple file paths are optional, based on the data stored in the file they will be placed correctly related to each other. 
* Activate. A boolean dictating if the component is active or not.
* Settings. A settings string that can be supplied by the Settings component.

Outputs:
* Geometry. A list of single surface Breps.
* Surface Info Keys. The keys of semantic info that corresponds 1:1 with the geometry.
* Surface Info Values. The values of semantic info that corresponds 1:1 with the geometry.
* Object Info Keys. The keys of sematic info that corresponds with the object (names).
* Object Info Values. The values of semantic info that corresponds with the object (names).

### Settings
The Settings component gives the user more control over the Simple Reader and Reader component.

Inputs:
* Translation. If true, the geometry will be placed in the rhino model as if the Rhino world coordinates comply 1:1 with the coordinate system that is used in the CityJSON file.
* Model Origin. If a point/coordinate is supplied this coordinate will be translated to be at the origin of the Rhino model (0,0,0).
* True north. If a value is supplied the geometry will be rotated around the Model Origin coordinate to comply with this True North.
* LoD. If a value is supplied only the data related to that LoD is loaded (multiple values allowed).

Outputs:
* Settings. A settings string which can be fed into the Simple Reader and/or Reader component

### LoD Reader
The LoD Reader component extracts the LoD numbers stored in a file.

Input:
* Path. A path to a CityJSON file. Multiple file paths are optional, but discouraged to use due to slow performance.

Output:
* LoD. List of present LoDs.

### Bakery
The Bakery component is a custom baking component that will not only bake the geometry, but also the related semantic data.

Input:
* Geometry. A List with geometry that is desired to be baked.
* Surface Info Keys. The keys related to the Surface Info Values (This list should be the same length as a branch of the Surface Info Values).
* Surface Info Values. The Values related to the Geometry. (This Tree should be the same length as the geometry List).
* Activate. A boolean dictating if the component is active or not (Recommended to use a button to activate and not a boolean toggle).

The semantic data will be stored per surface at: Properties->Object-Attribute User Text. Additionally the LoD and major object types will be used to create a hiarchy of layers.
Semantic values with a * are inherited from the parent object.

### Filter
The filter component filters the semantic data based on a key/value pair. 
The output of this component can be fed directly in the Information manager to filter the geometry

Input:
* Information Keys. The keys of semantic info, this can be either Surface Info Keys or Object Info Keys.
* Info Values. The values of semantic info, this can be either Surface Info Values or Object Info Values.
* Filter Info Key. The key that is used to filter on
* Filter Info Value(s). The values that are tested against
* Equals/ Not Equals. A boolean, if true the component will return the objects or surfaces where the values match, if false the component will return the objects or surfaces where the values do not math.

Output:
* Filtered Info Values. The Values of the objects/surfaces that match the requested query.

Note that this component does not filter the geometry, the Information Manager does this.

### Information Manager
The Object Info Keys and Object Info Values can not be directly used by the bakery component. 
The Manager Divider enables this data to be used.
The Information Manager also resolves the collecting of geometry based on the Filter component output

Input:
* Geometry. A list of single surface Breps.
* Surface Info Keys. The keys of semantic info that corresponds 1:1 with the geometry output of the Reader component.
* Surface Info Values. The values of semantic info that corresponds 1:1 with the geometry output of the Reader component.
* Object Info Keys. The keys of sematic info that corresponds with the object (names).
* Object Info Values. The values of semantic info that corresponds with the object (names).

Output:
* Merged Surface Info Keys. The keys of semantic info from both the input's surface and object keys that corresponds 1:1 with the geometry output of the Reader component.
* Merged Surface Info Values. The values of semantic info from both the input's surface and object values that corresponds 1:1 with the geometry output of the Reader component.

## Known issues/bugs
* Complex surfaces are not always correctly constructed.
* A considerable amount of solids are not loaded into Grasshopper/Rhino as solids.
* Filtering based on building information is currently challenging to do with the native grasshopper components.
