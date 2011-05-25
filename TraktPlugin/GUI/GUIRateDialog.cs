using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using Action = MediaPortal.GUI.Library.Action;
using System.ComponentModel;
using TraktPlugin.TraktAPI;

namespace TraktPlugin.GUI
{
    public class GUIRateDialog : GUIDialogWindow
    {
        public const int ID = 87300;

        public GUIRateDialog()
        {
            GetID = ID;
        }

        [SkinControlAttribute(10)]
        protected GUILabelControl lblRating = null;
        [SkinControlAttribute(100)]
        protected GUIButtonControl btnLove = null;
        [SkinControlAttribute(101)]
        protected GUIButtonControl btnHate = null;

        #region Public Properties

        public TraktAPI.TraktRateValue Rated { get; set; }
        public bool IsSubmitted { get; set; }

        #endregion

        #region Overides

        /// <summary>
        /// MediaPortal will set #currentmodule with GetModuleName()
        /// </summary>
        /// <returns>Localized Window Name</returns>
        public override string GetModuleName()
        {
            return Translation.RateDialog;
        }

        public override void Reset()
        {
            base.Reset();

            SetHeading("");
            SetLine(1, "");
            SetLine(2, "");
            SetLine(3, "");
            SetLine(4, "");
        }

        public override void DoModal(int ParentID)
        {
            LoadSkin();
            AllocResources();
            InitControls();

            base.DoModal(ParentID);
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.RateDialog.xml");
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.REMOTE_1:
                    Rated = TraktAPI.TraktRateValue.love;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_2:
                    Rated = TraktAPI.TraktRateValue.hate;
                    UpdateRating();
                    break; 
               
                case Action.ActionType.ACTION_SELECT_ITEM:
                    IsSubmitted = true;
                    PageDestroy();
                    return;

                case Action.ActionType.ACTION_PREVIOUS_MENU:
                case Action.ActionType.ACTION_CLOSE_DIALOG:
                case Action.ActionType.ACTION_CONTEXT_MENU:
                    IsSubmitted = false;
                    PageDestroy();
                    return;
            }

            base.OnAction(action);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);
            if (control == btnLove)
            {
                Rated = TraktAPI.TraktRateValue.love;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnHate)
            {
                Rated = TraktAPI.TraktRateValue.hate;
                IsSubmitted = true;
                PageDestroy();
                return;
            }            
        }

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                    base.OnMessage(message);
                    IsSubmitted = false;
                    UpdateRating();
                    return true;

                case GUIMessage.MessageType.GUI_MSG_SETFOCUS:
                    if (message.TargetControlId < 100 || message.TargetControlId > 101)
                        break;

                    Rated = (TraktAPI.TraktRateValue)(message.TargetControlId - 100);
                    UpdateRating();
                    break;
            }
            return base.OnMessage(message);
        }

        #endregion

        #region Private Methods

        private void UpdateRating()
        {
            // Update button states
            btnLove.Selected = (Rated == TraktAPI.TraktRateValue.love);
            btnHate.Selected = (Rated == TraktAPI.TraktRateValue.hate);

            btnLove.Focus = (Rated == TraktAPI.TraktRateValue.love);
            btnHate.Focus = (Rated == TraktAPI.TraktRateValue.hate);

            // Update Rating Description
            if (lblRating != null)
                lblRating.Label = GetRatingDescription();
        }

        private string GetRatingDescription()
        {
            string description = string.Empty;

            switch (Rated)
            {
                case TraktAPI.TraktRateValue.love:
                    description = Translation.RateLove;
                    break;
                case TraktAPI.TraktRateValue.hate:
                    description = Translation.RateHate;
                    break;
            }

            return description;
        }

        #endregion

        #region Public Methods

        public void SetHeading(string HeadingLine)
        {
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_LABEL_SET, GetID, 0, 1, 0, 0, null);
            msg.Label = HeadingLine;
            OnMessage(msg);
        }

        public void SetLine(int LineNr, string Line)
        {
            if (LineNr < 1) return;
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_LABEL_SET, GetID, 0, 1 + LineNr, 0, 0, null);
            msg.Label = Line;
            if ((msg.Label == string.Empty) || (msg.Label == "")) msg.Label = "  ";
            OnMessage(msg);
        }

        #endregion

    }
}
