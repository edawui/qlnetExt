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
   //public class FlatExtrapolation
   //{


   /*! \file flatextrapolation.hpp
       \brief flat interpolation decorator
       \ingroup math
   */

   //! Flat extrapolation given a base interpolation
   /*! \ingroup interpolations */
   public class FlatExtrapolation : Interpolation
   {

      public class FlatExtrapolationImpl : Interpolation.Impl
      {

         private Interpolation i_;

         public FlatExtrapolationImpl(Interpolation i)
         {
            i_ = i;
         }

         public void update() { i_.update(); }

         public double xMin()
         {
            return i_.xMin();
         }

         public double xMax() { return i_.xMax(); }

         public List<double> xValues()
         {
            Utils.QL_FAIL("not implemented");
            return null;
         }

         public List<double> yValues()
         {
            Utils.QL_FAIL("not implemented");
            return null;
         }
         public bool isInRange(double x) { return i_.isInRange(x); }
         public double value(double x)
         {
            double tmp = System.Math.Max(Math.Min(x, i_.xMax()), i_.xMin());
            return i_.value(tmp);
         }
         public double primitive(double x)
         {
            if (x >= i_.xMin() && x <= i_.xMax())
            {
               return i_.primitive(x);
            }
            if (x < i_.xMin())
            {
               return i_.primitive(i_.xMin()) - (i_.xMin() - x);
            }
            else
            {
               return i_.primitive(i_.xMax()) + (x - i_.xMax());
            }
         }

         public double derivative(double x)
         {
            if (x > i_.xMin() && x < i_.xMax())
            {
               return i_.derivative(x);
            }
            else
            {
               // that is the left derivative for xmin and
               // the right derivative for xmax
               return 0.0;
            }
         }

         public double secondDerivative(double x)
         {
            if (x > i_.xMin() && x < i_.xMax())
            {
               return i_.secondDerivative(x);
            }
            else
            {
               // that is the left derivative for xmin and
               // the right derivative for xmax
               return 0.0;
            }
         }

      }

      public FlatExtrapolation(Interpolation i)
      {
         impl_ = new FlatExtrapolationImpl(i);
         impl_.update();
      }
   }

   //! %Linear-interpolation and flat extrapolation factory and traits
   public class LinearFlat
   {
      //public  Interpolation interpolate<I1,I2>(I1 xBegin, int count, I2 yBegin)
      public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin)
      {
         return new FlatExtrapolation(new LinearInterpolation(xBegin, xEnd, yBegin));
      }
      static bool global = false;
      static int requiredPoints = 2;
   }

   // } // namespace QuantExt



}
