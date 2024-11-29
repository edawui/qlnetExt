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

/*! \file subperiodsswap.hpp
    \brief Single currency sub periods swap instrument

    \ingroup instruments
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class SubPeriodsSwap : Swap
   {
      double nominal_;
      bool isPayer_;

      Schedule fixedSchedule_;
      double fixedRate_;
      DayCounter fixedDayCount_;

      Schedule floatSchedule_;
      IborIndex floatIndex_;
      DayCounter floatDayCount_;
      Period floatPayTenor_;
      SubPeriodsCoupon.Type type_;



      public SubPeriodsSwap(Date effectiveDate, double nominal, Period swapTenor, bool isPayer,
                                 Period fixedTenor, double fixedRate, Calendar fixedCalendar,
                                 DayCounter fixedDayCount, BusinessDayConvention fixedConvention,
                                 Period floatPayTenor, IborIndex iborIndex,
                                 DayCounter floatingDayCount, DateGeneration.Rule rule,
                                 SubPeriodsCoupon.Type type)

      : base(2)
      {
         nominal_ = nominal; isPayer_ = isPayer; fixedRate_ = fixedRate; fixedDayCount_ = fixedDayCount;
         floatIndex_ = iborIndex; floatDayCount_ = floatingDayCount; floatPayTenor_ = floatPayTenor; type_ = type;


         Date terminationDate = effectiveDate + swapTenor;

         // Fixed leg
         Schedule fixedSchedule = new MakeSchedule()
                                      .from(effectiveDate)
                                      .to(terminationDate)
                                      .withTenor(fixedTenor)
                                      .withCalendar(fixedCalendar)
                                      .withConvention(fixedConvention)
                                      .withTerminationDateConvention(fixedConvention)
                                      .withRule(rule).value();

         legs_[0] = ((FixedRateLeg)(new FixedRateLeg(fixedSchedule)
                        .withNotionals(nominal_)))
                        .withCouponRates(fixedRate_, fixedDayCount_)
                        .withPaymentAdjustment(fixedConvention);

         // Sub Periods Leg, schedule is the PAY schedule
         BusinessDayConvention floatPmtConvention = iborIndex.businessDayConvention();
         Calendar floatPmtCalendar = iborIndex.fixingCalendar();
         Schedule floatSchedule = new MakeSchedule()
                                      .from(effectiveDate)
                                      .to(terminationDate)
                                      .withTenor(floatPayTenor)
                                      .withCalendar(floatPmtCalendar)
                                      .withConvention(floatPmtConvention)
                                      .withTerminationDateConvention(floatPmtConvention)
                                      .withRule(rule).value();

         legs_[1] = new SubPeriodsLeg(floatSchedule, floatIndex_)
                        .withNotional(nominal_)
                        .withPaymentAdjustment(floatPmtConvention)
                        .withPaymentDayCounter(floatDayCount_)
                        .withPaymentCalendar(floatPmtCalendar)
                        .includeSpread(false)
                        .withType(type_).value();

         // legs_[0] is fixed
         payer_[0] = isPayer_ ? -1.0 : +1.0;
         payer_[1] = isPayer_ ? +1.0 : -1.0;

         // Register this instrument with its coupons
         // Leg::const_iterator it;
         for (int it = 0; it < legs_[0].Count; ++it)//it = legs_[0].begin(); it != legs_[0].end(); ++it)
            legs_[0][it].registerWith(update); // registerWith(*it);
         for (int it = 0; it < legs_[1].Count; ++it)//it = legs_[1].begin(); it != legs_[1].end(); ++it)
            legs_[1][it].registerWith(update); //registerWith(*it);
      }

      public double fairRate()
      {
         //static Spread basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         calculate();
         Utils.QL_REQUIRE(legBPS_[0] != double.NaN, () => "result not available");
         return fixedRate_ - NPV_.Value / (legBPS_[0].Value / basisPoint);
      }

      public double nominal() { return nominal_; }

     public bool isPayer() { return isPayer_; }

      public Schedule fixedSchedule() { return fixedSchedule_; }

      public double fixedRate() { return fixedRate_; }

      public List<CashFlow> fixedLeg() { return legs_[0]; }

      public double fixedLegBPS() { return legBPS(0).Value; }

      public double fixedLegNPV() { return legNPV(0).Value; }

      public Schedule floatSchedule() { return floatSchedule_; }

      public IborIndex floatIndex() { return floatIndex_; }

      public SubPeriodsCoupon.Type type() { return type_; }

      public Period floatPayTenor() { return floatPayTenor_; }

      public List<CashFlow> floatLeg() { return legs_[1]; }

      public double floatLegBPS() { return legBPS(1).Value; }

      public double floatLegNPV() { return legNPV(1).Value; }
   }


}
