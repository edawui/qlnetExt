

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


   /*! \file floatingratefxlinkednotionalcoupon.hpp
       \brief Coupon paying a Libor-type index but with an FX linked notional

       \ingroup cashflows
   */
   //! %Coupon paying a Libor-type index on an fx-linked nominal
   //! \ingroup cashflows
   public class FloatingRateFXLinkedNotionalCoupon : FloatingRateCoupon
   {


      private FXLinkedCashFlow notional_;



      //! FloatingRateFXLinkedNotionalCoupon
      /*! Note that if you ask this coupon for it's nominal, you will get 0 back as the nominal is
       *  variable (and Coupon::nominal() is not virtual). To get the actual nominal call fxLinkedCashFlow().amount()
       */
      public FloatingRateFXLinkedNotionalCoupon(double foreignAmount, Date fxFixingDate, FxIndex fxIndex,
                                       bool invertFxIndex, Date paymentDate, Date startDate,
                                        Date endDate, int fixingDays,
                                        InterestRateIndex index, double gearing// = 1.0,
                                       , double spread// = 0.0
                                       , Date refPeriodStart// = new Date()
                                       , Date refPeriodEnd //= new Date()
                                       , DayCounter dayCounter //= new DayCounter(),
                                      , bool isInArrears = false)
        : base(paymentDate, 0.0, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart,
                             refPeriodEnd, dayCounter, isInArrears)
      {
         notional_ = new FXLinkedCashFlow(paymentDate, fxFixingDate, foreignAmount, fxIndex, invertFxIndex);

      }

      //! \name CashFlow interface
      //@{
      /*! We override FloatingRateCoupon::amount() here as we need to use the variable notional from
          the fxLinkedCashflow.
       */
      public override double amount() { return rate() * accrualPeriod() * notional_.amount(); }
      //@}

      //! Return the underlying FX linked notional
      public FXLinkedCashFlow fxLinkedCashFlow() { return notional_; }


      public override void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            base.accept(v);
      }





   }
}
