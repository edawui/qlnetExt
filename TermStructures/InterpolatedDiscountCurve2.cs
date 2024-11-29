
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

/*! \file interpolateddiscountcurve2.hpp
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
   //! InterpolatedDiscountCurve2 as in QuantLib, but with floating discount quotes and floating reference date
   /*! InterpolatedDiscountCurve2 as in QuantLib, but with
       floating discount quotes and floating reference date,
       reference date is always the global evaluation date,
       i.e. settlement days are zero and calendar is NullCalendar()

           \ingroup termstructures
   */

   public class InterpolatedDiscountCurve2 : YieldTermStructure
   {



      List<double> times_;
      List<Handle<Quote>> quotes_;
      List<double> data_;
      Date today_;
      Interpolation interpolation_;



      public InterpolatedDiscountCurve2(List<double> times, List<Handle<Quote>> quotes, DayCounter dc)
        : base(dc)
      {
         times_ = times; quotes_ = quotes;
         data_ = Enumerable.Repeat<double>(1.0, times_.Count).ToList();
         today_ = Settings.evaluationDate();

         for (int i = 0; i < quotes.Count; ++i)
         {
            Utils.QL_REQUIRE(times_.Count > 1, () => "at least two times required");
            Utils.QL_REQUIRE(times_.Count == quotes.Count, () => "size of time and quote vectors do not match");
            Utils.QL_REQUIRE(times_[0] == 0.0, () => "First time must be 0, got " + times_[0]);
            Utils.QL_REQUIRE(!quotes[i].empty(), () => "quote at index " + i + " is empty");
            quotes_[i].registerWith(update);
         }
         interpolation_ = new LogLinearInterpolation(times_, times_.Count, data_);
         Settings.evaluationDate();
      }
      //! date based ructor

      public InterpolatedDiscountCurve2(List<Date> dates, List<Handle<Quote>> quotes, DayCounter dc)
        : base(dc)
      {
         times_ = Enumerable.Repeat<double>(0.0, dates.Count).ToList();
         quotes_ = quotes;
         data_ = Enumerable.Repeat<double>(1.0, dates.Count).ToList();
         today_ = Settings.evaluationDate();


         for (int i = 0; i < dates.Count; ++i)
            times_[i] = dc.yearFraction(today_, dates[i]);
         for (int i = 0; i < quotes.Count; ++i)
         {
            Utils.QL_REQUIRE(times_.Count > 1, () => "at least two times required");
            Utils.QL_REQUIRE(times_.Count == quotes.Count, () => "size of time and quote vectors do not match");
            Utils.QL_REQUIRE(times_[0] == 0.0, () => "First time must be 0, got " + times_[0]);
            Utils.QL_REQUIRE(!quotes[i].empty(), () => "quote at index " + i + " is empty");
            quotes_[i].registerWith(update);
         }
         interpolation_ = new LogLinearInterpolation(times_, times_.Count, data_);
         Settings.evaluationDate();
      }

      //@}

      public override Date maxDate() { return Date.maxDate(); }

      public override void update()
      {
         base.update();

      }

      public override Date referenceDate()
      {
         calculate();
         return today_;
      }

      public override Calendar calendar() { return new NullCalendar(); }
      public override int settlementDays() { return 0; }

      protected override void performCalculations()
      {
         today_ = Settings.evaluationDate();
         for (int i = 0; i < times_.Count; ++i)
         {
            data_[i] = quotes_[i].currentLink().value();
         }
         interpolation_.update();
      }

      protected override double discountImpl(double t)
      {
         calculate();
         if (t <= this.times_.Last())
            return interpolation_.value(t, true);
         // flat fwd extrapolation
         double tMax = this.times_.Last();
         double dMax = this.data_.Last();
         double instFwdMax = -interpolation_.derivative(tMax) / dMax;
         return dMax * System.Math.Exp(-instFwdMax * (t - tMax));
      }


   }
}
