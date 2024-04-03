# essentials-basic-room-epi

An example of a room plugin for PepperDash Essentials that loads at runtime without the need to modify the Essentials core code.
Use this in conjunction with essentials-basic-tp-epi.

This plugin starts as "minimal-room" and "minimal-tp" and is being modified to add features from "EssentialsHuddleSpaceRoom" in an effort to understand the various features built into Essentials.

## Tutorial stage 1 - re-create miimal room

In stage 1 we are going to duplicate and rename the minimal plugins.

* copy the solution "essentials-custom-rooms"
* copy "configurationFile-essentials-minimal-room.json" and rename it "configurationFile-essentials-basic-room.json".
* add two projects
  * "essentials-basic-room-epi"
  * "essentials-basic-tp-epi"
* Using NuGet package manager install the following:
    1. "PepperDashEssentials"
        * Delete "NewtonSoft.Json.Compact" from the references folder to remove the duplicate library issue.
    2. "Crestron.SimplSharp.SDK.ProgramLibrary" to "minimal-tp" project only.

### essentials-minimal-room-epi

* copy all the classes from "essentials-minimal-room-epi" into the project
* rename the namespace to replace "minimal" with "basic"
* rename "IMinimalRoom.cs" to "IBasicRoom.cs"
* In "configurationFile-essentials-basic-room.json", find the room device "room-1" and change "type" from "minimal-room" to "basic-room".

* Factory.cs
  * rename type from "minimal-room" to "basic-room"

* Config.cs
  * create a file "Config.cs", this will include any items in the "properties" section of the config file.
  * Config will extend "EssentialsRoomPropertiesConfig", we'll implement the items in EssentialsRoomPropertiesConfig one at a time but don't need to implement any or add more yet.

* IBasicRoom.cs
  * IBasicRoom.cs is an interface to reference it in the touchpanel plugin and treat it as a different room type than "IEssentialsRoom".
  * PropertiesConfig will contain items from the properties section of the device in the config file.

* Device.cs
  * rename class interface inheritance from "IMinimalRoom" to "IBasicRoom"
  * un-comment the declaration of PropertiesConfig to meet interface requirement.

### essentials-minimal-tp-epi

* copy all the classes from "essentials-minimal-tp-epi" into the project
* rename the namespace to replace "minimal" with "basic"
* In "configurationFile-essentials-basic-room.json", find the touchpanel device "touchpanel-1" and change "type" from "minimal-tp" to "basic-tp", the type of touch panel will be defined in properties (e.g. "properties":{"type":"minimal-tp"} )
* create a project reference to "essentials-basic-room-epi"

* Factory.cs
  * rename type from "minimal-tp" to "basic-tp"

* Config.cs
  * leave unchanged

* Device.cs
  * rename "IMinimalRoom" to "IBasicRoom"
  * un-comment the declaration of PropertiesConfig to meet interface requirement

Compile, load and test the code. If you can't compile it's probably because a few usings are not included, easily fixed by right clicking on the errors and selecting auto fix.

### Tutorial stage 2

The project now works the same as the minimum plugins, we can now start adding functionality.
