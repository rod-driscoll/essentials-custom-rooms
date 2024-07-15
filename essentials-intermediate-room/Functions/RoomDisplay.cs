using essentials_basic_room_epi;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.CrestronIO;
using PepperDash.Essentials.Devices.Common.Environment.Somfy;
using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core.Shades;
using relay_controlled_motor_epi.Interfaces;

namespace essentials_basic_room.Functions
{
    public class RoomDisplay: IHasDefaultDisplay, ILogClassDetails
    {
        public string ClassName { get { return "RoomDisplay"; } }
        public uint LogLevel { get; set; }
        public Config config { get; private set; }

        private List<IRoutingSinkWithSwitching> Displays;
        public IRoutingSinkWithSwitching DefaultDisplay { get; private set; }
        public ShadeBase DefaultScreen { get; private set; }
        public ShadeBase DefaultLifter { get; private set; }
        public PowerStates DefaultDisplayPowerState { get; private set; }
        public BoolFeedback OnFeedback { get; private set; }
        public BoolFeedback IsWarmingUpFeedback { get; private set; }
        public BoolFeedback IsCoolingDownFeedback { get; private set; }

        public RoomDisplay(Config config)
        {
            LogLevel = 0;
            Debug.Console(LogLevel, "{0} constructor", ClassName);
            this.config = config;
            Displays = new List<IRoutingSinkWithSwitching>();
            CustomActivate();
        }
        public void CustomActivate()
        {
            Debug.Console(LogLevel, "{0} CustomActivate", ClassName);
            SetupDefaultDisplay();
            SetupDestinationList();
        }
        private void SetupDefaultDisplayLifter(DisplayPropsConfig displayProps)
        {
            Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter displayProps {1}", ClassName, displayProps.Lifter == null ? "== null" : displayProps.Lifter.DeviceKey);
            if (displayProps.Lifter != null)
            {
                var device_ = DeviceManager.GetDeviceForKey(displayProps.Lifter.DeviceKey);
                DefaultLifter = device_ as ShadeBase;
                //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter DefaultLifter {1}", ClassName, DefaultLifter == null ? "== null" : device_.Key);
                // device_ is IShadesOpenClosedFeedback
                if (device_ is IShadesOpenClosedFeedback)
                {
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter device_ is IShadesOpenClosedFeedback", ClassName);
                }
                if (device_ is ShadeBase)
                {
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter device_ is ShadeBase", ClassName);
                    var config_ = ConfigReader.ConfigObject.Devices.Find(x => x.Key == device_.Key);
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter config_ {1}", ClassName, config_ == null ? "== null" : config_.GetType().ToString());
                    var props_ = JsonConvert.DeserializeObject<RelayControlledShadeConfigProperties>(config_.Properties.ToString());
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter props_ {1}", ClassName, props_ == null ? "== null" : props_.GetType().ToString());

                    IOPortConfig relayConfig_ = props_.Relays.Open;
                    IKeyed csKey_ = DeviceManager.GetDeviceForKey(relayConfig_.PortDeviceKey);
                    var relay_ = (csKey_ as ISwitchedOutputCollection).SwitchedOutputs[relayConfig_.PortNumber];
                    Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter OPEN relay_ {1}", ClassName, relay_ == null ? "== null" : relayConfig_.PortNumber.ToString());
                    relay_.OutputIsOnFeedback.OutputChange += Lifter_OpenRelay_OutputIsOnFeedback_OutputChange;

                    relayConfig_ = props_.Relays.Close;
                    csKey_ = DeviceManager.GetDeviceForKey(relayConfig_.PortDeviceKey);
                    relay_ = (csKey_ as ISwitchedOutputCollection).SwitchedOutputs[relayConfig_.PortNumber];
                    Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter CLOSE relay_ {1}", ClassName, relay_ == null ? "== null" : relayConfig_.PortNumber.ToString());
                    relay_.OutputIsOnFeedback.OutputChange += Lifter_CloseRelay_OutputIsOnFeedback_OutputChange;
                }
                //Debug.Console(LogLevel, "{0} SetupDefaultDisplayLifter device_ configured", ClassName);
            }
        }
        private void SetupDefaultDisplayScreen(DisplayPropsConfig displayProps)
        {
            Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen displayProps {1}", ClassName, displayProps.Screen == null ? "== null" : displayProps.Screen.DeviceKey);
            if (displayProps.Screen != null)
            {
                var device_ = DeviceManager.GetDeviceForKey(displayProps.Screen.DeviceKey);
                DefaultScreen = device_ as ShadeBase;
                Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen DefaultScreen {1}", ClassName, DefaultScreen == null ? "== null" : device_.Key);

                if (device_ is ShadeBase)
                {
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen device_ is ShadeBase", ClassName);
                    var config_ = ConfigReader.ConfigObject.Devices.Find(x => x.Key == device_.Key);
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen config_ {1}", ClassName, config_ == null ? "== null" : config_.GetType().Name.ToString());
                    var props_ = JsonConvert.DeserializeObject<RelayControlledShadeConfigProperties>(config_.Properties.ToString());
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen props_ {1}", ClassName, props_ == null ? "== null" : props_.GetType().Name.ToString());

                    IOPortConfig relayConfig_ = props_.Relays.Open;
                    IKeyed csKey_ = DeviceManager.GetDeviceForKey(relayConfig_.PortDeviceKey);
                    var relay_ = (csKey_ as ISwitchedOutputCollection).SwitchedOutputs[relayConfig_.PortNumber];
                    Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen OPEN relay_ {1}", ClassName, relay_ == null ? "== null" : relayConfig_.PortNumber.ToString());
                    relay_.OutputIsOnFeedback.OutputChange += Screen_OpenRelay_OutputIsOnFeedback_OutputChange;

                    relayConfig_ = props_.Relays.Close;
                    csKey_ = DeviceManager.GetDeviceForKey(relayConfig_.PortDeviceKey);
                    relay_ = (csKey_ as ISwitchedOutputCollection).SwitchedOutputs[relayConfig_.PortNumber];
                    Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen CLOSE relay_ {1}", ClassName, relay_ == null ? "== null" : relayConfig_.PortNumber.ToString());
                    relay_.OutputIsOnFeedback.OutputChange += Screen_CloseRelay_OutputIsOnFeedback_OutputChange;
                }
                //Debug.Console(LogLevel, "{0} SetupDefaultDisplayScreen device_ configured", ClassName);
            }
        }
        private void SetupDefaultDisplay()
        {
            if(!String.IsNullOrEmpty(config.DefaultDisplayKey)) 
            {
                DefaultDisplay = DeviceManager.GetDeviceForKey(config.DefaultDisplayKey) as IRoutingSinkWithSwitching;
                Debug.Console(LogLevel, "{0} SetupDefaultDisplay {1}", ClassName, DefaultDisplay == null ? "== null" : DefaultDisplay.Key);
                if (DefaultDisplay != null)
                {
                    var dispTwoWay = DefaultDisplay as IHasPowerControlWithFeedback;
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplay, IHasPowerControlWithFeedback {1}", ClassName, dispTwoWay == null ? "== null" : DefaultDisplay.Key);
                    if (dispTwoWay != null)// Link power, warming, cooling to display
                        dispTwoWay.PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;
                    var dispWarmCool = DefaultDisplay as IWarmingCooling;
                    //Debug.Console(LogLevel, "{0} SetupDefaultDisplay, IWarmingCooling {1}", ClassName, dispWarmCool == null ? "== null" : DefaultDisplay.Key);
                    if (dispWarmCool != null)
                    {
                        dispWarmCool.IsWarmingUpFeedback.OutputChange   += IsWarmingUpFeedback_OutputChange; ;
                        dispWarmCool.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange; ;
                    }

                    var dispConfig_ = ConfigReader.ConfigObject.Devices.Find(x => x.Key == config.DefaultDisplayKey);
                    if (dispConfig_ != null)
                    {
                        //Debug.Console(LogLevel, "{0} SetupDefaultDisplay, found key in config", ClassName);
                        var props_ = JsonConvert.DeserializeObject<DisplayPropsConfig>(dispConfig_.Properties.ToString());
                        //Debug.Console(LogLevel, "{0} SetupDefaultDisplay props_ {1}", ClassName, props_ == null ? "== null" : config.DefaultDisplayKey);

                        SetupDefaultDisplayLifter(props_);
                        SetupDefaultDisplayScreen(props_);

                        /*
                         Debug.Console(LogLevel, "{0} SetupDefaultDisplay screenProps_ {1}", ClassName, props_.Screen == null ? "== null" : props_.Screen.DeviceKey);
                         if (props_.Screen != null)
                         {
                             var screen_ = DeviceManager.GetDeviceForKey(props_.Screen.DeviceKey);
                             var DefaultScreen = screen_ as ShadeBase;
                             Debug.Console(LogLevel, "{0} SetupDefaultDisplay DefaultScreen {1}", ClassName, DefaultScreen == null ? "== null" : DefaultScreen.Key);
                         }
                         */
                    }
                }
                else
                     Debug.Console(LogLevel, "{0} SetupDefaultDisplay config.DefaultDisplayKey {1}", ClassName, config.DefaultDisplayKey == null ? "== null" : config.DefaultDisplayKey);
           }
        }

        private void Screen_OpenRelay_OutputIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} Screen_OpenRelayFeedback: {1}", ClassName, e.BoolValue);
        }
        private void Screen_CloseRelay_OutputIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} Screen_CloseRelayFeedback: {1}", ClassName, e.BoolValue);
        }

        private void Lifter_OpenRelay_OutputIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} Lifter_OpenRelayFeedback: {1}", ClassName, e.BoolValue);
        }
        private void Lifter_CloseRelay_OutputIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} Lifter_CloseRelayFeedback: {1}", ClassName, e.BoolValue);
        }

        private void SetupDestinationList()
        {
            if (!String.IsNullOrEmpty(config.DestinationListKey))
            {
                var destinationList = ConfigReader.ConfigObject.DestinationLists[config.DestinationListKey];
                Debug.Console(LogLevel, "{0} SetupDestinationList {1}", ClassName, destinationList == null ? "== null" : destinationList.Count.ToString());
                Displays.Clear();
                if (destinationList != null)
                {
                    foreach (var destination in destinationList)
                    {
                        var dest = destination.Value.SinkDevice as IRoutingSinkWithSwitching;
                        if (dest != null)
                            Displays.Add(dest);
                        var dispTwoWay = dest as IHasPowerControlWithFeedback;
                        Debug.Console(LogLevel, "{0} SetupDisplays, IHasPowerControlWithFeedback {1}", ClassName, dispTwoWay == null ? "== null" : dest.Key);
                        if (dispTwoWay != null)
                        {
                            dispTwoWay.PowerIsOnFeedback.OutputChange -= PowerIsOnFeedback_OutputChange;
                            dispTwoWay.PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;
                        }
                        var dispWarmCool = dest as IWarmingCooling;
                        Debug.Console(LogLevel, "{0} SetupDisplays, IWarmingCooling {1}", ClassName, dispTwoWay == null ? "== null" : dest.Key);
                        if (dispWarmCool != null)
                        {
                            dispWarmCool.IsWarmingUpFeedback.OutputChange -= IsWarmingUpFeedback_OutputChange;
                            dispWarmCool.IsWarmingUpFeedback.OutputChange += IsWarmingUpFeedback_OutputChange;

                            dispWarmCool.IsCoolingDownFeedback.OutputChange -= IsCoolingDownFeedback_OutputChange;
                            dispWarmCool.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange;
                        }
                    }
                }
            }
        }

        void SetDefaultDisplayState(PowerStates state)
        {
            if(DefaultDisplayPowerState != state)
            {
                if (DefaultLifter != null)
                {
                    if (state == PowerStates.standby || state == PowerStates.cooling || state == PowerStates.off)
                        DefaultLifter.Close();
                    else if (state == PowerStates.on || state == PowerStates.warming)
                        DefaultLifter.Open();
                }
                DefaultDisplayPowerState = state;
            }
          
        }
        void IsCoolingDownFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var msg_ = ClassName;
            var device_ = sender as IKeyed;
            if (device_ != null)
                msg_ = msg_ + " " + device_.Key;
            msg_ = msg_ + " IsCoolingDownFeedback_OutputChange: " + e.BoolValue.ToString();
            SetDefaultDisplayState(e.BoolValue ? PowerStates.cooling : PowerStates.standby);
            msg_ = msg_ + ": " + DefaultDisplayPowerState.ToString();
            Debug.Console(LogLevel, msg_);
            //IsCoolingDownFeedback.FireUpdate();

        }

        void IsWarmingUpFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var msg_ = ClassName;
            var device_ = sender as IKeyed;
            if (device_ != null)
                msg_ = msg_ + " " + device_.Key;
            msg_ = msg_ + " IsWarmingUpFeedback_OutputChange: " + e.BoolValue.ToString();
            SetDefaultDisplayState(e.BoolValue ? PowerStates.warming : PowerStates.on);
            msg_ = msg_ + ": " + DefaultDisplayPowerState.ToString();
            Debug.Console(LogLevel, msg_);
            //IsWarmingUpFeedback.FireUpdate();
        }

        void PowerIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var msg_ = ClassName;
            var device_ = sender as IKeyed;
            if(device_ != null)
                msg_ = msg_ + " " + device_.Key;
            msg_ = msg_ + " PowerIsOnFeedback_OutputChange: " + e.BoolValue.ToString();
            var dispTwoWay = sender as IHasPowerControlWithFeedback;
            if (dispTwoWay != null)
            {
                if(DefaultDisplayPowerState != PowerStates.warming && DefaultDisplayPowerState != PowerStates.cooling)
                    SetDefaultDisplayState(dispTwoWay.PowerIsOnFeedback.BoolValue ? PowerStates.on : PowerStates.standby);
                msg_ = msg_ + ": " + DefaultDisplayPowerState.ToString();
                if (dispTwoWay.PowerIsOnFeedback.BoolValue != OnFeedback.BoolValue)
                {
                    msg_ = msg_ + ", updated";
                    //if (!dispTwoWay.PowerIsOnFeedback.BoolValue)
                    //    CurrentSourceInfo = null;
                    OnFeedback.FireUpdate();
                }
            }
            Debug.Console(LogLevel, msg_);
        }

        public void SetPowerOn()
        {
            Debug.Console(LogLevel, "{0} SetPowerOn", ClassName);
            foreach(var display in Displays)
            {
                var display_ = display as DisplayBase;
                if (display_ != null)
                {
                    Debug.Console(LogLevel, "{0}[{1}] SetPowerOn", ClassName, display_.Key);                        
                    display_.PowerOn();
                }
            }
        }
        public virtual void SetPowerOff()
        {
            Debug.Console(LogLevel, "{0} SetPowerOff", ClassName);
            foreach (var display in Displays)
            {
                var display_ = display as DisplayBase;
                if (display_ != null)
                {
                    Debug.Console(LogLevel, "{0}[{1}] SetPowerOff", ClassName, display_.Key);
                    display_.PowerOff();
                }
            }
        }
    }
}
