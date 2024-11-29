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

/*! \file crossccyswap.hpp
    \brief Swap instrument with legs involving two currencies

        \ingroup instruments
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class CrossCcySwap : Swap
   {


      public new class Arguments : Swap.Arguments
      {
         public List<Currency> currencies;

         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(legs.Count == currencies.Count, () => "Number of legs is not equal to number of currencies");
         }
      }

      public new class Results : Swap.Results
      {
         public
            List<double> inCcyLegNPV;
         public List<double> inCcyLegBPS;
         public List<double> npvDateDiscounts;

         public override void reset()
         {

            base.reset();
            inCcyLegNPV.Clear();
            inCcyLegBPS.Clear();
            npvDateDiscounts.Clear();
         }
      }

      public class Engine : GenericEngine<CrossCcySwap.Arguments, CrossCcySwap.Results>
      { }



      protected List<Currency> currencies_;


      List<double> inCcyLegNPV_;
      List<double> inCcyLegBPS_;
      List<double> npvDateDiscounts_;




      public CrossCcySwap(List<CashFlow> firstLeg, Currency firstLegCcy,
                         List<CashFlow> secondLeg, Currency secondLegCcy)
     : base(firstLeg, secondLeg)
      {

         currencies_ = new List<Currency>();


         inCcyLegNPV_ = new List<double>();
         inCcyLegBPS_ = new List<double>();
         npvDateDiscounts_ = new List<double>();

         currencies_.Resize(2);
         currencies_[0] = firstLegCcy;
         currencies_[1] = secondLegCcy;
      }


      public CrossCcySwap(List<List<CashFlow>> legs, List<bool> payer,
                                List<Currency> currencies)
        : base(legs, payer)
      {
         currencies_ = currencies;
         Utils.QL_REQUIRE(payer.Count == currencies_.Count, () => "int mismatch between payer ("
                                                             + payer.Count + ") and currencies (" + currencies_.Count
                                                             + ")");
      }

      protected CrossCcySwap(int legs)
      : base(legs)
      {
         currencies_ = new List<Currency>(legs);

         inCcyLegNPV_ = Enumerable.Repeat<double>(0.0, legs).ToList();
         npvDateDiscounts_ = Enumerable.Repeat<double>(0.0, legs).ToList();
      }


      public override void setupArguments(IPricingEngineArguments args)
      {

         base.setupArguments(args);

         Arguments arguments = (Arguments)args;
         Utils.QL_REQUIRE(arguments != null, () => "The arguments are not of type cross currency swap");

         arguments.currencies = currencies_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {

         base.fetchResults(r);

         Results results = (Results)r;
         Utils.QL_REQUIRE(results != null, () => "The results are not of type cross currency swap");

         if (!results.inCcyLegNPV.empty())
         {
            Utils.QL_REQUIRE(results.inCcyLegNPV.Count == inCcyLegNPV_.Count, () => "Wrong number of in currency leg NPVs returned by engine");
            inCcyLegNPV_ = results.inCcyLegNPV;
         }
         else
         {

            inCcyLegNPV_ = Enumerable.Repeat<double>(double.NaN, inCcyLegNPV_.Count).ToList();
            //std::fill(inCcyLegNPV_.begin(), inCcyLegNPV_.end(), Null<double>());
         }

         if (!results.inCcyLegBPS.empty())
         {
            Utils.QL_REQUIRE(results.inCcyLegBPS.Count == inCcyLegBPS_.Count, () => "Wrong number of in currency leg BPSs returned by engine");
            legBPS_ = results.legBPS;
         }
         else
         {
            inCcyLegBPS_ = Enumerable.Repeat<double>(double.NaN, inCcyLegBPS_.Count).ToList();
            // std::fill(inCcyLegBPS_.begin(), inCcyLegBPS_.end(), Null<double>());
         }

         if (!results.npvDateDiscounts.empty())
         {
            Utils.QL_REQUIRE(results.npvDateDiscounts.Count == npvDateDiscounts_.Count, () => "Wrong number of npv date discounts returned by engine");
            npvDateDiscounts_ = results.npvDateDiscounts;
         }
         else
         {
            npvDateDiscounts_ = Enumerable.Repeat<double>(double.NaN, npvDateDiscounts_.Count).ToList();
            //std::fill(npvDateDiscounts_.begin(), npvDateDiscounts_.end(), Null<double>());
         }
      }



      protected override void setupExpired()
      {
         base.setupExpired();
         inCcyLegBPS_ = Enumerable.Repeat<double>(0.0, inCcyLegBPS_.Count).ToList();
         inCcyLegNPV_ = Enumerable.Repeat<double>(0.0, inCcyLegNPV_.Count).ToList();
         npvDateDiscounts_ = Enumerable.Repeat<double>(0.0, npvDateDiscounts_.Count).ToList();
      }

      //@}
      //! \name Additional interface
      //@{
      public Currency legCurrency(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         return currencies_[j];
      }

      public double inCcyLegBPS(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         calculate();
         return inCcyLegBPS_[j];
      }

      public double inCcyLegNPV(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return inCcyLegNPV_[j];
      }

      public double npvDateDiscounts(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return npvDateDiscounts_[j];
      }

   }

}
