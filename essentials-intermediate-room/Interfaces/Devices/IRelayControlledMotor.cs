using PepperDash.Essentials.Core.Shades;
using PepperDash.Essentials.Core;

namespace relay_controlled_motor_epi.Interfaces
{
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
