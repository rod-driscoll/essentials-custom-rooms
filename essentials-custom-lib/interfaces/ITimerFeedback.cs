using PepperDash.Essentials.Core;

namespace avit_essentials_common.interfaces
{
    public interface ITimerFeedback
    {
        IntFeedback SecondsRemainingFeedback { get; }
        IntFeedback MilliSecondsRemainingFeedback { get; }
        IntFeedback SecondsElapsedFeedback { get; }
        IntFeedback MilliSecondsElapsedFeedback { get; }
    }
}
