using Newtonsoft.Json;

namespace essentials_advanced_room.Interfaces
{
    public interface IHasPassword
    {
        [JsonProperty("password")]
        string Password { get; set; }
    }
}
