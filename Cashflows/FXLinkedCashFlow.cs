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
   public class FXLinkedCashFlow : CashFlow
   {

      Date cashFlowDate_;
      Date fxFixingDate_;
      double foreignAmount_;
      FxIndex fxIndex_;
      bool invertIndex_;




      public FXLinkedCashFlow(Date cashFlowDate, Date fxFixingDate, double foreignAmount,
                                               FxIndex fxIndex, bool invertIndex)
      {
         cashFlowDate_ = cashFlowDate;
         fxFixingDate_ = fxFixingDate;
         foreignAmount_ = foreignAmount;
         fxIndex_ = fxIndex;
         invertIndex_ = invertIndex;

      }


      public override Date date() { return cashFlowDate_; }
      public override double amount() { return foreignAmount_ * fxRate(); }
      //@}

      public Date fxFixingDate() { return fxFixingDate_; }
      public FxIndex index() { return fxIndex_; }
      public bool invertIndex() { return invertIndex_; }


      public override void accept(IAcyclicVisitor v)
      {
         if (v != null)
            v.visit(this);
         else
            base.accept(v);
      }

      private double fxRate()
      {
         double fixing = fxIndex_.fixing(fxFixingDate_);
         return invertIndex_ ? 1.0 / fixing : fixing;
      }
   }

}
