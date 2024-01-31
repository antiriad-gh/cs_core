namespace Antiriad.Core.Helpers;

public static class Wildcard
{
  public static bool IsMatch(string text, string pattern)
  {
    return IsMatch(text.ToCharArray(), pattern.ToCharArray());
  }

  public static bool IsMatch(char[] text, char[] pattern)
  {
    var tindex = 0;
    var pindex = 0;
    var amark = -1;
    var pchar = '\0';
    var tlen = text.Length;
    var plen = pattern.Length;

    while (tindex < tlen)
    {
      var tchar = text[tindex++];

      if (pindex < plen)
        pchar = pattern[pindex];
      else if (amark >= 0)
        pchar = pattern[(pindex = amark)];

      if (pchar == '*')
      {
        while (pindex < plen - 1 && pattern[pindex + 1] == '*')
          pindex++;

        if ((amark = ++pindex) == plen) // last pattern char is * so accept remaining text
          return true;

        tindex--; // check last text char again
      }
      else if (pchar == '?')
      {
        if (++pindex == plen && amark >= 0) // pattern is finished but last char was *
          pindex = amark; // step back to last *
      }
      else if (tchar == pchar)
      {
        pindex++;

        if (tindex == tlen && pindex < plen) // text is finished but pattern is not
        {
          while (pindex < plen && pattern[pindex] == '*')
            pindex++;

          return pindex == plen; // true if all remaining pattern are *
        }
      }
      else if (tchar != pchar)
      {
        if (amark >= 0 && tindex < tlen)
          pindex = amark; // step back to last *
        else
          return false;
      }
    }

    return tindex == tlen;
  }
}