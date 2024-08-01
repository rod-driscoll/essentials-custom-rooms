using Crestron.SimplSharp;
using essentials_basic_room.Functions;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System;

namespace essentials_basic_room
{
    public class Device : EssentialsRoomBase, IBasicRoom, IHasAudioDevice, IHasPowerFunction, IHasDisplayFunction, IHasSetTopBoxFunction
    {
        public string ClassName { get { return "IntermediateRoom-Device"; } }
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
                Debug.Console(LogLevel, this, "constructor starting");
                PropertiesConfig = JsonConvert.DeserializeObject<Config> (config.Properties.ToString());
                //Debug.Console(LogLevel, this, "{0} PropertiesConfig {1}", ClassName, PropertiesConfig == null ? "==null" : "exists");
 
                //Power = new RoomPower(PropertiesConfig);
                //Power.PowerChange += Power_PowerChange;
                //Audio = new RoomAudio(PropertiesConfig);
                //Display = new RoomDisplay(PropertiesConfig);
                //SetTopBox = new RoomSetTopBox(PropertiesConfig);
                InitializeRoom();
                Debug.Console(LogLevel, this, "constructor complete");
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        HttpLogoServer LogoServer;
        void InitializeRoom()
        {
            Debug.Console(LogLevel, this, "InitializeRoom");
            try
            {
                LogoServer = new HttpLogoServer(8080, Global.DirectorySeparator + "html" + Global.DirectorySeparator);
            }
            catch (Exception)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Notice, "NOTICE: Logo server cannot be started. Likely already running in another program");
            }
            Debug.Console(LogLevel, this, "InitializeRoom complete");        }

        public override bool CustomActivate()
        {
            Debug.Console(LogLevel, this, "CustomActivate");
            try
            {

            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, this, "ERROR: CustomActivate {0}", e.Message);
            }
            Debug.Console(LogLevel, this, "CustomActivate done");
            return base.CustomActivate();
        }

        /// <summary>
        /// Derived from EssentialsRoomBase
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.Console(LogLevel, this, "SetDefaultLevels");
            Audio?.SetDefaultLevels();
        }

        /// <summary>
        /// This is the real shutdown
        /// called from EssentialsRoomBase.Shutdown
        /// </summary>
        protected override void EndShutdown()
        {
            Debug.Console(LogLevel, this, "EndShutdown");
            RunRouteAction("roomOff");
            //Debug.Console(LogLevel, this, "Display {0}", Display==null?"== null":"exists");
            Display?.SetPowerOff();
            Audio?.PresetOffRecall();
            Power?.SetPowerOff();
        }
        public void StartUp()
        {
            Debug.Console(LogLevel, this, "StartUp");
            //Debug.Console(LogLevel, this, "Display {0}", Display == null ? "== null" : "exists");
            Display?.SetPowerOn();
            SetDefaultLevels();
            Power?.SetPowerOn();
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
            Debug.Console(LogLevel, this, "RunDefaultPresentRoute");
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
            Debug.Console(LogLevel, "Power_PowerChange, current: {0}, {1} seconds remaining", args.Current.ToString(), args.SecondsRemaining.ToString());
            try
            {
                OnFeedback.FireUpdate(); // if this errors then check OnFeedbackFunc
                Debug.Console(LogLevel, "OnFeedback.FireUpdate() done");
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "Power_PowerChange ERROR: {0}", e.Message);
            }
            Debug.Console(LogLevel, "Power_PowerChange done");
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
            Debug.Console(LogLevel, this, "PowerOnToDefaultOrLastSource not implemented");
        }
        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.Console(LogLevel, this, "RoomVacatedForTimeoutPeriod not implemented");
        }

        #endregion
    }
}
