using Newtonsoft.Json;

namespace essentials_basic_room.Interfaces
{
    public interface IHasPassword
    {
        [JsonProperty("password")]
        string Password { get; set; }
    }
}
