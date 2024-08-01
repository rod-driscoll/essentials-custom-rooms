using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using essentials_advanced_room;
using essentials_advanced_room.Interfaces;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.SmartObjects;
using System;
using System.Text;

namespace essentials_advanced_tp.Drivers
{
    public class PinDriver: PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "PinDriver"; } }
        public uint LogLevel { get; set; }

        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }

        CTimer InactivityTimer;
        private readonly long _timeoutMs;

        StringBuilder PinEntryBuilder = new StringBuilder(4);
        SmartObjectNumeric PinKeypad;

        private bool _isAuthorised;
        public bool IsAuthorized
        {
            get { return _isAuthorised; }
            private set
            {
                _isAuthorised = value;
                TriList.SetBool(PageJoin, !value);
                TriList.SetBool(PressJoin, !value);
                (value ? (Action)Unregister : Register)();
                TriList.SetString(joins.UIStringJoin.PinDialogIcon, String.Format("Padlock {0}", IsAuthorized ? "Opened" : "Closed"));
            }
        }

        private string roomPin;
        private string uiPin;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public PinDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
                    : base(parent.TriList)
        {
            LogLevel = 2;
            Parent = parent;

            var config_ = config as IHasPassword;
            if (config_ != null)
                uiPin = config_.Password;
            else
                Debug.Console(LogLevel, "{0}. touchpanel password == null", ClassName);

            PressJoin = joins.UIBoolJoin.PinDialogShowPress;
            PageJoin = UIBoolJoin.PinDialog4DigitVisible;

            TriList.SetSigFalseAction(PressJoin, () => IsAuthorized = false);
            TriList.SetBool(joins.UIBoolJoin.PinDialogShowVisible, true);

            _timeoutMs = 1000 * (config.ScreenSaverTimeoutMin == 0 ? 20 : config.ScreenSaverTimeoutMin * 60);
            Debug.Console(LogLevel, "{0}. timeOut: {1}ms", ClassName, _timeoutMs);
            
            //Debug.Console(LogLevel, "{0}. testing for tsx52or60: type: {1}", ClassName, trilist.GetType().Name);
            Tswx52ButtonVoiceControl tswx52ButtonVoiceControl = TriList as Tswx52ButtonVoiceControl;
            if (tswx52ButtonVoiceControl != null)
            {
                //Debug.Console(LogLevel, "DefaultPanelMainInterfaceDriver is Tswx52ButtonVoiceControl. ExtenderTouchDetectionReservedSigs {1}= null", ClassName, tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs == null ? "=" : "!");
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Use();
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.DeviceExtenderSigChange += ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange;
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Time.UShortValue = 1;
                //Debug.Console(LogLevel, "{0} ManageInactivityTimer", ClassName);
                ManageInactivityTimer();
            }
            else
            {
                //Debug.Console(LogLevel, "{0} as TswX70Base", ClassName);
                TswX70Base tswX70Base = TriList as TswX70Base;
                if (tswX70Base != null)
                {
                    tswX70Base.ExtenderTouchDetectionReservedSigs.Use();
                    tswX70Base.ExtenderTouchDetectionReservedSigs.DeviceExtenderSigChange += ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange;
                    tswX70Base.ExtenderTouchDetectionReservedSigs.Time.UShortValue = 1;
                    //Debug.Console(LogLevel, "{0} ManageInactivityTimer", ClassName);
                    ManageInactivityTimer();
                    //Debug.Console(LogLevel, "{0} ManageInactivityTimer done", ClassName);
                }
            }
            PinKeypad = new SmartObjectNumeric(TriList.SmartObjects[UISmartObjectJoin.TechPinDialogKeypad], true);

            Register();

            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            var config_ = room.PropertiesConfig as IHasPassword;
            if (config_ != null)
                roomPin = config_.Password;
            else
                Debug.Console(LogLevel, "{0}. touchpanel password == null", ClassName);

            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
            SetupPinModal();
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(UIBoolJoin.PinDialogCancelPress);
            PinKeypad.Digit0.ClearSigAction();
            PinKeypad.Digit1.ClearSigAction();
            PinKeypad.Digit2.ClearSigAction();
            PinKeypad.Digit3.ClearSigAction();
            PinKeypad.Digit4.ClearSigAction();
            PinKeypad.Digit5.ClearSigAction();
            PinKeypad.Digit6.ClearSigAction();
            PinKeypad.Digit7.ClearSigAction();
            PinKeypad.Digit8.ClearSigAction();
            PinKeypad.Digit9.ClearSigAction();
        }

        void ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange(Crestron.SimplSharpPro.DeviceExtender currentDeviceExtender, Crestron.SimplSharpPro.SigEventArgs args)
        {

            if (args.Sig.BoolValue)
            {
                Debug.Console(0, "{0} ManageInactivityTimer DeviceExtenderSigChange", ClassName);
                ManageInactivityTimer();
            }
        }

        private void ManageInactivityTimer()
        {
            if (InactivityTimer != null)
            {
                Debug.Console(0, "{0} ManageInactivityTimer resetting", ClassName);
                InactivityTimer.Reset(_timeoutMs);
            }
            else
            {
                Debug.Console(0, "{0} ManageInactivityTimer creating new timer: {1}ms", ClassName, _timeoutMs);
                InactivityTimer = new CTimer((o) => InactivityTimerExpired(), _timeoutMs);
            }
            //Debug.Console(0, "{0} ManageInactivityTimer end", ClassName);
        }

        void InactivityTimerExpired()
        {
            Debug.Console(0, "{0} InactivityTimerExpired", ClassName);
            InactivityTimer.Stop();
            InactivityTimer.Dispose();
            InactivityTimer = null;

            IsAuthorized = false;
            Debug.Console(0, "{0} InactivityTimerExpired done", ClassName);
        }

        /// <summary>
        /// Wire up the keypad and buttons
        /// </summary>
        void SetupPinModal()
        {
            Debug.Console(0, "{0} SetupPinModal", ClassName);
            TriList.SetSigFalseAction(UIBoolJoin.PinDialogCancelPress, CancelPinDialog);
            try
            {
                PinKeypad.Digit0.UserObject = new Action<bool>(b => { if (b) DialPinDigit('0'); });
                PinKeypad.Digit1.UserObject = new Action<bool>(b => { if (b) DialPinDigit('1'); });
                PinKeypad.Digit2.UserObject = new Action<bool>(b => { if (b) DialPinDigit('2'); });
                PinKeypad.Digit3.UserObject = new Action<bool>(b => { if (b) DialPinDigit('3'); });
                PinKeypad.Digit4.UserObject = new Action<bool>(b => { if (b) DialPinDigit('4'); });
                PinKeypad.Digit5.UserObject = new Action<bool>(b => { if (b) DialPinDigit('5'); });
                PinKeypad.Digit6.UserObject = new Action<bool>(b => { if (b) DialPinDigit('6'); });
                PinKeypad.Digit7.UserObject = new Action<bool>(b => { if (b) DialPinDigit('7'); });
                PinKeypad.Digit8.UserObject = new Action<bool>(b => { if (b) DialPinDigit('8'); });
                PinKeypad.Digit9.UserObject = new Action<bool>(b => { if (b) DialPinDigit('9'); });
            }
            catch (Exception e)
            {
                Debug.Console(0, "{0} SetupPinModal ERROR: {1}", ClassName, e.Message);
                foreach (var out_ in PinKeypad.SmartObject.BooleanOutput)
                {
                    Debug.Console(0, "{0} SetupPinModal PinKeypad.BooleanOutput: {1}", ClassName, out_.Name);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        void DialPinDigit(char d)
        {
            PinEntryBuilder.Append(d);
            var len = PinEntryBuilder.Length;
            SetPinDotsFeedback(len);

            // check it!
            if (len == 4)
            {
                if ( (  uiPin != null &&   uiPin.Equals(PinEntryBuilder.ToString())) 
                  || (roomPin != null && roomPin.Equals(PinEntryBuilder.ToString())))
                {
                    IsAuthorized = true;
                    SetPinDotsFeedback(0);
                    Show();
                }
                else
                {
                    SetPinDotsFeedback(0);
                    TriList.SetBool(UIBoolJoin.PinDialogErrorVisible, true);
                    new CTimer(o =>
                    {
                        TriList.SetBool(UIBoolJoin.PinDialogErrorVisible, false);
                    }, 1500);
                }

                PinEntryBuilder.Remove(0, len); // clear it either way
            }
        }

        /// <summary>
        /// Draws the dots as pin is entered
        /// </summary>
        /// <param name="len"></param>
        void SetPinDotsFeedback(int len)
        {
            TriList.SetBool(UIBoolJoin.PinDialogDot1, len >= 1);
            TriList.SetBool(UIBoolJoin.PinDialogDot2, len >= 2);
            TriList.SetBool(UIBoolJoin.PinDialogDot3, len >= 3);
            TriList.SetBool(UIBoolJoin.PinDialogDot4, len == 4);
        }

        void CancelPinDialog()
        {
            PinEntryBuilder.Remove(0, PinEntryBuilder.Length);
        }
    }
}
