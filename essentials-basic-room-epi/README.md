# essentials-basic-room

An example of a room plugin for PepperDash Essentials that loads at runtime without the need to modify the Essentials core code.
Use this in conjunction with essentials-basic-tp-epi.

This plugin starts as "minimal-room" and "minimal-tp" and is being modified to add features from "EssentialsHuddleSpaceRoom" in an effort to understand the various features built into Essentials.

The final result is a user interface that:

* reads the room name from config and displays it
* has interlocked sub pages, each in their own driver file.
* a help button displaying text from a config file
* an info button displaying text from a config file

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

### essentials-basic-room-epi

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

### essentials-basic-tp-epi

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

## Tutorial stage 2 - implementing PanelMainInterfaceDriver and interlocked subpages

The project now works the same as the minimum plugins, we can now start adding functionality.

The existing room types use interface drivers, with "EssentialsPanelMainInterfaceDriver" being the parent driver and several children drivers. We will re-create our own version called "BasicPanelMainInterfaceDriver".

BasicPanelMainInterfaceDriver can manage all drivers so that sub pages can be interlocked and managed by buttons on header and footer drivers.

### BasicPanelMainInterfaceDriver

Use "EssentialsPanelMainInterfaceDriver" from PepperDash Essentials as a reference when creating this class, we want to understand it all so don't just copy everything in at once, make sure you understand it as you copy it in. You'll get an idea of dependencies as you go.

* create the class "BasicPanelMainInterfaceDriver", extend PanelDriverBase and implement IDisposable
* we'll handle reserved sigs in here, for now just initialize them and nothing else.
* add PanelDriverBase CurrentChildDriver to hold the sub drivers
* add a "List\<PanelDriverBase> ChildDrivers" property to store child drivers, this is not how it was done in EssentialsHeaderDriver
* add the required disposable interface implementations. In the method Dispose iterate through ChildDrivers and if any item implements IDisposeable then call dispose on it.
* create BasicPanelMainInterfaceDriver in Device.SetupPanelDrivers before room is defined.

Now the driver is running it isn't actually doing anything noticeable yet, so lets add an interlocked set of popup pages.

* add the property
  * public JoinedSigInterlock PopupInterlock { get; private set; }
* in the constructor add the following
  * PopupInterlock = new JoinedSigInterlock(TriList);

We'll add child drivers to PopupInterlock.

#### Help message subpage

TODO - fix these instructions.

* add the SetUpHelpButton method to "BasicHeaderDriver", this is going to pop up an interlocked page that shows a message from the config file.
* on the touchpanel add the following:
  * A help Dynamic Icon button with join 15086 (UIBoolJoin.HelpPress), visibility join 15084 (UIBoolJoin.HelpPageShowCallButtonVisible) and Dynamic Icon serial join 3954 (UIStringJoin.HeaderButtonIcon4)
  * a help subpage with visibility join 15085 (UIBoolJoin.HelpPageVisible) and help text serial join 3922 (UIStringJoin.HelpMessage).
* in the config file within the room device add the following under "properties"
  * "help": { "message":"Contact reception for help" }

#### Info message subpage

 Similar to the help message page. we have added text from the config file and put a toggling button on the page.
 The toggling button is being registered when the page appears and de-registered when it disappears.

## Tutorial stage 3 - Implementing a PIN page

Starting with "basic-tp" make a new project and call it "basc-tp-with-pin". We aren't going to change the factory definition though, this will be our tp plugin project moving forward and the old project is left as-is for reference.

There will be minimal changes to the main project.

### touch panel file

* tp file: "essentials-basic-tp-with-pin.vtp"
* smart graphics file: "essentials-basic-tp-with-pin.sgd"

* Copy the PIN page from the Essentials demo onto your touch panel.
  * The PIN page must have a z-order above all sub pages we want to block, we aren't closing anything when the pin is visible, just covering everything else to make it inaccessable on the touch panel.
* Compile the touch panel and copy the smart graphics file onto the processor ("\\user\\programX\\sgd\\essentials-basic-tp-with-pin.sgd")

### config file

* config file: "configurationFile-essentials-basic-room-with-pin.json"

* add a password and sgd file to the config file touchpanel device "properties" section (e.g. "password": "1988", "sgdFile": "essentials-basic-tp-with-pin.sgd" )
  * the password will be touch panel specific, so you can have a different password per touch panel.
* add a password to the config file room "properties" section (e.g. "password": "1234" )
  * the password is specific to the room.
* You can either choose to make passwords at a room level or at a touchpanel level or both at once. You can use one password as a back door if you like, for example tell the clients the room password and keep the touchpanel password as an admin password.

### room plugin

* room project: "essentials-basic-room-epi.csproj"
* room plugin: "essentials-basic-room-epi.dll"

* Define and add an interface for a password in the config.cs
  * public interface IHasPassword

### tp plugin

* tp project: "essentials-basic-tp-epi-with-pin.csproj"
* tp plugin: "essentials-basic-tp-epi.dll"
* PIN driver class: "PinDriver.cs"
  
This is just an updated version of "basic-tp" so the namespace and assemnbly name will stay "basic-tp" but in a different project so we can still refer to our previous "basic-tp" project before we added the PIN.

* unload the basic-tp project.
* copy basic-tp and rename the project to "basic-tp-with-pin" but do not change the assembly name or factory reference.

* in BasicPanelMainInterfaceDriver constructor add:
  * ChildDrivers.Add(new PinDriver(this, config));
* in BasicPanelMainInterfaceDriver remove any reference to "IsAuthorized", we're going to do that in the new driver class.
* create a new class "PinDriver", it will be similar to the other drivers we have created.
* copy and modify code related to the pin page from EssentialsMainInterfaceDriver.cs

Now we have a working PIN page.

## Tutorial stage 4 - Adding an audio DSP

We want to implement a third party audio DSP to use as the main room volume control, something I could not figure out using any of the built in Essentials room types, hence why this project was started.

We are going to create room and tp audio drivers to minimise the clutter in the room and and tp plugins.

Here are the main files:

* essentials-basic-audio-room.csproj
* essentials-basic-audio-tp.csproj
* configurationFile-essentials-basic-audio-room.json

Delete the loaded dlls from the processor before starting, we are going to use the same namespace so we don't want the code loading the wrong plugin.
 "essentials-basic-room-epi.dll"
 "essentials-basic-tp-epi.dll"

### epi-qsc-qsysdsp plugin

* download and load the latest QSYS plugin dll <[epi-qsc-qsysdsp](https://github.com/PepperDash/epi-qsc-qsysdsp/releases)> ("\\user\\programX\\plugins\\").
  
### basic-audio-room plugin

The room plugin is "essentials-basic-audio-room.csproj"

* add "RoomAudio.cs" and copy the code from the repo.
  * trying to put as much audio code in here as possible.
* add "IHasAudioDevice" to the Device definition
  * Create IHasAudioDevice interace and add RoomAudio to it.
* add the following property
  * public RoomAudio Audio { get; set; }
* add the following to the Device constructor
  * Audio = new RoomAudio(PropertiesConfig);
* add SetDefaultLevels() to Device to meet interface requirement and have it call Audio.SetDefaultLevels()
* add "defaultAudioKey" to Config.cs

### basic-audio-tp plugin

* Device.cs, Factory.cs and Config.cs don't change
* Create "BasicAudioDriver.cs" and copy the code from the repo. Most of the code has come from EssentialsPanelAvFunctionsDriver.
* Add the follwoing to the "BasicPanelMainInterfaceDriver" contructor
  * ChildDrivers.Add(new BasicAudioDriver(this, config));
* In SetupChildDrivers() we need to change the input parameter type to IBasicRoom then pass the room to all drivers instead of just the propertiesconfig, the driver needs to access the room to get the current device for the driver.
* Modify Setup() in each existing driver to accept IBasicRoom as an input parameter, and add code to get properties from that room in each Setup().
  * var roomConf = room.PropertiesConfig;

### basic-audio-tp touchpanel

* add volume and mic buttons to the footer, and the Volume-Dual-Mute popup from the Essentials demo tp.

### basic-audio-room config file

* add a dsp as per "configurationFile-essentials-basic-audio-room.json"
  * remember that the rooms must be below all other devices in the config file.
* here is an example of a single level control block definition in the config dsp device "properties":
  * "levelControlBlocks": { "fader-room-1": {  "label": "Room 1", "levelInstanceTag": "Room1.Master.Vol" } }
* add "defaultAudioKey" to the room properties

### qsys file

* create and emulate a qsys file with Named controls for the gains, such as
  * "Room1.Master.Vol"

## Tutorial stage 4.1 - Adding a mic level to the audio DSP

We currently have 1 volume control but that is all, we are now going to add a second volume control for mic mute.

### RoomAudio and RoomVolume

RoomAudio implementing IHasCurrentVolumeControls doesn't cater for multiple levels such as a mic, so we are going to create RoomVolume and move most of the implementation of RoomAudio into RoomVolume, then create multiple instances of RoomVolume and store them in a dictionary within RoomAudio.

Using an enum eVolumeKey to use for keys of the dictionary to avoid typos later.

### Room Config

Add a new property "DefaultMicKey" to Config and the file, it'll be used like DefaultAudioKey in code.

### BasicAudioDriver

BasicAudioDriver needs to be modified so we can call an instance of it for each volume node. When BasicAudioDriver is created it needs to know the key for the instance to find the device in the current room, and the joins to control it, "BasicAudioDriverControls" will be a new class used to hold the key for the volume device and the joins to control it, the joins will be in another new class called "BasicAudioDriverJoins".

### BasicPanelMainInterfaceDriver update for mic

We are now going to define an instance of BasicAudioDriver for the program volume and another for the mic level.

Now we have volume and mic control working.
