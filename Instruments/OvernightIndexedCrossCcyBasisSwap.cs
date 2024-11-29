
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

/*! \file oiccbasisswap.hpp
    \brief Cross currency overnight index swap paying compounded overnight vs. float

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
   public class OvernightIndexedCrossCcyBasisSwap : Swap
   {

      public new class Arguments : Swap.Arguments
      {

         public List<Currency> currency;
         public double paySpread;
         public double recSpread;
      }

      public new class Results : Swap.Results
      {

         public double fairPayLegSpread;
         public double fairRecLegSpread;

         public override void reset()
         {
            base.reset();
            fairPayLegSpread = double.NaN;
            fairRecLegSpread = double.NaN;
         }

      }

      public class Engine : GenericEngine<Arguments, Results>
      {
      }


      double payNominal_, recNominal_;
      Currency payCurrency_, recCurrency_;
      Schedule paySchedule_, recSchedule_;
      OvernightIndex payIndex_, recIndex_;
      double paySpread_, recSpread_;
      List<Currency> currency_;

      double fairPayLegSpread_;
      double fairRecLegSpread_;


      public OvernightIndexedCrossCcyBasisSwap(
          double payNominal, Currency payCurrency, Schedule paySchedule,
           OvernightIndex payIndex, double paySpread, double recNominal, Currency recCurrency,
           Schedule recSchedule, OvernightIndex recIndex, double recSpread)
          : base(2)
      {
         payNominal_ = payNominal; recNominal_ = recNominal; payCurrency_ = payCurrency; recCurrency_ = recCurrency;
         paySchedule_ = paySchedule; recSchedule_ = recSchedule; payIndex_ = payIndex; recIndex_ = recIndex;
         paySpread_ = paySpread; recSpread_ = recSpread; currency_ = new List<Currency>(2);

         payIndex_.registerWith(update);
         recIndex_.registerWith(update);

         //registerWith(payIndex);
         //registerWith(recIndex);
         initialize();
      }

      private void initialize()
      {
         legs_[0] = new OvernightLeg(paySchedule_, payIndex_).withNotionals(payNominal_).withSpreads(paySpread_);
         legs_[0].Insert(0, (CashFlow)(new SimpleCashFlow(-payNominal_, paySchedule_.dates().First())));
         legs_[0].Add((CashFlow)(new SimpleCashFlow(payNominal_, paySchedule_.dates().Last())));

         legs_[1] = new OvernightLeg(recSchedule_, recIndex_).withNotionals(recNominal_).withSpreads(recSpread_);
         legs_[1].Insert(0,
                        (CashFlow)(new SimpleCashFlow(-recNominal_, recSchedule_.dates().First())));
         legs_[1].Add((CashFlow)(new SimpleCashFlow(recNominal_, recSchedule_.dates().Last())));

         for (int j = 0; j < 2; ++j)
         {
            for (int i = 0; i < legs_[j].Count; ++i)//Leg::iterator i = legs_[j].begin(); i != legs_[j].end(); ++i)
               legs_[j][i].registerWith(update);//registerWith(*i);
         }

         payer_[0] = -1.0;
         payer_[1] = +1.0;

         currency_[0] = payCurrency_;
         currency_[1] = recCurrency_;
      }

    public  double fairPayLegSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairPayLegSpread_ != double.NaN, () => "result not available");
         return fairPayLegSpread_;
      }

      public double fairRecLegSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairRecLegSpread_ != double.NaN, () => "result not available");
         return fairRecLegSpread_;
      }

      public double payLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[0] != double.NaN, () => "result not available");
         return legBPS_[0].Value;
      }

      public double recLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[1] != double.NaN, () => "result not available");
         return legBPS_[1].Value;
      }

      public double payLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[0] != double.NaN, () => "result not available");
         return legNPV_[0].Value;
      }

      public double recLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[1] != double.NaN, () => "result not available");
         return legNPV_[1].Value;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         Arguments arguments = (Arguments)args;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type");

         arguments.currency = currency_;
         arguments.paySpread = paySpread_;
         arguments.recSpread = recSpread_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);

         Results results = (Results)r;
         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         fairRecLegSpread_ = results.fairRecLegSpread;
         fairPayLegSpread_ = results.fairPayLegSpread;
      }

   }
}
