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

/*! \file crossassetanalytics.hpp
    \brief basis functions for analytics in the cross asset model
    \ingroup crossassetmodel
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;
namespace QLNetExt.CrossAssetAnalytics
{
   public partial class Utils
   {

      public interface IE
      {
         double eval(CrossAssetModel x, double t);
      }

      /*! generic integrand */
      //      template<class E> Real integral_helper(const CrossAssetModel* x, const E& e, const Real t);

      /*! generic integral calculation */
      //    template<typename E> Real integral(const CrossAssetModel* model, const E& e, const Real a, const Real b);

      /*! product expression, 2 factors */
      // template<typename E1, typename E2> struct P2_
      public class P2_<E1, E2>:IE
          where E1 : IE
          where E2 : IE
      {

         E1 e1_;
         E2 e2_;

         public P2_(E1 e1, E2 e2)
         {
            e1_ = e1;
            e2_ = e2;
         }
        public double eval(CrossAssetModel x, double t)
         {
            return e1_.eval(x, t) * e2_.eval(x, t);
         }
      }

      /*! product expression, 3 factors */
      public class P3_<E1, E2, E3>:IE
         where E1 : IE
         where E2 : IE
         where E3 : IE
      {

         E1 e1_;
         E2 e2_;
         E3 e3_;

         public P3_(E1 e1, E2 e2, E3 e3)
         {
            e1_ = e1;
            e2_ = e2;
            e3_ = e3;
         }
       public  double eval(CrossAssetModel x, double t)
         {
            return e1_.eval(x, t) * e2_.eval(x, t) * e3_.eval(x, t);
         }
      }

      /*! product expression, 4 factors */
      public class P4_<E1, E2, E3, E4>:IE
               where E1 : IE
               where E2 : IE
               where E3 : IE
               where E4 : IE
      {

         E1 e1_;
         E2 e2_;
         E3 e3_;
         E4 e4_;

         public P4_(E1 e1, E2 e2, E3 e3, E4 e4)
         {
            e1_ = e1;
            e2_ = e2;
            e3_ = e3;
            e4_ = e4;
         }
        public double eval(CrossAssetModel x, double t)
         {
            return e1_.eval(x, t) * e2_.eval(x, t) * e3_.eval(x, t) * e4_.eval(x, t);
         }
      }




      /*! product expression, 5 factors */
      public class P5_<E1, E2, E3, E4, E5>:IE
               where E1 : IE
               where E2 : IE
               where E3 : IE
               where E4 : IE
               where E5 : IE
      {

         E1 e1_;
         E2 e2_;
         E3 e3_;
         E4 e4_;
         E5 e5_;

         public P5_(E1 e1, E2 e2, E3 e3, E4 e4, E5 e5)
         {
            e1_ = e1;
            e2_ = e2;
            e3_ = e3;
            e4_ = e4;
            e5_ = e5;
         }
        public double eval(CrossAssetModel x, double t)
         {
            return e1_.eval(x, t) * e2_.eval(x, t) * e3_.eval(x, t) * e4_.eval(x, t) * e5_.eval(x, t);
         }
      }



      /*! creator function for product expression, 2 factors */
      //template<class E1, class E2> const
      public static P2_<E1, E2> P<E2, E1>(E1 e1, E2 e2)
                where E1 : IE
             where E2 : IE

      { return (new P2_<E1, E2>(e1, e2)); }

      ///*! creator function for product expression, 3 factors */
      //public static P3_<E1, E2, E3> P<E1, E2, E3>(E1 e1, E2 e2, E3 e3)
      //          where E1 : IE
      //       where E2 : IE
      //       where E3 : IE

      //{ return (new P3_<E1, E2, E3>(e1, e2, e3)); }

      public static P3_<E1, E2, E3> P<E1, E2, E3>(E1 e1, E2 e2, E3 e3)
                where E1 : IE
             where E2 : IE
             where E3 : IE

      { return (new P3_<E1, E2, E3>(e1, e2, e3)); }


      //
      /*! creator function for product expression, 4 factors */
      public static P4_<E1, E2, E3, E4> P<E1, E2, E3, E4>(E1 e1, E2 e2, E3 e3, E4 e4)
                where E1 : IE
             where E2 : IE
             where E3 : IE
             where E4 : IE

      {
         return (new P4_<E1, E2, E3, E4>(e1, e2, e3, e4));
      }


      /*! creator function for product expression, 5 factors */
      public static P5_<E1, E2, E3, E4, E5> P<E1, E2, E3, E4, E5>(E1 e1, E2 e2, E3 e3, E4 e4, E5 e5)
                where E1 : IE
             where E2 : IE
             where E3 : IE
             where E4 : IE
             where E5 : IE

      {
         return (new P5_<E1, E2, E3, E4, E5>(e1, e2, e3, e4, e5));
      }


      //// inline

      //template<class E> inline
      public static double integral_helper(CrossAssetModel x, IE e, double t)
       //  where E : IE
      {
         return e.eval(x, t);
      }


      public static double integral(CrossAssetModel x, IE e, double a, double b)
             //where E : IE

      {
         //Func<double, double> theFunc = xyz => integral_helper(x, e, xyz);
         //return x.integrator().value(theFunc, a, b);

         return x.integrator().value(xyz => integral_helper(x, e, xyz), a, b);
         // return 0.0;//todo x.integrator()->operator() (boost::bind(&integral_helper<E>, x, e, _1), a, b);
      }

   }
}
