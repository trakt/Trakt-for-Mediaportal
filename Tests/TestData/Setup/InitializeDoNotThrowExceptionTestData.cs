using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Authentication;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Setup
{
  public class InitializeDoNotThrowExceptionTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = ""
        },
        new TraktAuthorization
        {
          RefreshToken = ""
        },
        "[Trakt.NotAuthorized]"
      };
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
        },
        new TraktAuthorization
        {
          RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb"
        },
        "[Trakt.AuthorizationSucceed]"
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}