

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
   public class FxEqOptionHelper : CalibrationHelper
   {


      bool hasMaturity_;
      Period maturity_;
      Date exerciseDate_;
      Calendar calendar_;
      double strike_;
      Handle<Quote> spot_;
      Handle<YieldTermStructure> foreignYield_;
      double tau_;
      double atm_;
      Option.Type type_;
      VanillaOption option_;
      double effStrike_;



      public FxEqOptionHelper(Period maturity, Calendar calendar, double strike,
                                       Handle<Quote> spot, Handle<Quote> volatility,
                                       Handle<YieldTermStructure> domesticYield,
                                       Handle<YieldTermStructure> foreignYield,
                                      CalibrationHelper.CalibrationErrorType errorType)
       : base(volatility, domesticYield, errorType)
      {
         hasMaturity_ = true; maturity_ = maturity;




         calendar_ = calendar; strike_ = strike; spot_ = spot; foreignYield_ = foreignYield;

         spot_.registerWith(update);
         foreignYield_.registerWith(update);


         // registerWith(spot_);
         //registerWith(foreignYield_);
      }

      public FxEqOptionHelper(Date exerciseDate, double strike, Handle<Quote> spot,
                                     Handle<Quote> volatility, Handle<YieldTermStructure> domesticYield,
                                     Handle<YieldTermStructure> foreignYield,
                                    CalibrationHelper.CalibrationErrorType errorType)
     : base(volatility, domesticYield, errorType)
      {
         hasMaturity_ = false; exerciseDate_ = exerciseDate;
         strike_ = strike; spot_ = spot; foreignYield_ = foreignYield;
         spot_.registerWith(update);
         foreignYield_.registerWith(update);
         //registerWith(spot_);
         //registerWith(foreignYield_);
      }

      protected override void performCalculations()
      {
         if (hasMaturity_)
            exerciseDate_ = calendar_.advance(termStructure_.currentLink().referenceDate(), maturity_);
         tau_ = termStructure_.currentLink().timeFromReference(exerciseDate_);
         atm_ = spot_.currentLink().value() * foreignYield_.currentLink().discount(tau_) / termStructure_.currentLink().discount(tau_);
         effStrike_ = strike_;
         if (effStrike_ == double.NaN)//Null<Real>())
            effStrike_ = atm_;
         type_ = effStrike_ >= atm_ ? Option.Type.Call : Option.Type.Put;
         StrikedTypePayoff payoff = new PlainVanillaPayoff(type_, effStrike_);
         //StrikedTypePayoff payoff = new StrikedTypePayoff(type_, effStrike_);
         Exercise exercise = new EuropeanExercise(exerciseDate_);
         option_ = new VanillaOption(payoff, exercise);
         base.performCalculations();
      }

      public override double modelValue()
      {
         calculate();
         option_.setPricingEngine(engine_);
         return option_.NPV();
      }

      public override double blackPrice(double volatility)
      {
         calculate();
         double stdDev = volatility * System.Math.Sqrt(tau_);
         return Utils.blackFormula(type_, effStrike_, atm_, stdDev, termStructure_.currentLink().discount(tau_));
      }


      public override void addTimesTo(List<double> times)
      {
      }

   public VanillaOption option() { return option_; }
   

} // namespace QuantExt

}
