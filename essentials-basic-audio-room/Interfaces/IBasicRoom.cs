using essentials_basic_room.Functions;
using essentials_custom_rooms_epi;
using PepperDash.Essentials.Core;

namespace essentials_basic_room_epi
{
    public interface IBasicRoom: IEssentialsRoom//, IHasCurrentVolumeControls
    {
        Config PropertiesConfig { get; }
        void StartUp();
    }
    public interface IBasicRoomSetup
    {
        void Setup(IBasicRoom room);
    }
    public interface IHasAudioDevice
    {
        RoomAudio Audio { get; set; }
    }
    public interface IHasPowerFunction
    {
        RoomPower Power { get; set; }
    }

}