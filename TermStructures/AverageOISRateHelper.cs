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
   public class AverageOISRateHelper : RelativeDateRateHelper
   {


      protected AverageOIS averageOIS_;
      // Swap
      protected Period spotLagTenor_;
      protected Period swapTenor_;
      // Fixed leg
      protected Period fixedTenor_;
      protected DayCounter fixedDayCounter_;
      protected Calendar fixedCalendar_;
      protected BusinessDayConvention fixedConvention_;
      protected BusinessDayConvention fixedPaymentAdjustment_;
      // ON leg
      protected OvernightIndex overnightIndex_;
      protected Period onTenor_;
      protected Handle<Quote> onSpread_;
      protected int rateCutoff_;
      // Curves
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_;
      protected Handle<YieldTermStructure> discountHandle_;
      protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;


      AverageOISRateHelper(Handle<Quote> fixedRate, Period spotLagTenor,
                                            Period swapTenor,
                                            // Fixed leg
                                            Period fixedTenor, DayCounter fixedDayCounter,
                                            Calendar fixedCalendar, BusinessDayConvention fixedConvention,
                                           BusinessDayConvention fixedPaymentAdjustment,
                                            // ON leg
                                            OvernightIndex overnightIndex,
                                            Period onTenor, Handle<Quote> onSpread, int rateCutoff,
                                            // Exogenous discount curve
                                            Handle<YieldTermStructure> discountCurve) : base(fixedRate)
      {
         spotLagTenor_ = spotLagTenor; swapTenor_ = swapTenor; fixedTenor_ = fixedTenor;
         fixedDayCounter_ = fixedDayCounter; fixedCalendar_ = fixedCalendar; fixedConvention_ = fixedConvention;
         fixedPaymentAdjustment_ = fixedPaymentAdjustment; overnightIndex_ = overnightIndex; onTenor_ = onTenor;
         onSpread_ = onSpread; rateCutoff_ = rateCutoff; discountHandle_ = discountCurve;


         bool onIndexHasCurve = !overnightIndex_.forwardingTermStructure().empty();
         bool haveDiscountCurve = !discountHandle_.empty();
         Utils.QL_REQUIRE(!(onIndexHasCurve & haveDiscountCurve), () => "Have both curves nothing to solve for.");

         if (!onIndexHasCurve)
         {
            IborIndex clonedIborIndex = overnightIndex_.clone(termStructureHandle_);
            overnightIndex_ = (OvernightIndex)(clonedIborIndex);
            overnightIndex_.unregisterWith(update);//termStructureHandle_);
         }

         overnightIndex_.registerWith(update);
         onSpread_.registerWith(update);
         discountHandle_.registerWith(update);

         //registerWith(overnightIndex_);
         //registerWith(onSpread_);
         //registerWith(discountHandle_);

         initializeDates();
      }

      protected override void initializeDates()
      {

         averageOIS_ = new
             MakeAverageOIS(swapTenor_, overnightIndex_, onTenor_, 0.0, fixedTenor_, fixedDayCounter_, spotLagTenor_, new Period(0, TimeUnit.Days))
                 .withFixedCalendar(fixedCalendar_)
                 .withFixedConvention(fixedConvention_)
                 .withFixedTerminationDateConvention(fixedConvention_)
                 .withFixedPaymentAdjustment(fixedPaymentAdjustment_)
                 .withRateCutoff(rateCutoff_)
                 .withDiscountingTermStructure(discountRelinkableHandle_)
                 .GetAverageOIS();

         earliestDate_ = averageOIS_.startDate();
         latestDate_ = averageOIS_.maturityDate();
      }

      public override double impliedQuote()
      {

         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         averageOIS_.recalculate();

         // Calculate the fair fixed rate after accounting for the
         // spread in the spread quote. Recall, the spread quote was
         // intentionally not added to instrument averageOIS_.
         //static  double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         double onLegNPV = averageOIS_.overnightLegNPV();
         double ondouble = onSpread_.empty() ? 0.0 : onSpread_.currentLink().value();
         double spreadNPV = averageOIS_.overnightLegBPS() * ondouble / basisPoint;
         double onLegNPVwithdouble = onLegNPV + spreadNPV;
         double result = -onLegNPVwithdouble / (averageOIS_.fixedLegBPS() / basisPoint);
         return result;
      }

      public override void setTermStructure(YieldTermStructure t)
      {

         bool observer = false;
         //YieldTermStructure temp(t, no_deletion); //todo Debug
         termStructureHandle_.linkTo(t, observer); //(temp,observer);

         if (discountHandle_.empty())
            discountRelinkableHandle_.linkTo(t, observer);
         else
            discountRelinkableHandle_.linkTo(discountHandle_, observer);

         base.setTermStructure(t);
      }

      public double onSpread()
      {
         return onSpread_.empty() ? 0.0 : onSpread_.currentLink().value();
      }

      public AverageOIS averageOIS() { return averageOIS_; }

      public void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");

         //      Visitor<AverageOISRateHelper>* v1 = dynamic_cast<Visitor<AverageOISRateHelper>*>(&v);
         //if (v1 != 0)
         //   v1.visit(*this);
         //else
         //   RateHelper::accept(v);
      }

   }
}
