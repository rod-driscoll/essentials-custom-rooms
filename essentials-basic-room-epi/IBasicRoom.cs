using essentials_custom_rooms_epi;
using PepperDash.Essentials.Core;

namespace essentials_basic_room_epi
{
    public interface IBasicRoom: IEssentialsRoom
    {
        Config PropertiesConfig { get; }
    }
}