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


   public class SpreadedSmileSection : QLNet.SpreadedSmileSection
   {
      public SpreadedSmileSection(SmileSection underlyingSection,
                                               Handle<Quote> spread)
       : base(underlyingSection, spread) { }

      //public SpreadedSmileSection(SpreadedSmileSection underlyingSection)
      //  : base(underlyingSection) {
      //   }

      protected override double volatilityImpl(double k)
      {
         double spreadedVol = volatilityImpl(k);
         return System.Math.Max(spreadedVol, 0.0);
      }
   }

}
