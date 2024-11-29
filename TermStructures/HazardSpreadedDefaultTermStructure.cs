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

/*! \file qle/termstructures/hazardspreadeddefaulttermstructure.hpp
    \brief adds a ant hazard rate spread to a default term structure
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
   public class HazardSpreadedDefaultTermStructure : HazardRateStructure
   {

      Handle<DefaultProbabilityTermStructure> source_;
      Handle<Quote> spread_;


      public HazardSpreadedDefaultTermStructure(
     Handle<DefaultProbabilityTermStructure> source, Handle<Quote> spread)
         : base()
      {
         source_ = source; spread_ = spread;

         if (!source_.empty())
            enableExtrapolation(source_.currentLink().allowsExtrapolation());

         source_.registerWith(update);
         spread_.registerWith(update);
      }
      protected override double hazardRateImpl(double t)
      {
         return source_.currentLink().hazardRate(t) + spread_.currentLink().value();
      }

      protected override double survivalProbabilityImpl(double t)
      {
         return source_.currentLink().survivalProbability(t) * System.Math.Exp(-spread_.currentLink().value() * t);
      }

      public override DayCounter dayCounter() { return source_.currentLink().dayCounter(); }

      public override Date maxDate() { return source_.currentLink().maxDate(); }

      public override double maxTime() { return source_.currentLink().maxTime(); }

      public override Date referenceDate() { return source_.currentLink().referenceDate(); }

      public override Calendar calendar() { return source_.currentLink().calendar(); }

      public override int settlementDays() { return source_.currentLink().settlementDays(); }


   }
}
