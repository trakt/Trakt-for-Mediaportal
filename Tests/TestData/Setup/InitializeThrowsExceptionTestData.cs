using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Exceptions;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Setup
{
  public class InitializeThrowsExceptionTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
        },
        new TraktAuthorizationException("exception occurred"),
        "[Trakt.AuthorizationFailed]"
      };
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
        },
        new TraktAuthenticationException("exception occurred"),
        "[Trakt.InvalidRefreshToken]"
      };
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
        },
        new TraktException("exception occurred"),
        "[Trakt.AuthorizationFailed]"
      };
      yield return new object[]
      {
        new TraktPluginSettings
        {
          RefreshToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
        },
        new ArgumentException("exception occurred"),
        "[Trakt.RefreshTokenIsEmptyOrContainsSpaces]"
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}