using essentials_basic_room.Interfaces;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace essentials_advanced_tp
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