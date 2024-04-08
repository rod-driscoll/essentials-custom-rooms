using essentials_basic_room.Interfaces;
using Newtonsoft.Json;
using PepperDash.Essentials.Room.Config;

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
    }
}
