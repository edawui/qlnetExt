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

/*! \file blackinvertedvoltermstructure.hpp
    \brief Black volatility surface that inverts an existing surface.
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
   //! Black volatility surface that inverts an existing surface.
   /*! This class is used when one wants a USD/EUR volatility, at a given USD/EUR strike
       when only a EUR/USD volatility surface is present.

               \ingroup termstructures
   */
   public class BlackInvertedVolTermStructure : BlackVolTermStructure
   {

      private Handle<BlackVolTermStructure> vol_;




      //! Constructor takes a BlackVolTermStructure and takes everything from that
      /*! This will work with both a floating and fixed reference date underlying surface,
          since we are reimplementing the reference date and update methods */
      public BlackInvertedVolTermStructure(Handle<BlackVolTermStructure> vol)
        : base(vol.currentLink().businessDayConvention(), vol.currentLink().dayCounter())
      {
         vol_ = vol;
         vol_.registerWith(update);
         //registerWith(vol_);
      }

      //! return the underlying vol surface
      public Handle<BlackVolTermStructure> underlyingVol() { return vol_; }

      //! \name TermStructure interface
      //@{
      public override Date referenceDate() { return vol_.currentLink().referenceDate(); }

      public override Date maxDate() { return vol_.currentLink().maxDate(); }
      public override int settlementDays() { return vol_.currentLink().settlementDays(); }
      public override Calendar calendar() { return vol_.currentLink().calendar(); }
      //! \name Observer interface
      //@{
      public override void update() { notifyObservers(); }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike()
      {
         double min = vol_.currentLink().minStrike();
         if (min == double.MinValue || min == 0)
            return 0; // we allow ATM calls
         else
            return 1 / vol_.currentLink().maxStrike();
      }
      public override double maxStrike()
      {
         double min = vol_.currentLink().minStrike();
         if (min == double.MinValue || min == 0)
            return double.MaxValue;
         else
            return 1 / min;
      }
      //@}
      //! \name Visitability
      //@{
      public virtual void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            Utils.QL_FAIL("not an event visitor");
      }

      //@}
      protected override double blackVarianceImpl(double t, double strike)
      {
         return vol_.currentLink().blackVariance(t, strike == 0 ? 0 : 1 / strike);
      }

      protected override double blackVolImpl(double t, double strike)
      {
         return vol_.currentLink().blackVol(t, strike == 0 ? 0 : 1 / strike);
      }

   }
}
