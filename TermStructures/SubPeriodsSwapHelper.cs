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
    \brief Single currency sub periods swap helper
    \ingroup termstructures
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt.TermStructures
{
   //! Rate helper for bootstrapping using Sub Periods Swaps
   /*! \ingroup termstructures
   */

   public class SubPeriodsSwapHelper : RelativeDateRateHelper
   {


      SubPeriodsSwap swap_;
      IborIndex iborIndex_;
      Period swapTenor_;
      Period fixedTenor_;
      Calendar fixedCalendar_;
      DayCounter fixedDayCount_;
      BusinessDayConvention fixedConvention_;
      Period floatPayTenor_;
      DayCounter floatDayCount_;
      SubPeriodsCoupon.Type type_;

      RelinkableHandle<YieldTermStructure> termStructureHandle_;
      Handle<YieldTermStructure> discountHandle_;
      RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;


      void no_deletion(YieldTermStructure y) { }


      public SubPeriodsSwapHelper(Handle<Quote> spread, Period swapTenor, Period fixedTenor,
                                                Calendar fixedCalendar, DayCounter fixedDayCount,
                                               BusinessDayConvention fixedConvention, Period floatPayTenor,
                                               IborIndex iborIndex,
                                                DayCounter floatDayCount,
                                                Handle<YieldTermStructure> discountingCurve,
                                               SubPeriodsCoupon.Type type)
        : base(spread)
      {
         iborIndex_ = iborIndex; swapTenor_ = swapTenor; fixedTenor_ = fixedTenor;
         fixedCalendar_ = fixedCalendar; fixedDayCount_ = fixedDayCount; fixedConvention_ = fixedConvention;
         floatPayTenor_ = floatPayTenor; floatDayCount_ = floatDayCount; type_ = type; discountHandle_ = discountingCurve;


         iborIndex_ = iborIndex_.clone(termStructureHandle_);
         iborIndex_.unregisterWith(update); //termStructureHandle_);

         iborIndex_.registerWith(update);
         spread.registerWith(update);
         discountHandle_.registerWith(update);

         initializeDates();
      }

      protected override void initializeDates()
      {

         // build swap
         Date valuationDate = Settings.evaluationDate();
         Calendar spotCalendar = iborIndex_.fixingCalendar();
         int spotDays = iborIndex_.fixingDays();
         // move val date forward in case it is a holiday
         valuationDate = spotCalendar.adjust(valuationDate);
         Date effectiveDate = spotCalendar.advance(valuationDate, new Period(spotDays, TimeUnit.Days));

         swap_ = new SubPeriodsSwap(
             effectiveDate, 1.0, swapTenor_, true, fixedTenor_, 0.0, fixedCalendar_, fixedDayCount_, fixedConvention_,
             floatPayTenor_, iborIndex_, floatDayCount_, DateGeneration.Rule.Backward, type_);

         IPricingEngine engine = new DiscountingSwapEngine(discountRelinkableHandle_);
         swap_.setPricingEngine(engine);

         // set earliest and latest
         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();

         FloatingRateCoupon lastFloating = (FloatingRateCoupon)(swap_.floatLeg().Last());
         //# ifdef QL_USE_INDEXED_COUPON
         //         /* May need to adjust latestDate_ if you are projecting libor based
         //         on tenor length rather than from accrual date to accrual date. */
         //         Date fixingValueDate = iborIndex_.valueDate(lastFloating.fixingDate());
         //         Date endValueDate = iborIndex_.maturityDate(fixingValueDate);
         //         latestDate_ = Date.Max(latestDate_, endValueDate);
         //#else
         //         /* Subperiods coupons do not have a par approximation either... */
         //         if (((SubPeriodsCoupon)(lastFloating)) != null)
         //         {
         //            Date fixingValueDate2 = iborIndex_.valueDate(lastFloating.fixingDate());
         //            Date endValueDate2 = iborIndex_.maturityDate(fixingValueDate2);
         //            latestDate_ = Date.Max(latestDate_, endValueDate2);
         //         }
         //#endif
         //todo
         if (((SubPeriodsCoupon)(lastFloating)) != null)
         {
            Date fixingValueDate = iborIndex_.valueDate(lastFloating.fixingDate());
            Date endValueDate = iborIndex_.maturityDate(fixingValueDate);
            latestDate_ = Date.Max(latestDate_, endValueDate);
         }
      }

      public override void setTermStructure(YieldTermStructure t)
      {

         bool observer = false;

         YieldTermStructure temp = t;// (t, no_deletion);
         termStructureHandle_.linkTo(temp, observer);

         if (discountHandle_.empty())
            discountRelinkableHandle_.linkTo(temp, observer);
         else
            discountRelinkableHandle_.linkTo(discountHandle_, observer);

         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "Termstructure not set");
         swap_.recalculate();
         return swap_.fairRate();
      }

      public void accept(IAcyclicVisitor v)
      {
         if (v == null)
         {
            v.visit(this);
         }
         else
         {
            //base.accept(v);
         }
      }

   }
}
