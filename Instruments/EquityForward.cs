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
   public class EquityForward : Instrument
   {



      public class Arguments : IPricingEngineArguments
      {
         public string name;
         public Currency currency;
         public Position.Type longShort;
         public double quantity;
         public Date maturityDate;
         public double strike;
         public void validate()
         {
            Utils.QL_REQUIRE(quantity > 0, () => "quantity should be positive: " + quantity);
            Utils.QL_REQUIRE(strike > 0, () => "strike should be positive: " + strike);
         }
      }

      public class Engine : GenericEngine<Arguments, Results>
      {
      }





      // data members
      string name_;
      Currency currency_;
      Position.Type longShort_;
      double quantity_;
      Date maturityDate_;
      double strike_;





      public EquityForward(string name, Currency currency, Position.Type longShort,
                                       double quantity, Date maturityDate, double strike)
      {
         name_ = name;
         currency_ = currency;
         longShort_ = longShort;
         quantity_ = quantity;
         maturityDate_ = maturityDate;
         strike_ = strike;
      }



      public string name() { return name_; }
      public Currency currency() { return currency_; }
      public Position.Type longShort() { return longShort_; }
      public double quantity() { return quantity_; }
      public Date maturityDate() { return maturityDate_; }
      public double strike() { return strike_; }
      public IPricingEngine engine() { return engine_; }

      public override bool isExpired()
      {
         return new QLNet.simple_event(maturityDate_).hasOccurred();
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         Arguments arguments = (Arguments)args;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type in equityforward");
         arguments.name = name_;
         arguments.currency = currency_;
         arguments.longShort = longShort_;
         arguments.quantity = quantity_;
         arguments.maturityDate = maturityDate_;
         arguments.strike = strike_;
      }


   }
}
