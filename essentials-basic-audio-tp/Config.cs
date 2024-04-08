using essentials_basic_room.Interfaces;
using essentials_custom_rooms_epi;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace essentials_basic_tp_epi
{
    public class Config : CrestronTouchpanelPropertiesConfig, IHasPassword
    {
        /// <summary>
        /// Model of touchpanel, e.g. "Tsw770"
        /// </summary>
        public string Type { get; set; }


        [JsonProperty("password")]
        public string Password { get; set; }
    }
}