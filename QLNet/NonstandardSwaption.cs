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

namespace QLNet
{
   public class NonstandardSwaption : Option
   {


      //! %Arguments for nonstandard swaption calculation
      public new class Arguments : Option.Arguments//NonstandardSwap.Arguments// 
      {

         public NonstandardSwap swap;
         public Settlement.Type settlementType;
         public NonstandardSwap.Arguments NonstandardSwapArguments;


         public Arguments()
         { }

         public Arguments(NonstandardSwap.Arguments nonstandardSwapArguments)
         {
            NonstandardSwapArguments = nonstandardSwapArguments;
         }

         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(swap != null, () => "underlying non standard swap not set");
            Utils.QL_REQUIRE(exercise != null, () => "exercise not set");
         }
      }

      //! base class for nonstandard swaption engines
      public class Engine : GenericEngine<NonstandardSwaption.Arguments, NonstandardSwaption.Results>
      {

      }





      NonstandardSwap swap_;
      Settlement.Type settlementType_;

      public VanillaSwap.Type type()
      {
         return swap_.type();
      }

      public NonstandardSwap underlyingSwap()
      {
         return swap_;
      }

      public NonstandardSwaption(Swaption fromSwaption)
        : base(new Payoff(), fromSwaption.exercise())
      {
         swap_ = new NonstandardSwap(fromSwaption.underlyingSwap());
         settlementType_ = fromSwaption.settlementType();
         swap_.registerWith(update);
      }

      public NonstandardSwaption(
         NonstandardSwap swap,
         Exercise exercise, Settlement.Type delivery)
         : base(new Payoff(), exercise)
      {
         swap_ = swap;
         settlementType_ = delivery;
         swap_.registerWith(update);
      }

      public override bool isExpired()
      {

         return (new simple_event(exercise_.dates().Last())).hasOccurred();
      }

      public override void setupArguments(IPricingEngineArguments args)
      {

         swap_.setupArguments(args);
         NonstandardSwaption.Arguments arguments = (NonstandardSwaption.Arguments)args;
         // guments* arguments =
         //    dynamic_cast<arguments*>(args);

         Utils.QL_REQUIRE(arguments != null, () => "argument types do not match");

         arguments.swap = swap_;
         arguments.exercise = exercise_;
         arguments.settlementType = settlementType_;


      }

      public List<CalibrationHelper> calibrationBasket(
       SwapIndex standardSwapBase,
       SwaptionVolatilityStructure swaptionVolatility
       )//,  BasketGeneratingEngine.::CalibrationBasketType basketType)
      {
         //TODO BasketGeneratingEngine to implement in QLNet
         return null;
         /*
        boost::shared_ptr<BasketGeneratingEngine> engine =
            boost::dynamic_pointer_cast<BasketGeneratingEngine>(engine_);
        QL_REQUIRE(engine, "engine is not a basket generating engine");
engine_->reset();
        setupArguments(engine_->getArguments());
engine_->getArguments()->validate();
        return engine->calibrationBasket(exercise_, standardSwapBase,
                                         swaptionVolatility, basketType);

         */
      }
   }

}
