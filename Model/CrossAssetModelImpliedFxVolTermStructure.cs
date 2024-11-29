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

   //public class AnalyticCcLgmFxOptionEngine
   //{
   //}

   public class CrossAssetModelImpliedFxVolTermStructure : BlackVolTermStructure
   {
      CrossAssetModel model_;
      int fxIndex_;
      bool purelyTimeBased_;
      AnalyticCcLgmFxOptionEngine engine_;
      Date referenceDate_;
      double relativeTime_;
      double irDom_;
      double irFor_;
      double fx_;


      public CrossAssetModelImpliedFxVolTermStructure(
      CrossAssetModel model, int foreignCurrencyIndex, BusinessDayConvention bdc,
      DayCounter dc, bool purelyTimeBased)
     : base(bdc, dc == (new DayCounter()) ? model.irlgm1f(0).termStructure().currentLink().dayCounter() : dc)
      {
         model_ = model;
         fxIndex_ = foreignCurrencyIndex;
         purelyTimeBased_ = purelyTimeBased;

         engine_ = new AnalyticCcLgmFxOptionEngine(model_, foreignCurrencyIndex);
         referenceDate_ = purelyTimeBased ? null : model_.irlgm1f(0).termStructure().currentLink().referenceDate();
         model_.registerWith(update);
         //engine_.cache(false);
         double fxSpot = model_.fxbs(fxIndex_).fxSpotToday().currentLink().value();
         Utils.QL_REQUIRE(fxSpot > 0, () => "FX Spot for index " + fxIndex_ + " must be positive");
         state(0.0, 0.0, System.Math.Log(fxSpot));
         update();
      }

      protected override double blackVarianceImpl(double t, double strike)
      {

         double tmpStrike = strike;

         double fxSpot = System.Math.Exp(fx_);
         double domDisc = model_.discountBond(0, relativeTime_, relativeTime_ + t, irDom_);
         double forDisc = model_.discountBond(fxIndex_ + 1, relativeTime_, relativeTime_ + t, irFor_);
         double atm = fxSpot * forDisc / domDisc;

         if (tmpStrike == double.NaN)
         {
            tmpStrike = atm;
         }

         Option.Type type = (tmpStrike >= atm) ? Option.Type.Call : Option.Type.Put;

         StrikedTypePayoff payoff = new PlainVanillaPayoff(type, tmpStrike);

         double premium = engine_.value(relativeTime_, relativeTime_ + t, payoff, domDisc, atm);

         double impliedStdDev = 0.0;
         try
         {
            impliedStdDev = Utils.blackFormulaImpliedStdDev(type, tmpStrike, atm, premium, domDisc);
         }
         catch (Exception ex)
         {
         }

         return impliedStdDev * impliedStdDev;
      }

      protected override double blackVolImpl(double t, double strike)
      {
         double tmp = System.Math.Max(1.0E-6, t);
         return System.Math.Sqrt(blackVarianceImpl(tmp, strike) / tmp);
      }

      public override Date referenceDate()
      {
         Utils.QL_REQUIRE(!purelyTimeBased_, () => "reference date not available for purely " + "time based term structure");
         return referenceDate_;
      }

      public void referenceDate(Date d)
      {
         Utils.QL_REQUIRE(!purelyTimeBased_, () => "reference date not available for purely " + "time based term structure");
         referenceDate_ = d;
         update();
      }

      public void referenceTime(double t)
      {
         Utils.QL_REQUIRE(purelyTimeBased_, () => "reference time can only be set for purely " + "time based term structure");
         relativeTime_ = t;
      }

      public void state(double domesticIr, double foreignIr, double logFx)
      {
         irDom_ = domesticIr;
         irFor_ = foreignIr;
         fx_ = logFx;
      }

      public void move(Date d, double domesticIr, double foreignIr,
                                                           double logFx)
      {
         state(domesticIr, foreignIr, logFx);
         referenceDate(d);
      }

      public void move(double t, double domesticIr, double foreignIr,
                                                           double logFx)
      {
         state(domesticIr, foreignIr, logFx);
         referenceTime(t);
      }

      public override void update()
      {
         if (!purelyTimeBased_)
         {
            relativeTime_ = dayCounter().yearFraction(model_.irlgm1f(0).termStructure().currentLink().referenceDate(), referenceDate_);
         }
         notifyObservers();
      }

      public override Date maxDate() { return Date.maxDate(); }

      public override double maxTime() { return double.MaxValue; }//QL_MAX_REAL; }

      public override double minStrike() { return 0.0; }

      public override double maxStrike() { return double.MaxValue; } //QL_MAX_REAL; }


   }
}
