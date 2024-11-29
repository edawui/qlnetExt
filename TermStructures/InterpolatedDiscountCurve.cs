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

/*! \file interpolateddiscountcurve.hpp
    \brief interpolated discount term structure
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
   //! InterpolatedDiscountCurve based on loglinear interpolation of DiscountFactors
   /*! InterpolatedDiscountCurve based on loglinear interpolation of DiscountFactors,
       flat fwd extrapolation is always enabled, the term structure has always a
       floating reference date

           \ingroup termstructures
       */
   public class InterpolatedDiscountCurve : YieldTermStructure
   {



      List<double> times_;
      List<double> timeDiffs_;
      List<Quote> quotes_;

      public InterpolatedDiscountCurve(List<double> times, List<Handle<Quote>> quotes,
                                  int settlementDays, Calendar cal, DayCounter dc)
           : base(settlementDays, cal, dc)
      {
         times_ = times;
         initalise(quotes);
      }

      //! ructor that takes a vector of dates
      public InterpolatedDiscountCurve(List<Date> dates, List<Handle<Quote>> quotes,
                               int settlementDays, Calendar cal, DayCounter dc)
        : base(settlementDays, cal, dc)
      {
         times_ = new List<double>(dates.Count);
         for (int i = 0; i < dates.Count; ++i)
            times_[i] = timeFromReference(dates[i]);
         initalise(quotes);
      }
      //@}


      private void initalise(List<Handle<Quote>> quotes)
      {
         Utils.QL_REQUIRE(times_.Count > 1, () => "at least two times required");
         Utils.QL_REQUIRE(times_[0] == 0.0, () => "First time must be 0, got " + times_[0]); // or date=asof
         Utils.QL_REQUIRE(times_.Count == quotes.Count, () => "size of time and quote vectors do not match");
         for (int i = 0; i < quotes.Count; ++i)
            quotes_.Add(quotes[i]);// (boost::make_shared<LogQuote>(quotes[i]));
         for (int i = 0; i < times_.Count - 1; ++i)
            timeDiffs_.Add(times_[i + 1] - times_[i]);
      }

      //! \name TermStructure interface
      //@{
      public override Date maxDate() { return Date.maxDate(); } // flat fwd extrapolation
                                                                //@}

      protected override double discountImpl(double t)
      {
         // List<double>::_iterator it = std::upper_bound(times_.begin(), times_.end(), t);
         int it = times_.IndexOf(t);

         int i = System.Math.Min(it, times_.Count - 1);
         double weight = (times_[i] - t) / timeDiffs_[i - 1];
         // this handles extrapolation (t > times.back()) as well
         double value = (1.0 - weight) * quotes_[i].value() + weight * quotes_[i - 1].value();
         return System.Math.Exp(value);
      }


   }
}
