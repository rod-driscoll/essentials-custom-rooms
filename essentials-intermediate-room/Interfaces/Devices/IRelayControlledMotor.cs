using PepperDash.Essentials.Core.Shades;
using PepperDash.Essentials.Core;

namespace relay_controlled_motor_epi.Interfaces
{
    // not used because it would need to be put into PepperDash.Core or a similar core library which is yet another dependency to lose
    public interface IOpenClosedFeedback
    {
        BoolFeedback IsOpenFeedback { get; }
        BoolFeedback IsClosedFeedback { get; }
    }
    public interface IRelayControlledMotor : IShadesOpenCloseStop, IOpenClosedFeedback
    {
        StringFeedback StatusFeedback { get; }
        IntFeedback PercentOpenFeedback { get; }
        IntFeedback RemainingFeedback { get; }
        BoolFeedback IsStoppedFeedback { get; }
        BoolFeedback IsOpeningFeedback { get; }
        BoolFeedback IsClosingFeedback { get; }
    }
}
