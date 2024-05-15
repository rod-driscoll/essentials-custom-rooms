using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentials_basic_room.Functions
{
    public class MotorisedLifter: ILogClassDetails
    {
        public string ClassName { get { return "Lifter"; } }
        public uint LogLevel { get; set; }
        public Config config { get; private set; }

        public MotorisedLifter(Config config)
        {
            LogLevel = 0;
            Debug.Console(LogLevel, "{0} constructor", ClassName);
            this.config = config;
        }

    }
}
