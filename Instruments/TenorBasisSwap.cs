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

/*! \file tenorbasisswap.hpp
    \brief Single currency tenor basis swap instrument

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






   public class FairShortSpreadHelper : ISolver1d
   {

      IPricingEngine engine_;
      double longLegNPV_;
      Swap.Results results_;
      Swap.Arguments arguments_;
      int shortNo_;
      List<CashFlow> shortLeg_;


      public FairShortSpreadHelper(TenorBasisSwap swap, Handle<YieldTermStructure> discountCurve, double longLegNPV)
      {
         longLegNPV_ = longLegNPV;


         engine_ = new DiscountingSwapEngine(discountCurve);
         arguments_ = (Swap.Arguments)engine_.getArguments();
         swap.setupArguments(arguments_);
         shortNo_ = swap.payLongIndex()?1:0;
         shortLeg_ = arguments_.legs[shortNo_];
         results_ = (Swap.Results)engine_.getResults();
      }

      public override double value(double shortSpread)
      {
         // Change the spread on the leg and recalculate
         //Leg::_iterator it;
         for (int it = 0; it < shortLeg_.Count; ++it) //(it = shortLeg_.begin(); it != shortLeg_.end(); ++it) {
         {
            SubPeriodsCoupon c= (SubPeriodsCoupon)shortLeg_[it];
            //c.spread() = shortSpread;
            ((SubPeriodsCoupon)shortLeg_[it]).Spread= shortSpread;
            //todo debug
         }
         engine_.calculate();
         return results_.legNPV[shortNo_].Value + longLegNPV_;
      }


   }


   public class TenorBasisSwap : Swap
   {



      //! %Results from tenor basis swap calculation
      public new class Results : Swap.Results
      {
         public double fairLongSpread;
         public double fairShortSpread;
         public override void reset()
         {
            base.reset();
            fairLongSpread = double.NaN;
            fairShortSpread = double.NaN;
         }
      }

      public class Engine : GenericEngine<Arguments, Results>
      { }

      double nominal_;
      bool payLongIndex_;

      Schedule longSchedule_;
      IborIndex longIndex_;
      double longSpread_;

      Schedule shortSchedule_;
      IborIndex shortIndex_;
      double shortSpread_;
      Period shortPayTenor_;
      bool includeSpread_;
      SubPeriodsCoupon.Type type_;
      int shortNo_, longNo_;

      double fairLongSpread_;
      double fairShortSpread_;


      public TenorBasisSwap(Date effectiveDate, double nominal, Period swapTenor, bool payLongIndex,
                                IborIndex longIndex, double longSpread,
                                IborIndex shortIndex, double shortSpread,
                                Period shortPayTenor, DateGeneration.Rule rule, bool includeSpread,
                                SubPeriodsCoupon.Type type)
     : base(2)
      {
         nominal_ = nominal; payLongIndex_ = payLongIndex; longIndex_ = longIndex; longSpread_ = longSpread;
         shortIndex_ = shortIndex; shortSpread_ = shortSpread; shortPayTenor_ = shortPayTenor; includeSpread_ = includeSpread;
         type_ = type;


         // Checks
         Period longTenor = longIndex_.tenor();
         Utils.QL_REQUIRE(shortPayTenor_ >= shortIndex_.tenor(), () => "Expected short payment tenor to exceed/equal shortIndex tenor");
         Utils.QL_REQUIRE(shortPayTenor_ <= longTenor, () => "Expected short payment tenor to be at most longSchedule tenor");

         // Create the default long and short schedules
         Date terminationDate = effectiveDate + swapTenor;

         longSchedule_ = new MakeSchedule()
                             .from(effectiveDate)
                             .to(terminationDate)
                             .withTenor(longTenor)
                             .withCalendar(longIndex_.fixingCalendar())
                             .withConvention(longIndex_.businessDayConvention())
                             .withTerminationDateConvention(longIndex_.businessDayConvention())
                             .withRule(rule)
                             .endOfMonth(longIndex_.endOfMonth())
                             .value();

         if (shortPayTenor_ == shortIndex_.tenor())
         {
            shortSchedule_ = new MakeSchedule()
                                 .from(effectiveDate)
                                 .to(terminationDate)
                                 .withTenor(shortPayTenor_)
                                 .withCalendar(shortIndex_.fixingCalendar())
                                 .withConvention(shortIndex_.businessDayConvention())
                                 .withTerminationDateConvention(shortIndex_.businessDayConvention())
                                 .withRule(rule)
                                 .endOfMonth(shortIndex_.endOfMonth())
                                 .value();
         }
         else
         {
            /* Where the payment tenor is longer, the SubPeriodsLeg
               will handle the adjustments. We just need to give the
               anchor dates. */
            shortSchedule_ = new MakeSchedule()
                                 .from(effectiveDate)
                                 .to(terminationDate)
                                 .withTenor(shortPayTenor_)
                                 .withCalendar(new NullCalendar())
                                 .withConvention(BusinessDayConvention.Unadjusted)
                                 .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                                 .withRule(rule)
                                 .endOfMonth(shortIndex_.endOfMonth())
                                 .value();
         }

         // Create legs
         initializeLegs();
      }

      public TenorBasisSwap(double nominal, bool payLongIndex, Schedule longSchedule,
                               IborIndex longIndex, double longSpread,
                               Schedule shortSchedule, IborIndex shortIndex,
                              double shortSpread, bool includeSpread, SubPeriodsCoupon.Type type)
    : base(2)
      {
         nominal_ = nominal; payLongIndex_ = payLongIndex; longSchedule_ = longSchedule; longIndex_ = longIndex;
         longSpread_ = longSpread; shortSchedule_ = shortSchedule; shortIndex_ = shortIndex; shortSpread_ = shortSpread;
         includeSpread_ = includeSpread; type_ = type;

         // Checks
         Period longTenor = longSchedule_.tenor();
         Utils.QL_REQUIRE(longTenor == longIndex_.tenor(), () => "Expected longSchedule tenor to equal longIndex tenor");
         shortPayTenor_ = shortSchedule_.tenor();
         Utils.QL_REQUIRE(shortPayTenor_ >= shortIndex_.tenor(), () => "Expected shortSchedule tenor to exceed/equal shortIndex tenor");
         Utils.QL_REQUIRE(shortPayTenor_ <= longTenor, () => "Expected shortSchedule tenor to be at most longSchedule tenor");

         // Create legs
         initializeLegs();
      }




      public void initializeLegs()
      {

         // Long leg
         BusinessDayConvention longPmtConvention = longIndex_.businessDayConvention();
         DayCounter longDayCounter = longIndex_.dayCounter();
         List<CashFlow> longLeg =
                           ((IborLeg)(
                           ((IborLeg)(
                           new IborLeg(longSchedule_, longIndex_)
                           .withNotionals(nominal_)))
                           .withSpreads(longSpread_)
                           .withPaymentAdjustment(longPmtConvention)))
                           .withPaymentDayCounter(longDayCounter).value();


         // Short leg
         List<CashFlow> shortLeg = new List<CashFlow>();
         BusinessDayConvention shortPmtConvention = shortIndex_.businessDayConvention();
         DayCounter shortDayCounter = shortIndex_.dayCounter();
         Calendar shortPmtCalendar = shortIndex_.fixingCalendar();
         if (shortPayTenor_ == shortIndex_.tenor())
         {
            shortLeg =
                           ((IborLeg)(
                            ((IborLeg)(
                           new IborLeg(shortSchedule_, shortIndex_)
                           .withNotionals(nominal_)))
                           .withSpreads(shortSpread_)
                           .withPaymentAdjustment(shortPmtConvention)))
                           .withPaymentDayCounter(shortDayCounter).value();
         }
         else
         {
            shortLeg = new SubPeriodsLeg(shortSchedule_, shortIndex_)
                           .withNotional(nominal_)
                           .withSpread(shortSpread_)
                           .withPaymentAdjustment(shortPmtConvention)
                           .withPaymentDayCounter(shortDayCounter)
                           .withPaymentCalendar(shortPmtCalendar)
                           .includeSpread(includeSpread_)
                           .withType(type_).value();
         }

         // Pay (Rec) leg is legs_[0] (legs_[1])
         payer_[0] = -1.0;
         payer_[1] = +1.0;

         longNo_ = (!payLongIndex_) ? 1 : 0;
         shortNo_ = (payLongIndex_) ? 1 : 0;
         legs_[longNo_] = longLeg;
         legs_[shortNo_] = shortLeg;

         // Register this instrument with its coupons
         //Leg::const_iterator it;
         for (int it = 0; it < legs_[0].Count; ++it)//it = legs_[0].begin(); it != legs_[0].end(); ++it)
            legs_[0][it].registerWith(update);//registerWith(*it);
         for (int it = 0; it < legs_[1].Count; ++it)//it = legs_[1].begin(); it != legs_[1].end(); ++it)
            legs_[1][it].registerWith(update);//registerWith(*it);
      }



      public double longLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[longNo_] != double.NaN, () => "Long leg BPS not available");
         return legBPS_[longNo_].Value;
      }

      public double longLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[longNo_] != double.NaN, () => "Long leg NPV not available");
         return legNPV_[longNo_].Value;
      }

      public double fairLongLegSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairLongSpread_ != double.NaN, () => "Long leg fair spread not available");
         return fairLongSpread_;
      }

      public double shortLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[shortNo_] != double.NaN, () => "Short leg BPS not available");
         return legBPS_[shortNo_].Value;
      }

      public double shortLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[shortNo_] != double.NaN, () => "Short leg NPV not available");
         return legNPV_[shortNo_].Value;
      }

      public double fairShortLegSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairShortSpread_ != double.NaN, () => "Short leg fair spread not available");
         return fairShortSpread_;
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         fairLongSpread_ = double.NaN;
         fairShortSpread_ = double.NaN;
      }

      public override void fetchResults(IPricingEngineResults r)
      {

         //static double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         base.fetchResults(r);

         Results results = (Results)(r);

         if (results != null)
         {
            fairLongSpread_ = results.fairLongSpread;
            fairShortSpread_ = results.fairShortSpread;
         }
         else
         {
            fairLongSpread_ = double.NaN;
            fairShortSpread_ = double.NaN;
         }

         // Long fair spread should be fine - no averaging or compounding
         if (fairLongSpread_ == double.NaN)
         {
            if (legBPS_[longNo_] != double.NaN)
            {
               fairLongSpread_ = longSpread_ - NPV_.Value / (legBPS_[longNo_].Value / basisPoint);
            }
         }

         /* Short fair spread calculation ok if no averaging/compounding OR
            if there is averaging/compounding and the spread is added after */
         if (fairShortSpread_ == double.NaN)
         {
            if (shortPayTenor_ == shortIndex_.tenor() || !includeSpread_)
            {
               if (legBPS_[shortNo_] != double.NaN)
               {
                  fairShortSpread_ = shortSpread_ - NPV_.Value / (legBPS_[shortNo_].Value / basisPoint);
               }
            }
            else
            {
               // Need the discount curve
               Handle<YieldTermStructure> discountCurve;
               DiscountingSwapEngine engine = (DiscountingSwapEngine)(engine_);
               if (engine != null)
               {
                  discountCurve = engine.discountCurve();
                  // Calculate a guess
                  double guess = 0.0;
                  if (legBPS_[shortNo_] != double.NaN)
                  {
                     guess = shortSpread_ - NPV_.Value / (legBPS_[shortNo_].Value / basisPoint);
                  }
                  // Attempt to solve for fair spread
                  double step = 1e-4;
                  double accuracy = 1e-8;
                  FairShortSpreadHelper f = new FairShortSpreadHelper(this, discountCurve, legNPV_[longNo_].Value);
                  Brent solver = new Brent();
                  fairShortSpread_ = solver.solve(f, accuracy, guess, step);
               }
            }
         }
      }

      // Inline definitions
      public double nominal() { return nominal_; }

      public bool payLongIndex() { return payLongIndex_; }

      public Schedule longSchedule() { return longSchedule_; }

      public IborIndex longIndex() { return longIndex_; }

      public double longSpread() { return longSpread_; }

      public List<CashFlow> longLeg() { return payLongIndex_ ? legs_[0] : legs_[1]; }

      public Schedule shortSchedule() { return shortSchedule_; }

      public IborIndex shortIndex() { return shortIndex_; }

      public double shortSpread() { return shortSpread_; }

      public Period shortPayTenor() { return shortPayTenor_; }

      public bool includeSpread() { return includeSpread_; }

      public SubPeriodsCoupon.Type type() { return type_; }

      public List<CashFlow> shortLeg() { return payLongIndex_ ? legs_[1] : legs_[0]; }


   }
}
