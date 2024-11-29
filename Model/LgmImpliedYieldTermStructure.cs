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

/*! \file lgmimpliedyieldtermstructure.hpp
    \brief yield term structure implied by a LGM model
    \ingroup models
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;


namespace QLNetExt
{

   //! Lgm Implied Yield Term Structure
   /*! The termstructure has the reference date of the model's
       termstructure at construction, but you can vary this
       as well as the state.
       The purely time based variant is mainly there for
       perfomance reasons, note that it does not provide the
       full term structure interface and does not send
       notifications on reference time updates.

           \ingroup models
    */

   public class LgmImpliedYieldTermStructure : YieldTermStructure
   {
      protected LinearGaussMarkovModel model_;
      protected bool purelyTimeBased_;
      protected Date referenceDate_;
      protected double relativeTime_;
      protected double state_;


      public LgmImpliedYieldTermStructure( LinearGaussMarkovModel model,
                                  DayCounter  dc 
                                   ,bool purelyTimeBased = false)
         :base(dc)//model.parametrization().termStructure().link.dayCounter())
      {
         //  YieldTermStructure(dc == DayCounter()? model->parametrization()->termStructure()->dayCounter() : dc),
         model_ = model;
         purelyTimeBased_ = purelyTimeBased;
         //referenceDate_=purelyTimeBased? Null<Date>() : model_->parametrization()->termStructure()->referenceDate()),
         state_ = 0.0;

         model_.registerWith(update);
         update();
      }

         public LgmImpliedYieldTermStructure(LinearGaussMarkovModel model)
         :this(model, model.parametrization().termStructure().link.dayCounter())
      {
       }

            public override Date maxDate()
      {
         // we don't care - let the underlying classes throw
         // exceptions if applicable
         return Date.maxDate();
      }

      public override double maxTime() {
         // see maxDate
         return System.Double.MaxValue;//QL_MAX_REAL;
      }

      public override Date referenceDate() {
         Utils.QL_REQUIRE(!purelyTimeBased_, () => "reference date not available for purely " +
                                                   "time based term structure");
         return referenceDate_;
      }

      public void referenceDate(Date d)
      {
         Utils.QL_REQUIRE(!purelyTimeBased_, () => "reference date not available for purely " +
                                                  "time based term structure");
         referenceDate_ = d;
         //todo  update();
      }

      public void referenceTime(double t)
      {
         Utils.QL_REQUIRE(purelyTimeBased_, () => "reference time can only be set for purely " +
                                             "time based term structure");
         relativeTime_ = t;
      }

      public void state(double s) { state_ = s; }

      public void move(Date d, double s)
      {
         state(s);
         referenceDate(d);
      }

      public void move(double t, double s)
      {
         state(s);
         referenceTime(t);
      }

      public override void update()
      {
         if (!purelyTimeBased_)
         {
            relativeTime_ =
               model_.parametrization().termStructure().link.dayCounter().yearFraction(
                        model_.parametrization().termStructure().link.referenceDate(),
                         referenceDate_);
         }
         notifyObservers();
      }

      protected override double discountImpl(double t) {
         Utils.QL_REQUIRE(t >= 0.0, () => "negative time (" + t.ToString() + ") given");
         return model_.discountBond(relativeTime_, relativeTime_ + t, state_);
      }


   }


   //! Lgm Implied Yts Fwd Corrected
   /*! the target curve should have a reference date consistent with
     the model's term structure

     \ingroup models
   */
   public class LgmImpliedYtsFwdFwdCorrected : LgmImpliedYieldTermStructure
   {
      private Handle<YieldTermStructure> targetCurve_;

      public LgmImpliedYtsFwdFwdCorrected(LinearGaussMarkovModel model,
                                  Handle<YieldTermStructure> targetCurve
                                  , DayCounter dc
                                  , bool purelyTimeBased = false)
         :base(model, dc, purelyTimeBased)
      { }

      public LgmImpliedYtsFwdFwdCorrected(LinearGaussMarkovModel model,
                                  Handle<YieldTermStructure> targetCurve):
         this(model,targetCurve, model.parametrization().termStructure().link.dayCounter())
      { }


      protected override double discountImpl(double t)
      {
         Utils.QL_REQUIRE(t >= 0.0, () => "negative time (" + t.ToString() + ") given");
         return base.discountImpl(t) * targetCurve_.link.discount(relativeTime_ + t) /
                targetCurve_.link.discount(relativeTime_) * model_.parametrization().termStructure().link.discount(relativeTime_) /
                model_.parametrization().termStructure().link.discount(relativeTime_ + t);
      }
   }



   //! Lgm Implied Yts Spot Corrected
   /*! the target curve should have a reference date consistent with
     the model's term structure

     \ingroup models
   */
   public class LgmImpliedYtsSpotCorrected : LgmImpliedYieldTermStructure {

      private Handle<YieldTermStructure> targetCurve_;

      public LgmImpliedYtsSpotCorrected(LinearGaussMarkovModel model,
                                      Handle<YieldTermStructure> targetCurve,
                                      DayCounter dc,
                                      bool purelyTimeBased=false):base(model, dc, purelyTimeBased)
      { }


      public LgmImpliedYtsSpotCorrected(LinearGaussMarkovModel model,
                                 Handle<YieldTermStructure> targetCurve):
         this(model,targetCurve, model.parametrization().termStructure().link.dayCounter())
      { }
      protected override double discountImpl(double t)
      {
         Utils.QL_REQUIRE(t >= 0.0, () => "negative time (" + t.ToString() + ") given");
         return base.discountImpl(t) * targetCurve_.link.discount(t) *
                model_.parametrization().termStructure().link.discount(relativeTime_) /
                model_.parametrization().termStructure().link.discount(relativeTime_ + t);
               }
         }
}
