
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
   //using Leg = List<CashFlow>;//> Leg;




   class PricerSetter : IAcyclicVisitor
   //, IVisitor<CashFlow>,
   //public Visitor<Coupon>,
   //public Visitor<AverageONIndexedCoupon>,
   //public Visitor<SubPeriodsCoupon>
   {

      FloatingRateCouponPricer pricer_;


      public PricerSetter(FloatingRateCouponPricer pricer)
      {
         pricer_ = pricer;
      }


      public void visit(CashFlow cashFlow)
      {
         // nothing to do
      }

      public void visit(Coupon coupon)
      {
         // nothing to do
      }

      public void visit(AverageONIndexedCoupon c)
      {
         AverageONIndexedCouponPricer averageONIndexedCouponPricer = (AverageONIndexedCouponPricer)pricer_;
         Utils.QL_REQUIRE(averageONIndexedCouponPricer != null, () => "Pricer not compatible with Average ON Indexed coupon");
         c.setPricer(averageONIndexedCouponPricer);
      }

      public void visit(SubPeriodsCoupon c)
      {
         SubPeriodsCouponPricer subPeriodsCouponPricer = (SubPeriodsCouponPricer)pricer_;
         Utils.QL_REQUIRE(subPeriodsCouponPricer != null, () => "Pricer not compatible with sub-periods coupon");
         c.setPricer(subPeriodsCouponPricer);
      }



      void IAcyclicVisitor.visit(object o)
      {
         //todo
         throw new NotImplementedException();
      }

      public static void setCouponPricer(List<CashFlow> leg, FloatingRateCouponPricer pricer)
      {
         PricerSetter setter = new PricerSetter(pricer);
         for (int i = 0; i < leg.Count; ++i)
         {
            leg[i].accept(setter);
         }
      }

      public static void setCouponPricers(ref List<CashFlow> leg, List<FloatingRateCouponPricer> pricers)
      {

         int nCashFlows = leg.Count;
         Utils.QL_REQUIRE(nCashFlows > 0, () => "No cashflows");

         int nPricers = pricers.Count;
         Utils.QL_REQUIRE(nCashFlows >= nPricers, () => "Mismatch between leg size (" + nCashFlows + ") and number of pricers (" + nPricers + ")");

         for (int i = 0; i < nCashFlows; ++i)
         {
            PricerSetter setter = new PricerSetter( i < nPricers ? pricers[i] : pricers[nPricers - 1]);
            leg[i].accept(setter);
         }
      }

   }
}
