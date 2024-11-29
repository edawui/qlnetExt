
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

/*! \file averageonindexedcoupon.hpp
    \brief coupon paying the weighted average of the daily overnight rate

        \ingroup cashflows
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using QLNet;

namespace QLNetExt
{
   //using List<CashFlow> = List<CashFlow>;//> Leg;

   public class AverageONIndexedCoupon : FloatingRateCoupon
   {


      List<Date> valueDates_, fixingDates_;
      List<double> fixings_;
      int numPeriods_;
      List<double> dt_;
      int rateCutoff_;


      public AverageONIndexedCoupon(Date paymentDate, double nominal, Date startDate,
                                                 Date endDate,
                                                OvernightIndex overnightIndex, double gearing,
                                                double spread, int rateCutoff, DayCounter dayCounter)
          : base(paymentDate, nominal, startDate, endDate, overnightIndex.fixingDays(), overnightIndex,
                          gearing, spread, new Date(), new Date(), dayCounter, false)
      {
         rateCutoff_ = rateCutoff;
         fixings_ = new List<double>();
         dt_ = new List<double>();
         valueDates_ = new List<Date>();
         fixingDates_ = new List<Date>();

         // Populate the value dates.
         Schedule sch = (new MakeSchedule())
                            .from(startDate)
                            .to(endDate)
                            .withTenor(new Period(1, TimeUnit.Days)) //todo Check
                            .withCalendar(overnightIndex.fixingCalendar())
                            .withConvention(overnightIndex.businessDayConvention())
                            .backwards()
                            .value();

         valueDates_ = sch.dates();
         //QL_ENSURE(valueDates_.Count - rateCutoff_ >= 2, "degenerate schedule");
         Utils.QL_REQUIRE(valueDates_.Count - rateCutoff_ >= 2, () => "degenerate schedule");

         // Populate the fixing dates.
         numPeriods_ = valueDates_.Count - 1;
         if (overnightIndex.fixingDays() == 0)
         {
            fixingDates_ = valueDates_.GetRange(0, valueDates_.Count - 1);// todo check =List<Date>(valueDates_.begin(), valueDates_.end() - 1);
         }
         else
         {
            fixingDates_.Resize(numPeriods_);
            for (int i = 0; i < numPeriods_; ++i)
               fixingDates_[i] = overnightIndex.fixingDate(valueDates_[i]);
         }

         // Populate the accrual periods.
         dt_.Resize(numPeriods_);
         for (int i = 0; i < numPeriods_; ++i)
            dt_[i] = dayCounter.yearFraction(valueDates_[i], valueDates_[i + 1]);
      }

      public List<double> indexFixings()
      {

         fixings_.Resize(numPeriods_);
         int i;

         for (i = 0; i < numPeriods_ - rateCutoff_; ++i)
         {
            fixings_[i] = index_.fixing(fixingDates_[i]);
         }

         double cutoffFixing = fixings_[i - 1];
         while (i < numPeriods_)
         {
            fixings_[i] = cutoffFixing;
            i++;
         }

         return fixings_;
      }

      public override Date fixingDate() { return fixingDates_[(int)(fixingDates_.Count - rateCutoff_)]; }

      public override void accept(IAcyclicVisitor v)
      {
         // IAcyclicVisitor<AverageONIndexedCoupon>* v1 = dynamic_cast<Visitor<AverageONIndexedCoupon>*>(v);
         if (v == null)
         {
            v.visit(this);
         }
         else
         {
            base.accept(v);
         }
      }



       public List<Date> fixingDates()  { return fixingDates_; }
      //! accrual periods for the averaging
      public List<double> dt()  { return dt_; }
      //! fixings to be averaged
      //public List<double> indexFixings() ;
      //! value dates for the rates to be averaged
      public List<Date> valueDates()  { return valueDates_; }
      //! rate cutoff associated with the coupon
      public  int rateCutoff()  { return rateCutoff_; }
   }


   //! helper class building a sequence of overnight coupons
   /*! \ingroup cashflows
   */
   public class AverageONLeg
   {
      Schedule schedule_;
      OvernightIndex overnightIndex_;
      List<double> notionals_;
      DayCounter paymentDayCounter_;
      BusinessDayConvention paymentAdjustment_;
      List<double> gearings_;
      List<double> spreads_;
      Calendar paymentCalendar_;
      int rateCutoff_;
      AverageONIndexedCouponPricer couponPricer_;




      public AverageONLeg(Schedule schedule, OvernightIndex i)
      {
         schedule_ = schedule;
         overnightIndex_ = i;
         paymentAdjustment_ = BusinessDayConvention.Following;
         paymentCalendar_ = new Calendar();
         rateCutoff_ = 0;
      }
      public AverageONLeg withNotional(double notional)
      {
         notionals_ = new List<double>();
         notionals_.Add(notional);
         return this;
      }

      public AverageONLeg withNotionals(List<double> notionals)
      {
         notionals_ = notionals;
         return this;
      }

      public AverageONLeg withPaymentDayCounter(DayCounter dayCounter)
      {
         paymentDayCounter_ = dayCounter;
         return this;
      }

      public AverageONLeg withPaymentAdjustment(BusinessDayConvention convention)
      {
         paymentAdjustment_ = convention;
         return this;
      }

      public AverageONLeg withGearing(double gearing)
      {
         gearings_ = new List<double>();
         gearings_.Add(gearing);
         return this;
      }

      public AverageONLeg withGearings(List<double> gearings)
      {
         gearings_ = gearings;
         return this;
      }

      public AverageONLeg withSpread(double spread)
      {
         spreads_ = new List<double>();
         spreads_.Add(spread);
         return this;
      }

      public AverageONLeg withSpreads(List<double> spreads)
      {
         spreads_ = spreads;
         return this;
      }

      public AverageONLeg withRateCutoff(int rateCutoff)
      {
         rateCutoff_ = rateCutoff;
         return this;
      }

      public AverageONLeg withPaymentCalendar(Calendar calendar)
      {
         paymentCalendar_ = calendar;
         return this;
      }

      public AverageONLeg withAverageONIndexedCouponPricer(AverageONIndexedCouponPricer couponPricer)
      {
         couponPricer_ = couponPricer;
         return this;
      }

      //operator Leg()  
      public List<CashFlow> Get()
      {

         Utils.QL_REQUIRE(!notionals_.empty(), () => "No notional given for average overnight leg.");

         List<CashFlow> cashflows = new List<CashFlow>();
         Date startDate = new Date();
         Date endDate = new Date();
         Date paymentDate;

         Calendar calendar;
         if (!paymentCalendar_.empty())
         {
            calendar = paymentCalendar_;
         }
         else
         {
            calendar = schedule_.calendar();
         }

         int numPeriods = schedule_.size() - 1;
         for (int i = 0; i < numPeriods; ++i)
         {
            startDate = schedule_.date(i);
            endDate = schedule_.date(i + 1);
            paymentDate = calendar.adjust(endDate, paymentAdjustment_);


            AverageONIndexedCoupon cashflow = new AverageONIndexedCoupon(paymentDate, Utils.Get<double>(notionals_, i, notionals_.Last()),
                                                                        startDate, endDate, overnightIndex_,
                                                                        Utils.Get<double>(gearings_, i, 1.0),
                                                                        Utils.Get<double>(spreads_, i, 0.0),
                                                                            rateCutoff_, paymentDayCounter_);

            if (couponPricer_!=null)
            {
               cashflow.setPricer(couponPricer_);
            }

            cashflows.Add(cashflow);
         }
         return cashflows;
      }

   }
}
