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

/*! \file fxbsparametrization.hpp
    \brief FX Black Scholes parametrization
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

   //! FX Black Scholes parametrizations
   /*! Base class for FX Black Scholes parametrizations
       \ingroup models
   */
   public abstract class FxBsParametrization : Parametrization
   {

      /*! The currency refers to the foreign currency, the spot
          is as of today (i.e. the discounted spot) */
      public FxBsParametrization(Currency foreignCurrency, Handle<Quote> fxSpotToday)
       : base(foreignCurrency)
      {
         fxSpotToday_ = fxSpotToday;
      }

      /*! must satisfy variance(0) = 0.0, variance'(t) >= 0 */
      public abstract double variance(double t);
      //public virtual double variance(double t)
      //{ }

      /*! is supposed to be positive */
      public virtual double sigma(double t)
      {
         return System.Math.Sqrt((variance(tr(t)) - variance(tl(t))) / h_);
               }

      public virtual double stdDeviation(double t)
      {
         return System.Math.Sqrt(variance(t));
      }

      public Handle<Quote> fxSpotToday()
      { return fxSpotToday_; }

      // public override void update()
      //public override void update()
      //{
      //   throw new NotImplementedException();
      //}

      private Handle<Quote> fxSpotToday_;
   }

   // inline

} // namespace QuantExt
