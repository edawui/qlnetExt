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

using QLNet;

namespace QLNetExt
{
    public class PseudoParameter : Parameter
    {

        private new class Impl : Parameter.Impl
        {
            public Impl()
            { }

            public override double value(Vector p, double t)
            {
                Utils.QL_FAIL("pseudo - parameter can not be asked to values");
                return double.NaN;
            }
        }


        public PseudoParameter(int size , Constraint constraint)// = new  QLNet.NoConstraint())
              : base(size, new PseudoParameter.Impl(), constraint)
        { }

        public PseudoParameter(int size = 0)
              : base(size, new PseudoParameter.Impl(), new QLNet.NoConstraint())
        { }


        //public PseudoParameter()
        //     : base(0, new PseudoParameter.Impl(), new QLNet.NoConstraint())
        //{ }

    }
}
