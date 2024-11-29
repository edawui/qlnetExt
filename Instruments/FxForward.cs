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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class FxForward : Instrument
   {


      public class Arguments : IPricingEngineArguments
      {
         public
             double nominal1;
         public Currency currency1;
         public double nominal2;
         public Currency currency2;
         public Date maturityDate;
         public bool payCurrency1;
         public void validate()
         {
            Utils.QL_REQUIRE(nominal1 > 0.0, () => "nominal1  should be positive: " + nominal1);
            Utils.QL_REQUIRE(nominal2 > 0.0, () => "nominal2 should be positive: " + nominal2);
         }



      }

      public new class Results : Instrument.Results
      {
         public Money npv;
         public ExchangeRate fairForwardRate;
         public override void reset()
         {

            base.reset();

            npv = new Money(0.0, new Currency());
            fairForwardRate = new ExchangeRate();
         }
      };

     public class Engine : GenericEngine<Arguments, Results> { };






      double nominal1_;
      Currency currency1_;
      double nominal2_;
      Currency currency2_;
      Date maturityDate_;
      bool payCurrency1_;

      // results
      Money npv_;
      ExchangeRate fairForwardRate_;


      public double currency1Nominal() { return nominal1_; }
      public double currency2Nominal() { return nominal2_; }
      public Currency currency1() { return currency1_; }
      public Currency currency2() { return currency2_; }
      public Date maturityDate() { return maturityDate_; }
      public bool payCurrency1() { return payCurrency1_; }
      public IPricingEngine engine() { return engine_; }





      public FxForward(double nominal1, Currency currency1, double nominal2, Currency currency2,
                       Date maturityDate, bool payCurrency1)
      {

         nominal1_ = nominal1; currency1_ = currency1; nominal2_ = nominal2; currency2_ = currency2;
         maturityDate_ = maturityDate; payCurrency1_ = payCurrency1;

      }

      public FxForward(Money nominal1, ExchangeRate forwardRate, Date maturityDate,
                      bool sellingNominal)
      {
         nominal1_ = nominal1.value; currency1_ = nominal1.currency; maturityDate_ = maturityDate;
         payCurrency1_ = sellingNominal;


         Utils.QL_REQUIRE(currency1_ == forwardRate.target, () => "Currency of nominal1 does not match target (domestic) currency in the exchange rate.");

         Money otherNominal = forwardRate.exchange(nominal1);
         nominal2_ = otherNominal.value;
         currency2_ = otherNominal.currency;
      }

      public FxForward(Money nominal1, Handle<Quote> fxForwardQuote, Currency currency2,
                       Date maturityDate, bool sellingNominal)
      {
         nominal1_ = nominal1.value; currency1_ = nominal1.currency; currency2_ = currency2; maturityDate_ = maturityDate;
         payCurrency1_ = sellingNominal;


         Utils.QL_REQUIRE(fxForwardQuote.currentLink().isValid(), () => "The FX Forward quote is not valid.");

         nominal2_ = nominal1_ / fxForwardQuote.currentLink().value();
      }

      public override bool isExpired()
      {
         return new simple_event(maturityDate_).hasOccurred();
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         npv_ = new Money(0.0, new Currency());
         fairForwardRate_ = new ExchangeRate();
      }

      public override void setupArguments(IPricingEngineArguments args)
      {

         Arguments arguments = (Arguments)(args);

         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type in fxforward");

         arguments.nominal1 = nominal1_;
         arguments.currency1 = currency1_;
         arguments.nominal2 = nominal2_;
         arguments.currency2 = currency2_;
         arguments.maturityDate = maturityDate_;
         arguments.payCurrency1 = payCurrency1_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {

         base.fetchResults(r);

         Results results = (Results)r;

         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         npv_ = results.npv;
         fairForwardRate_ = results.fairForwardRate;
      }

   }
}
