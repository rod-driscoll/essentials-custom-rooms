using Crestron.SimplSharpPro;
using System.Text;

namespace avit_essentials_common.IRPorts
{
    public interface IIROutputPort
    {
        int IRDriversLoadedCount { get; }
        string[] AvailableIRCmds();
        string[] AvailableIRCmds(uint IRDriverID);
        string[] AvailableStandardIRCmds();
        string[] AvailableStandardIRCmds(uint IRDriverID);
        string GetStandardCmdFromIRCmd(string IRCommand);
        string GetStandardCmdFromIRCmd(uint IRDriverID, string IRCommand);
        string IRDriverFileNameByIRDriverId(uint IRDriverId);
        uint IRDriverIdByFileName(string IRFileName);
        bool IsIRCommandAvailable(string IRCmdName);
        bool IsIRCommandAvailable(uint IRDriverID, string IRCmdName);
        uint LoadIRDriver(string IRFileName);
        void Press(string IRCmdName);
        void Press(uint IRDriverID, string IRCmdName);
        void PressAndRelease(string IRCmdName, ushort TimeOutInMS);
        void PressAndRelease(uint IRDriverID, string IRCmdName, ushort TimeOutInMS);
        void Release();
        void SendSerialData(string SerialDataToSend);
        void SetIRSerialSpec(eIRSerialBaudRates baudRate, eIRSerialDataBits numberOfDataBits, eIRSerialParityType parityType, eIRSerialStopBits numStopBits, Encoding stringEncoding);
        void UnloadAllIRDrivers();
        void UnloadIRDriver();
        void UnloadIRDriver(uint IRDriverIDtoUnload);
    }
}
