using Crestron.SimplSharp;
using essentials_basic_room.Functions;
using essentials_custom_rooms_epi;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System;

namespace essentials_basic_room_epi
{
    public class Device : EssentialsRoomBase, IBasicRoom, IHasAudioDevice, IHasPowerFunction
    {
        public string ClassName = "Device";

        public Config PropertiesConfig { get; private set; }

        public RoomPower Power { get; set; }
        public RoomAudio Audio { get; set; }

        public string Test;

        public Device(DeviceConfig config)
            : base(config)
        {
            try
            {
                Debug.Console(2, this, "{0} constructor starting", ClassName);
                PropertiesConfig = JsonConvert.DeserializeObject<Config> (config.Properties.ToString());
                //Debug.Console(2, this, "{0} PropertiesConfig {1}", ClassName, PropertiesConfig == null ? "==null" : "exists");
                Power = new RoomPower(this, PropertiesConfig);
                Power.PowerChange += Power_PowerChange;
                Audio = new RoomAudio(PropertiesConfig);
                Debug.Console(2, this, "{0} RoomAudio created", ClassName);
                Debug.Console(2, this, "{0} Room as IBasicRoom {1}", ClassName, (this as IBasicRoom) == null ? "==null" : "exists");

                InitializeRoom();
                Debug.Console(2, this, "{0} constructor complete", ClassName);
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error building room: \n{0}", e);
            }
        }

        void InitializeRoom()
        {
            //Debug.Console(2, this, "{0} InitializeRoom", ClassName);
            //Debug.Console(2, this, "{0} InitializeRoom complete", ClassName);
        }

        public override bool CustomActivate()
        {
            Debug.Console(2, this, "{0} CustomActivate", ClassName);
            try
            {

            }
            catch (Exception e)
            {
                Debug.Console(2, this, "{0} ERROR: CustomActivate {1}", ClassName, e.Message);
            }
            Debug.Console(2, this, "{0} CustomActivate done", ClassName);
            return base.CustomActivate();
        }

        /// <summary>
        /// Derived from EssentialsRoomBase
        /// </summary>
        public override void SetDefaultLevels()
        {
            Debug.Console(2, this, "{0} SetDefaultLevels", ClassName);
            Audio.SetDefaultLevels();
        }

        /// <summary>
        /// This is the real shutdown
        /// called from EssentialsRoomBase.Shutdown
        /// </summary>
        protected override void EndShutdown()
        {
            Debug.Console(2, this, "{0} EndShutdown", ClassName);
            Audio.PresetOffRecall();
            Power.SetPowerOff();
        }

        public void StartUp()
        {
            Debug.Console(2, this, "{0} StartUp", ClassName);
            SetDefaultLevels();
            Power.SetPowerOn();
        }
        public void RunRouteAction(string routeKey, Action successCallback)
        {
            // Run this on a separate thread
            new CTimer(o =>
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Run route action '{0}'", routeKey);

                StartUp();

                // report back when done
                if (successCallback != null)
                    successCallback();

            }, 0); // end of CTimer
        }
        public void RunRouteAction(string routeKey)
        {
            RunRouteAction(routeKey, new Action(() => { }));
        }
        public override bool RunDefaultPresentRoute()
        {
            Debug.Console(2, this, "{0} RunDefaultPresentRoute", ClassName);
            RunRouteAction("");
            return true;
        }
        /// <summary>
        /// Subsrciption from RoomPower, called when power changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Power_PowerChange(object sender, PowerEventArgs args)
        {
            Debug.Console(2, "{0} Power_PowerChange, current: {1}, {2} seconds remaining", ClassName, args.Current.ToString(), args.SecondsRemaining.ToString());
            try
            {
                OnFeedback.FireUpdate(); // if this errors then check OnFeedbackFunc
                Debug.Console(2, "{0} OnFeedback.FireUpdate() done", ClassName);
            }
            catch (Exception e)
            {
                Debug.Console(2, "{0} Power_PowerChange ERROR: {1}", ClassName, e.Message);
            }
            Debug.Console(2, "{0} Power_PowerChange done", ClassName);
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
            Debug.Console(2, this, "{0} PowerOnToDefaultOrLastSource not implemented", ClassName);
        }
        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.Console(2, this, "{0} RoomVacatedForTimeoutPeriod not implemented", ClassName);
        }

        #endregion
    }
}
