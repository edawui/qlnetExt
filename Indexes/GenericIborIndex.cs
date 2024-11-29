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

/*! \file genericiborindex.hpp
    \brief Generic Ibor Index
    \ingroup indexes
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{

   //! Generic Ibor Index
   /*! This Ibor Index allows you to wrap any arbitary currency in a generic index.

       We assume 2 settlement days, Target Calendar, ACT/360.

       The name is always CCY-GENERIC so there is no risk of collision with real ibor names
               \ingroup indexes
    */
   public class GenericIborIndex :  IborIndex {
public    GenericIborIndex( Period tenor,  Currency ccy,   Handle<YieldTermStructure> h )
        : base(ccy.code + "-GENERIC", tenor, 2, ccy,new  TARGET(), BusinessDayConvention.Following, false,new Actual360(), h) { }
}
}
