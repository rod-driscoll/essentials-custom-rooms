using Crestron.SimplSharpPro;
using System.Collections.Generic;

namespace avit_essentials_common.IRPorts
{
    public interface IIROutputPortsAdvanced: IIROutputPorts
    {
        //
        // Summary:
        //     Collection of IR output ports on the device.
        Dictionary<int, IIROutputPort> IROutputPortsDict { get; set; }
    }
}
