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
{

   //! EQ Black Scholes parametrization
   /*! EQ Black Scholes parametrization, with constant volatility
       \ingroup models
   */
   public class EqBsConstantParametrization : EqBsParametrization
   {

      PseudoParameter sigma_;


      /*! The currency refers to the equity currency, the
          spots are as of today (i.e. the discounted spot) */
      public EqBsConstantParametrization(Currency currency, String eqName, Handle<Quote> eqSpotToday,
                                      Handle<Quote> fxSpotToday, double sigma,
                                      Handle<YieldTermStructure> eqIrCurveToday,
                                      Handle<YieldTermStructure> eqDivYieldCurveToday)
      : base(currency, eqName, eqSpotToday, fxSpotToday, eqIrCurveToday, eqDivYieldCurveToday)
      {
         sigma_ = new PseudoParameter(1);
         sigma_.setParam(0, inverse(0, sigma));
      }


      protected override double direct(int t, double x)
      {
         return x * x;
      }

      protected override double inverse(int i, double y) { return System.Math.Sqrt(y); }

      public override double variance(double t)
      {
         return direct(0, sigma_.parameters()[0]) * direct(0, sigma_.parameters()[0]) * t;
      }

      public override double sigma(double t) { return direct(0, sigma_.parameters()[0]); }

      public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i == 0, () => "parameter " + i + " does not exist, only have 0");
         return sigma_;
      }




      // inline

   } // namespace QuantExt

}

