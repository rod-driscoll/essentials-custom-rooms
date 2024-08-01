using essentials_basic_room;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;
using System;

namespace essentials_advanced_tp.Drivers
{
    public class HelpButtonDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "HelpButtonDriver"; } }
        public uint LogLevel { get; set; }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public HelpButtonDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Parent = parent;
            TriList.SetSigFalseAction(UIBoolJoin.HelpPress, () =>
                parent.PopupInterlock.ShowInterlockedWithToggle(UIBoolJoin.HelpPageVisible) );
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            if (roomConf == null)
                Debug.Console(LogLevel, "{0} roomConf == null", ClassName);
            // Help roomConf and popup
            string message_ = null;
            if (roomConf?.Help != null)
            {
                Debug.Console(LogLevel, "{0} roomConf.Help != null", ClassName);
                Debug.Console(LogLevel, "{0} roomConf.Help.Message: {1}", ClassName, roomConf.Help.Message);
                if (roomConf.Help.Message != null)
                    message_ = roomConf.Help.Message;
            }
            else // older config
            {
                Debug.Console(LogLevel, "{0} roomConf.Help == null", ClassName);
                if (roomConf.HelpMessage != null)
                    message_ = roomConf.HelpMessage;
            }
            if (String.IsNullOrEmpty(message_))
                message_ = "No help message configured";
                Debug.Console(LogLevel, "{0} message_: {1}", ClassName, message_);
            TriList.SetString(UIStringJoin.HelpMessage, message_);
            TriList.SetString(UIStringJoin.HeaderButtonIcon4, "Help");
            TriList.SetBool(UIBoolJoin.HelpPageShowCallButtonVisible, true);
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

    }
}
