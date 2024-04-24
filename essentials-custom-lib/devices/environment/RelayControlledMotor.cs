// 20240330 Rod Driscoll <rod@theavitgroup.com.au>
// Copied PepperDash.Essentials.Devices.Common.Environment.Somfy.RelayControlledMotor
//  for debugging

using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.CrestronIO;
using PepperDash.Essentials.Core.Shades;
using System.Collections.Generic;

namespace PepperDash.Essentials.Devices.Common.Environment.Somfy
{
    /// <summary>
    /// Controls a single Motor using three relays
    /// </summary>
    public class RelayControlledMotor : ShadeBase, IShadesOpenCloseStop
    {
        RelayControlledMotorConfigProperties Config;

        ISwitchedOutput OpenRelay;
        ISwitchedOutput StopOrPresetRelay;
        ISwitchedOutput CloseRelay;

        int RelayPulseTime;

        public string StopOrPresetButtonLabel { get; set; }

        public RelayControlledMotor(string key, string name, RelayControlledMotorConfigProperties config)
            : base(key, name)
        {
            Config = config;

            RelayPulseTime = Config.RelayPulseTime;

            StopOrPresetButtonLabel = Config.StopOrPresetLabel;

        }

        public override bool CustomActivate()
        {
            //Debug.Console(1, this, "CustomActivate: '{0}'", this.Name);
            //Create ISwitchedOutput objects based on props
            OpenRelay = GetSwitchedOutputFromDevice(Config.Relays.Open);
            CloseRelay = GetSwitchedOutputFromDevice(Config.Relays.Close);
            Debug.Console(1, this, "CustomActivate: '{0}' open+close relays configured", this.Name);
            StopOrPresetRelay = GetSwitchedOutputFromDevice(Config.Relays.StopOrPreset);
            Debug.Console(1, this, "CustomActivate: '{0}' StopOrPreset relays configured", this.Name);

            return base.CustomActivate();
        }

        public override void Open()
        {
            Debug.Console(1, this, "Opening Motor: '{0}'", this.Name);

            PulseOutput(OpenRelay, RelayPulseTime);
        }

        public override void Stop()
        {
            Debug.Console(1, this, "Stopping Motor: '{0}'", this.Name);

            PulseOutput(StopOrPresetRelay, RelayPulseTime);
        }

        public override void Close()
        {
            Debug.Console(1, this, "Closing Motor: '{0}'", this.Name);

            PulseOutput(CloseRelay, RelayPulseTime);
        }

        void PulseOutput(ISwitchedOutput output, int pulseTime)
        {
            output.On();
            CTimer pulseTimer = new CTimer(new CTimerCallbackFunction((o) => output.Off()), pulseTime);
        }

        /// <summary>
        /// Attempts to get the port on teh specified device from config
        /// </summary>
        /// <param name="relayConfig"></param>
        /// <returns></returns>
        ISwitchedOutput GetSwitchedOutputFromDevice(IOPortConfig relayConfig)
        {               
            //Debug.Console(1, this, "GetSwitchedOutputFromDevice: '{0}'", this.Name);

            if (relayConfig == null)
            {
                Debug.Console(1, this, "GetSwitchedOutputFromDevice: '{0}' relayConfig==null", this.Name);
            }
            var portDevice = DeviceManager.GetDeviceForKey(relayConfig.PortDeviceKey);

            if (portDevice != null)
            {
                return (portDevice as ISwitchedOutputCollection).SwitchedOutputs[relayConfig.PortNumber];
            }
            else
            {
                Debug.Console(1, this, "Error: Unable to get relay on port '{0}' from device with key '{1}'", relayConfig.PortNumber, relayConfig.PortDeviceKey);
                return null;
            }
        }

    }

    public class RelayControlledMotorConfigProperties
    {
        public int RelayPulseTime { get; set; }
        public MotorRelaysConfig Relays { get; set; }
        public string StopOrPresetLabel { get; set; }

        public class MotorRelaysConfig
        {
            public IOPortConfig Open { get; set; }
            public IOPortConfig StopOrPreset { get; set; }
            public IOPortConfig Close { get; set; }
        }
    }

    public class RelayControlledMotorFactory : EssentialsPluginDeviceFactory<RelayControlledMotor>
    {
        public RelayControlledMotorFactory()
        {
            TypeNames = new List<string>() { "relaycontrolledmotor" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Generic Comm Device");
            var props = Newtonsoft.Json.JsonConvert.DeserializeObject<RelayControlledMotorConfigProperties>(dc.Properties.ToString());

            return new RelayControlledMotor(dc.Key, dc.Name, props);
        }
    }

}