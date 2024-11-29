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

/*! \file lgm.hpp
    \brief lgm model class
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
   public class LinearGaussMarkovModel : LinkableCalibratedModel
   {

      private IrLgm1fParametrization parametrization_;
      StochasticProcess1D stateProcess_;

      public LinearGaussMarkovModel(IrLgm1fParametrization parametrization)//:base()
      {
         parametrization_ = parametrization;

         stateProcess_ = new IrLgm1fStateProcess(parametrization_);//todo bost::make_shared<IrLgm1fStateProcess>(parametrization_);
         arguments_.Resize<Parameter>(2);
         arguments_[0] = parametrization_.parameter(0);
         arguments_[1] = parametrization_.parameter(1);

         parametrization_.termStructure().registerWith(update);

         //tODO         //registerWith(parametrization_.termStructure().link.);
      }


      public override void update()
      {
         parametrization_.update();
         notifyObservers();
      }

      public override void generateArguments() { update(); }

      public StochasticProcess1D stateProcess()
      {
         return stateProcess_;
      }


      public IrLgm1fParametrization parametrization()
      {
         return parametrization_;
      }

      public double numeraire(double t, double x, Handle<YieldTermStructure> discountCurve)
      {
         Utils.QL_REQUIRE(t >= 0.0, () => "t (" + t.ToString() + ") >= 0 required in LGM::numeraire");
         double Ht = parametrization_.H(t);
         return System.Math.Exp(Ht * x + 0.5 * Ht * Ht * parametrization_.zeta(t)) /
                (discountCurve.empty() ? parametrization_.termStructure().link.discount(t) : discountCurve.link.discount(t));
      }

      public double numeraire(double t, double x)
      {
         return numeraire(t, x, new Handle<YieldTermStructure>());
      }

      /*! calibration constraints, these can be used directly, or
    through the customized calibrate methods above */
      public List<bool> MoveVolatility(int i)
      {
         Utils.QL_REQUIRE(i < parametrization_.parameter(0).size(),
                   () => "volatility index (" + i.ToString() + ") out of range 0..."
                   + (parametrization_.parameter(0).size() - 1).ToString());
         List<bool> res = Enumerable.Repeat(true, parametrization_.parameter(0).size() + parametrization_.parameter(1).size()).ToList();
         res[i] = false;
         return res;
      }

      public List<bool> MoveReversion(int i)
      {
         Utils.QL_REQUIRE(i < parametrization_.parameter(1).size(),
                    () => "reversion index (" + i.ToString() +
                     ") out of range 0..." + (parametrization_.parameter(1).size() - 1).ToString());
         List<bool> res = Enumerable.Repeat(true, parametrization_.parameter(0).size() + parametrization_.parameter(1).size()).ToList();

         res[parametrization_.parameter(0).size() + i] = false;
         return res;
      }


      public double discountBond(double t, double T, double x)
      {
         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>();
         return discountBond(t, T, x, discountCurve);
      }

      public double discountBond(double t, double T, double x, Handle<YieldTermStructure> discountCurve)
      {
         if (Utils.close_enough(t, T))
         { return 1.0; }
         Utils.QL_REQUIRE(T >= t && t >= 0.0, () => "T(" + T.ToString() + ") >= t(" + t.ToString() + ") >= 0 required in LGM::discountBond");

         double Ht = parametrization_.H(t);
         double HT = parametrization_.H(T);
         return (discountCurve.empty()
                     ? parametrization_.termStructure().link.discount(T) / parametrization_.termStructure().link.discount(t)
                     : discountCurve.link.discount(T) / discountCurve.link.discount(t)) *
                System.Math.Exp(-(HT - Ht) * x - 0.5 * (HT * HT - Ht * Ht) * parametrization_.zeta(t));
      }


      public double reducedDiscountBond(double t, double T, double x)//, Handle<YieldTermStructure> discountCurve)
      {
         return reducedDiscountBond(t, T, x, new Handle<YieldTermStructure>());
      }

      public double reducedDiscountBond(double t, double T, double x, Handle<YieldTermStructure> discountCurve)
      {


         if (Utils.close_enough(t, T))
            return 1.0 / numeraire(t, x, discountCurve);
         Utils.QL_REQUIRE(T >= t && t >= 0.0, () => "T(" + T.ToString() + ") >= t(" + t.ToString() + ") >= 0 required in LGM::reducedDxsiscountBond");
         double HT = parametrization_.H(T);
         return (discountCurve.empty() ? parametrization_.termStructure().link.discount(T) : discountCurve.link.discount(T)) *
               System.Math.Exp(-HT * x - 0.5 * HT * HT * parametrization_.zeta(t));
      }



      public double discountBondOption(Option.Type type, double K, double t, double S,
                                                             double T,
                                                              Handle<YieldTermStructure> discountCurve)
      {
         Utils.QL_REQUIRE(T > S && S >= t && t >= 0.0, () => "T(" + T.ToString() + ") > S(" + S.ToString() + ") >= t(" + t.ToString() + ") >= 0 required in LGM::discountBondOption");
         double w = (type == Option.Type.Call ? 1.0 : -1.0);
         double pS = discountCurve.empty() ? parametrization_.termStructure().link.discount(S) : discountCurve.link.discount(S);
         double pT = discountCurve.empty() ? parametrization_.termStructure().link.discount(T) : discountCurve.link.discount(T);
         // slight generalization of Lichters, Stamm, Gallagher 11.2.1
         // with t < S only resulting in a different time at which zeta
         // has to be taken
         double sigma = System.Math.Sqrt(parametrization_.zeta(t)) * (parametrization_.H(T) - parametrization_.H(S));
         double dp = (System.Math.Log(pT / (K * pS)) / sigma + 0.5 * sigma);
         double dm = dp - sigma;
         CumulativeNormalDistribution N = new CumulativeNormalDistribution();
         return w * (pT * N.value(w * dp) - pS * K * N.value(w * dm));
      }

      public void calibrateVolatilitiesIterative(List<CalibrationHelper> helpers, OptimizationMethod method,
         EndCriteria endCriteria, Constraint constraint, List<double> weights)
      {
         for (int i = 0; i < helpers.Count; ++i)
         {
            List<CalibrationHelper> h = Enumerable.Repeat(helpers[i], 1).ToList();//= En(1, helpers[i]);
            calibrate(h, method, endCriteria, constraint, weights, MoveVolatility(i));
         }
      }


      public void calibrateReversionsIterative(List<CalibrationHelper> helpers,
                                                           OptimizationMethod method, EndCriteria endCriteria,
                                                            Constraint constraint, List<double> weights)
      {
         for (int i = 0; i < helpers.Count; ++i)
         {
            List<CalibrationHelper> h = Enumerable.Repeat(helpers[i], 1).ToList();
            calibrate(h, method, endCriteria, constraint, weights, MoveReversion(i));
         }
      }


   }
}
