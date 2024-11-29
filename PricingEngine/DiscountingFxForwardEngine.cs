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

/*! \file qle/pricingengines/discountingfxforwardengine.hpp
    \brief Engine to value an FX Forward off two yield curves

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

   //! Discounting FX Forward Engine

   /*! This class implements pricing of FX Forwards by discounting the future
       nominal cash flows using the respective yield curves. The npv is
       expressed in ccy1. The given currencies ccy1 and ccy2 are matched
       to the correct fx forward legs. The evaluation date is the
       reference date of either discounting curve (which must be equal).

               \ingroup engines
   */

   public class DiscountingFxForwardEngine : FxForward.Engine
   {

      Currency ccy1_;
      Handle<YieldTermStructure> currency1Discountcurve_;
      Currency ccy2_;
      Handle<YieldTermStructure> currency2Discountcurve_;
      Handle<Quote> spotFX_;
      bool includeSettlementDateFlows_;
      Date settlementDate_;
      Date npvDate_;

      /*! \param ccy1, currency1Discountcurve
                 Currency 1 and its discount curve.
          \param ccy2, currency2Discountcurve
                 Currency 2 and its discount curve.
          \param spotFX
                 The market spot rate quote, given as units of ccy1
                 for one unit of ccy2. The spot rate must be given
                 w.r.t. a settlement equal to the npv date.
          \param includeSettlementDateFlows, settlementDate
                 If includeSettlementDateFlows is true (false), cashflows
                 on the settlementDate are (not) included in the NPV.
                 If not given the settlement date is set to the
                 npv date.
          \param npvDate
                 Discount to this date. If not given the npv date
                 is set to the evaluation date
      */


      public DiscountingFxForwardEngine(
     Currency ccy1, Handle<YieldTermStructure> currency1Discountcurve, Currency ccy2,
     Handle<YieldTermStructure> currency2Discountcurve, Handle<Quote> spotFX,
    bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
      {
         ccy1_ = ccy1; currency1Discountcurve_ = currency1Discountcurve; ccy2_ = ccy2;
         currency2Discountcurve_ = currency2Discountcurve; spotFX_ = spotFX;
         includeSettlementDateFlows_ = includeSettlementDateFlows; settlementDate_ = settlementDate; npvDate_ = npvDate;

         currency1Discountcurve_.registerWith(update);
         currency2Discountcurve_.registerWith(update);
         spotFX_.registerWith(update);
      }

      public override void calculate()
      {

         Date npvDate = npvDate_;
         if (npvDate == null)
         {
            npvDate = currency1Discountcurve_.currentLink().referenceDate();
         }
         Date settlementDate = settlementDate_;
         if (settlementDate == null)
         {
            settlementDate = npvDate;
         }

         double tmpNominal1, tmpNominal2;
         bool tmpPayCurrency1;
         if (ccy1_ == arguments_.currency1)
         {
            Utils.QL_REQUIRE(ccy2_ == arguments_.currency2, () => "mismatched currency pairs ("
                                                         + ccy1_ + "," + ccy2_ + ") in the egine and ("
                                                         + arguments_.currency1 + "," + arguments_.currency2
                                                         + ") in the instrument");
            tmpNominal1 = arguments_.nominal1;
            tmpNominal2 = arguments_.nominal2;
            tmpPayCurrency1 = arguments_.payCurrency1;
         }
         else
         {
            Utils.QL_REQUIRE(ccy1_ == arguments_.currency2 && ccy2_ == arguments_.currency1, () =>
                      "mismatched currency pairs (" + ccy1_ + "," + ccy2_ + ") in the egine and ("
                                                    + arguments_.currency1 + "," + arguments_.currency2
                                                    + ") in the instrument");
            tmpNominal1 = arguments_.nominal2;
            tmpNominal2 = arguments_.nominal1;
            tmpPayCurrency1 = !arguments_.payCurrency1;
         }

         Utils.QL_REQUIRE(!currency1Discountcurve_.empty() && !currency2Discountcurve_.empty(), () =>
                    "Discounting term structure handle is empty.");

         Utils.QL_REQUIRE(currency1Discountcurve_.currentLink().referenceDate() == currency2Discountcurve_.currentLink().referenceDate(), () =>
                     "Term structures should have the same reference date.");

         Utils.QL_REQUIRE(arguments_.maturityDate >= currency1Discountcurve_.currentLink().referenceDate(), () =>
                    "FX forward maturity should exceed or equal the discount curve reference date.");

         results_.value = 0.0;
         results_.fairForwardRate = new ExchangeRate(ccy2_, ccy1_, tmpNominal1 / tmpNominal2); // strike rate

         if (!new simple_event(arguments_.maturityDate).hasOccurred(settlementDate, includeSettlementDateFlows_))
         {
            double disc1near = currency1Discountcurve_.currentLink().discount(npvDate);
            double disc1far = currency1Discountcurve_.currentLink().discount(arguments_.maturityDate);
            double disc2near = currency2Discountcurve_.currentLink().discount(npvDate);
            double disc2far = currency2Discountcurve_.currentLink().discount(arguments_.maturityDate);
            double fxfwd = disc1near / disc1far * disc2far / disc2near * spotFX_.currentLink().value();
            // results_.value =
            //     (tmpPayCurrency1 ? -1.0 : 1.0) * (tmpNominal1 * disc1far / disc1near -
            //                                       tmpNominal2 * disc2far / disc2near * spotFX_.value());
            results_.value = (tmpPayCurrency1 ? -1.0 : 1.0) * disc1far / disc1near * (tmpNominal1 - tmpNominal2 * fxfwd);
            results_.fairForwardRate = new ExchangeRate(ccy2_, ccy1_, fxfwd);
         }
         results_.npv = new Money(ccy1_, results_.value.Value);

      } // calculate


   }
}
