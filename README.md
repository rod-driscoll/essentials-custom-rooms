# essentials-custom-rooms

Repositories containing custom plugins for PepperDash Essentials that demonstrate creating custom rooms and user interfaces.
The examples are created with DotNet v4.7.2 so only work on 4 series processors, it would not be very difficult to modify them to work in DotNet 3.5 for 3 seried. It was decided not to support old architecture and this code may be modified to DotNet v6.0 in the future.

## Pre-requisites

* You need to be able to get a regular demo of PepperDash Essentials working.
  * Instructions for PepperDash Essentials are available at <https://github.com/PepperDash/Essentials/wiki/Get-started#how-to-get-started>
* You need to be able to develop Crestron in SimplSharp.
* You need to know how to load a program onto a Crestron processor and touchpanel on a Crestron touchpanel.
  
## essentials-minimal-room

The solution file "essentials-minimal-room" is an example of creating a custom room in Crestron SimplSharp which is loaded as a plugin, therefore the Core Essentials does not need to be modified.

The code does nothing more than toggles a button on a user interface.

### Installation

1. Download a release version of Essentials.cplz <https://github.com/PepperDash/Essentials/releases> and load it into a processor.
   1. Initial load will error out and create a "\\user\\programX\\" folder on the processor for loading plugins and the config.
2. Load "essentials-minimal-room-config.json" into the "\\user\\programX\\" folder.
3. Load the touch panel file and configure it to communicate with the processor on IPID 03 as per the demo config file..
4. Compile and load the plugins from this repo
   1. essentials-minimal-room-epi.dll
   2. essentials-minimal-tp-epi.dll

### Dependencies

Using NuGet package manager install the following:

1. "PepperDashEssentials"
   1. Delete "NewtonSoft.Json.Compact" from the references folder to remove the duplicate library issue.
2. "Crestron.SimplSharp.SDK.ProgramLibrary" to "minimal-tp" project only.

### Information

When using a room plugin, the room is not defined in the "room" section of the config file because it is not a built in room, you must define the room as the last device in the "devices" section because the room must be loaded last.

#### essentials-minimal-room-epi

This minimal-room is skeleton code which doesn't currently do any more than demonstrate creating a room that is not built into Eentials.

* Factory.cs
  * "minimal-room" is the "Type" you need to define in the config file in the "devices" section as the last device.

* Device.cs
  * device.cs extends "EssentialsRoomBase" which is required to define an Essentials room.
  * "minimal-room" provides empty implementation of the required interfaces of "EssentialsRoomBase".
  * device.cs implements "IMinimalRoom", which is an interface to reference it in the "minimal-tp" plugin and treat it as a different room type than "IEssentialsRoom".
  * when adding functionality you will add a Config property and an IDevice interface

* IMinimalRoom.cs
  * IMinimalRoom.cs is an interface to reference it in the "minimal-tp" plugin and treat it as a different room type than "IEssentialsRoom".
  * IMinimalRoom.cs is currently empty, as you add items to the properties section ofthe room config file you will add them here so the program can read them.

The following file is not used for a "minimal-room" and will be added when expanding upon the minimal room.

* Config.cs (not implemented in "minimal-room")
  * when expanding the room config.cs will need to be included.
  * any items added to config.cs would be read from the "properties" section of the config file for the room.
  * config.cs will most likely extend from "EssentialsRoomPropertiesConfig" to provide a lot of standard functionality.

#### essentials-minimal-tp-epi

If you add a touch panel to the Essentials config file using the model as the "type" (e.g "type":"tsw1070"), then the touchapen will be created with all the built in functionality and interfaces, and cannot be extended or modified. To define a customisable touch panel you need to use a plugin.

In the Essentials config file set "type":"minimal-tp" and then the type of touch panel will be defined in properties (e.g. "properties":{"type":"minimal-tp"} )

* Factory.cs
  * "minimal-tp" is the "Type" you need to define in the config file in the "devices" section as the last device.
  * factory.cs will need to be updated when additional touchpanel models are released.
  
* Config.cs
  * required to add "Type" to the config file device properties, so the plugin can determine the model of the touchpanel.
  * extends "CrestronTouchpanelPropertiesConfig" to access the properties "defaultRoomKey" and "control":{"method":"ipid","ipid":"03"}

* Device.cs
  * extends "TouchpanelBase" which handles the creation and registration of the touchpanel.
  * You will add drivers in the "SetupPanelDrivers" method, this is where the majority of custom code will start.
  * "SetupPanelDrivers" checks for your custom room interface (e.g. "IMinimalRoom") and implement code, in this demo we have created and ToggleButtonDriver here.

* ToggleButtonDriver.cs
  * this is an example driver that toggles a button, this completely custom, many such drivers will be the core of custom plugins.

## contributors

Rod Driscoll: <rod@theavitgroup.com.au>
