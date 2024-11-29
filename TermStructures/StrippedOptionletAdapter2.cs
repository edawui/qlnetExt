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

/*! \file qle/termstructures/strippedoptionletadapter2.hpp
    \brief StrippedOptionlet Adapter (with a deeper update method)
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
   public class StrippedOptionletAdapter2 : OptionletVolatilityStructure//, public QuantLib::LazyObject {
   {
      StrippedOptionletBase optionletStripper_;
      int nInterpolations_;
      List<Interpolation> strikeInterpolations_;


      public StrippedOptionletAdapter2(StrippedOptionletBase s)
    : base(s.settlementDays(), s.calendar(), s.businessDayConvention(), s.dayCounter())
      {
         optionletStripper_ = s;
         nInterpolations_ = s.optionletMaturities();
         strikeInterpolations_ = new List<Interpolation>(nInterpolations_);


         optionletStripper_.registerWith(update);
      }


      public override void update()
      {
         optionletStripper_.update(); // just in case
         base.update();
      }

      public OptionletStripper optionletStripper()
      {
         return (OptionletStripper)optionletStripper_;
      }

      protected override SmileSection smileSectionImpl(double t)
      {
         List<double> optionletStrikes =
             optionletStripper_.optionletStrikes(0); // strikes are the same for all times ?!
         List<double> stddevs = new List<double>();
         for (int i = 0; i < optionletStrikes.Count; i++)
         {
            stddevs.Add(volatilityImpl(t, optionletStrikes[i]) * System.Math.Sqrt(t));
         }
         // Extrapolation may be a problem with splines, but since minStrike()
         // and maxStrike() are set, we assume that no one will use stddevs for
         // strikes outside these strikes
         CubicInterpolation.BoundaryCondition bc =
            (optionletStrikes.Count >= 4) ? CubicInterpolation.BoundaryCondition.Lagrange : CubicInterpolation.BoundaryCondition.SecondDerivative;
         InterpolatedSmileSection<Cubic> smileSection = new InterpolatedSmileSection<Cubic>(
          t, optionletStrikes, stddevs, double.NaN, new Cubic(CubicInterpolation.DerivativeApprox.Spline, false, bc, 0.0, bc, 0.0), new Actual365Fixed(), volatilityType(), displacement());

         return smileSection;
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

         //  List<double >& atmForward = optionletStripper_.atmOptionletRate();
         //  List<double>& optionletTimes = optionletStripper_.optionletTimes();

         for (int i = 0; i < nInterpolations_; ++i)
         {
            List<double> optionletStrikes = optionletStripper_.optionletStrikes(i);
            List<double> optionletVolatilities = optionletStripper_.optionletVolatilities(i);
            // strikeInterpolations_[i] = boost::shared_ptr<SABRInterpolation>(new
            //            SABRInterpolation(optionletStrikes.begin(), optionletStrikes.end(),
            //                              optionletVolatilities.begin(),
            //                              optionletTimes[i], atmForward[i],
            //                              0.02,0.5,0.2,0.,
            //                              false, true, false, false
            //                              //alphaGuess_, betaGuess_,
            //                              //nuGuess_, rhoGuess_,
            //                              //isParameterFixed_[0],
            //                              //isParameterFixed_[1],
            //                              //isParameterFixed_[2],
            //                              //isParameterFixed_[3]
            //                              ////,
            //                              //vegaWeightedSmileFit_,
            //                              //endCriteria_,
            //                              //optMethod_
            //                              ));
            strikeInterpolations_[i] = new LinearInterpolation(optionletStrikes, optionletStrikes.Count, optionletVolatilities);

            // QL_ENSURE(strikeInterpolations_[i].endCriteria()!=EndCriteria::MaxIterations,
            //          "section calibration failed: "
            //          "option time " << optionletTimes[i] <<
            //          ": " <<
            //              ", alpha " <<  strikeInterpolations_[i].alpha()<<
            //              ", beta "  <<  strikeInterpolations_[i].beta() <<
            //              ", nu "    <<  strikeInterpolations_[i].nu()   <<
            //              ", rho "   <<  strikeInterpolations_[i].rho()  <<
            //              ", error " <<  strikeInterpolations_[i].interpolationError()
            //              );
         }
      }

      public override double minStrike()
      {
         return optionletStripper_.optionletStrikes(0).First(); // FIX
      }

      public override double maxStrike()
      {
         return optionletStripper_.optionletStrikes(0).First(); // FIX
      }

      public override Date maxDate() { return optionletStripper_.optionletFixingDates().Last(); }

      public override VolatilityType volatilityType() { return optionletStripper_.volatilityType(); }

      public override double displacement() { return optionletStripper_.displacement(); }
   }

}

