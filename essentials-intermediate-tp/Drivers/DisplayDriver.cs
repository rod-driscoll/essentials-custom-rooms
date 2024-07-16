using Crestron.SimplSharp;
using essentials_basic_room.Functions;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Linq;
using joins = essentials_basic_tp_epi.joins;

namespace essentials_basic_tp.Drivers
{
    public class DisplayDriver : PanelDriverBase, IBasicRoomSetup, IDisposable
    {
        public string ClassName { get { return "DisplayDriver"; } }
        public uint LogLevel { get; set; }
        CTimer SecondTimer;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        NotificationRibbonDriver ribbonDriver;

        //private List<IRoutingSinkWithSwitching> CurrentDevices;
        public IRoutingSinkWithSwitching CurrentDefaultDevice { get; private set; }
        public PowerStates CurrentDefaultDevicePowerState { get; private set; }

        public uint PowerToggleJoin { get; private set; }
        public uint PowerToggleText { get; private set; }
        public DisplayDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Parent = parent;

            PowerToggleJoin = UIBoolJoin.DisplayPowerTogglePress;
            PowerToggleText = joins.UIStringJoin.DisplayPowerStatus;

            var ribbon = Parent.ChildDrivers.First(x => x is NotificationRibbonDriver);
            if (ribbon != null)
                ribbonDriver = ribbon as NotificationRibbonDriver;
            
            Register(); // the driver is always available so register here rather than on popupinterlock
            
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;

            IHasPowerControlWithFeedback dispTwoWay;
            IWarmingCooling dispWarmCool;
            if (CurrentDefaultDevice != null) // Disconnect current room 
            {
                CurrentDefaultDevice.CurrentSourceChange -= this.CurrentDefaultDevice_CurrentSourceChange;
                dispTwoWay = CurrentDefaultDevice as IHasPowerControlWithFeedback;
                Debug.Console(LogLevel, "{0} Setup, Disconnect IHasPowerControlWithFeedback {1}", ClassName, dispTwoWay == null ? "== null" : CurrentDefaultDevice.Key);
                if (dispTwoWay != null)
                    dispTwoWay.PowerIsOnFeedback.OutputChange -= PowerIsOnFeedback_OutputChange;
                dispWarmCool = CurrentDefaultDevice as IWarmingCooling;
                Debug.Console(LogLevel, "{0} Setup, Disconnect IWarmingCooling {1}", ClassName, dispTwoWay == null ? "== null" : CurrentDefaultDevice.Key);
                if (dispWarmCool != null)
                {
                    dispWarmCool.IsWarmingUpFeedback.OutputChange -= IsWarmingUpFeedback_OutputChange;
                    dispWarmCool.IsCoolingDownFeedback.OutputChange -= IsCoolingDownFeedback_OutputChange;
                }
            }

            var room_ = room as IHasDisplayFunction;
            Debug.Console(LogLevel, "{0} Setup, IHasDisplayFunction {1}", ClassName, room_ == null ? "== null" : room.Key);
            if (room_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, Driver {1}", ClassName, room_.Display == null ? "== null" : "exists");
                CurrentDefaultDevice = room_.Display.DefaultDisplay;
                Debug.Console(LogLevel, "{0} Setup, Driver.DefaultDisplay {1}", ClassName, room_.Display.DefaultDisplay == null ? "== null" : room_.Display.DefaultDisplay.Key);
                CurrentDefaultDevice.CurrentSourceChange += CurrentDefaultDevice_CurrentSourceChange;
                
                dispTwoWay = CurrentDefaultDevice as IHasPowerControlWithFeedback;
                Debug.Console(LogLevel, "{0} Setup, IHasPowerControlWithFeedback {1}", ClassName, dispTwoWay == null ? "== null" : CurrentDefaultDevice.Key);
                if (dispTwoWay != null)// Link power, warming, cooling to display
                    dispTwoWay.PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;
                dispWarmCool = CurrentDefaultDevice as IWarmingCooling;
                Debug.Console(LogLevel, "{0} Setup, IWarmingCooling {1}", ClassName, dispWarmCool == null ? "== null" : CurrentDefaultDevice.Key);
                if (dispWarmCool != null)
                {
                    dispWarmCool.IsWarmingUpFeedback.OutputChange += IsWarmingUpFeedback_OutputChange; ;
                    dispWarmCool.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange; ;
                }
            }
            Debug.Console(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }
        private void IsCoolingDownFeedback_OutputChange(object sender, FeedbackEventArgs e) // sender is a BoolFeedback Key 'IsCoolingDown'
        {
            var device_ = sender as IKeyed;
            if (device_ != null)
                Debug.Console(LogLevel, "{0} IsCoolingDownFeedback_OutputChange Key: {1}", ClassName, device_.Key);
            //var display_ = sender as IWarmingCooling;  // this is always null
            if (e.BoolValue)
                CurrentDefaultDevicePowerState = PowerStates.cooling;
            else
            {
                CurrentDefaultDevicePowerState = PowerStates.standby;
                UpdateCurrentDisplayFeedback();
            }
            var display_ = CurrentDefaultDevice as IWarmingCooling;
            if (display_ != null)
            {
                Debug.Console(LogLevel, "{0} IsCoolingDownFeedback_OutputChange: {1}", ClassName, display_.IsCoolingDownFeedback.BoolValue);
                StartSecondTimer(display_.IsCoolingDownFeedback.BoolValue || display_.IsWarmingUpFeedback.BoolValue);
            }
            else
                Debug.Console(LogLevel, "{0} IsCoolingDownFeedback_OutputChange {1}", ClassName, e.BoolValue);
        }

        private void IsWarmingUpFeedback_OutputChange(object sender, FeedbackEventArgs e) // sender is a BoolFeedback Key 'IsWarmingUp'
        {
            if (e.BoolValue)
                CurrentDefaultDevicePowerState = PowerStates.warming;
            else
            {
                CurrentDefaultDevicePowerState = PowerStates.on;
                UpdateCurrentDisplayFeedback();
            }
            var device_ = sender as IKeyed; 
            if (device_ != null)
                Debug.Console(LogLevel, "{0} IsWarmingUpFeedback_OutputChange Key: {1}", ClassName, device_.Key);
            //var display_ = sender as IWarmingCooling; // this is always null
            var display_ = CurrentDefaultDevice as IWarmingCooling;
            if (display_ != null)
            {
                Debug.Console(LogLevel, "{0} IsWarmingUpFeedback_OutputChange: {1}", ClassName, display_.IsWarmingUpFeedback.BoolValue);
                StartSecondTimer(display_.IsCoolingDownFeedback.BoolValue || display_.IsWarmingUpFeedback.BoolValue);
            }
            else
                Debug.Console(LogLevel, "{0} IsWarmingUpFeedback_OutputChange {1}", ClassName, e.BoolValue);
        }

        private void PowerIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var display_ = sender as IHasPowerControlWithFeedback;
            if (display_ != null)
                Debug.Console(LogLevel, "{0} PowerIsOnFeedback_OutputChange: {1}", ClassName, display_.PowerIsOnFeedback.BoolValue);
            else
                Debug.Console(LogLevel, "{0} PowerIsOnFeedback_OutputChange {1}", ClassName, e.BoolValue);
            UpdateCurrentDisplayFeedback();
       }

        private void CurrentDefaultDevice_CurrentSourceChange(SourceListItem info, ChangeType type)
        {
            Debug.Console(LogLevel, "{0} CurrentDefaultDevice_CurrentSourceChange", ClassName);
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
            TriList.SetSigFalseAction(PowerToggleJoin, () =>
            {
                var currentDisplay_ = CurrentDefaultDevice as IHasPowerControlWithFeedback;
                Debug.Console(LogLevel, "{0} PowerToggleJoin pressed {1}", ClassName, currentDisplay_ == null ? "== null" : CurrentDefaultDevice.Key);
                currentDisplay_?.PowerToggle();
            });
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(PowerToggleJoin);
        }

        public void UpdateCurrentDisplayFeedback()
        {
            if (CurrentDefaultDevice != null) // Disconnect current room 
            {
                var dispWarmCool = CurrentDefaultDevice as IWarmingCooling;
                var dispTwoWay = CurrentDefaultDevice as IHasPowerControlWithFeedback;
                if (dispWarmCool != null)
                {
                    if (dispWarmCool.IsWarmingUpFeedback.BoolValue)
                    {
                        CurrentDefaultDevicePowerState = PowerStates.warming;
                        //TriList.SetBool(PowerToggleJoin, !TriList.GetBool(PowerToggleJoin)); // can't use GetBool because it gets BooleanOutput
                        TriList.SetBool(PowerToggleJoin, !TriList.BooleanInput[PowerToggleJoin].BoolValue);
                        //Debug.Console(LogLevel, "{0} UpdateCurrentDisplayFeedback, warming", ClassName);
                    }
                    else if (dispWarmCool.IsCoolingDownFeedback.BoolValue)
                    {
                        CurrentDefaultDevicePowerState = PowerStates.cooling;
                        //TriList.SetBool(PowerToggleJoin, !TriList.GetBool(PowerToggleJoin)); // can't use GetBool because it gets BooleanOutput
                        TriList.SetBool(PowerToggleJoin, !TriList.BooleanInput[PowerToggleJoin].BoolValue);
                        //Debug.Console(LogLevel, "{0} UpdateCurrentDisplayFeedback, cooling", ClassName);
                    }
                }
                else if (dispTwoWay != null)
                {
                    if (dispTwoWay.PowerIsOnFeedback.BoolValue)
                        CurrentDefaultDevicePowerState = PowerStates.on;
                    else
                        CurrentDefaultDevicePowerState = PowerStates.standby;
                }
            }
            if (CurrentDefaultDevicePowerState == PowerStates.on)
            {
                TriList.SetBool(PowerToggleJoin, true);
                Debug.Console(LogLevel, "{0} UpdateCurrentDisplayFeedback, setting", ClassName);
            }
            else if (CurrentDefaultDevicePowerState == PowerStates.off || CurrentDefaultDevicePowerState == PowerStates.standby)
            {
                TriList.SetBool(PowerToggleJoin, false); 
                Debug.Console(LogLevel, "{0} UpdateCurrentDisplayFeedback, clearing", ClassName);
            }
            TriList.SetString(PowerToggleText, CurrentDefaultDevicePowerState.ToString());

            Debug.Console(LogLevel, "{0} UpdateCurrentDisplayFeedback: {1}", ClassName, CurrentDefaultDevicePowerState.ToString());
        }
        private void StartSecondTimer(bool enable)
        {
            Debug.Console(0, "{0} StartSecondTimer: {1}", ClassName, enable);
            if (!enable)
            {
                Dispose();
            }
            else if (SecondTimer == null)
            {
                Debug.Console(0, "{0} StartSecondTimer creating new PowerTimer", ClassName);
                SecondTimer = new CTimer(SecondTimerExpired, this, 1000, 1000);
            }
            UpdateCurrentDisplayFeedback();
            //Debug.Console(0, "{0} StartSecondTimer end", ClassName);
        }

        private void SecondTimerExpired(object userSpecific)
        {
            if (CurrentDefaultDevice != null) // make the button flash when warming or cooling
            {
                var dispWarmCool = CurrentDefaultDevice as IWarmingCooling;
                Debug.Console(LogLevel, "{0} SecondTimerExpired dispWarmCool {1}", ClassName, dispWarmCool == null ? "== null" : CurrentDefaultDevice.Key);
                if (dispWarmCool != null)
                    if (!dispWarmCool.IsWarmingUpFeedback.BoolValue && !dispWarmCool.IsCoolingDownFeedback.BoolValue)
                        Dispose();
            }
            UpdateCurrentDisplayFeedback();
        }

        public void Dispose()
        {
            if (SecondTimer != null)
            {
                SecondTimer.Stop();
                SecondTimer = null;
            }
        }

    }

}
