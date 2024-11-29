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

/*! \file qle/termstructures/datedstrippedoptionletbase.hpp
    \brief abstract class for optionlet surface with fixed reference date
    \ingroup termstructures
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   //class DatedStrippedOptionletBase
   //{
   //}
   //! Stripped Optionlet base class interface
   /*! Abstract base class interface for a (time indexed) List of (strike indexed) optionlet
       (i.e. caplet/floorlet) volatilities with a fixed reference date.

               \ingroup termstructues
   */
   public abstract class DatedStrippedOptionletBase : LazyObject
   {

      public abstract List<double> optionletStrikes(int i);
      public abstract List<double> optionletVolatilities(int i);

      public abstract List<Date> optionletFixingDates();
      public abstract List<double> optionletFixingTimes();
      public abstract int optionletMaturities();

      public abstract List<double> atmOptionletRates();

      public abstract Date referenceDate();
      public abstract DayCounter dayCounter();
      public abstract Calendar calendar();
      public abstract BusinessDayConvention businessDayConvention();
      public abstract VolatilityType volatilityType();
      public abstract double displacement();

      //public abstract VolatilityType volatilityType();
      //public abstract double displacement();


   }
}
