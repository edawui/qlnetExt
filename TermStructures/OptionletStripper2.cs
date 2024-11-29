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
   /*! Helper class to extend an OptionletStripper1 object stripping
    additional optionlet (i.e. caplet/floorlet) volatilities (a.k.a.
    forward-forward volatilities) from the (cap/floor) At-The-Money
    term volatilities of a CapFloorTermVolCurve.
*/
   public class OptionletStripper2 : OptionletStripper
   {

      public class ObjectiveFunction:ISolver1d
      {
         SimpleQuote spreadQuote_;
         CapFloor cap_;
         double targetValue_;
         Handle<YieldTermStructure> discount_;


         public ObjectiveFunction(OptionletStripper1 optionletStripper1, CapFloor cap,
      double targetValue, Handle<YieldTermStructure> discount)
         {
            cap_ = cap; targetValue_ = targetValue; discount_ = discount;

            OptionletVolatilityStructure adapter = new StrippedOptionletAdapter(optionletStripper1);

            // set an implausible value, so that calculation is forced
            // at first operator()(Volatility x) call
            spreadQuote_ = new SimpleQuote(-1.0);

            OptionletVolatilityStructure spreadedAdapter =
               new SpreadedOptionletVolatility(new Handle<OptionletVolatilityStructure>(adapter), new Handle<Quote>(spreadQuote_));

            // Use the same volatility type as optionletStripper1
            // Anything else would not make sense
            IPricingEngine engine = null;
            if (optionletStripper1.volatilityType() == VolatilityType.ShiftedLognormal)
            {
               engine = new BlackCapFloorEngine(
                   discount_, new Handle<OptionletVolatilityStructure>(spreadedAdapter), optionletStripper1.displacement());
            }
            else if (optionletStripper1.volatilityType() == VolatilityType.Normal)
            {
               engine = new BachelierCapFloorEngine(discount_, new Handle<OptionletVolatilityStructure>(spreadedAdapter));
            }
            else
            {
               Utils.QL_FAIL("Unknown volatility type: " + optionletStripper1.volatilityType());
            }

            cap_.setPricingEngine(engine);
         }

         public override double value(double s)
         {
            if (s != spreadQuote_.value())
               spreadQuote_.setValue(s);
            return cap_.NPV() - targetValue_;
         }

      }


      OptionletStripper1 stripper1_;
      Handle<CapFloorTermVolCurve> atmCapFloorTermVolCurve_;
      DayCounter dc_;
      int nOptionExpiries_;
      List<double> atmCapFloorStrikes_;
      List<double> atmCapFloorPrices_;
      List<double> spreadsVolImplied_;
      List<CapFloor> caps_;
      int maxEvaluations_;
      double accuracy_;
      VolatilityType inputVolatilityType_;
      double inputDisplacement_;



      public OptionletStripper2(OptionletStripper1 optionletStripper1,
                                        Handle<CapFloorTermVolCurve> atmCapFloorTermVolCurve,
                                        VolatilityType type, double displacement)
    : base(optionletStripper1.termVolSurface(), optionletStripper1.iborIndex(),
                        optionletStripper1.discountCurve(), optionletStripper1.volatilityType(),
                        optionletStripper1.displacement())
      {


         stripper1_ = optionletStripper1; atmCapFloorTermVolCurve_ = atmCapFloorTermVolCurve;
         dc_ = stripper1_.termVolSurface().dayCounter(); nOptionExpiries_ = atmCapFloorTermVolCurve.currentLink().optionTenors().Count;
         atmCapFloorStrikes_ = new List<double>(nOptionExpiries_); atmCapFloorPrices_ = new List<double>(nOptionExpiries_);
         spreadsVolImplied_ = new List<double>(nOptionExpiries_);
         caps_ = new List<CapFloor>(nOptionExpiries_); maxEvaluations_ = 10000; accuracy_ = 1.0e-6; inputVolatilityType_ = type;
         inputDisplacement_ = displacement;


         stripper1_.registerWith(update);
         atmCapFloorTermVolCurve_.registerWith(update);

         Utils.QL_REQUIRE(dc_ == atmCapFloorTermVolCurve.currentLink().dayCounter(), () => "different day counters provided");
      }

      protected override void performCalculations()
      {

         // optionletStripper data
         optionletDates_ = stripper1_.optionletFixingDates();
         optionletPaymentDates_ = stripper1_.optionletPaymentDates();
         optionletAccrualPeriods_ = stripper1_.optionletAccrualPeriods();
         optionletTimes_ = stripper1_.optionletFixingTimes();
         atmOptionletRate_ = stripper1_.atmOptionletRates();
         for (int i = 0; i < optionletTimes_.Count; ++i)
         {
            optionletStrikes_[i] = stripper1_.optionletStrikes(i);
            optionletVolatilities_[i] = stripper1_.optionletVolatilities(i);
         }

         // atmCapFloorTermVolCurve data
         List<Period> optionExpiriesTenors = atmCapFloorTermVolCurve_.currentLink().optionTenors();
         List<double> optionExpiriesTimes = atmCapFloorTermVolCurve_.currentLink().optionTimes();

         // discount curve
         Handle<YieldTermStructure> discountCurve =
             discount_.empty() ? iborIndex_.forwardingTermStructure() : discount_;

         for (int j = 0; j < nOptionExpiries_; ++j)
         {
            // Dummy strike, doesn't get used for ATM curve
            double atmOptionVol = atmCapFloorTermVolCurve_.currentLink().volatility(optionExpiriesTimes[j], 33.3333);

            // Create a cap for each pillar point on ATM curve and attach relevant pricing engine i.e. Black if
            // quotes are shifted lognormal and Bachelier if quotes are normal
            IPricingEngine engine = null;
            if (inputVolatilityType_ == VolatilityType.ShiftedLognormal)
            {
               engine = new BlackCapFloorEngine(discountCurve, atmOptionVol, dc_, inputDisplacement_);
            }
            else if (inputVolatilityType_ == VolatilityType.Normal)
            {
               engine = new BachelierCapFloorEngine(discountCurve, atmOptionVol, dc_);
            }
            else
            {
               Utils.QL_FAIL("unknown volatility type: " + volatilityType_);
            }

            // Using Null<double>() as strike => strike will be set to ATM rate. However, to calculate ATM rate, QL requires
            // a BlackCapFloorEngine to be set (not a BachelierCapFloorEngine)! So, need a temp BlackCapFloorEngine with a
            // dummy vol to calculate ATM rate. Needs to be fixed in QL.
            IPricingEngine tempEngine = new BlackCapFloorEngine(discountCurve, 0.01);
            caps_[j] = new MakeCapFloor(CapFloorType.Cap, optionExpiriesTenors[j], iborIndex_, double.NaN, new Period(0, TimeUnit.Days))
                                   .withPricingEngine(tempEngine);

            // Now set correct engine and get the ATM rate and the price
            caps_[j].setPricingEngine(engine);
            atmCapFloorStrikes_[j] = caps_[j].atmRate(discountCurve);
            atmCapFloorPrices_[j] = caps_[j].NPV();
         }

         spreadsVolImplied_ = spreadsVolImplied(discountCurve);

         StrippedOptionletAdapter adapter = new StrippedOptionletAdapter(stripper1_);

         double unadjustedVol, adjustedVol;
         for (int j = 0; j < nOptionExpiries_; ++j)
         {
            for (int i = 0; i < optionletVolatilities_.Count; ++i)
            {
               if (i <= caps_[j].floatingLeg().Count)
               {
                  unadjustedVol = adapter.volatility(optionletTimes_[i], atmCapFloorStrikes_[j]);
                  adjustedVol = unadjustedVol + spreadsVolImplied_[j];

                  // insert adjusted volatility
                  //List<double>::_iterator previous =
                  //  lower_bound(optionletStrikes_[i].begin(), optionletStrikes_[i].end(), atmCapFloorStrikes_[j]);
                  int previous = optionletStrikes_[i].IndexOf(atmCapFloorStrikes_[j]);
                  int insertIndex = previous - optionletStrikes_[i].Count;

                  optionletStrikes_[i].Insert(insertIndex, atmCapFloorStrikes_[j]);
                  optionletVolatilities_[i].Insert(insertIndex, adjustedVol);
               }
            }
         }
      }

      public List<double> spreadsVolImplied(Handle<YieldTermStructure> discount)
      {

         Brent solver = new Brent();
         List<double> result = new List<double>(nOptionExpiries_);
         double guess = 0.0001, minSpread = -0.1, maxSpread = 0.1;
         for (int j = 0; j < nOptionExpiries_; ++j)
         {
            ObjectiveFunction f = new ObjectiveFunction(stripper1_, caps_[j], atmCapFloorPrices_[j], discount);
            solver.setMaxEvaluations(maxEvaluations_);
            double root = solver.solve(f, accuracy_, guess, minSpread, maxSpread);
            result[j] = root;
         }
         return result;
      }

      public List<double> spreadsVol()
      {
         calculate();
         return spreadsVolImplied_;
      }

      public List<double> atmCapFloorStrikes()
      {
         calculate();
         return atmCapFloorStrikes_;
      }

      public List<double> atmCapFloorPrices()
      {
         calculate();
         return atmCapFloorPrices_;
      }

   }
}
