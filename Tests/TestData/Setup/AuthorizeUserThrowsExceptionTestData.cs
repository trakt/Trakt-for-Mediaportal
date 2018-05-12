using System;
using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Exceptions;
using TraktPluginMP2.Settings;

namespace Tests.TestData.Setup
{
  public class AuthorizeUserThrowsExceptionTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        "EFE67ED9",
        new TraktAuthenticationOAuthException("exception occurred"),
        "[Trakt.InvalidAuthorizationCode]"
      };
      yield return new object[]
      {
        "EFE67ED9",
        new TraktException("exception occurred"),
        "[Trakt.AuthorizationFailed]"
      };
      yield return new object[]
      {
        "EFE67ED9",
        new ArgumentException("exception occurred"),
        "[Trakt.AuthorizationCodeIsEmptyOrContainsSpaces]"
      };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}