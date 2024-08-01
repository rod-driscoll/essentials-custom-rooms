using avit_essentials_common.IRPorts;
using Crestron.SimplSharp.Reflection;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common;
using System;
using System.Linq;

namespace essentials_advanced_room.Functions
{
    public interface IHasDefaultSetTopBox
    {
        ISetTopBoxControls DefaultSetTopBox { get; }
    }

    public class RoomSetTopBox: IHasDefaultSetTopBox, ILogClassDetails
    {
        public string ClassName { get { return "RoomSetTopBox"; } }
        public uint LogLevel { get; set; }
        public Config config { get; private set; }

        public ISetTopBoxControls DefaultSetTopBox { get; private set; }

        public RoomSetTopBox(Config config)
        {
            LogLevel = 0; // 0 == log everything, 2 == logs almost nothing
            Debug.Console(LogLevel, "{0} constructor starting", ClassName);
            this.config = config;
            IKeyed device_;
            try
            {
                if (!String.IsNullOrEmpty(config.DefaultSetTopBoxKey))
                {
                    Debug.Console(LogLevel, "{0} DefaultSetTopBoxKey: {1}", ClassName, config.DefaultSetTopBoxKey);
                    //Debug.Console(LogLevel, "{0} SetTopBoxPresetsURL: {1}", ClassName, config.SetTopBoxPresetsURL == null ? "== null" : config.SetTopBoxPresetsURL);
                    device_ = DeviceManager.GetDeviceForKey(config.DefaultSetTopBoxKey);
                    if (device_ != null)
                    {
                        Debug.Console(LogLevel, "{0} DefaultSetTopBox is {1}", ClassName, device_.GetType().Name);
                        
                        // this doesn't work if the interface is not from the same assembly
                        DefaultSetTopBox = device_ as ISetTopBoxControls; 
                        Debug.Console(LogLevel, "{0} ISetTopBoxControls {1}", ClassName, DefaultSetTopBox == null?"== null":"exists");
                        
                        var stb_ = device_ as IRSetTopBoxBase; // null for non crestron ir ports
                        Debug.Console(LogLevel, "{0} IRSetTopBoxBase, {1} a Crestron native IR port", ClassName, stb_ == null?"Not":"Is");
                        var stbPortController_ = device_ as IHasIrOutputPortController; // null, not the same assembly
                        Debug.Console(LogLevel, "{0} IHasIrOutputPortController {1}", ClassName, stbPortController_ == null ? "== null" : "exists");
                        if (stb_ != null) // Crestron IR port
                        {
                            if (stb_.IrPort != null)
                                stb_.IrPort.DriverLoaded.OutputChange += DriverLoaded_OutputChange;
                        }
                        else if (stbPortController_ != null) // non-Crestron IR port, won't run, not part of assembly
                        {
                            try
                            {
                                Debug.Console(LogLevel, "{0} stbPortController.IrPort {1}", ClassName, stbPortController_ == null ? "== null" : "exists");
                                if (stbPortController_.IrPort != null)
                                    stbPortController_.IrPort.DriverLoaded.OutputChange += DriverLoaded_OutputChange;
                            }
                            catch (Exception e)
                            {
                                Debug.Console(LogLevel, "{0} IHasIrOutputPortController ERROR: {1}", ClassName, e.Message);
                            }
                        }
                        else // a whole lot of experimentation to try and load IrPort from the assembly because the interface isn't in this assembly
                        {
                            //TryToLoadFromAssembly();
                        }
                        var stbPresets_ = device_ as ITvPresetsProvider;
                        if (stbPresets_.TvPresets != null && config.SetTopBoxPresetsURL != null)
                        {
                            stbPresets_.TvPresets.ImagesLocalHostPrefix = config.SetTopBoxPresetsURL;
                            Debug.Console(LogLevel, "{0} ISetTopBoxControls, setting path: '{1}'", ClassName, config.SetTopBoxPresetsURL);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} RoomSetTopBox constructor ERROR: {1}", ClassName, e.Message);
            }
        }
                            
        private void DriverLoaded_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} DriverLoaded, DefaultSetTopBox {1}", ClassName, DefaultSetTopBox==null?"== null": DefaultSetTopBox.GetType().Name);
            if (DefaultSetTopBox != null)
            {
                IRSetTopBoxBase stb_ = DefaultSetTopBox as IRSetTopBoxBase;
                if (stb_ != null)
                {
                    if (stb_.IrPort != null)
                    {
                        IrOutputPortController irPort_ = stb_.IrPort;
                        //Debug.Console(LogLevel, "{0} IrPort {1}", ClassName, irPort_ == null ? "== null" : "exists");
                        if (irPort_ != null)
                        {
                            Debug.Console(LogLevel, "{0} DefaultSetTopBox IrPort: '{1}', path: '{2}', loaded: {3}", ClassName, irPort_.Name, irPort_.DriverFilepath, irPort_.DriverIsLoaded);
                            //irPort_.PrintAvailableCommands();
                            //foreach (var command in irPort_.IrFileCommands)
                            //    Debug.Console(LogLevel, "{0} command: {1}", ClassName, command);
                        }
                    }
                    Debug.Console(LogLevel, "{0} TvPresets: '{1}'", ClassName, stb_.TvPresets == null ? "== null" : stb_.TvPresets.Name);
                    Debug.Console(LogLevel, "{0} stb_.TvPresets.ImagesLocalHostPrefix: {1}", ClassName, stb_.TvPresets.ImagesLocalHostPrefix == null ? "== null" : stb_.TvPresets.ImagesLocalHostPrefix);
                    /*if (stb_.TvPresets != null && config.SetTopBoxPresetsURL != null)
                    {
                        stb_.TvPresets.ImagesLocalHostPrefix = config.SetTopBoxPresetsURL;
                        Debug.Console(LogLevel, "{0} ISetTopBoxControls, setting path: '{1}'", ClassName, config.SetTopBoxPresetsURL);
                    }*/
                }
            }
        }

        /// <summary>
        /// this is obsolete but the code is left here for refernce if i want to invoke a method from a plugin.
        /// You can't access an instance of an object from an assembly where the object definition is not defined in a shared assembly.
        /// </summary>
        [Obsolete("TryToLoadFromAssembly is obsolete - code only exists for learning")]
        private void TryToLoadFromAssembly()
        {
            var assemblyName_ = "global-cache-ip2ir-epi";
            // true below
            var isloaded_ = PluginLoader.CheckIfAssemblyLoaded(assemblyName_);
            Debug.Console(LogLevel, "{0} CheckIfAssemblyLoaded({1}): {2}", ClassName, assemblyName_, isloaded_);

            // exists below
            var loadedAssembly_ = PluginLoader.LoadedAssemblies.FirstOrDefault(a => a.Name == assemblyName_);
            Debug.Console(LogLevel, "{0} LoadedAssembly {1}", ClassName, loadedAssembly_ == null ? "== null" : "exists");

            //exists below
            var assembly_ = loadedAssembly_.Assembly;
            Debug.Console(LogLevel, "{0} Assembly {1}", ClassName, assembly_ == null ? "== null" : "exists");
            //PluginLoader.SetEssentialsAssembly("global-cache-ip2ir-epi", assembly_);

            string typeName_ = String.Empty;
            string interfaceName_ = String.Empty;
            string propertyName_ = String.Empty;
            string methodName_ = String.Empty;

            try
            {
                CType[] ctypes_ = assembly_.GetTypes();

                typeName_ = "IRSetTopBoxBaseAdvanced";
                interfaceName_ = "IHasIrOutputPortController"; // i'd like to cast to this
                propertyName_ = "IrPort"; // i want this type is IROutputPortIP2IR: IIROutputPort
                methodName_ = "Up";

                //Debug.Console(LogLevel, "{0} typeName: {1}, ", ClassName, typeName_);
                var type_ = ctypes_.FirstOrDefault(x => x.Name == typeName_);
                Debug.Console(LogLevel, "{0} type: {1} ", ClassName, type_ == null ? "== null" : "exists");
                if (type_ != null)
                {
                    //Debug.Console(LogLevel, "{0} interfaceName_: {1}, ", ClassName, interfaceName_);
                    var interfaces_ = type_.GetInterfaces();
                    //Debug.Console(LogLevel, "{0} interfaces_ {1}, ", ClassName, interfaces_ == null ? "==null" : "exist");
                    if (interfaces_ != null)
                    {
                        var interface_ = interfaces_.FirstOrDefault(x => x.Name == interfaceName_);
                        Debug.Console(LogLevel, "{0} interface: {1} {2}", ClassName, interfaceName_, interface_ == null ? "== null" : "exists");
                        if (interface_ != null)
                        {
                            Type t = interface_.GetType();
                            object zInstance = Crestron.SimplSharp.Reflection.Activator.CreateInstance(t);
                        }
                    }

                    //Debug.Console(LogLevel, "{0} propertyName_: {1}, ", ClassName, propertyName_);
                    var property_ = type_.GetProperty(propertyName_);
                    Debug.Console(LogLevel, "{0} property: {1} {2} ", ClassName, propertyName_, property_ == null ? "== null" : "exists");
                    if (property_ != null)
                    {
                        //trying to do this...
                        // (device_ as IHasIrOutputPortController).IrPort.DriverLoaded.OutputChange += DriverLoaded_OutputChange;
                        // where property is IrPort

                        //property.invoke(DriverLoaded.OutputChange) += DriverLoaded_OutputChange;
                    }

                    var method_ = type_.GetMethod(methodName_);
                    Debug.Console(LogLevel, "{0} method: {1} {2} ", ClassName, methodName_, method_ == null ? "== null" : "exists");
                    if (method_ != null)
                    {
                        /* // this is how we can invoke the method
                        var action_ = new DeviceActionWrapper
                        {
                            DeviceKey = config.DefaultSetTopBoxKey,
                            MethodName = methodName_,
                            Params = new object[] { false }
                        };
                        DeviceJsonApi.DoDeviceAction(action_);
                    */
                        //ParameterInfo[] parameters = method_.GetParameters();
                        //object[] parameters2 = parameters.Select((ParameterInfo p, int i) => ConvertType(action.Params[i], p.ParameterType)).ToArray();
                        //method_.Invoke(device_, parameters2);
                        //method_.Invoke(device_, new object[] { false });
                    }
                }

                /* // other known members of stb-avdanced
                typeName_ = "IIrOutputPort";

                typeName_ = "IrOutputPortIP2IR";
                interfaceName_ = "IIrOutputPort";

                typeName_ = "IrOutputPortControllerIP2IR";
                interfaceName_ = "IIrOutputPortController";
                propertyName_ = "IrPort";

                typeName_ = "Device";
                interfaceName_ = "IIrOutputPortsAdvanced"; ;
                interfaceName_ = "IIrOutputPorts";
                */
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} ERROR: {1}", ClassName, e.Message);
            }
            /* // null below
            var interfaceType_ = assembly_.GetType(interfaceName_);
            Debug.Console(LogLevel, "{0} interfaceType_ {1}", ClassName, interfaceType_ == null ? "== null" : "exists");

            // null
            if(interfaceType_ != null)
            {
                var portController_ = (IHasIrOutputPortController)Crestron.SimplSharp.Reflection.Activator.CreateInstance(interfaceType_);
                portController_.IrPort.DriverLoaded.OutputChange += DriverLoaded_OutputChange;
                Debug.Console(LogLevel, "{0} subscribed to DriverLoaded_OutputChange", ClassName);
            }
            // errors
            var interfaces_ = interfaceType_.GetInterfaces();
            Debug.Console(LogLevel, "{0} Interfaces {1}", ClassName, interfaces_ == null ? "== null" : "exists");
            if(interfaces_ != null) {}
            // errors
            var interface_ = interfaceType_.GetInterfaces().FirstOrDefault(a => a.Name == interfaceName_);
            Debug.Console(LogLevel, "{0} Interface {1}", ClassName, interface_ == null ? "== null" : "exists");
            */

            //var factory = (IDeviceFactory)Crestron.SimplSharp.Reflection.Activator.CreateInstance(type);
            // namespace = "global_cache_ip2ir_epi"
            //var types = assy.GetTypes().Where(ct => typeof(IDeviceFactory).IsAssignableFrom(ct) && !ct.IsInterface && !ct.IsAbstract);

        }
    }
}
