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


/*! \file lgmimpliedyieldtermstructure.hpp
    \brief yield term structure implied by a LGM model
    \ingroup termstructures
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{

   //! Statically Corrected Yield Term Structure
   /*! This termstructure takes a floating reference date term structure
       and two fixed reference date term structures, applying a static
       correction to the floating ts implied by the two fixed ones.
       Usually the floating term structure will coincide with
       the first fixed at ruction time. Also, the two fixed
       termstructures should have the same reference date and all three
       termstructures should have the same day counter. */
   public class StaticallyCorrectedYieldTermStructure : YieldTermStructure
   {

      Dictionary<cache_key, double> cache_c_;// Dictionary<double, cache_hasher>> cache_c_;
      // end cache
      Handle<YieldTermStructure> x_, source_, target_;
      DynamicsType.YieldCurveRollDown rollDown_;




      public StaticallyCorrectedYieldTermStructure(Handle<YieldTermStructure> floatingTermStructure,
                                           Handle<YieldTermStructure> fixedSourceTermStructure,
                                           Handle<YieldTermStructure> fixedTargetTermStructure,
                                          DynamicsType.YieldCurveRollDown rollDown = DynamicsType.YieldCurveRollDown.ForwardForward)
        : base(floatingTermStructure.currentLink().dayCounter())
      {
         x_ = floatingTermStructure;
         source_ = fixedSourceTermStructure;
         target_ = fixedTargetTermStructure;
         rollDown_ = rollDown;

         floatingTermStructure.registerWith(update);
         fixedSourceTermStructure.registerWith(update);
         fixedTargetTermStructure.registerWith(update);
      }

      public override Date maxDate() { return x_.currentLink().maxDate(); }

      public override void update() { }

      public override Date referenceDate() { return x_.currentLink().referenceDate(); }

      public override Calendar calendar() { return x_.currentLink().calendar(); }
      public override int settlementDays() { return x_.currentLink().settlementDays(); }

      public void flushCache() { cache_c_.Clear(); }

      protected override double discountImpl(double t)
      {
         double c = 1.0;
         if (rollDown_ == DynamicsType.YieldCurveRollDown.ForwardForward)
         {
            double t0 = source_.currentLink().timeFromReference(referenceDate());
            // roll down = ForwardForward
            // cache lookup
            cache_key k  = new cache_key(t0, t );
           // Dictionary<cache_key, double>.Enumerator i = cache_c_.GetEnumerator().find(k);


            int i = cache_c_.Keys.ToList<cache_key>().FindIndex(xyz => xyz == k);
            if (i == cache_c_.Count)
            {              
             
               c = source_.currentLink().discount(t0) / source_.currentLink().discount(t0 + t) * target_.currentLink().discount(t0 + t) / target_.currentLink().discount(t0);
               cache_c_.Add(k, c);// std::make_pair(k, c));
            }
            else
            {
               c = cache_c_[k];//.second;
            }
         }
         else
         {
            // roll down = ConstantDiscount
            // cache lookup
            cache_key k = new cache_key(0.0, t);
            int i = cache_c_.Keys.ToList<cache_key>().FindIndex(xyz => xyz == k);
           // boost::unordered_map<cache_key, Real>::_iterator i = cache_c_.find(k);
            if (i == cache_c_.Count)
            {
               c = target_.currentLink().discount(t) / source_.currentLink().discount(t);
               cache_c_.Add(k, c);
            }
            else
            {
               c = cache_c_[k];
            }
         }
         return x_.currentLink().discount(t) * c;
      }



      // cache for exact discretization
      protected struct cache_key
      {
         public double t0;
         public double dt;
         public cache_key(double the_t0, double the_dt)
         {
            t0 = the_t0; dt = the_dt;
         }
         public static bool operator ==(cache_key o, cache_key o2)
         {
            return (o2.t0 == o.t0) & (o2.dt == o.dt);
         }


         public static bool operator !=(cache_key o, cache_key o2)
         {
            return (o2.t0 != o.t0) || (o2.dt != o.dt);
         }

         public override bool Equals(object obj)
         {
            cache_key o = (cache_key)obj;
            if (!(o == null))
            {
               return o == this;
            }
            return false;

         }


         public override int GetHashCode()
         {
            return t0.GetHashCode() + dt.GetHashCode();
         }

         // public static int operator()(double x)
         // {
         //    int seed = 0;
         // //boost::hash_combine(seed, x);
         //    return seed;
         //}

      }


      //protected struct cache_hasher //: Func<cache_key, int>
      //{
      //   public static int Apply(cache_key x)
      //   {
      //      int seed = 0;
      //      seed = seed + x.GetHashCode();
      //      //todo boost::hash_combine(seed, x.t0);
      //      //todo    boost::hash_combine(seed, x.dt);
      //      return seed;
      //   }

      //   // cache for process drift and diffusion (e.g. used in Euler discretization)
      //   public static int Apply(int x)
      //   {
      //      int seed = 0;
      //      seed = seed + x.GetHashCode();//
      //                                    //boost::hash_combine(seed, x);
      //      return seed;
      //   }
      //   Dictionary<cache_key, Vector> cache_m_;
      //   Dictionary<cache_key, Matrix> cache_d_;
      //   // mutable boost::unordered_map<cache_key, Array, cache_hasher> cache_m_;
      //   //mutable boost::unordered_map<cache_key, Matrix, cache_hasher> cache_v_, cache_d_;
      //} // ExactDiscretization





      //      private:
      //    // FIXME: remove cache
      //    // cache for source and target forwards
      //    struct cache_key
      //{
      //   double t0, t;
      //   bool operator ==( cache_key& o)  { return (t0 == o.t0) && (t == o.t); }
      //    };
      //    struct cache_hasher : std::unary_function<cache_key, std::size_t>
      //{
      //   std::size_t operator()(cache_key & x)  {
      //            std::size_t seed = 0;
      //   boost::hash_combine(seed, x.t0);
      //            boost::hash_combine(seed, x.t);
      //            return seed;
      //        }
      //    };
      //};

      // inline


   }
}
