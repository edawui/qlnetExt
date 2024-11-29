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
   /*! \file qle/termstructures/datedstrippedoptionlet.hpp
       \brief Stripped optionlet surface with fixed reference date
       \ingroup termstructures
   */

   public class DatedStrippedOptionlet : DatedStrippedOptionletBase
   {

      //void checkInputs() ;

      Date referenceDate_;
      Calendar calendar_;
      BusinessDayConvention businessDayConvention_;
      List<Date> optionletDates_;
      int nOptionletDates_;
      List<double> optionletTimes_;
      List<List<double>> optionletStrikes_;
      List<List<double>> optionletVolatilities_;
      List<double> optionletAtmRates_;
      DayCounter dayCounter_;
      VolatilityType type_;
      double displacement_;

      public DatedStrippedOptionlet(Date referenceDate, StrippedOptionletBase s)
      {
         referenceDate_ = referenceDate; calendar_ = s.calendar(); businessDayConvention_ = s.businessDayConvention();
         optionletDates_ = s.optionletFixingDates(); nOptionletDates_ = s.optionletMaturities();
         optionletTimes_ = s.optionletFixingTimes(); optionletStrikes_ = new List<List<double>>(nOptionletDates_);// List<double>();
         optionletVolatilities_ = new List<List<double>>(nOptionletDates_); optionletAtmRates_ = s.atmOptionletRates();
         dayCounter_ = s.dayCounter(); type_ = s.volatilityType(); displacement_ = s.displacement();


         // Populate the optionlet strikes and volatilities
         for (int i = 0; i < nOptionletDates_; ++i)
         {
            optionletStrikes_[i] = s.optionletStrikes(i);
            optionletVolatilities_[i] = s.optionletVolatilities(i);
         }
      }

      public DatedStrippedOptionlet(Date referenceDate, Calendar calendar,
                                                 BusinessDayConvention bdc, List<Date> optionletDates,
                                                  List<List<double>> strikes,
                                                  List<List<double>> volatilities,
                                                  List<double> optionletAtmRates, DayCounter dayCounter,
                                                 VolatilityType type, double displacement)
      {
         referenceDate_ = referenceDate; calendar_ = calendar; businessDayConvention_ = bdc; optionletDates_ = optionletDates;
         nOptionletDates_ = optionletDates.Count; optionletTimes_ = new List<double>(nOptionletDates_); optionletStrikes_ = strikes;
         optionletVolatilities_ = volatilities; optionletAtmRates_ = optionletAtmRates; dayCounter_ = dayCounter; type_ = type;
         displacement_ = displacement;



         checkInputs();
         // Populate the optionlet times
         for (int i = 0; i < nOptionletDates_; ++i)
         { optionletTimes_[i] = dayCounter_.yearFraction(referenceDate_, optionletDates_[i]); }
      }

      private void checkInputs()
      {

         Utils.QL_REQUIRE(!optionletDates_.empty(), () => "Need at least one optionlet to create optionlet surface");
         Utils.QL_REQUIRE(nOptionletDates_ == optionletVolatilities_.Count, () =>
                     "Mismatch between number of option tenors (" + nOptionletDates_ + ") and number of volatility rows ("
                                                                 + optionletVolatilities_.Count + ")");
         Utils.QL_REQUIRE(nOptionletDates_ == optionletStrikes_.Count, () => "Mismatch between number of option tenors ("
                                                                      + nOptionletDates_ + ") and number of strike rows ("
                                                                      + optionletStrikes_.Count + ")");
         Utils.QL_REQUIRE(nOptionletDates_ == optionletAtmRates_.Count, () => "Mismatch between number of option tenors ("
                                                                       + nOptionletDates_ + ") and number of ATM rates ("
                                                                       + optionletAtmRates_.Count + ")");
         Utils.QL_REQUIRE(optionletDates_[0] > referenceDate_, () =>
                     "First option date (" + optionletDates_[0] + ") must be greater than the reference date");
         Utils.QL_REQUIRE(UtilsExt.IsIncreasingMontonically<Date>(optionletDates_), () =>
                     "Optionlet dates must be sorted in ascending order");



         for (int i = 0; i < nOptionletDates_; ++i)
         {
            Utils.QL_REQUIRE(!optionletStrikes_[i].empty(), () => "The " + i + " row of strikes is empty");
            Utils.QL_REQUIRE(optionletStrikes_[i].Count == optionletVolatilities_[i].Count, () =>
                        "int of " + i + " row of strikes and volatilities are not equal");
            Utils.QL_REQUIRE(UtilsExt.IsIncreasingMontonically<double>(optionletStrikes_[i]), () =>
                        "The " + i + " row of strikes is not sorted in ascending order");
         }
      }

      public override List<double> optionletStrikes(int i)
      {
         Utils.QL_REQUIRE(i < optionletStrikes_.Count, () => "index (" + i + ") must be less than optionletStrikes size ("
                                                               + optionletStrikes_.Count + ")");
         return optionletStrikes_[i];
      }

      public override List<double> optionletVolatilities(int i)
      {
         Utils.QL_REQUIRE(i < optionletVolatilities_.Count, () => "index (" + i + ") must be less than optionletVolatilities size ("
                                                                   + optionletVolatilities_.Count + ")");
         return optionletVolatilities_[i];
      }

      public override List<Date> optionletFixingDates() { return optionletDates_; }

      public override List<double> optionletFixingTimes() { return optionletTimes_; }

      public override int optionletMaturities() { return nOptionletDates_; }

      public override List<double> atmOptionletRates() { return optionletAtmRates_; }

      public override DayCounter dayCounter() { return dayCounter_; }

      public override Calendar calendar() { return calendar_; }

      public override Date referenceDate() { return referenceDate_; }

      public override BusinessDayConvention businessDayConvention() { return businessDayConvention_; }

      public override VolatilityType volatilityType() { return type_; }

      public override double displacement() { return displacement_; }
   }

}
