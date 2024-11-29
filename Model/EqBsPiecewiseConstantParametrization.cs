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

/*! \file eqbspiecewiseconstantparametrization.hpp
    \brief piecewise constant model parametrization
    \ingroup models
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{


   public class EqBsPiecewiseConstantParametrization : EqBsParametrization, IPiecewiseConstantHelper1
   {
      PiecewiseConstantHelper1 piecewiseConstantHelper1_;

      /*! The currency refers to the equity currency, the spots
          are as of today (i.e. the discounted spot) */
      public EqBsPiecewiseConstantParametrization(Currency currency, String eqName,
                                           Handle<Quote> eqSpotToday, Handle<Quote> fxSpotToday,
                                           Vector times, Vector sigma,
                                           Handle<YieldTermStructure> eqIrCurveToday,
                                           Handle<YieldTermStructure> eqDivYieldCurveToday)
      : base(currency, eqName, eqSpotToday, fxSpotToday, eqIrCurveToday, eqDivYieldCurveToday)
      {
         piecewiseConstantHelper1_ = new PiecewiseConstantHelper1(times);
         initialize(sigma);
      }
      /*! The term structure is needed in addition because it
          it's day counter and reference date is needed to
          convert dates to times. It should be the term structure
          of the domestic IR component in the cross asset model,
          since this is defining the model's date-time conversion
          in more general terms. */
      public EqBsPiecewiseConstantParametrization(Currency currency, String eqName,
                                                Handle<Quote> eqSpotToday, Handle<Quote> fxSpotToday,
                                                List<Date> dates, Vector sigma,
                                                Handle<YieldTermStructure> domesticTermStructure,
                                                Handle<YieldTermStructure> eqIrCurveToday,
                                                Handle<YieldTermStructure> eqDivYieldCurveToday)
            : base(currency, eqName, eqSpotToday, fxSpotToday, eqIrCurveToday, eqDivYieldCurveToday)
      {
         piecewiseConstantHelper1_ = new PiecewiseConstantHelper1(dates, domesticTermStructure);
         initialize(sigma);
      }


      public override double variance(double t)
      {
         return piecewiseConstantHelper1_.int_y_sqr(t);
      }
      public override double sigma(double t)
      {
         return piecewiseConstantHelper1_.y(t);

      }
      public override Vector parameterTimes(int i)
      {
         Utils.QL_REQUIRE(i == 0, () => "parameter " + i + " does not exist, only have 0");
         return piecewiseConstantHelper1_.t();
      }

      public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i == 0, () => "parameter " + i + " does not exist, only have 0");
         return piecewiseConstantHelper1_.p();

      }
      public override void update()
      {
         piecewiseConstantHelper1_.update();
      }


      protected override double direct(int i, double x)
      {
         return piecewiseConstantHelper1_.direct(x);
      }
      protected override double inverse(int i, double y)
      {
         return piecewiseConstantHelper1_.inverse(y);
      }

      private void initialize(Vector sigma)
      {
         Utils.QL_REQUIRE(piecewiseConstantHelper1_.t().Count + 1 == sigma.size(),
                  () => "alpha size (" + sigma.Count + ") inconsistent to times size ("
                                   + piecewiseConstantHelper1_.t().Count + ")");

         // store raw parameter values
         for (int i = 0; i < piecewiseConstantHelper1_.p().size(); ++i)
         {
            piecewiseConstantHelper1_.p().setParam(i, inverse(0, sigma[i]));
         }
         update();
      }

      public Vector t()
      {
         return piecewiseConstantHelper1_.t();
      }

      public Parameter p()
      {
         return piecewiseConstantHelper1_.p();
      }

      public double direct(double x)
      {
         return piecewiseConstantHelper1_.direct(x);
      }

      public double inverse(double y)
      {
         return piecewiseConstantHelper1_.inverse(y);
      }

      public double y(double t)
      {
         return piecewiseConstantHelper1_.y(t);
      }

      public double int_y_sqr(double t)
      {
         return piecewiseConstantHelper1_.int_y_sqr(t);

      }
   }
}
