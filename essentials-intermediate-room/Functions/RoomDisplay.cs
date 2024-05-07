using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System.Collections.Generic;

namespace essentials_basic_room.Functions
{
    public class RoomDisplay: IHasDefaultDisplay, ILogClassDetails
    {
        public string ClassName { get { return "RoomDisplay"; } }
        public uint LogLevel { get; set; }
        public Config config { get; private set; }

        private List<IRoutingSinkWithSwitching> Displays;
        public IRoutingSinkWithSwitching DefaultDisplay { get; private set; }
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
        private void SetupDefaultDisplay()
        {
            DefaultDisplay = DeviceManager.GetDeviceForKey(config.DefaultDisplayKey) as IRoutingSinkWithSwitching;
            Debug.Console(LogLevel, "{0} SetupDefaultDisplay {1}", ClassName, DefaultDisplay == null ? "== null" : DefaultDisplay.Key);

            if (DefaultDisplay != null)
            {
                var dispTwoWay = DefaultDisplay as IHasPowerControlWithFeedback;
                Debug.Console(LogLevel, "{0} SetupDefaultDisplay, IHasPowerControlWithFeedback {1}", ClassName, dispTwoWay == null ? "== null" : DefaultDisplay.Key);
                if (dispTwoWay != null)// Link power, warming, cooling to display
                    dispTwoWay.PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;
                var dispWarmCool = DefaultDisplay as IWarmingCooling;
                Debug.Console(LogLevel, "{0} SetupDefaultDisplay, IWarmingCooling {1}", ClassName, dispTwoWay == null ? "== null" : DefaultDisplay.Key);
                if (dispWarmCool != null)
                {
                    dispWarmCool.IsWarmingUpFeedback.OutputChange   += IsWarmingUpFeedback_OutputChange; ;
                    dispWarmCool.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange; ;
                }
            }
        }
        private void SetupDestinationList()
        {
            var destinationList = ConfigReader.ConfigObject.DestinationLists[config.DestinationListKey];
            Displays.Clear();
            foreach (var destination in destinationList)
            {
                var dest = destination.Value.SinkDevice as IRoutingSinkWithSwitching;

                if (dest != null)
                {
                    Displays.Add(dest);
                }

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

        void IsCoolingDownFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var msg_ = ClassName;
            var device_ = sender as IKeyed;
            if (device_ != null)
                msg_ = msg_ + " " + device_.Key;
            msg_ = msg_ + " IsCoolingDownFeedback_OutputChange: " + e.BoolValue.ToString();
            DefaultDisplayPowerState = e.BoolValue ? PowerStates.cooling : PowerStates.standby;
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
            DefaultDisplayPowerState = e.BoolValue ? PowerStates.warming: PowerStates.on;
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
                    DefaultDisplayPowerState = dispTwoWay.PowerIsOnFeedback.BoolValue? PowerStates.on: PowerStates.standby;
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
