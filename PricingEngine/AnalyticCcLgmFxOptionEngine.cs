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

//using QLNetExt.CrossAssetAnalytics;

namespace QLNetExt
{

   public class AnalyticCcLgmFxOptionEngine : VanillaOption.Engine
   {


      CrossAssetModel model_;
      int foreignCurrency_;
      bool cacheEnabled_;
      bool cacheDirty_;
      double cachedIntegrals_;
      double cachedT0_;
      double cachedT_;

      public AnalyticCcLgmFxOptionEngine(CrossAssetModel model, int foreignCurrency)
      {
         model_ = model;
         foreignCurrency_ = foreignCurrency;
         cacheEnabled_ = false;
         cacheDirty_ = true;
      }

      public double value(double t0, double t, StrikedTypePayoff payoff,
                                          double domesticDiscount, double fxForward)
      {
         double H0 = CrossAssetAnalytics.Utils.Hz.Helper(0).eval(model_.get(), t);
         double Hi = CrossAssetAnalytics.Utils.Hz.Helper(foreignCurrency_ + 1).eval(model_.get(), t);

         // just a shortcut
         int i = foreignCurrency_;

         CrossAssetModel x = model_.get();

         if (cacheDirty_ || !cacheEnabled_ || !(QLNet.Utils.close_enough(cachedT0_, t0) && QLNet.Utils.close_enough(cachedT_, t)))
         {
            cachedIntegrals_ =
                // first term
                H0 * H0 * (CrossAssetAnalytics.Utils.zetaz.Helper(0).eval(x, t) - CrossAssetAnalytics.Utils.zetaz.Helper(0).eval(x, t0)) -
                2.0 * H0 * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0)), t0, t) + CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0)), t0, t) +
                // second term
                Hi * Hi * (CrossAssetAnalytics.Utils.zetaz.Helper(i + 1).eval(x, t) - CrossAssetAnalytics.Utils.zetaz.Helper(i + 1).eval(x, t0)) -
                2.0 * Hi * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1)), t0, t) +
                CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1)), t0, t) -
                // third term
                2.0 * (H0 * Hi * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.rzz.Helper(0, i + 1)), t0, t) -
                       H0 * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.rzz.Helper(i + 1, 0)), t0, t) -
                       Hi * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.rzz.Helper(0, i + 1)), t0, t) +
                       CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.rzz.Helper(0, i + 1)), t0, t));
            cacheDirty_ = false;
            cachedT0_ = t0;
            cachedT_ = t;
         }

         double variance = cachedIntegrals_ +
                        // term two three/fourth
                        (CrossAssetAnalytics.Utils.vx.Helper(i).eval(x, t) - CrossAssetAnalytics.Utils.vx.Helper(i).eval(x, t0)) +
                        // forth term
                        2.0 * (H0 * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.sx.Helper(i), CrossAssetAnalytics.Utils.rzx.Helper(0, i)), t0, t) -
                               CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(0), CrossAssetAnalytics.Utils.az.Helper(0), CrossAssetAnalytics.Utils.sx.Helper(i), CrossAssetAnalytics.Utils.rzx.Helper(0, i)), t0, t)) -
                        // fifth term
                        2.0 * (Hi * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.sx.Helper(i), CrossAssetAnalytics.Utils.rzx.Helper(i + 1, i)), t0, t) -
                               CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i + 1), CrossAssetAnalytics.Utils.az.Helper(i + 1), CrossAssetAnalytics.Utils.sx.Helper(i), CrossAssetAnalytics.Utils.rzx.Helper(i + 1, i)), t0, t));

         BlackCalculator black = new BlackCalculator(payoff, fxForward, System.Math.Sqrt(variance), domesticDiscount);

         return black.value();
      }

      public override void calculate()
      {

         QLNet.Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European, () => "only European options are allowed");

         StrikedTypePayoff payoff = (StrikedTypePayoff)arguments_.payoff;
         Utils.QL_REQUIRE(payoff != null, () => "only striked payoff is allowed");

         Date expiry = arguments_.exercise.lastDate();
         double t = model_.irlgm1f(0).termStructure().currentLink().timeFromReference(expiry);

         if (t <= 0.0)
         {
            // option is expired, we do not value any possibly non settled
            // flows, i.e. set the npv to zero in this case
            results_.value = 0.0;
            return;
         }

         double foreignDiscount = model_.irlgm1f(foreignCurrency_ + 1).termStructure().currentLink().discount(expiry);
         double domesticDiscount = model_.irlgm1f(0).termStructure().currentLink().discount(expiry);

         double fxForward = model_.fxbs(foreignCurrency_).fxSpotToday().currentLink().value() * foreignDiscount / domesticDiscount;

         results_.value = value(0.0, t, payoff, domesticDiscount, fxForward);

      } // calculate()

   }
}
