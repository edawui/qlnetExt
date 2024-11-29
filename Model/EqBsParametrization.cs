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

/*! \file eqbsparametrization.hpp
    \brief EQ Black Scholes parametrization
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
   public abstract class EqBsParametrization : Parametrization
   {


      //! EQ Black Scholes parametrizations
      /*! Base class for EQ Black Scholes parametrizations
          \ingroup models
      */
      /*! The currency refers to the equity currency,
     the equity and fx spots are as of today
     (i.e. the discounted spot) */
      public EqBsParametrization(Currency eqCcy, string eqName, Handle<Quote> equitySpotToday,
                         Handle<Quote> fxSpotToday, Handle<YieldTermStructure> equityIrCurveToday,
                         Handle<YieldTermStructure> equityDivYieldCurveToday)
                        : base(eqCcy)
      {

         eqSpotToday_ = equitySpotToday;
         fxSpotToday_ = fxSpotToday;
         eqRateCurveToday_ = equityIrCurveToday;
         eqDivYieldCurveToday_ = equityDivYieldCurveToday;
         eqName_ = eqName;
      }

      /*! must satisfy variance(0) = 0.0, variance'(t) >= 0 */
      public virtual double sigma(double t)
      {
         return System.Math.Sqrt((variance(tr(t)) - variance(tl(t))) / h_);
      }

      public virtual double stdDeviation(double t)
      { return System.Math.Sqrt(variance(t)); }

      public virtual Handle<Quote> eqSpotToday()
      { return eqSpotToday_; }

      public virtual Handle<Quote> fxSpotToday() { return fxSpotToday_; }

      public virtual Handle<YieldTermStructure> equityIrCurveToday() { return eqRateCurveToday_; }

      public virtual Handle<YieldTermStructure> equityDivYieldCurveToday()
      {
         return eqDivYieldCurveToday_;
      }

      public string eqName() { return eqName_; }

      public override void update()
      {
         throw new NotImplementedException();
      }

      public abstract double variance(double t);

      Handle<Quote> eqSpotToday_; Handle<Quote> fxSpotToday_;
      Handle<YieldTermStructure> eqRateCurveToday_;
      Handle<YieldTermStructure> eqDivYieldCurveToday_;
      string eqName_;
   }

   // inline



} // namespace QuantExt
