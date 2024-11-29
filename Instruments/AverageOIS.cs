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

/*! \file averageois.hpp
    \brief Swap of arithmetic average overnight index against fixed

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
   // using Leg = List<CashFlow>;

   public class AverageOIS : Swap
   {

      public enum Type { Receiver = -1, Payer = 1 };




      Type type_;
      List<double> nominals_;

      List<double> fixedRates_;
      DayCounter fixedDayCounter_;
      BusinessDayConvention fixedPaymentAdjustment_;
      Calendar fixedPaymentCalendar_;

      OvernightIndex overnightIndex_;
      BusinessDayConvention onPaymentAdjustment_;
      Calendar onPaymentCalendar_;
      int rateCutoff_;
      List<double> onSpreads_;
      List<double> onGearings_;
      DayCounter onDayCounter_;
      AverageONIndexedCouponPricer onCouponPricer_;




      //! Arithmetic average ON leg vs. fixed leg ructor.


      public AverageOIS(Type type, double nominal, Schedule fixedLegSchedule, double fixedRate,
                              DayCounter fixedDCB, BusinessDayConvention fixedLegPaymentAdjustment,
                              Calendar fixedLegPaymentCalendar, Schedule onLegSchedule,
                             OvernightIndex overnightIndex,
                             BusinessDayConvention onLegPaymentAdjustment, Calendar onLegPaymentCalendar,
                             int rateCutoff, double onLegSpread, double onLegGearing, DayCounter onLegDCB,
                             AverageONIndexedCouponPricer onLegCouponPricer)
          : base(2)
      {
         type_ = type;
         nominals_ = Enumerable.Repeat<double>(nominal, 1).ToList();
         fixedRates_ = Enumerable.Repeat<double>(fixedRate, 1).ToList();
         fixedDayCounter_ = fixedDCB; fixedPaymentAdjustment_ = fixedLegPaymentAdjustment;
         fixedPaymentCalendar_ = fixedLegPaymentCalendar; overnightIndex_ = overnightIndex;
         onPaymentAdjustment_ = onLegPaymentAdjustment; onPaymentCalendar_ = onLegPaymentCalendar; rateCutoff_ = rateCutoff;
         onSpreads_ = Enumerable.Repeat<double>(onLegSpread, 1).ToList();
         onGearings_ = Enumerable.Repeat<double>(onLegGearing, 1).ToList();
         onDayCounter_ = onLegDCB; onCouponPricer_ = onLegCouponPricer;


         initialize(fixedLegSchedule, onLegSchedule);
      }


      /*! Arithmetic average ON leg vs. fixed leg ructor, allowing for
     varying nominals, fixed rates, ON leg spreads and ON leg gearings.
 */

      public AverageOIS(Type type, List<double> nominals, Schedule fixedLegSchedule,
                          List<double> fixedRates, DayCounter fixedDCB,
                          BusinessDayConvention fixedLegPaymentAdjustment, Calendar fixedLegPaymentCalendar,
                           Schedule onLegSchedule, OvernightIndex overnightIndex,
                          BusinessDayConvention onLegPaymentAdjustment, Calendar onLegPaymentCalendar,
                          int rateCutoff, List<double> onLegSpreads, List<double> onLegGearings,
                           DayCounter onLegDCB,
                          AverageONIndexedCouponPricer onLegCouponPricer)
       : base(2)
      {
         type_ = type;
         nominals_ = nominals;
         fixedRates_ = fixedRates;
         fixedDayCounter_ = fixedDCB;
         fixedPaymentAdjustment_ = fixedLegPaymentAdjustment;
         fixedPaymentCalendar_ = fixedLegPaymentCalendar;
         overnightIndex_ = overnightIndex;
         onPaymentAdjustment_ = onLegPaymentAdjustment;
         onPaymentCalendar_ = onLegPaymentCalendar;
         rateCutoff_ = rateCutoff;
         onSpreads_ = onLegSpreads;
         onGearings_ = onLegGearings;
         onDayCounter_ = onLegDCB;
         onCouponPricer_ = onLegCouponPricer;


         initialize(fixedLegSchedule, onLegSchedule);
      }

      private void initialize(Schedule fixedLegSchedule, Schedule onLegSchedule)
      {
         // Fixed leg.
         legs_[0] = ((FixedRateLeg)(((FixedRateLeg)(new FixedRateLeg(fixedLegSchedule)
                        .withNotionals(nominals_)))
                        .withCouponRates(fixedRates_, fixedDayCounter_)
                        .withPaymentAdjustment(fixedPaymentAdjustment_)))
                        .withPaymentCalendar(fixedPaymentCalendar_);

         // Average ON leg.
         AverageONLeg tempAverageONLeg = new AverageONLeg(onLegSchedule, overnightIndex_)
                                             .withNotionals(nominals_)
                                             .withPaymentAdjustment(onPaymentAdjustment_)
                                             .withPaymentCalendar(onPaymentCalendar_)
                                             .withRateCutoff(rateCutoff_)
                                             .withSpreads(onSpreads_)
                                             .withGearings(onGearings_)
                                             .withPaymentDayCounter(onDayCounter_);


         if (onCouponPricer_ != null)
         {
            tempAverageONLeg = tempAverageONLeg.withAverageONIndexedCouponPricer(onCouponPricer_);
         }

         legs_[1] = tempAverageONLeg.Get();

         // Set the fixed and ON leg to pay (receive) and receive (pay) resp.
         switch (type_)
         {

            case AverageOIS.Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;

            case AverageOIS.Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;

            default:
               Utils.QL_FAIL("Unknown average ON index swap type");
               break;
         }
      }


      public double nominal()
      {
         Utils.QL_REQUIRE(nominals_.Count == 1, () => "Swap has varying nominals");
         return nominals_[0];
      }

      public double fixedRate()
      {
         Utils.QL_REQUIRE(fixedRates_.Count == 1, () => "Swap has varying fixed rates");
         return fixedRates_[0];
      }

      public double onSpread()
      {
         Utils.QL_REQUIRE(onSpreads_.Count == 1, () => "Swap has varying ON spreads");
         return onSpreads_[0];
      }

      public double onGearing()
      {
         Utils.QL_REQUIRE(onGearings_.Count == 1, () => "Swap has varying ON gearings");
         return onGearings_[0];
      }

      public double fixedLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[0] != double.NaN, () => "fixedLegBPS not available");
         return legBPS_[0].Value;
      }

      public double fixedLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[0] != double.NaN, () => "fixedLegNPV not available");
         return legNPV_[0].Value;
      }

      public double fairRate()
      {
         //static  double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         calculate();
         return -overnightLegNPV() / (fixedLegBPS() / basisPoint);
      }

      public double overnightLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[1] != double.NaN, () => "overnightLegBPS not available");
         return legBPS_[1].Value;
      }

      public double overnightLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[1] != double.NaN, () => "overnightLegNPV not available");
         return legNPV_[1].Value;
      }

      public double fairSpread()
      {
         Utils.QL_REQUIRE(onSpreads_.Count == 1, () => "fairSpread not implemented for varying spreads.");
         //static  double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         calculate();
         return onSpreads_[0] - NPV_.Value / (overnightLegBPS() / basisPoint);
      }

      public void setONIndexedCouponPricer(AverageONIndexedCouponPricer onCouponPricer)
      {
         QLNetExt.PricerSetter.setCouponPricer(legs_[1], onCouponPricer);
         update();
      }




      //@{
      public Type type() { return type_; }

      //public  double nominal() ;
      public List<double> nominals() { return nominals_; }

      //Rate fixedRate() ;
      public List<double> fixedRates() { return fixedRates_; }
      public DayCounter fixedDayCounter() { return fixedDayCounter_; }

      public OvernightIndex overnightIndex() { return overnightIndex_; }
      public int rateCutoff() { return rateCutoff_; }
      //Spread onSpread() ;
      public List<double> onSpreads() { return onSpreads_; }
      //   double onGearing() ;
      public List<double> onGearings() { return onGearings_; }
      public DayCounter onDayCounter() { return onDayCounter_; }

      public List<CashFlow> fixedLeg() { return legs_[0]; }
      public List<CashFlow> overnightLeg() { return legs_[1]; }

   }
}
