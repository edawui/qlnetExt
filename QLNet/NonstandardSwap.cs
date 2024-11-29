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

namespace QLNet
{


   using Leg = List<QLNet.CashFlow>;


   public class NonstandardSwap : Swap
   {




      //! %Arguments for nonstandard swap calculation
      public new class Arguments : Swap.Arguments
      {
         public Arguments() : base()
         {
            type = VanillaSwap.Type.Receiver;
         }

         public VanillaSwap.Type type;
         public List<double> fixedNominal, floatingNominal;

         public List<Date> fixedResetDates;
         public List<Date> fixedPayDates;
         public List<double> floatingAccrualTimes;
         public List<Date> floatingResetDates;
         public List<Date> floatingFixingDates;
         public List<Date> floatingPayDates;

         public List<double> fixedCoupons;
         public List<double> fixedRate;
         public List<double> floatingSpreads;
         public List<double> floatingGearings;
         public List<double> floatingCoupons;

         public IborIndex iborIndex;

         public List<bool> fixedIsRedemptionFlow;
         public List<bool> floatingIsRedemptionFlow;

         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(fixedNominal.Count == fixedPayDates.Count, () =>
                        "number of fixed leg nominals plus redemption flows " +
                       "different from number of payment dates");

            Utils.QL_REQUIRE(fixedRate.Count == fixedPayDates.Count, () =>
                        "number of fixed rates plus redemption flows different from " +
                       "number of payment dates");

            Utils.QL_REQUIRE(floatingNominal.Count == floatingPayDates.Count, () =>
                        "number of float leg nominals different from number of " +
                       "payment dates");

            Utils.QL_REQUIRE(fixedResetDates.Count == fixedPayDates.Count, () =>
                        "number of fixed start dates different from " +
                       "number of fixed payment dates");

            Utils.QL_REQUIRE(fixedPayDates.Count == fixedCoupons.Count, () =>
                        "number of fixed payment dates different from " +
                       "number of fixed coupon amounts");

            Utils.QL_REQUIRE(floatingResetDates.Count == floatingPayDates.Count, () =>
                        "number of floating start dates different from " +
                       "number of floating payment dates");

            Utils.QL_REQUIRE(floatingFixingDates.Count == floatingPayDates.Count, () =>
                        "number of floating fixing dates different from " +
                       "number of floating payment dates");

            Utils.QL_REQUIRE(floatingAccrualTimes.Count == floatingPayDates.Count, () =>
                        "number of floating accrual Times different from " +
                       "number of floating payment dates");
            Utils.QL_REQUIRE(floatingSpreads.Count == floatingPayDates.Count, () =>
                        "number of floating spreads different from " +
                       "number of floating payment dates");

            Utils.QL_REQUIRE(floatingPayDates.Count == floatingCoupons.Count, () =>
                       "number of floating payment dates different from " +
                       "number of floating coupon amounts");
         }


      }

      public new class Results : Swap.Results
      {

         //public override void reset()
         //{ base.reset(); }
      }



      public class Engine : GenericEngine<NonstandardSwap.Arguments, NonstandardSwap.Results>
      {
      }

      //void init();
      //void setupExpired() const;
      VanillaSwap.Type type_;
      List<double> fixedNominal_, floatingNominal_;
      Schedule fixedSchedule_;
      List<double> fixedRate_;
      DayCounter fixedDayCount_;
      Schedule floatingSchedule_;
      IborIndex iborIndex_;
      List<double> spread_;
      List<double> gearing_;
      bool singleSpreadAndGearing_;
      DayCounter floatingDayCount_;
      BusinessDayConvention paymentConvention_;
      bool intermediateCapitalExchange_;
      bool finalCapitalExchange_;
      // results



      public VanillaSwap.Type type() { return type_; }

      public List<double> fixedNominal()
      {
         return fixedNominal_;
      }
      public List<double> floatingNominal()
      {
         return floatingNominal_;
      }

      public Schedule fixedSchedule()
      {
         return fixedSchedule_;
      }

      public List<double> fixedRate()
      {
         return fixedRate_;
      }

      public DayCounter fixedDayCount()
      {
         return fixedDayCount_;
      }

      public Schedule floatingSchedule()
      {
         return floatingSchedule_;
      }

      public IborIndex iborIndex()
      {
         return iborIndex_;
      }

      public double spread()
      {
         Utils.QL_REQUIRE(singleSpreadAndGearing_, () => "spread is a vector, use spreads inspector instead");
         return spread_[0];
      }

      public double gearing()
      {
         Utils.QL_REQUIRE(singleSpreadAndGearing_, () => "gearing is a vector, use gearings inspector instead");
         return gearing_[0];
      }

      public List<double> spreads()
      {
         return spread_;
      }

      public List<double> gearings()
      {
         return gearing_;
      }

      public DayCounter floatingDayCount()
      {
         return floatingDayCount_;
      }

      public BusinessDayConvention paymentConvention()
      {
         return paymentConvention_;
      }

      public Leg fixedLeg() { return legs_[0]; }

      public Leg floatingLeg() { return legs_[1]; }


      public NonstandardSwap(VanillaSwap fromVanilla)
        : base(2)
      {
         type_ = (VanillaSwap.Type)fromVanilla.swapType;
         fixedNominal_ = Enumerable.Repeat<double>(fromVanilla.nominal, fromVanilla.fixedLeg().Count).ToList();
         floatingNominal_ = Enumerable.Repeat<double>(fromVanilla.nominal, fromVanilla.floatingLeg().Count).ToList();
         fixedSchedule_ = fromVanilla.fixedSchedule();
         fixedRate_ = Enumerable.Repeat<double>(fromVanilla.fixedRate, fromVanilla.fixedLeg().Count).ToList();


         fixedDayCount_ = fromVanilla.fixedDayCount();
         floatingSchedule_ = fromVanilla.floatingSchedule();
         iborIndex_ = fromVanilla.iborIndex();
         spread_ = Enumerable.Repeat<double>(fromVanilla.spread, fromVanilla.floatingLeg().Count).ToList();


         gearing_ = Enumerable.Repeat<double>(1.0, fromVanilla.floatingLeg().Count).ToList();
         singleSpreadAndGearing_ = true;
         floatingDayCount_ = fromVanilla.floatingDayCount();
         paymentConvention_ = fromVanilla.paymentConvention();
         intermediateCapitalExchange_ = false;
         finalCapitalExchange_ = false;

         init();
      }

      public NonstandardSwap(
         VanillaSwap.Type type, List<double> fixedNominal,
         List<double> floatingNominal, Schedule fixedSchedule,
         List<double> fixedRate, DayCounter fixedDayCount,
         Schedule floatingSchedule,
         IborIndex iborIndex, double gearing,
         double spread, DayCounter floatingDayCount,
         bool intermediateCapitalExchange, bool finalCapitalExchange,
        BusinessDayConvention paymentConvention)
        : base(2)
      {
         type_ = type;
         fixedNominal_ = fixedNominal;
         floatingNominal_ = floatingNominal;
         fixedSchedule_ = fixedSchedule;
         fixedRate_ = fixedRate;
         fixedDayCount_ = fixedDayCount;
         floatingSchedule_ = floatingSchedule;
         iborIndex_ = iborIndex;
         spread_ = Enumerable.Repeat<double>(spread, floatingNominal.Count).ToList();
         gearing_ = Enumerable.Repeat<double>(gearing, floatingNominal.Count).ToList();
         singleSpreadAndGearing_ = true;
         floatingDayCount_ = floatingDayCount;
         intermediateCapitalExchange_ = intermediateCapitalExchange;
         finalCapitalExchange_ = finalCapitalExchange;


         if (Enum.IsDefined(typeof(BusinessDayConvention),paymentConvention))
            paymentConvention_ = paymentConvention;
         else
            paymentConvention_ = floatingSchedule_.businessDayConvention();
         init();
      }

      NonstandardSwap(
         VanillaSwap.Type type, List<double> fixedNominal,
         List<double> floatingNominal, Schedule fixedSchedule,
         List<double> fixedRate, DayCounter fixedDayCount,
         Schedule floatingSchedule,
         IborIndex iborIndex,
         List<double> gearing, List<double> spread,
         DayCounter floatingDayCount,
         bool intermediateCapitalExchange, bool finalCapitalExchange,
        BusinessDayConvention paymentConvention)
        : base(2)
      {
         type_ = type;
         fixedNominal_ = fixedNominal;
         floatingNominal_ = floatingNominal; fixedSchedule_ = fixedSchedule;
         fixedRate_ = fixedRate; fixedDayCount_ = fixedDayCount;
         floatingSchedule_ = floatingSchedule; iborIndex_ = iborIndex;
         spread_ = spread; gearing_ = gearing; singleSpreadAndGearing_ = false;
         floatingDayCount_ = floatingDayCount;
         intermediateCapitalExchange_ = intermediateCapitalExchange;
         finalCapitalExchange_ = finalCapitalExchange;


         if (Enum.IsDefined(typeof(BusinessDayConvention), paymentConvention))////paymentConvention == null)
            paymentConvention_ = paymentConvention;
         else
            paymentConvention_ = floatingSchedule_.businessDayConvention();
         init();
      }

      void init()
      {

         Utils.QL_REQUIRE(fixedNominal_.Count == fixedRate_.Count, () =>
                    "Fixed nominal size ("
                        + fixedNominal_.Count
                        + ") does not match fixed rate size ("
                        + fixedRate_.Count + ")");

         Utils.QL_REQUIRE(fixedNominal_.Count == fixedSchedule_.Count - 1, () =>
                    "Fixed nominal size (" + fixedNominal_.Count
                                           + ") does not match schedule size ("
                                           + fixedSchedule_.Count + ") - 1");

         Utils.QL_REQUIRE(floatingNominal_.Count == floatingSchedule_.Count - 1, () =>
                    "Floating nominal size ("
                        + floatingNominal_.Count
                        + ") does not match schedule size ("
                        + floatingSchedule_.Count + ") - 1");

         Utils.QL_REQUIRE(floatingNominal_.Count == spread_.Count, () =>
                    "Floating nominal size (" + floatingNominal_.Count
                                              + ") does not match spread size ("
                                              + spread_.Count + ")");

         Utils.QL_REQUIRE(floatingNominal_.Count == gearing_.Count, () =>
                    "Floating nominal size ("
                        + floatingNominal_.Count
                        + ") does not match gearing size (" + gearing_.Count
                        + ")");

         // if the gearing is zero then the ibor leg will be set up with fixed
         // coupons which makes trouble here in this context. We therefore use
         // a dirty trick and enforce the gearing to be non zero.
         for (int i = 0; i < gearing_.Count; ++i)
         {
            if (Utils.close(gearing_[i], 0.0))
               gearing_[i] = QLNet.Const.QL_EPSILON;
         }


         // FixedRateLeg temp = new FixedRateLeg(fixedSchedule_);
         //temp = (FixedRateLeg)(temp.withNotionals(fixedNominal_));
         //legs_[0] = temp.withPaymentAdjustment(paymentConvention_);


         legs_[0] = (FixedRateLeg)(new FixedRateLeg(fixedSchedule_))
                     .withNotionals(fixedNominal_)
                     .withPaymentAdjustment(paymentConvention_);

         legs_[1] = ((IborLeg)((IborLeg)(new IborLeg(floatingSchedule_, iborIndex_)
                       .withNotionals(floatingNominal_)))
                       .withPaymentDayCounter(floatingDayCount_)
                       .withPaymentAdjustment(paymentConvention_))
                       .withSpreads(spread_)
                       .withGearings(gearing_);

         if (intermediateCapitalExchange_)
         {
            for (int i = 0; i < legs_[0].Count - 1; i++)
            {
               double cap = fixedNominal_[i] - fixedNominal_[i + 1];
               if (!QLNet.Utils.close(cap, 0.0))
               {
                  //List<CashFlow>.Enumerator it1 = legs_[0].GetEnumerator();//..begin();
                  //std::advance(it1, i + 1);

                  int it1 = 0;
                  it1 += i + 1;
                  legs_[0].Insert(it1, (CashFlow)(new Redemption(cap, legs_[0][i].date())));

                  //List<double>::iterator it2 = fixedNominal_.begin();
                  //std::advance(it2, i + 1);
                  int it2 = 0;
                  it2 += i + 1;
                  fixedNominal_.Insert(it2, fixedNominal_[i]);

                  //List<double>::iterator it3 = fixedRate_.begin();
                  //std::advance(it3, i + 1);
                  int it3 = 0;
                  it3 += i + 1;
                  fixedRate_.Insert(it3, 0.0);

                  i++;
               }
            }
            for (int i = 0; i < legs_[1].Count - 1; i++)
            {
               double cap = floatingNominal_[i] - floatingNominal_[i + 1];
               if (!Utils.close(cap, 0.0))
               {
                  //List<boost::shared_ptr<CashFlow>>::iterator it1 =
                  //legs_[1].begin();
                  //std::advance(it1, i + 1);
                  int it1 = 0;
                  it1 += i + 1;

                  legs_[1].Insert(it1, (CashFlow)(new Redemption(cap, legs_[1][i].date())));

                  //List<double>::iterator it2 = floatingNominal_.begin();
                  //std::advance(it2, i + 1);
                  int it2 = 0;
                  it2 += i + 1;

                  floatingNominal_.Insert(it2, floatingNominal_[i]);
                  i++;
               }
            }
         }

         if (finalCapitalExchange_)
         {
            legs_[0].Add((CashFlow)(new Redemption(fixedNominal_.Last(), legs_[0].Last().date())));
            fixedNominal_.Add(fixedNominal_.Last());
            fixedRate_.Add(0.0);
            legs_[1].Add((CashFlow)(new Redemption(floatingNominal_.Last(), legs_[1].Last().date())));
            floatingNominal_.Add(floatingNominal_.Last());
         }

         for (int i = 0; i < legs_[1].Count; ++i)//Leg::_iterator i = legs_[1].begin(); i < legs_[1].end(); ++i)
            legs_[1][i].registerWith(update);

         switch (type_)
         {
            case VanillaSwap.Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;
            case VanillaSwap.Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;
               //default:
               // Utils.QL_FAIL("Unknown nonstandard-swap type");
         }
      }

      //public Leg fixedLeg() { return legs_[0]; }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);
         NonstandardSwap.Arguments arguments = args as NonstandardSwap.Arguments;


         if (arguments==null)//(arguments == null))
            return; // swap engine ...


         arguments.type = type_;
         arguments.fixedNominal = fixedNominal_;
         arguments.floatingNominal = floatingNominal_;
         arguments.fixedRate = fixedRate_;

         Leg fixedCoupons = fixedLeg();

         arguments.fixedResetDates = arguments.fixedPayDates = new List<Date>(fixedCoupons.Count);
         arguments.fixedCoupons = new List<double>(fixedCoupons.Count);
         arguments.fixedIsRedemptionFlow = Enumerable.Repeat<bool>(false, fixedCoupons.Count).ToList();

         for (int i = 0; i < fixedCoupons.Count; ++i)
         {
            FixedRateCoupon coupon = (FixedRateCoupon)(fixedCoupons[i]);
            if (!(coupon == null))
            {
               arguments.fixedPayDates[i] = coupon.date();
               arguments.fixedResetDates[i] = coupon.accrualStartDate();
               arguments.fixedCoupons[i] = coupon.amount();
            }
            else
            {
               CashFlow cashflow = (CashFlow)fixedCoupons[i];
               int j = arguments.fixedPayDates.IndexOf(cashflow.date());

               //List <Date>::_iterator j =
               //    std::find(arguments.fixedPayDates.begin(),
               //              arguments.fixedPayDates.end(), cashflow.date());

               Utils.QL_REQUIRE(j != arguments.fixedPayDates.Count, () => "nominal redemption on " + cashflow.date() + "has no corresponding coupon");
               int jIdx = j; //- arguments.fixedPayDates.begin();
               arguments.fixedIsRedemptionFlow[i] = true;
               arguments.fixedCoupons[i] = cashflow.amount();
               arguments.fixedResetDates[i] =
                                arguments.fixedResetDates[jIdx];
               arguments.fixedPayDates[i] = cashflow.date();
            }
         }

         Leg floatingCoupons = floatingLeg();

         arguments.floatingResetDates = arguments.floatingPayDates =
                     arguments.floatingFixingDates = new List<Date>(floatingCoupons.Count);
         arguments.floatingAccrualTimes = new List<double>(floatingCoupons.Count);
         arguments.floatingSpreads = new List<double>(floatingCoupons.Count);

         arguments.floatingGearings = new List<double>(floatingCoupons.Count);
         arguments.floatingCoupons = new List<double>(floatingCoupons.Count);
         arguments.floatingIsRedemptionFlow = Enumerable.Repeat<bool>(false, floatingCoupons.Count).ToList();

         for (int i = 0; i < floatingCoupons.Count; ++i)
         {
            IborCoupon coupon = (IborCoupon)floatingCoupons[i];
            if (!(coupon == null))
            {
               arguments.floatingResetDates[i] = coupon.accrualStartDate();
               arguments.floatingPayDates[i] = coupon.date();
               arguments.floatingFixingDates[i] = coupon.fixingDate();
               arguments.floatingAccrualTimes[i] = coupon.accrualPeriod();
               arguments.floatingSpreads[i] = coupon.spread();
               arguments.floatingGearings[i] = coupon.gearing();
               try
               {
                  arguments.floatingCoupons[i] = coupon.amount();
               }
               catch (Exception Error)
               {
                  arguments.floatingCoupons[i] = double.NaN;//Null<double>();
               }
            }
            else
            {
               CashFlow cashflow = (CashFlow)floatingCoupons[i];

               int j = arguments.floatingPayDates.IndexOf(cashflow.date());

               //List<Date>::_iterator j = std::find(
               //    arguments.floatingPayDates.begin(),
               //    arguments.floatingPayDates.end(), cashflow.date());
               Utils.QL_REQUIRE(j != arguments.floatingPayDates.Count, () => "nominal redemption on " + cashflow.date() + "has no corresponding coupon");
               int jIdx = j; //- arguments.floatingPayDates.begin();
               arguments.floatingIsRedemptionFlow[i] = true;
               arguments.floatingCoupons[i] = cashflow.amount();
               arguments.floatingResetDates[i] =
                                   arguments.floatingResetDates[jIdx];
               arguments.floatingFixingDates[i] =
                   arguments.floatingFixingDates[jIdx];
               arguments.floatingAccrualTimes[i] = 0.0;
               arguments.floatingSpreads[i] = 0.0;
               arguments.floatingGearings[i] = 1.0;
               arguments.floatingPayDates[i] = cashflow.date();
            }
         }

         arguments.iborIndex = new IborIndex();
      }

      protected override void setupExpired() { base.setupExpired(); }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);
      }

   }
}
