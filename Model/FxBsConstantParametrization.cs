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

   //! FX Black Scholes parametrization
   /*! FX Black Scholes parametrization, with ant volatility
       \ingroup models
   */
   public abstract class FxBsConstantParametrization : FxBsParametrization
   {

      /*! The currency refers to the foreign currency, the
          spot is as of today (i.e. the discounted spot) */
      public FxBsConstantParametrization(Currency currency, Handle<Quote> fxSpotToday, double sigma)
         : base(currency, fxSpotToday)
      {
         //: FxBsParametrization(currency, fxSpotToday),
         sigma_ = new PseudoParameter(1);
         sigma_.setParam(0, inverse(0, sigma));
      }

      public override double variance(double t)
      {
         return direct(0, sigma_.parameters()[0]) * direct(0, sigma_.parameters()[0]) * t;

      }

      public override double sigma(double t)
      {
         return direct(0, sigma_.parameters()[0]);
      }


   //public virtual double sigma(double t);
   public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i == 0, () => "parameter " + i + " does not exist, only have 0");
         return sigma_;
      }


      protected override double direct(int i, double x)
      {
         return x * x;
      }

      protected override double inverse(int i, double y)
      {
         return System.Math.Sqrt(y);
      }

      private PseudoParameter sigma_;


   } // namespace QuantExt
}
