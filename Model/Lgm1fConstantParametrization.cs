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

/*! \file irlgm1fconstantparametrization.hpp
    \brief constant model parametrization
    \ingroup models
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt.Model
{
   class Lgm1fConstantParametrization<TS> : Lgm1fParametrization<TS>
      where TS : YieldTermStructure
      //where TS : IObservable
   {
      public
          Lgm1fConstantParametrization(Currency currency, Handle<TS> termStructure, double alpha, double kappa)
         : base(currency, termStructure)
      {
         alpha_ = new PseudoParameter(1);
         kappa_ = new PseudoParameter(1);
         zeroKappaCutoff_ = 1.0E-6;
         alpha_.setParam(0, inverse(0, alpha));
         kappa_.setParam(0, inverse(0, kappa));

      }


      public override double zeta(double t)
      {
         return direct(0, alpha_.parameters()[0]) * direct(0, alpha_.parameters()[0]) * t / (this.scaling_ * this.scaling_);

      }
      public override double H(double t)
      {
         if (System.Math.Abs(kappa_.parameters()[0]) < zeroKappaCutoff_)
         {
            return this.scaling_ * t + this.shift_;
         }
         else
         {
            return this.scaling_ * (1.0 - System.Math.Exp(-kappa_.parameters()[0] * t)) / kappa_.parameters()[0] + this.shift_;
         }

      }


      public override double alpha(double t)
      {
         return direct(0, alpha_.parameters()[0]) / this.scaling_;
      }
      public override double kappa(double t)
      {
         return kappa_.parameters()[0];
      }

      public override double Hprime(double t)
      {
         return this.scaling_ * System.Math.Exp(-kappa_.parameters()[0] * t);
      }

      public override double Hprime2(double t)

      {
         return -this.scaling_ * kappa_.parameters()[0] * System.Math.Exp(-kappa_.parameters()[0] * t);

      }

      public override Parameter parameter(int i)
      {
         Utils.QL_REQUIRE(i < 2, () => "parameter " + i + " does not exist, only have 0..1");
         if (i == 0)
            return alpha_;
         else
            return kappa_;
      }

      protected override double direct(int i, double x)
      {
         return i == 0 ? x * x : x;
      }

      protected override double inverse(int i, double y)
      {
         return i == 0 ? System.Math.Sqrt(y) : y;
      }

      public override void update()
      {
         //base.update();
         throw new NotImplementedException();
      }

      private PseudoParameter alpha_;
      private PseudoParameter kappa_;
      private double zeroKappaCutoff_;
   }

}

