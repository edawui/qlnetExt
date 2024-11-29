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

/*! \file pricingengines/analyticlgmswaptionengine.hpp
    \brief analytic engine for european swaptions in the LGM model

        \ingroup engines
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using QLNet;

namespace QLNetExt.PricingEngine
{

   //! Analytic LGM swaption engine for european exercise
   /*! \ingroup swaptionengines

       All fixed coupons with start date greater or equal to the respective
       option expiry are considered to be
       part of the exercise into right.

       References:

       Hagan, Evaluating and hedging exotic swap instruments via LGM

       Lichters, Stamm, Gallagher: Modern Derivatives Pricing and Credit Exposure
       Analysis, Palgrave Macmillan, 2015, 11.2.2

       \warning Cash settled swaptions are not supported

       The basis between the given discounting curve (or - if not given - the
       model curve) and the forwarding curve attached to the underlying swap's
       ibor index is taken into account by a static correction spread for
       the underlying's fixed leg. Likewise a spread on the floating leg is
       taken into account.

       Note that we assume H' does not change its sign, but this is a general
       requirement of the LGM parametrization anyway (see the base parametrization
       class).

       \ingroup engines
   */

   public class AnalyticLgmSwaptionEngine : GenericEngine<Swaption.Arguments, Swaption.Results>
   {
      
      /*! nextCoupon is Mapping A, proRata is Mapping B
       in Lichters, Stamm, Gallagher (2015), 11.2.2 */
      public enum FloatSpreadMapping
      { nextCoupon, proRata };



      IrLgm1fParametrization p_;
      Handle<YieldTermStructure> c_;
      FloatSpreadMapping floatSpreadMapping_;
      double H0_; double D0_;
      double zetaex_; double S_m1;
      List<double> S_;
      List<double> Hj_;
      List<double> Dj_;
      int j1_;
      int k1_;



      public AnalyticLgmSwaptionEngine(LinearGaussMarkovModel model,
                                                     Handle<YieldTermStructure> discountCurve,
                                                     FloatSpreadMapping floatSpreadMapping)
    : base()
      {

         p_ = model.parametrization();
         c_ = discountCurve.empty() ? p_.termStructure() : discountCurve;
         floatSpreadMapping_ = floatSpreadMapping;

         model.registerWith(update);
         //p_.registerWith(update);
         c_.registerWith(update);
      }

      public AnalyticLgmSwaptionEngine(CrossAssetModel model, int ccy,
                                                     Handle<YieldTermStructure> discountCurve,
                                                     FloatSpreadMapping floatSpreadMapping)
    : base()
      {
         p_ = model.irlgm1f(ccy);

         c_ = discountCurve.empty() ? p_.termStructure() : discountCurve;
         floatSpreadMapping_ = floatSpreadMapping;

         model.registerWith(update);
         //p_.registerWith(update);
         c_.registerWith(update);
      }

      public AnalyticLgmSwaptionEngine(IrLgm1fParametrization irlgm1f,
                                                     Handle<YieldTermStructure> discountCurve,
                                                     FloatSpreadMapping floatSpreadMapping)
    : base()
      {
         p_ = irlgm1f;
         c_ = discountCurve.empty() ? p_.termStructure() : discountCurve;
         floatSpreadMapping_ = floatSpreadMapping;
         c_.registerWith(update);

      }

      public void calculate()
      {

         Utils.QL_REQUIRE(arguments_.settlementType == Settlement.Type.Physical, () => "cash-settled swaptions are not supported ...");

         Date reference = p_.termStructure().currentLink().referenceDate();

         Date expiry = arguments_.exercise.dates().Last();

         if (expiry <= reference)
         {
            // swaption is expired, possibly generated swap is not
            // valued by this engine, so we set the npv to zero
            results_.value = 0.0;
            return;
         }

         VanillaSwap swap = arguments_.swap;
         Option.Type type = arguments_.type == VanillaSwap.Type.Payer ? Option.Type.Call : Option.Type.Put;
         Schedule fixedSchedule = swap.fixedSchedule();
         Schedule floatSchedule = swap.floatingSchedule();

         j1_ = UtilsExt.LowerBound<Date>(fixedSchedule.dates(), expiry);//std::lower_bound(fixedSchedule.dates().begin(), fixedSchedule.dates().end(), expiry) -
                                                                        //fixedSchedule.dates().begin();
         k1_ = UtilsExt.LowerBound<Date>(floatSchedule.dates(), expiry);////std::lower_bound(floatSchedule.dates().begin(), floatSchedule.dates().end(), expiry) -
                                                                        //floatSchedule.dates().begin();

         // compute S_i, i.e. equivalent fixed rate spreads compensating for
         // a) a possibly non-zero float spread and
         // b) a spread between the ibor indices forwarding curve and the
         //     discounting curve
         // here, we do not work with a spread corrections directly, but
         // with this multiplied by the nominal and accrual basis,
         // so S_i is really an amount correction.

         S_.Resize<double>(arguments_.fixedCoupons.Count - j1_);
         for (int i = 0; i < S_.Count; ++i)
         {
            S_[i] = 0.0;
         }
         S_m1 = 0.0;
         int ratio = (int)((double)(arguments_.floatingCoupons.Count) / (double)(arguments_.fixedCoupons.Count) + 0.5);
         Utils.QL_REQUIRE(ratio >= 1, () => "floating leg's payment frequency must be equal or " +
                                "higher than fixed leg's payment frequency in " +
                                "analytic lgm swaption engine");

         int k = k1_;
         IborIndex flatIbor = swap.iborIndex().clone(c_);
         for (int j = j1_; j < arguments_.fixedCoupons.Count; ++j)
         {
            double sum1 = 0.0, sum2 = 0.0;
            for (int rr = 0; rr < ratio && k < arguments_.floatingCoupons.Count; ++rr, ++k)
            {
               double amount = arguments_.floatingCoupons[k];
               double lambda1 = 0.0, lambda2 = 1.0;
               if (floatSpreadMapping_ == FloatSpreadMapping.proRata)
               {
                  // we do not use the exact pay dates but the ratio to determine
                  // the distance to the adjacent payment dates
                  lambda2 = (double)(rr + 1) / (double)(ratio);
                  lambda1 = 1.0 - lambda2;
               }
               if (amount != double.NaN)
               {
                  double flatAmount = flatIbor.fixing(arguments_.floatingFixingDates[k]) *
                                    arguments_.floatingAccrualTimes[k] * arguments_.nominal;
                  double correction = (amount - flatAmount) * c_.currentLink().discount(arguments_.floatingPayDates[k]);
                  sum1 += lambda1 * correction;
                  sum2 += lambda2 * correction;
               }
               else
               {
                  // if no amount is given, we do not need a spread correction
                  // due to different forward / discounting curves since then
                  // no curve is attached to the swap's ibor index and so we
                  // assume a one curve setup;
                  // but we can still have a float spread that has to be converted
                  // into a fixed leg's payment
                  double correction = arguments_.nominal * arguments_.floatingSpreads[k] *
                                    arguments_.floatingAccrualTimes[k] * c_.currentLink().discount(arguments_.floatingPayDates[k]);
                  sum1 += lambda1 * correction;
                  sum2 += lambda2 * correction;
               }
            }
            if (j > j1_)
            {
               S_[j - j1_ - 1] += sum1 / c_.currentLink().discount(arguments_.fixedPayDates[j - 1]);
            }
            else
            {
               S_m1 += sum1 / c_.currentLink().discount(arguments_.floatingResetDates[k1_]);
            }
            S_[j - j1_] += sum2 / c_.currentLink().discount(arguments_.fixedPayDates[j]);
         }

         double w = type == Option.Type.Call ? -1.0 : 1.0;

         // it is a requirement that H' does not change its sign,
         // with u = -1.0 we handle the case H' < 0
         double u = p_.Hprime(0.0) > 0.0 ? 1.0 : -1.0;

         // do the actual pricing

         zetaex_ = p_.zeta(p_.termStructure().currentLink().timeFromReference(expiry));
         H0_ = p_.H(p_.termStructure().currentLink().timeFromReference(arguments_.floatingResetDates[k1_]));
         D0_ = c_.currentLink().discount(arguments_.floatingResetDates[k1_]);
         Hj_.Resize<double>(arguments_.fixedCoupons.Count - j1_);
         Dj_.Resize<double>(arguments_.fixedCoupons.Count - j1_);
         for (int j = j1_; j < arguments_.fixedCoupons.Count; ++j)
         {
            Hj_[j - j1_] = p_.H(p_.termStructure().currentLink().timeFromReference(arguments_.fixedPayDates[j]));
            Dj_[j - j1_] = c_.currentLink().discount(arguments_.fixedPayDates[j - j1_]);
         }

         Brent b = new Brent();
         double yStar = 0;
         //ISolver1d temp = (double x) => this.yStarHelper(x);


         try
         {
            TempSolvingFunction f = new TempSolvingFunction(this);
            yStar = b.solve(f, 1.0E-6, 0.0, 0.01);
         }
         catch (Exception e)
         {
            Utils.QL_FAIL("AnalyticLgmSwaptionEngine, failed to compute yStar, " + e.ToString());
         }

         CumulativeNormalDistribution N = new CumulativeNormalDistribution();
         double sqrt_zetaex = System.Math.Sqrt(zetaex_);
         double sum = 0.0;
         for (int j = j1_; j < arguments_.fixedCoupons.Count; ++j)
         {
            sum += w * (arguments_.fixedCoupons[j] - S_[j - j1_]) * Dj_[j - j1_] *
                  N.value(u * w * (yStar + (Hj_[j - j1_] - H0_) * zetaex_) / sqrt_zetaex);
         }
         sum += -w * S_m1 * D0_ * N.value(u * w * yStar / sqrt_zetaex);
         sum += w * (arguments_.nominal * Dj_.Last() * N.value(u * w * (yStar + (Hj_.Last() - H0_) * zetaex_) / sqrt_zetaex) -
                         arguments_.nominal * D0_ * N.value(u * w * yStar / sqrt_zetaex));
         results_.value = sum;

         results_.additionalResults["fixedAmountCorrectionSettlement"] = S_m1;
         results_.additionalResults["fixedAmountCorrections"] = S_;

      } // calculate


      private class TempSolvingFunction : ISolver1d
      {
         AnalyticLgmSwaptionEngine swapEngine_;

         public TempSolvingFunction(AnalyticLgmSwaptionEngine swapEngine)
         {
            swapEngine_ = swapEngine;
         }
         public override double value(double v)
         {
            return swapEngine_.yStarHelper(v);
         }
      }


      private double yStarHelper(double y)
      {
         double sum = 0.0;
         for (int j = j1_; j < arguments_.fixedCoupons.Count; ++j)
         {
            sum += (arguments_.fixedCoupons[j] - S_[j - j1_]) * Dj_[j - j1_] *
                   System.Math.Exp(-(Hj_[j - j1_] - H0_) * y - 0.5 * (Hj_[j - j1_] - H0_) * (Hj_[j - j1_] - H0_) * zetaex_);
         }
         sum += -S_m1 * D0_;
         //sum += Dj_[Dj_.Count-1] * arguments_.nominal *
         //System.Math.Exp(-(Hj_[Hj_.Count - 1] - H0_) * y - 0.5 * (Hj_[Hj_.Count - 1] - H0_) * (Hj_[Hj_.Count - 1] - H0_) * zetaex_);

         sum += Dj_.Last() * arguments_.nominal *
        System.Math.Exp(-(Hj_.Last() - H0_) * y - 0.5 * (Hj_.Last() - H0_) * (Hj_.Last() - H0_) * zetaex_);
         sum -= D0_ * arguments_.nominal;
         return sum;
      }




   }
}
