using System.Text.RegularExpressions;

namespace SmartIrcBot4net.Extensions
{
  public static class HelperExtensions
  {
    public static string Get(this GroupCollection groupCollection, string key)
    {
      var res = groupCollection[key];

      if (res == null) {
        return null;
      } else if (res.Value == null) {
        return null;
      } else if (res.Value.Length == 0) {
        return null;
      }

      return res.Value;
    }
  }
}

