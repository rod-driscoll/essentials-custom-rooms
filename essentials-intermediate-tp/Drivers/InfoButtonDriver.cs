using essentials_basic_room;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;

namespace essentials_advanced_tp.Drivers
{
    public class InfoButtonDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "InfoButtonDriver"; } }
        public uint LogLevel { get; set; }

        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public InfoButtonDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Parent = parent;

            PressJoin = UIBoolJoin.HeaderRoomButtonPress;
            PageJoin = UIBoolJoin.RoomHeaderInfoPageVisible;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;
            
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.PreviousJoin == PageJoin)
                Unregister();
            if (e.NewJoin == PageJoin)
                Register();
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            if (roomConf?.Addresses != null)
            {
                Debug.Console(LogLevel, "{0} Addresses != null", ClassName);
                if (roomConf.Addresses.PhoneNumber != null)
                {
                    Debug.Console(LogLevel, "{0} PhoneNumber != null", ClassName);
                    TriList.SetString(joins.UIStringJoin.PhoneNumber, roomConf.Addresses.PhoneNumber);
                }
                if (roomConf.Addresses.SipAddress != null) 
                    TriList.SetString(joins.UIStringJoin.SipAddress, roomConf.Addresses.SipAddress);
            }
            TriList.SetString(UIStringJoin.HeaderButtonIcon3, "Info");
            //Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin);
            
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
            TriList.SetSigFalseAction(joins.UIBoolJoin.ToggleButtonPress, () =>
                TriList.SetBool(joins.UIBoolJoin.ToggleButtonPress, 
                    !TriList.BooleanInput[joins.UIBoolJoin.ToggleButtonPress].BoolValue) );
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(joins.UIBoolJoin.ToggleButtonPress);
        }

    }
}
