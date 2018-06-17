using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace TraktPluginMP2.Models
{
  public class TraktSetupModel : IWorkflowModel
  {
    private static readonly Guid TRAKT_SETUP_MODEL_ID = new Guid("0A24888F-63C0-442A-9DF6-431869BDE803");

    private readonly TraktSetupModelManager _manager;

    public TraktSetupModel()
    {
      _manager = TraktSetupModelContainer.ResolveManager();
    }

    #region Public properties - Bindable Data

    public AbstractProperty IsScrobbleEnabledProperty
    {
      get { return _manager.IsScrobbleEnabledProperty; }
    }

    public bool IsScrobbleEnabled
    {
      get { return _manager.IsScrobbleEnabled; }
      set { _manager.IsScrobbleEnabled = value; }
    }

    public AbstractProperty IsUserAuthorizedProperty
    {
      get { return _manager.IsUserAuthorizedProperty; }
    }

    public bool IsUserAuthorized
    {
      get { return _manager.IsUserAuthorized; }
      set { _manager.IsUserAuthorized = value; }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _manager.TestStatusProperty; }
    }

    public string TestStatus
    {
      get { return _manager.TestStatus; }
      set { _manager.TestStatus = value; }
    }

    public AbstractProperty PinCodeProperty
    {
      get { return _manager.PinCodeProperty; }
    }

    public string PinCode
    {
      get { return _manager.PinCode; }
      set { _manager.PinCode = value; }
    }

    public AbstractProperty IsSynchronizingProperty
    {
      get { return _manager.IsSynchronizingProperty; }
    }

    public bool IsSynchronizing
    {
      get { return _manager.IsSynchronizing; }
      set { _manager.IsSynchronizing = value; }
    }

    #endregion

    #region Public methods - Commands

    public void AuthorizeUser()
    {
      _manager.AuthorizeUser();
    }

    public void SyncMediaToTrakt()
    {
      _manager.SyncMediaToTrakt();

    }

    #endregion

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _manager.Initialize();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    public Guid ModelId
    {
      get { return TRAKT_SETUP_MODEL_ID; }
    }
  }
}
