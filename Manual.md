# Opening one (or more) CityJSON file(s)

## 1.1 Bring the CityJSON data into grasshopper

To bring the CityJSON data into grasshopper the `Reader Objects` component is required. This component accepts three inputs. For now we will focus on the top two inputs: `Path` and `Activate`.

The `path` input accepts a string that represents the file path of the file that is desired to be opened. It accepts one or more paths. The easiest way to fetch this file path is to use the grasshopper native `file path` component. This allows you to right click on the component and select either 'Select one existing file' if one file is to be opened or 'Select many existing files' if multiple files are to be opened.

Connecting the `file path` component to the `Reader Objects` component does not active the `Reader Objects` component. The `Activate` input has a default false value. As long as this value is false the component will be inactive, and settings can be changed without the risk of rerunning the component.

![Inactive script that will bring the CityJSON data into grasshopper](./Images/manual/read_simple_1.jpg)

To activate the component a grasshopper native `Boolean toggle` component can be connected. Setting this component to active will active the `Reader Objects` component. Depending on the size and complexity of the file this can take a while to load.
It is recommend to set the preview geometry to wireframe if large files are processed. This will reduce the load on the system.

Once the processes of this component are finished the data is brought into grasshopper. The geometry is exposed via the `Geometry` output, the surface semantic information is via the `Surface Information` output, and the object semantic information is exposed via the `Object Information`.

## 1.2 Bring the CityJSON data into Rhino

Now that we have the data in Grasshopper it is fairly easy to move this data to rhino. This can be done in two different ways. The easiest way is to just bake the geometry output. This can be done if no semantic information related to the geometry is required. Just right click the component and press "Bake…". This will bake a 'naked' copy of the geometry into rhino.

![The easiest way to bake is by right clicking the component and pressing bake](./Images/manual/bake_simple_1.jpg)

If semantic data is required some extra components are required. It is recommended to deactivate the `Reader Objects` component before moving on. We are going to make more connections to new components and if these are made incorrectly the components could load for a long time if the data-stream is active. De-activate the `Reader Objects` component by toggling the `Boolean toggle` to false.

Now `Information manager` component is required. This component will copy all the object information to all the surfaces information instances belonging to that object. It also does some filtering operations, but we will cover those later. Connect the `Geometry`, `Surface Information`, and  `Object Information` outputs of the `Reader Objects` component to the `Geometry`, `Surface Information`, and  `Object Information` inputs of the Information manager` component. Nothing happens, because the data stream is still disabled.

Following the `Information manager` component the `Bakery` component is required. This component will bake the data in Rhino. Connect the `Geometry` and `Merged Surface Information` outputs of the `Information manager` component to the `Merged Surface Information` inputs of the `Bakery` component.

Similar to the `Reader Objects` component the `Bakery` component has an `Activate` input. This works similar to the one of the `Reader Objects` component. If the input is true, the `Bakery` component will bake. I would recommend connecting a grasshopper native `Button` component. We only want to bake on pressing this button. If a toggle is used the bakery would bake the data to rhino every time the data-stream is updated.

![Inactive script that will bring the CityJSON data into grasshopper and move it to rhino](./images/manual/bake_simple_2.jpg)

Now the `Boolean toggle` component connected to the `Reader Objects` component can be set to true again. This will rerun the script. Once the script is finished the button connected to the `Bakery` component can be pressed. This will bake the data to Rhino.

The data will stored in a layer structure containing some of the attribute data. The other data can be found in the "Attribute User Text" window in rhino. This window can be found under the "Properties" tab.

![On the left the layer structure and on the right the attribute data of the baked geometry](./images/manual/bake_semantic_example.jpg)

## 1.3 Fine tuning the CityJSON data input

## 1.4 Fetching document data and materials

## 1.5 Opening Template data
