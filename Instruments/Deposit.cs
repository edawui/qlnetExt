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
   public class Deposit : Instrument
   {



      public new class Arguments : IPricingEngineArguments
      {
         public IborIndex index;
         public List<CashFlow> leg;
         public void validate()
         {
            Utils.QL_REQUIRE(leg.Count == 3, () => "deposit arugments: unexpected number of cash flows (" + leg.Count
                                                                                          + "), should be 3");
         }

      }

      public new class Results : Instrument.Results
      {
         public double fairRate;
         public override void reset()
         {
            base.reset();
            fairRate = double.NaN;
         }
      }

      public class Engine : GenericEngine<Arguments, Results>
      { }





      Date fixingDate_, startDate_, maturityDate_;
      IborIndex index_;
      List<CashFlow> leg_;
      // results
      double fairRate_;








      public Date fixingDate() { return fixingDate_; }
      public Date startDate() { return startDate_; }
      public Date maturityDate() { return maturityDate_; }
      public double fairRate()
      {
         calculate();
         return fairRate_;
      }
      public List<CashFlow> leg() { return leg_; }
      //@}

      public
      Deposit(double nominal, double rate, Period tenor, int fixingDays,
                        Calendar calendar, BusinessDayConvention convention, bool endOfMonth,
                        DayCounter dayCounter, Date tradeDate, bool isLong, Period forwardStart)
      {

         leg_ = new List<CashFlow>(3);
         index_ = new IborIndex("despoit-helper-index", tenor, fixingDays, new Currency(), calendar, convention,
                                                endOfMonth, dayCounter);
         // move to next good day
         Date referenceDate = calendar.adjust(tradeDate);
         startDate_ = index_.valueDate(referenceDate);
         fixingDate_ = index_.fixingDate(startDate_);
         maturityDate_ = index_.maturityDate(startDate_);
         double w = isLong ? 1.0 : -1.0;
         leg_[0] = new Redemption(-w * nominal, startDate_);
         leg_[1] =
             new FixedRateCoupon(maturityDate_, w * nominal, rate, dayCounter, startDate_, maturityDate_);
         leg_[2] = new Redemption(w * nominal, maturityDate_);
      }

      public override bool isExpired()
      {
         //QLNet.simple_event simple_Event = new simple_event(maturityDate_).hasOccurred();
         return new simple_event(maturityDate_).hasOccurred();

         //return detail.//::simple_event(maturityDate_).hasOccurred();
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         fairRate_ = double.NaN;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {

         Arguments arguments = (Arguments)args;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type in deposit");
         arguments.leg = leg_;
         arguments.index = index_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {

         base.fetchResults(r);
         Results results = (Results)(r);
         Utils.QL_REQUIRE(results != null, () => "wrong result type");
         fairRate_ = results.fairRate;
      }













   }


}
