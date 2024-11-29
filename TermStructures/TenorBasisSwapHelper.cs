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
    \brief Single currency tenor basis swap helper
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
   //! Rate helper for bootstrapping using Libor tenor basis swaps
   /*! \ingroup termstructures
   */
   public class TenorBasisSwapHelper : RelativeDateRateHelper
   {


      Period swapTenor_;
      IborIndex longIndex_;
      IborIndex shortIndex_;
      Period shortPayTenor_;
      bool spreadOnShort_;
      bool includeSpread_;
      SubPeriodsCoupon.Type type_;

      TenorBasisSwap swap_;
      RelinkableHandle<YieldTermStructure> termStructureHandle_;
      Handle<YieldTermStructure> discountHandle_;
      RelinkableHandle<YieldTermStructure> discountRelinkableHandle_;


      public TenorBasisSwapHelper(Handle<Quote> spread, Period swapTenor,
                                            IborIndex longIndex,
                                            IborIndex shortIndex, Period shortPayTenor,
                                             Handle<YieldTermStructure> discountingCurve, bool spreadOnShort,
                                            bool includeSpread, SubPeriodsCoupon.Type type)
     : base(spread)
      {
         swapTenor_ = swapTenor; longIndex_ = longIndex; shortIndex_ = shortIndex;
         spreadOnShort_ = spreadOnShort; includeSpread_ = includeSpread; type_ = type; discountHandle_ = discountingCurve;


         bool longIndexHasCurve = !longIndex_.forwardingTermStructure().empty();
         bool shortIndexHasCurve = !shortIndex_.forwardingTermStructure().empty();
         bool haveDiscountCurve = !discountHandle_.empty();
         Utils.QL_REQUIRE(!(longIndexHasCurve && shortIndexHasCurve && haveDiscountCurve), () => "Have all curves nothing to solve for.");

         if (longIndexHasCurve && !shortIndexHasCurve)
         {
            shortIndex_ = shortIndex_.clone(termStructureHandle_);
            shortIndex_.unregisterWith(update);//termStructureHandle_);
         }
         else if (!longIndexHasCurve && shortIndexHasCurve)
         {
            longIndex_ = longIndex_.clone(termStructureHandle_);
            longIndex_.unregisterWith(update);// termStructureHandle_);
         }
         else if (!longIndexHasCurve && !shortIndexHasCurve)
         {
            Utils.QL_FAIL("Need at least one of the indices to have a valid curve.");
         }

         shortPayTenor_ = (shortPayTenor == new Period()) ? shortIndex_.tenor() : shortPayTenor;

         longIndex_.registerWith(update);
         shortIndex_.registerWith(update);
         discountHandle_.registerWith(update);
         initializeDates();
      }
      
      protected override void initializeDates()
      {

         Date valuationDate = Settings.evaluationDate();
         Calendar spotCalendar = longIndex_.fixingCalendar();
         int spotDays = longIndex_.fixingDays();
         Date effectiveDate = spotCalendar.advance(valuationDate, new Period(spotDays, TimeUnit.Days));

         swap_ = new TenorBasisSwap(effectiveDate, 1.0, swapTenor_, true, longIndex_, 0.0,
                                                                      shortIndex_, 0.0, shortPayTenor_,
                                                                      DateGeneration.Rule.Backward, includeSpread_, type_);

         IPricingEngine engine = new DiscountingSwapEngine(discountRelinkableHandle_);
         swap_.setPricingEngine(engine);

         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();

         FloatingRateCoupon lastFloating = (FloatingRateCoupon)((termStructureHandle_ == shortIndex_.forwardingTermStructure()) ? swap_.shortLeg().Last()
                                                                             : swap_.longLeg().Last());
         //# ifdef QL_USE_INDEXED_COUPON
         //         /* May need to adjust latestDate_ if you are projecting libor based
         //        on tenor length rather than from accrual date to accrual date. */
         //         Date fixingValueDate = shortIndex_.valueDate(lastFloating.fixingDate());
         //         Date endValueDate = shortIndex_.maturityDate(fixingValueDate);
         //         latestDate_ = Date.Max(latestDate_, endValueDate);
         //#else
         //         /* Subperiods coupons do not have a par approximation either... */
         //         if ((SubPeriodsCoupon)(lastFloating) == null)
         //         {
         //            Date fixingValueDate2 = shortIndex_.valueDate(lastFloating.fixingDate());
         //            Date endValueDate2 = shortIndex_.maturityDate(fixingValueDate2);
         //            latestDate_ = Date.Max(latestDate_, endValueDate2);
         //         }
         //#endif
         Date fixingValueDate2 = shortIndex_.valueDate(lastFloating.fixingDate());
         Date endValueDate2 = shortIndex_.maturityDate(fixingValueDate2);
         latestDate_ = Date.Max(latestDate_, endValueDate2);
      }

      public override void setTermStructure(YieldTermStructure t)
      {

         bool observer = false;

         YieldTermStructure temp = t;// t, no_deletion);
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
         return (spreadOnShort_ ? swap_.fairShortLegSpread() : swap_.fairLongLegSpread());
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


         //   Visitor<TenorBasisSwapHelper>* v1 = dynamic_cast<Visitor<TenorBasisSwapHelper>*>(&v);
         //if (v1 != 0)
         //   v1.visit(*this);
         //else
         //   RateHelper::accept(v);

      }

      
   }
}
