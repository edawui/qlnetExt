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
   class OISRateHelper : RelativeDateRateHelper
   {


      protected int settlementDays_;
      protected Period swapTenor_;
      protected OvernightIndex overnightIndex_;
      protected DayCounter fixedDayCounter_;
      protected int paymentLag_;
      protected bool endOfMonth_;
      protected Frequency paymentFrequency_;
      protected BusinessDayConvention fixedConvention_;
      protected BusinessDayConvention paymentAdjustment_;
      protected DateGeneration.Rule rule_;

      protected OvernightIndexedSwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_;
      protected Handle<YieldTermStructure> discountHandle_;
      protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;

      OISRateHelper(int settlementDays, Period swapTenor, Handle<Quote> fixedRate,
                              OvernightIndex overnightIndex, DayCounter fixedDayCounter,
                             int paymentLag, bool endOfMonth, Frequency paymentFrequency,
                             BusinessDayConvention fixedConvention, BusinessDayConvention paymentAdjustment,
                             DateGeneration.Rule rule, Handle<YieldTermStructure> discountingCurve)
    : base(fixedRate)
      {
         settlementDays_ = settlementDays; swapTenor_ = swapTenor;
         overnightIndex_ = overnightIndex; fixedDayCounter_ = fixedDayCounter; paymentLag_ = paymentLag;
         endOfMonth_ = endOfMonth; paymentFrequency_ = paymentFrequency; fixedConvention_ = fixedConvention;
         paymentAdjustment_ = paymentAdjustment; rule_ = rule; discountHandle_ = discountingCurve;


         bool onIndexHasCurve = !overnightIndex_.forwardingTermStructure().empty();
         bool haveDiscountCurve = !discountHandle_.empty();
         Utils.QL_REQUIRE(!(onIndexHasCurve && haveDiscountCurve), () => "Have both curves nothing to solve for.");

         if (!onIndexHasCurve)
         {
            IborIndex clonedIborIndex = overnightIndex_.clone(termStructureHandle_);
            overnightIndex_ = (OvernightIndex)(clonedIborIndex);
            overnightIndex_.unregisterWith(update);// termStructureHandle_);
         }

         overnightIndex_.registerWith(update);
         discountHandle_.registerWith(update);
         initializeDates();
      }

      protected override void initializeDates()
      {

         swap_ = new MakeOIS(swapTenor_, overnightIndex_, 0.0)
                     .withSettlementDays(settlementDays_)
                     .withFixedLegDayCount(fixedDayCounter_)
                     .withEndOfMonth(endOfMonth_)
                     .withPaymentFrequency(paymentFrequency_)
                     .withRule(rule_)
                     // TODO: patch QL?
                     //.withFixedAccrualConvention(fixedConvention_)
                     //.withFixedPaymentConvention(paymentAdjustment_)
                     //.withPaymentLag(paymentLag_)
                     .withDiscountingTermStructure(discountRelinkableHandle_);

         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();

         // Latest Date may need to be updated due to payment lag.
         Date date;
         if (paymentLag_ > 0)
         {
            date = CashFlows.nextCashFlowDate(swap_.leg(0), false, latestDate_);
            date = Date.Max(date, CashFlows.nextCashFlowDate(swap_.leg(1), false, latestDate_));
            latestDate_ = Date.Max(date, latestDate_);
         }
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer
         // force recalculation when needed
         bool observer = false;

         YieldTermStructure temp = t;
         termStructureHandle_.linkTo(temp, observer);

         if (discountHandle_.empty())
            discountRelinkableHandle_.linkTo(temp, observer);
         else
            discountRelinkableHandle_.linkTo(discountHandle_, observer);

         base.setTermStructure(t);
      }

      public override double impliedQuote() {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // we didn't register as observers - force calculation
         swap_.recalculate();
         return swap_.fairRate().Value;
      }

      public void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");
      }

   }


public class DatedOISRateHelper:RelativeDateRateHelper
   {
      
   protected OvernightIndex overnightIndex_;
 protected DayCounter fixedDayCounter_;
 protected int paymentLag_;
 protected Frequency paymentFrequency_;
 protected BusinessDayConvention fixedConvention_;
 protected BusinessDayConvention paymentAdjustment_;
 protected DateGeneration.Rule rule_;
 
 protected OvernightIndexedSwap swap_;
 protected RelinkableHandle<YieldTermStructure> termStructureHandle_;
 protected Handle<YieldTermStructure> discountHandle_;
   protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;


  public DatedOISRateHelper( Date startDate,  Date endDate,  Handle<Quote> fixedRate,
                                        OvernightIndex overnightIndex,
                                        DayCounter fixedDayCounter, int paymentLag,
                                       Frequency paymentFrequency, BusinessDayConvention fixedConvention,
                                       BusinessDayConvention paymentAdjustment, DateGeneration.Rule rule,
                                        Handle<YieldTermStructure> discountingCurve)
    : base(fixedRate)
         { 
         overnightIndex_=overnightIndex; fixedDayCounter_=fixedDayCounter;
      paymentLag_=paymentLag; paymentFrequency_=paymentFrequency; fixedConvention_=fixedConvention;
      paymentAdjustment_=paymentAdjustment; rule_=rule; discountHandle_ = discountingCurve;
   

      bool onIndexHasCurve = !overnightIndex_.forwardingTermStructure().empty();
      bool haveDiscountCurve = !discountHandle_.empty();
      Utils.QL_REQUIRE(!(onIndexHasCurve && haveDiscountCurve),()=> "Have both curves nothing to solve for.");

      if (!onIndexHasCurve)
      {
         IborIndex clonedIborIndex = overnightIndex_.clone(termStructureHandle_);
         overnightIndex_ =(OvernightIndex)(clonedIborIndex);
            overnightIndex_.unregisterWith(update);// (termStructureHandle_);
      }

         overnightIndex_.registerWith(update);
         discountHandle_.registerWith(update);

      swap_ = new MakeOIS(new Period(), overnightIndex_, 0.0)
                  .withEffectiveDate(startDate)
                  .withTerminationDate(endDate)
                  .withFixedLegDayCount(fixedDayCounter_)
                  .withPaymentFrequency(paymentFrequency_)
                  .withRule(rule_)
                  // TODO: patch QL
                  //.withPaymentLag(paymentLag_)
                  //.withFixedAccrualConvention(fixedConvention_)
                  //.withFixedPaymentConvention(paymentAdjustment_)
                  .withDiscountingTermStructure(termStructureHandle_);

      earliestDate_ = swap_.startDate();
      latestDate_ = swap_.maturityDate();
   }

  public void DatedsetTermStructure(YieldTermStructure t)
   {
      // do not set the relinkable handle as an observer -
      // force recalculation when needed
      bool observer = false;

         YieldTermStructure temp = t;//, no_deletion);
      termStructureHandle_.linkTo(temp, observer);

      if (discountHandle_.empty())
         discountRelinkableHandle_.linkTo(temp, observer);
      else
         discountRelinkableHandle_.linkTo(discountHandle_, observer);

      base.setTermStructure(t);
   }

   public double DatedimpliedQuote()  {
    Utils.QL_REQUIRE(termStructure_ != null, ()=>"term structure not set");
   // we didn't register as observers - force calculation
   swap_.recalculate();
    return swap_.fairRate().Value;
}
      protected override void initializeDates()
      {
      }
void Datedaccept(IAcyclicVisitor v)
{
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");
      }
   }


}
