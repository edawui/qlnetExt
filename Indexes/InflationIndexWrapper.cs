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

/*! \file inflationindexwrapper.hpp
    \brief wrapper classes for inflation yoy and interpolation
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;


namespace QLNetExt
{
   public class ZeroInflationIndexWrapper : ZeroInflationIndex
   {
      private ZeroInflationIndex source_;
      private InterpolationType interpolation_;


      public ZeroInflationIndexWrapper(ZeroInflationIndex source,
                                                      InterpolationType interpolation)
    : base(source.familyName(), source.region(), source.revised(), source.interpolated(),
                         source.frequency(), source.availabilityLag(), source.currency(),
                         source.zeroInflationTermStructure())
      {
         source_ = source; interpolation_ = interpolation;

      }

      public override double fixing(Date fixingDate, bool forecastTodaysFixing= false)
      {

         // duplicated logic from CPICashFlow::amount()

         // what interpolation do we use? Index / flat / linear
         if (interpolation_ == InterpolationType.AsIndex)
         {
            return source_.fixing(fixingDate);
         }
         else
         {
            KeyValuePair<Date, Date> dd = Utils.inflationPeriod(fixingDate, new Frequency());


            double indexStart = source_.fixing(dd.Key);
            if (interpolation_ == InterpolationType.Linear)
            {
               double indexEnd = source_.fixing(dd.Value + new Period(1, TimeUnit.Days));
               // linear interpolation
               return indexStart +
                      (indexEnd - indexStart) * (fixingDate - dd.Key) /
                          ((dd.Value + new Period(1, TimeUnit.Days)) -
                           dd.Key); // can't get to next period's value within current period
            }
            else
            {
               // no interpolation, i.e. flat = ant, so use start-of-period value
               return indexStart;
            }
         }
      }

      private double  forecastFixing(Date fixingDate)
      {
         throw new NotImplementedException(); //Exception("");

         return double.NaN;
      }

   }



   public class YoYInflationIndexWrapper : YoYInflationIndex
   {
      private ZeroInflationIndex zeroIndex_;

      public YoYInflationIndexWrapper(ZeroInflationIndex zeroIndex, bool interpolated, Handle<YoYInflationTermStructure> ts)
       : base(zeroIndex.familyName(), zeroIndex.region(), zeroIndex.revised(), interpolated, true, zeroIndex.frequency(), zeroIndex.availabilityLag(), zeroIndex.currency(), ts)
      {
         zeroIndex_ = zeroIndex;
      }

      public override double fixing(Date fixingDate, bool forecastTodaysFixing= false)
      {

         // duplicated logic from YoYInflationIndex, this would not be necessary, if forecastFixing
         // was defined virtual in InflationIndex
         Date today = Settings.evaluationDate();
         Date todayMinusLag = today - availabilityLag_;
         KeyValuePair<Date, Date> lim = Utils.inflationPeriod(todayMinusLag, frequency_);
         Date lastFix = lim.Key - 1;

         Date flatMustForecastOn = lastFix + 1;
         Date interpMustForecastOn = lastFix + 1 - new Period(frequency_);

         if (interpolated() && fixingDate >= interpMustForecastOn)
         {
            return forecastFixing(fixingDate);
         }

         if (!interpolated() && fixingDate >= flatMustForecastOn)
         {
            return forecastFixing(fixingDate);
         }

         // historical fixing
         return base.fixing(fixingDate);
      }

      private double forecastFixing(Date fixingDate)
      {
         if (!yoyInflationTermStructure().empty())
            return base.fixing(fixingDate);
         double f1 = zeroIndex_.fixing(fixingDate);
         double f0 = zeroIndex_.fixing(fixingDate - new Period(1, TimeUnit.Years)); // FIXME convention ?
         return (f1 - f0) / f0;
      }
   }

   //! YY coupon pricer that takes the nominal ts directly instead of reading it from the yoy ts
   /*! This is useful if no yoy ts is given, as it might be the case of the yoy inflation index wrapper */
   public class YoYInflationCouponPricer2 : YoYInflationCouponPricer
   {
      public Handle<YieldTermStructure> nominalTs_;

      public YoYInflationCouponPricer2(
           Handle<YieldTermStructure> nominalTs,
           Handle<YoYOptionletVolatilitySurface> capletVol)// = new Handle<YoYOptionletVolatilitySurface>())
           : base(capletVol)
      {
         nominalTs_ = nominalTs;

      }

      public override void initialize(InflationCoupon coupon)
      {

         // duplicated logic from YoYInflationCouponPricer
         coupon_ = (YoYInflationCoupon)(coupon);
         Utils.QL_REQUIRE(coupon_ != null, () => "year-on-year inflation coupon needed");
         gearing_ = coupon_.gearing();
         spread_ = coupon_.spread();
         paymentDate_ = coupon_.date();

         // this is different from QuantLib::YoYInflationCouponPricer
         rateCurve_ = nominalTs_;

         // past or future fixing is managed in YoYInflationIndex::fixing()
         // use yield curve from index (which sets discount)

         discount_ = 1.0;
         if (paymentDate_ > rateCurve_.currentLink().referenceDate())
            discount_ = rateCurve_.currentLink().discount(paymentDate_);

         spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;
      }
   }

}
