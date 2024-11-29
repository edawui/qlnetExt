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
   public class DatedStrippedOptionletAdapter : OptionletVolatilityStructure
   {
      DatedStrippedOptionletBase optionletStripper_;
      int nInterpolations_;
      List<Interpolation> strikeInterpolations_;



      public DatedStrippedOptionletAdapter(DatedStrippedOptionletBase s)
    : base(s.referenceDate(), s.calendar(), s.businessDayConvention(), s.dayCounter())
      {
         optionletStripper_ = s;
         nInterpolations_ = s.optionletMaturities();
         strikeInterpolations_ = new List<Interpolation>(nInterpolations_);
         optionletStripper_.registerWith(update);
      }

      protected override SmileSection smileSectionImpl(double t)
      {
         Utils.QL_FAIL("Smile section not yet implemented for DatedStrippedOptionletAdapter");
         // Arbitrarily choose the first row of strikes for the smile section independent variable
         // Generally a reasonable choice since:
         // 1) OptionletStripper1: all strike rows are the same
         // 2) OptionletStripper2: optionletStrikes(i) is a decreasing sequence
         // Still possibility of arbitrary externally provided strike rows where (0) does not include all
         List<double> optionletStrikes = optionletStripper_.optionletStrikes(0);
         List<double> stdDevs = new List<double>(optionletStrikes.Count);
         for (int i = 0; i < optionletStrikes.Count; ++i)
            stdDevs[i] = volatilityImpl(t, optionletStrikes[i]) * Math.Sqrt(t);

         // Use a linear interpolated smile section.
         // TODO: possibly make this configurable?
         return new InterpolatedSmileSection<Linear>(t, optionletStrikes, stdDevs, double.NaN, new Linear(),
                                                                    new Actual365Fixed(), volatilityType(), displacement());
      }

      protected override double volatilityImpl(double length, double strike)
      {
         calculate();

         List<double> vol = new List<double>(nInterpolations_);
         for (int i = 0; i < nInterpolations_; ++i)
            vol[i] = strikeInterpolations_[i].value(strike, true);

         List<double> optionletTimes = optionletStripper_.optionletFixingTimes();
         LinearInterpolation timeInterpolator = new LinearInterpolation(optionletTimes, optionletTimes.Count, vol);
         return timeInterpolator.value(length, true);
      }

      protected override void performCalculations()
      {
         for (int i = 0; i < nInterpolations_; ++i)
         {
            List<double> optionletStrikes = optionletStripper_.optionletStrikes(i);
            List<double> optionletVolatilities = optionletStripper_.optionletVolatilities(i);
            strikeInterpolations_[i] = new LinearInterpolation(optionletStrikes, optionletStrikes.Count, optionletVolatilities);
         }
      }

      public override double minStrike()
      {
         double minStrike = optionletStripper_.optionletStrikes(0).First();
         for (int i = 1; i < nInterpolations_; ++i)
         {
            minStrike = Math.Min(optionletStripper_.optionletStrikes(i).First(), minStrike);
         }
         return minStrike;
      }

      public override double maxStrike()
      {
         double maxStrike = optionletStripper_.optionletStrikes(0).Last();
         for (int i = 1; i < nInterpolations_; ++i)
         {
            maxStrike = Math.Max(optionletStripper_.optionletStrikes(i).Last(), maxStrike);
         }
         return maxStrike;
      }

      public override Date maxDate() { return optionletStripper_.optionletFixingDates().Last(); }

      public override VolatilityType volatilityType() { return optionletStripper_.volatilityType(); }

      public override double displacement() { return optionletStripper_.displacement(); }


   }
}
