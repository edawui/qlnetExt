/*
Copyright (C) 2022  Edem Dawui (edawui@gmail.com)

 This file is part of QLNetExt Project.

QLNetExt is based on ORE library, a free-software/open-source library
 for transparent pricing and risk analysis - http://opensourcerisk.org
 
 This program is distributed on the basis that it will form a useful
 contribution to risk analytics and model standardisation, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 FITNESS FOR A PARTICULAR PURPOSE. See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class PeriodParser
   {
      public PeriodParser()
      { }

      public Period parse(string str)
      {
         Utils.QL_REQUIRE(str.Length > 1, () => "period string length must be at least 2");

         List<string> subStrings = new List<string>();
         string reducedString = str;

         string mainChars = "DdWwMmYy";

         int iPos, reducedStringDim = 100000, max_iter = 0;
         while (reducedStringDim > 0)
         {

            // char first = reducedString.First(ch => mainChars.IndexOf(ch) > 0);
            int firstIndex = (reducedString.Select((ch, i) => new { Character = ch, Index = i })
                                            .First(obj => mainChars.IndexOf(obj.Character) > 0) ?? new { Character = '\0', Index = -1 }).Index;

            iPos = firstIndex; //reducedString.find_first_of("DdWwMmYy");
            int subStringDim = iPos + 1;
            reducedStringDim = reducedString.Length - subStringDim;
            subStrings.Add(reducedString.Substring(0, subStringDim));
            reducedString = reducedString.Substring(iPos + 1, reducedStringDim);
            ++max_iter;
            Utils.QL_REQUIRE(max_iter < str.Length, () => "unknown '" + str + "' unit");
         }

         Period result = parseOnePeriod(subStrings[0]);
         for (int i = 1; i < subStrings.Count; ++i)
            result += parseOnePeriod(subStrings[i]);
         return result;
      }

      public Period parseOnePeriod(string str)
      {
         Utils.QL_REQUIRE(str.Length > 1, () => "single period require a string of at least 2 characters");

         string mainChars = "DdWwMmYy";
         int firstIndex = (str.Select((ch, i) => new { Character = ch, Index = i })
                                           .First(obj => mainChars.IndexOf(obj.Character) > 0) ?? new { Character = '\0', Index = -1 }).Index;

         int iPos = firstIndex;//str.find_first_of("DdWwMmYy");
         Utils.QL_REQUIRE(iPos == str.Length - 1, () => "unknown '" + str.Substring(str.Length - 1, str.Length) + "' unit");
         TimeUnit units = TimeUnit.Days;
         char abbr = ((str[iPos].ToString()).ToUpper()).ToCharArray()[0];


         if (abbr == 'D') units = TimeUnit.Days;
         else if (abbr == 'W') units = TimeUnit.Weeks;
         else if (abbr == 'M') units = TimeUnit.Months;
         else if (abbr == 'Y') units = TimeUnit.Years;

         string mainChars2 = "-+0123456789";
         int firstIndex2 = (str.Select((ch, i) => new { Character = ch, Index = i })
                                       .First(obj => mainChars2.IndexOf(obj.Character) > 0) ?? new { Character = '\0', Index = -1 }).Index;

         int nPos = firstIndex2;//str.find_first_of("-+0123456789");

         Utils.QL_REQUIRE(nPos < iPos, () => "no numbers of " + units + " provided");
         int n;
         try
         {
            n = int.Parse((str.Substring(nPos, iPos)).ToString());
            //boost::lexical_cast<Integer>(str.substr(nPos,iPos));
            return new Period(n, units);

         }
         catch (Exception e)
         {
            Utils.QL_FAIL("unable to parse the number of units of " + units + " in '" + str + "'. Error:" + e.Message);
            return null;
         }

      }

   }
}
