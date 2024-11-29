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

/*! \file qle/pricingengines/crossccyswapengine.hpp
    \brief Cross currency swap engine

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

   //! Cross currency swap engine

   /*! This class implements an engine for pricing swaps comprising legs that
       invovlve two currencies. The npv is expressed in ccy1. The given currencies
       ccy1 and ccy2 are matched to the correct swap legs. The evaluation date is the
       reference date of either discounting curve (which must be equal).

               \ingroup engines
   */
   public class CrossCcySwapEngine : CrossCcySwap.Engine
   {

      Currency ccy1_;
      Handle<YieldTermStructure> currency1Discountcurve_;
      Currency ccy2_;
      Handle<YieldTermStructure> currency2Discountcurve_;
      Handle<Quote> spotFX_;
      bool includeSettlementDateFlows_;
      Date settlementDate_;
      Date npvDate_;


      public CrossCcySwapEngine(Currency ccy1, Handle<YieldTermStructure> currency1Discountcurve,
                                         Currency ccy2, Handle<YieldTermStructure> currency2Discountcurve,
                                         Handle<Quote> spotFX, bool includeSettlementDateFlows,
                                         Date settlementDate, Date npvDate)

      {
         ccy1_ = ccy1; currency1Discountcurve_ = currency1Discountcurve; ccy2_ = ccy2;
         currency2Discountcurve_ = currency2Discountcurve; spotFX_ = spotFX;
         includeSettlementDateFlows_ = includeSettlementDateFlows; settlementDate_ = settlementDate; npvDate_ = npvDate;

         currency1Discountcurve_.registerWith(update);
         currency2Discountcurve_.registerWith(update);
         spotFX_.registerWith(update);

         //registerWith(currency1Discountcurve_);
         //registerWith(currency2Discountcurve_);
         //registerWith(spotFX_);
      }

      public override void calculate()
      {

         Utils.QL_REQUIRE(!currency1Discountcurve_.empty() & !currency2Discountcurve_.empty(), () =>
                     "Discounting term structure handle is empty.");

         Utils.QL_REQUIRE(!spotFX_.empty(), ()=>"FX spot quote handle is empty.");

         Utils.QL_REQUIRE(currency1Discountcurve_.currentLink().referenceDate() == currency2Discountcurve_.currentLink().referenceDate(), () =>
                     "Term structures should have the same reference date.");
         Date referenceDate = currency1Discountcurve_.currentLink().referenceDate();
         Date settlementDate = settlementDate_;
         if (settlementDate_ == new Date())
         {
            settlementDate = referenceDate;
         }
         else
         {
            Utils.QL_REQUIRE(settlementDate >= referenceDate, () => "Settlement date (" + settlementDate
                                                                            + ") cannot be before discount curve " +
                                                                               "reference date (" + referenceDate + ")");
         }

         int numLegs = arguments_.legs.Count;
         // - Instrument::Results
         if (npvDate_ == new Date())
         {
            results_.valuationDate = referenceDate;
         }
         else
         {
            Utils.QL_REQUIRE(npvDate_ >= referenceDate, () => "NPV date (" + npvDate_ + ") cannot be before " +
                                                              "discount curve reference date ("
                                                              + referenceDate + ")");
            results_.valuationDate = npvDate_;
         }
         results_.value = 0.0;
         results_.errorEstimate = double.NaN;
         // - Swap::Results
         results_.legNPV.Resize(numLegs);
         results_.legBPS.Resize(numLegs);
         results_.startDiscounts.Resize(numLegs);
         results_.endDiscounts.Resize(numLegs);
         // - CrossCcySwap::Results
         results_.inCcyLegNPV.Resize(numLegs);
         results_.inCcyLegBPS.Resize(numLegs);
         results_.npvDateDiscounts.Resize(numLegs);

         bool includeReferenceDateFlows = includeSettlementDateFlows_ ? includeSettlementDateFlows_ : Settings.includeReferenceDateEvents;

         for (int legNo = 0; legNo < numLegs; legNo++)
         {
            try
            {
               // Choose the correct discount curve for the leg.
               Handle<YieldTermStructure> legDiscountCurve;
               if (arguments_.currencies[legNo] == ccy1_)
               {
                  legDiscountCurve = currency1Discountcurve_;
               }
               else
               {
                  Utils.QL_REQUIRE(arguments_.currencies[legNo] == ccy2_, () => "leg ccy (" + arguments_.currencies[legNo]
                                                                                + ") must be ccy1 (" + ccy1_
                                                                                + ") or ccy2 (" + ccy2_ + ")");
                  legDiscountCurve = currency2Discountcurve_;
               }
               results_.npvDateDiscounts[legNo] = legDiscountCurve.currentLink().discount(results_.valuationDate);

               // Calculate the NPV and BPS of each leg in its currency.
               double npv, bps = 0;
               CashFlows.npvbps(arguments_.legs[legNo], legDiscountCurve, includeReferenceDateFlows, settlementDate,
                                 results_.valuationDate, out npv, out bps);
               results_.inCcyLegNPV[legNo] = npv; results_.inCcyLegBPS[legNo] = bps;

               results_.inCcyLegNPV[legNo] *= arguments_.payer[legNo];
               results_.inCcyLegBPS[legNo] *= arguments_.payer[legNo];

               results_.legNPV[legNo] = results_.inCcyLegNPV[legNo];
               results_.legBPS[legNo] = results_.inCcyLegBPS[legNo];

               // Convert to NPV currency if necessary.
               if (arguments_.currencies[legNo] != ccy1_)
               {
                  results_.legNPV[legNo] *= spotFX_.currentLink().value();
                  results_.legBPS[legNo] *= spotFX_.currentLink().value();
               }

               // Get start date and end date discount for the leg
               Date startDate = CashFlows.startDate(arguments_.legs[legNo]);
               if (startDate >= currency1Discountcurve_.currentLink().referenceDate())
               {
                  results_.startDiscounts[legNo] = legDiscountCurve.currentLink().discount(startDate);
               }
               else
               {
                  results_.startDiscounts[legNo] = null;
               }

               Date maturityDate = CashFlows.maturityDate(arguments_.legs[legNo]);
               if (maturityDate >= currency1Discountcurve_.currentLink().referenceDate())
               {
                  results_.endDiscounts[legNo] = legDiscountCurve.currentLink().discount(maturityDate);
               }
               else
               {
                  results_.endDiscounts[legNo] = null;
               }

            }
            catch (Exception e)
            {
               Utils.QL_FAIL((legNo + 1) + " leg: " + e.Message);
            }

            results_.value += results_.legNPV[legNo];
         }
      }


   }
}
