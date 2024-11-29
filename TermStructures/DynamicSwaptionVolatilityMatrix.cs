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

/*! \file termstructures/dynamicswaptionvolmatrix.hpp
    \brief dynamic swaption volatility matrix
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
   //! Takes a SwaptionVolatilityMatrix with fixed reference date and turns it into a floating reference date term
   // structure.
   /*! This class takes a SwaptionVolatilityMatrix with fixed
       reference date and turns it into a floating reference date
       term structure.
       There are different ways of reacting to time decay that can be
       specified.

           \ingroup termstructures
   */

   public class DynamicSwaptionVolatilityMatrix : SwaptionVolatilityStructure
   {
      SwaptionVolatilityStructure source_;
      DynamicsType.ReactionToTimeDecay decayMode_;
      Date originalReferenceDate_;
      VolatilityType volatilityType_;


      public DynamicSwaptionVolatilityMatrix(SwaptionVolatilityStructure source, int settlementDays, Calendar calendar,
     DynamicsType.ReactionToTimeDecay decayMode)
     : base(settlementDays, calendar, source.businessDayConvention(), source.dayCounter())
      {
         source_ = source; decayMode_ = decayMode; originalReferenceDate_ = source.referenceDate();
         volatilityType_ = source.volatilityType();
      }

      public override Period maxSwapTenor()
      {
         return source_.maxSwapTenor();
      }

      protected override SmileSection smileSectionImpl(double optionTime, double swapLength)
      {
         // dummy strike, just as in SwaptionVolatilityMatrix
         return new FlatSmileSection(optionTime, volatilityImpl(optionTime, swapLength, 0.05),
                                                     source_.dayCounter(), double.NaN, source_.volatilityType(),
                                                     shiftImpl(optionTime, swapLength));
      }

      protected override double volatilityImpl(double optionTime, double swapLength, double strike)
      {
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            double tf = source_.timeFromReference(referenceDate());
            if (source_.volatilityType() == VolatilityType.ShiftedLognormal)
            {
               Utils.QL_REQUIRE(Utils.close_enough(source_.shift(tf + optionTime, swapLength), source_.shift(tf, swapLength)),
                         () => "DynamicSwaptionVolatilityMatrix: Shift must be ant in option time direction");
            }
            return System.Math.Sqrt((source_.blackVariance(tf + optionTime, swapLength, strike) -
                              source_.blackVariance(tf, swapLength, strike)) /
                             optionTime);
         }
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ConstantVariance)
         {
            return source_.volatility(optionTime, swapLength, strike);
         }
         Utils.QL_FAIL("unexpected decay mode (" + decayMode_ + ")");
         return double.NaN;
      }

      protected override double shiftImpl(double optionTime, double swapLength)
      {
         if (source_.volatilityType() == VolatilityType.Normal)
         {
            return 0.0;
         }
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            double tf = source_.timeFromReference(referenceDate());
            return source_.shift(tf + optionTime, swapLength);
         }
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ConstantVariance)
         {
            return source_.shift(optionTime, swapLength);
         }
         Utils.QL_FAIL("unexpected decay mode (" + decayMode_ + ")");
         return double.NaN;
      }

      public override double minStrike() { return source_.minStrike(); }

      public override double maxStrike() { return source_.maxStrike(); }

      public override Date maxDate()
      {
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            return source_.maxDate();
         }
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ConstantVariance)
         {
            return new Date(System.Math.Min(Date.maxDate().serialNumber(), referenceDate().serialNumber() -
                                                                     originalReferenceDate_.serialNumber() +
                                                                     source_.maxDate().serialNumber()));
         }
         Utils.QL_FAIL("unexpected decay mode (" + decayMode_ + ")");
         return null;
      }

      public override void update() { base.update(); }



      public override VolatilityType volatilityType() { return volatilityType_; }


   }
}
