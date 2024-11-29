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

/*! \file qle/instruments/oibasisswap.hpp
    \brief Overnight index swap paying compounded overnight vs. float

        \ingroup instruments
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public class OvernightIndexedBasisSwap : Swap
   {
      public enum Type { Receiver = -1, Payer = 1 };

      Type type_;
      List<double> nominals_;
      Schedule oisSchedule_;
      OvernightIndex overnightIndex_;
      Schedule iborSchedule_;
      IborIndex iborIndex_;
      double oisSpread_, iborSpread_;

      public Type type() { return type_; }
      List<double> nominals() { return nominals_; }

      public Schedule oisSchedule() { return oisSchedule_; }

      public Schedule iborSchedule() { return iborSchedule_; }

      public double oisSpread() { return oisSpread_; }
      public double iborSpread() { return iborSpread_; }

      public List<CashFlow> iborLeg() { return legs_[0]; }
      public List<CashFlow> overnightLeg() { return legs_[1]; }


      public OvernightIndex overnightIndex() { return overnightIndex_; }
      public IborIndex iborIndex() { return iborIndex_; }


      public OvernightIndexedBasisSwap(Type type, double nominal, Schedule oisSchedule,
                                                        OvernightIndex overnightIndex,
                                                        Schedule iborSchedule,
                                                        IborIndex iborIndex, double oisSpread,
                                                       double iborSpread) : base(2)
      {
         type_ = type; nominals_ = Enumerable.Repeat<double>(nominal, 1).ToList();//List<double>(1, nominal);
         oisSchedule_ = oisSchedule;
         overnightIndex_ = overnightIndex; iborSchedule_ = iborSchedule; iborIndex_ = iborIndex; oisSpread_ = oisSpread;
         iborSpread_ = iborSpread;

         initialize();
      }

      public OvernightIndexedBasisSwap(Type type, List<double> nominals, Schedule oisSchedule,
                                                       OvernightIndex overnightIndex,
                                                       Schedule iborSchedule,
                                                       IborIndex iborIndex,
                                                      double oisSpread, double iborSpread)
     : base(2)
      {
         type_ = type; nominals_ = nominals; oisSchedule_ = oisSchedule; overnightIndex_ = overnightIndex;
         iborSchedule_ = iborSchedule; iborIndex_ = iborIndex; oisSpread_ = oisSpread; iborSpread_ = iborSpread;


         initialize();
      }


      private void initialize()
      {
         legs_[0] = ((IborLeg)(new IborLeg(iborSchedule_, iborIndex_).withNotionals(nominals_))).withSpreads(iborSpread_);

         legs_[1] = new OvernightLeg(oisSchedule_, overnightIndex_).withNotionals(nominals_).withSpreads(oisSpread_);

         for (int j = 0; j < 2; ++j)
         {
            for (int i = 0; i < legs_[j].Count; ++i)//Leg::iterator i = legs_[j].begin(); i != legs_[j].end(); ++i)
               legs_[j][i].registerWith(update);// registerWith(*i);
         }

         switch (type_)
         {
            case Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;
            case Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;
            default:
               Utils.QL_FAIL("Unknown overnight-swap type");
               break;
         }
      }


      public double fairOvernightSpread()
      {
         double basisPoint = 1.0e-4;
         //static double basisPoint = 1.0e-4;
         calculate();
         return oisSpread_ - NPV_.Value / (overnightLegBPS() / basisPoint);
      }

      public double fairIborSpread()
      {
         //static double basisPoint = 1.0e-4;
         double basisPoint = 1.0e-4;
         calculate();
         return iborSpread_ - NPV_.Value / (iborLegBPS() / basisPoint);
      }

      public double overnightLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[1] != double.NaN, () => "result not available");
         return legBPS_[1].Value;
      }

      public double iborLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(legBPS_[0] != double.NaN, () => "result not available");
         return legBPS_[0].Value;
      }

      double iborLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[0] != double.NaN, () => "result not available");
         return legNPV_[0].Value;
      }

      public double overnightLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[1] != double.NaN, () => "result not available");
         return legNPV_[1].Value;
      }

      public double nominal()
      {
         Utils.QL_REQUIRE(nominals_.Count == 1, () => "varying nominals");
         return nominals_[0];
      }

   

   }
}
