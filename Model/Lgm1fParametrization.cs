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
   public abstract class Lgm1fParametrization<TS> : Parametrization where TS : IObservable
   {
      /*! zeta must satisfy zeta(0) = 0, zeta'(t) >= 0 */
     public abstract double zeta(double t);
      /*! H must be such that H' does not change its sign */
      public abstract double H(double t);
      // public abstract double alpha(double t);
      //public abstract double kappa(double t);
      // public abstract double Hprime(double t);
      //public abstract double Hprime2(double t);
      //public abstract double hullWhiteSigma(double t);
      //public abstract Handle<TS> termStructure();

      ///*! allows to apply a shift to H (model invariance 1) */
      //public double shift()
      //{ return 0.0; }

      /*! allows to apply a scaling to H and zeta (model invariance 2),
        note that if a non unit scaling is provided, then
        the parameterValues method returns the unscaled alpha,
        while all other methods return scaled (and shifted) values */
      //public double scaling()
      //{
      //   return 0.0;
      //}


      protected double shift_;
      protected double scaling_;

      private Handle<TS> termStructure_;



      public Lgm1fParametrization(Currency currency, Handle<TS> termStructure) : base(currency)
      {
         shift_ = 0.0;
         scaling_ = 1.0;
         termStructure_ = termStructure;
      }


      public virtual double alpha(double t)
      {
         return System.Math.Sqrt((zeta(tr(t)) - zeta(tl(t))) / h_) / scaling_;
      }

      public virtual double Hprime(double t)
      {
         return scaling_ * (H(tr(t)) - H(tl(t))) / h_;
      }

      public virtual double Hprime2(double t)
      {
         return scaling_ * (H(tr2(t)) - 2.0 * H(tm2(t)) + H(tl2(t))) / (h2_ * h2_);
      }
      public virtual double hullWhiteSigma(double t)
      {
         return Hprime(t) * alpha(t);
      }

      public virtual double kappa(double t)
      { return -Hprime2(t) / Hprime(t); }

      public virtual Handle<TS> termStructure() { return termStructure_; }

      public virtual double shift() { return shift_; }

      public virtual double scaling() { return scaling_; }



   }

   public class IrLgm1fParametrization : Lgm1fParametrization<YieldTermStructure>
   {


      public IrLgm1fParametrization(Currency currency, Handle<YieldTermStructure> termStructure) : base(currency, termStructure)
      { }


      public override double H(double t)
      {
         throw new NotImplementedException();
      }

      public override void update()
      {
         throw new NotImplementedException();
      }

      public override double zeta(double t)
      {
         throw new NotImplementedException();
      }
   }


}

