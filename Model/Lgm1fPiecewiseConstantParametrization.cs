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

/*! \file irlgm1fpiecewiseconstantparametrization.hpp
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


   //! LGM 1F Piecewise Constant Parametrization
   /*! \ingroup models
   */
   using IrLgm1fPiecewiseConstantParametrization = Lgm1fPiecewiseConstantParametrization<YieldTermStructure>;

   public abstract class Lgm1fPiecewiseConstantParametrization<TS> : Lgm1fParametrization<TS>,
                                                                     IPiecewiseConstantHelper1,
                                                                    IPiecewiseConstantHelper2
      where TS : YieldTermStructure
   {
      IPiecewiseConstantHelper1 iPiecewiseConstantHelper1_;
      IPiecewiseConstantHelper2 iPiecewiseConstantHelper2_;

      public Lgm1fPiecewiseConstantParametrization(Currency currency, Handle<TS> termStructure,
                                          Vector alphaTimes, Vector alpha, Vector kappaTimes,
                                          Vector kappa) : base(currency, termStructure)
      {
         iPiecewiseConstantHelper1_ = new PiecewiseConstantHelper1(alphaTimes);
         iPiecewiseConstantHelper2_ = new PiecewiseConstantHelper2(kappaTimes);
         initialize(alpha, kappa);
      }

      public Lgm1fPiecewiseConstantParametrization(Currency currency, Handle<TS> termStructure,
                                            List<Date> kappaDates, Vector kappa,
                                            List<Date> alphaDates, Vector alpha)
           : base(currency, termStructure)
      {
         iPiecewiseConstantHelper1_ = new PiecewiseConstantHelper1(alphaDates, termStructure);
         iPiecewiseConstantHelper2_ = new PiecewiseConstantHelper2(kappaDates, termStructure);
         initialize(alpha, kappa);
      }


      private void initialize(Vector alpha, Vector kappa)
      {
         Utils.QL_REQUIRE(iPiecewiseConstantHelper1_.t().size() + 1 == alpha.size(),
                  () => "alpha size (" + alpha.size() + ") inconsistent to times size ("
                                   + iPiecewiseConstantHelper1_.t().size() + ")");
         Utils.QL_REQUIRE(iPiecewiseConstantHelper2_.t().size() + 1 == kappa.size(),
                   () => "kappa size (" + kappa.size() + ") inconsistent to times size ("
                                   + iPiecewiseConstantHelper2_.t().size() + ")");
         // store raw parameter values
         for (int i = 0; i < iPiecewiseConstantHelper1_.p().size(); ++i)
         {

            iPiecewiseConstantHelper1_.p().setParam(i, inverse(0, alpha[i]));
         }
         for (int i = 0; i < iPiecewiseConstantHelper2_.p().size(); ++i)
         {
            this.iPiecewiseConstantHelper2_.p().setParam(i, inverse(1, kappa[i]));
         }
         update();

      }





      protected override double direct(int i, double x)
      {
         return i == 0 ? iPiecewiseConstantHelper1_.direct(x) : iPiecewiseConstantHelper2_.direct(x);
      }

      protected override double inverse(int i, double y)
      {
         return i == 0 ? iPiecewiseConstantHelper1_.inverse(y) : iPiecewiseConstantHelper2_.inverse(y);
      }

      public override double zeta(double t)
      {
         return iPiecewiseConstantHelper1_.int_y_sqr(t) / (this.scaling_ * this.scaling_);
      }

      public override double H(double t)
      {
         return this.scaling_ * iPiecewiseConstantHelper2_.int_exp_m_int_y(t) + this.shift_;
      }

      public override double alpha(double t)
      {
         return iPiecewiseConstantHelper1_.y(t) / this.scaling_;
      }

      public override double kappa(double t)
      {
         return iPiecewiseConstantHelper2_.y(t);
      }

      public override double Hprime(double t)
      {
         return this.scaling_ * iPiecewiseConstantHelper2_.exp_m_int_y(t);
      }

      public override double Hprime2(double t)
      {
         return -this.scaling_ * iPiecewiseConstantHelper2_.exp_m_int_y(t) * kappa(t);
      }


      public override void update()
      {

         iPiecewiseConstantHelper1_.update();
         iPiecewiseConstantHelper2_.update();
      }

      public override Vector parameterTimes(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i + " does not exist, only have 0..1");
         if (i == 0)
            return iPiecewiseConstantHelper1_.t();
         else
            return iPiecewiseConstantHelper2_.t();
      }


      public override Parameter parameter(int i)
      {
         
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i + " does not exist, only have 0..1");
         if (i == 0)
            return iPiecewiseConstantHelper1_.p();
         else
            return iPiecewiseConstantHelper2_.p();
      }

      

      double IPiecewiseConstantHelper1.direct(double x)
      {
         throw new NotImplementedException();
      }


      double IPiecewiseConstantHelper1.int_y_sqr(double t)
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper1.inverse(double y)
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper2.direct(double x)
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper2.exp_m_int_y(double t)
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper2.int_exp_m_int_y(double t)
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper2.inverse(double y)
      {
         throw new NotImplementedException();
      }


      Vector IPiecewiseConstantHelper2.t()
      {
         throw new NotImplementedException();
      }

      void IPiecewiseConstantHelper2.update()
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper2.y(double t)
      {
         throw new NotImplementedException();
      }
      Parameter IPiecewiseConstantHelper2.p()
      {
         throw new NotImplementedException();
      }

      Parameter IPiecewiseConstantHelper1.p()
      {
         throw new NotImplementedException();
      }


      Vector IPiecewiseConstantHelper1.t()
      {
         throw new NotImplementedException();
      }

      void IPiecewiseConstantHelper1.update()
      {
         throw new NotImplementedException();
      }

      double IPiecewiseConstantHelper1.y(double t)
      {
         throw new NotImplementedException();
      }
      
   }
}
