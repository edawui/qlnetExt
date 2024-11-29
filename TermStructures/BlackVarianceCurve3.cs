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

/*! \file blackvariancecurve3.hpp
    \brief Black volatility curve modelled as variance curve
    \ingroup termstructures
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;


namespace QLNetExt.TermStructures
{
   //! Black volatility curve modelled as variance curve
   /*! This class calculates time-dependent Black volatilities using
       as input a vector of (ATM) Black volatilities observed in the
       market.

       The calculation is performed interpolating on the variance curve.
       Linear interpolation is used.

       \todo check time extrapolation

               \ingroup termstructures
   */


   public class BlackVarianceCurve3:   BlackVarianceTermStructure 
   //public class BlackVarianceCurve3: LazyObject,  BlackVarianceTermStructure 
    {

       List<double> times_;
          List<Handle<Quote>> quotes_;
           List<double> variances_;
           Interpolation varianceCurve_;
  

    public  BlackVarianceCurve3(int settlementDays,  Calendar cal, BusinessDayConvention bdc,
                                          DayCounter dc,  List<double> times,
                                          List<Handle<Quote>> blackVolCurve)
    //BlackVarianceTermStructure(settlementDays, cal, bdc, dc)
    {
         times_=times;
         quotes_=blackVolCurve;



         Utils.QL_REQUIRE(times.Count == blackVolCurve.Count, ()=>"mismatch between date vector and black vol vector");

         // cannot have dates[0]==referenceDate, since the
         // value of the vol at dates[0] would be lost
         // (variance at referenceDate must be zero)
         Utils.QL_REQUIRE(times[0] > 0, ()=>"cannot have times[0] <= 0");

         // Now insert 0 at the start of times_
         times_.Insert(0, 0);

         variances_ = new List<double>(times_.Count);
         variances_[0] = 0.0;
         for (int j = 1; j < times_.Count; j++)
         {
            Utils.QL_REQUIRE(times_[j] > times_[j - 1],()=> "times must be sorted unique!");
            //registerWith(
               quotes_[j - 1].registerWith(update);
         }
         varianceCurve_ = new Linear().interpolate(times_, times_.Count, variances_);//.begin(), times_.end(), variances_.begin());
      }

      public override void update()
      {
         base.update(); // calls notifyObservers
        //LazyObject::update();    // as does this

      }

   protected override void performCalculations()  {
    for (int j = 1; j <= quotes_.Count; j++) {
        variances_[j] = times_[j] * quotes_[j - 1].currentLink().value() * quotes_[j - 1].currentLink().value();
        Utils.QL_REQUIRE(variances_[j] >= variances_[j - 1], ()=>
                   "variance must be non-decreasing at j:" + j + " got var[j]:" + variances_[j]
                                                           + " and var[j-1]:" + variances_[j - 1]);
   }
   varianceCurve_.update();
}

protected override double blackVarianceImpl(double t, double dd)
      {
    calculate();
    if (t <= times_.Last()) {
        return varianceCurve_.value(t, true);
    } else {
        // extrapolate with flat vol
        return varianceCurve_.value(times_.Last(), true) * t / times_.Last();
    }
}


      public override Date maxDate()  { return Date.maxDate(); }

   public override double minStrike()  { return double.MinValue; }

public override double maxStrike()  { return double.MaxValue; }

public virtual void accept(IAcyclicVisitor v)
{
   if (v != null)
      v.visit(this);
   else
      Utils.QL_FAIL("not an event visitor");
}



   }
}
