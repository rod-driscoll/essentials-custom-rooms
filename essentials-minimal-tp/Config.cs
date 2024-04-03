using PepperDash.Essentials.Core;

namespace essentials_minimal_tp
{
    public class Config : CrestronTouchpanelPropertiesConfig
    {
        /// <summary>
        /// Model of touchpanel, e.g. "Tsw770"
        /// </summary>
        public string Type { get; set; }
    }
}