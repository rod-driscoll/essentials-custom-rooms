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
using System.Runtime.Remoting.Messaging;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using Serilog.Events;

namespace essentials_basic_tp.Drivers
{
    internal class RoomCombineDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "RoomCombineDriver"; } }
        public LogEventLevel LogLevel { get; set; }

        public uint TogglePageJoin { get; private set; }
        public uint PageJoin { get; private set; }
        public uint ConfirmJoin { get; private set; }
        public uint CancelJoin { get; private set; }
        public uint CombineJoin { get; private set; } // TODO: add multiple join scenario joins
        public uint UnCombineJoin { get; private set; }

        public uint[] CombineModePressJoin { get; private set; }
        public uint[] CombineModeFeedbackJoin { get; private set; }
        public string[] CombineModeScenarioKeys { get; private set; }
        public JoinedSigInterlock CombineModeJoinInterlock { get; private set; }
        public int CurrentJoinMode { get; private set; }
        private enum JoinModeButtonMode { Idle, Selected, Active }
        private int selectedJoinMode;

        public IEssentialsRoomCombiner CurrentDefaultDevice { get; private set; }
        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;

        public RoomCombineDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = LogEventLevel.Information;
            Parent = parent;

            TogglePageJoin = joins.UIBoolJoin.CombinePagePress;
            PageJoin = joins.UIBoolJoin.CombinePageVisible;
            ConfirmJoin = joins.UIBoolJoin.CombineConfirmPress;
            CancelJoin = joins.UIBoolJoin.CombineCancelPress;
            CombineJoin = joins.UIBoolJoin.CombineConfirmPress;
            UnCombineJoin = joins.UIBoolJoin.UnCombinePress;
       
            TriList.SetSigFalseAction(TogglePageJoin, () => Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));
            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;

            CombineModePressJoin = new uint[] {
                joins.UIBoolJoin.UnCombinePress,
                joins.UIBoolJoin.CombineMode1Press,
                joins.UIBoolJoin.CombineMode2Press,
                joins.UIBoolJoin.CombineMode3Press
            };
            CombineModeFeedbackJoin = new uint[] {
                joins.UIBoolJoin.UnCombineFeedback,
                joins.UIBoolJoin.CombineMode1Feedback,
                joins.UIBoolJoin.CombineMode2Feedback,
                joins.UIBoolJoin.CombineMode3Feedback
            };
            CombineModeScenarioKeys = new string[] // todo, get these keys from CurrentDefaultDevice 
            {
                "scenario-combine-none",
                "scenario-combine-all",
                "scenario-town-hall",
                "scenario-combine-3"
            };

            CombineModeJoinInterlock = new JoinedSigInterlock(TriList);
            foreach (var btn_ in CombineModePressJoin)
                TriList.SetSigFalseAction(btn_, () => CombineModeJoinInterlock.ShowInterlockedWithToggle(btn_));
            CombineModeJoinInterlock.StatusChanged += CombineModeJoinInterlock_StatusChanged; ;

            TriList.SetSigFalseAction(ConfirmJoin, () => TriggerCombineMode(selectedJoinMode));
            
            Register();
            Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }
        /// <summary>
        /// This is called for each change so no need to iterate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CombineModeJoinInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (TriList.BooleanInput[e.NewJoin].BoolValue) // only need to see this once per change
            {
                Debug.LogMessage(LogLevel, "{0} CombineModeJoinInterlock_StatusChanged, e.NewJoin: {1}, e.PreviousJoin: {2}", ClassName, e.NewJoin, e.PreviousJoin);
                selectedJoinMode = (int)e.NewJoin;

                for (var i=0; i<CombineModePressJoin.Length; i++)
                {
                    TriList.SetBool(CombineModeFeedbackJoin[i], CurrentJoinMode == i);
                    // set the mode of the button to change the colour
                    var val_ = (ushort)(CurrentJoinMode == i ? JoinModeButtonMode.Active :
                        e.NewJoin == CombineModePressJoin[i] ? JoinModeButtonMode.Selected: JoinModeButtonMode.Idle);
                    TriList.SetUshort(CombineModePressJoin[i], val_);
                    // set the active button and clear the rest - not necessary here, done in interlock
                    // TriList.BooleanInput[CombineModePressJoin[i]].BoolValue = e.NewJoin == CombineModePressJoin[i];
                }            
            }

        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Debug.LogMessage(LogLevel, "{0} PopupInterlock_StatusChanged, e.NewJoin: {1}, e.PreviousJoin: {2}", ClassName, e.NewJoin, e.PreviousJoin);
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
            Debug.LogMessage(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            //Unregister();
            Debug.LogMessage(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            Debug.LogMessage(LogLevel, "{0} Register", ClassName);
            //foreach (var devConf in ConfigReader.ConfigObject.Devices) { }
            var roomCombinerList = DeviceManager.AllDevices.OfType<IEssentialsRoomCombiner>().ToList();
            if (roomCombinerList.Count > 0)
            {
                Debug.LogMessage(LogLevel, "{0} Found {1} RoomCombiners, assingong {2} =  ", ClassName, roomCombinerList.Count, roomCombinerList[0].Key);
                CurrentDefaultDevice = roomCombinerList[0];
                CurrentDefaultDevice.RoomCombinationScenarioChanged += CurrentDefaultDevice_RoomCombinationScenarioChanged;
            }
        }

        private void CurrentDefaultDevice_RoomCombinationScenarioChanged(object sender, EventArgs e)
        {
            Debug.LogMessage(LogLevel, "{0} RoomCombinationScenarioChanged: {1}", ClassName, CurrentDefaultDevice.CurrentScenario.Key);
            Debug.LogMessage(LogLevel, "{0} Name: {1}", ClassName, CurrentDefaultDevice.CurrentScenario.Name);
            //Debug.LogMessage(LogLevel, "{0} Name: {1}", ClassName, CurrentDefaultDevice.CurrentScenario.IsActiveFeedback.);
        }

        public void Unregister()
        {
            Debug.LogMessage(LogLevel, "{0} Unregister", ClassName);
            if(CurrentDefaultDevice != null)
            {
                CurrentDefaultDevice.RoomCombinationScenarioChanged -= CurrentDefaultDevice_RoomCombinationScenarioChanged;
                CurrentDefaultDevice = null;
            }
        }

        public void TriggerCombineMode(int mode)
        {
            Debug.LogMessage(LogLevel, "{0} TriggerCombineMode, CurrentDefaultDevice {1}", ClassName, CurrentDefaultDevice == null ? "== null" : CurrentDefaultDevice.Key);
            if (CurrentDefaultDevice != null)
            {

                //CurrentDefaultDevice.TogglePartitionState("partition-1"); nah
                CurrentDefaultDevice.SetRoomCombinationScenario(CombineModeScenarioKeys[mode]);
            }
        }

    }
}
