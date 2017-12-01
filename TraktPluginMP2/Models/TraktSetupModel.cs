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

    private readonly TraktSetupManager _manager;

    public TraktSetupModel()
    {
      _manager = TraktSetupContainer.ResolveManager();
    }

    #region Public properties - Bindable Data

    public AbstractProperty IsEnabledProperty
    {
      get { return _manager.IsEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return _manager.IsEnabled; }
      set { _manager.IsEnabled = value; }
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

    public AbstractProperty UsernameProperty
    {
      get { return _manager.UsernameProperty; }
    }

    public string Username
    {
      get { return _manager.Username; }
      set { _manager.Username = value; }
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
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // do save
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
