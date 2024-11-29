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

/*! \file makeaverageois.hpp
    \brief Helper class to instantiate standard average ON indexed swaps.

        \ingroup instruments
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class MakeAverageOIS
   {

      Period swapTenor_;
      OvernightIndex overnightIndex_;
      Period onTenor_;
      double fixedRate_;
      Period fixedTenor_;
      DayCounter fixedDayCounter_;
      Period spotLagTenor_;
      Period forwardStart_;

      AverageOIS.Type type_;
      double nominal_;
      Date effectiveDate_;
      Date terminationDate_;
      Calendar spotLagCalendar_;

      Calendar fixedCalendar_;
      BusinessDayConvention fixedConvention_;
      BusinessDayConvention fixedTerminationDateConvention_;
      DateGeneration.Rule fixedRule_;
      bool fixedEndOfMonth_;
      Date fixedFirstDate_;
      Date fixedNextToLastDate_;
      BusinessDayConvention fixedPaymentAdjustment_;
      Calendar fixedPaymentCalendar_;

      Calendar onCalendar_;
      BusinessDayConvention onConvention_;
      BusinessDayConvention onTerminationDateConvention_;
      DateGeneration.Rule onRule_;
      bool onEndOfMonth_;
      Date onFirstDate_;
      Date onNextToLastDate_;
      int rateCutoff_;
      double onSpread_;
      double onGearing_;
      DayCounter onDayCounter_;
      BusinessDayConvention onPaymentAdjustment_;
      Calendar onPaymentCalendar_;

      IPricingEngine engine_;
      AverageONIndexedCouponPricer onCouponPricer_;




      public MakeAverageOIS(Period swapTenor, OvernightIndex overnightIndex,
                                 Period onTenor, double fixedRate, Period fixedTenor,
                                 DayCounter fixedDayCounter, Period spotLagTenor,
                                 Period forwardStart)

      {
         swapTenor_ = swapTenor; overnightIndex_ = overnightIndex; onTenor_ = onTenor; fixedRate_ = fixedRate;
         fixedTenor_ = fixedTenor; fixedDayCounter_ = fixedDayCounter; spotLagTenor_ = spotLagTenor;
         forwardStart_ = forwardStart;
         type_ = AverageOIS.Type.Receiver; nominal_ = 1.0; effectiveDate_ = new Date();
         spotLagCalendar_ = overnightIndex.fixingCalendar(); fixedCalendar_ = new WeekendsOnly(); fixedConvention_ = BusinessDayConvention.Unadjusted;
         fixedTerminationDateConvention_ = BusinessDayConvention.Unadjusted; fixedRule_ = DateGeneration.Rule.Backward; fixedEndOfMonth_ = false;
         fixedFirstDate_ = new Date(); fixedNextToLastDate_ = new Date();
         fixedPaymentAdjustment_ = overnightIndex.businessDayConvention();
         fixedPaymentCalendar_ = overnightIndex.fixingCalendar(); onCalendar_ = overnightIndex.fixingCalendar();
         onConvention_ = overnightIndex.businessDayConvention();
         onTerminationDateConvention_ = overnightIndex.businessDayConvention(); onRule_ = DateGeneration.Rule.Backward;
         onEndOfMonth_ = false; onFirstDate_ = new Date(); onNextToLastDate_ = new Date(); rateCutoff_ = 0; onSpread_ = 0.0;
         onGearing_ = 1.0; onDayCounter_ = overnightIndex.dayCounter();
         onPaymentAdjustment_ = overnightIndex.businessDayConvention();
         onPaymentCalendar_ = overnightIndex.fixingCalendar();
         onCouponPricer_ = new AverageONIndexedCouponPricer();
      }

      //public AverageOIS value()
      //{
      //   AverageOIS swap = this;
      //   return swap;
      //}

     public AverageOIS GetAverageOIS()
      {

         // Deduce the effective date if it is not given.
         Date effectiveDate;
         if (effectiveDate_ != new Date())
         {
            effectiveDate = effectiveDate_;
         }
         else
         {
            Date valuationDate = Settings.evaluationDate();
            Date spotDate = spotLagCalendar_.advance(valuationDate, spotLagTenor_);
            effectiveDate = spotDate + forwardStart_;
         }

         // Deduce the termination date if it is not given.
         Date terminationDate;
         if (terminationDate_ != new Date())
         {
            terminationDate = terminationDate_;
         }
         else
         {
            terminationDate = effectiveDate + swapTenor_;
         }

         Schedule fixedSchedule = new Schedule(effectiveDate, terminationDate, fixedTenor_, fixedCalendar_, fixedConvention_,
                                fixedTerminationDateConvention_, fixedRule_, fixedEndOfMonth_, fixedFirstDate_,
                                fixedNextToLastDate_);

         Schedule onSchedule = new Schedule(effectiveDate, terminationDate, onTenor_, onCalendar_, onConvention_,
                             onTerminationDateConvention_, onRule_, onEndOfMonth_, onFirstDate_, onNextToLastDate_);

         AverageOIS swap =// new AverageOIS(
                 new AverageOIS(type_, nominal_, fixedSchedule, fixedRate_, fixedDayCounter_, fixedPaymentAdjustment_,
                                fixedPaymentCalendar_, onSchedule, overnightIndex_, onPaymentAdjustment_, onPaymentCalendar_,
                                rateCutoff_, onSpread_, onGearing_, onDayCounter_, onCouponPricer_);//);

         swap.setPricingEngine(engine_);
         return swap;
      }

      public MakeAverageOIS receiveFixed(bool receiveFixed)
      {
         type_ = receiveFixed ? AverageOIS.Type.Receiver : AverageOIS.Type.Payer;
         return this;
      }

      public MakeAverageOIS withType(AverageOIS.Type type)
      {
         type_ = type;
         return this;
      }

      public MakeAverageOIS withNominal(double nominal)
      {
         nominal_ = nominal;
         return this;
      }

      public MakeAverageOIS withEffectiveDate(Date effectiveDate)
      {
         effectiveDate_ = effectiveDate;
         return this;
      }

      public MakeAverageOIS withTerminationDate(Date terminationDate)
      {
         terminationDate_ = terminationDate;
         swapTenor_ = new Period();
         return this;
      }

      public MakeAverageOIS withRule(DateGeneration.Rule rule)
      {
         fixedRule_ = rule;
         onRule_ = rule;
         return this;
      }

      public MakeAverageOIS withSpotLagCalendar(Calendar spotLagCalendar)
      {
         spotLagCalendar_ = spotLagCalendar;
         return this;
      }

      public MakeAverageOIS withFixedCalendar(Calendar fixedCalendar)
      {
         fixedCalendar_ = fixedCalendar;
         return this;
      }

      public MakeAverageOIS withFixedConvention(BusinessDayConvention fixedConvention)
      {
         fixedConvention_ = fixedConvention;
         return this;
      }

      public MakeAverageOIS      withFixedTerminationDateConvention(BusinessDayConvention fixedTerminationDateConvention)
      {
         fixedTerminationDateConvention_ = fixedTerminationDateConvention;
         return this;
      }

      public MakeAverageOIS withFixedRule(DateGeneration.Rule fixedRule)
      {
         fixedRule_ = fixedRule;
         return this;
      }

      public MakeAverageOIS withFixedEndOfMonth(bool fixedEndOfMonth)
      {
         fixedEndOfMonth_ = fixedEndOfMonth;
         return this;
      }

      public MakeAverageOIS withFixedFirstDate(Date fixedFirstDate)
      {
         fixedFirstDate_ = fixedFirstDate;
         return this;
      }

      public MakeAverageOIS withFixedNextToLastDate(Date fixedNextToLastDate)
      {
         fixedNextToLastDate_ = fixedNextToLastDate;
         return this;
      }

      public MakeAverageOIS withFixedPaymentAdjustment(BusinessDayConvention fixedPaymentAdjustment)
      {
         fixedPaymentAdjustment_ = fixedPaymentAdjustment;
         return this;
      }

      public MakeAverageOIS withFixedPaymentCalendar(Calendar fixedPaymentCalendar)
      {
         fixedPaymentCalendar_ = fixedPaymentCalendar;
         return this;
      }

      public MakeAverageOIS withONCalendar(Calendar onCalendar)
      {
         onCalendar_ = onCalendar;
         return this;
      }

      public MakeAverageOIS withONConvention(BusinessDayConvention onConvention)
      {
         onConvention_ = onConvention;
         return this;
      }

      public MakeAverageOIS withONTerminationDateConvention(BusinessDayConvention onTerminationDateConvention)
      {
         onTerminationDateConvention_ = onTerminationDateConvention;
         return this;
      }

      public MakeAverageOIS withONRule(DateGeneration.Rule onRule)
      {
         onRule_ = onRule;
         return this;
      }

      public MakeAverageOIS withONEndOfMonth(bool onEndOfMonth)
      {
         onEndOfMonth_ = onEndOfMonth;
         return this;
      }

      public MakeAverageOIS withONFirstDate(Date onFirstDate)
      {
         onFirstDate_ = onFirstDate;
         return this;
      }

      public MakeAverageOIS withONNextToLastDate(Date onNextToLastDate)
      {
         onNextToLastDate_ = onNextToLastDate;
         return this;
      }

      public MakeAverageOIS withRateCutoff(int rateCutoff)
      {
         rateCutoff_ = rateCutoff;
         return this;
      }

      public MakeAverageOIS withONSpread(double onSpread)
      {
         onSpread_ = onSpread;
         return this;
      }

      public MakeAverageOIS withONGearing(double onGearing)
      {
         onGearing_ = onGearing;
         return this;
      }

      public MakeAverageOIS withONDayCounter(DayCounter onDayCounter)
      {
         onDayCounter_ = onDayCounter;
         return this;
      }

      public MakeAverageOIS withONPaymentAdjustment(BusinessDayConvention onPaymentAdjustment)
      {
         onPaymentAdjustment_ = onPaymentAdjustment;
         return this;
      }

      public MakeAverageOIS withONPaymentCalendar(Calendar onPaymentCalendar)
      {
         onPaymentCalendar_ = onPaymentCalendar;
         return this;
      }

      public MakeAverageOIS
      withONCouponPricer(AverageONIndexedCouponPricer onCouponPricer)
      {
         onCouponPricer_ = onCouponPricer;
         return this;
      }

      public MakeAverageOIS withDiscountingTermStructure(Handle<YieldTermStructure> discountCurve)
      {
         bool includeSettlementDateFlows = false;
         engine_ = new DiscountingSwapEngine(discountCurve, includeSettlementDateFlows);
         return this;
      }

      public MakeAverageOIS withPricingEngine(IPricingEngine engine)
      {
         engine_ = engine;
         return this;
      }

   }
}
