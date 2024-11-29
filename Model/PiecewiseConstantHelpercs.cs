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


/*! \file piecewiseconstanthelper.hpp
    \brief helper classes for piecewise constant parametrizations
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

   class PiecewiseConstantHelpers
   {


      internal static void CheckTimes(Vector t)
      {
         if (t.Count == 0)
            return;
         Utils.QL_REQUIRE(t[0] > 0.0, () => "first time (" + t[0].ToString() + ") must be positive");
         for (int i = 0; i < t.Count - 1; ++i)
         {
            Utils.QL_REQUIRE(t[i] < t[i + 1], () => "times must be strictly increasing, entries at (" +
                                                     i.ToString() + "," + (i + 1).ToString() + ") are (" +
                                                     t[i].ToString() + "," + t[i + 1].ToString()
                                                     );
         }
      }



      internal static Vector DatesToTimes(List<Date> dates, YieldTermStructure yts)
      {
         Vector res = new Vector(dates.Count);
         //List< double> res = new List<double>();// (dates.size());
         for (int i = 0; i < dates.Count; ++i)
         {
            //res.Add( yts.timeFromReference(dates[i]));
            res[i] = yts.timeFromReference(dates[i]);
         }
         return res;
      }


   }
   

   public interface IPiecewiseConstantHelper1
   {
      //PseudoParameter Y_ { get; }

      Vector t();

      Parameter p();

      double direct(double x);

      double inverse(double y);

      void update();

      double y(double t);

      double int_y_sqr(double t);


   }


   public class PiecewiseConstantHelper1 : IPiecewiseConstantHelper1
   {
      Vector t_;
      PseudoParameter y_;
      List<double> b_;

      //public Vector T_
      //{ get { return t_; } }


      //public PseudoParameter Y_
      //{ get { return y_; } }

      public PiecewiseConstantHelper1(Vector t)
      {
         t_ = t;
         y_ = new PseudoParameter(t.Count + 1);
         PiecewiseConstantHelpers.CheckTimes(t_);
      }

      public PiecewiseConstantHelper1(List<Date> dates, YieldTermStructure yts)
                  : this(PiecewiseConstantHelpers.DatesToTimes(dates, yts))
      { }


      public Vector t() { return t_; }

      public Parameter p() { return y_; }

      public double direct(double x) { return x * x; }

      public double inverse(double y) { return System.Math.Sqrt(y); }

      public void update()
      {
         double sum = 0.0;
         b_.Resize(t_.Count);
         for (int i = 0; i < t_.Count; ++i)
         {
            sum += direct(y_.parameters()[i]) * direct(y_.parameters()[i]) * (t_[i] - (i == 0 ? 0.0 : t_[i - 1]));
            b_[i] = sum;
         }
      }




      public double y(double t)
      {
         return direct(UtilsExt.QL_PIECEWISE_FUNCTION(t_, y_.parameters(), t));
      }



      public double int_y_sqr(double t)
      {
         if (t < 0.0)
            return 0.0;
         // int i = 0;//todo std::upper_bound(t_.begin(), t_.end(), t) - t_.begin();
         int i = UtilsExt.UpperBound<double>(t_, t);

         double res = 0.0;
         if (i >= 1)
            res += b_[System.Math.Min(i - 1, b_.Count - 1)];
         double a = direct(y_.parameters()[System.Math.Min(i, y_.size() - 1)]);
         res += a * a * (t - (i == 0 ? 0.0 : t_[i - 1]));
         return res;
      }


   }


   public interface IPiecewiseConstantHelper11
   {
      PiecewiseConstantHelper1 helper1();
      PiecewiseConstantHelper1 helper2();
   }


   public class PiecewiseConstantHelper11 : IPiecewiseConstantHelper11
   {
      PiecewiseConstantHelper1 h1_;
      PiecewiseConstantHelper1 h2_;

      public PiecewiseConstantHelper11(Vector t1, Vector t2)
      {
         h1_ = new PiecewiseConstantHelper1(t1);
         h2_ = new PiecewiseConstantHelper1(t2);
      }


      public PiecewiseConstantHelper11(List<Date> dates1, List<Date> dates2, YieldTermStructure yts)
              : this(PiecewiseConstantHelpers.DatesToTimes(dates1, yts)
                    , PiecewiseConstantHelpers.DatesToTimes(dates2, yts))
      { }


      public PiecewiseConstantHelper1 helper1() { return h1_; }
      public PiecewiseConstantHelper1 helper2() { return h2_; }


   }


   public interface IPiecewiseConstantHelper2
   {

      //Vector T_ { get; }
      //PseudoParameter Y_ { get; }


      Vector t();

      Parameter p();

      double direct(double x);

      double inverse(double y);

      void update();

      double y(double t);

      double exp_m_int_y(double t);

      double int_exp_m_int_y(double t);

   }



   public interface IPiecewiseConstantHelper3
   {


       Vector t1();
       Vector t2();
       Vector tUnion();

       Parameter p1();

       Parameter p2();
       double direct1(double x);

       double inverse1(double y);
       double inverse2(double y);
      
       double direct2(double x);
      

       void update();

       double y1(double t);


       double y2(double t);

       double int_y1_sqr_exp_2_int_y2(double t);
      
   }


   public class PiecewiseConstantHelper2 : IPiecewiseConstantHelper2
   {
      Vector t_;
      double zeroCutoff_;
      PseudoParameter y_;
      List<double> b_;
      List<double> c_;

      //public Vector T_
      //{ get { return t_; }  }

      //public PseudoParameter Y_
      //{ get { return y_; } }


      public PiecewiseConstantHelper2(Vector t)
      {
         zeroCutoff_ = 1.0E-6;
         t_ = t;
         y_ = new PseudoParameter(t.Count + 1);
         PiecewiseConstantHelpers.CheckTimes(t_);
      }

      public PiecewiseConstantHelper2(List<Date> dates, YieldTermStructure yts)
                      : this(PiecewiseConstantHelpers.DatesToTimes(dates, yts))
      { }

      public Vector t() { return t_; }

      public Parameter p() { return y_; }

      public double direct(double x) { return x; }

      public double inverse(double y) { return y; }

      public void update()
      {
         double sum = 0.0;
         double sum2 = 0.0;
         b_.Resize(t_.Count);
         c_.Resize(t_.Count);
         for (int i = 0; i < t_.Count; ++i)
         {
            double t0 = (i == 0 ? 0.0 : t_[i - 1]);
            sum += direct(y_.parameters()[i]) * (t_[i] - t0);
            b_[i] = sum;
            double b2Tmp = (i == 0 ? 0.0 : b_[i - 1]);
            if (System.Math.Abs(direct(y_.parameters()[i])) < zeroCutoff_)
            {
               sum2 += (t_[i] - t0) * System.Math.Exp(-b2Tmp);
            }
            else
            {
               sum2 += (System.Math.Exp(-b2Tmp) - System.Math.Exp(-b2Tmp - direct(y_.parameters()[i]) * (t_[i] - t0))) /
                       direct(y_.parameters()[i]);
            }
            c_[i] = sum2;
         }
      }

      public double y(double t)
      {
         return direct(UtilsExt.QL_PIECEWISE_FUNCTION(t_, y_.parameters(), t));
      }



      public double exp_m_int_y(double t)
      {
         if (t < 0.0)
            return 1.0;
         //int i = 0;//todo  std::upper_bound(t_.begin(), t_.end(), t) - t_.begin();
         int i = UtilsExt.UpperBound<double>(t_, t);

         double res = 0.0;
         if (i >= 1)
            res += b_[System.Math.Min(i - 1, b_.Count - 1)];
         double a = y_.parameters()[System.Math.Min(i, y_.size() - 1)];
         res += a * (t - (i == 0 ? 0.0 : t_[i - 1]));
         return System.Math.Exp(-res);
      }


      public double int_exp_m_int_y(double t)
      {
         if (t < 0.0)
            return 0.0;
         //int i = 0;//todo std::upper_bound(t_.begin(), t_.end(), t) - t_.begin();
         int i = UtilsExt.UpperBound<double>(t_, t);

         double res = 0.0;
         if (i >= 1)
            res += c_[System.Math.Min(i - 1, c_.Count - 1)];
         double a = direct(y_.parameters()[System.Math.Min(i, y_.size() - 1)]);
         double t0 = (i == 0 ? 0.0 : t_[i - 1]);
         double b2Tmp = (i == 0 ? 0.0 : b_[i - 1]);
         if (System.Math.Abs(a) < zeroCutoff_)
         {
            res += System.Math.Exp(-b2Tmp) * (t - t0);
         }
         else
         {
            res += (System.Math.Exp(-b2Tmp) - System.Math.Exp(-b2Tmp - a * (t - t0))) / a;
         }
         return res;
      }



   }


   public class PiecewiseConstantHelper3: IPiecewiseConstantHelper3
   {
      //private double zeroCutoff_;

      //      protected:
      //    const Array t1_, t2_;
      //      mutable Array tUnion_;
      //    /*! y1, y2 are the raw values in the sense of parameter transformation */
      //    const boost::shared_ptr<PseudoParameter> y1_, y2_;



      //      mutable Array y1Union_, y2Union_;

      //private:
      //    mutable std::vector<Real> b_, c_;
      //   };
      private double zeroCutoff_;
      private List<double> b_;
      private List<double> c_;

      protected Vector t1_;
      protected Vector t2_;
      protected Vector tUnion_;
      protected Vector y1Union_;
      protected Vector y2Union_;

      protected PseudoParameter y1_;
      protected PseudoParameter y2_;

      public PiecewiseConstantHelper3(Vector t1, Vector t2)
      {

         zeroCutoff_ = 1.0E-6;
         t1_ = t1;
         t2_ = t2;
         y1_ = new PseudoParameter(t1.Count + 1);
         y2_ = new PseudoParameter(t2.Count + 1);
         PiecewiseConstantHelpers.CheckTimes(t1_);
         PiecewiseConstantHelpers.CheckTimes(t2_);
      }

      public PiecewiseConstantHelper3(List<Date> dates1, List<Date> dates2, YieldTermStructure yts) :
                    this(PiecewiseConstantHelpers.DatesToTimes(dates1, yts),
                        PiecewiseConstantHelpers.DatesToTimes(dates2, yts))
      { }


      public Vector t1() { return t1_; }
      public Vector t2() { return t2_; }
      public Vector tUnion() { return tUnion_; }

      public Parameter p1() { return y1_; }

      public Parameter p2() { return y2_; }

      public double direct1(double x) { return x * x; }

      public double inverse1(double y) { return System.Math.Sqrt(y); }
      public double inverse2(double y) { return y; }

      public double direct2(double x) { return x; }


      public void update()
      {
         List<double> tTmp = new List<double>(t1_);
         tTmp.AddRange(t2_);

         tTmp.Sort();
         tTmp = tTmp.Distinct(UtilsExt.EqualityFactory.Create<double>((x, y) => Utils.close_enough(x, y))).ToList<double>();
         tUnion_ = new Vector(tTmp);// (tTmp.begin(), tTmp.end());

         y1Union_ = new Vector(tUnion_.Count + 1);
         y2Union_ = new Vector(tUnion_.Count + 1);
         for (int i = 0; i < tUnion_.Count + 1; ++i)
         {
            // choose a safe t for y1 and y2 evaluation
            double t = (i == tUnion_.size() ? (tUnion_.size() == 0 ? 1.0 : tUnion_.size() + 1.0)
                                          : (0.5 * (tUnion_[i] + (i > 0 ? tUnion_[i - 1] : 0.0))));
            y1Union_[i] = UtilsExt.QL_PIECEWISE_FUNCTION(t1_, y1_.parameters(), t);
            y2Union_[i] = UtilsExt.QL_PIECEWISE_FUNCTION(t2_, y2_.parameters(), t);
         }
         double sum = 0.0, sum2 = 0.0;
         b_.Resize(tUnion_.Count);
         c_.Resize(tUnion_.Count);
         for (int i = 0; i < tUnion_.Count; ++i)
         {
            double t0 = (i == 0 ? 0.0 : tUnion_[i - 1]);
            sum += direct2(y2Union_[i]) * (tUnion_[i] - t0);
            b_[i] = sum;
            double b2Tmp = (i == 0 ? 0.0 : b_[i - 1]);
            if (System.Math.Abs(direct2(y2Union_[i])) < zeroCutoff_)
            {
               sum2 += direct1(y1Union_[i]) * direct1(y1Union_[i]) * (tUnion_[i] - t0) * System.Math.Exp(2.0 * b2Tmp);
            }
            else
            {
               sum2 += direct1(y1Union_[i]) * direct1(y1Union_[i]) *
                       (System.Math.Exp(2.0 * b2Tmp + 2.0 * direct2(y2Union_[i]) * (tUnion_[i] - t0)) - System.Math.Exp(2.0 * b2Tmp)) /
                       (2.0 * direct2(y2Union_[i]));
            }
            c_[i] = sum2;
         }
      }

      public double y1(double t)
      {
         return direct1(UtilsExt.QL_PIECEWISE_FUNCTION(t1_, y1_.parameters(), t));
      }


      public double y2(double t)
      {
         return direct2(UtilsExt.QL_PIECEWISE_FUNCTION(t2_, y2_.parameters(), t));
      }

      public double int_y1_sqr_exp_2_int_y2(double t)
      {
         if (t < 0.0)
            return 0.0;
         // int i = UtilsExt.UpperBoundVector(tUnion_, t);//todo  std::upper_bound(tUnion_.begin(), tUnion_.end(), t) - tUnion_.begin();
         int i = UtilsExt.UpperBound<double>(tUnion_, t);//todo  std::upper_bound(tUnion_.begin(), tUnion_.end(), t) - tUnion_.begin();

         double res = 0.0;
         if (i >= 1)
            res += c_[System.Math.Min(i - 1, c_.Count - 1)];
         double a = direct2(y2Union_[System.Math.Min(i, y2Union_.size() - 1)]);
         double b = direct1(y1Union_[System.Math.Min(i, y1Union_.size() - 1)]);
         double t0 = (i == 0 ? 0.0 : tUnion_[i - 1]);
         double b2Tmp = (i == 0 ? 0.0 : b_[i - 1]);
         if (System.Math.Abs(a) < zeroCutoff_)
         {
            res += b * b * System.Math.Exp(2.0 * b2Tmp) * (t - t0);
         }
         else
         {
            res += b * b * (System.Math.Exp(2.0 * b2Tmp + 2.0 * a * (t - t0)) - System.Math.Exp(2.0 * b2Tmp)) / (2.0 * a);
         }
         return res;
      }

   }

}
