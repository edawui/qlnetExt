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

/*! \file fxbspiecewiseconstantparametrization.hpp
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
{//! FX Black Scholes constant parametrization
 /*! FX Black Scholes parametrization with piecewise
     constant volatility
     \ingroup models
 */
   public class FxBsPiecewiseConstantParametrization : FxBsParametrization, IPiecewiseConstantHelper1
   {
      PiecewiseConstantHelper1 piecewiseConstantHelper1_;

      /*! The currency refers to the foreign currency, the spot
          is as of today (i.e. the discounted spot) */
      public FxBsPiecewiseConstantParametrization(Currency currency, Handle<Quote> fxSpotToday, Vector times,
                                            Vector sigma)
      : base(currency, fxSpotToday)
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
      public FxBsPiecewiseConstantParametrization(Currency currency, Handle<Quote> fxSpotToday,
                                           List<Date> dates, Vector sigma,
                                           Handle<YieldTermStructure> domesticTermStructure)
         : base(currency, fxSpotToday)
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

      //  protected:
      //Real direct(Size i, Real x) const;
      //  Real inverse(Size i, Real y) const;

      private void initialize(Vector sigma)
      {

         Utils.QL_REQUIRE(piecewiseConstantHelper1_.t().size() + 1 == sigma.size(),
              () => "alpha size (" + sigma.size() + ") inconsistent to times size ("
                                + piecewiseConstantHelper1_.t().size() + ")");

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
