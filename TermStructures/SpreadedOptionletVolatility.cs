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

/*! \file qle/termstructures/spreadedswaptionvolatility.hpp
    \brief Adds floor toSpreadedSwaptionVolatility
    \ingroup termstructures
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{

  
   public class SpreadedOptionletVolatility : QLNet.SpreadedOptionletVolatility
   {


      public SpreadedOptionletVolatility(Handle<OptionletVolatilityStructure> baseVol,
                                                          Handle<Quote> spread)
       : base(baseVol, spread) { }

      protected override SmileSection smileSectionImpl(Date d)
      {
         SpreadedSmileSection section = (SpreadedSmileSection)(smileSectionImpl(d));
         return section;

         //return new SpreadedSmileSection(section);
      }

      protected override SmileSection smileSectionImpl(double optionTime)
      {
         SpreadedSmileSection section = (SpreadedSmileSection)(smileSectionImpl(optionTime));
         return section;
         //return new SpreadedSmileSection(section);
      }

      protected override double volatilityImpl(double t, double s)
      {
         double spreadedVol = volatilityImpl(t, s);
         return System.Math.Max(spreadedVol, 0.0);
      }
   }

}
