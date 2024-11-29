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

/*! \file qle/math/stabilisedglls.hpp
    \brief Numerically stabilised general linear least squares
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

   //! Numerically stabilised general linear least squares
   /*! The input data is lineaerly transformed before performing the linear least squares fit.
     The linear least squares fit on the transformed data is done using the
     GeneralLinearLeastSquares class. */

      //todo : this is class is defaulting to the existing Linear Least Squares Regression code in QLNet

public   class StabilisedGLLS<ArgumentType>: LinearLeastSquaresRegression<ArgumentType>
   {
      public    enum Method
      {
         None,      // No stabilisation
         MaxAbs,    // Divide x and y values by max of abs of values (per x coordinate, y)
         MeanStdDev // Subtract mean and divide by std dev (per x coordinate, y)
      };

      protected Vector a_, err_, residuals_, standardErrors_, xMultiplier_, xShift_;
      protected double yMultiplier_, yShift_;
      protected Method method_;
      protected LinearLeastSquaresRegression<ArgumentType> glls_;


      public StabilisedGLLS(List<ArgumentType> x, List<double> y, List<Func<ArgumentType, double>> v, Method method)
         :base(x,y,v)
      {

         a_ = new Vector(v.Count, 0);
         err_ = new Vector(v.Count, 0);
         residuals_ = new Vector(x.Count, 0);
         standardErrors_ = new Vector(v.Count, 0);

         method_ = method;

        calculate(x, y, v);
      }



   public void calculate(List<ArgumentType> x, List<double> y, List<Func<ArgumentType, double>> v)
      {
         /*
         List<double> xData = new List<double>(x.Count);
         List<double> yData = new List<double>(y.Count);

         xMultiplier_ = new Vector(1, 1.0);
         xShift_ = new Vector(1, 0.0);
         yMultiplier_ = 1.0;
         yShift_ = 0.0;

        
         switch (method_)
         {
            case Method.None:
               break;
            case Method.MaxAbs:
               {
                  double mx = 0.0, my = 0.0;
                  for (int i = 0; i < x.Count; ++i)
                  {
                     mx = System.Math.Max(System.Math.Abs(x[i]), mx);
                  }
                  if (!Utils.close_enough(mx, 0.0))
                     xMultiplier_[0] = 1.0 / mx;
                  for (int i = 0; i < y.Count; ++i)
                  {
                     my = System.Math.Max(System.Math.Abs(y[i]), my); //std::max(std::abs(y[i]), my);
                  }
                  if (!Utils.close_enough(my, 0.0))
                     yMultiplier_ = 1.0 / my;
                  break;
               }
            case Method.MeanStdDev:
               {
                  //accumulator_set<Real, stats<tag::mean, tag::variance>> acc;

                  for (int i = 0; i < x.Count; ++i)
                  {
                     acc(x[i]);
                  }
                  xShift_[0] = -mean(acc);
                  Real tmp = variance(acc);
                  if (!close_enough(tmp, 0.0))
                     xMultiplier_[0] = 1.0 / std::sqrt(tmp);
                  accumulator_set<Real, stats<tag::mean, tag::variance>> acc2;
                  for (int i = 0; i < static_cast<int>(y.end() - y.begin()); ++i)
                  {
                     acc2(y[i]);
                  }
                  yShift_ = -mean(acc2);
                  Real tmp2 = variance(acc2);
                  if (!close_enough(tmp2, 0.0))
                     yMultiplier_ = 1.0 / std::sqrt(tmp2);
                  break;
               }
            default:
               QL_FAIL("unknown stabilisation method");
         }

         for (int i = 0; i < static_cast<int>(x.end() - x.begin()); ++i)
         {
            xData[i] = (x[i] + xShift_[0]) * xMultiplier_[0];
         }
         for (int i = 0; i < static_cast<int>(y.end() - y.begin()); ++i)
         {
            yData[i] = (y[i] + yShift_) * yMultiplier_;
         }

         glls_ = boost::make_shared<GeneralLinearLeastSquares>(xData, yData, v);
      */
            }



   }
}
