using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI;
using Action = MediaPortal.GUI.Library.Action;
using System;

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
        protected GUICheckMarkControl btnOneHeart = null;
        [SkinControlAttribute(1002)]
        protected GUICheckMarkControl btnTwoHeart = null;
        [SkinControlAttribute(1003)]
        protected GUICheckMarkControl btnThreeHeart = null;
        [SkinControlAttribute(1004)]
        protected GUICheckMarkControl btnFourHeart = null;
        [SkinControlAttribute(1005)]
        protected GUICheckMarkControl btnFiveHeart = null;
        [SkinControlAttribute(1006)]
        protected GUICheckMarkControl btnSixHeart = null;
        [SkinControlAttribute(1007)]
        protected GUICheckMarkControl btnSevenHeart = null;
        [SkinControlAttribute(1008)]
        protected GUICheckMarkControl btnEightHeart = null;
        [SkinControlAttribute(1009)]
        protected GUICheckMarkControl btnNineHeart = null;
        [SkinControlAttribute(1010)]
        protected GUICheckMarkControl btnTenHeart = null;

        #region Public Properties

        public TraktRateValue Rated { get; set; }
        public bool IsSubmitted { get; set; }

        [Obsolete("This property is no longer used")]
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
                    Rated = TraktRateValue.one;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_2:
                    Rated = TraktRateValue.two;
                    UpdateRating();
                    break; 
               
                case Action.ActionType.REMOTE_3:
                    Rated = TraktRateValue.three;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_4:
                    Rated = TraktRateValue.four;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_5:
                    Rated = TraktRateValue.five;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_6:
                    Rated = TraktRateValue.six;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_7:
                    Rated = TraktRateValue.seven;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_8:
                    Rated = TraktRateValue.eight;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_9:
                    Rated = TraktRateValue.nine;
                    UpdateRating();
                    break;

                case Action.ActionType.REMOTE_0:
                    Rated = TraktRateValue.ten;
                    UpdateRating();
                    break;

                case Action.ActionType.ACTION_KEY_PRESSED:
                    // some types of remotes send ACTION_KEY_PRESSED instead of REMOTE_0 - REMOTE_9 commands
                    if (action.m_key != null)
                    {
                        int key = action.m_key.KeyChar;
                        if (key >= '0' && key <= '9')
                        {
                            if (key == 0) key = 10;
                            Rated = (TraktRateValue)key;
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

                    // read just rating and default control focus
                    int defaultControlId = 1000 + (int)rating;
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

                    if (message.TargetControlId < 1001 || message.TargetControlId > 1010)
                        break;

                  
                    Rated = (TraktRateValue)(message.TargetControlId - 1000);
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
            GUICheckMarkControl[] btnHearts = new GUICheckMarkControl[10]
            { 
                btnOneHeart, btnTwoHeart, btnThreeHeart, btnFourHeart, btnFiveHeart,
				btnSixHeart, btnSevenHeart, btnEightHeart, btnNineHeart, btnTenHeart
            };

            for (int i = 0; i < 10; i++)
            {
                btnHearts[i].Label = string.Empty;
                btnHearts[i].Selected = ((int)Rated > i);
            }
            if ((int)Rated >= 1)
                btnHearts[(int)Rated - 1].Focus = true;

            // Update Rating Description
            lblRatingAdvanced.Label = Rated == TraktRateValue.unrate ? GetRatingDescription() : string.Format("({0}) {1} / 10", GetRatingDescription(), (int)Rated);
        }

        private void SetControlVisibility()
        {
            // mandatory controls
            btnLove.Visible = false;
            btnHate.Visible = false;

            btnOneHeart.Visible = true;
            btnTwoHeart.Visible = true;
            btnThreeHeart.Visible = true;
            btnFourHeart.Visible = true;
            btnFiveHeart.Visible = true;
            btnSixHeart.Visible = true;
            btnSevenHeart.Visible = true;
            btnEightHeart.Visible = true;
            btnNineHeart.Visible = true;
            btnTenHeart.Visible = true;

            // conditional controls
            if (lblRating != null)
                lblRating.Visible = false;
            if (lblRatingAdvanced != null)
                lblRatingAdvanced.Visible = true;
        }

        private string GetRatingDescription()
        {
            string description = string.Empty;

            switch (Rated)
            {
                case TraktRateValue.unrate:
                    description = Translation.UnRate;
                    break;
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
                case TraktRateValue.one:
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
