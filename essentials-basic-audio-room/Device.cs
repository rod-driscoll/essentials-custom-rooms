using essentials_basic_room.Functions;
using essentials_custom_rooms_epi;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System;

namespace essentials_basic_room_epi
{
    public class Device : EssentialsRoomBase, IBasicRoom, IHasAudioDevice//, IHasCurrentVolumeControls
    {
        public string ClassName = "Device";

        public Config PropertiesConfig { get; private set; }

        public RoomAudio Audio { get; set; }

        public Device(DeviceConfig config)
            : base(config)
        {
            try
            {
                Debug.Console(2, this, "{0} constructor starting", ClassName);
                PropertiesConfig = JsonConvert.DeserializeObject<Config> (config.Properties.ToString());
                //Debug.Console(2, this, "{0} PropertiesConfig {1}", ClassName, PropertiesConfig == null ? "==null" : "exists");
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

        #region unused interface definitions
        protected override Func<bool> OnFeedbackFunc { get { return () => { return false; }; } }
        protected override Func<bool> IsWarmingFeedbackFunc { get { return () => { return false; }; } }
        protected override Func<bool> IsCoolingFeedbackFunc { get { return () => { return false; }; } }

        protected override void EndShutdown()
        {
            Debug.Console(2, this, "{0} EndShutdown not implemented", ClassName);
        }
        public override void PowerOnToDefaultOrLastSource()
        {
            Debug.Console(2, this, "{0} PowerOnToDefaultOrLastSource not implemented", ClassName);
        }
        public override bool RunDefaultPresentRoute()
        {
            Debug.Console(2, this, "{0} RunDefaultPresentRoute not implemented", ClassName);
            return false;
        }
        public override void RoomVacatedForTimeoutPeriod(object o)
        {
            Debug.Console(2, this, "{0} RoomVacatedForTimeoutPeriod not implemented", ClassName);
        }

        #endregion
    }
}
