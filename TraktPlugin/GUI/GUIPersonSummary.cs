using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Extensions;
using TraktPlugin.TraktAPI.DataStructures;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
    public class GUIPersonSummary : GUIWindow
    {
        #region Skin Controls

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Enums

        enum View
        {
            Summary,
            MovieCredits,
            ShowCredits
        }

        #endregion

        #region Constructor

        public GUIPersonSummary()
        {

        }

        #endregion

        #region Private Variables

        bool StopDownload = false;
        DateTime LastRequest = new DateTime();
        Dictionary<string, TraktPersonSummary> people = new Dictionary<string, TraktPersonSummary>();
        Dictionary<string, TraktPersonMovieCredits> movieCredits = new Dictionary<string, TraktPersonMovieCredits>();
        Dictionary<string, TraktPersonShowCredits> showCredits = new Dictionary<string, TraktPersonShowCredits>();
        View CurrentView { get; set; }

        TraktPersonMovieCredits MovieCredits
        {
            get
            {
                string personId = CurrentPerson.Ids.Trakt.ToString();

                if (!movieCredits.ContainsKey(personId) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var credits = TraktAPI.TraktAPI.GetMovieCreditsForPerson(personId);
                    if (credits == null) return null;

                    if (movieCredits.ContainsKey(personId))
                        movieCredits.Remove(personId);

                    movieCredits.Add(personId, credits);

                    LastRequest = DateTime.UtcNow;
                }
                return movieCredits[personId];
            }
        }

        TraktPersonShowCredits ShowCredits
        {
            get
            {
                string personId = CurrentPerson.Ids.Trakt.ToString();

                if (!showCredits.ContainsKey(personId) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var credits = TraktAPI.TraktAPI.GetShowCreditsForPerson(personId);
                    if (credits == null) return null;

                    if (showCredits.ContainsKey(personId))
                        showCredits.Remove(personId);

                    showCredits.Add(personId, credits);

                    LastRequest = DateTime.UtcNow;
                }
                return showCredits[personId];
            }
        }

        TraktPersonSummary PersonSummary
        {
            get
            {
                var slug = _loadParameter.ToSlug();

                if (!people.ContainsKey(slug) || LastRequest < DateTime.UtcNow.Subtract(new TimeSpan(0, TraktSettings.WebRequestCacheMinutes, 0)))
                {
                    var person = TraktAPI.TraktAPI.GetPersonSummary(slug);
                    if (person == null) return null;

                    if (people.ContainsKey(slug))
                        people.Remove(slug);

                    people.Add(slug, person);
                    
                    LastRequest = DateTime.UtcNow;
                }
                return people[slug];
            }
        }

        #endregion

        #region Base Overrides

        public override int GetID
        {
            get
            {
                return (int)TraktGUIWindows.PersonSummary;
            }
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\Trakt.Person.Summary.xml");
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (string.IsNullOrEmpty(_loadParameter) && CurrentPerson == null)
            {
                TraktLogger.Info("Exiting Person Summary as there is no loading parameter or current person set");
                GUIWindowManager.ActivateWindow(GUIWindowManager.GetPreviousActiveWindow());
                return;
            }

            // Clear GUI Properties
            ClearProperties();

            // Init Properties
            InitProperties();

            // Load the correct view
            switch (CurrentView)
            {
                case View.Summary:
                    LoadPersonSummary();
                    break;
                case View.MovieCredits:
                    LoadMovieCredits();
                    break;
                case View.ShowCredits:
                    LoadShowCredits();
                    break;
            }
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            StopDownload = true;
            ClearProperties();

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            // wait for any background action to finish
            if (GUIBackgroundTask.Instance.IsBusy) return;
            
            switch (controlId)
            {
                // Facade
                case (50):
                    var selectedItem = Facade.SelectedListItem;
                    if (selectedItem == null) return;

                    if (CurrentView == View.Summary)
                    {
                        if (selectedItem.TVTag as string == View.MovieCredits.ToString())
                        {
                            LoadMovieCredits();
                        }
                        else if (selectedItem.TVTag as string == View.ShowCredits.ToString())
                        {
                            LoadShowCredits();
                        }
                    }
                    else if (CurrentView == View.MovieCredits)
                    {
                        GUIPersonMovieCredits.CurrentPerson = CurrentPerson;
                        GUIPersonMovieCredits.CurrentCredits = selectedItem.MusicTag as TraktPersonMovieCredits;
                        GUIPersonMovieCredits.CurrentCreditType = (Credit)selectedItem.TVTag;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.PersonCreditMovies);
                    }
                    else if (CurrentView == View.ShowCredits)
                    {
                        GUIPersonShowCredits.CurrentPerson = CurrentPerson;
                        GUIPersonShowCredits.CurrentCredits = selectedItem.MusicTag as TraktPersonShowCredits;
                        GUIPersonShowCredits.CurrentCreditType = (Credit)selectedItem.TVTag;
                        GUIWindowManager.ActivateWindow((int)TraktGUIWindows.PersonCreditShows);
                    }
                    break;

                default:
                    break;
            }
            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    if (CurrentView == View.Summary)
                    {
                        _loadParameter = null;
                        CurrentPerson = null;
                        base.OnAction(action);
                    }
                    else
                    {
                        // return to summary view
                        CurrentView = View.Summary;
                        LoadPersonSummary();
                    }
                    break; 

                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            base.OnShowContextMenu();
        }

        #endregion

        #region Private Methods

        private void LoadMovieCredits()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return MovieCredits;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var credits = result as TraktPersonMovieCredits;
                    SendMovieCreditsToFacade(credits);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendMovieCreditsToFacade(TraktPersonMovieCredits credits)
        {
            if (credits == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                CurrentView = View.Summary;
                LoadPersonSummary();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // set the current view
            CurrentView = View.MovieCredits;

            var personImages = new List<GUITraktImage>();

            int itemId = 0;
            GUIPersonListItem item = null;

            // Add all the Cast and Crew items
            if (credits.Cast != null && credits.Cast.Count > 0)
            {
                // add image for download
                var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                personImages.Add(images);

                item = new GUIPersonListItem(Translation.Actor, (int)TraktGUIWindows.PersonSummary);
                item.Label2 = string.Format(Translation.MovieCount, credits.Cast.Count);
                item.TVTag = Credit.Cast;
                item.MusicTag = credits;
                item.Images = images;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            if (credits.Crew != null)
            {
                if (credits.Crew.Production != null && credits.Crew.Production.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Production, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Production.Count);
                    item.TVTag = Credit.Production;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Directing != null && credits.Crew.Directing.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Directing, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Directing.Count);
                    item.TVTag = Credit.Directing;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Writing != null && credits.Crew.Writing.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Writing, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Writing.Count);
                    item.TVTag = Credit.Writing;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Art != null && credits.Crew.Art.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Art, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Art.Count);
                    item.TVTag = Credit.Art;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Camera != null && credits.Crew.Camera.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Camera, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Camera.Count);
                    item.TVTag = Credit.Camera;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.CostumeAndMakeUp != null && credits.Crew.CostumeAndMakeUp.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.CostumeAndMakeUp, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.CostumeAndMakeUp.Count);
                    item.TVTag = Credit.CostumeAndMakeUp;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Sound != null && credits.Crew.Sound.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Sound, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Sound.Count);
                    item.TVTag = Credit.Sound;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }
            }

            if (Facade.Count == 0)
            {
                GUIUtils.ShowNotifyDialog(Translation.MovieCredits, Translation.NoCreditsFound);
                CurrentView = View.Summary;
                LoadPersonSummary();
                return;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());

            GUIPersonListItem.GetImages(personImages, false);
        }

        private void LoadShowCredits()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return ShowCredits;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var credits = result as TraktPersonShowCredits;
                    SendShowCreditsToFacade(credits);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendShowCreditsToFacade(TraktPersonShowCredits credits)
        {
            if (credits == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                CurrentView = View.Summary;
                LoadPersonSummary();
                return;
            }

            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            // set the current view
            CurrentView = View.ShowCredits;

            var personImages = new List<GUITraktImage>();

            int itemId = 0;
            GUIPersonListItem item = null;

            // Add all the Cast and Crew items
            if (credits.Cast != null && credits.Cast.Count > 0)
            {
                // add image for download
                var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                personImages.Add(images);

                item = new GUIPersonListItem(Translation.Actor, (int)TraktGUIWindows.PersonSummary);
                item.Label2 = string.Format(Translation.ShowCount, credits.Cast.Count);
                item.TVTag = Credit.Cast;
                item.Images = images;
                item.MusicTag = credits;
                item.ItemId = Int32.MaxValue - itemId;
                item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                item.OnItemSelected += OnItemSelected;
                Utils.SetDefaultIcons(item);
                Facade.Add(item);
                itemId++;
            }

            if (credits.Crew != null)
            {
                if (credits.Crew.Production != null && credits.Crew.Production.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Production, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.Production.Count);
                    item.TVTag = Credit.Production;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Directing != null && credits.Crew.Directing.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Directing, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.Directing.Count);
                    item.TVTag = Credit.Directing;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Writing != null && credits.Crew.Writing.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Writing, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.Writing.Count);
                    item.TVTag = Credit.Writing;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Art != null && credits.Crew.Art.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Art, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.Art.Count);
                    item.TVTag = Credit.Art;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Camera != null && credits.Crew.Camera.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);
                    
                    item = new GUIPersonListItem(Translation.Camera, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.Camera.Count);
                    item.TVTag = Credit.Camera;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.CostumeAndMakeUp != null && credits.Crew.CostumeAndMakeUp.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.CostumeAndMakeUp, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.ShowCount, credits.Crew.CostumeAndMakeUp.Count);
                    item.TVTag = Credit.CostumeAndMakeUp;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }

                if (credits.Crew.Sound != null && credits.Crew.Sound.Count > 0)
                {
                    // add image for download
                    var images = new GUITraktImage { PeopleImages = CurrentPerson.Images };
                    personImages.Add(images);

                    item = new GUIPersonListItem(Translation.Sound, (int)TraktGUIWindows.PersonSummary);
                    item.Label2 = string.Format(Translation.MovieCount, credits.Crew.Sound.Count);
                    item.TVTag = Credit.Sound;
                    item.MusicTag = credits;
                    item.Images = images;
                    item.ItemId = Int32.MaxValue - itemId;
                    item.IconImage = GUIImageHandler.GetDefaultPoster(false);
                    item.IconImageBig = GUIImageHandler.GetDefaultPoster();
                    item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
                    item.OnItemSelected += OnItemSelected;
                    Utils.SetDefaultIcons(item);
                    Facade.Add(item);
                    itemId++;
                }
            }

            if (Facade.Count == 0)
            {
                GUIUtils.ShowNotifyDialog(Translation.ShowCredits, Translation.NoCreditsFound);
                CurrentView = View.Summary;
                LoadPersonSummary();
                return;
            }

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);

            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());

            GUIPersonListItem.GetImages(personImages, false);
        }

        private void LoadPersonSummary()
        {
            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                if (CurrentPerson != null)
                    return CurrentPerson;

                CurrentPerson = PersonSummary;

                return CurrentPerson;
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var person = result as TraktPersonSummary;
                    SendPersonSummaryToFacade(person);
                }
            }, Translation.GettingSearchResults, true);
        }

        private void SendPersonSummaryToFacade(TraktPersonSummary person)
        {
            // clear facade
            GUIControl.ClearControl(GetID, Facade.GetID);

            if (person == null)
            {
                GUIUtils.ShowNotifyDialog(Translation.Error, Translation.ErrorGeneral);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }
            
            // publish person properties
            PublishSkinProperties(person);

            var personImages = new List<GUITraktImage>();
            int itemId = 0;

            // add image for download
            var images = new GUITraktImage { PeopleImages = person.Images };
            personImages.Add(images);

            // Add movie and show credit items
            var item = new GUIPersonListItem(Translation.MovieCredits, (int)TraktGUIWindows.PersonSummary);

            item.TVTag = View.MovieCredits.ToString();
            item.ItemId = Int32.MaxValue - itemId;
            item.Images = images;
            item.IconImage = GUIImageHandler.GetDefaultPoster(false);
            item.IconImageBig = GUIImageHandler.GetDefaultPoster();
            item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
            item.OnItemSelected += OnItemSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);
            itemId++;

            // add image for download
            images = new GUITraktImage { PeopleImages = person.Images };
            personImages.Add(images);

            item = new GUIPersonListItem(Translation.ShowCredits, (int)TraktGUIWindows.PersonSummary);

            item.TVTag = View.ShowCredits.ToString();
            item.ItemId = Int32.MaxValue - itemId;
            item.Images = images;
            item.IconImage = GUIImageHandler.GetDefaultPoster(false);
            item.IconImageBig = GUIImageHandler.GetDefaultPoster();
            item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
            item.OnItemSelected += OnItemSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);
            itemId++;

            // Set Facade Layout
            Facade.SetCurrentLayout("List");
            GUIControl.FocusControl(GetID, Facade.GetID);
            
            // set facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());

            // Download images Async
            DownloadFanart(person);

            GUIPersonListItem.GetImages(personImages, false);
        }

        private void DownloadFanart(TraktPersonSummary person)
        {
            var tFanart = new Thread(obj =>
            {
                var tPerson = obj as TraktPersonSummary;

                string remoteThumb = TraktSettings.DownloadFullSizeFanart ? tPerson.Images.Fanart.FullSize : tPerson.Images.Fanart.MediumSize;
                string localThumb = tPerson.Images.Fanart.LocalImageFilename(ArtworkType.PersonFanart);

                if (localThumb == null || remoteThumb == null)
                    return;

                GUIImageHandler.DownloadImage(remoteThumb, localThumb);
                if (!StopDownload)
                {
                    Thread.Sleep(500);
                    GUIUtils.SetProperty("#Trakt.Person.FanartFilename", localThumb);
                }
            })
            {
                Name = "Fanart"
            };            
            
            tFanart.Start(person);            
        }

        private void InitProperties()
        {
            if (CurrentPerson != null)
            {
                PublishSkinProperties(CurrentPerson);
            }
            return;
        }

        private void ClearProperties()
        {
            StopDownload = false;
            GUICommon.ClearPersonProperties();
        }

        private void PublishSkinProperties(TraktPersonSummary person)
        {
            GUICommon.SetPersonProperties(person);
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item == null) return;

            PublishSkinProperties(CurrentPerson);
        }

        #endregion

        #region Public Static Properties

        public static TraktPersonSummary CurrentPerson { get; set; }

        #endregion
    }
}