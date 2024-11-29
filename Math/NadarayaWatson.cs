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

namespace QLNetExt
{

   public abstract class RegressionImpl
   {
      public abstract void update();
      public abstract double value(double x);
      public abstract double standardDeviation(double x);
   }

   public abstract class KernelForNadarayaWatson//<I1, I2> where I1: IComparable
                                                //    where I2: IComparable
   {
      //public KernelForNadarayaWatson()
      // {

      // }

      public abstract double value(double input);

   }

   public class NadarayaWatsonImpl : RegressionImpl// where I1 : IComparable
                                                   //       where I2 : IComparable
   {
      List<double> xBegin_;
      int xEnd_;
      List<double> yBegin_;

      KernelForNadarayaWatson kernel_;



      /*! \pre the \f$ x \f$ values must be sorted.
        \pre kernel needs a double operator()(double x) implementation
    */
      public NadarayaWatsonImpl(List<double> xBegin, int xEnd, List<double> yBegin, KernelForNadarayaWatson kernel)
      {
         xBegin_ = xBegin; xEnd_ = xEnd; yBegin_ = yBegin; kernel_ = kernel;
      }

      public override void update() { }

      public override double value(double x)
      {

         double tmp1 = 0.0, tmp2 = 0.0;

         for (int i = 0; i < xEnd_; ++i)
         {
            double tmp = kernel_.value(x - xBegin_[i]);
            tmp1 += yBegin_[i] * tmp;
            tmp2 += tmp;
         }

         return QLNet.Utils.close_enough(tmp2, 0.0) ? 0.0 : tmp1 / tmp2;
      }

      public override double standardDeviation(double x)
      {

         double tmp1 = 0.0, tmp1b = 0.0, tmp2 = 0.0;

         for (int i = 0; i < xEnd_; ++i)
         {

            double tmp = kernel_.value(x - xBegin_[i]);
            tmp1 += yBegin_[i] * tmp;
            tmp1b += yBegin_[i] * yBegin_[i] * tmp;
            tmp2 += tmp;
         }

         return QLNet.Utils.close_enough(tmp2, 0.0) ? 0.0 : System.Math.Sqrt(tmp1b / tmp2 - (tmp1 * tmp1) / (tmp2 * tmp2));
      }

   }




   //! Nadaraya Watgon regression
   /*! This implements the estimator

       \f[
       m(x) = \frac{\sum_i y_i K(x-x_i)}{\sum_i K(x-x_i)}
       \f]

       \ingroup interpolations
   */
   public class NadarayaWatson
   {
      private RegressionImpl impl_;

      /*! \pre the \f$ x \f$ values must be sorted.
          \pre kernel needs a Real operator()(Real x) implementation
      */
      public NadarayaWatson(List<double> xBegin, int xEnd, List<double> yBegin, KernelForNadarayaWatson kernel)
      {
         impl_ = new NadarayaWatsonImpl(xBegin, xEnd, yBegin, kernel);
      }

      public double value(double x)
      {
         return impl_.value(x);
      }

      public double standardDeviation(double x) { return impl_.standardDeviation(x); }

   }
}
