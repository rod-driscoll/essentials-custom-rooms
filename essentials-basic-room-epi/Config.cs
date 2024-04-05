using Newtonsoft.Json;
using PepperDash.Essentials.Room.Config;

namespace essentials_custom_rooms_epi
{
    public interface IHasPassword
    {
        [JsonProperty("password")]
        string Password { get; set; }
    }

    public class Config : EssentialsRoomPropertiesConfig, IHasPassword
    {

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
