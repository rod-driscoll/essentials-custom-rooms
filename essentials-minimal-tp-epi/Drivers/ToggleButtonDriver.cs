using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;

namespace essentials_minimal_tp_epi.Drivers
{
    public class ToggleButtonDriver : PanelDriverBase
    {
        public string ClassName { get { return "ToggleButtonDriver"; } }
        public ToggleButtonDriver(BasicTriListWithSmartObject triList) : base(triList)
        {
            TriList.SetSigTrueAction(UiJoins.ToggleButtonJoin, () => ToggleButtonPress(UiJoins.ToggleButtonJoin));
            Debug.Console(2, "{0} loaded", ClassName);
        }

        public void ToggleButtonPress(uint join)
        {
            bool state_ = TriList.BooleanInput[join].BoolValue;
            Debug.Console(2, "{0} ToggleButtonPress[{1}]: {2}", ClassName, join, state_);
            TriList.SetBool(join, !state_);           
        }
    }
}
