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

/*! \file gaussian1dmodel.hpp
    \brief basic interface for one factor interest rate models
*/

// uncomment to enable NTL support (see below for more details and references)
// #define GAUSS1D_ENABLE_NTL


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QLNet;

namespace QLNet
{
   //class Gaussian1dModel
   //{
   public abstract class Gaussian1dModel : LazyObject, ITermStructureConsistentModel
   {
      /*! One factor interest rate model interface class
          The only methods that must be implemented by subclasses
          are the numeraire and zerobond methods for an input array
          of state variable values. The variable $y$ is understood
          to be the standardized (zero mean, unit variance) version
          of the model's original state variable $x$.

          NTL support may be enabled by defining GAUSS1D_ENABLE_NTL in this
          file. For details on NTL see
                   http://www.shoup.net/ntl/

          \warning the variance of the state process conditional on
          $x(t)=x$ must be independent of the value of $x$

      */




      /*! Computes the integral
      \f[ {2\pi}^{-0.5} \int_{a}^{b} p(x) \exp{-0.5*x*x} \mathrm{d}x \f]
      with
      \f[ p(x) = ax^4+bx^3+cx^2+dx+e \f].
      */
      public static double gaussianPolynomialIntegral(double a, double b,
                                               double c, double d,
                                               double e, double y0,
                                               double y1)
      {
         //  NormalDistribution temp = new NormalDistribution();
         InverseCumulativeNormal erf = new InverseCumulativeNormal();

         double aa = 4.0 * a, ba = 2.0 * Const.M_SQRT2 * b, ca = 2.0 * c,
                     da = Const.M_SQRT2 * d;
         double x0 = y0 * Const.M_SQRT_2, x1 = y1 * Const.M_SQRT_2;

         return (0.125 * (3.0 * aa + 2.0 * ca + 4.0 * e) * erf.value(x1) -
                 1.0 / (4.0 * Const.M_SQRTPI) * System.Math.Exp(-x1 * x1) *
                     (2.0 * aa * x1 * x1 * x1 + 3.0 * aa * x1 +
                      2.0 * ba * (x1 * x1 + 1.0) + 2.0 * ca * x1 + 2.0 * da)) -
                (0.125 * (3.0 * aa + 2.0 * ca + 4.0 * e) * erf.value(x0) -
                 1.0 / (4.0 * Const.M_SQRTPI) * System.Math.Exp(-x0 * x0) *
                     (2.0 * aa * x0 * x0 * x0 + 3.0 * aa * x0 +
                      2.0 * ba * (x0 * x0 + 1.0) + 2.0 * ca * x0 + 2.0 * da));

      }

      /*! Computes the integral
      \f[ {2\pi}^{-0.5} \int_{a}^{b} p(x) \exp{-0.5*x*x} \mathrm{d}x \f]
      with
      \f[ p(x) = a(x-h)^4+b(x-h)^3+c(x-h)^2+d(x-h)+e \f].
      */
      public static double gaussianShiftedPolynomialIntegral(double a, double b, double c,
                                             double d, double e, double h,
                                             double x0, double x1)
      {
         return gaussianPolynomialIntegral(
         a, -4.0 * a * h + b, 6.0 * a * h * h - 3.0 * b * h + c,
         -4 * a * h * h * h + 3.0 * b * h * h - 2.0 * c * h + d,
         a * h * h * h * h - b * h * h * h + c * h * h - d * h + e, x0, x1);
      }
      /*! Generates a grid of values for the standardized state variable $y$
         at time $T$
          conditional on $y(t)=y$, covering yStdDevs standard deviations
         consisting of
          2*gridPoints+1 points */

      //const Disposable<Array> yGrid(const Real yStdDevs, const int gridPoints,
      //                              const Real T = 1.0, const Real t = 0,
      //                              const Real y = 0) const;

      //private:
      // It is of great importance for performance reasons to cache underlying
      // swaps generated from indexes. In addition the indexes may only be given
      // as templates for the conventions with the tenor replaced by the actual
      // one later on.

      struct CachedSwapKey
      {
         public SwapIndex index;
         public Date fixing;
         public Period tenor;


         public CachedSwapKey(SwapIndex _index,
          Date _fixing,
          Period _tenor)
         {
            index = _index;
            fixing = _fixing;
            tenor = _tenor;
         }

         public static bool operator ==(CachedSwapKey o, CachedSwapKey o2)
         {

            return o.index.name() == o2.index.name() && o.fixing == o2.fixing &&
                   o.tenor == o2.tenor;
         }

         public static bool operator !=(CachedSwapKey o, CachedSwapKey o2)
         {

            return (o.index.name() != o2.index.name()) || (o.fixing == o2.fixing) || (o.tenor == o2.tenor);
         }

         public override bool Equals(object obj)
         {
            CachedSwapKey o = (CachedSwapKey)obj;
            if (!(o == null))
            {
               return o == this;
            }
            return false;

         }

         public override int GetHashCode()
         {
            return base.GetHashCode();
         }

      }


      struct CachedSwapKeyHasher
      //: std::unary_function<CachedSwapKey, std::size_t>
      {
         public static int Apply(CachedSwapKey x)
         {
            return QLNetExt.UtilsExt.HashHelper.GetHashCode(x.index.name(), x.fixing.serialNumber(), x.tenor.length(), x.tenor.units());
         }
      }

      private Dictionary<CachedSwapKey, VanillaSwap> swapCache_;




      // we let derived classes register with the termstructure
      protected Gaussian1dModel(Handle<YieldTermStructure> yieldTermStructure)
      {
         termStructure_ = yieldTermStructure;
         //registerWith(Settings.evaluationDate());
         //base..registerWith()
         termStructure_.registerWith(update);
      }

      #region ITermStructureConsistentModel
      public Handle<YieldTermStructure> termStructure()
      {
         return termStructure_;
      }

      public Handle<YieldTermStructure> termStructure_ { get; set; }

      #endregion


      protected StochasticProcess1D stateProcess_;
      protected Date evaluationDate_;
      protected bool enforcesTodaysHistoricFixings_;

      protected override void performCalculations()
      {
         evaluationDate_ = Settings.evaluationDate();
         enforcesTodaysHistoricFixings_ = Settings.enforcesTodaysHistoricFixings;
      }

      void generateArguments()
      {
         calculate();
         notifyObservers();
      }

      // retrieve underlying swap from cache if possible, otherwise
      // create it and store it in the cache
      VanillaSwap underlyingSwap(SwapIndex index, Date expiry, Period tenor)
      {

         CachedSwapKey k = new CachedSwapKey(index, expiry, tenor);
         //CacheType::iterator i = swapCache_.find(k);
         int i = swapCache_.Keys.ToList<CachedSwapKey>().FindIndex(xyz => xyz == k);
         if (i == swapCache_.Count)
         {
            VanillaSwap underlying = index.clone(tenor).underlyingSwap(expiry);
            swapCache_.Add(k, underlying);
            return underlying;
         }
         return swapCache_[k];
      }


      StochasticProcess1D stateProcess()
      {
         Utils.QL_REQUIRE(stateProcess_ != null, () => "state process not set");
         return stateProcess_;
      }


      protected abstract double numeraireImpl(double t, double y, Handle<YieldTermStructure> yts);

      protected abstract double zerobondImpl(double T, double t, double y, Handle<YieldTermStructure> yts);



      public double numeraire(double t, double y, Handle<YieldTermStructure> yts)
      {

         return numeraireImpl(t, y, yts);
      }

      public double zerobond(double T, double t, double y, Handle<YieldTermStructure> yts)
      {
         return zerobondImpl(T, t, y, yts);
      }

      public double zerobond(double T, double t, double y)
      {

         Handle<YieldTermStructure> yts = new Handle<YieldTermStructure>();
         return zerobond(T, t, y, yts);
      }

      public double numeraire(Date referenceDate, double y, Handle<YieldTermStructure> yts)
      {

         return numeraire(termStructure().currentLink().timeFromReference(referenceDate), y, yts);
      }

      public double zerobond(Date maturity, Date referenceDate,
                             double y)
      {
         Handle<YieldTermStructure> yts = new Handle<YieldTermStructure>();
         return zerobond(maturity, referenceDate, y, yts);
      }



      public double zerobond(Date maturity, Date referenceDate,
                                   double y, Handle<YieldTermStructure> yts)
      {

         return zerobond(termStructure().currentLink().timeFromReference(maturity),
                         referenceDate != null ? termStructure().currentLink().timeFromReference(referenceDate) : 0.0,
                         y, yts);
      }



      public double forwardRate(Date fixing,
                                    Date referenceDate, double y, IborIndex iborIdx)
      {

         Utils.QL_REQUIRE(iborIdx != null, () => "no ibor index given");

         calculate();

         if (fixing <= (evaluationDate_ + (enforcesTodaysHistoricFixings_ ? 0 : -1)))
            return iborIdx.fixing(fixing);

         Handle<YieldTermStructure> yts =
             iborIdx.forwardingTermStructure(); // might be empty, then use
                                                // model curve

         Date valueDate = iborIdx.valueDate(fixing);
         Date endDate = iborIdx.fixingCalendar().advance(
             valueDate, iborIdx.tenor(), iborIdx.businessDayConvention(),
             iborIdx.endOfMonth());
         // FIXME Here we should use the calculation date calendar ?
         double dcf = iborIdx.dayCounter().yearFraction(valueDate, endDate);

         return (zerobond(valueDate, referenceDate, y, yts) -
                 zerobond(endDate, referenceDate, y, yts)) /
                (dcf * zerobond(endDate, referenceDate, y, yts));
      }

      public double swapRate(Date fixing, Period tenor,
                                      Date referenceDate, double y, SwapIndex swapIdx)
      {

         Utils.QL_REQUIRE(swapIdx != null, () => "no swap index given");

         calculate();

         if (fixing <= (evaluationDate_ + (enforcesTodaysHistoricFixings_ ? 0 : -1)))
            return swapIdx.fixing(fixing);

         Handle<YieldTermStructure> ytsf =
             swapIdx.iborIndex().forwardingTermStructure();
         Handle<YieldTermStructure> ytsd =
             swapIdx.discountingTermStructure(); // either might be empty, then
                                                 // use model curve

         Schedule sched, floatSched;

         VanillaSwap underlying = underlyingSwap(swapIdx, fixing, tenor);

         sched = underlying.fixedSchedule();

         OvernightIndexedSwapIndex oisIdx = (OvernightIndexedSwapIndex)swapIdx;
         if (oisIdx != null)
         {
            floatSched = sched;
         }
         else
         {
            floatSched = underlying.floatingSchedule();
         }

         double annuity = swapAnnuity(fixing, tenor, referenceDate, y,
                                    swapIdx);  // should be fine for
                                               // overnightindexed swap indices as
                                               // well
         double floatleg = 0.0;
         if (ytsf.empty() && ytsd.empty())
         { // simple 100-formula can be used
           // only in one curve setup
            floatleg =
                (zerobond(sched.dates()[0], referenceDate, y) -
                 zerobond(sched.calendar().adjust(sched.dates()[sched.dates().Count - 1], underlying.paymentConvention()),
                          referenceDate, y));
         }
         else
         {
            for (int i = 1; i < floatSched.size(); i++)
            {
               floatleg +=
                   (zerobond(floatSched[i - 1], referenceDate, y, ytsf) /
                        zerobond(floatSched[i], referenceDate, y, ytsf) -
                    1.0) *
                   zerobond(floatSched.calendar().adjust(
                                floatSched[i], underlying.paymentConvention()),
                            referenceDate, y, ytsd);
            }
         }
         return floatleg / annuity;
      }

      public double swapAnnuity(Date fixing, Period tenor,
                                         Date referenceDate, double y,
                                        SwapIndex swapIdx)
      {

         Utils.QL_REQUIRE(swapIdx != null, () => "no swap index given");

         calculate();

         Handle<YieldTermStructure> ytsd =
             swapIdx.discountingTermStructure(); // might be empty, then use
                                                 // model curve
         VanillaSwap underlying =
                   underlyingSwap(swapIdx, fixing, tenor);

         Schedule sched = underlying.fixedSchedule();

         double annuity = 0.0;
         for (int j = 1; j < sched.size(); j++)
         {
            annuity += zerobond(sched.calendar().adjust(
                                    sched.date(j), underlying.paymentConvention()),
                                referenceDate, y, ytsd) *
                       swapIdx.dayCounter().yearFraction(sched.date(j - 1),
                                                          sched.date(j));
         }
         return annuity;
      }

      public double zerobondOption(
           Option.Type type, Date expiry, Date valueDate,
           Date maturity, double strike, Date referenceDate,
           double y, Handle<YieldTermStructure> yts, double yStdDevs,
           int yGridPoints, bool extrapolatePayoff,
           bool flatPayoffExtrapolation)
      {

         calculate();

         double fixingTime = termStructure().currentLink().timeFromReference(expiry);
         double referenceTime =
             referenceDate == null ? 0.0 : termStructure().currentLink().timeFromReference(referenceDate);

         Vector yg = yGrid(yStdDevs, yGridPoints, fixingTime, referenceTime, y);
         Vector z = yGrid(yStdDevs, yGridPoints);

         Vector p = new Vector(yg.size());

         for (int i = 0; i < yg.size(); i++)
         {
            double expValDsc = zerobond(valueDate, expiry, yg[i], yts);
            double discount =
                zerobond(maturity, expiry, yg[i], yts) / expValDsc;
            p[i] =
                        System.Math.Max((type == Option.Type.Call ? 1.0 : -1.0) * (discount - strike),
                                 0.0) /
                        numeraire(fixingTime, yg[i], yts) * expValDsc;
         }

         CubicInterpolation payoff = new CubicInterpolation(z, z.Count, p,


             //z.begin(), z.end(), p.begin(),
             CubicInterpolation.DerivativeApprox.Spline,
             true,
             CubicInterpolation.BoundaryCondition.Lagrange, 0.0, CubicInterpolation.BoundaryCondition.Lagrange, 0.0);

         double price = 0.0;
         for (int i = 0; i < z.size() - 1; i++)
         {
            price += gaussianShiftedPolynomialIntegral(
                0.0, payoff.cCoefficients()[i], payoff.bCoefficients()[i],
                payoff.aCoefficients()[i], p[i], z[i], z[i], z[i + 1]);
         }
         if (extrapolatePayoff)
         {
            if (flatPayoffExtrapolation)
            {
               price += gaussianShiftedPolynomialIntegral(
                   0.0, 0.0, 0.0, 0.0, p[z.size() - 2], z[z.size() - 2],
                   z[z.size() - 1], 100.0);
               price += gaussianShiftedPolynomialIntegral(0.0, 0.0, 0.0, 0.0, p[0],
                                                          z[0], -100.0, z[0]);
            }
            else
            {
               if (type == Option.Type.Call)
                  price += gaussianShiftedPolynomialIntegral(
                      0.0, payoff.cCoefficients()[z.size() - 2],
                      payoff.bCoefficients()[z.size() - 2],
                      payoff.aCoefficients()[z.size() - 2], p[z.size() - 2],
                      z[z.size() - 2], z[z.size() - 1], 100.0);
               if (type == Option.Type.Put)
                  price += gaussianShiftedPolynomialIntegral(
                      0.0, payoff.cCoefficients()[0], payoff.bCoefficients()[0],
                      payoff.aCoefficients()[0], p[0], z[0], -100.0, z[0]);
            }
         }

         return numeraire(referenceTime, y, yts) * price;
      }


      public Vector yGrid(double stdDevs,
                                                int gridPoints,
                                               double T = 1, double t = 0,
                                               double y = 0)
      {

         // we use that the standard deviation is independent of $x$ here !

         Utils.QL_REQUIRE(stateProcess_ != null, () => "state process not set");

         Vector result = new Vector(2 * gridPoints + 1, 0.0);

         double x_t, e_0_t, e_t_T, stdDev_0_t, stdDev_t_T;
         double stdDev_0_T = stateProcess_.stdDeviation(0.0, 0.0, T);
         double e_0_T = stateProcess_.expectation(0.0, 0.0, T);

         if (t < Const.QL_EPSILON)
         {
            // stdDev_0_t = 0.0;
            stdDev_t_T = stdDev_0_T;
            // e_0_t = 0.0;
            // x_t = 0.0;
            e_t_T = e_0_T;
         }
         else
         {
            stdDev_0_t = stateProcess_.stdDeviation(0.0, 0.0, t);
            stdDev_t_T = stateProcess_.stdDeviation(t, 0.0, T - t);
            e_0_t = stateProcess_.expectation(0.0, 0.0, t);
            x_t = y * stdDev_0_t + e_0_t;
            e_t_T = stateProcess_.expectation(t, x_t, T - t);
         }

         double h = stdDevs / ((double)gridPoints);

         for (int j = -gridPoints; j <= gridPoints; j++)
         {
            result[j + gridPoints] =
                (e_t_T + stdDev_t_T * ((double)j) * h - e_0_T) / stdDev_0_T;
         }

         return result;
      }


      void ITermStructureConsistentModel.notifyObservers()
      {
         //todo
      }

      void ITermStructureConsistentModel.registerWith(Callback handler)
      {
         termStructure().currentLink().registerWith(handler);
      }

      void ITermStructureConsistentModel.unregisterWith(Callback handler)
      {
         termStructure().currentLink().unregisterWith(handler);

      }

      void ITermStructureConsistentModel.update()
      {
         termStructure().currentLink().update();

      }

   }
}
