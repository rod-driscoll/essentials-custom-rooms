using essentials_basic_room.Functions;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace essentials_basic_room_epi
{
    public interface IBasicRoom : IEssentialsRoom, ILogClassDetails
    {
        Config PropertiesConfig { get; }
        void StartUp();
    }
    public interface ILogClassDetails
    {
        uint LogLevel { set; }
        string ClassName { get; }
    }
    /// <summary>
    /// drivers contain this so they can set up when the attached room changes
    /// </summary>
    public interface IBasicRoomSetup: ILogClassDetails
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
    public interface IHasDisplayFunction
    {
        RoomDisplay Display { get; set; }
    }
    public interface ILifterConfig
    {
        [JsonProperty("lifter")]
        LifterConfig Lifter { get; set; }
    }
    public interface IScreenConfig
    {
        [JsonProperty("screen")]
        ScreenConfig Screen { get; set; }
    }

}