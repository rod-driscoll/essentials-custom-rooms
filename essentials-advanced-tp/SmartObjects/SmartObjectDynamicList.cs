using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.SmartObjects;
using System;
using System.Linq;

// copied using PepperDash.Essentials.Core.SmartObjects.SmartObjectDynamicList
// and added visibility
namespace essentials_basic_tp.Drivers
{
    public class SmartObjectDynamicList : SmartObjectHelperBase
    {
        public const string SigNameScrollToItem = "Scroll To Item";
        public const string SigNameSetNumberOfItems = "Set Number of Items";

        public uint NameSigOffset { get; private set; }

        public ushort Count
        {
            get
            {
                return SmartObject.UShortInput[SigNameSetNumberOfItems].UShortValue;
            }
            set { SmartObject.UShortInput[SigNameSetNumberOfItems].UShortValue = value; }
        }

        /// <summary>
        /// The limit of the list object, as defined by VTPro settings.  Zero if object
        /// is not a list
        /// </summary>
        public int MaxCount { get; private set; }

        /// <summary>
        /// Wrapper for smart object
        /// </summary>
        /// <param name="so"></param>
        /// <param name="useUserObjectHandler">True if the standard user object action handler will be used</param>
        /// <param name="nameSigOffset">The starting join of the string sigs for the button labels</param>
		public SmartObjectDynamicList(SmartObject so, bool useUserObjectHandler, uint nameSigOffset) : base(so, useUserObjectHandler)
        {
            try
            {
                // Just try to touch the count signal to make sure this is indeed a dynamic list
                var c = Count;
                NameSigOffset = nameSigOffset;
                MaxCount = SmartObject.BooleanOutput.Count(s => s.Name.EndsWith("Pressed"));
                //Debug.Console(LogLevel, "Smart object {0} has {1} max", so.ID, MaxCount);
            }
            catch
            {
                var msg = string.Format("SmartObjectDynamicList: Smart Object {0:X2}-{1} is not a dynamic list. Ignoring", so.Device.ID, so.ID);
                Debug.Console(0, Debug.ErrorLogLevel.Error, msg);
            }
        }

        /// <summary>
        /// Builds a new list item
        /// </summary>
        public void SetItem(uint index, string mainText, string iconName, Action<bool> action)
        {
            SetItemMainText(index, mainText);
            SetItemIcon(index, iconName);
            SetItemButtonAction(index, action);
            //try
            //{
            //    SetMainButtonText(index, text);
            //    SetIcon(index, iconName);
            //    SetButtonAction(index, action);
            //}
            //catch(Exception e)
            //{
            //    Debug.Console(1, "Cannot set Dynamic List item {0} on smart object {1}", index, SmartObject.ID);
            //    ErrorLog.Warn(e.ToString());
            //}
        }

        public void SetItemMainText(uint index, string text)
        {
            if (index > MaxCount) return;
            var name = "Item " + index.ToString() + " Text";
            if (!SmartObject.StringInput.Contains(name))
                name = "Set Item " + index.ToString() + " Text";
            if (!SmartObject.StringInput.Contains(name))
                name = "Set " + name;
            if (!SmartObject.StringInput.Contains(name))
                name = "text-o" + index.ToString();
            if (SmartObject.StringInput.Contains(name))
                SmartObject.StringInput[name].StringValue = text;
            else // The list item template defines CIPS tags that refer to standard joins
                (SmartObject.Device as BasicTriList).StringInput[NameSigOffset + index].StringValue = text;
        }

        public void SetItemIcon(uint index, string iconName)
        {
            if (index > MaxCount) return;
            SmartObject.StringInput[string.Format("Set Item {0} Icon Serial", index)].StringValue = iconName;
        }

        public void SetItemButtonAction(uint index, Action<bool> action)
        {
            if (index > MaxCount) return;
            SmartObject.BooleanOutput[string.Format("Item {0} Pressed", index)].UserObject = action;
        }

        /// <summary>
        /// Sets the feedback on the given line, clearing others when interlocked is set
        /// </summary>
        public void SetFeedback(uint index, bool interlocked)
        {
            if (interlocked)
                ClearFeedbacks();
            SmartObject.BooleanInput[string.Format("Item {0} Selected", index)].BoolValue = true;
        }

        /// <summary>
        /// Clears all button feedbacks
        /// </summary>
        public void ClearFeedbacks()
        {
            for (int i = 1; i <= Count; i++)
                SmartObject.BooleanInput[string.Format("Item {0} Selected", i)].BoolValue = false;
        }

        /// <summary>
        /// Removes Action object from all buttons
        /// </summary>
        public void ClearActions()
        {
            Debug.Console(2, "SO CLEAR");
            for (ushort i = 1; i <= MaxCount; i++)
                SmartObject.BooleanOutput[string.Format("Item {0} Pressed", i)].UserObject = null;
        }

        #region aditions for essentials_basic_tp.Drivers
        public void SetItemVisible(uint index, bool state)
        {
            var sig = SmartObject.BooleanInput[string.Format("Item {0} Visible", index)];
            if (sig != null)
                sig.BoolValue = state;
        }
        public void SetItemEnable(uint index, bool state)
        {
            var sig = SmartObject.BooleanInput[string.Format("Item {0} Enable", index)];
            if (sig == null)
                sig = SmartObject.BooleanInput[string.Format("Item {0} Enabled", index)];
            if (sig != null)
                sig.BoolValue = state;
        }

        public bool GetItemVisible(uint index)
        {
            var sig = SmartObject.BooleanInput[string.Format("Item {0} Visible", index)];
            if (sig == null) return false;
            return sig.BoolValue;
        }
        public bool GetItemEnable(uint index)
        {
            var sig = SmartObject.BooleanInput[string.Format("Item {0} Enable", index)];
            if (sig == null)
                sig = SmartObject.BooleanInput[string.Format("Item {0} Enabled", index)];
            if (sig == null) return false;
            return sig.BoolValue;
        }

        internal void SetItemButtonAction(uint i, bool v)
        {
            throw new NotImplementedException();
        }

        public BoolInputSig GetBoolFeedbackSig(uint index)
        {
            var name = "fb" + index.ToString();
            if (!SmartObject.BooleanInput.Contains(name))
                name = "Item " + index.ToString() + " Select";
            if (!SmartObject.BooleanInput.Contains(name))
                name = "Item " + index.ToString() + " Selected";
            if (!SmartObject.BooleanInput.Contains(name))
                name = "Tab Button " + index.ToString() + " Select";
            if (SmartObject.BooleanInput.Contains(name))
                return SmartObject.BooleanInput[name];
            else
                return null;
        }

        #endregion
    }
}