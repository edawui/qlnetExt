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
   public class AnalyticXAssetLgmEquityOptionEngine : VanillaOption.Engine
   {
      CrossAssetModel model_;
      int eqIdx_;
      int ccyIdx_;

      public AnalyticXAssetLgmEquityOptionEngine(
    CrossAssetModel model, int eqName, int EqCcy)
      {
         model_ = model;
         eqIdx_ = eqName; ccyIdx_ = EqCcy;
      }

      public double value(double t0, double t, StrikedTypePayoff payoff, double discount, double eqForward)
      {

         int k = eqIdx_;
         int i = ccyIdx_;
         CrossAssetModel x = model_.get();

         double Hi_t = CrossAssetAnalytics.Utils.Hz.Helper(i).eval(x, t);

         // calculate the full variance. This is the equity analogy to eqn: 12.18 in Lichters,Stamm,Gallagher
         double variance = 0;
         variance += (CrossAssetAnalytics.Utils.vs.Helper(k).eval(x, t) - CrossAssetAnalytics.Utils.vs.Helper(k).eval(x, t0));

         variance += Hi_t * Hi_t * (CrossAssetAnalytics.Utils.zetaz.Helper(i).eval(x, t) - CrossAssetAnalytics.Utils.zetaz.Helper(i).eval(x, t0));
         variance -= 2.0 * Hi_t * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i), CrossAssetAnalytics.Utils.az.Helper(i), CrossAssetAnalytics.Utils.az.Helper(i)), t0, t);
         variance += CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i), CrossAssetAnalytics.Utils.Hz.Helper(i), CrossAssetAnalytics.Utils.az.Helper(i), CrossAssetAnalytics.Utils.az.Helper(i)), t0, t);

         variance += 2.0 * Hi_t * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.rzs.Helper(i, k), CrossAssetAnalytics.Utils.ss.Helper(k), CrossAssetAnalytics.Utils.az.Helper(i)), t0, t);
         variance -= 2.0 * CrossAssetAnalytics.Utils.integral(x, CrossAssetAnalytics.Utils.P(CrossAssetAnalytics.Utils.Hz.Helper(i), CrossAssetAnalytics.Utils.rzs.Helper(i, k), CrossAssetAnalytics.Utils.ss.Helper(k), CrossAssetAnalytics.Utils.az.Helper(i)), t0, t);

         double stdev = System.Math.Sqrt(variance);
         BlackCalculator black = new BlackCalculator(payoff, eqForward, stdev, discount);

         return black.value();
      }

      public override void calculate()
      {

         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European, () => "only European options are allowed");

         StrikedTypePayoff payoff = (StrikedTypePayoff)(arguments_.payoff);
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

         double divDiscount = model_.eqbs(eqIdx_).equityDivYieldCurveToday().currentLink().discount(expiry);
         double eqIrDiscount = model_.eqbs(eqIdx_).equityIrCurveToday().currentLink().discount(expiry);
         double cashflowsDiscount = model_.irlgm1f(ccyIdx_).termStructure().currentLink().discount(expiry);

         double eqForward = model_.eqbs(eqIdx_).eqSpotToday().currentLink().value() * divDiscount / eqIrDiscount;

         results_.value = value(0.0, t, payoff, cashflowsDiscount, eqForward);

      } // calculate()


   }
}
