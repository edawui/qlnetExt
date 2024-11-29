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

/*! \file irlgm1fpiecewiseconstanthullwhiteadaptor.hpp
    \brief adaptor to emulate piecewise constant Hull White parameters
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
   using IrLgm1fPiecewiseConstantHullWhiteAdaptor =  Lgm1fPiecewiseConstantHullWhiteAdaptor<YieldTermStructure> ;

   public class Lgm1fPiecewiseConstantHullWhiteAdaptor<TS> : Lgm1fParametrization<TS>,
                                                             IPiecewiseConstantHelper2,
                                                             IPiecewiseConstantHelper3
                                                            where TS : QLNet.YieldTermStructure

   {
      //! LGM 1f Piecewise Constant Hull White Adaptor
      /*! \ingroup models
      */
   
      public Lgm1fParametrization<TS> lgm1fParametrization_;
      private PiecewiseConstantHelper3 piecewiseConstantHelper3_;
      private PiecewiseConstantHelper2 piecewiseConstantHelper2_;


      public Lgm1fPiecewiseConstantHullWhiteAdaptor(
          Currency currency, Handle<TS> termStructure, Vector sigmaTimes, Vector sigma,
          Vector kappaTimes, Vector kappa)
          : base(currency, termStructure)
      {
         piecewiseConstantHelper3_ = new PiecewiseConstantHelper3(sigmaTimes, kappaTimes);
         piecewiseConstantHelper2_ = new PiecewiseConstantHelper2(kappaTimes);

         initialize(sigma, kappa);
      }

      Lgm1fPiecewiseConstantHullWhiteAdaptor(
     Currency currency, Handle<TS> termStructure, List<Date> sigmaDates, Vector sigma,
     List<Date> kappaDates, Vector kappa)
    : base(currency, termStructure)
      {
         piecewiseConstantHelper3_ = new PiecewiseConstantHelper3(sigmaDates, kappaDates, termStructure);
         piecewiseConstantHelper2_ = new PiecewiseConstantHelper2(kappaDates, termStructure);

         initialize(sigma, kappa);
      }

      private void initialize(Vector sigma, Vector kappa)
      {
         Utils.QL_REQUIRE(piecewiseConstantHelper3_.t1().size() + 1 == sigma.size(),
                    () => "sigma size (" + sigma.size() + ") inconsistent to times size ("
                                    + piecewiseConstantHelper3_.t1().size() + ")");
         Utils.QL_REQUIRE(piecewiseConstantHelper2_.t().size() + 1 == kappa.size(),
                  () => "kappa size (" + kappa.size() + ") inconsistent to times size ("
                                   + piecewiseConstantHelper2_.t().size() + ")");

         // store raw parameter values
         for (int i = 0; i < piecewiseConstantHelper3_.p1().size(); ++i)
         {
            piecewiseConstantHelper3_.p1().setParam(i, inverse(0, sigma[i]));
         }
         for (int i = 0; i < piecewiseConstantHelper3_.p2().size(); ++i)
         {
            piecewiseConstantHelper3_.p2().setParam(i, inverse(1, kappa[i]));
         }
         for (int i = 0; i < piecewiseConstantHelper2_.p().size(); ++i)
         {
            piecewiseConstantHelper2_.p().setParam(i, inverse(1, kappa[i]));
         }
         update();
      }

      // 

      protected override double direct(int i, double x)
      {
         return i == 0 ? piecewiseConstantHelper3_.direct1(x) : piecewiseConstantHelper2_.direct(x);
      }

      protected override double inverse(int i, double y)
      {
         return i == 0 ? piecewiseConstantHelper3_.inverse1(y) : piecewiseConstantHelper2_.inverse(y);
      }

      public override double zeta(double t)
      {
         return piecewiseConstantHelper3_.int_y1_sqr_exp_2_int_y2(t) / (this.scaling_ * this.scaling_);
      }

      public override double alpha(double t)
      {
         return hullWhiteSigma(t) / Hprime(t) / this.scaling_;
      }

      public override double H(double t)
      {
         return this.scaling_ * piecewiseConstantHelper2_.int_exp_m_int_y(t) + this.shift_;
      }

      public override double kappa(double t)
      {
         return piecewiseConstantHelper2_.y(t);
      }

      public override double Hprime(double t)
      {
         return this.scaling_ * piecewiseConstantHelper2_.exp_m_int_y(t);
      }

      public override double Hprime2(double t)
      {
         return -this.scaling_ * piecewiseConstantHelper2_.exp_m_int_y(t) * kappa(t);
      }

      public override double hullWhiteSigma(double t)
      {
         return piecewiseConstantHelper3_.y1(t);
      }

      public override void update()
      {
         piecewiseConstantHelper3_.update();
         piecewiseConstantHelper2_.update();
      }

      public override Vector parameterTimes(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i + " does not exist, only have 0..1");
         if (i == 0)
            return piecewiseConstantHelper3_.t1();
         else
            return piecewiseConstantHelper2_.t();
      }


      public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i + " does not exist, only have 0..1");
         if (i == 0)
            return piecewiseConstantHelper3_.p1();
         else
            return piecewiseConstantHelper2_.p();
      }

      public Vector t()
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).t();
      }

      public Parameter p()
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).p();
      }

      public double direct(double x)
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).direct(x);
      }

      public double inverse(double y)
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).inverse(y);
      }

      public double y(double t)
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).y(t);
      }

      public double exp_m_int_y(double t)
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).exp_m_int_y(t);
      }

      public double int_exp_m_int_y(double t)
      {
         return ((IPiecewiseConstantHelper2)piecewiseConstantHelper2_).int_exp_m_int_y(t);
      }

      public Vector t1()
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).t1();
      }

      public Vector t2()
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).t2();
      }

      public Vector tUnion()
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).tUnion();
      }

      public Parameter p1()
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).p1();
      }

      public Parameter p2()
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).p2();
      }

      public double direct1(double x)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).direct1(x);
      }

      public double inverse1(double y)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).inverse1(y);
      }

      public double inverse2(double y)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).inverse2(y);
      }

      public double direct2(double x)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).direct2(x);
      }

      public double y1(double t)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).y1(t);
      }

      public double y2(double t)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).y2(t);
      }

      public double int_y1_sqr_exp_2_int_y2(double t)
      {
         return ((IPiecewiseConstantHelper3)piecewiseConstantHelper3_).int_y1_sqr_exp_2_int_y2(t);
      }

      // typedef


   } // namespace QuantExt

}
