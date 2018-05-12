using System.Collections;
using System.Collections.Generic;
using TraktApiSharp.Authentication;

namespace Tests.TestData.Setup
{
  public class AuthorizeUserDoNotThrowEceptionTestData : IEnumerable<object[]>
  {
    public IEnumerator<object[]> GetEnumerator()
    {
      yield return new object[]
      {
        "EFE67ED9",
        new TraktAuthorization
        {
          RefreshToken = "7e7605b9103c1c1f7afcf18dabc583499155ea6318a9d7e49f06568866bd4adb",
          AccessToken = "20f668873a00806dfc22136042f2eb81a44d98ab9b739acf68f2b46b784dcd0c"
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