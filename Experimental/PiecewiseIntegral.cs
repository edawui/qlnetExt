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

/*! \file piecewiseintegral.hpp
    \brief Integral of a piecewise well behaved function using
           a custom integrator for the pieces. It can be forced
           that the function is integrated only over intervals
           strictly not containing the critical points
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{

   public class PiecewiseIntegral : Integrator
   {



      protected Integrator integrator_;
      protected List<double> criticalPoints_;
      protected double eps_;


      public PiecewiseIntegral(Integrator integrator,
                             List<double> criticalPoints,
                              bool avoidCriticalPoints = true)
         : base(1.0, 1)
      {
         integrator_ = integrator;
         criticalPoints_ = criticalPoints;
         eps_ = avoidCriticalPoints ? (1.0 + Const.QL_EPSILON) : 1.0;

         criticalPoints_.Sort();

         criticalPoints_ = criticalPoints_.Distinct(UtilsExt.EqualityFactory.Create<double>((x, y) => Utils.close_enough(x, y))).ToList<double>();
      }

      protected override double integrate(Func<double, double> f, double a, double b)
      {

         int a0 = UtilsExt.LowerBoundVector((Vector)criticalPoints_, a);
         int b0 = UtilsExt.LowerBoundVector((Vector)criticalPoints_, b);


         if (a0 == criticalPoints_.Count)
         {
            double tmp = 1.0;
            if (!criticalPoints_.empty())
            {
               if (Utils.close_enough(a, criticalPoints_.Last()))//std back() is last()
               {
                  tmp = eps_;
               }
            }
            return integrate_h(f, a * tmp, b);
         }

         double res = 0.0;

         if (!Utils.close_enough(a, criticalPoints_[a0]))
         {
            res += integrate_h(f, a, System.Math.Min(criticalPoints_[a0] / eps_, b));
         }

         if (b0 == criticalPoints_.Count)
         {
            b0--;
            if (!Utils.close_enough(criticalPoints_[b0], b))
            {
               res += integrate_h(f, (criticalPoints_[b0]) * eps_, b);
            }
         }

         for (int x = a0; x < b0; ++x)
         {
            res += integrate_h(f, criticalPoints_[x] * eps_, System.Math.Min(criticalPoints_[x + 1] / eps_, b));
         }

         return res;
      }

      private double integrate_h(Func<double, double> f, double a, double b)
      {


         if (!Utils.close_enough(a, b))
            return integrator_.value(f, a, b);
         else
            return 0.0;
      }


   }
}
