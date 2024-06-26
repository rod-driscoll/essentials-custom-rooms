﻿using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace essentials_basic_tp_epi.Drivers
{
    public class InfoButtonDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "InfoButtonDriver"; } }

        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public InfoButtonDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Parent = parent;

            PressJoin = UIBoolJoin.HeaderRoomButtonPress;
            PageJoin = UIBoolJoin.RoomHeaderInfoPageVisible;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;
            
            Debug.Console(2, "{0} constructor done", ClassName);
        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.NewJoin == PageJoin)
                Register();
            else if (e.PreviousJoin == PageJoin)
                Unregister();
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(2, "{0} Setup", ClassName);
            EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            if (roomConf?.Addresses != null)
            {
                Debug.Console(2, "{0} Addresses != null", ClassName);
                if (roomConf.Addresses.PhoneNumber != null)
                {
                    Debug.Console(2, "{0} PhoneNumber != null", ClassName);
                    TriList.SetString(joins.UIStringJoin.PhoneNumber, roomConf.Addresses.PhoneNumber);
                }
                if (roomConf.Addresses.SipAddress != null) 
                    TriList.SetString(joins.UIStringJoin.SipAddress, roomConf.Addresses.SipAddress);
            }
            TriList.SetString(UIStringJoin.HeaderButtonIcon3, "Info");
            //Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin);
            
            Debug.Console(2, "{0} Setup done", ClassName);
        }

        public void Register()
        {
            Debug.Console(2, "{0} Register", ClassName);
            TriList.SetSigFalseAction(joins.UiBoolJoin.ToggleButtonPress, () =>
                TriList.SetBool(joins.UiBoolJoin.ToggleButtonPress, 
                    !TriList.BooleanInput[joins.UiBoolJoin.ToggleButtonPress].BoolValue) );
        }
        public void Unregister()
        {
            Debug.Console(2, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(joins.UiBoolJoin.ToggleButtonPress);
        }

    }
}
