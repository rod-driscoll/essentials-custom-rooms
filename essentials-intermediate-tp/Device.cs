using Crestron.SimplSharpPro;
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
    public class Device : TouchpanelBase, ILogClassDetails
    {
        public string ClassName { get { return "Device"; } }
        public uint LogLevel { get; set; }
        public PanelDriverBase PanelDriver { get; private set; }
        Config config;

        /// <summary>
        /// Config constructor
        /// </summary>
 		public Device(string key, string name, BasicTriListWithSmartObject panel, Config config)
            : base(key, name, panel, config)
        {
            LogLevel = 2;
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
            Debug.Console(0, this, "{0} Room as IBasicRoom {1}", ClassName, (room as IBasicRoom) == null ? "==null" : "exists");

            if (room is IBasicRoom)
            {
                Debug.Console(0, this, "Room '{0}' is IBasicRoom", roomKey);
                var room_ = (room as IBasicRoom);

                Debug.Console(LogLevel, "{0} PropertiesConfig {1}", ClassName, room_.PropertiesConfig == null ? "==null" : "exists");
                Debug.Console(LogLevel, "{0} Properties {1}", ClassName, room_.Config.Properties == null ? "==null" : "exists");

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
            Debug.Console(LogLevel, this, "{0} ExtenderSystemReservedSigs_DeviceExtenderSigChange not implemented", ClassName);
        }

    }
}