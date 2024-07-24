using essentials_basic_room.Interfaces;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Room.Config;
using System.Collections.Generic;

namespace essentials_basic_room_epi
{
    public class Config : EssentialsRoomPropertiesConfig, IHasPassword
    {

        [JsonProperty("password")]
        public string Password { get; set; }

        #region display config

        [JsonProperty("defaultDisplayKey")]
        public string DefaultDisplayKey { get; set; }

        [JsonProperty("destinationListKey")]
        public string DestinationListKey { get; set; }
        
        #endregion display config

        #region audio config

        /// <summary>
        /// The key of the default audio device
        /// </summary>
        [JsonProperty("defaultAudioKey")]
        public string DefaultAudioKey { get; set; }

        [JsonProperty("defaultMicKey")]
        public string DefaultMicKey { get; set; }

        [JsonProperty("faders")]
        public Dictionary<string, BasicVolumeLevelConfig> Faders { get; set; }

        [JsonProperty("audioPresets")]
        public Dictionary<string, BasicAudioPresetConfig> AudioPresets { get; set; }

        #endregion audio config

        #region settopbox config

        [JsonProperty("defaultSetTopBoxKey")]
        public string DefaultSetTopBoxKey { get; set; }
        [JsonProperty("setTopBoxPresetsURL")]
        public string SetTopBoxPresetsURL { get; set; }

        #endregion settopbox config
    }

    /// <summary>
    /// an array of devices
    /// </summary>
    public class BasicAudioPresetConfig
    {
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("function")]
        public string Function { get; set; }
    }

    /// <summary>
    /// an array of devices
    /// </summary>
    public class BasicVolumeLevelConfig
    {
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

    }

    public class DisplayPropsConfig
    {
        [JsonProperty("control")]
        public EssentialsControlPropertiesConfig Control { get; set; }

        [JsonProperty("lifter")]
        public LifterConfig Lifter { get; set; }

        [JsonProperty("screen")]
        public ScreenConfig Screen { get; set; }
    }

    public class LifterConfig
    {
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        [JsonProperty("downTriggersDisplayOn")]
        public bool DownTriggersDisplayOn { get; set; }
        [JsonProperty("displayOffTriggersUp")]
        public bool DisplayOffTriggersUp { get; set; }
    }
    public class ScreenConfig
    {
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        [JsonProperty("downTriggersDisplayOn")]
        public bool DownTriggersDisplayOn { get; set; }
        [JsonProperty("displayOnTriggersDown")]
        public bool DisplayOnTriggersDown { get; set; }
        [JsonProperty("upTriggersDisplayOff")]
        public bool UpTriggersDisplayOff { get; set; }
        [JsonProperty("displayOffTriggersUp")]
        public bool DisplayOffTriggersUp { get; set; }
    }
}
