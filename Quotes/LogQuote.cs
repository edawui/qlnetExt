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

   //! Class for storing logs of quotes for log-linear interpolation.
   /*! \test the correctness of the returned values is tested by
           checking them against the log of the returned values of q_

       \ingroup quotes
   */
   public class LogQuote : Quote//,  IObserver
   {
      protected Handle<Quote> q_;
      protected double logValue_;

      public LogQuote(Handle<Quote> q)
      {
         q_ = q;

         q_.registerWith(update);
         update();
      }



      public double quote()
      {
         return q_.currentLink().value();
      }

      public override double value() { return logValue_; }

      public override bool isValid() { return q_.currentLink().isValid(); }

      public void update()
      {
         double v = q_.currentLink().value();
         Utils.QL_REQUIRE(v > 0.0, () => "Invalid quote, cannot take log of non-postive number");
         logValue_ = System.Math.Log(v);
      }

   }
}

