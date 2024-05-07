# essentials-intermediate-room

An example of a room plugin for PepperDash Essentials that loads at runtime without the need to modify the Essentials core code.
Use this in conjunction with essentials-intermediate-tp-epi.

This plugin starts as "minimal-room" and "minimal-tp" and is being modified to add features from "EssentialsHuddleSpaceRoom" in an effort to understand the various features built into Essentials.

## Stage 1

* Use basic-audio-room and basic-audio-tp to start
* Add a display plugin and load the dll into the plugin directory, for testing i have used <https://github.com/rod-driscoll/epi-pjlink/tree/wip-pjlink-protocol>.
* Copy and rename the config file "configurationFile-essentials-intermediate-room.json"
* Add the display to the config file.
* add defaultDisplayKey to the config.
* 