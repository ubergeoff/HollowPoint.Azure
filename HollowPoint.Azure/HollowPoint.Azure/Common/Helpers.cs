using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint.Azure.Common
{
    public class Helpers
    {
        public static string[] QuickSplit(string stringToSplit, string delimitedSplitCharacters)
        {
            List<string> splitCharacters = new List<string>();
            for (var x = 0; x < delimitedSplitCharacters.Length; x++)
            {
                var c = delimitedSplitCharacters.Substring(x, 1);
                splitCharacters.Add(c);
            }


            return stringToSplit.Split(splitCharacters.ToArray(), StringSplitOptions.RemoveEmptyEntries);

        }

    }
}
