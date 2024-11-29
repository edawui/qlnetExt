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

/*! \file qle/instruments/currencyswap.hpp
    \brief Interest rate swap with extended interface

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
   //public class Leg : List<CashFlow>
   //{

   //}

   public class CurrencySwap : Instrument
   {

      public class Arguments : IPricingEngineArguments
      {

         public List<List<CashFlow>> legs;
         public List<double> payer;
         public List<Currency> currency;


         public Arguments(List<List<CashFlow>> _legs, List<double> _payer, List<Currency> _currency)
         {
            legs = _legs;
            payer = _payer;
            currency = _currency;
         }

         public Arguments()
         { }

         public void validate()
         {
            Utils.QL_REQUIRE(legs.Count == payer.Count, () => "number of legs and multipliers differ");
            Utils.QL_REQUIRE(currency.Count == legs.Count, () => "number of legs and currencies differ");
         }


         //void IPricingEngineArguments.validate()
         //   {
         //      throw new NotImplementedException();
         //   }

      }



      public new class Results : Instrument.Results
      {
         public List<double> legNPV, inCcyLegNPV;
         public List<double> legBPS, inCcyLegBPS;
         public List<double> startDiscounts, endDiscounts;
         public double npvDateDiscount;
         public override void reset()
         {
            base.reset();
            legNPV.Clear();
            legBPS.Clear();
            inCcyLegNPV.Clear();
            inCcyLegBPS.Clear();
            startDiscounts.Clear();
            endDiscounts.Clear();
            npvDateDiscount = double.NaN;
         }

      }


     public  class Engine :  GenericEngine<CurrencySwap.Arguments, CurrencySwap.Results> {};


   protected List<List<CashFlow>> legs_;
      protected List<double> payer_;
      protected List<Currency> currency_;
      protected List<double> legNPV_, inCcyLegNPV_;
      protected List<double> legBPS_, inCcyLegBPS_;
      protected List<double> startDiscounts_, endDiscounts_;
      protected double npvDateDiscount_;


      public CurrencySwap(int nLegs) : base()
      {
         legs_ = new List<List<CashFlow>>(nLegs);
         payer_ = new List<double>(nLegs);
         currency_ = new List<Currency>(nLegs);
         legNPV_ = new List<double>(nLegs);
         inCcyLegNPV_ = new List<double>(nLegs);
         legBPS_ = new List<double>(nLegs);
         inCcyLegBPS_ = new List<double>(nLegs);
         startDiscounts_ = new List<double>(nLegs);
         endDiscounts_ = new List<double>(nLegs);
      }

      public CurrencySwap(List<List<CashFlow>> legs, List<bool> payer,
                               List<Currency> currency) : base()
      {
         legs_ = legs;
         payer_ = Enumerable.Repeat<double>(1.0, legs.Count).ToList();
         currency_ = currency;

         legNPV_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();
         inCcyLegNPV_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();
         legBPS_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();
         inCcyLegBPS_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();
         startDiscounts_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();
         endDiscounts_ = Enumerable.Repeat<double>(0.0, legs.Count).ToList();


         npvDateDiscount_ = 0.0;

         Utils.QL_REQUIRE(payer.Count == legs_.Count, () => "size mismatch between payer (" + payer.Count + ") and legs ("
                                                                                  + legs_.Count + ")");
         Utils.QL_REQUIRE(currency.Count == legs_.Count, () => "size mismatch between currency (" + currency.Count + ") and legs ("
                                                                                        + legs_.Count + ")");
         for (int j = 0; j < legs_.Count; ++j)
         {
            if (payer[j])
               payer_[j] = -1.0;
            for (int i = 0; i < legs_[j].Count; ++i) //Leg::iterator i = legs_[j].begin(); i != legs_[j].end(); ++i)
               legs_[j][i].registerWith(update);//registerWith(*i);
         }
      }

      public override bool isExpired()
      {
         for (int j = 0; j < legs_.Count; ++j)
         {
            for (int i = 0; i < legs_[j].Count; ++i)
               if (!(legs_[j][i].hasOccurred()))
                  return false;
         }
         return true;
      }


      protected override void setupExpired()
      {
         base.setupExpired();

         legBPS_ = Enumerable.Repeat<double>(0.0, legBPS_.Count).ToList();
         legNPV_ = Enumerable.Repeat<double>(0.0, legNPV_.Count).ToList();
         inCcyLegNPV_ = Enumerable.Repeat<double>(0.0, inCcyLegNPV_.Count).ToList();
         inCcyLegBPS_ = Enumerable.Repeat<double>(0.0, inCcyLegBPS_.Count).ToList();
         startDiscounts_ = Enumerable.Repeat<double>(0.0, startDiscounts_.Count).ToList();
         endDiscounts_ = Enumerable.Repeat<double>(0.0, endDiscounts_.Count).ToList();

         npvDateDiscount_ = 0.0;
      }

      public override void setupArguments(QLNet.IPricingEngineArguments args)
      {
         CurrencySwap.Arguments arguments = (CurrencySwap.Arguments)args;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type");

         arguments.legs = legs_;
         arguments.payer = payer_;
         arguments.currency = currency_;
      }

      public override void fetchResults(QLNet.IPricingEngineResults r)
      {
         base.fetchResults(r);

         CurrencySwap.Results results = (CurrencySwap.Results)r;
         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         if (!results.legNPV.empty())
         {
            Utils.QL_REQUIRE(results.legNPV.Count == legNPV_.Count, () => "wrong number of leg NPV returned");
            legNPV_ = results.legNPV;
         }
         else
         {
            legNPV_ = Enumerable.Repeat<double>(double.NaN, legNPV_.Count).ToList();

            // std::fill(legNPV_.begin(), legNPV_.end(), Null<double>());
         }

         if (!results.legBPS.empty())
         {
            Utils.QL_REQUIRE(results.legBPS.Count == legBPS_.Count, () => "wrong number of leg BPS returned");
            legBPS_ = results.legBPS;
         }
         else
         {
            legBPS_ = Enumerable.Repeat<double>(double.NaN, legBPS_.Count).ToList();
            // std::fill(legBPS_.begin(), legBPS_.end(), Null<double>());
         }

         if (!results.inCcyLegNPV.empty())
         {
            Utils.QL_REQUIRE(results.inCcyLegNPV.Count == inCcyLegNPV_.Count, () => "wrong number of leg NPV returned");
            inCcyLegNPV_ = results.inCcyLegNPV;
         }
         else
         {
            inCcyLegNPV_ = Enumerable.Repeat<double>(double.NaN, inCcyLegNPV_.Count).ToList();
            //std::fill(inCcyLegNPV_.begin(), inCcyLegNPV_.end(), Null<double>());
         }

         if (!results.inCcyLegBPS.empty())
         {
            Utils.QL_REQUIRE(results.inCcyLegBPS.Count == inCcyLegBPS_.Count, () => "wrong number of leg BPS returned");
            inCcyLegBPS_ = results.inCcyLegBPS;
         }
         else
         {
            inCcyLegBPS_ = Enumerable.Repeat<double>(double.NaN, inCcyLegBPS_.Count).ToList();
            //std::fill(inCcyLegBPS_.begin(), inCcyLegBPS_.end(), Null<double>());
         }

         if (!results.startDiscounts.empty())
         {
            Utils.QL_REQUIRE(results.startDiscounts.Count == startDiscounts_.Count,
                       () => "wrong number of leg start discounts returned");
            startDiscounts_ = results.startDiscounts;
         }
         else
         {
            startDiscounts_ = Enumerable.Repeat<double>(double.NaN, startDiscounts_.Count).ToList();
            // std::fill(startDiscounts_.begin(), startDiscounts_.end(), Null<DiscountFactor>());
         }

         if (!results.endDiscounts.empty())
         {
            Utils.QL_REQUIRE(results.endDiscounts.Count == endDiscounts_.Count, () => "wrong number of leg end discounts returned");
            endDiscounts_ = results.endDiscounts;
         }
         else
         {
            endDiscounts_ = Enumerable.Repeat<double>(double.NaN, endDiscounts_.Count).ToList();
            //std::fill(endDiscounts_.begin(), endDiscounts_.end(), Null<DiscountFactor>());
         }

         if (results.npvDateDiscount != double.NaN)// Null<DiscountFactor>())
         {
            npvDateDiscount_ = results.npvDateDiscount;
         }
         else
         {
            npvDateDiscount_ = double.NaN; // Null<DiscountFactor>();
         }
      }



      public Date startDate()
      {
         Utils.QL_REQUIRE(!legs_.empty(), () => "no legs given");
         Date d = CashFlows.startDate(legs_[0]);
         for (int j = 1; j < legs_.Count; ++j)
            d = Date.Min(d, CashFlows.startDate(legs_[j]));
         return d;
      }

      public Date maturityDate()
      {
         Utils.QL_REQUIRE(!legs_.empty(), () => "no legs given");
         Date d = CashFlows.maturityDate(legs_[0]);
         for (int j = 1; j < legs_.Count; ++j)
            d = Date.Max(d, CashFlows.maturityDate(legs_[j]));
         return d;
      }

      //public void CurrencySwap::arguments::validate() {
      //    Utils.QL_REQUIRE(legs.Count == payer.Count, "number of legs and multipliers differ");
      //    Utils.QL_REQUIRE(currency.Count == legs.Count, "number of legs and currencies differ");
      //}

      //void CurrencySwap::results::reset()
      //{
      //   Instrument::results::reset();
      //   legNPV.clear();
      //   legBPS.clear();
      //   inCcyLegNPV.clear();
      //   inCcyLegBPS.clear();
      //   startDiscounts.clear();
      //   endDiscounts.clear();
      //   npvDateDiscount = Null<DiscountFactor>();
      //}




      public double legBPS(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         calculate();
         return legBPS_[j];
      }
      public double legNPV(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return legNPV_[j];
      }
      public double inCcyLegBPS(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         calculate();
         return inCcyLegBPS_[j];
      }
      public double inCcyLegNPV(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return inCcyLegNPV_[j];
      }
      public double startDiscounts(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return startDiscounts_[j];
      }
      public double endDiscounts(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return endDiscounts_[j];
      }
      public double npvDateDiscount()
      {
         calculate();
         return npvDateDiscount_;
      }
      public List<CashFlow> leg(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         return legs_[j];
      }
      public Currency legCurrency(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         return currency_[j];
      }
      public List<List<CashFlow>> legs() { return legs_; }
      public List<Currency> currencies() { return currency_; }
      public IPricingEngine engine() { return engine_; }
      //@}

   }

   public class VanillaCrossCurrencySwap : CurrencySwap
   {
      public VanillaCrossCurrencySwap(bool payFixed, Currency fixedCcy, double fixedNominal,
                                                    Schedule fixedSchedule, double fixedRate,
                                                    DayCounter fixedDayCount, Currency floatCcy,
                                                   double floatNominal, Schedule floatSchedule,
                                                    IborIndex iborIndex, double floatSpread,
                                                   BusinessDayConvention paymentConvention)
    : base(4)
      {

         BusinessDayConvention convention;
         if (!Enum.IsDefined(typeof(BusinessDayConvention), paymentConvention))
            convention = paymentConvention;
         else
            convention = floatSchedule.businessDayConvention();

         // fixed leg
         currency_[0] = fixedCcy;
         payer_[0] = (payFixed ? -1 : +1);
         legs_[0] = ((FixedRateLeg)(new FixedRateLeg(fixedSchedule)
                        .withNotionals(fixedNominal)))
                        .withCouponRates(fixedRate, fixedDayCount)
                        .withPaymentAdjustment(convention)
                        .value();

         // add initial and final notional exchange
         currency_[1] = fixedCcy;
         payer_[1] = payer_[0];
         legs_[1].Add((CashFlow)(
             new SimpleCashFlow(-fixedNominal, fixedSchedule.calendar().adjust(fixedSchedule.dates().First(), convention))));
         legs_[1].Add((CashFlow)(
             new SimpleCashFlow(fixedNominal, fixedSchedule.calendar().adjust(fixedSchedule.dates().Last(), convention))));

         // floating leg
         currency_[2] = floatCcy;
         payer_[2] = (payFixed ? +1 : -1);
         legs_[2] = ((IborLeg)(((IborLeg)(new IborLeg(floatSchedule, iborIndex)
                        .withNotionals(floatNominal)))
                        .withPaymentDayCounter(iborIndex.dayCounter())
                        .withPaymentAdjustment(convention)))
                        .withSpreads(floatSpread);
         for (int i = 0; i < legs_[2].Count; ++i) //(Leg::_iterator i = legs_[2].begin(); i<legs_[2].end(); ++i)
            legs_[2][i].registerWith(update);//registerWith(*i);

         // add initial and final notional exchange
         currency_[3] = floatCcy;
         payer_[3] = payer_[2];
         legs_[3].Add((CashFlow)(
             new SimpleCashFlow(-floatNominal, floatSchedule.calendar().adjust(floatSchedule.dates().First(), convention))));
         legs_[3].Add((CashFlow)(
             new SimpleCashFlow(floatNominal, floatSchedule.calendar().adjust(floatSchedule.dates().Last(), convention))));
      }

   }

   //-------------------------------------------------------------------------

   public class CrossCurrencySwap : CurrencySwap
   {

      public CrossCurrencySwap(bool payFixed, Currency fixedCcy, List<double> fixedNominals,
                                          Schedule fixedSchedule, List<double> fixedRates,
                                          DayCounter fixedDayCount, Currency floatCcy,
                                         List<double> floatNominals, Schedule floatSchedule,
                                        IborIndex iborIndex, List<double> floatSpreads,
                                        BusinessDayConvention paymentConvention)
        : base(4)
      {

         BusinessDayConvention convention;
         if (Enum.IsDefined(typeof(BusinessDayConvention), paymentConvention))
            convention = paymentConvention;
         else
            convention = floatSchedule.businessDayConvention();

         // fixed leg
         currency_[0] = fixedCcy;
         payer_[0] = (payFixed ? -1 : +1);
         legs_[0] = ((FixedRateLeg)(new FixedRateLeg(fixedSchedule)
                        .withNotionals(fixedNominals)))
                        .withCouponRates(fixedRates, fixedDayCount)
                        .withPaymentAdjustment(convention);

         // add initial, interim and final notional flows
         currency_[1] = fixedCcy;
         payer_[1] = payer_[0];
         legs_[1].Add(
             (CashFlow)(new SimpleCashFlow(-fixedNominals[0], fixedSchedule.dates().First())));

         Utils.QL_REQUIRE(fixedNominals.Count < fixedSchedule.Count, () => "too many fixed nominals provided");
         for (int i = 1; i < fixedNominals.Count; i++)
         {
            double flow = fixedNominals[i - 1] - fixedNominals[i];
            legs_[1].Add((CashFlow)(
                new SimpleCashFlow(flow, fixedSchedule.calendar().adjust(fixedSchedule[i], convention))));
         }
         if (fixedNominals.Last() > 0)
            legs_[1].Add((CashFlow)(new SimpleCashFlow(
                fixedNominals.Last(), fixedSchedule.calendar().adjust(fixedSchedule.dates().Last(), convention))));

         // floating leg
         currency_[2] = floatCcy;
         payer_[2] = (payFixed ? +1 : -1);
         legs_[2] = ((IborLeg)(((IborLeg)(new IborLeg(floatSchedule, iborIndex)
                        .withNotionals(floatNominals)))
                        .withPaymentDayCounter(iborIndex.dayCounter())
                        .withPaymentAdjustment(convention)))
                        .withSpreads(floatSpreads);
         for (int i = 0; i < legs_[2].Count; ++i) //(Leg::_iterator i = legs_[2].begin(); i < legs_[2].end(); ++i)
            legs_[2][i].registerWith(update);// registerWith(*i);

         // add initial, interim and final notional flows
         currency_[3] = floatCcy;
         payer_[3] = payer_[2];
         legs_[3].Add((CashFlow)(new SimpleCashFlow(-floatNominals[0], floatSchedule.dates().First())));
         Utils.QL_REQUIRE(floatNominals.Count < floatSchedule.Count, () => "too many float nominals provided");
         for (int i = 1; i < floatNominals.Count; i++)
         {
            double flow = floatNominals[i - 1] - floatNominals[i];
            legs_[3].Add((CashFlow)(
                new SimpleCashFlow(flow, floatSchedule.calendar().adjust(floatSchedule[i], convention))));
         }
         if (floatNominals.Last() > 0)
            legs_[3].Add((CashFlow)(new SimpleCashFlow(
                floatNominals.Last(), floatSchedule.calendar().adjust(floatSchedule.dates().Last(), convention))));
      }

      //-------------------------------------------------------------------------
      public CrossCurrencySwap(bool pay1, Currency ccy1, List<double> nominals1, Schedule schedule1,
                                          List<double> rates1, DayCounter dayCount1, Currency ccy2,
                                          List<double> nominals2, Schedule schedule2, List<double> rates2,
                                           DayCounter dayCount2,
                                         BusinessDayConvention paymentConvention)
         : base(4)
      {

         BusinessDayConvention convention;
         if (Enum.IsDefined(typeof(BusinessDayConvention), paymentConvention))
            convention = paymentConvention;
         else
            convention = schedule1.businessDayConvention();

         // fixed leg 1
         currency_[0] = ccy1;
         payer_[0] = (pay1 ? -1 : +1);
         legs_[0] = ((FixedRateLeg)(new FixedRateLeg(schedule1)
                        .withNotionals(nominals1)))
                        .withCouponRates(rates1, dayCount1)
                        .withPaymentAdjustment(convention);

         // add initial, interim and final notional flows
         currency_[1] = ccy1;
         payer_[1] = payer_[0];
         legs_[1].Add((CashFlow)(
             new SimpleCashFlow(-nominals1[0], schedule1.calendar().adjust(schedule1.dates().First(), convention))));
         Utils.QL_REQUIRE(nominals1.Count < schedule1.Count, () => "too many fixed nominals provided, leg 1");
         for (int i = 1; i < nominals1.Count; i++)
         {
            double flow = nominals1[i - 1] - nominals1[i];
            legs_[1].Add((CashFlow)(
                new SimpleCashFlow(flow, schedule1.calendar().adjust(schedule1[i], convention))));
         }
         if (nominals1.Last() > 0)
            legs_[1].Add((CashFlow)(
                new SimpleCashFlow(nominals1.Last(), schedule1.calendar().adjust(schedule1.dates().Last(), convention))));

         // fixed leg 2
         currency_[2] = ccy2;
         payer_[2] = (pay1 ? +1 : -1);
         legs_[2] = ((FixedRateLeg)(new FixedRateLeg(schedule2)
                        .withNotionals(nominals2)))
                        .withCouponRates(rates2, dayCount2)
                        .withPaymentAdjustment(convention);

         // add initial, interim and final notional flows
         currency_[3] = ccy2;
         payer_[3] = payer_[2];
         legs_[3].Add((CashFlow)(
             new SimpleCashFlow(-nominals2[0], schedule2.calendar().adjust(schedule2.dates().First(), convention))));
         Utils.QL_REQUIRE(nominals2.Count < schedule2.Count, () => "too many fixed nominals provided, leg 2");
         for (int i = 1; i < nominals2.Count; i++)
         {
            double flow = nominals2[i - 1] - nominals2[i];
            legs_[3].Add((CashFlow)(
                new SimpleCashFlow(flow, schedule2.calendar().adjust(schedule2[i], convention))));
         }
         if (nominals2.Last() > 0)
            legs_[3].Add((CashFlow)(
                new SimpleCashFlow(nominals2.Last(), schedule2.calendar().adjust(schedule2.dates().Last(), convention))));
      }

      //-------------------------------------------------------------------------
      public CrossCurrencySwap(bool pay1, Currency ccy1, List<double> nominals1, Schedule schedule1,
                                          IborIndex iborIndex1, List<double> spreads1,
                                         Currency ccy2, List<double> nominals2, Schedule schedule2,
                                          IborIndex iborIndex2, List<double> spreads2,
                                         BusinessDayConvention paymentConvention)
        : base(4)
      {

         BusinessDayConvention convention;
         if (Enum.IsDefined(typeof(BusinessDayConvention), paymentConvention))
            convention = paymentConvention;
         else
            convention = schedule1.businessDayConvention();

         // floating leg 1
         currency_[0] = ccy1;
         payer_[0] = (pay1 ? -1 : +1);
         legs_[0] = ((IborLeg)(((IborLeg)(new IborLeg(schedule1, iborIndex1)
                        .withNotionals(nominals1)))
                        .withPaymentDayCounter(iborIndex1.dayCounter())
                        .withPaymentAdjustment(convention)))
                        .withSpreads(spreads1);
         for (int i = 0; i < legs_[0].Count; ++i)//Leg::_iterator i = legs_[0].begin(); i < legs_[0].end(); ++i)
            legs_[0][i].registerWith(update);// registerWith(*i);

         // add initial, interim and final notional flows
         currency_[1] = ccy1;
         payer_[1] = payer_[0];
         legs_[1].Add((CashFlow)(
             new SimpleCashFlow(-nominals1[0], schedule1.calendar().adjust(schedule1.dates().First(), convention))));
         Utils.QL_REQUIRE(nominals1.Count < schedule1.Count, () => "too many float nominals provided");
         for (int i = 1; i < nominals1.Count; i++)
         {
            double flow = nominals1[i - 1] - nominals1[i];
            legs_[1].Add((CashFlow)(
                new SimpleCashFlow(flow, schedule1.calendar().adjust(schedule1[i], convention))));
         }
         if (nominals1.Last() > 0)
            legs_[1].Add((CashFlow)(
                new SimpleCashFlow(nominals1.Last(), schedule1.calendar().adjust(schedule1.dates().Last(), convention))));

         // floating leg 2
         currency_[2] = ccy2;
         payer_[2] = (pay1 ? +1 : -1);
         legs_[2] = ((IborLeg)(((IborLeg)(new IborLeg(schedule2, iborIndex2)
                        .withNotionals(nominals2)))
                        .withPaymentDayCounter(iborIndex2.dayCounter())
                        .withPaymentAdjustment(convention)))
                        .withSpreads(spreads2);
         for (int i = 0; i < legs_[2].Count; ++i)//(Leg::_iterator i = legs_[2].begin(); i < legs_[2].end(); ++i)
            legs_[2][i].registerWith(update);

         // add initial, interim and final notional flows
         currency_[3] = ccy2;
         payer_[3] = payer_[2];
         legs_[3].Add((CashFlow)(
             new SimpleCashFlow(-nominals2[0], schedule2.calendar().adjust(schedule2.dates().First(), convention))));
         Utils.QL_REQUIRE(nominals2.Count < schedule2.Count, () => "too many float nominals provided");
         for (int i = 1; i < nominals2.Count; i++)
         {
            double flow = nominals2[i - 1] - nominals2[i];
            legs_[3].Add((CashFlow)(
                new SimpleCashFlow(flow, schedule2.calendar().adjust(schedule2[i], convention))));
         }
         if (nominals2.Last() > 0)
            legs_[3].Add((CashFlow)(
                new SimpleCashFlow(nominals2.Last(), schedule2.calendar().adjust(schedule2.dates().Last(), convention))));
      }
   }


}
