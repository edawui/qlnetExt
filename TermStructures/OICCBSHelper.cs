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

/*! \file oiccbasisswaphelper.hpp
    \brief Overnight Indexed Cross Currency Basis Swap helpers
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
   class OICCBSHelper : RelativeDateRateHelper
   {
      //! Rate helper for bootstrapping over Overnight Indexed CC Basis Swap Spreads
      /*
        The bootstrap affects the receive leg's discount curve only.

            \ingroup termstructures
      */

      protected int settlementDays_;
      protected Period term_;
      protected OvernightIndex payIndex_;
      protected Period payTenor_;
      protected OvernightIndex recIndex_;
      protected Period recTenor_;
      protected Handle<YieldTermStructure> fixedDiscountCurve_;
      protected bool spreadQuoteOnPayLeg_;
      protected bool fixedDiscountOnPayLeg_;

      protected OvernightIndexedCrossCcyBasisSwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_;


      public OICCBSHelper(int settlementDays,
                             Period term, // swap maturity
                             OvernightIndex payIndex, Period payTenor,
                             OvernightIndex recIndex,
                             Period recTenor, // swap maturity
                             Handle<Quote> spreadQuote, Handle<YieldTermStructure> fixedDiscountCurve,
                            bool spreadQuoteOnPayLeg, bool fixedDiscountOnPayLeg)
     : base(spreadQuote)
      {
         settlementDays_ = settlementDays; term_ = term; payIndex_ = payIndex;
         payTenor_ = payTenor; recIndex_ = recIndex; recTenor_ = recTenor; fixedDiscountCurve_ = fixedDiscountCurve;
         spreadQuoteOnPayLeg_ = spreadQuoteOnPayLeg; fixedDiscountOnPayLeg_ = fixedDiscountOnPayLeg;


         payIndex_.registerWith(update);
         recIndex_.registerWith(update);
         fixedDiscountCurve_.registerWith(update);
         initializeDates();
      }

      protected override void initializeDates()
      {
         Date asof = Settings.evaluationDate();
         Date settlementDate = payIndex_.fixingCalendar().advance(asof, settlementDays_, TimeUnit.Days);
         Schedule paySchedule = new MakeSchedule().from(settlementDate).to(settlementDate + term_).withTenor(payTenor_).value();
         Schedule recSchedule = new MakeSchedule().from(settlementDate).to(settlementDate + term_).withTenor(recTenor_).value();
         Currency payCurrency = new EURCurrency(); // arbitrary here
         Currency recCurrency = new GBPCurrency(); // recCcy != payCcy, but FX=1
         Quote fx = new SimpleQuote(1.0);
         swap_ = new OvernightIndexedCrossCcyBasisSwap(10000.0, // arbitrary payNominal
                                                   payCurrency, paySchedule, payIndex_,
                                                   0.0,     // zero pay spread
                                                   10000.0, // recNominal consistent with FX rate used
                                                   recCurrency, recSchedule, recIndex_,
                                                   0.0); // target receive spread
         if (fixedDiscountOnPayLeg_)
         {
            IPricingEngine engine = new OvernightIndexedCrossCcyBasisSwapEngine(fixedDiscountCurve_, payCurrency, termStructureHandle_, recCurrency, new Handle<Quote>(fx));
            swap_.setPricingEngine(engine);
         }
         else
         {
            IPricingEngine engine = new OvernightIndexedCrossCcyBasisSwapEngine(termStructureHandle_, payCurrency, fixedDiscountCurve_, recCurrency, new Handle<Quote>(fx));
            swap_.setPricingEngine(engine);
         }

         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // we didn't register as observers - force calculation
         swap_.recalculate();
         if (spreadQuoteOnPayLeg_)
            return swap_.fairPayLegSpread();
         else
            return swap_.fairRecLegSpread();
      }

      public void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");

      }
   }
}

