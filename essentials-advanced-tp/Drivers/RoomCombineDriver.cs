using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using essentials_advanced_room;
using essentials_advanced_tp.Drivers;
using joins = essentials_advanced_tp.joins;
using PepperDash.Core;
using PepperDash.Essentials.Room.Config;

namespace essentials_basic_tp.Drivers
{
    internal class RoomCombineDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "RoomCombineDriver"; } }
        public uint LogLevel { get; set; }

        public uint TogglePageJoin { get; private set; }
        public uint PageJoin { get; private set; }
        public uint ConfirmJoin { get; private set; }
        public uint CancelJoin { get; private set; }
        public uint CombineJoin { get; private set; } // TODO: add multiple join scenario joins
        public uint UnCombineJoin { get; private set; }
        public IEssentialsRoomCombiner CurrentDefaultDevice { get; private set; }
        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;

        public RoomCombineDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Parent = parent;

            TogglePageJoin = joins.UIBoolJoin.CombinePagePress;
            PageJoin = joins.UIBoolJoin.CombinePageVisible;
            ConfirmJoin = joins.UIBoolJoin.CombineConfirmPress;
            CancelJoin = joins.UIBoolJoin.CombineCancelPress;
            CombineJoin = joins.UIBoolJoin.CombineConfirmPress;
            UnCombineJoin = joins.UIBoolJoin.UnCombinePress;

            TriList.SetSigFalseAction(TogglePageJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;
           
            Register();
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }
        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Debug.Console(LogLevel, "{0} PopupInterlock_StatusChanged, e.NewJoin: {1}, e.PreviousJoin: {2}", ClassName, e.NewJoin, e.PreviousJoin);
            if (e.PreviousJoin == PageJoin)
                Unregister();
            if (e.NewJoin == PageJoin)
                Register();
        }
        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            //Unregister();
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
        }

    }
}
