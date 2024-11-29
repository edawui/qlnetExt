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

/*! \file dynamicstype.hpp
    \brief dynamics type definitions
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
   public class DynamicsType
   {
      /*! \addtogroup termstructues
          @{
      */

      //! Stickyness
      public enum Stickyness { StickyStrike, StickyLogMoneyness, StickyAbsoluteMoneyness };

      //! Reaction to Time Decay
      public enum ReactionToTimeDecay { ConstantVariance, ForwardForwardVariance };

      //! Yield Curve Roll Down
      public enum YieldCurveRollDown { ConstantDiscounts, ForwardForward };

      /*! @} */

      public string ToString(Stickyness t)
      {
         switch (t)
         {
            case Stickyness.StickyStrike:
               return "StickyStrike";

            case Stickyness.StickyLogMoneyness:
               return "StickyLogMoneyness";

            case Stickyness.StickyAbsoluteMoneyness:
               return "StickyAbsoluteMoneyness";

            default:
               return "Unknown stickyness type (" + t + ")";
         }
         //return null;
      }

      public string ToString(ReactionToTimeDecay t)
      {
         switch (t)
         {
            case ReactionToTimeDecay.ConstantVariance:
               return "ConstantVariance";
            case ReactionToTimeDecay.ForwardForwardVariance:
               return "ForwardForwardVariance";
            default:
               return "Unknown reaction to time decay type (" + t + ")";
         }
      }

      public string ToString(YieldCurveRollDown t)
      {
         switch (t)
         {
            case YieldCurveRollDown.ConstantDiscounts:
               return "ConstantDiscounts";
            case YieldCurveRollDown.ForwardForward:
               return "ForwardForward";
            default:
               return "Unknown yield curve roll down type (" + t + ")";
         }
      }

   } // namesapce QuantExt
}
