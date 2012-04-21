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
        [SkinControlAttribute(11)]
        protected GUILabelControl lblRatingAdvanced = null;
        [SkinControlAttribute(99)]
        protected GUIButtonControl btnUnRate = null;
        [SkinControlAttribute(100)]
        protected GUIButtonControl btnLove = null;
        [SkinControlAttribute(101)]
        protected GUIButtonControl btnHate = null;
        [SkinControlAttribute(1001)]
        protected GUIToggleButtonControl btnOneHeart = null;
        [SkinControlAttribute(1002)]
        protected GUIToggleButtonControl btnTwoHeart = null;
        [SkinControlAttribute(1003)]
        protected GUIToggleButtonControl btnThreeHeart = null;
        [SkinControlAttribute(1004)]
        protected GUIToggleButtonControl btnFourHeart = null;
        [SkinControlAttribute(1005)]
        protected GUIToggleButtonControl btnFiveHeart = null;
        [SkinControlAttribute(1006)]
        protected GUIToggleButtonControl btnSixHeart = null;
        [SkinControlAttribute(1007)]
        protected GUIToggleButtonControl btnSevenHeart = null;
        [SkinControlAttribute(1008)]
        protected GUIToggleButtonControl btnEightHeart = null;
        [SkinControlAttribute(1009)]
        protected GUIToggleButtonControl btnNineHeart = null;
        [SkinControlAttribute(1010)]
        protected GUIToggleButtonControl btnTenHeart = null;

        #region Public Properties

        public TraktRateValue Rated { get; set; }
        public bool IsSubmitted { get; set; }
        public bool ShowAdvancedRatings { get; set; }

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

            // Check Skin Compatibility
            CheckSkinCompatibility();

            // let skin show the correct rate buttons/label            
            SetControlVisibility();

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
                    if (ShowAdvancedRatings)
                        Rated = TraktRateValue.one;
                    else
                        Rated = TraktRateValue.ten;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_2:
                    if (ShowAdvancedRatings)
                        Rated = TraktRateValue.two;
                    else
                        Rated = TraktRateValue.one;
                    UpdateRating();
                    break; 
               
                case Action.ActionType.REMOTE_3:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.three;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_4:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.four;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_5:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.five;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_6:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.six;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_7:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.seven;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_8:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.eight;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_9:
                    if (ShowAdvancedRatings)
                    {
                        Rated = TraktRateValue.nine;
                        UpdateRating();
                    }
                    break;

                case Action.ActionType.REMOTE_0:
                    if (ShowAdvancedRatings)
                        Rated = TraktRateValue.ten;
                    else
                        Rated = TraktRateValue.unrate;
                    UpdateRating();
                    break;

                case Action.ActionType.ACTION_KEY_PRESSED:
                    // some types of remotes send ACTION_KEY_PRESSED instead of REMOTE_0 - REMOTE_9 commands
                    if (action.m_key != null)
                    {
                        int key = action.m_key.KeyChar;
                        if (ShowAdvancedRatings && (key >= '0' && key <= '9'))
                        {
                            if (key == 0) key = 10;
                            Rated = (TraktRateValue)key;
                            UpdateRating();
                        }
                        else if (!ShowAdvancedRatings && (key >= '0' && key <= '2'))
                        {
                            if (key == 0) Rated = TraktRateValue.unrate;
                            if (key == 1) Rated = TraktRateValue.ten;
                            if (key == 2) Rated = TraktRateValue.one;
                            UpdateRating();
                        }
                    }
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

            if (control == btnUnRate)
            {
                Rated = TraktRateValue.unrate;
                IsSubmitted = true;
                PageDestroy();
                return;
            }

            if (control == btnLove)
            {
                Rated = TraktRateValue.ten;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnHate)
            {
                Rated = TraktRateValue.one;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            if (control == btnOneHeart)
            {
                Rated = TraktRateValue.one;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnTwoHeart)
            {
                Rated = TraktRateValue.two;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnThreeHeart)
            {
                Rated = TraktRateValue.three;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnFourHeart)
            {
                Rated = TraktRateValue.four;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnFiveHeart)
            {
                Rated = TraktRateValue.five;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnSixHeart)
            {
                Rated = TraktRateValue.six;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnSevenHeart)
            {
                Rated = TraktRateValue.seven;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnEightHeart)
            {
                Rated = TraktRateValue.eight;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnNineHeart)
            {
                Rated = TraktRateValue.nine;
                IsSubmitted = true;
                PageDestroy();
                return;
            }
            else if (control == btnTenHeart)
            {
                Rated = TraktRateValue.ten;
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
                    // store old rating so default control in skin does override
                    TraktRateValue rating = Rated;

                    base.OnMessage(message);

                    // readjust rating and default control focus
                    int defaultControlId = ShowAdvancedRatings ? 1000 + (int)rating : rating == TraktRateValue.ten ? 100 : 101;
                    GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SETFOCUS, GetID, 0, defaultControlId, 0, 0, null);
                    OnMessage(msg);

                    IsSubmitted = false;
                    UpdateRating();
                    return true;

                case GUIMessage.MessageType.GUI_MSG_SETFOCUS:
                    if (btnUnRate != null && message.TargetControlId == btnUnRate.GetID)
                    {
                        Rated = TraktRateValue.unrate;
                        UpdateRating();
                        break;
                    }

                    if (!ShowAdvancedRatings && (message.TargetControlId < 100 || message.TargetControlId > 101))
                        break;
                    if (ShowAdvancedRatings && (message.TargetControlId < 1001 || message.TargetControlId > 1010))
                        break;

                    if (ShowAdvancedRatings)
                        Rated = (TraktRateValue)(message.TargetControlId - 1000);
                    else
                        Rated = message.TargetControlId == 100 ? TraktRateValue.ten : TraktRateValue.one;
                    UpdateRating();
                    break;
            }
            return base.OnMessage(message);
        }

        #endregion

        #region Private Methods

        private void UpdateRating()
        {
            if (btnUnRate != null)
            {
                btnUnRate.Selected = (Rated == TraktRateValue.unrate);
                btnUnRate.Focus = (Rated == TraktRateValue.unrate);
            }

            // Update button states
            if (!ShowAdvancedRatings)
            {
                btnLove.Selected = (Rated == TraktRateValue.ten);
                btnHate.Selected = (Rated == TraktRateValue.one);

                btnLove.Focus = (Rated == TraktRateValue.ten);
                btnHate.Focus = (Rated == TraktRateValue.one);
            }
            else
            {
                GUIToggleButtonControl[] btnHearts = new GUIToggleButtonControl[10] { btnOneHeart, btnTwoHeart, btnThreeHeart, btnFourHeart, btnFiveHeart,
															                          btnSixHeart, btnSevenHeart, btnEightHeart, btnNineHeart, btnTenHeart };

                for (int i = 0; i < 10; i++)
                {
                    btnHearts[i].Selected = ((int)Rated > i);
                }
                if ((int)Rated >= 1)
                    btnHearts[(int)Rated - 1].Focus = true;
            }

            // Update Rating Description
            if (lblRating != null && !ShowAdvancedRatings)
            {
                lblRating.Label = GetRatingDescription();
            }
            else if (lblRatingAdvanced != null && ShowAdvancedRatings)
            {
                lblRatingAdvanced.Label = Rated == TraktRateValue.unrate ? GetRatingDescription() : string.Format("({0}) {1} / 10", GetRatingDescription(), (int)Rated);
            }
        }

        private void SetControlVisibility()
        {
            // mandatory controls
            btnLove.Visible = !ShowAdvancedRatings;
            btnHate.Visible = !ShowAdvancedRatings;

            // skins may take a while to be updated
            // introduced in v1.6.0, this also includes skin check
            if (ShowAdvancedRatings)
            {
                btnOneHeart.Visible = ShowAdvancedRatings;
                btnTwoHeart.Visible = ShowAdvancedRatings;
                btnThreeHeart.Visible = ShowAdvancedRatings;
                btnFourHeart.Visible = ShowAdvancedRatings;
                btnFiveHeart.Visible = ShowAdvancedRatings;
                btnSixHeart.Visible = ShowAdvancedRatings;
                btnSevenHeart.Visible = ShowAdvancedRatings;
                btnEightHeart.Visible = ShowAdvancedRatings;
                btnNineHeart.Visible = ShowAdvancedRatings;
                btnTenHeart.Visible = ShowAdvancedRatings;
            }

            // conditional controls
            if (lblRating != null)
                lblRating.Visible = !ShowAdvancedRatings;
            if (lblRatingAdvanced != null)
                lblRatingAdvanced.Visible = ShowAdvancedRatings;
        }

        private string GetRatingDescription()
        {
            string description = string.Empty;

            switch (Rated)
            {
                case TraktRateValue.unrate:
                    description = Translation.UnRate;
                    break;
                case TraktRateValue.love:
                case TraktRateValue.ten:
                    description = Translation.RateLove;
                    break;
                case TraktRateValue.nine:
                    description = Translation.RateNine;
                    break;
                case TraktRateValue.eight:
                    description = Translation.RateEight;
                    break;
                case TraktRateValue.seven:
                    description = Translation.RateSeven;
                    break;
                case TraktRateValue.six:
                    description = Translation.RateSix;
                    break;
                case TraktRateValue.five:
                    description = Translation.RateFive;
                    break;
                case TraktRateValue.four:
                    description = Translation.RateFour;
                    break;
                case TraktRateValue.three:
                    description = Translation.RateThree;
                    break;
                case TraktRateValue.two:
                    description = Translation.RateTwo;
                    break;
                case TraktRateValue.hate:
                case TraktRateValue.one:
                    description = Translation.RateHate;
                    break;
            }

            return description;
        }

        private void CheckSkinCompatibility()
        {
            // if one of the advanced buttons are not supported
            // then its safe to say only simple ratings are supported
            if (btnOneHeart == null)
            {
                ShowAdvancedRatings = false;
            }
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
