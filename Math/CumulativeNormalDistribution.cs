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

/*! \file cumulativenormaldistribution.hpp
    \brief cumulative normal distribution based on std::erf (since C++11),
    \ingroup math
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   /*! Cumulative normal distribution
       This implementation relies on std::erf if c++11 is supported,
       otherwise falls back on boost::math::erf. */
   public class CumulativeNormalDistribution : QLNet.CumulativeNormalDistribution
   {
      private double average_, sigma_;

      public CumulativeNormalDistribution(double average = 0.0, double sigma = 1.0) : base(average, sigma)
      {
         average_ = average;
         sigma_ = sigma;
      }

      public new double value(double z)
      {
         return value(z);
         //return 0.5 * (1.0 + System.Math.Erf((z - average_) / sigma_ * Const.M_SQRT_2 ));
      }
   }


}
