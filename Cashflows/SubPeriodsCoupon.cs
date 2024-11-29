
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
   using Leg = List<CashFlow>;//> Leg;

   public class SubPeriodsCoupon : FloatingRateCoupon
   {
      public enum Type { Averaging, Compounding };


      Type type_;
      bool includeSpread_;
      List<Date> valueDates_, fixingDates_;
      List<double> fixings_;
      int numPeriods_;
      List<double> accrualFractions_;



      public SubPeriodsCoupon(Date paymentDate, double nominal, Date startDate, Date endDate,
                                      InterestRateIndex index, Type type,
                                      BusinessDayConvention convention, double spread, DayCounter dayCounter,
                                      bool includeSpread, double gearing)
       : base(paymentDate, nominal, startDate, endDate, index.fixingDays(), index, gearing, spread, new Date(),
                           new Date(), dayCounter, false)
      {
         type_ = type;
         includeSpread_ = includeSpread;


         // Populate the value dates.
         Schedule sch = (new MakeSchedule())
                            .from(startDate)
                            .to(endDate)
                            .withTenor(index.tenor())
                            .withCalendar(index.fixingCalendar())
                            .withConvention(convention)
                            .withTerminationDateConvention(convention)
                            .backwards()
                            .value();
         valueDates_ = sch.dates();
         Utils.QL_REQUIRE(valueDates_.Count >= 2, () => "Degenerate schedule.");//QL_ENSURE(valueDates_.size() >= 2, "Degenerate schedule.");

         // Populate the fixing dates.
         numPeriods_ = valueDates_.Count - 1;
         if (index.fixingDays() == 0)
         {
            fixingDates_ = valueDates_.GetRange(0, valueDates_.Count - 1);//List<Date>(valueDates_.begin(), valueDates_.end() - 1);
         }
         else
         {
            fixingDates_.Resize(numPeriods_);
            for (int i = 0; i < numPeriods_; ++i)
               fixingDates_[i] = index.fixingDate(valueDates_[i]);
         }

         // Populate the accrual periods.
         accrualFractions_.Resize(numPeriods_);
         for (int i = 0; i < numPeriods_; ++i)
         {
            accrualFractions_[i] = dayCounter.yearFraction(valueDates_[i], valueDates_[i + 1]);
         }
      }

      public List<double> indexFixings()
      {

         fixings_.Resize(numPeriods_);

         for (int i = 0; i < numPeriods_; ++i)
         {
            fixings_[i] = index_.fixing(fixingDates_[i]);
         }

         return fixings_;
      }

      public override void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");


         /*
         SubPeriodsCoupon v1 = (SubPeriodsCoupon)v;
        
         if (v1 != null)
         {
            v1.visit(this);//todo debug
         }
         else
         {
            base.accept(v);
         }*/
      }

      //! fixing dates for the sub-periods
      public List<Date> fixingDates() { return fixingDates_; }
      //! accrual periods for the sub-periods
      public List<double> accrualFractions() { return accrualFractions_; }
      //! fixings for the sub-periods
      //public List<Rate> indexFixings() ;
      //! value dates for the sub-periods
      public List<Date> valueDates() { return valueDates_; }
      //! whether sub-period fixings are averaged or compounded
      public Type type() { return type_; }
      //! whether to include/exclude spread in compounding/averaging
      public bool includeSpread() { return includeSpread_; }
      //! Need to be able to change spread to solve for fair spread
      //public override double spread()  { return spread_; }
      //@}
      //! \name FloatingRateCoupon interface
      //@{
      //! the date when the coupon is fully determined
      public override Date fixingDate() { return fixingDates_.Last(); }

   }


   public class SubPeriodsLeg
   {



      Schedule schedule_;
      InterestRateIndex index_;

      List<double> notionals_;
      DayCounter paymentDayCounter_;
      BusinessDayConvention paymentAdjustment_;
      List<double> gearings_;
      List<double> spreads_;
      Calendar paymentCalendar_;
      SubPeriodsCoupon.Type type_;
      bool includeSpread_;

      public SubPeriodsLeg(Schedule schedule, InterestRateIndex index)
      {
         schedule_ = schedule;
         index_ = index;
         notionals_ = Enumerable.Repeat<double>(1.0, 1).ToList();



         paymentAdjustment_ = BusinessDayConvention.Following;
         paymentCalendar_ = new Calendar();
         type_ = SubPeriodsCoupon.Type.Compounding;
      }

      public SubPeriodsLeg withNotional(double notional)
      {
         notionals_ = Enumerable.Repeat<double>(notional, 1).ToList();
         return this;
      }

      public SubPeriodsLeg withNotionals(List<double> notionals)
      {
         notionals_ = notionals;
         return this;
      }

      public SubPeriodsLeg withPaymentDayCounter(DayCounter dayCounter)
      {
         paymentDayCounter_ = dayCounter;
         return this;
      }

      public SubPeriodsLeg withPaymentAdjustment(BusinessDayConvention convention)
      {
         paymentAdjustment_ = convention;
         return this;
      }

      public SubPeriodsLeg withGearing(double gearing)
      {
         gearings_ = Enumerable.Repeat<double>(gearing, 1).ToList();
         return this;
      }

      public SubPeriodsLeg withGearings(List<double> gearings)
      {
         gearings_ = gearings;
         return this;
      }

      public SubPeriodsLeg withSpread(double spread)
      {
         spreads_ = Enumerable.Repeat<double>(spread, 1).ToList();
         return this;
      }

      public SubPeriodsLeg withSpreads(List<double> spreads)
      {
         spreads_ = spreads;
         return this;
      }

      public SubPeriodsLeg withPaymentCalendar(Calendar calendar)
      {
         paymentCalendar_ = calendar;
         return this;
      }

      public SubPeriodsLeg withType(SubPeriodsCoupon.Type type)
      {
         type_ = type;
         return this;
      }

      public SubPeriodsLeg includeSpread(bool includeSpread)
      {
         includeSpread_ = includeSpread;
         return this;
      }

      public Leg value()
      {

         Leg cashflows = new Leg();
         Date startDate = new Date();
         Date endDate = new Date();
         Date paymentDate = new Date();

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
         if (numPeriods == 0)
            return cashflows;

         startDate = schedule_.date(0);
         for (int i = 0; i < numPeriods; ++i)
         {
            endDate = schedule_.date(i + 1);
            paymentDate = calendar.adjust(endDate, paymentAdjustment_);
            // the sub periods coupon might produce degenerated schedules, in this
            // case we just join the current period with the next one
            // we catch all QL exceptions here, although we should only pick the one
            // that is thrown in case of a degenerated schedule, but there is no way
            // of identifying it except parsing the exception text, which isn't a
            // clean solution either
            try
            {
               SubPeriodsCoupon cashflow =
                   new SubPeriodsCoupon(paymentDate, Utils.Get<double>(notionals_, i, notionals_.Last()), startDate, endDate,
                                        index_, type_, paymentAdjustment_, Utils.Get<double>(spreads_, i, 0.0),
                                        paymentDayCounter_, includeSpread_, Utils.Get<double>(gearings_, i, 1.0));

               cashflows.Add(cashflow);
               //cashflows.Append<CashFlow>(cashflow);
               startDate = endDate;
            }
            catch (Exception Err)// QuantLib::Error&)//todo
            {
            }
         }

         SubPeriodsCouponPricer pricer = new SubPeriodsCouponPricer();
         PricerSetter.setCouponPricer(cashflows, pricer);

         return cashflows;
      }
   }

}

