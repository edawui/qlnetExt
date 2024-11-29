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

   //! Converts OptionletVolatilityStructure with fixed reference date into a floating reference date term structure.
   /*! Different ways of reacting to time decay can be specified.

       \warning No checks are performed that the supplied OptionletVolatilityStructure has a fixed reference date

           \ingroup termstructures
   */


   public class DynamicOptionletVolatilityStructure : OptionletVolatilityStructure
   {

      OptionletVolatilityStructure source_;
      DynamicsType.ReactionToTimeDecay decayMode_;
      Date originalReferenceDate_;
      VolatilityType volatilityType_;
      double displacement_;



      DynamicOptionletVolatilityStructure(OptionletVolatilityStructure source, int settlementDays, Calendar calendar,
    DynamicsType.ReactionToTimeDecay decayMode)
    : base(settlementDays, calendar, source.businessDayConvention(), source.dayCounter())
      {
         source_ = source; decayMode_ = decayMode; originalReferenceDate_ = source.referenceDate();
         volatilityType_ = source.volatilityType(); displacement_ = source.displacement();

         // Set extrapolation to source's extrapolation initially
         enableExtrapolation(source.allowsExtrapolation());
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

      protected override SmileSection smileSectionImpl(double optionTime)
      {
         // Again, what strikes do we chose? Should not need this in any case.
         Utils.QL_FAIL("Smile section not implemented for DynamicOptionletVolatilityStructure");
         return null;
      }

      protected override double volatilityImpl(double optionTime, double strike)
      {
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ConstantVariance)
         {
            return source_.volatility(optionTime, strike);


         }

         // TODO: check validity of ForwardVariance option before using it.
         Utils.QL_REQUIRE(decayMode_ != DynamicsType.ReactionToTimeDecay.ForwardForwardVariance, () => "ForwardVariance not yet supported for DynamicOptionletVolatilityStructure");
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            double timeToRef = source_.timeFromReference(referenceDate());
            double varToRef = source_.blackVariance(timeToRef, strike);
            double varToOptTime = source_.blackVariance(timeToRef + optionTime, strike);
            return System.Math.Sqrt((varToOptTime - varToRef) / optionTime);
         }

         Utils.QL_FAIL("Unexpected decay mode (" + decayMode_ + ")");

         return double.NaN;
      }




      public override VolatilityType volatilityType() { return volatilityType_; }

      public override double displacement() { return displacement_; }


   }
}
