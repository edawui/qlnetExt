
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

/*! \file averageonindexedcouponpricer.hpp
    \brief Pricer for average overnight indexed coupons

        \ingroup cashflows
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class AverageONIndexedCouponPricer: FloatingRateCouponPricer
   {


      Approximation approximationType_;
      double gearing_;
      double spread_;
      double accrualPeriod_;
      OvernightIndex overnightIndex_;

      AverageONIndexedCoupon coupon_;

      public enum Approximation { Takada, None };

      public AverageONIndexedCouponPricer(Approximation approxType = Approximation.Takada)
      {
         approximationType_ = approxType;
      }

      public override void initialize(FloatingRateCoupon coupon)
      {

         coupon_ = (AverageONIndexedCoupon)coupon;
         Utils.QL_REQUIRE(coupon_ != null, () => "AverageONIndexedCoupon required");

         overnightIndex_ = (OvernightIndex)coupon_.index();
         Utils.QL_REQUIRE(overnightIndex_ != null, () => "OvernightIndex required");

         gearing_ = coupon_.gearing();
         spread_ = coupon_.spread();
         accrualPeriod_ = coupon_.accrualPeriod();
      }

      public override double swapletRate()
      {

         List<Date> fixingDates = coupon_.fixingDates();
         List<double> accrualFractions = coupon_.dt();
         int numPeriods = accrualFractions.Count;
         double accumulateddouble = 0;

         if (approximationType_ == Approximation.Takada)
         {
            int i = 0;
            Date valuationDate = Settings.evaluationDate();
            // Deal with past fixings.
            while (i < numPeriods && fixingDates[i] < valuationDate)
            {
               double pastFixing = overnightIndex_.fixing(fixingDates[i]);
               accumulateddouble += pastFixing * accrualFractions[i];
               ++i;
            }
            // Use valuation date's fixing also if available.
            if (i < numPeriods && fixingDates[i] == valuationDate)
            {
               double valuationDateFixing = QLNet.IndexManager.instance().getHistory(overnightIndex_.name()).value()[valuationDate].Value;
               if (valuationDateFixing != double.NaN)
               {
                  accumulateddouble += valuationDateFixing * accrualFractions[i];
                  ++i;
               }
            }
            // Use Takada approximation (2011) for forecasting.
            if (i < numPeriods)
            {
               Handle<YieldTermStructure> projectionCurve = overnightIndex_.forwardingTermStructure();
               Utils.QL_REQUIRE(!projectionCurve.empty(), () => "Null term structure set to this instance of "
                                                        + overnightIndex_.name());

               Date startForecast = coupon_.valueDates()[i];
               Date endForecast = coupon_.valueDates()[numPeriods];
               double startDiscount = projectionCurve.currentLink().discount(startForecast);
               double endDiscount = projectionCurve.currentLink().discount(endForecast);
               accumulateddouble += System.Math.Log(startDiscount / endDiscount);
            }
         }
         else if (approximationType_ == Approximation.None)
         {
            List<double> fixings = coupon_.indexFixings();
            for (int i = 0; i < numPeriods; ++i)
            {
               accumulateddouble += fixings[i] * accrualFractions[i];
            }
         }
         else
         {
            Utils.QL_FAIL("Invalid Approximation for AverageONIndexedCouponPricer");
         }
         // Return factor * rate + spread
         double rate = gearing_ * accumulateddouble / accrualPeriod_ + spread_;
         return rate;
      }


      public override double swapletPrice()
      {
         Utils.QL_FAIL("swapletPrice not available");
         return double.NaN;
      }
      public override double capletPrice(double x)
      {
         Utils.QL_FAIL("capletPrice not available");
         return double.NaN;
      }
      public override double capletRate(double x) { Utils.QL_FAIL("capletdouble not available"); return double.NaN; }
      public override double floorletPrice(double x) { Utils.QL_FAIL("floorletPrice not available"); return double.NaN; }
      public override double floorletRate(double x) { Utils.QL_FAIL("floorletdouble not available"); return double.NaN; }





   }
}
