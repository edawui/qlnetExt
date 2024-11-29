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

/*! \file qle/pricingengines/discountingequityforwardengine.hpp
    \brief Engine to value an Equity Forward contract

    \ingroup engines
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using QLNet;

namespace QLNetExt
{
   //! Discounting Equity Forward Engine

   /*! This class implements pricing of Equity Forwards by discounting the future
       nominal cash flows using the respective yield curves. The forward price is
       estimated using reference rate and dividend yield curves as input. The
       cashflows are discounted using a separate discounting curve input.

               \ingroup engines
   */


   public class DiscountingEquityForwardEngine : EquityForward.Engine
   {
      Handle<YieldTermStructure> equityRefRateCurve_;
      Handle<YieldTermStructure> divYieldCurve_;
      Handle<Quote> equitySpot_;
      Handle<YieldTermStructure> discountCurve_;
      bool includeSettlementDateFlows_;
      Date settlementDate_;
      Date npvDate_;



      /*! \param equityInterestRateCurve
               The IR rate curve for estimating forward price.
        \param dividendYieldCurve
               The dividend yield term structure for estimating
               forward price.
        \param equitySpot
               The market spot rate quote.
        \param discountCurve
               The discount curve
        \param includeSettlementDateFlows, settlementDate
               If includeSettlementDateFlows is true (false), cashflows
               on the settlementDate are (not) included in the NPV.
               If not given the settlement date is set to the
               npv date.
        \param npvDate
               Discount to this date. If not given the npv date
               is set to the evaluation date
    */


      public DiscountingEquityForwardEngine(
     Handle<YieldTermStructure> equityInterestRateCurve, Handle<YieldTermStructure> dividendYieldCurve,
     Handle<Quote> equitySpot, Handle<YieldTermStructure> discountCurve,
    bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
      {
         equityRefRateCurve_ = equityInterestRateCurve; divYieldCurve_ = dividendYieldCurve; equitySpot_ = equitySpot;
         discountCurve_ = discountCurve; includeSettlementDateFlows_ = includeSettlementDateFlows;
         settlementDate_ = settlementDate; npvDate_ = npvDate;


         equityRefRateCurve_.registerWith(update);
         divYieldCurve_.registerWith(update);
         equitySpot_.registerWith(update);
         discountCurve_.registerWith(update);
      }

      public Handle<YieldTermStructure> equityReferenceRateCurve() { return equityRefRateCurve_; }
      public Handle<YieldTermStructure> divYieldCurve() { return divYieldCurve_; }
      public Handle<YieldTermStructure> discountCurve() { return discountCurve_; }

      public Handle<Quote> equitySpot() { return equitySpot_; }


      public override void calculate()
      {

         Date npvDate = npvDate_;
         if (npvDate == null)
         {
            npvDate = divYieldCurve_.currentLink().referenceDate();
         }
         Date settlementDate = settlementDate_;
         if (settlementDate == null)
         {
            settlementDate = npvDate;
         }

         results_.value = 0.0;


         if (!(new simple_event(arguments_.maturityDate).hasOccurred(settlementDate, includeSettlementDateFlows_)))
         {
            double lsInd = ((arguments_.longShort == Position.Type.Long) ? 1.0 : -1.0);
            double qty = arguments_.quantity;
            Date maturity = arguments_.maturityDate;
            double strike = arguments_.strike;
            double forwardPrice =
                equitySpot_.currentLink().value() * divYieldCurve_.currentLink().discount(maturity) / equityRefRateCurve_.currentLink().discount(maturity);
            double df = discountCurve_.currentLink().discount(maturity);
            results_.value = (lsInd * qty) * (forwardPrice - strike) * df;
         }
      } // calculate

   }
}
