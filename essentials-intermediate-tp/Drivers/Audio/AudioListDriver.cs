using avit_essentials_common.interfaces;
using essentials_basic_room;
using essentials_basic_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using System;
using System.Collections.Generic;
using SubpageReferenceList = essentials_basic_tp.Drivers.SubpageReferenceList;

namespace essentials_advanced_tp.Drivers
{
    public class AudioListDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "AudioListDriver"; } }
        private uint _logLevel;
        public uint LogLevel
        {
            get { return _logLevel; }
            set
            {
                try
                {
                    _logLevel = value;
                    foreach (var driver in ChildDrivers)
                    {
                        var driver_ = driver as ILogClassDetails;
                        //Debug.Console(2, "{0} Setting LogLevel {1}, {2} {3}", ClassName, _logLevel, driver.GetType().Name, driver_ == null ? " = null" : "exists");
                        if (driver_ != null)
                            driver_.LogLevel = _logLevel;
                    }
                    //Debug.Console(2, "{0} Setting LogLevel {1} done", ClassName, _logLevel);
                }
                catch (Exception e)
                {
                    Debug.Console(0, "{0} Setting LogLevel ERROR: {1}", ClassName, e.Message);
                }
            }
        }
        private BasicPanelMainInterfaceDriver Parent;

        SubpageReferenceList srl { get; set; }

        uint SmartObjectId = joins.UISmartObjectJoin.VolumeList;
        uint dig_offset = 4;
        uint ana_offset = 1;
        uint ser_offset = 1;

        public List<BasicAudioLevelDriver> ChildDrivers = new List<BasicAudioLevelDriver>();

        public AudioListDriver(BasicPanelMainInterfaceDriver parent)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Debug.Console(0, "{0} loading", ClassName);
            this.Parent = parent;

            srl = new SubpageReferenceList(parent.TriList, SmartObjectId, dig_offset, ana_offset, ser_offset);

            Debug.Console(0, "{0} srl.Count: {1}", ClassName, srl.Count);
            
            for (uint i = 1;i <= srl.MaxDefinedItems; i++) {
                ChildDrivers.Add(new BasicAudioLevelDriver(Parent,
                    new BasicAudioDriverControls(i.ToString(),
                        new BasicAudioDriverSigs
                        {
                            UpPress   = srl.GetBoolFeedbackSig(i, (uint)1),
                            DownPress = srl.GetBoolFeedbackSig(i, (uint)2),
                            MutePress = srl.GetBoolFeedbackSig(i, (uint)3),
                            MuteFb    = srl.BoolInputSig(i, (uint)3),
                            Slider1   = srl.UShortInputSig(i, (ushort)1),
                            Slider1Fb = srl.GetUShortOutputSig(i, (ushort)1),
                            Label     = srl.StringInputSig(i, (ushort)1),
                        })
                ));
            }

            //Register();
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        public void Setup(IBasicRoom room)
        {
            //Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            // need to get list of room levels
            bool[] isVisible = new bool[srl.MaxDefinedItems];
            var CurrentRoom_ = room as IHasAudioDevice; // implements this class
            if (CurrentRoom_ != null)
            {
                foreach (var driver in ChildDrivers)
                {
                    driver.Setup(room);
                    var key_ = driver.controls.Key; // number, not name
                    //Debug.Console(LogLevel, "{0} Setup key: {1} {2}", ClassName, key_, driver.CurrentDevice==null?"== null":"exists");
                    if (driver.CurrentVolumeDevice != null) 
                    {
                        var i = Convert.ToInt16(key_);
                        if (i > 0 && i <= srl.MaxDefinedItems)
                            isVisible[i - 1] = true;
                    }
                }
            }
            for (uint i = 0; i < isVisible.Length; i++)
            {
                //Debug.Console(LogLevel, "{0} SetInputVisible: {1}:{2}", ClassName, i, isVisible[i]);
                srl.SetInputVisible(i+1, isVisible[i]);
            }
         
            Debug.Console(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }
    }
}
