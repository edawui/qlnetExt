/*
Copyright (C) 2022  Edem Dawui (edawui@gmail.com)

 This file is part of QLNetExt Project.

QLNetExt is based on ORE library, a free-software/open-source library
 for transparent pricing and risk analysis - http://opensourcerisk.org
 
 This program is distributed on the basis that it will form a useful
 contribution to risk analytics and model standardisation, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 FITNESS FOR A PARTICULAR PURPOSE. See the license for more details.
*/S FOR A PARTICULAR PURPOSE. See the license for more details.
*/

/*! \file gaussian1dcrossassetadaptor.hpp
    \brief adaptor class that extracts one irlgm1f component
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
   public class Gaussian1dCrossAssetAdaptor : Gaussian1dModel
   {

      LinearGaussMarkovModel x_;


      public Gaussian1dCrossAssetAdaptor(LinearGaussMarkovModel model)
    : base(model.parametrization().termStructure())
      {
         x_ = model;
         initialize();
      }

      public Gaussian1dCrossAssetAdaptor(int ccy, CrossAssetModel model)
    : base(model.irlgm1f(ccy).termStructure())
      {
         x_ = model.lgm(ccy);

         initialize();
      }

      private void initialize()
      {
         x_.registerWith(update);
         //registerWith(x_);
         stateProcess_ = x_.stateProcess();
      }

      
      protected override double numeraireImpl(double t, double y, Handle<YieldTermStructure> yts)
      {
         double d = yts.empty() ? 1.0 : x_.parametrization().termStructure().currentLink().discount(t) / yts.currentLink().discount(t);
         double x = y * System.Math.Sqrt(x_.parametrization().zeta(t));
         return d * x_.numeraire(t, x);
      }

      protected override double zerobondImpl(double T, double t, double y, Handle<YieldTermStructure> yts)
      {
         double d = yts.empty() ? 1.0 : x_.parametrization().termStructure().currentLink().discount(t) /
                                          x_.parametrization().termStructure().currentLink().discount(T) * yts.currentLink().discount(T) /
                                          yts.currentLink().discount(t);
         double x = y * System.Math.Sqrt(x_.parametrization().zeta(t));
         return d * x_.discountBond(t, T, x);
      }
   }
}
