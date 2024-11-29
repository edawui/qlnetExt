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

/*! \file basistwoswaphelper.hpp
    \brief Libor basis swap helper as two swaps
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
   public class BasisTwoSwapHelper : RelativeDateRateHelper
   {


      protected Period swapTenor_;
      protected Calendar calendar_;
      // Long tenor swap
      protected Frequency longFixedFrequency_;
      protected BusinessDayConvention longFixedConvention_;
      protected DayCounter longFixedDayCount_;
      protected IborIndex longIndex_;
      // Short tenor swap
      protected Frequency shortFixedFrequency_;
      protected BusinessDayConvention shortFixedConvention_;
      protected DayCounter shortFixedDayCount_;
      protected IborIndex shortIndex_;
      protected bool longMinusShort_;

      protected VanillaSwap longSwap_;
      protected VanillaSwap shortSwap_;

      protected RelinkableHandle<YieldTermStructure> termStructureHandle_;
      protected Handle<YieldTermStructure> discountHandle_;
      protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;





      public BasisTwoSwapHelper(Handle<Quote> spread, Period swapTenor, Calendar calendar,
                                          // Long tenor swap
                                          Frequency longFixedFrequency, BusinessDayConvention longFixedConvention,
                                           DayCounter longFixedDayCount,
                                           IborIndex longIndex,
                                          // Short tenor swap
                                          Frequency shortFixedFrequency, BusinessDayConvention shortFixedConvention,
                                           DayCounter shortFixedDayCount,
                                           IborIndex shortIndex, bool longMinusShort,
                                           // Discount curve
                                           Handle<YieldTermStructure> discountingCurve)
       : base(spread)
      {
         swapTenor_ = swapTenor; calendar_ = calendar;
         longFixedFrequency_ = longFixedFrequency; longFixedConvention_ = longFixedConvention;
         longFixedDayCount_ = longFixedDayCount; longIndex_ = longIndex; shortFixedFrequency_ = shortFixedFrequency;
         shortFixedConvention_ = shortFixedConvention; shortFixedDayCount_ = shortFixedDayCount; shortIndex_ = shortIndex;
         longMinusShort_ = longMinusShort; discountHandle_ = discountingCurve;


         Utils.QL_REQUIRE(longIndex_.tenor() >= shortIndex_.tenor(), () => "Tenor of longIndex should be at least tenor of shortIndex.");

         bool longIndexHasCurve = !longIndex_.forwardingTermStructure().empty();
         bool shortIndexHasCurve = !shortIndex_.forwardingTermStructure().empty();
         bool haveDiscountCurve = !discountHandle_.empty();
         Utils.QL_REQUIRE(!(longIndexHasCurve & shortIndexHasCurve & haveDiscountCurve), () => "Have all curves nothing to solve for.");

         if (longIndexHasCurve & !shortIndexHasCurve)
         {
            shortIndex_ = shortIndex_.clone(termStructureHandle_);
            shortIndex_.unregisterWith(update);//termStructureHandle_);
         }
         else if (!longIndexHasCurve & shortIndexHasCurve)
         {
            longIndex_ = longIndex_.clone(termStructureHandle_);
            longIndex_.unregisterWith(update);//termStructureHandle_);
         }
         else if (!longIndexHasCurve & !shortIndexHasCurve)
         {
            Utils.QL_FAIL("Need at least one of the indices to have a valid curve.");
         }

         longIndex_.registerWith(update);
         shortIndex_.registerWith(update);
         discountHandle_.registerWith(update);

         //registerWith(longIndex_);
         //registerWith(shortIndex_);
         //registerWith(discountHandle_);
         initializeDates();
      }

      protected override void initializeDates()
      {

         /* Important to use a fixed rate of 0.0 here to avoid the calculation
            of the atm swap rate in MakeVanillaSwap operator ...(). If it is
            Null, you get an exception because the discountRelinkableHandle_
            is initially empty. */
         longSwap_ = new MakeVanillaSwap(swapTenor_, longIndex_, 0.0)
                         .withDiscountingTermStructure(discountRelinkableHandle_)
                         .withFixedLegDayCount(longFixedDayCount_)
                         .withFixedLegTenor(new Period(longFixedFrequency_))
                         .withFixedLegConvention(longFixedConvention_)
                         .withFixedLegTerminationDateConvention(longFixedConvention_)
                         .withFixedLegCalendar(calendar_)
                         .withFloatingLegCalendar(calendar_);

         shortSwap_ = new MakeVanillaSwap(swapTenor_, shortIndex_, 0.0)
                          .withDiscountingTermStructure(discountRelinkableHandle_)
                          .withFixedLegDayCount(shortFixedDayCount_)
                          .withFixedLegTenor(new Period(shortFixedFrequency_))
                          .withFixedLegConvention(shortFixedConvention_)
                          .withFixedLegTerminationDateConvention(shortFixedConvention_)
                          .withFixedLegCalendar(calendar_)
                          .withFloatingLegCalendar(calendar_);

         earliestDate_ = Date.Min(longSwap_.startDate(), shortSwap_.startDate());
         latestDate_ = Date.Max(longSwap_.maturityDate(), shortSwap_.maturityDate());

         /* May need to adjust latestDate_ if you are projecting libor based
            on tenor length rather than from accrual date to accrual date. */
#if QL_USE_INDEXED_COUPON
      if (termStructureHandle_ == shortIndex_.forwardingTermStructure())
      {
         FloatingRateCoupon lastFloating =
            (FloatingRateCoupon)(shortSwap_.floatingLeg().Last());
         Date fixingValueDate = shortIndex_.valueDate(lastFloating.fixingDate());
         Date endValueDate = shortIndex_.maturityDate(fixingValueDate);
         latestDate_ = Date.Max(latestDate_, endValueDate);
      }
      if (termStructureHandle_ == longIndex_.forwardingTermStructure())
      {
         FloatingRateCoupon lastFloating =
             (FloatingRateCoupon)(longSwap_.floatingLeg().Last());
         Date fixingValueDate = longIndex_.valueDate(lastFloating.fixingDate());
         Date endValueDate = longIndex_.maturityDate(fixingValueDate);
         latestDate_ = Date.Max(latestDate_, endValueDate);
      }
#endif
      }

      public override void setTermStructure(YieldTermStructure t)
      {

         bool observer = false;

         //YieldTermStructure temp(t, no_deletion);
         termStructureHandle_.linkTo(t, observer);

         if (discountHandle_.empty())
            discountRelinkableHandle_.linkTo(t, observer);
         else
            discountRelinkableHandle_.linkTo(discountHandle_, observer);

         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "Termstructure not set");
         longSwap_.recalculate();
         shortSwap_.recalculate();
         if (longMinusShort_)
            return longSwap_.fairRate() - shortSwap_.fairRate();
         else
            return shortSwap_.fairRate() - longSwap_.fairRate();
      }

      public void accept(IAcyclicVisitor v)
      {
         //Visitor<BasisTwoSwapHelper>* v1 = dynamic_cast<Visitor<BasisTwoSwapHelper>*>(&v);
         //if (v1 != 0)
         //   v1.visit(*this);
         //else
         //   RateHelper::accept(v);

         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");

      }
      
      public VanillaSwap shortSwap() { return shortSwap_; }

      public VanillaSwap longSwap() { return longSwap_; }

   }
}
