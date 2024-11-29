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

/*! \file qle/pricingengines/discountingcurrencyswapengine.hpp
    \brief discounting currency swap engine

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

   //! %Discounting %CurrencySwap Engine

   /*! This class generalizes QuantLib's DiscountingSwapEngine. It takes
       leg currencies into account and converts into the provided "npv
       currency", which must be one of the leg currencies. The evaluation
       date is the reference date of either of the discounting curves (which
       must be equal).

               \ingroup engines
   */
   public class DiscountingCurrencySwapEngine : CurrencySwap.Engine
   {

      /*! The FX spots must be given as units of npvCurrency per respective
        currency. The spots must be given w.r.t. a settlement date equal
        to the npv date. */


      List<Handle<YieldTermStructure>> discountCurves_;
      List<Handle<Quote>> fxQuotes_;
      List<Currency> currencies_;
      Currency npvCurrency_;
      bool includeSettlementDateFlows_;
      Date settlementDate_;
      Date npvDate_;


      public DiscountingCurrencySwapEngine(List<Handle<YieldTermStructure>> discountCurves, List<Handle<Quote>> fxQuotes,
                                            List<Currency> currencies, Currency npvCurrency,
                                            bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
      {
         discountCurves_ = discountCurves; fxQuotes_ = fxQuotes; currencies_ = currencies; npvCurrency_ = npvCurrency;
         includeSettlementDateFlows_ = includeSettlementDateFlows; settlementDate_ = settlementDate; npvDate_ = npvDate;


         Utils.QL_REQUIRE(discountCurves_.Count == currencies_.Count, () => "Number of currencies does not match number of discount curves.");
         Utils.QL_REQUIRE(fxQuotes_.Count == currencies_.Count, () => "Number of currencies does not match number of FX quotes.");

         for (int i = 0; i < discountCurves_.Count; i++)
         {
            discountCurves_[i].registerWith(update);
            fxQuotes_[i].registerWith(update);
         }
      }

      private Handle<YieldTermStructure> fetchTS(Currency ccy)
      {
         int i = currencies_.IndexOf(ccy);
         if (i == currencies_.Count)
            return new Handle<YieldTermStructure>();
         else
            return discountCurves_[i];
      }

      private Handle<Quote> fetchFX(Currency ccy)
      {
         int i = currencies_.IndexOf(ccy);
         if (i == currencies_.Count)
            return new Handle<Quote>();
         else
            return fxQuotes_[i];
      }

      public override void calculate()
      {

         for (int i = 0; i < arguments_.currency.Count; i++)
         {
            Currency ccy = arguments_.currency[i];
            Handle<YieldTermStructure> yts = fetchTS(ccy);
            Utils.QL_REQUIRE(!yts.empty(), () => "Discounting term structure is empty for " + ccy.name);
            Handle<Quote> fxQuote = fetchFX(ccy);
            Utils.QL_REQUIRE(!fxQuote.empty(), () => "FX quote is empty for " + ccy.name);
         }

         Handle<YieldTermStructure> npvCcyYts = fetchTS(npvCurrency_);

         // Instrument settlement date
         Date referenceDate = npvCcyYts.currentLink().referenceDate();
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

         // Prepare the results containers
         int numLegs = arguments_.legs.Count;

         // - Instrument::results
         if (npvDate_ == new Date())
         {
            results_.valuationDate = referenceDate;
         }
         else
         {
            Utils.QL_REQUIRE(npvDate_ >= referenceDate, () => "NPV date (" + npvDate_ + ") cannot be before " +
                                                                              "discount curve reference date (" + referenceDate + ")");
            results_.valuationDate = npvDate_;
         }
         results_.value = 0.0;
         results_.errorEstimate = double.NaN;

         // - CurrencySwap::results
         results_.legNPV.Resize(numLegs);
         results_.legBPS.Resize(numLegs);
         results_.inCcyLegNPV.Resize(numLegs);
         results_.inCcyLegBPS.Resize(numLegs);
         results_.startDiscounts.Resize(numLegs);
         results_.endDiscounts.Resize(numLegs);

         bool includeRefDateFlows =
             includeSettlementDateFlows_ ? includeSettlementDateFlows_ : Settings.includeReferenceDateEvents;

         results_.npvDateDiscount = npvCcyYts.currentLink().discount(results_.valuationDate);

         for (int i = 0; i < numLegs; ++i)
         {
            try
            {
               Currency ccy = arguments_.currency[i];
               Handle<YieldTermStructure> yts = fetchTS(ccy);
               double npv = double.NaN;
               double bps = double.NaN;
               CashFlows.npvbps(arguments_.legs[i], yts, includeRefDateFlows, settlementDate,
                                           results_.valuationDate, out npv, out bps);
               results_.inCcyLegNPV[i] = npv;
               results_.inCcyLegBPS[i] = bps;

               results_.inCcyLegNPV[i] *= arguments_.payer[i];
               if (results_.inCcyLegBPS[i] != double.NaN)
               {
                  results_.inCcyLegBPS[i] *= arguments_.payer[i];
               }

               // Converts into base currency and adds.
               Handle<Quote> fx = fetchFX(ccy);
               results_.legNPV[i] = results_.inCcyLegNPV[i] * fx.currentLink().value();
               if (results_.inCcyLegBPS[i] != double.NaN)
               {
                  results_.legBPS[i] = results_.inCcyLegBPS[i] * fx.currentLink().value();
               }
               else
               {
                  results_.legBPS[i] = double.NaN;
               }

               results_.value += results_.legNPV[i];

               if (!arguments_.legs[i].empty())
               {
                  Date d1 = CashFlows.startDate(arguments_.legs[i]);
                  if (d1 >= referenceDate)
                     results_.startDiscounts[i] = yts.currentLink().discount(d1);
                  else
                     results_.startDiscounts[i] = double.NaN;//Null<DiscountFactor>();

                  Date d2 = CashFlows.maturityDate(arguments_.legs[i]);
                  if (d2 >= referenceDate)
                     results_.endDiscounts[i] = yts.currentLink().discount(d2);
                  else
                     results_.endDiscounts[i] = double.NaN;
               }
               else
               {
                  results_.startDiscounts[i] = double.NaN;
                  results_.endDiscounts[i] = double.NaN;
               }

            }
            catch (Exception e)
            {
               Utils.QL_FAIL("leg " + i + ": " + e.Message);
            }
         }


      }

   }
}
