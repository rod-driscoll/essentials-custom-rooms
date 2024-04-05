using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;
using System;

namespace essentials_basic_tp_epi.Drivers
{
    public class HelpButtonDriver : PanelDriverBase, IHasRoomSetup
    {
        public string ClassName { get { return "HelpButtonDriver"; } }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public HelpButtonDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Parent = parent;
            TriList.SetSigFalseAction(UIBoolJoin.HelpPress, () =>
                parent.PopupInterlock.ShowInterlockedWithToggle(UIBoolJoin.HelpPageVisible) );
            Debug.Console(2, "{0} constructor done", ClassName);
        }

        public void Setup(EssentialsRoomPropertiesConfig roomConf)
        {
            Debug.Console(2, "{0} Setup", ClassName);
            if (roomConf == null)
                Debug.Console(2, "{0} roomConf == null", ClassName);
            // Help roomConf and popup
            string message_ = null;
            if (roomConf.Help != null)
            {
                Debug.Console(2, "{0} roomConf.Help != null", ClassName);
                Debug.Console(2, "{0} roomConf.Help.Message: {1}", ClassName, roomConf.Help.Message);
                if (roomConf.Help.Message != null)
                    message_ = roomConf.Help.Message;
            }
            else // older config
            {
                Debug.Console(2, "{0} roomConf.Help == null", ClassName);
                if (roomConf.HelpMessage != null)
                    message_ = roomConf.HelpMessage;
            }
            if (String.IsNullOrEmpty(message_))
                message_ = "No help message configured";
                Debug.Console(2, "{0} message_: {1}", ClassName, message_);
            TriList.SetString(UIStringJoin.HelpMessage, message_);
            TriList.SetString(UIStringJoin.HeaderButtonIcon4, "Help");
            TriList.SetBool(UIBoolJoin.HelpPageShowCallButtonVisible, true);
            Debug.Console(2, "{0} Setup done", ClassName);
        }

    }
}
