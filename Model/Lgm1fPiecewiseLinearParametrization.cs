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

/*! \file irlgm1fpiecewiselinearparametrization.hpp
    \brief piecewise linear model parametrization
    \ingroup models
*/

//! Lgm 1f Piecewise Linear Parametrization
/*! parametrization with piecewise linear H and zeta,
    w.r.t. zeta this is the same as piecewise constant alpha,
    w.r.t. H this is implemented with a new (helper) parameter
    h > 0, such that \f$H(t) = \int_0^t h(s) ds\f$

    \warning this class is considered experimental, it is not
             tested well and might have conceptual issues
             (e.g. kappa is zero almost everywhere); you
             might rather want to rely on the piecewise
             constant parametrization

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
   using IrLgm1fPiecewiseLinearParametrization = Lgm1fPiecewiseLinearParametrization<YieldTermStructure> ;
   
   public abstract class Lgm1fPiecewiseLinearParametrization<TS> : Lgm1fParametrization<TS>, IPiecewiseConstantHelper11
      where TS : YieldTermStructure
   {

      PiecewiseConstantHelper11 piecewiseConstantHelper11_;

      public Lgm1fPiecewiseLinearParametrization(Currency currency,
                                                 Handle<TS> termStructure,
                                                 Vector alphaTimes,
                                                 Vector alpha, Vector hTimes,
                                                 Vector h)
    : base(currency, termStructure)
      {
         piecewiseConstantHelper11_ = new PiecewiseConstantHelper11(alphaTimes, hTimes);

         initialize(alpha, h);
      }


      public Lgm1fPiecewiseLinearParametrization(Currency currency,
                                                 Handle<TS> termStructure,
                                                 List<Date> alphaDates,
                                                 Vector alpha,
                                                 List<Date> hDates,
                                                 Vector h)
         : base(currency, termStructure)
      {
         piecewiseConstantHelper11_ = new PiecewiseConstantHelper11(alphaDates, hDates, (YieldTermStructure)termStructure.link);

         initialize(alpha, h);
      }

      private void initialize(Vector alpha, Vector h)
      {
         Utils.QL_REQUIRE(helper1().t().size() + 1 == alpha.size(),
                  () => "alpha size (" + alpha.Count.ToString() + ") inconsistent to times size (" + helper1().t().Count.ToString() + ")");
         Utils.QL_REQUIRE(helper2().t().size() + 1 == h.size(), () => "h size (" + h.Count + ") inconsistent to times size ("
                                                                      + helper1().t().Count.ToString() + ")");
         // store raw parameter values
         for (int i = 0; i < helper1().p().size(); ++i)
         {
            helper1().p().setParam(i, inverse(0, alpha[i]));
         }
         for (int i = 0; i < helper2().p().size(); ++i)
         {
            helper2().p().setParam(i, inverse(1, h[i]));
         }
         update();
      }


      protected override double direct(int i, double x)
      {
         return i == 0 ? helper1().direct(x) : helper2().direct(x);
      }
      protected override double inverse(int i, double y)
      {
         return i == 0 ? helper1().inverse(y) : helper2().inverse(y);
      }

      public override double zeta(double t)
      {
         return helper1().int_y_sqr(t) / (this.scaling_ * this.scaling_);
      }



      public override double alpha(double t)
      {
         return helper1().y(t) / this.scaling_;
      }

      public override double kappa(double t)
      {
         return 0.0; // almost everywhere
      }

      public override double Hprime(double t)
      {
         return this.scaling_ * helper2().y(t);
      }

      public override double Hprime2(double t)
      {
         return 0.0; // almost everywhere
      }

      public override Vector parameterTimes(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i.ToString() + " does not exist, only have 0..1");
         if (i == 0)
            return helper1().t();
         else
            return helper2().t();
         ;
      }

      public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i.ToString() + " does not exist, only have 0..1");
         if (i == 0)
            return helper1().p();
         else
            return helper2().p();
      }


      public override double H(double t)
      {
         return this.scaling_ * helper2().int_y_sqr(t) + this.shift_;
      }

      public override void update()
      {
         helper1().update();
         helper2().update();
      }



      public PiecewiseConstantHelper1 helper1()
      {
         return piecewiseConstantHelper11_.helper1();
      }

      public PiecewiseConstantHelper1 helper2()
      {
         return piecewiseConstantHelper11_.helper2();
      }
   }
}
