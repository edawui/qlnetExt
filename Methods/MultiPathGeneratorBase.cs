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

/*! \file multipathgeneratorbase.hpp
    \brief base class for multi path generators
    \ingroup methods
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNetExt
{
   public abstract class MultiPathGeneratorBase
   {
      public abstract Sample<MultiPath> next();

      public abstract void reset();


   }


   public class MultiPathGeneratorMersenneTwister : MultiPathGeneratorBase
   {
      //using PseudoRandom_rsg = InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>;

      StochasticProcess process_;
      TimeGrid grid_;
      ulong seed_;

      MultiPathGenerator<InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>> pg_;

      bool antitheticSampling_;
      bool antitheticVariate_;



      public MultiPathGeneratorMersenneTwister(StochasticProcess process, TimeGrid grid, ulong seed, bool antitheticSampling)

      {
         process_ = process; grid_ = grid; seed_ = seed; antitheticSampling_ = antitheticSampling; antitheticVariate_ = true;

         reset();
      }

      public override Sample<MultiPath> next()
      {
         throw new NotImplementedException();
      }

      public override void reset()
      {

         InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal> rsg =
           (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>)
           (new PseudoRandom()).make_sequence_generator(process_.size() * (grid_.size() - 1), seed_);

         pg_ = new MultiPathGenerator<InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>>(process_, grid_, rsg, false);
      }

   }




   public class MultiPathGeneratorSobol : MultiPathGeneratorBase
   {
      StochasticProcess process_;
      TimeGrid grid_;
      ulong seed_;

      //GenericLowDiscrepancy<SobolRsg, InverseCumulativeNormal>
      MultiPathGenerator<InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal>> pg_;

      //MultiPathGenerator<LowDiscrepancy. ::rsg_type>> pg_;

      public MultiPathGeneratorSobol(StochasticProcess process,
                                                 TimeGrid grid, ulong seed)
      {
         process_ = process;
         grid_ = grid;
         seed_ = seed;

         reset();
      }

      public override Sample<MultiPath> next()
      {
         throw new NotImplementedException();
      }

      public override void reset()
      {
         //LowDiscrepancy::rsg_type rsg =
         InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal> rsg =
            (InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal>)
            (new LowDiscrepancy()).make_sequence_generator(process_.size() * (grid_.size() - 1), seed_);

         pg_ = new MultiPathGenerator<InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal>>(process_, grid_, rsg, false);
      }

   }


   public class MultiPathGeneratorSobolBrownianBridge : MultiPathGeneratorBase
   {

      StochasticProcess process_;
      TimeGrid grid_;
      SobolBrownianGenerator.Ordering ordering_;
      ulong seed_;
      SobolRsg.DirectionIntegers directionIntegers_;
      SobolBrownianGenerator gen_;
      Sample<MultiPath> next_;


      public MultiPathGeneratorSobolBrownianBridge(
    StochasticProcess process, TimeGrid grid,
    SobolBrownianGenerator.Ordering ordering, ulong seed, SobolRsg.DirectionIntegers directionIntegers)
      {
         process_ = process;
         grid_ = grid;
         ordering_ = ordering;
         seed_ = seed;
         directionIntegers_ = directionIntegers;
         next_ = new Sample<MultiPath>(new MultiPath(process.size(), grid), 1.0);

         reset();
      }

      public override void reset()
      {
         gen_ = new SobolBrownianGenerator(process_.size(), grid_.size() - 1, ordering_, seed_,
                                                           directionIntegers_);
      }

      public override Sample<MultiPath> next()
      {
         Vector asset = process_.initialValues();
         MultiPath path = next_.value;
         for (int j = 0; j < asset.Count; ++j)
         {
            path[j].setFront(asset[j]);
         }
         next_.weight = gen_.nextPath();
         List<double> output = new List<double>(asset.size());
         for (int i = 1; i < grid_.size(); ++i)
         {
            double t = grid_[i - 1];
            double dt = grid_.dt(i - 1);
            gen_.nextStep(output);
            Vector tmp = new Vector(output);
            asset = process_.evolve(t, asset, dt, tmp);
            for (int j = 0; j < asset.size(); ++j)
            {
               path[j][i] = asset[j];
            }
         }
         return next_;
      }



   }
}
