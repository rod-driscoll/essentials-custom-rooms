using essentials_basic_room.Interfaces;
using Newtonsoft.Json;
using PepperDash.Essentials.Room.Config;
using System.Collections.Generic;

namespace essentials_custom_rooms_epi
{
    public class Config : EssentialsRoomPropertiesConfig, IHasPassword
    {

        [JsonProperty("password")]
        public string Password { get; set; }

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
        public Dictionary<string, BasicVolumeLevelConfig> AudioPresets { get; set; }
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
}
