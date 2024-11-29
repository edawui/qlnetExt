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
   /*! \file qle/termstructures/spreadedswaptionvolatility.hpp
      \brief Adds floor toSpreadedSwaptionVolatility
      \ingroup termstructures
  */

   public class SpreadedSwaptionVolatility : QLNet.SpreadedSwaptionVolatility
   {


      SpreadedSwaptionVolatility(Handle<SwaptionVolatilityStructure> baseVol,
                                                           Handle<Quote> spread)
       : base(baseVol, spread) { }

      protected override SmileSection smileSectionImpl(Date d, Period p)
      {
         SpreadedSmileSection section = (SpreadedSmileSection)(smileSectionImpl(d, p));
         return section;

         //return new SpreadedSmileSection(section);
      }

      protected override SmileSection smileSectionImpl(double t, double l)
      {
         SpreadedSmileSection section = (SpreadedSmileSection)(smileSectionImpl(t, l));
         return section;

         //return new SpreadedSmileSection(section);
      }

      protected override double volatilityImpl(Date d, Period p, double strike)
      {
         double spreadedVol = volatilityImpl(d, p, strike);
         return System.Math.Max(spreadedVol, 0.0);
      }

      protected override double volatilityImpl(double t, double l, double strike)
      {
         double spreadedVol = volatilityImpl(t, l, strike);
         return System.Math.Max(spreadedVol, 0.0);
      }
   }

}
