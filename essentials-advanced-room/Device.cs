using Crestron.SimplSharp;
using essentials_advanced_room.Functions;
using essentials_advanced_room.Functions.Audio;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using Serilog.Events;
using System;
using DisplayBase = PepperDash.Essentials.Devices.Common.Displays.DisplayBase;

namespace essentials_advanced_room
{
    public class Device : EssentialsRoomBase, IAdvancedRoom, IHasAudioDevice, IHasPowerFunction, IHasDisplayFunction, IHasSetTopBoxFunction
    {
        public string ClassName { get { return "IntermediateRoom-Device"; } }
        public LogEventLevel LogLevel { get; set; }

        public Config PropertiesConfig { get; private set; }

        // Room drivers
        public RoomPower Power { get; set; }    
        public RoomAudio Audio { get; set; }
        public RoomDisplay Display { get; set; }
        public RoomSetTopBox SetTopBox { get; set; }

        bool loadAudio;
        bool loadDisplay;
        bool loadSetTopBox;

        //HttpLogoServer LogoServer; // no need for this, just define "logo":{"type":"system"} in a room properties config

        public Device(DeviceConfig config)
            : base(config)
        {
            try
            {
                LogLevel = LogEventLevel.Verbose;
                Debug.LogMessage(LogLevel, this, "constructor starting");
                PropertiesConfig = JsonConvert.DeserializeObject<Config> (config.Properties.ToString());
                //Debug.LogMessage(LogLevel, this, "{0} PropertiesConfig {1}", ClassName, PropertiesConfig == null ? "==null" : "exists");
 
                Power = new RoomPower(PropertiesConfig);
                Power.PowerChange += Power_PowerChange;

                // load drivers only if associated devices or config exists
                foreach (var dev in DeviceManager.GetDevices())
                {
                    //if (dev.Group.Equals("dipslays") && !loadDisplay)
                    if (dev is DisplayBase && !loadDisplay)
                    {
                        Debug.LogMessage(LogLevel, "{0} Loading RoomDisplay", ClassName);
                        Display = new RoomDisplay(PropertiesConfig);
                        loadDisplay = true;
                    }
                    //else if (dev.Group.StartsWith("settopbox") && !loadSetTopBox)
                    else if (dev is ISetTopBoxControls && !loadSetTopBox)
                    {
                        Debug.LogMessage(LogLevel, "{0} Loading SetTopBoxDriver", ClassName);
                        SetTopBox = new RoomSetTopBox(PropertiesConfig);
                        loadSetTopBox = true;
                    }
                }
                foreach (var dev in ConfigReader.ConfigObject.Devices)
                {
                    //if (dev is IAdvancedRoomSetup && !loadAudio)
                    if (dev.Group.StartsWith("audio") && !loadAudio)
                    {
                        Debug.LogMessage(LogLevel, "{0} Loading BasicAudioDriver", ClassName);
                        Audio = new RoomAudio(PropertiesConfig);
                        loadAudio = true;
                    }
                }

                InitializeRoom();
                Debug.LogMessage(LogLevel, this, "constructor complete");
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogEventLevel.Debug, this, "Error building room: \n{0}", e);
            }
        }

        void InitializeRoom()
        {
            Debug.LogMessage(LogLevel, this, "InitializeRoom");
            try
            {
                //LogoServer = new HttpLogoServer(8080, Global.DirectorySeparator + "html" + Global.DirectorySeparator);
            }
            catch (Exception)
            {
                Debug.LogMessage(LogEventLevel.Warning, "NOTICE: Logo server cannot be started. Likely already running in another program");
            }
            Debug.LogMessage(LogLevel, this, "InitializeRoom complete");        }

        public override bool CustomActivate()
        {
            Debug.LogMessage(LogLevel, this, "CustomActivate");
            try
            {

            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, this, "ERROR: CustomActivate {0}", e.Message);
            }
            Debug.LogMessage(LogLevel, this, "CustomActivate done");
            return base.CustomActivate();
        }

        /// <summary>
        /// Derived from EssentialsRoomBase
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.LogMessage(LogLevel, this, "SetDefaultLevels");
            Audio?.SetDefaultLevels();
        }

        /// <summary>
        /// This is the real shutdown
        /// called from EssentialsRoomBase.Shutdown
        /// </summary>
        protected override void EndShutdown()
        {
            Debug.LogMessage(LogLevel, this, "EndShutdown");
            RunRouteAction("roomOff");
            //Debug.LogMessage(LogLevel, this, "Display {0}", Display==null?"== null":"exists");
            Display?.SetPowerOff();
            Audio?.PresetOffRecall();
            Power?.SetPowerOff();
        }
        public void StartUp()
        {
            Debug.LogMessage(LogLevel, this, "StartUp");
            //Debug.LogMessage(LogLevel, this, "Display {0}", Display == null ? "== null" : "exists");
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
                    Debug.LogMessage(LogEventLevel.Warning, "Run route action '{0}'", routeKey);

                    if(routeKey != "roomOff")
                        StartUp();

                    // report back when done
                    if (successCallback != null)
                        successCallback();
                }
                catch (Exception e)
                {
                    Debug.LogMessage(LogEventLevel.Debug, this, "ERROR in routing: {0}", e);
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
            Debug.LogMessage(LogLevel, this, "RunDefaultPresentRoute");
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
            Debug.LogMessage(LogLevel, "Power_PowerChange, current: {0}, {1} seconds remaining", args.Current.ToString(), args.SecondsRemaining.ToString());
            try
            {
                OnFeedback.FireUpdate(); // if this errors then check OnFeedbackFunc
                Debug.LogMessage(LogLevel, "OnFeedback.FireUpdate() done");
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, "Power_PowerChange ERROR: {0}", e.Message);
            }
            Debug.LogMessage(LogLevel, "Power_PowerChange done");
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
            Debug.LogMessage(LogLevel, this, "PowerOnToDefaultOrLastSource not implemented");
        }
        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.LogMessage(LogLevel, this, "RoomVacatedForTimeoutPeriod not implemented");
        }

        #endregion
    }
}
