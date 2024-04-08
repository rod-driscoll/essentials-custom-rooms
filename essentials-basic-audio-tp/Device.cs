﻿using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.UI;

namespace essentials_basic_tp_epi
{
    // This would be EssentialsTouchpanelController in default Essentials
    public class Device : TouchpanelBase
    {
        public string ClassName = "Device";
        public PanelDriverBase PanelDriver { get; private set; }
        Config config;

        /// <summary>
        /// Config constructor
        /// </summary>
 		public Device(string key, string name, BasicTriListWithSmartObject panel, Config config)
            : base(key, name, panel, config)
        {
            this.config = config;
        }

        /// <summary>
        /// Sets up drivers and links them to the room specified
        /// </summary>
        /// <param name="roomKey">key of room to link the drivers to</param>
        protected override void SetupPanelDrivers(string roomKey)
        {
            // Clear out any existing actions
            Panel.ClearAllSigActions();
            Debug.Console(0, this, "Linking TP '{0}' to Room '{1}'", Key, roomKey);

            var mainDriver = new BasicPanelMainInterfaceDriver(Panel, config);

            // spin up different room drivers depending on room type
            var room = DeviceManager.GetDeviceForKey(roomKey);
            if (room == null)
                Debug.Console(0, this, "GetDeviceForKey({0}) is null", roomKey);

            Debug.Console(0, this, "Room '{0}' type '{1}'", roomKey, room.GetType());
            if (room is EssentialsRoomBase)
            {
                Debug.Console(0, this, "Room '{0}' is EssentialsRoomBase", roomKey);
                Panel.SetString(UIStringJoin.CurrentRoomName, (room as EssentialsRoomBase).Name);
            }

            Debug.Console(0, this, "Room '{0}' checking for IBasicRoom", roomKey);
            Debug.Console(0, this, "{0} Room as IBasicRoom {1}= null", ClassName, (room as IBasicRoom) == null ? "=" : "!");

            if (room is IBasicRoom)
            {
                Debug.Console(0, this, "Room '{0}' is IBasicRoom", roomKey);
                var room_ = (room as IBasicRoom);

                if (room_.PropertiesConfig == null)
                    Debug.Console(2, "{0} PropertiesConfig == null", ClassName);
                else
                    Debug.Console(2, "{0} PropertiesConfig != null", ClassName);

                if (room_.Config.Properties == null)
                    Debug.Console(2, "{0} Properties == null", ClassName);
                else
                {
                    Debug.Console(2, "{0} Properties != null", ClassName);

                }

                mainDriver.SetupChildDrivers(room_);
                Debug.Console(0, this, "Room '{0}' UI Controllers loaded", roomKey);

            }
            else if (room is IEssentialsHuddleSpaceRoom)
            {
                Debug.Console(0, this, "Room '{0}' is IEssentialsHuddleSpaceRoom - not implemented", roomKey);
            }
            else
            {
                Debug.Console(0, this, "room '{0}' Interface Driver not implemented", roomKey);
            }
            Debug.Console(0, this, "*** Linking TP '{0}' to Room '{1}' COMPLETE", Key, roomKey);
        }

        protected override void ExtenderSystemReservedSigs_DeviceExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
        {
            Debug.Console(2, this, "{0} ExtenderSystemReservedSigs_DeviceExtenderSigChange not implemented", ClassName);
        }

    }
}