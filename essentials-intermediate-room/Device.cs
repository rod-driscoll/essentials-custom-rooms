using Crestron.SimplSharp;
using essentials_basic_room.Functions;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System;

namespace essentials_basic_room_epi
{
    public class Device : EssentialsRoomBase, IBasicRoom, IHasAudioDevice, IHasPowerFunction, IHasDisplayFunction, IHasSetTopBoxFunction
    {
        public string ClassName { get { return "DefaultSetTopBox"; } }
        public uint LogLevel { get; set; }

        public Config PropertiesConfig { get; private set; }

        // Room drivers
        public RoomPower Power { get; set; }
        public RoomAudio Audio { get; set; }
        public RoomDisplay Display { get; set; }
        public RoomSetTopBox SetTopBox { get; set; }

        public Device(DeviceConfig config)
            : base(config)
        {
            try
            {
                LogLevel = 0;
                Debug.Console(LogLevel, this, "{0} constructor starting", ClassName);
                PropertiesConfig = JsonConvert.DeserializeObject<Config> (config.Properties.ToString());
                //Debug.Console(LogLevel, this, "{0} PropertiesConfig {1}", ClassName, PropertiesConfig == null ? "==null" : "exists");
 
                Power = new RoomPower(PropertiesConfig);
                Power.PowerChange += Power_PowerChange;

                Audio = new RoomAudio(PropertiesConfig);

                Display = new RoomDisplay(PropertiesConfig);

                SetTopBox = new RoomSetTopBox(PropertiesConfig);
                InitializeRoom();
                Debug.Console(LogLevel, this, "{0} constructor complete", ClassName);
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        void InitializeRoom()
        {
            //Debug.Console(LogLevel, this, "{0} InitializeRoom", ClassName);
            //Debug.Console(LogLevel, this, "{0} InitializeRoom complete", ClassName);
        }

        public override bool CustomActivate()
        {
            Debug.Console(LogLevel, this, "{0} CustomActivate", ClassName);
            try
            {

            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, this, "{0} ERROR: CustomActivate {1}", ClassName, e.Message);
            }
            Debug.Console(LogLevel, this, "{0} CustomActivate done", ClassName);
            return base.CustomActivate();
        }

        /// <summary>
        /// Derived from EssentialsRoomBase
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.Console(LogLevel, this, "{0} SetDefaultLevels", ClassName);
            Audio.SetDefaultLevels();
        }

        /// <summary>
        /// This is the real shutdown
        /// called from EssentialsRoomBase.Shutdown
        /// </summary>
        protected override void EndShutdown()
        {
            Debug.Console(LogLevel, this, "{0} EndShutdown", ClassName);
            RunRouteAction("roomOff");
            Audio.PresetOffRecall();
            Power.SetPowerOff();
            Display.SetPowerOff();
        }

        public void StartUp()
        {
            Debug.Console(LogLevel, this, "{0} StartUp", ClassName);
            SetDefaultLevels();
            Power.SetPowerOn();
            Display.SetPowerOn();
        }

        CCriticalSection RunRouteLock = new CCriticalSection();
        public void RunRouteAction(string routeKey, Action successCallback)
        {
            // Run this on a separate thread
            new CTimer(o =>
            {
                RunRouteLock.TryEnter(); // try to prevent multiple simultaneous selections
                try
                {
                    Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Run route action '{0}'", routeKey);

                    if(routeKey != "roomOff")
                        StartUp();

                    // report back when done
                    if (successCallback != null)
                        successCallback();
                }
                catch (Exception e)
                {
                    Debug.Console(1, this, "ERROR in routing: {0}", e);
                }
                RunRouteLock.Leave();
            }, 0); // end of CTimer
        }
        public void RunRouteAction(string routeKey)
        {
            RunRouteAction(routeKey, new Action(() => { }));
        }
        public override bool RunDefaultPresentRoute()
        {
            Debug.Console(LogLevel, this, "{0} RunDefaultPresentRoute", ClassName);
            RunRouteAction("defaultRoute");
            return true;
        }
        /// <summary>
        /// Subsrciption from RoomPower, called when power changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Power_PowerChange(object sender, PowerEventArgs args)
        {
            Debug.Console(LogLevel, "{0} Power_PowerChange, current: {1}, {2} seconds remaining", ClassName, args.Current.ToString(), args.SecondsRemaining.ToString());
            try
            {
                OnFeedback.FireUpdate(); // if this errors then check OnFeedbackFunc
                Debug.Console(LogLevel, "{0} OnFeedback.FireUpdate() done", ClassName);
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} Power_PowerChange ERROR: {1}", ClassName, e.Message);
            }
            Debug.Console(LogLevel, "{0} Power_PowerChange done", ClassName);
        }

        #region power interface definitions
        // these interface definitions have to be here, it'd be better to put them inside of RoomPower.cs, but that would require major code re-structuring
        protected override Func<bool> OnFeedbackFunc        { get { return () => { return Power.PowerStatus == PowerStates.on; }; } }
        protected override Func<bool> IsWarmingFeedbackFunc { get { return () => { return Power.PowerStatus == PowerStates.warming; ; }; } }
        protected override Func<bool> IsCoolingFeedbackFunc { get { return () => { return Power.PowerStatus == PowerStates.cooling; ; }; } }

        #endregion powerinterface definitions

        #region unused interface definitions

        public override void PowerOnToDefaultOrLastSource()
        {
            Debug.Console(LogLevel, this, "{0} PowerOnToDefaultOrLastSource not implemented", ClassName);
        }
        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.Console(LogLevel, this, "{0} RoomVacatedForTimeoutPeriod not implemented", ClassName);
        }

        #endregion
    }
}
