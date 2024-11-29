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

/*! \file audbbsw.hpp
    \brief AUD-BBSW index
    \ingroup indexes
*/
//! AUD-BBSW index
/*! AUD-BBSW rate fixed by the AFMA.

    See <http://www.afma.com.au/data/bbsw.html>.

    \remark Using Australia calendar, should be Sydney.

    \warning Convention should be Modified Following Bimonthly.
    \warning Check EOM.

            \ingroup indexes
*/
using QLNet;

namespace QLNetExt
{
   public class AUDBbsw : IborIndex
    {
        public AUDBbsw(Period tenor)
           : base("AUD-BBSW", tenor, 0, new AUDCurrency(), new Australia(),BusinessDayConvention.ModifiedFollowing, false
                 ,new Actual365Fixed(), new Handle<YieldTermStructure>())
        { }

        public AUDBbsw(Period tenor, Handle<YieldTermStructure> h)
           : base("AUD-BBSW", tenor, 0, new AUDCurrency(), new Australia(),BusinessDayConvention.ModifiedFollowing, false
                 ,new Actual365Fixed(), h)
       { }
    }

}
