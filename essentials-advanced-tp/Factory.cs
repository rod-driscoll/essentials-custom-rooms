using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace essentials_advanced_tp
{
    /// <summary>
    /// Plugin device factory for devices that use IBasicCommunication
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed
    /// </remarks>
    /// <example>
    /// "EssentialsPluginFactoryTemplate" renamed to "HelloWorldFactory"
    /// </example>
    public class Factory : EssentialsPluginDeviceFactory<Device>
    {
        /// <summary>
        /// Plugin device factory constructor
        /// </summary>
        /// <remarks>
        /// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
        /// </remarks>
        /// <example>
        /// Set the minimum Essentials Framework Version
        /// <code>
        /// MinimumEssentialsFrameworkVersion = "1.6.4;
        /// </code>
        /// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
        /// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
        /// </code>
        /// </example>
        public Factory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";
            TypeNames = new List<string>() { "advanced-tp" };
        }

        /// <summary>
        /// Builds and returns an instance of EssentialsPluginDeviceTemplate
        /// </summary>
        /// <param name="dc">device configuration</param>
        /// <returns>plugin device or null</returns>
        /// <remarks>		
        /// The example provided below takes the device key, name, properties config and the comms device created.
        /// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
        /// </remarks>
        /// <seealso cref="PepperDash.Core.eControlMethod"/>
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            try
            {
                Debug.LogMessage(LogEventLevel.Warning, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);
                var comm = CommFactory.GetControlPropertiesConfig(dc);
                var props = JsonConvert.DeserializeObject<Config>(dc.Properties.ToString());

                var panel = GetPanelForType(props.Type, comm.IpIdInt, String.Empty);
                Debug.LogMessage(LogEventLevel.Information, "[{0}] Factory, panel {1}", dc.Key, panel == null ? "== null" : "exists");
                if (panel == null)
                    Debug.LogMessage(LogEventLevel.Information, "Unable to create Touchpanel for type {0}. Touchpanel Controller WILL NOT function correctly", dc.Type);

                var panelController = new Device(dc.Key, dc.Name, panel, props);
                Debug.LogMessage(LogEventLevel.Information, "[{0}] Factory, panelController {1}", dc.Key, panelController == null ? "== null" : "exists");

                return panelController;
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogEventLevel.Error, "{0} BuildDevice ERROR: {1} ", TypeNames[0], e.Message);
                throw new Exception(e.Message);
            }
        }
        private BasicTriListWithSmartObject GetPanelForType(string panelType, uint id, string projectName)
        {
            try
            {
                var type = Regex.Replace(panelType, @"[^a-zA-Z0-9]", string.Empty).ToLower();
                Debug.LogMessage(LogEventLevel.Information, "[{0}] Factory Attempting to GetPanelForType: {1}", TypeNames[0], type);
                Debug.LogMessage(LogEventLevel.Information, "[{0}] Factory, {1} {2}", TypeNames[0], type, Global.ControlSystem == null ? "== null" : String.Format("exists p:{0}", Global.ControlSystem.ProgramNumber));
                if (type == "tsw550")
                    return new Tsw550(id, Global.ControlSystem);
                else if (type == "tsw552")
                    return new Tsw552(id, Global.ControlSystem);
                else if (type == "tsw560")
                    return new Tsw560(id, Global.ControlSystem);
                else if (type == "tsw570")
                    return new Tsw570(id, Global.ControlSystem);
                else if (type == "tsw750")
                    return new Tsw750(id, Global.ControlSystem);
                else if (type == "tsw752")
                    return new Tsw752(id, Global.ControlSystem);
                else if (type == "tsw760")
                    return new Tsw760(id, Global.ControlSystem);
                else if (type == "tsw770")
                    return new Tsw770(id, Global.ControlSystem);
                else if (type == "tsw1050")
                    return new Tsw1050(id, Global.ControlSystem);
                else if (type == "tsw1052")
                    return new Tsw1052(id, Global.ControlSystem);
                else if (type == "tsw1060")
                    return new Tsw1060(id, Global.ControlSystem);
                else if (type == "tsw1070")
                {
                    var dev_ = new Tsw1070(id, Global.ControlSystem); 
                    Debug.LogMessage(LogEventLevel.Information, "[{0}] Factory, {1} {2}", TypeNames[0], type, dev_ == null ? "== null" : "exists");
                    return dev_;
                }                  
                    //return new Tsw1070(id, Global.ControlSystem);
                else if (type == "ts770")
                    return new Ts770(id, Global.ControlSystem);
                else if (type == "ts1070")
                    return new Ts1070(id, Global.ControlSystem);
                else if (type == "xpanel")
                    return new XpanelForSmartGraphics(id, Global.ControlSystem);
                /*
                else if (type == "crestronapp") // this throws an assembly error because Crestron can't manage their own libraries
                {
                    var app = new CrestronApp(id, Global.ControlSystem);
                    app.ParameterProjectName.Value = projectName;
                    return app;
                }
                */
                else
                {
                    Debug.LogMessage(LogEventLevel.Verbose, "WARNING: Cannot create TSW controller with type '{0}'", type);
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogEventLevel.Verbose, "WARNING: Cannot create TSW base class. Panel will not function: {0}", e.Message);
                return null;
            }
        }
    }
}

