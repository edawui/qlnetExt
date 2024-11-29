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


/*! \file qle/termstructures/swaptionvolatilityconverter.hpp
    \brief Convert swaption volatilities from one type to another
    \ingroup termstructures
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{


   //! Container for holding swap conventions needed by the SwaptionVolatilityConverter
   public class SwapConventions
   {

      int settlementDays_;
      Period fixedTenor_;
      Calendar fixedCalendar_;
      BusinessDayConvention fixedConvention_;
      DayCounter fixedDayCounter_;
      IborIndex floatIndex_;


      //! Constructor
      public SwapConventions(int settlementDays, Period fixedTenor, Calendar fixedCalendar,
                    BusinessDayConvention fixedConvention, DayCounter fixedDayCounter,
                     IborIndex floatIndex)
      {
         settlementDays_ = settlementDays; fixedTenor_ = fixedTenor; fixedCalendar_ = fixedCalendar;
         fixedConvention_ = fixedConvention; fixedDayCounter_ = fixedDayCounter; floatIndex_ = floatIndex;
      }

      //! \name Inspectors
      //@{
      public int settlementDays() { return settlementDays_; }
      public Period fixedTenor() { return fixedTenor_; }
      public Calendar fixedCalendar() { return fixedCalendar_; }
      public BusinessDayConvention fixedConvention() { return fixedConvention_; }
      public DayCounter fixedDayCounter() { return fixedDayCounter_; }
      public IborIndex floatIndex() { return floatIndex_; }
      //@}

   }


   public class SwaptionVolatilityConverter
   {
      Date asof_;
      SwaptionVolatilityStructure svsIn_;
      Handle<YieldTermStructure> discount_;
      SwapConventions conventions_;
      VolatilityType targetType_;
      Matrix targetShifts_;

      // Variables for implied volatility solver
      double accuracy_;
      int maxEvaluations_;
      static double minVol_ = 1.0e-7;


      static double maxVol_ = 10.0;




      public SwaptionVolatilityConverter(Date asof,
                                                                SwaptionVolatilityStructure svsIn,
                                                                Handle<YieldTermStructure> discount,
                                                                SwapConventions conventions,
                                                                VolatilityType targetType, Matrix targetShifts)
      {
         asof_ = asof; svsIn_ = svsIn; discount_ = discount; conventions_ = conventions; targetType_ = targetType;
         targetShifts_ = targetShifts; accuracy_ = 1.0e-5; maxEvaluations_ = 100;


         // Some checks
         checkInputs();
      }

      public SwaptionVolatilityConverter(Date asof,
                                                           SwaptionVolatilityStructure svsIn,
                                                           SwapIndex swapIndex,
                                                           VolatilityType targetType, Matrix targetShifts)
      {
         asof_ = asof; svsIn_ = svsIn; discount_ = swapIndex.discountingTermStructure();
         conventions_ = new SwapConventions(swapIndex.fixingDays(), swapIndex.fixedLegTenor(),
                                                          swapIndex.fixingCalendar(), swapIndex.fixedLegConvention(),
                                                          swapIndex.dayCounter(), swapIndex.iborIndex());

         targetType_ = targetType; targetShifts_ = targetShifts; accuracy_ = 1.0e-5; maxEvaluations_ = 100;


         // Some checks
         if (discount_.empty())
            discount_ = swapIndex.iborIndex().forwardingTermStructure();
         checkInputs();
      }

      private void checkInputs()
      {
         Utils.QL_REQUIRE(svsIn_.referenceDate() == asof_, () =>
                    "SwaptionVolatilityConverter requires the asof date and reference date to align");
         Utils.QL_REQUIRE(!discount_.empty() & discount_.currentLink().referenceDate() == asof_, () =>
                    "SwaptionVolatilityConverter requires a valid discount curve with reference date equal to asof date");
         Handle<YieldTermStructure> forwardCurve = conventions_.floatIndex().forwardingTermStructure();
         Utils.QL_REQUIRE(!forwardCurve.empty() & forwardCurve.currentLink().referenceDate() == asof_, () =>
                    "SwaptionVolatilityConverter requires a valid forward curve with reference date equal to asof date");
      }

      private SwaptionVolatilityStructure convert()
      {
         // If SwaptionVolatilityMatrix passed in
         SwaptionVolatilityMatrix svMatrix = (SwaptionVolatilityMatrix)(svsIn_);
         if (svMatrix != null)
            return convert(svMatrix);

         // If we get to here, then not supported
         Utils.QL_FAIL("SwaptionVolatilityConverter requires a SwaptionVolatilityMatrix as input");
         return null;
      }

      private SwaptionVolatilityStructure convert(SwaptionVolatilityMatrix svMatrix)
      {

         // Some aspects of original volatility structure that we will need
         DayCounter dayCounter = svMatrix.dayCounter();
         bool extrapolation = svMatrix.allowsExtrapolation();
         Calendar calendar = svMatrix.calendar();
         BusinessDayConvention bdc = svMatrix.businessDayConvention();

         List<Date> optionDates = svMatrix.optionDates();
         List<Period> optionTenors = svMatrix.optionTenors();
         List<Period> swapTenors = svMatrix.swapTenors();
         List<double> optionTimes = svMatrix.optionTimes();
         List<double> swapLengths = svMatrix.swapLengths();
         int nOptionTimes = optionTimes.Count;
         int nSwapLengths = swapLengths.Count;

         double dummyStrike = 0.0;
         double inVolatility = 0.0;
         VolatilityType inType = svMatrix.volatilityType();
         double inShift = 0.0;
         double targetShift = 0.0;

         // If target type is ShiftedLognormal and shifts are provided, check size
         if (targetType_ == VolatilityType.ShiftedLognormal & !targetShifts_.empty())
         {
            Utils.QL_REQUIRE(targetShifts_.rows() == nOptionTimes, () =>
                       "SwaptionVolatilityConverter: number of shift rows does not equal the number of option tenors");
            Utils.QL_REQUIRE(targetShifts_.columns() == nSwapLengths, () =>
                       "SwaptionVolatilityConverter: number of shift columns does not equal the number of swap tenors");
         }

         // Calculate the converted volatilities
         Matrix volatilities = new Matrix(nOptionTimes, nSwapLengths);
         for (int i = 0; i < nOptionTimes; ++i)
         {
            for (int j = 0; j < nSwapLengths; ++j)
            {
               inVolatility = svMatrix.volatility(optionTimes[i], swapLengths[j], dummyStrike);
               inShift = svMatrix.shift(optionTimes[i], swapLengths[j]);
               if (!targetShifts_.empty())
                  targetShift = targetShifts_[i, j];
               volatilities[i, j] = convert(optionDates[i], swapTenors[j], dayCounter, inVolatility, inType, targetType_,
                                            inShift, targetShift);
            }
         }

         //// Return the new swaption volatility matrix
         //if (calendar.empty() || optionTenors.empty())
         //{
         //   // Original matrix was created with fixed option dates
         //   return new SwaptionVolatilityMatrix(asof_, optionDates, swapTenors, volatilities, new Actual365Fixed(), extrapolation, targetType_, targetShifts_);
         //}
         //else
         //{
         //   return new SwaptionVolatilityMatrix(asof_, calendar, bdc, optionTenors, swapTenors, volatilities, new Actual365Fixed(), extrapolation, targetType_, targetShifts_);
         //}
         return null;
      }

      //// Ignore "warning C4996: 'Quantlib::Swaption::impliedVolatility': was declared deprecated"
      //#ifdef BOOST_MSVC
      //#pragma warning(push)
      //#pragma warning(disable : 4996)
      //#endif

      private double convert(Date expiry, Period swapTenor, DayCounter volDayCounter,
                                          double inVol, VolatilityType inType, VolatilityType outType, double inShift,
                                          double outShift)
      {

         // Create the underlying swap with fixed rate = fair rate
         // We rely on the fact that MakeVanillaSwap sets the fixed rate to the fair rate if it is left null in the ctor
         Date effectiveDate = conventions_.fixedCalendar().advance(expiry, conventions_.settlementDays(), TimeUnit.Days);
         IPricingEngine engine = new DiscountingSwapEngine(discount_);
         VanillaSwap swap = new MakeVanillaSwap(swapTenor, conventions_.floatIndex())
                                                   .withEffectiveDate(effectiveDate)
                                                   .withFixedLegTenor(conventions_.fixedTenor())
                                                   .withFixedLegDayCount(conventions_.fixedDayCounter())
                                                   .withFloatingLegSpread(0.0)
                                                   .withPricingEngine(engine);
         double atmRate = swap.fairRate();

         // Create the swaption
         Exercise exercise = new EuropeanExercise(expiry);
         Swaption swaption = new Swaption(swap, exercise, Settlement.Type.Physical);

         // Price the swaption with the input volatility
         IPricingEngine swaptionEngine;
         if (inType == VolatilityType.ShiftedLognormal)
         {
            swaptionEngine = new BlackSwaptionEngine(discount_, inVol, volDayCounter, inShift);
         }
         else
         {
            swaptionEngine = new BachelierSwaptionEngine(discount_, inVol, volDayCounter);
         }
         swaption.setPricingEngine(swaptionEngine);

         double impliedVol = 0.0;
         try
         {
            double npv = swaption.NPV();

            // Calculate guess for implied volatility solver
            double guess = 0.0;
            if (outType == VolatilityType.ShiftedLognormal)
            {
               Utils.QL_REQUIRE(atmRate + outShift > 0.0, () => "SwaptionVolatilityConverter: ATM rate + shift must be > 0.0");
               if (inType == VolatilityType.Normal)
                  guess = inVol / (atmRate + outShift);
               else
                  guess = inVol * (atmRate + inShift) / (atmRate + outShift);
            }
            else
            {
               if (inType == VolatilityType.Normal)
                  guess = inVol;
               else
                  guess = inVol * (atmRate + inShift);
            }

            // Note: In implying the volatility the volatility day counter is hardcoded to Actual365Fixed
            impliedVol = swaption.impliedVolatility(npv, discount_, guess, accuracy_, maxEvaluations_, minVol_, maxVol_,
                                                     outShift, outType);
         }
         catch (Exception e)
         {
            // couldn't find implied volatility
            Utils.QL_FAIL("SwaptionVolatilityConverter: volatility conversion failed while trying to convert volatility" +
                    " for expiry "
                    + expiry + " and swap tenor " + swapTenor + ". Error: " + e.Message);
         }

         return impliedVol;
      }


   }
}
