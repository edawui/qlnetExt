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
   //using CapFloorMatrix=  List<List<CapFloor>>;

   /*! Helper class to strip optionlet (i.e. caplet/floorlet) volatilities
 (a.k.a. forward-forward volatilities) from the (cap/floor) term
 volatilities of a CapFloorTermVolSurface.
*/
   public class OptionletStripper1 : OptionletStripper
   {


      Matrix capFloorPrices_, optionletPrices_;
      Matrix capFloorVols_;
      Matrix optionletStDevs_, capletVols_;

      List<List<CapFloor>> capFloors_;
      List<List<SimpleQuote>> volQuotes_;
      List<List<IPricingEngine>> capFloorEngines_;
      bool floatingSwitchStrike_;
      bool capFlooMatrixNotInitialized_;
      double switchStrike_;
      double accuracy_;
      int maxIter_;
      bool dontThrow_;
      VolatilityType inputVolatilityType_;
      double inputDisplacement_;


      public OptionletStripper1(CapFloorTermVolSurface termVolSurface,
                                        IborIndex index, double switchStrike, double accuracy,
                                       int maxIter, Handle<YieldTermStructure> discount,
                                        VolatilityType type, double displacement, bool dontThrow) : this(termVolSurface, index
                                           , switchStrike, accuracy, maxIter, discount, type, displacement, dontThrow, type, displacement)
      { }

      public OptionletStripper1(CapFloorTermVolSurface termVolSurface,
                                          IborIndex index, double switchStrike, double accuracy,
                                         int maxIter, Handle<YieldTermStructure> discount,
                                          VolatilityType type, double displacement, bool dontThrow,
                                          VolatilityType targetVolatilityType,// = VolatilityType.ShiftedLognormal,
                                          double targetDisplacement)//=0.0)
      : base(termVolSurface, index, discount, targetVolatilityType,//? targetVolatilityType : type,
                          targetDisplacement)// ? targetDisplacement : displacement)
      {
        
         volQuotes_ = Enumerable.Repeat(Enumerable.Repeat<SimpleQuote>(new SimpleQuote(), nStrikes_).ToList(), nOptionletTenors_).ToList();
         floatingSwitchStrike_ = switchStrike == double.NaN ? true : false; capFlooMatrixNotInitialized_ = true;
         switchStrike_ = switchStrike; accuracy_ = accuracy; maxIter_ = maxIter; dontThrow_ = dontThrow;
         inputVolatilityType_ = type; inputDisplacement_ = displacement;


         capFloorPrices_ = new Matrix(nOptionletTenors_, nStrikes_);
         optionletPrices_ = new Matrix(nOptionletTenors_, nStrikes_);
         capletVols_ = new Matrix(nOptionletTenors_, nStrikes_);
         capFloorVols_ = new Matrix(nOptionletTenors_, nStrikes_);

         double firstGuess = 0.14; // guess is only used for shifted lognormal vols
         optionletStDevs_ = new Matrix(nOptionletTenors_, nStrikes_, firstGuess);

         capFloors_ = new List<List<CapFloor>>(nOptionletTenors_);
         capFloorEngines_ = new List<List<IPricingEngine>>(nOptionletTenors_);
      }

      protected override void performCalculations()
      {

         // update dates
         Date referenceDate = termVolSurface_.referenceDate();
         DayCounter dc = termVolSurface_.dayCounter();
         BlackCapFloorEngine dummy = new BlackCapFloorEngine(//new BlackCapFloorEngine( // discounting does not matter here
                iborIndex_.forwardingTermStructure(), 0.20, dc);
         for (int i = 0; i < nOptionletTenors_; ++i)
         {
            CapFloor temp = new MakeCapFloor(CapFloorType.Cap, capFloorLengths_[i], iborIndex_,
                                         0.04// dummy strike
                                        , new Period(0, TimeUnit.Days))
                                .withPricingEngine(dummy);
            FloatingRateCoupon lFRC = temp.lastFloatingRateCoupon();
            optionletDates_[i] = lFRC.fixingDate();
            optionletPaymentDates_[i] = lFRC.date();
            optionletAccrualPeriods_[i] = lFRC.accrualPeriod();
            optionletTimes_[i] = dc.yearFraction(referenceDate, optionletDates_[i]);
            atmOptionletRate_[i] = lFRC.indexFixing();
         }

         if (floatingSwitchStrike_)
         {
            double averageAtmOptionletdouble = 0.0;
            for (int i = 0; i < nOptionletTenors_; ++i)
            {
               averageAtmOptionletdouble += atmOptionletRate_[i];
            }
            switchStrike_ = averageAtmOptionletdouble / nOptionletTenors_;
         }

         Handle<YieldTermStructure> discountCurve =
            discount_.empty() ? iborIndex_.forwardingTermStructure() : discount_;

         List<double> strikes = termVolSurface_.strikes();
         // initialize CapFloorMatrix
         if (capFlooMatrixNotInitialized_)
         {
            for (int i = 0; i < nOptionletTenors_; ++i)
            {
               capFloors_[i].Resize(nStrikes_);
               capFloorEngines_[i].Resize(nStrikes_);
            }
            // ruction might go here
            for (int j = 0; j < nStrikes_; ++j)
            {
               for (int i = 0; i < nOptionletTenors_; ++i)
               {
                  volQuotes_[i][j] = new SimpleQuote();
                  if (inputVolatilityType_ == VolatilityType.ShiftedLognormal)//ShiftedLognormal)
                  {
                     capFloorEngines_[i][j] = new BlackCapFloorEngine(
                         discountCurve, new Handle<Quote>(volQuotes_[i][j]), dc, inputDisplacement_);
                  }
                  else if (inputVolatilityType_ == VolatilityType.Normal)
                  {
                     capFloorEngines_[i][j] = new BachelierCapFloorEngine(discountCurve, new Handle<Quote>(volQuotes_[i][j]), dc);
                  }
                  else
                  {
                     Utils.QL_FAIL("unknown volatility type: " + volatilityType_);
                  }
               }
            }
            capFlooMatrixNotInitialized_ = false;
         }

         for (int j = 0; j < nStrikes_; ++j)
         {
            // using out-of-the-money options
            CapFloorType capFloorType = strikes[j] < switchStrike_ ? CapFloorType.Floor : CapFloorType.Cap;
            Option.Type optionletType = strikes[j] < switchStrike_ ? Option.Type.Put : Option.Type.Call;

            double previousCapFloorPrice = 0.0;
            for (int i = 0; i < nOptionletTenors_; ++i)
            {

               capFloorVols_[i, j] = termVolSurface_.volatility(capFloorLengths_[i], strikes[j], true);
               volQuotes_[i][j].setValue(capFloorVols_[i, j]);
               capFloors_[i][j] = new MakeCapFloor(capFloorType, capFloorLengths_[i], iborIndex_, strikes[j]
                                                   , new Period(-0, TimeUnit.Days))
                                             .withPricingEngine(capFloorEngines_[i][j]);
               capFloorPrices_[i, j] = capFloors_[i][j].NPV();
               optionletPrices_[i, j] = capFloorPrices_[i, j] - previousCapFloorPrice;
               previousCapFloorPrice = capFloorPrices_[i, j];
               double d = discountCurve.currentLink().discount(optionletPaymentDates_[i]);
               double optionletAnnuity = optionletAccrualPeriods_[i] * d;
               try
               {
                  if (volatilityType_ == VolatilityType.ShiftedLognormal)
                  {
                     optionletStDevs_[i, j] = Utils.blackFormulaImpliedStdDev(
                         optionletType, strikes[j], atmOptionletRate_[i], optionletPrices_[i, j], optionletAnnuity,
                         displacement_, optionletStDevs_[i, j], accuracy_, maxIter_);
                  }
                  else if (volatilityType_ == VolatilityType.Normal)
                  {
                     optionletStDevs_[i, j] =
                         System.Math.Sqrt(optionletTimes_[i]) *
                         Utils.bachelierBlackFormulaImpliedVol(optionletType, strikes[j], atmOptionletRate_[i],
                                                         optionletTimes_[i], optionletPrices_[i, j], optionletAnnuity);
                  }
                  else
                  {
                     Utils.QL_FAIL("Unknown target volatility type: " + volatilityType_);
                  }
               }
               catch (Exception e)
               {
                  if (dontThrow_)
                     optionletStDevs_[i, j] = 0.0;
                  else
                     Utils.QL_FAIL("could not bootstrap optionlet:" +
                             "\n type:    "
                             + optionletType + "\n strike:  " + strikes[j]
                             + "\n atm:     " + atmOptionletRate_[i]
                             + "\n price:   " + optionletPrices_[i, j] + "\n annuity: " + optionletAnnuity
                             + "\n expiry:  " + optionletDates_[i] + "\n error:   " + e.ToString());
               }
               optionletVolatilities_[i][j] = optionletStDevs_[i, j] / System.Math.Sqrt(optionletTimes_[i]);
            }
         }
      }


      public Handle<YieldTermStructure> discountCurve()  { return discount_; }

   public Matrix capletVols()
      {
         calculate();
         return capletVols_;
      }

      public Matrix capFloorPrices()
      {
         calculate();
         return capFloorPrices_;
      }

      public Matrix capFloorVolatilities()
      {
         calculate();
         return capFloorVols_;
      }

      public Matrix optionletPrices()
      {
         calculate();
         return optionletPrices_;
      }

      public double switchStrike()
      {
         if (floatingSwitchStrike_)
            calculate();
         return switchStrike_;
      }


   }
}
