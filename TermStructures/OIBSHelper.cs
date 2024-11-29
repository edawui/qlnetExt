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

/*! \file qle/termstructures/oibasisswaphelper.hpp
    \brief Overnight Indexed Basis Swap rate helpers
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
   //typedef RelativeDateBootstrapHelper<YieldTermStructure>
   public class OIBSHelper : RelativeDateRateHelper
   {


      protected int settlementDays_;
      protected Period tenor_;
      protected OvernightIndex overnightIndex_;
      protected IborIndex iborIndex_;
      protected OvernightIndexedBasisSwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_;




      OIBSHelper(int settlementDays,
                        Period tenor, // swap maturity
                        Handle<Quote> oisSpread, OvernightIndex overnightIndex,
                        IborIndex iborIndex)
    : base(oisSpread)
      {
         settlementDays_ = settlementDays; tenor_ = tenor;
         overnightIndex_ = overnightIndex; iborIndex_ = iborIndex;

         overnightIndex_.registerWith(update);
         iborIndex_.registerWith(update);
         initializeDates();
      }

      protected override void initializeDates()
      {

         IborIndex clonedIborIndex = iborIndex_.clone(termStructureHandle_);
         // avoid notifications
         iborIndex_.unregisterWith(update);//termStructureHandle_);

         Date asof = Settings.evaluationDate();
         Date settlementDate = iborIndex_.fixingCalendar().advance(asof, settlementDays_, TimeUnit.Days);
         Schedule oisSchedule = new MakeSchedule().from(settlementDate).to(settlementDate + tenor_).withTenor(new Period(1, TimeUnit.Years)).forwards().value();
         Schedule iborSchedule = new MakeSchedule().from(settlementDate).to(settlementDate + tenor_).withTenor(iborIndex_.tenor()).forwards().value();
         swap_ = new OvernightIndexedBasisSwap(OvernightIndexedBasisSwap.Type.Payer,
                                                                                            10000.0, // arbitrary
                                                                                            oisSchedule, overnightIndex_,
                                                                                            iborSchedule, clonedIborIndex
                                                                                            , 0.0, 0.0);//check if it should be 0.0
         IPricingEngine engine = new DiscountingSwapEngine(overnightIndex_.forwardingTermStructure());
         swap_.setPricingEngine(engine);

         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed
         //termStructureHandle_.linkTo(shared_ptr<YieldTermStructure>(t, no_deletion), false);

         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // we didn't register as observers - force calculation
         swap_.recalculate();
         return swap_.fairOvernightSpread();
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

