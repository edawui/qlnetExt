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

/*! \file crossccybasisswap.hpp
    \brief Cross currency basis swap instrument

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
   public class CrossCcyBasisSwap : CrossCcySwap
   {




      public new class Arguments : CrossCcySwap.Arguments
      {
         public
             double paySpread;
         public

               double recSpread;

         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(paySpread != double.NaN, () => "Pay spread cannot be null");
            Utils.QL_REQUIRE(recSpread != double.NaN, () => "Rec spread cannot be null");
         }
      }


      public new class Results : CrossCcySwap.Results
      {
         public double fairPaySpread;
         public double fairRecSpread;
         public override void reset()
         {
            base.reset();
            fairPaySpread = double.NaN;
            fairRecSpread = double.NaN;
         }
      }



      double payNominal_;
      Currency payCurrency_;
      Schedule paySchedule_;
      IborIndex payIndex_;
      double paySpread_;

      double recNominal_;
      Currency recCurrency_;
      Schedule recSchedule_;
      IborIndex recIndex_;

      double recSpread_;

      double fairPaySpread_;
      double fairRecSpread_;




      public double payNominal() { return payNominal_; }
      public Currency payCurrency() { return payCurrency_; }
      public Schedule paySchedule() { return paySchedule_; }
      public IborIndex payIndex() { return payIndex_; }
      public double paySpread() { return paySpread_; }

      public double recNominal() { return recNominal_; }
      public Currency recCurrency() { return recCurrency_; }
      public Schedule recSchedule() { return recSchedule_; }
      public IborIndex recIndex() { return recIndex_; }
      public double recSpread() { return recSpread_; }
      //@}

      //! \name Additional interface
      //@{
      public double fairPaySpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairPaySpread_ != double.NaN, () => "Fair pay spread is not available");
         return fairPaySpread_;
      }
      public double fairRecSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairRecSpread_ != double.NaN, () => "Fair pay spread is not available");
         return fairRecSpread_;
      }






      public CrossCcyBasisSwap(double payNominal, Currency payCurrency, Schedule paySchedule,
                               IborIndex payIndex, double paySpread, double recNominal,
                               Currency recCurrency, Schedule recSchedule,
                               IborIndex recIndex, double recSpread)
          : base(2)
      {
         payNominal_ = payNominal; payCurrency_ = payCurrency; paySchedule_ = paySchedule;
         payIndex_ = payIndex; paySpread_ = paySpread; recNominal_ = recNominal; recCurrency_ = recCurrency;
         recSchedule_ = recSchedule; recIndex_ = recIndex; recSpread_ = recSpread;

         payIndex_.registerWith(update);
         recIndex_.registerWith(update);

         //registerWith(payIndex);
         //registerWith(recIndex);
         initialize();
      }

      private void initialize()
      {
         // Pay leg
         legs_[0] = ((IborLeg)(new IborLeg(paySchedule_, payIndex_).withNotionals(payNominal_))).withSpreads(paySpread_);
         payer_[0] = -1.0;
         currencies_[0] = payCurrency_;
         // Pay leg notional exchange at start.
         Date initialPayDate = paySchedule_.dates().First();
         CashFlow initialPayCF = (CashFlow)(new SimpleCashFlow(-payNominal_, initialPayDate));
         legs_[0].Insert(0, initialPayCF);
         // Pay leg notional exchange at end.
         Date finalPayDate = paySchedule_.dates().Last();
         CashFlow finalPayCF = (CashFlow)(new SimpleCashFlow(payNominal_, finalPayDate));
         legs_[0].Add(finalPayCF);

         // Receive leg
         legs_[1] = ((IborLeg)(new IborLeg(recSchedule_, recIndex_).withNotionals(recNominal_))).withSpreads(recSpread_);
         payer_[1] = +1.0;
         currencies_[1] = recCurrency_;
         // Receive leg notional exchange at start.
         Date initialRecDate = recSchedule_.dates().Last();
         CashFlow initialRecCF = (CashFlow)(new SimpleCashFlow(-recNominal_, initialRecDate));
         legs_[1].Insert(0, initialRecCF);
         // Receive leg notional exchange at end.
         Date finalRecDate = recSchedule_.dates().Last();
         CashFlow finalRecCF = (CashFlow)(new SimpleCashFlow(recNominal_, finalRecDate));
         legs_[1].Add(finalRecCF);

         // Register the instrument with all cashflows on each leg.
         for (int legNo = 0; legNo < 2; legNo++)
         {
            //Leg::iterator it;
            for (int it = 0; it < legs_[legNo].Count; ++it)//.begin(); it != legs_[legNo].end(); ++it)
            {
               legs_[legNo][it].registerWith(update);
               //registerWith(*it);
            }
         }
      }

      public override void setupArguments(IPricingEngineArguments args)
      {

         base.setupArguments(args);

         Arguments arguments = (Arguments)args;

         /* Returns here if e.g. args is CrossCcySwap::arguments which
            is the case if PricingEngine is a CrossCcySwap::engine. */
         if (arguments == null)
            return;

         arguments.paySpread = paySpread_;
         arguments.recSpread = recSpread_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {

         base.fetchResults(r);

         Results results = (Results)r;
         if (results != null)
         {
            /* If PricingEngine::results are of type
               results */
            fairPaySpread_ = results.fairPaySpread;
            fairRecSpread_ = results.fairRecSpread;
         }
         else
         {
            /* If not, e.g. if the engine is a CrossCcySwap::engine */
            fairPaySpread_ = double.NaN;
            fairRecSpread_ = double.NaN;
         }

         /* Calculate the fair pay and receive spreads if they are null */
         //static double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         if (fairPaySpread_ == double.NaN)
         {
            if (legBPS_[0] != double.NaN)
               fairPaySpread_ = paySpread_ - NPV_.Value / (legBPS_[0].Value / basisPoint);
         }
         if (fairRecSpread_ == double.NaN)
         {
            if (legBPS_[1] != double.NaN)
               fairRecSpread_ = recSpread_ - NPV_.Value / (legBPS_[1].Value / basisPoint);
         }
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         fairPaySpread_ = double.NaN;
         fairRecSpread_ = double.NaN;
      }

   }

}
