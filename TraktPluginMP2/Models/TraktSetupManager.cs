using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using TraktPluginMP2.Services;
using TraktPluginMP2.Settings;

namespace TraktPluginMP2.Models
{
  public class TraktSetupManager
  {
    private readonly IMediaPortalServices _mediaPortalServices;
    private readonly ITraktServices _traktServices;

    private readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    private readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), string.Empty);
    private readonly AbstractProperty _usermameProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _pinCodeProperty = new WProperty(typeof(string), null);
    private readonly AbstractProperty _isSynchronizingProperty = new WProperty(typeof(bool), false);


    public TraktSetupManager(IMediaPortalServices mediaPortalServices, ITraktServices traktServices)
    {
      _traktServices = traktServices;
      _mediaPortalServices = mediaPortalServices;
    }

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool)_isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _testStatusProperty; }
    }

    public string TestStatus
    {
      get { return (string)_testStatusProperty.GetValue(); }
      set { _testStatusProperty.SetValue(value); }
    }

    public AbstractProperty UsernameProperty
    {
      get { return _usermameProperty; }
    }

    public string Username
    {
      get { return (string)_usermameProperty.GetValue(); }
      set { _usermameProperty.SetValue(value); }
    }

    public AbstractProperty PinCodeProperty
    {
      get { return _pinCodeProperty; }
    }

    public string PinCode
    {
      get { return (string)_pinCodeProperty.GetValue(); }
      set { _pinCodeProperty.SetValue(value); }
    }

    public AbstractProperty IsSynchronizingProperty
    {
      get { return _isSynchronizingProperty; }
    }

    public bool IsSynchronizing
    {
      get { return (bool)_isSynchronizingProperty.GetValue(); }
      set { _isSynchronizingProperty.SetValue(value); }
    }

    public void AuthorizeUser()
    {
      if (string.IsNullOrEmpty(PinCode) || PinCode.Length != 8)
      {
        TestStatus = "[Trakt.WrongToken]";
        _mediaPortalServices.GetLogger().Error("Wrong pin entered");
        return;
      }

      if (!_traktServices.GetTraktLogin().Login(PinCode))
      {
        TestStatus = "[Trakt.UnableLogin]";
        return;
      }
        
      if (string.IsNullOrEmpty(Username))
      {
        TestStatus = "[Trakt.EmptyUsername]";
        _mediaPortalServices.GetLogger().Error("Username is missing");
        return;
      }

      ISettingsManager settingsManager = _mediaPortalServices.GetSettingsManager();
      TraktPluginSettings settings = settingsManager.Load<TraktPluginSettings>();

      settings.EnableTrakt = IsEnabled;
      settings.Username = Username;
      settingsManager.Save(settings);

      TestStatus = "[Trakt.LoggedIn]";
    }

    public void SyncMediaToTrakt()
    {
      // TODO: implement!
    }
  }
}