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

/*! \file depositengine.hpp
    \brief deposit engine
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class DepositEngine : Deposit.Engine
   {
      Handle<YieldTermStructure> discountCurve_;
      bool includeSettlementDateFlows_;
      Date settlementDate_, npvDate_;



      public DepositEngine(Handle<YieldTermStructure> discountCurve,
                            bool includeSettlementDateFlows, Date settlementDate, Date npvDate)

      {
         discountCurve_ = discountCurve; includeSettlementDateFlows_ = includeSettlementDateFlows;
         settlementDate_ = settlementDate; npvDate_ = npvDate;

         discountCurve_.registerWith(update);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(!discountCurve_.empty(), () => "discounting term structure handle is empty");

         results_.value = 0.0;
         results_.errorEstimate = double.NaN;

         Date refDate = discountCurve_.currentLink().referenceDate();

         Date settlementDate = settlementDate_;
         if (settlementDate_ == new Date())
         {
            settlementDate = refDate;
         }
         else
         {
            Utils.QL_REQUIRE(settlementDate >= refDate, () => "settlement date (" + settlementDate + ") before discount curve reference date (" + refDate + ")");
         }

         Date valuationDate = npvDate_;
         if (npvDate_ == new Date())
         {
            valuationDate = refDate;
         }
         else
         {
            Utils.QL_REQUIRE(npvDate_ >= refDate, () => "npv date (" + npvDate_ + ") before discount curve reference date (" + refDate + ")");
         }

         bool includeRefDateFlows = includeSettlementDateFlows_ ? includeSettlementDateFlows_ : Settings.includeReferenceDateEvents;

         results_.value = CashFlows.npv(arguments_.leg, discountCurve_, includeRefDateFlows, settlementDate, valuationDate);

         results_.fairRate = arguments_.index.clone(discountCurve_).fixing(refDate);

      }


   }
}
