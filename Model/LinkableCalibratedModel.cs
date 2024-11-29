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

/*! \file linkablecalibratedmodel.hpp
    \brief calibrated model class with linkable parameters
    \ingroup models
*/




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;


namespace QLNetExt
{
   public abstract class LinkableCalibratedModel : Event, IObserver //: IObservable, IObserver
   {

      public LinkableCalibratedModel()
      {
         constraint_ = new PrivateConstraint(arguments_);
         endCriteria_ = EndCriteria.Type.None;
      }


      public virtual void update()
      {
         generateArguments();
         notifyObservers();
      }


      public Constraint constraint()
      {
         return null;
      }

      //! Returns end criteria result
      public EndCriteria.Type endCriteria()
      { return endCriteria_; }

      //! Returns the problem values
      public Vector problemValues()
      { return problemValues_; }

      // protected:
      public virtual void generateArguments()
      { }

      protected List<Parameter> arguments_;
      protected Constraint constraint_;
      protected EndCriteria.Type endCriteria_;
      protected Vector problemValues_;


      //! Calibrate to a set of market instruments (usually caps/swaptions)
      /*! An additional constraint can be passed which must be
          satisfied in addition to the constraints of the model.
      //*/
      // calibrate(const std::vector<boost::shared_ptr<CalibrationHelper>>&, OptimizationMethod& method,
      //                     const EndCriteria& endCriteria, const Constraint& constraint = Constraint(),
      //                     const std::vector<Real>& weights = std::vector<Real>(),
      //                     const std::vector<bool>& fixParameters = std::vector<bool>());


      public void calibrate(List<CalibrationHelper> instruments,
                                        OptimizationMethod method, EndCriteria endCriteria)
      {
         calibrate(instruments, method, endCriteria, new Constraint(),new List<double>(), new List<bool>());
      }


      public void calibrate(List<CalibrationHelper> instruments,
                                        OptimizationMethod method, EndCriteria endCriteria,
                                         Constraint additionalConstraint)
      {
         calibrate(instruments, method, endCriteria, additionalConstraint, new List<double>(), new List<bool>());
      }


      public void calibrate(List<CalibrationHelper> instruments,
                                        OptimizationMethod method, EndCriteria endCriteria,
                                         Constraint additionalConstraint, List<double> weights)
      {
         calibrate(instruments, method, endCriteria, additionalConstraint,weights, new List<bool>());
      }

         public void calibrate(List<CalibrationHelper> instruments,
                                        OptimizationMethod method, EndCriteria endCriteria,
                                         Constraint additionalConstraint, List<double> weights,
                                       List<bool> fixParameters)
      {

         Utils.QL_REQUIRE(weights.empty() || weights.Count == instruments.Count,
                           () => "mismatch between number of instruments ("
                           + instruments.Count.ToString() + ") and weights("
                           + weights.Count.ToString() + ")"
                           );

         Constraint c = new Constraint();
         if (additionalConstraint.empty())
         { c = constraint_; }
         else
         {
            c = new CompositeConstraint(constraint_, additionalConstraint);
         }
         List<double> w = weights.empty() ? Enumerable.Repeat(1.0, instruments.Count).ToList() : weights;

         Vector prms = parameters();
         List<bool> all = Enumerable.Repeat(false, prms.Count).ToList();
         Projection proj = new Projection(prms, fixParameters.Count > 0 ? fixParameters : all);
         CalibrationFunction f = new CalibrationFunction(this, instruments, w, proj);
         ProjectedConstraint pc = new ProjectedConstraint(c, proj);
         Problem prob = new Problem(f, pc, proj.project(prms));
         endCriteria_ = method.minimize(prob, endCriteria);
         Vector result = new Vector(prob.currentValue());
         setParams(proj.include(result));
         problemValues_ = prob.values(result);

         //notifyObservers();
      }


      public override Date date()
      {
         throw new NotImplementedException();
         return new Date();
      }


      public double value(Vector parameters, List<CalibrationHelper> instruments)
      {
         List<double> w = Enumerable.Repeat(1.0, instruments.Count).ToList();
         Projection p = new Projection(parameters);
         CalibrationFunction f = new CalibrationFunction(this, instruments, w, p);
         return f.value(parameters);
      }

      //! Returns array of arguments on which calibration is done
     public Vector parameters()
      {
         int size = 0, i;
         for (i = 0; i < arguments_.Count; i++)
            size += arguments_[i].size();
         Vector prmters = new Vector(size);
         int k = 0;
         for (i = 0; i < arguments_.Count; i++)
         {
            for (int j = 0; j < arguments_[i].size(); j++, k++)
            {
               prmters[k] = arguments_[i].parameters()[j];
            }
         }
         return prmters;
      }

      public void setParams(Vector parameters)
      {
         //Vector.Enumerator p = parameters.First();
         int p = 0;
         for (int i = 0; i < arguments_.Count; ++i)
         {
            for (int j = 0; j < arguments_[i].size(); ++j, ++p)
            {
               Utils.QL_REQUIRE(p != parameters.Count, () => "parameter array too small");
               arguments_[i].setParam(j, parameters[p]);
            }
         }
         Utils.QL_REQUIRE(p == parameters.Count, () => "parameter array too big!");
         generateArguments();
         notifyObservers();
      }


   }

   //private:
   //! Constraint imposed on arguments
   public class PrivateConstraint : Constraint
   {
      private class Impl : IConstraint //Constraint.Impl
      {
         List<Parameter> arguments_;

         public Impl(List<Parameter> arguments)
         {
            arguments_ = arguments;
         }

         public bool test(Vector parameters)
         {
            int k = 0;
            for (int i = 0; i < arguments_.Count; i++)
            {
               int size = arguments_[i].size();
               Vector testParams = new Vector(size);
               for (int j = 0; j < size; j++, k++)
                  testParams[j] = parameters[k];
               if (!arguments_[i].testParams(testParams))
                  return false;
            }
            return true;
         }

         public Vector upperBound(Vector parameters)
         {
            int k = 0, k2 = 0;
            int totalSize = 0;
            for (int i = 0; i < arguments_.Count; i++)
            {
               totalSize += arguments_[i].size();
            }
            Vector result = new Vector(totalSize);
            for (int i = 0; i < arguments_.Count; i++)
            {
               int size = arguments_[i].size();
               Vector partialParams = new Vector(size);
               for (int j = 0; j < size; j++, k++)
                  partialParams[j] = parameters[k];
               Vector tmpBound = arguments_[i].constraint().upperBound(partialParams);
               for (int j = 0; j < size; j++, k2++)
                  result[k2] = tmpBound[j];
            }
            return result;
         }

         public Vector lowerBound(Vector parameters)
         {
            int k = 0, k2 = 0;
            int totalSize = 0;
            for (int i = 0; i < arguments_.Count; i++)
            {
               totalSize += arguments_[i].size();
            }
            Vector result = new Vector(totalSize);
            for (int i = 0; i < arguments_.Count; i++)
            {
               int size = arguments_[i].size();
               Vector partialParams = new Vector(size);
               for (int j = 0; j < size; j++, k++)
                  partialParams[j] = parameters[k];
               Vector tmpBound = arguments_[i].constraint().lowerBound(partialParams);
               for (int j = 0; j < size; j++, k2++)
                  result[k2] = tmpBound[j];
            }
            return result;
         }

      }
      public PrivateConstraint(List<Parameter> arguments) : base(new PrivateConstraint.Impl(arguments))
      { }
   }

   //! Calibration cost function class
   public class CalibrationFunction : CostFunction
   {

      private LinkableCalibratedModel model_;
      List<CalibrationHelper> instruments_;
      List<double> weights_;

      Projection projection_;

      public CalibrationFunction(LinkableCalibratedModel model, List<CalibrationHelper> h,
                     List<double> weights, Projection projection)
      {
         model_ = model;//, no_deletion),
         instruments_ = h;
         weights_ = weights;
         projection_ = projection;
      }

      public override double value(Vector parameters)
      {
         model_.setParams(projection_.include(parameters));
         double value = 0.0;
         for (int i = 0; i < instruments_.Count; i++)
         {
            double diff = instruments_[i].calibrationError();
            value += diff * diff * weights_[i];
         }
         return System.Math.Sqrt(value);
      }

      public override Vector values(Vector parameters)
      {
         model_.setParams(projection_.include(parameters));
         Vector values = new Vector(instruments_.Count);
         for (int i = 0; i < instruments_.Count; i++)
         {
            values[i] = instruments_[i].calibrationError() * System.Math.Sqrt(weights_[i]);
         }
         return values;
      }

      public override double finiteDifferenceEpsilon() { return 1e-6; }


   }

   //! Linkable Calibrated Model
   /*! \ingroup models
   */
}
