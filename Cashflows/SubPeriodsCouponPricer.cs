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
   public class SubPeriodsCouponPricer : FloatingRateCouponPricer
   {


      double gearing_;
      double spread_;
      double accrualPeriod_;
      InterestRateIndex index_;
      SubPeriodsCoupon.Type type_;
      bool includeSpread_;

      SubPeriodsCoupon coupon_;

      public SubPeriodsCouponPricer() { }

      public override void initialize(FloatingRateCoupon coupon)
      {

         coupon_ = (SubPeriodsCoupon)coupon;
         Utils.QL_REQUIRE(coupon_ != null, () => "SubPeriodsCoupon required");

         index_ = (InterestRateIndex)coupon_.index();
         Utils.QL_REQUIRE(index_ != null, () => "InterestRateIndex required");

         gearing_ = coupon_.gearing();
         spread_ = coupon_.spread();
         accrualPeriod_ = coupon_.accrualPeriod();
         type_ = coupon_.type();
         includeSpread_ = coupon_.includeSpread();
      }

      public override double swapletRate()
      {

         List<double> accrualFractions = coupon_.accrualFractions();
         int numPeriods = accrualFractions.Count;
         double incSpread = includeSpread_ ? spread_ : 0.0;
         double excSpread = includeSpread_ ? 0.0 : spread_;
         double accumulatedRate;
         double rate = 0; ;

         List<double> fixings = coupon_.indexFixings();
         if (type_ == SubPeriodsCoupon.Type.Averaging)
         {
            accumulatedRate = 0.0;
            for (int i = 0; i < numPeriods; ++i)
            {
               accumulatedRate += (fixings[i] + incSpread) * accrualFractions[i];
            }
            rate = gearing_ * accumulatedRate / accrualPeriod_ + excSpread;
         }
         else if (type_ == SubPeriodsCoupon.Type.Compounding)
         {
            accumulatedRate = 1.0;
            for (int i = 0; i < numPeriods; ++i)
            {
               accumulatedRate *= (1.0 + (fixings[i] + incSpread) * accrualFractions[i]);
            }
            rate = gearing_ * (accumulatedRate - 1.0) / accrualPeriod_ + excSpread;
         }
         else
         {
            Utils.QL_FAIL("Invalid sub-period coupon type");
         }

         return rate;
      }



      public override double swapletPrice()  { Utils.QL_FAIL("swapletPrice not available" ); return 0.0;      }
      public override double capletPrice(double rate)  { Utils.QL_FAIL("capletPrice not available"); return 0.0;      }
      public override double capletRate(double rate )  { Utils.QL_FAIL("capletdouble not available"); return 0.0; }
      public override double floorletPrice(double rate )  { Utils.QL_FAIL("floorletPrice not available"); return 0.0; }
      public override double floorletRate(double rate )  { Utils.QL_FAIL("floorletdouble not available"); return 0.0; }


   }

}

