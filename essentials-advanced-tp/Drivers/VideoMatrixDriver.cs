using Crestron.SimplSharpPro.Diagnostics;
using essentials_advanced_room;
using Independentsoft.Exchange;
using Org.BouncyCastle.Asn1.Ocsp;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;
using static Crestron.SimplSharpPro.DM.Audio;

namespace essentials_advanced_tp.Drivers
{
    public class VideoMatrixDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "VideoMatrixDriver"; } }
        public LogEventLevel LogLevel { get; set; }
        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }
        public uint MaxInputs { get; private set; }
        public uint MaxOutputs { get; private set; }

        IAdvancedRoom CurrentRoom;

        IMatrixRouting CurrentRouter;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public VideoMatrixDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = LogEventLevel.Information;
            Parent = parent;
            // may need to change this from GenericModalVisible if we use the GenericModal dialog for anything else such as room combine
            PressJoin = joins.UIBoolJoin.VideoMatrixVisible;
            PageJoin = joins.UIBoolJoin.VideoMatrixVisible;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;

            MaxInputs  = MaxInputs  == 0 ? 2 : MaxInputs;
            MaxOutputs = MaxOutputs == 0 ? 1 : MaxOutputs;

            Register(); // the driver is always available so register here rather than on popupinterlock

            Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Debug.LogMessage(LogLevel, "{0} PopupInterlock_StatusChanged, e.NewJoin: {1}, e.PreviousJoin: {2}", ClassName, e.NewJoin, e.PreviousJoin);
            if (e.PreviousJoin == PageJoin)
                Unregister();
            if (e.NewJoin == PageJoin)
                Register();
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.LogMessage(LogLevel, "{0} Setup {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;

            var routers = DeviceManager.AllDevices.OfType<IMatrixRouting>().ToList();
            if (routers != null)
            {
                Debug.LogMessage(LogLevel, "{0} IMatrixRouting {1} found", ClassName, routers.Count);
                CurrentRouter = routers[0];

                foreach (var router in routers)
                {
                    Debug.LogMessage(LogLevel, "{0} router InputSlots: {1}, OutputSlots: {2}", ClassName, router.InputSlots.Count, router.OutputSlots.Count);
                    foreach (var input_ in router.InputSlots)
                    {
                        Debug.LogMessage(LogLevel, "{0} router InputSlot: {1}, val.Key: {2}, val.Name: {3}, val.SlotNumber: {4}, val.TxDeviceKey: {5}",
                            ClassName, input_.Key, input_.Value.Key, input_.Value.Name, input_.Value.SlotNumber, input_.Value.TxDeviceKey);

                        foreach (var output_ in router.OutputSlots)
                        {
                            uint join_ = joins.UIBoolJoin.VideoMatrixBase + (uint)input_.Value.SlotNumber + (uint)(MaxInputs * (output_.Value.SlotNumber - 1));
                            Debug.LogMessage(LogLevel, "{0} router adding EventHandler for DIG-{1}, input[{2}]: {3}, output[{4}]: {5})", ClassName, join_, input_.Value.SlotNumber, input_.Key, output_.Value.SlotNumber, output_.Key);
                            TriList.SetSigFalseAction(join_, () =>
                            {
                                Debug.LogMessage(LogLevel, "{0} Route({1}, {2}, {3})", ClassName, input_.Key, output_.Key, eRoutingSignalType.AudioVideo.ToString());
                                CurrentRouter.Route(input_.Key, output_.Key, eRoutingSignalType.AudioVideo);
                            });
                            // TODO: check this for when there are multiple outputs - only tested for a single output so far
                            output_.Value.OutputSlotChanged += (sender, args) =>
                            {
                                Debug.LogMessage(LogLevel, "{0} OutputSlotChanged for DIG-{1}, input[{2}]: {3}, output[{4}]: {5}, type: {6})", ClassName, join_, input_.Value.SlotNumber, input_.Key, output_.Value.SlotNumber, output_.Key, eRoutingSignalType.AudioVideo.ToString());
                                var out_ = sender as IRoutingOutputSlot;
                                if (out_ != null)
                                    TriList.SetBool(join_, input_.Value.SlotNumber == out_.CurrentRoutes[eRoutingSignalType.Video].SlotNumber);
                            };
                        }
                    }
                    foreach (var output_ in router.OutputSlots)
                    {
                        Debug.LogMessage(LogLevel, "{0} router OutputSlots: {1}, val.Key: {2}, val.Name: {3}, val.SlotNumber: {4}, val.RxDeviceKey: {5}",
                            ClassName, output_.Key, output_.Value.Key, output_.Value.Name, output_.Value.SlotNumber, output_.Value.RxDeviceKey);
                        if (output_.Value.CurrentRoutes != null)
                        {
                            foreach (var route in output_.Value.CurrentRoutes)
                            {
                                Debug.LogMessage(LogLevel, "{0} route: {1}", ClassName, route.Key);
                                if(route.Value != null)
                                {
                                    Debug.LogMessage(LogLevel, "{0} route.Value.Key: {1}", ClassName, route.Value.Key);
                                    Debug.LogMessage(LogLevel, "{0} route: {1}, Value: {2}, input_: {3}", ClassName, route.Key, route.Value.Key, route.Value.SlotNumber);
                                }
                            }
                        }
                    }

                }
            }


            Debug.LogMessage(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void DoRoute(uint input, uint output)
        {
            Debug.LogMessage(LogLevel, "{0} DoRoute: {1}->{2}", ClassName, input, output);
            /*
            NvxApplicationVideoReceiver rx_;
            var rx = item.DeviceActual;
            var stream = rx.Device as IStreamWithHardware;

            if (input == 0)
            {
                stream?.ClearStream();
                rx.Display.ReleaseRoute();
            }
            else
            {
                if (!_transmitters.TryGetValue(s, out NvxApplicationVideoTransmitter device))
                    return;
                rx.Display.ReleaseAndMakeRoute((IRoutingOutputs)device.Source, _enableAudioBreakaway ? eRoutingSignalType.Video : eRoutingSignalType.AudioVideo);
            }
            */
        }
        public void Register()
        {
            Debug.LogMessage(LogLevel, "{0} Register", ClassName);
            //var sourceDev = DeviceManager.GetDeviceForKey(SourceKey) as IRoutingOutputs;


            //var matrix = DeviceManager.GetDeviceForKey("NvxRouter-PrimaryStream") as IRoutingOutputs;
                /* 
            var encoders = DeviceManager.AllDevices.OfType<IRoutingOutputs>().ToList();
            foreach (var enc in encoders)
            {
                Debug.LogMessage(LogLevel, "{0} IRoutingOutputs found: {1}, type: {2}", ClassName, enc.Key, enc.GetType().ToString());
                foreach (var port in enc.OutputPorts)
                {
                    Debug.LogMessage(LogLevel, "{0} OutputPort: {1}, {2}, {3}", ClassName, port.Key, port.ConnectionType, port.GetType().ToString());
                }
                IRoutingOutputs found: NvxRouter-PrimaryStream,     type: Nes.Routing.PrimaryStreamRouter
                    OutputPort: Rx - 4 - StreamOutput, Streaming,           PepperDash.Ese.RoutingOutputPort      
                IRoutingOutputs found: NvxRouter - SecondaryAudio,  type: res.Routing.SecondaryAudioRouter
                    OutputPort: Tx - 1 - SecondaryAudioOutput, Streaming,   Peppeials.Core.RoutingOutputPort
                    OutputPort: Tx - 2 - SecondaryAudioOutput, Streaming,   Peppeials.Core.RoutingOutputPort
                    OutputPort: Rx - 4 - SecondaryAudioOutput, Streaming,   Peppeials.Core.RoutingOutputPort

                IRoutingOutputs found: NvxRouter,                   type: NvxEpi.FeaturesGlobalRouter

                IRoutingOutputs found: ClickShare - 1,              type: PepperDash.Evices.Common.GenericSource
                    OutputPort: anyOut, Hdmi,                           PepperDash.Essentials.Core.Roort
                IRoutingOutputs found: ClickShare - 2,              type: PepperDash.Evices.Common.GenericSource
                    OutputPort: anyOut, Hdmi,                           PepperDash.Essentials.Core.Roort

                IRoutingOutputs found: Tx - 1,                      type: NvxEpi.Devices.Nvx36
                    OutputPort: HdmiOutput, Hdmi,                       PepperDash.Essentials.CorputPort
                    OutputPort: SecondaryAudioOutput, LineAudio,        PepperDashCore.RoutingOutputPort
                    OutputPort: AnalogAudioOutput, LineAudio,           PepperDash.Ese.RoutingOutputPort
                    OutputPort: StreamOutput, Streaming,                PepperDash.EssentitingOutputPort
                IRoutingOutputs found: Tx - 2,                      type: NvxEpi.Devices.Nvx36
                    OutputPort: HdmiOutput, Hdmi,                       PepperDash.Essentials.CorputPort
                    OutputPort: SecondaryAudioOutput, LineAudio,        PepperDashCore.RoutingOutputPort
                    OutputPort: AnalogAudioOutput, LineAudio,           PepperDash.Ese.RoutingOutputPort
                    OutputPort: StreamOutput, Streaming,                PepperDash.EssentitingOutputPort

                IRoutingOutputs found: Rx - 4,                      type: NvxEpi.Devices.Nvx36
                    OutputPort: HdmiOutput, Hdmi, PepperDash.Essentials.CorputPort
                    OutputPort: SecondaryAudioOutput, LineAudio,        PepperDashCore.RoutingOutputPort
                    OutputPort: AnalogAudioOutput, LineAudio,           PepperDash.Ese.RoutingOutputPort

            }

            var decoders = DeviceManager.AllDevices.OfType<IRoutingInputs>().ToList();
            foreach (var dec in decoders)
            {
                Debug.LogMessage(LogLevel, "{0} IRoutingInputs found: {1}, type: {2}", ClassName, dec.Key, dec.GetType().ToString());
                foreach (var port in dec.InputPorts)
                {
                    Debug.LogMessage(LogLevel, "{0} InputPort: {1}, {2}, {3}", ClassName, port.Key, port.ConnectionType, port.GetType().ToString());
                }
            }

                List<uint> inputPresses = new List<uint>();
            for(uint i=1; i < MaxInputs; i++)
            {
                inputPresses.Add(joins.UIBoolJoin.VideoMatrixVisible + i);
                //TriList.BooleanOutput[joins.UIBoolJoin.VideoMatrixVisible + i].SetBoolSigAction(DoRoute);
                //TriList.BooleanOutput[joins.UIBoolJoin.VideoMatrixVisible + i].SetSigFalseAction()
                //TriList.SetSigFalseAction(joins.UIBoolJoin.VideoMatrixVisible + i, () => DoRoute((uint)i));
            }
                */
            /*
            var rx = item.DeviceActual;
            var stream = rx.Device as IStreamWithHardware;
            trilist.SetUShortSigAction((uint)(joinMap.OutputVideo.JoinNumber + item.DeviceId - 1),
                s =>
                {
                    if (s == 0)
                    {
                        stream?.ClearStream();

                        rx.Display.ReleaseRoute();
                    }
                    else
                    {
                        if (!_transmitters.TryGetValue(s, out NvxApplicationVideoTransmitter device))
                            return;

                        rx.Display.ReleaseAndMakeRoute((IRoutingOutputs)device.Source, _enableAudioBreakaway ? eRoutingSignalType.Video : eRoutingSignalType.AudioVideo);
                    }
                });

            if (item.DeviceActual.Device is IVideowallMode hdmiOut)
            {
                trilist.SetUShortSigAction((uint)(joinMap.OutputAspectRatioMode.JoinNumber + item.DeviceId - 1), hdmiOut.SetVideoAspectRatioMode);
            }
            */
        }

        public void Unregister()
        {
            Debug.LogMessage(LogLevel, "{0} Unregister", ClassName);
        }
    }
}
