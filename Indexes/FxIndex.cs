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
   public class FxIndex : Index, IObserver
   {

      protected String familyName_;
      protected int fixingDays_;
      protected Currency sourceCurrency_, targetCurrency_;
      protected Handle<YieldTermStructure> sourceYts_, targetYts_;
      protected String name_;
      protected Handle<Quote> fxQuote_;
      protected bool useQuote_;

      private Calendar fixingCalendar_;


      public FxIndex(String familyName, int fixingDays, Currency source, Currency target,
                  Calendar fixingCalendar, Handle<Quote> fxQuote)
                 :this(familyName, fixingDays, source, target, fixingCalendar,fxQuote
                      , new Handle<YieldTermStructure>(), new Handle<YieldTermStructure>())
            { }

      public FxIndex(String familyName, int fixingDays, Currency source, Currency target,
                     Calendar fixingCalendar )
                     :this(familyName,  fixingDays, source, target,
                      fixingCalendar, new Handle<YieldTermStructure>(), new Handle<YieldTermStructure>())
      { }

      public FxIndex(String familyName, int fixingDays, Currency source, Currency target,
                     Calendar fixingCalendar, Handle<YieldTermStructure> sourceYts,
                     Handle<YieldTermStructure> targetYts) : base()
      {
         familyName_ = familyName;
         fixingDays_ = fixingDays;
         sourceCurrency_ = source;
         targetCurrency_ = target;

         sourceYts_ = sourceYts;
         targetYts_ = targetYts;
         fixingCalendar_ = fixingCalendar;

         //System.IO.StreamWriter tmp = new System.IO.StreamWriter("");
         // StringBuilder tmp = new StringBuilder()
         // tmp.Append(familyName_ +" " +  sourceCurrency_.code + "/" + targetCurrency_.code);
         //name_ = tmp.ToString();

         name_ = familyName_ + " " + sourceCurrency_.code + "/" + targetCurrency_.code;
         QLNet.Settings.evaluationDate();
         QLNet.IndexManager.instance().notifier(name());

         //registerWith(Settings::instance().evaluationDate());
         //registerWith(IndexManager::instance().notifier(name()));

         // we should register with the exchange rate manager
         // to be notified of changes in the spot exchange rate
         // however currently exchange rates are not quotes anyway
         // so this is to be revisited later
      }

      public FxIndex(String familyName, int fixingDays, Currency source, Currency target,
                  Calendar fixingCalendar, Handle<Quote> fxQuote,
                  Handle<YieldTermStructure> sourceYts, Handle<YieldTermStructure> targetYts) : base()
      {
         familyName_ = familyName; fixingDays_ = fixingDays; sourceCurrency_ = source; targetCurrency_ = target;
         sourceYts_ = sourceYts; targetYts_ = targetYts; fxQuote_ = fxQuote; useQuote_ = true;
         fixingCalendar_ = fixingCalendar;

         //std::ostringstream tmp;
         //tmp << familyName_ << " " << sourceCurrency_.code() << "/" << targetCurrency_.code();
         //name_ = tmp.str();

         name_ = familyName_ + " " + sourceCurrency_.code + "/" + targetCurrency_.code;
         QLNet.Settings.evaluationDate();
         QLNet.IndexManager.instance().notifier(name());

         // registerWith(Settings::instance().evaluationDate());
         //registerWith(IndexManager::instance().notifier(name()));
         // we should register with the exchange rate manager
         // to be notified of changes in the spot exchange rate
         // however currently exchange rates are not quotes anyway
         // so this is to be revisited later
      }

      public override double fixing(Date fixingDate, bool forecastTodaysFixing=false)
      {

         Utils.QL_REQUIRE(isValidFixingDate(fixingDate), () => "Fixing date " + fixingDate + " is not valid");

         Date today = Settings.evaluationDate();

         if (fixingDate > today || (fixingDate == today && forecastTodaysFixing))
            return forecastFixing(fixingDate);

         double result = double.NaN;

         if (fixingDate < today || Settings.enforcesTodaysHistoricFixings)
         {
            // must have been fixed
            // do not catch exceptions
            result = pastFixing(fixingDate);
            Utils.QL_REQUIRE(result != double.NaN, () => "Missing " + name() + " fixing for " + fixingDate);
         }
         else
         {
            try
            {
               // might have been fixed
               result = pastFixing(fixingDate);
            }
            catch (Exception error)
            {
               ; // fall through and forecast
            }
            if (result == double.NaN)
               return forecastFixing(fixingDate);
         }

         return result;
      }

      double forecastFixing(Date fixingDate)
      {
         Utils.QL_REQUIRE(!sourceYts_.empty() && !targetYts_.empty(), () => "null term structure set to this instance of " + name());

         // we base the forecast always on the exchange rate (and not on today's
         // fixing)
         double rate;
         if (!useQuote_)
         {
            rate = ExchangeRateManager.Instance.lookup(sourceCurrency_, targetCurrency_).rate;
         }
         else
         {
            rate = fxQuote_.currentLink().value();
         }

         // the exchange rate is interpreted as the spot rate w.r.t. the index's
         // settlement date
         Date refValueDate = valueDate(Settings.evaluationDate());

         // the fixing is obeying the settlement delay as well
         Date fixingValueDate = valueDate(fixingDate);

         // we can assume fixingValueDate >= valueDate
         Utils.QL_REQUIRE(fixingValueDate >= refValueDate, () => "value date for requested fixing as of "
                                                         + fixingDate + " (" + fixingValueDate
                                                         + ") must be greater or equal to today's fixing value date ("
                                                         + refValueDate + ")");

         // compute the forecast applying the usual no arbitrage principle
         double forward = rate * sourceYts_.currentLink().discount(fixingValueDate) * targetYts_.currentLink().discount(refValueDate) /
                        (sourceYts_.currentLink().discount(refValueDate) * targetYts_.currentLink().discount(fixingValueDate));

         return forward;
      }

      public override String name() { return name_; }

      public override Calendar fixingCalendar() { return fixingCalendar_; }

      public override bool isValidFixingDate(Date d) { return fixingCalendar().isBusinessDay(d); }

      public void update() { notifyObservers(); }

      public Date fixingDate(Date valueDate)
      {
         Date fixingDate = fixingCalendar().advance(valueDate, -fixingDays_, TimeUnit.Days);
         return fixingDate;
      }

      public Date valueDate(Date fixingDate)
      {
         Utils.QL_REQUIRE(isValidFixingDate(fixingDate), () => fixingDate + " is not a valid fixing date");
         return fixingCalendar().advance(fixingDate, fixingDays_, TimeUnit.Days);
      }

      public double pastFixing(Date fixingDate)
      {
         Utils.QL_REQUIRE(isValidFixingDate(fixingDate), () => fixingDate + " is not a valid fixing date");
         return timeSeries().value()[fixingDate].Value;
      }

   } // namespace QuantLib

}
