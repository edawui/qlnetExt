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

/*! \file termstructures/dynamicblackvoltermstructure.hpp
    \brief dynamic black volatility term structure
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

   namespace Tag
   {
      public struct curve { };
      public struct surface { };
   }
   //public enum Tag { Curve, Surface };


   public class DynamicBlackVolTermStructure<T> : BlackVolTermStructure where T : struct
   {


      //! Takes a BlackVolTermStructure with fixed reference date and turns it into a floating reference date term structure.
      /*! This class takes a BlackVolTermStructure with fixed reference date
          and turns it into a floating reference date term structure.
          There are different ways of reacting to time decay that can be
          specified. As an additional feature, the class will return the
          ATM volatility if a null strike is given (currently, for this
          extrapolation must be allowed, since there is a check in
          VolatilityTermStructure we can no extend or bypass). ATM is
          defined as the forward level here (which is of particular
          interest for FX term structures).

          if curve is specified, a more efficient implementation for
          variance and volatility is used just passing through the
          given strike to the source term structure; note that in this
          case a null strike will not be converted to atm though.

              \ingroup termstructures
      */
      //template<typename mode = tag::surface> class DynamicBlackVolTermStructure : public BlackVolTermStructure {
      //public:
      /* For a stickyness that involves ATM calculations, the yield term
         structures and the spot (as of today, i.e. without settlement lag)
         must be given. They are also required if an ATM volatility with null
         strike is requested. The termstructures are expected to have a
         floating reference date consistent with the spot.
         Since we have to store the initial forward curve at ruction,
         we sample it on a grid that can be customized here, too. The curve
         is then linearly interpolated and extrapolated flat after the
         last grid point. */


      Handle<BlackVolTermStructure> source_;
      DynamicsType.ReactionToTimeDecay decayMode_;
      DynamicsType.Stickyness stickyness_;
      Handle<YieldTermStructure> riskfree_, dividend_;
      Handle<Quote> spot_;
      Date originalReferenceDate_;
      bool atmKnown_;
      List<double> forwardCurveSampleGrid_, initialForwards_;
      Interpolation initialForwardCurve_;




      public DynamicBlackVolTermStructure(Handle<BlackVolTermStructure> source, int settlementDays,
                                  Calendar calendar, DynamicsType.ReactionToTimeDecay decayMode = DynamicsType.ReactionToTimeDecay.ConstantVariance,
                                 DynamicsType.Stickyness stickyness = DynamicsType.Stickyness.StickyLogMoneyness)
            : this(source, settlementDays, calendar, decayMode, stickyness, new Handle<YieldTermStructure>(), new Handle<YieldTermStructure>(),
                                      new Handle<Quote>(), new List<double>())
      { }


      //  protected:
      ///* BlackVolTermStructure interface */
      //double blackVarianceImpl(Time t, double strike);
      //  Volatility blackVolImpl(Time t, double strike);
      //  /* immplementations for curve and surface tags */
      //  double blackVarianceImplTag(Time t, double strike, tag::curve);
      //  double blackVarianceImplTag(Time t, double strike, tag::surface);



      public DynamicBlackVolTermStructure(Handle<BlackVolTermStructure> source, int settlementDays,
                                  Calendar calendar, DynamicsType.ReactionToTimeDecay decayMode// = DynamicsType.ReactionToTimeDecay.ConstantVariance,
                                , DynamicsType.Stickyness stickyness// = DynamicsType.Stickyness.StickyLogMoneyness,
                                 , Handle<YieldTermStructure> riskfree,
                                  Handle<YieldTermStructure> dividend,
                                  Handle<Quote> spot,
                                  List<double> forwardCurveSampleGrid) :
      base(settlementDays, calendar, source.currentLink().businessDayConvention(), source.currentLink().dayCounter())

      {

         source_ = source; decayMode_ = decayMode; stickyness_ = stickyness; riskfree_ = riskfree; dividend_ = dividend;
         spot_ = spot; originalReferenceDate_ = source.currentLink().referenceDate();
         atmKnown_ = !riskfree.empty() && !dividend.empty() && !spot.empty();
         forwardCurveSampleGrid_ = forwardCurveSampleGrid;


         Utils.QL_REQUIRE(stickyness == DynamicsType.Stickyness.StickyStrike || stickyness == DynamicsType.Stickyness.StickyLogMoneyness, () => "stickyness (" + stickyness
                                                                                                  + ") not supported");
         Utils.QL_REQUIRE(decayMode == DynamicsType.ReactionToTimeDecay.ConstantVariance || decayMode == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance,
                    () => "reaction to time decay (" + decayMode + ") not supported");

         source.registerWith(update);// source);

         if (stickyness != DynamicsType.Stickyness.StickyStrike)
         {
            Utils.QL_REQUIRE(atmKnown_, () => "for stickyness other than strike, the term structures and spot must be given");
            Utils.QL_REQUIRE(riskfree_.currentLink().referenceDate() == source_.currentLink().referenceDate(),
                      () => "at ruction time the reference dates of the volatility term structure ("
                           + source.currentLink().referenceDate() + ") and the risk free yield term structure ("
                           + riskfree_.currentLink().referenceDate() + ") must be the same");
            Utils.QL_REQUIRE(dividend_.currentLink().referenceDate() == source_.currentLink().referenceDate(), () =>
                       "at ruction time the reference dates of the volatility term structure ("
                           + source.currentLink().referenceDate() + ") and the dividend term structure (" + riskfree_.currentLink().referenceDate()
                           + ") must be the same");
            riskfree_.registerWith(update);// (riskfree_);
            dividend_.registerWith(update);//(dividend_);
            spot_.registerWith(update);//(spot_);
         }

         if (atmKnown_)
         {
            if (forwardCurveSampleGrid_.Count == 0)
            {
               // use default grid
               double[] tmp = new double[]{ 0.0, 0.25, 0.5,  0.75, 1.0,  2.0,  3.0,  4.0,  5.0,  6.0,  7.0,
                           8.0, 9.0,  10.0, 12.0, 15.0, 20.0, 25.0, 30.0, 40.0, 50.0, 60.0 };
               forwardCurveSampleGrid_ = tmp.ToList();//Enumerable.Repeat(tmp, tmp.Length).ToList();// + sizeof(tmp) / sizeof(tmp[0]));
            }
            Utils.QL_REQUIRE(Utils.close_enough(forwardCurveSampleGrid_[0], 0.0), () => "forward curve sample grid must start at 0 ("
                                                                          + forwardCurveSampleGrid_[0]);
            initialForwards_.Resize(forwardCurveSampleGrid_.Count);
            for (int i = 1; i < forwardCurveSampleGrid_.Count; ++i)
            {
               Utils.QL_REQUIRE(forwardCurveSampleGrid_[i] > forwardCurveSampleGrid_[i - 1], () =>
                          "forward curve sample grid must have increasing times (at "
                              + (i - 1) + ", " + i + ": " + forwardCurveSampleGrid_[i - 1] + ", "
                              + forwardCurveSampleGrid_[i]);
            }
            for (int i = 0; i < forwardCurveSampleGrid_.Count; ++i)
            {
               double t = forwardCurveSampleGrid_[i];
               initialForwards_[i] = spot_.currentLink().value() / riskfree_.currentLink().discount(t) * dividend_.currentLink().discount(t);
            }

            // initialForwardCurve_ = new FlatExtrapolation(LinearInterpolation)(
            //    forwardCurveSampleGrid_.begin(), forwardCurveSampleGrid_.end(), initialForwards_.begin()));
            // LinearInterpolation temp = new LinearInterpolation(forwardCurveSampleGrid_, forwardCurveSampleGrid_.Count, initialForwards_);

            initialForwardCurve_ = new FlatExtrapolation(new LinearInterpolation(forwardCurveSampleGrid_, forwardCurveSampleGrid_.Count, initialForwards_));

            initialForwardCurve_.enableExtrapolation();
         }
      }

      public override void update() { base.update(); }

      public override Date maxDate()
      {
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            return source_.currentLink().maxDate();
         }

         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ConstantVariance)
         {
            return new Date(Math.Min(Date.maxDate().serialNumber(), referenceDate().serialNumber() -
                                                                     originalReferenceDate_.serialNumber() +
                                                                     source_.currentLink().maxDate().serialNumber()));
         }
         Utils.QL_FAIL("unexpected decay mode (" + decayMode_ + ")");
         return null;
      }

      public override double minStrike()
      {
         if (stickyness_ == DynamicsType.Stickyness.StickyStrike)
         {
            return source_.currentLink().minStrike();
         }
         if (stickyness_ == DynamicsType.Stickyness.StickyLogMoneyness)
         {
            // we do not specify this, since it is maturity dependent
            // instead we allow for extrapolation when asking the
            // source for a volatility and are not in sticky strike mode
            return 0.0;
         }
         Utils.QL_FAIL("unexpected stickyness (" + stickyness_ + ")");
         return double.NaN;
      }

      public override double maxStrike()
      {
         if (stickyness_ == DynamicsType.Stickyness.StickyStrike)
         {
            return source_.currentLink().maxStrike();
         }
         if (stickyness_ == DynamicsType.Stickyness.StickyLogMoneyness)
         {
            // see above
            return double.MaxValue;
         }
         Utils.QL_FAIL("unexpected stickyness (" + stickyness_ + ")");
         return double.NaN;
      }

      protected override double blackVolImpl(double t, double strike)
      {
         double tmp = Math.Max(1.0E-6, t);
         return System.Math.Sqrt(blackVarianceImpl(tmp, strike) / tmp);
      }

      protected override double blackVarianceImpl(double t, double strike)
      {

         if (mode() == typeof(Tag.surface))
         { return blackVarianceImplTagSurface(t, strike); }
         else if (mode() == typeof(Tag.curve))
         { return blackVarianceImplTagCurve(t, strike); }
         else
         { return double.NaN; }

      }

      private Type mode()
      {
         return typeof(T);

      }

      public double blackVarianceImplTagSurface(double t, double strike)//, DynamicBlackVolTermStructure.Tag mode)//surface
      {
         if (strike == double.NaN)
         {
            Utils.QL_REQUIRE(atmKnown_, () => "can not calculate atm level (null strike is given) because a curve or the spot is missing");
            strike = spot_.currentLink().value() / riskfree_.currentLink().discount(t) * dividend_.currentLink().discount(t);
         }
         double scenarioT0 = 0.0, scenarioT1 = t;
         double scenarioStrike0 = strike, scenarioStrike1 = strike;
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            scenarioT0 = source_.currentLink().timeFromReference(referenceDate());
            scenarioT1 = scenarioT0 + t;
         }
         if (stickyness_ == DynamicsType.Stickyness.StickyLogMoneyness)
         {
            double forward = spot_.currentLink().value() / riskfree_.currentLink().discount(t) * dividend_.currentLink().discount(t);
            scenarioStrike1 = initialForwardCurve_.value(scenarioT1) / forward * strike;
            scenarioStrike0 = initialForwardCurve_.value(scenarioT0) / spot_.currentLink().value() * strike;
         }
         return source_.currentLink().blackVariance(scenarioT1, scenarioStrike1, true) -
                source_.currentLink().blackVariance(scenarioT0, scenarioStrike0, true);
      }

      public double blackVarianceImplTagCurve(double t, double strike)//, DynamicBlackVolTermStructure.Tag mode)//curve
      {
         if (decayMode_ == DynamicsType.ReactionToTimeDecay.ForwardForwardVariance)
         {
            double scenarioT0 = source_.currentLink().timeFromReference(referenceDate());
            double scenarioT1 = scenarioT0 + t;
            return source_.currentLink().blackVariance(scenarioT1, strike, true) - source_.currentLink().blackVariance(scenarioT0, strike, true);
         }
         else
         {
            return source_.currentLink().blackVariance(t, strike, true);
         }
      }

   }
}
