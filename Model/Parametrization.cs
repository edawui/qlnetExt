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


using QLNet;

namespace QLNetExt
{
   public abstract class Parametrization
   {
      Currency currency_;
      Vector emptyTimes_;
      Parameter emptyParameter_;

      protected double h_;
      protected double h2_;

      public Parametrization(Currency currency)
      {
         h_ = 1.0E-6;

         h2_ = 1.0E-4;
         currency_ = currency;
         emptyParameter_ = new NullParameter();
      }


      public Currency currency()
      { return currency_; }

      public virtual Vector parameterTimes(int size)
      { return emptyTimes_; }


      /*! these are the actual (real) parameter values in contrast
          to the raw values which are stored in Parameter::params_
          and on which the optimization is done; there might be
          a transformation between real and raw values in
          order to implement a constraint (this is generally
          preferable to using a hard constraint) */
      public virtual Vector parameterValues(int i)
      {
         Vector tmp = parameter(i).parameters();
         Vector res = new Vector(tmp.Count);
         for (int ii = 0; ii < res.Count; ++ii)
         {
            res[ii] = direct(i, tmp[ii]);
         }
         return res;
      }


      /*! the parameter storing the raw values */
      public virtual Parameter parameter(int size)
      { return emptyParameter_; }

      /*! this method should be called when input parameters
          linked via references or pointers change in order
          to ensure consistent results */
      public abstract void update();



      //protected:
      /*! step size for numerical differentiation */
      //protected int  Real h_, h2_;
      /*! adjusted central difference scheme */
      protected double tr(double t) { return t > 0.5 * h_ ? t + 0.5 * h_ : h_; }
      protected double tl(double t) { return System.Math.Max(t - 0.5 * h_, 0.0); }
      protected double tr2(double t) { return t > h2_ ? t + h2_ : 2.0 * h2_; }
      protected double tm2(double t) { return t > h2_ ? t : h2_; }
      protected double tl2(double t) { return System.Math.Max(t - h2_, 0.0); }

      /*! transformations between raw and real parameters */
      protected virtual double direct(int size, double x) { return x; }
      protected virtual double inverse(int size, double y) { return y; }



   }
}
