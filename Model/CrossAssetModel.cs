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
/*! \file models/crossassetmodel.hpp
    \brief cross asset model
    \ingroup crossassetmodel
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNetExt.CrossAssetModelTypes
{

   //! Cross Asset Type
   //! \ingroup crossassetmodel
   public enum AssetType { IR, FX, INF, EQ };
}

namespace QLNetExt
{
   using QLNetExt.CrossAssetModelTypes;
   using QLNet;

   public class CrossAssetModel : LinkableCalibratedModel
   {



      /* members */

      protected int nIrLgm1f_;
      protected int nFxBs_;
      protected int nEqBs_;
      protected int nInfl_;
      protected int totalNumberOfParameters_;
      protected List<Parametrization> p_;
      protected List<LinearGaussMarkovModel> lgm_;
      protected Matrix rho_;
      QLNet.MatrixUtilitites.SalvagingAlgorithm salvaging_;
      protected Integrator integrator_;
      protected CrossAssetStateProcess stateProcessExact_;
      protected CrossAssetStateProcess stateProcessEuler_;


      public CrossAssetModel get()
      { return this; }

      /*! Parametrizations must be given in the following order
     - IR  (first parametrization defines the domestic currency)
     - FX  (for all pairs domestic-ccy defined by the IR models)
     - EQ  (for all names equity currency defined in Parametrization)
     If the correlation matrix is not given, it is initialized
     as the unit matrix (and can be customized after
     construction of the model).
 */

      public CrossAssetModel(List<Parametrization> parametrizations,
                                 Matrix correlation, MatrixUtilitites.SalvagingAlgorithm salvaging)
    : base()
      {
         p_ = parametrizations;
         rho_ = correlation;
         salvaging_ = salvaging;
         initialize();
      }

      /*! IR-FX model based constructor */
      public CrossAssetModel(List<LinearGaussMarkovModel> currencyModels,
                            List<FxBsParametrization> fxParametrizations
                           ) : this(currencyModels, fxParametrizations, new Matrix(), MatrixUtilitites.SalvagingAlgorithm.None)
      {
      }
      public CrossAssetModel(List<LinearGaussMarkovModel> currencyModels,
                          List<FxBsParametrization> fxParametrizations,
                          Matrix correlation, MatrixUtilitites.SalvagingAlgorithm salvaging)
 : base()
      {
         lgm_ = currencyModels;
         rho_ = correlation;

         salvaging_ = salvaging;

         for (int i = 0; i < currencyModels.Count; ++i)
         {
            p_.Add(currencyModels[i].parametrization());
         }
         for (int i = 0; i < fxParametrizations.Count; ++i)
         {
            p_.Add(fxParametrizations[i]);
         }
         initialize();
      }


      public int components(AssetType t)
      {
         switch (t)
         {
            case AssetType.IR:
               return nIrLgm1f_;
               break;
            case AssetType.FX:
               return nFxBs_;
               break;
            case AssetType.INF:
               return nInfl_;
               break;
            case AssetType.EQ:
               return nEqBs_;
               break;
            default:
               Utils.QL_FAIL("asset class " + t + " not known.");
               return 0;//todo remove
               break;
         }
      }



      public int idx(AssetType t, int i)
      {
         switch (t)
         {
            case AssetType.IR:
               Utils.QL_REQUIRE(i < nIrLgm1f_, () => "ir index (" + i + ") must be in 0..." + (nIrLgm1f_ - 1));
               return i;
            case AssetType.FX:
               Utils.QL_REQUIRE(nFxBs_ > 0, () => "fx index (" + i + ") invalid, no fx components");
               Utils.QL_REQUIRE(i < nFxBs_, () => "fx index (" + i + ") must be in 0..." + (nFxBs_ - 1));
               return nIrLgm1f_ + i;
            case AssetType.INF:
               Utils.QL_REQUIRE(nInfl_ > 0, () => "inflation index (" + i + ") invalid, no eq components");
               Utils.QL_REQUIRE(i < nInfl_, () => "inflation index (" + i + ") must be in 0..." + (nInfl_ - 1));
               return nIrLgm1f_ + nFxBs_ + i;
            case AssetType.EQ:
               Utils.QL_REQUIRE(nEqBs_ > 0, () => "eq index (" + i + ") invalid, no eq components");
               Utils.QL_REQUIRE(i < nEqBs_, () => "eq index (" + i + ") must be in 0..." + (nEqBs_ - 1));
               return nIrLgm1f_ + nFxBs_ + nInfl_ + i;
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo remove
               break;
         }
      }


      public int cIdx(AssetType t, int i, int offset)
      {
         Utils.QL_REQUIRE(offset < brownians(t, i), () => "c-offset (" + offset + ") for asset class " + t + " and index " + i
                                                              + " must be in 0..." + (brownians(t, i) - 1));
         // the return values below assume specific models and have to be
         // generalized when other model types are added
         switch (t)
         {
            case AssetType.IR:
               Utils.QL_REQUIRE(i < nIrLgm1f_, () => "irlgm1f index (" + i + ") must be in 0..." + (nIrLgm1f_ - 1));
               return i;
            case AssetType.FX:
               Utils.QL_REQUIRE(nFxBs_ > 0, () => "fx index (" + i + ") invalid, no fx components");
               Utils.QL_REQUIRE(i < nFxBs_, () => "fxbs index (" + i + ") must be in 0..." + (nFxBs_ - 1));
               return nIrLgm1f_ + i;
            case AssetType.INF:
               Utils.QL_REQUIRE(nInfl_ > 0, () => "inflation index (" + i + ") invalid, no inflation components");
               Utils.QL_REQUIRE(i < nInfl_, () => "inflation index (" + i + ") must be in 0..." + (nInfl_ - 1));
               return nIrLgm1f_ + nFxBs_ + i;
            case AssetType.EQ:
               Utils.QL_REQUIRE(nEqBs_ > 0, () => "eq index (" + i + ") invalid, no eq components");
               Utils.QL_REQUIRE(i < nEqBs_, () => "eqbs index (" + i + ") must be in 0..." + (nEqBs_ - 1));
               return nIrLgm1f_ + nFxBs_ + nInfl_ + i;
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo remove
         }
      }


      public int brownians(AssetType t, int i)
      {
         switch (t)
         {
            case AssetType.IR:
               return 1;
            case AssetType.FX:
               return 1;
            case AssetType.INF:
               Utils.QL_FAIL("Inflation not yet supported, brownians member to be completed later");
               return 0;//todo

            case AssetType.EQ:
               return 1;
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo
         }
      }


      public int stateVariables(AssetType t, int i)
      {
         switch (t)
         {
            case AssetType.IR:
               return 1;
            case AssetType.FX:
               return 1;
            case AssetType.INF:
               Utils.QL_FAIL("Inflation not yet supported, stateVariables member function to be completed later");
               return 0;//todo debug
            case AssetType.EQ:
               return 1;
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo debug
         }
      }


      public int pIdx(AssetType t, int i, int offset)
      {
         Utils.QL_REQUIRE(offset < stateVariables(t, i), () => "p-offset (" + offset + ") for asset class "
                                                               + t.ToString() + " and index " + i
                                                               + " must be in 0..." + ( stateVariables(t, i) - 1 ));
         // the return values below assume specific models and have to be
         // generalized when other model types are added
         switch (t)
         {
            case AssetType.IR:
               Utils.QL_REQUIRE(i < nIrLgm1f_, () => "irlgm1f index (" + i + ") must be in 0..." + (nIrLgm1f_ - 1));
               return i;
            case AssetType.FX:
               Utils.QL_REQUIRE(nFxBs_ > 0, () => "fx index (" + i + ") invalid, no fx components");
               Utils.QL_REQUIRE(i < nFxBs_, () => "fxbs index (" + i + ") must be in 0..." + (nFxBs_ - 1));
               return nIrLgm1f_ + i;
            case AssetType.INF:
               Utils.QL_REQUIRE(nInfl_ > 0, () => "inflation index (" + i + ") invalid, no inflation components");
               Utils.QL_REQUIRE(i < nInfl_, () => "inflation index (" + i + ") must be in 0..." + (nInfl_ - 1));
               return nIrLgm1f_ + nFxBs_ + i;
            case AssetType.EQ:
               Utils.QL_REQUIRE(nEqBs_ > 0, () => "eq index (" + i + ") invalid, no eq components");
               Utils.QL_REQUIRE(i < nEqBs_, () => "eqbs index (" + i + ") must be in 0..." + (nEqBs_ - 1));
               return nIrLgm1f_ + nFxBs_ + nInfl_ + i;
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo remove
         }
      }

      public int aIdx(AssetType t, int i, int offset)
      {
         Utils.QL_REQUIRE(offset < arguments(t, i), () => "a-offset (" + offset + ") for asset class " + t + " and index " + i
                                                        + " must be in 0..." + (arguments(t, i) - 1));
         // the return values below assume specific models and have to be
         // generalized when other model types are added
         int tmp_infl_args_count = 1; // TODO : update this when inflation being implemented
         switch (t)
         {
            case AssetType.IR:
               Utils.QL_REQUIRE(i < nIrLgm1f_, () => "irlgm1f index (" + i + ") must be in 0..." + (nIrLgm1f_ - 1));
               return 2 * i + offset;
            case AssetType.FX:
               Utils.QL_REQUIRE(nFxBs_ > 0, () => "fx index (" + i + ") invalid, no fx components");
               Utils.QL_REQUIRE(i < nFxBs_, () => "fxbs index (" + i + ") must be in 0..." + (nFxBs_ - 1));
               return 2 * nIrLgm1f_ + i;
            case AssetType.INF:
               // don't forget tmp_infl_args_count when implementing this.
               Utils.QL_FAIL("Inflation not yet supported - this is to be completed later as part of the inflation implementation");
               return 0;//todo
            case AssetType.EQ:
               return (arguments(AssetType.IR, i) * nIrLgm1f_) + (arguments(AssetType.FX, i) * nFxBs_) + (tmp_infl_args_count * nInfl_) +
                      (arguments(AssetType.EQ, i) * i) + offset;
               return 0;//todo
            default:
               Utils.QL_FAIL("CR, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo remove
         }
      }



      public int arguments( AssetType t, int i) {
    switch (t) {
    case AssetType.IR:
        return 2;
            case AssetType.FX:
        return 1;
            case AssetType.INF:
        Utils.QL_FAIL("Inflation not yet supported, arguments member function to be completed later");
               return 3;//todo
            case AssetType.EQ:
        return 1;
    default:
        Utils.QL_FAIL("EQ, COM not yet supported or type (" + t + ") unknown");
               return 0;//todo
   }
}

public IrLgm1fParametrization irlgm1f(int ccy)
      {
         return lgm(ccy).parametrization();
      }

      public LinearGaussMarkovModel lgm(int ccy)
      {
         return lgm_[idx(AssetType.IR, ccy)];
      }

      public int ccyIndex(Currency ccy)
      {
         int i = 0;
         // FIXME: remove try/catch
         try
         {
            // irlgm1f() will throw if out of bounds
            while (irlgm1f(i).currency() != ccy)
               ++i;
            return i;
         }
         catch (Exception ex)
         {
            Utils.QL_FAIL("currency " + ccy.code + " not present in cross asset model");
            return i;//todo remove
         }
      }

      // inline

      public StochasticProcess stateProcess(CrossAssetStateProcess.discretization disc)
      {
         return null;//tododisc == CrossAssetStateProcess.discretization.exact ? stateProcessExact_ : stateProcessEuler_;
      }

      public int dimension() { return nIrLgm1f_ * 1 + nFxBs_ * 1 + (nInfl_ * 1) + (nEqBs_ * 1); }

      public int brownians() { return nIrLgm1f_ * 1 + nFxBs_ * 1 + (nInfl_ * 1) + (nEqBs_ * 1); }

      public int totalNumberOfParameters() { return totalNumberOfParameters_; }

      //public  LinearGaussMarkovModel lgm( int ccy)  {
      //    return lgm_[idx(AssetType.IR, ccy)];
      //}

      //public  IrLgm1fParametrization irlgm1f( int ccy)  {
      //    return lgm(ccy).parametrization();
      //}

      public double numeraire(int ccy, double t, double x,
                                             Handle<YieldTermStructure> discountCurve)
      {
         return lgm(ccy).numeraire(t, x, discountCurve);
      }


      public double discountBond(int ccy, double t, double T, double x)
      {
         return discountBond(ccy, t, T, x, new Handle<YieldTermStructure>());
      }

         public double discountBond(int ccy, double t, double T, double x,
                                                Handle<YieldTermStructure> discountCurve)
      {
         return lgm(ccy).discountBond(t, T, x, discountCurve);
      }

      public double reducedDiscountBond(int ccy, double t, double T, double x,
                                                       Handle<YieldTermStructure> discountCurve)
      {
         return lgm(ccy).reducedDiscountBond(t, T, x, discountCurve);
      }

      public double discountBondOption(int ccy, Option.Type type, double K, double t,
                                                       double S, double T,
                                                      Handle<YieldTermStructure> discountCurve)
      {
         return lgm(ccy).discountBondOption(type, K, t, S, T, discountCurve);
      }

      public FxBsParametrization fxbs(int ccy)
      {
         return (FxBsParametrization)(p_[idx(AssetType.FX, ccy)]);
      }

      public EqBsParametrization eqbs(int name)
      {
         return (EqBsParametrization)(p_[idx(AssetType.EQ, name)]);
      }

      public Matrix correlation() { return rho_; }

      public Integrator integrator() { return integrator_; }



      public double correlation(AssetType s, int i, AssetType t, int j,
                                         int iOffset=0, int jOffset=0)
      {
         return rho_[cIdx(s, i, iOffset), cIdx(t, j, jOffset)];
      }

      public void correlation(AssetType s, int i, AssetType t, int j, double value,
                                         int iOffset=0, int jOffset=0)
      {
         int row = cIdx(s, i, iOffset);
         int column = cIdx(t, j, jOffset);
         Utils.QL_REQUIRE(row != column || Utils.close_enough(value, 1.0), () => "correlation must be 1 at (" + row + "," + column + ")");
         Utils.QL_REQUIRE(value >= -1.0 && value <= 1.0, () => "correlation must be in [-1,1] at (" + row + "," + column + ")");
         // we can not check for non-negative eigenvalues, since we do not
         // know when the correlation matrix setup is finished, but this
         // is effectively one in the state process later on anyway and
         // the user can also use checkCorrelationMatrix() to verify this
         rho_[row, column] = rho_[column, row] = value;
         update();
      }

      protected void initialize()
      {
         initializeParametrizations();
         initializeCorrelation();
         initializeArguments();
         finalizeArguments();
         checkModelConsistency();
         initDefaultIntegrator();
         initStateProcess();
      }

      protected void initDefaultIntegrator()
      {
         setIntegrationPolicy(new SimpsonIntegral(1.0E-8, 100), true);
      }
      protected void initStateProcess()
      {
         //todo stateProcessEuler_ = new CrossAssetStateProcess(this, CrossAssetStateProcess.discretization.euler, salvaging_);
         //todo  stateProcessExact_ = new CrossAssetStateProcess(this, CrossAssetStateProcess.discretization.exact, salvaging_);
      }



      protected void setIntegrationPolicy(Integrator integrator,
                                             bool usePiecewiseIntegration)
      {

         if (!usePiecewiseIntegration)
         {
            integrator_ = integrator;
            return;
         }

         // collect relevant times from parametrizations
         // we don't have to sort them or make them unique,
         // this is all done in PiecewiseIntegral for us

         List<double> allTimes = new List<double>();
         for (int i = 0; i < nIrLgm1f_; ++i)
         {
            allTimes.AddRange(p_[idx(AssetType.IR, i)].parameterTimes(0));
            allTimes.AddRange(p_[idx(AssetType.IR, i)].parameterTimes(1));
         }
         for (int i = 0; i < nFxBs_; ++i)
         {
            allTimes.AddRange(p_[idx(AssetType.FX, i)].parameterTimes(0));

         }
         for (int i = 0; i < nInfl_; ++i)
         {
            Utils.QL_FAIL("Inflation not yet fully supported. This should be filled out later.");
         }
         for (int i = 0; i < nEqBs_; ++i)
         {
            allTimes.AddRange(p_[idx(AssetType.EQ, i)].parameterTimes(0));
         }

         // use piecewise integrator avoiding the step points
         integrator_ = new PiecewiseIntegral(integrator, allTimes, true);
      }


      protected void initializeParametrizations()
      {

         // count the parametrizations and check their order and their support

         nIrLgm1f_ = 0;
         nFxBs_ = 0;
         nInfl_ = 0;
         nEqBs_ = 0;

         int i = 0;

         bool genericCtor = lgm_.empty();
         while (i < p_.Count && (IrLgm1fParametrization)(p_[i]) != null)
         {
            // initialize model, if generic constructor was used
            if (genericCtor)
            {
               lgm_.Add(new LinearGaussMarkovModel((IrLgm1fParametrization)(p_[i])));
            }
            // count things
            ++nIrLgm1f_;
            ++i;
         }

         // FX parametrizations
         while (i < p_.Count && (FxBsParametrization)(p_[i]) != null)
         {
            ++nFxBs_;
            ++i;
         }

         // Inf parametrizations

         // Eq parametrizations
         while (i < p_.Count && (EqBsParametrization)(p_[i]) != null)
         {
            ++nEqBs_;
            ++i;
         }

         Utils.QL_REQUIRE(nIrLgm1f_ > 0, () => "at least one ir parametrization must be given");

         Utils.QL_REQUIRE(nFxBs_ == nIrLgm1f_ - 1, () => "there must be n-1 fx " +
                                                  "for n ir parametrizations, found "
                                                 + nIrLgm1f_ + " ir and " + nFxBs_ + " fx parametrizations");

         Utils.QL_REQUIRE(nIrLgm1f_ + nFxBs_ + nInfl_ + nEqBs_ == p_.Count,
                   () => "problem initializing CrossAssetModel parametrizations");

         // check currencies

         // without an order or a hash function on Currency this seems hard
         // to do in a simpler way ...
         int uniqueCurrencies = 0;
         List<Currency> currencies = new List<Currency>();
         for (int ii = 0; ii < nIrLgm1f_; ++ii)
         {
            int tmp = 1;
            for (int j = 0; j < ii; ++j)
            {
               if (irlgm1f(ii).currency() == currencies[j])
                  tmp = 0;
            }
            uniqueCurrencies += tmp;
            currencies.Add(irlgm1f(ii).currency());
         }
         Utils.QL_REQUIRE(uniqueCurrencies == nIrLgm1f_, () => "there are duplicate currencies " +
                                                   "in the set of irlgm1f " +
                                                   "parametrizations");
         for (int ii = 0; ii < nFxBs_; ++ii)
         {
            Utils.QL_REQUIRE(fxbs(ii).currency() == irlgm1f(ii + 1).currency(),
                      () => "fx parametrization #" + ii + " must be for currency of ir parametrization #" + (ii + 1)
                                              + ", but they are " + fxbs(ii).currency() + " and "
                                              + irlgm1f(ii + 1).currency() + " respectively");
         }
         // check the equity currencies to ensure they are covered by CrossAssetModel
         for (int ii = 0; ii < nEqBs_; ++ii)
         {
            Currency eqCcy = eqbs(ii).currency();
            try
            {
               int eqCcyIdx = ccyIndex(eqCcy);
               Utils.QL_REQUIRE(eqCcyIdx < nIrLgm1f_, () => "Invalid currency for equity " + eqbs(ii).eqName());
            }
            catch (Exception ex)
            {
               Utils.QL_FAIL("Invalid currency (" + eqCcy.code + ") for equity " + eqbs(ii).eqName());
            }
         }
      }

      void initializeCorrelation()
      {
         int n = nIrLgm1f_ + nFxBs_ + nInfl_ + nEqBs_;
         if (rho_.empty())
         {
            rho_ = new Matrix(n, n, 0.0);
            for (int i = 0; i < n; ++i)
               rho_[i, i] = 1.0;
            return;
         }
         Utils.QL_REQUIRE(rho_.rows() == n && rho_.columns() == n, () => "correlation matrix is " + rho_.rows() + " x "
                                                                                       + rho_.columns() + " but should be "
                                                                                       + n + " x " + n);
         checkCorrelationMatrix();
      }

      protected void checkCorrelationMatrix()
      {
         int n = rho_.rows();
         int m = rho_.columns();
         Utils.QL_REQUIRE(rho_.columns() == n, () => "correlation matrix (" + n + " x " + m + " must be square");
         for (int i = 0; i < n; ++i)
         {
            for (int j = 0; j < m; ++j)
            {
               Utils.QL_REQUIRE(Utils.close_enough(rho_[i, j], rho_[j, i]), () => "correlation matrix is no symmetric, for (i,j)=("
                                                                      + i + "," + j + ") rho(i,j)=" + rho_[i, j]
                                                                      + " but rho(j,i)=" + rho_[j, i]);
               Utils.QL_REQUIRE(rho_[i, j] >= -1.0 && rho_[i, j] <= 1.0, () => "correlation matrix has invalid entry at (i,j)=("
                                                                         + i + "," + j + ") equal to " + rho_[i, j]);
            }
            Utils.QL_REQUIRE(Utils.close_enough(rho_[i, i], 1.0), () => "correlation matrix must have unit diagonal elements, "
                                                      + "but rho(i,i)=" + rho_[i, i] + " for i=" + i);
         }

         SymmetricSchurDecomposition ssd = new SymmetricSchurDecomposition(rho_);
         for (int i = 0; i < ssd.eigenvalues().size(); ++i)
         {
            Utils.QL_REQUIRE(ssd.eigenvalues()[i] >= 0.0, () => "correlation matrix has negative eigenvalue at "
                                                        + i + " (" + ssd.eigenvalues()[i] + ")");
         }
      }

      protected void initializeArguments()
      {

         Utils.QL_REQUIRE(nInfl_ == 0, () => "Inflation not covered yet, when covered please remove this check and update this function "
                                             + "(how many parameters per inflation index?)");
         arguments_.Resize(2 * nIrLgm1f_ + nFxBs_ + nEqBs_);
         for (int i = 0; i < nIrLgm1f_; ++i)
         {
            // volatility
            arguments_[aIdx(AssetType.IR, i, 0)] = irlgm1f(i).parameter(0);
            // reversion
            arguments_[aIdx(AssetType.IR, i, 1)] = irlgm1f(i).parameter(1);
         }
         for (int i = 0; i < nFxBs_; ++i)
         {
            // volatility
            arguments_[aIdx(AssetType.FX, i, 0)] = fxbs(i).parameter(0);
         }
         for (int i = 0; i < nInfl_; ++i)
         {
            // inflation
            Utils.QL_FAIL("Inflation not supported yet");
         }
         for (int i = 0; i < nEqBs_; ++i)
         {
            // volatility
            arguments_[aIdx(AssetType.EQ, i, 0)] = eqbs(i).parameter(0);
         }
      }

      protected void finalizeArguments()
      {

         totalNumberOfParameters_ = 0;
         for (int i = 0; i < arguments_.Count; ++i)
         {
            Utils.QL_REQUIRE(arguments_[i] != null, () => "unexpected error: argument " + i + " is null");
            totalNumberOfParameters_ += arguments_[i].size();
         }
      }

      protected void checkModelConsistency()
      {
         Utils.QL_REQUIRE(nIrLgm1f_ > 0, () => "at least one IR component must be given");
         Utils.QL_REQUIRE(nIrLgm1f_ + nFxBs_ + nInfl_ + nEqBs_ == p_.Count,
                    () => "the parametrizations must be given in the following order: ir, "
                    + "fx, inflation, equity (others not supported by this class), found "
                        + nIrLgm1f_ + " ir, " + nFxBs_ + " fx, " + nInfl_ + " inflation and " + nEqBs_
                        + " equity parametrizations, but there are " + p_.Count + " parametrizations given in total");
      }




      void calibrateIrLgm1fVolatilitiesIterative(
    int ccy, List<CalibrationHelper> helpers, OptimizationMethod method,
    EndCriteria endCriteria, Constraint constraint, List<double> weights)
      {
         lgm(ccy).calibrateVolatilitiesIterative(helpers, method, endCriteria, constraint, weights);
         update();
      }

      void calibrateIrLgm1fReversionsIterative(
       int ccy, List<CalibrationHelper> helpers, OptimizationMethod method,
          EndCriteria endCriteria, Constraint constraint, List<double> weights)
      {
         lgm(ccy).calibrateReversionsIterative(helpers, method, endCriteria, constraint, weights);
         update();
      }

      void calibrateIrLgm1fGlobal(int ccy,
                                                   List<CalibrationHelper> helpers,
                                                   OptimizationMethod method, EndCriteria endCriteria,
                                                   Constraint constraint, List<double> weights)
      {
         lgm(ccy).calibrate(helpers, method, endCriteria, constraint, weights);
         update();
      }

      void calibrateBsVolatilitiesIterative(
       AssetType assetClass, int idx, List<CalibrationHelper> helpers,
          OptimizationMethod method, EndCriteria endCriteria, Constraint constraint,
         List<double> weights)
      {
         bool isFx = (assetClass == AssetType.FX);
         bool isEq = (assetClass == AssetType.EQ);
         Utils.QL_REQUIRE(isFx || isEq, () => "Unsupported AssetType for BS calibration");
         for (int i = 0; i < helpers.Count; ++i)
         {
            List<CalibrationHelper> h = new List<CalibrationHelper>();
            h.Add(helpers[i]);
            calibrate(h, method, endCriteria, constraint, weights, MoveBsVolatility(assetClass, idx, i));
         }
         update();
      }

      void calibrateBsVolatilitiesGlobal(AssetType assetType, int aIdx,
                                                         List<CalibrationHelper> helpers,
                                                          OptimizationMethod method, EndCriteria endCriteria,
                                                          Constraint constraint, List<double> weights)
      {
         bool isFx = (assetType == AssetType.FX);
         bool isEq = (assetType == AssetType.EQ);
         Utils.QL_REQUIRE(isFx || isEq, () => "Unsupported AssetType for BS calibration");
         calibrate(helpers, method, endCriteria, constraint, weights, MoveBsVolatilities(assetType, aIdx));
         update();
      }

      //      /*! returns the state process with a given discretization */
      //      const boost::shared_ptr<StochasticProcess>
      //    stateProcess(CrossAssetStateProcess::discretization disc = CrossAssetStateProcess::exact) const;

      //      /*! total dimension of model (sum of number of state variables) */
      //      Size dimension() const;

      //      /*! total number of Brownian motions (this is less or equal to dimension) */
      //      Size brownians() const;

      //      /*! total number of parameters that can be calibrated */
      //      Size totalNumberOfParameters() const;

      //      /*! number of components for an asset class */
      //      Size components(const AssetType t) const;

      //      /*! number of brownian motions for a component */
      //      Size brownians(const AssetType t, const Size i) const;

      //      /*! number of state variables for a component */
      //      Size stateVariables(const AssetType t, const Size i) const;

      //      /*! return index for currency (0 = domestic, 1 = first
      //        foreign currency and so on) */
      //      Size ccyIndex(const Currency& ccy) const;

      //      /*! return index for equity (0 = first equity) */
      //      Size eqIndex(const std::string& eqName) const;

      //      /*! observer and linked calibrated model interface */
      //      void update();
      //      void generateArguments();

      //      /*! LGM1F components, ccy=0 refers to the domestic currency */
      //      const boost::shared_ptr<LinearGaussMarkovModel> lgm(const Size ccy) const;

      //      const boost::shared_ptr<IrLgm1fParametrization> irlgm1f(const Size ccy) const;

      //      Real numeraire(const Size ccy, const Time t, const Real x,
      //                     Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>()) const;

      //      Real discountBond(const Size ccy, const Time t, const Time T, const Real x,
      //                        Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>()) const;

      //      Real reducedDiscountBond(const Size ccy, const Time t, const Time T, const Real x,
      //                               Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>()) const;

      //      Real discountBondOption(const Size ccy, Option::Type type, const Real K, const Time t, const Time S, const Time T,
      //                              Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>()) const;

      //      /*! FXBS components, ccy=0 referes to the first foreign currency,
      //          so it corresponds to ccy+1 if you want to get the corresponding
      //          irmgl1f component */
      //      const boost::shared_ptr<FxBsParametrization> fxbs(const Size ccy) const;

      //      /*! EQBS components */
      //      const boost::shared_ptr<EqBsParametrization> eqbs(const Size ccy) const;

      //      /* ... add more components here ...*/

      //      /*! correlation linking the different marginal models, note that
      //          the use of asset class pairs specific inspectors is
      //          recommended instead of the global matrix directly */
      //      const Matrix& correlation() const;

      //      /*! check if correlation matrix is valid */
      //      void checkCorrelationMatrix() const;

      //      /*! index of component in the parametrization vector */
      //      Size idx(const AssetType t, const Size i) const;

      //      /*! index of component in the correlation matrix, by offset */
      //      Size cIdx(const AssetType t, const Size i, const Size offset = 0) const;

      //      /*! index of component in the stochastic process array, by offset */
      //      Size pIdx(const AssetType t, const Size i, const Size offset = 0) const;

      //      /*! correlation between two components */
      //      const Real& correlation(const AssetType s, const Size i, const AssetType t, const Size j, const Size iOffset = 0,
      //                            const Size jOffset = 0) const;
      //      /*! set correlation */
      //      void correlation(const AssetType s, const Size i, const AssetType t, const Size j, const Real value,
      //                     const Size iOffset = 0, const Size jOffset = 0);

      //      /*! analytical moments require numerical integration,
      //        which can be customized here */
      //      void setIntegrationPolicy(const boost::shared_ptr<Integrator> integrator,
      //                              const bool usePiecewiseIntegration = true) const;
      //      const boost::shared_ptr<Integrator> integrator() const;

      //      /*! calibration procedures */

      //      /*! calibrate irlgm1f volatilities to a sequence of ir options with
      //          expiry times equal to step times in the parametrization */
      //      void calibrateIrLgm1fVolatilitiesIterative(const Size ccy,
      //                                               const std::vector<boost::shared_ptr<CalibrationHelper>>& helpers,
      //                                               OptimizationMethod& method, const EndCriteria& endCriteria,
      //                                               const Constraint& constraint = Constraint(),
      //                                               const std::vector<Real>& weights = std::vector<Real>());

      //    /*! calibrate irlgm1f reversion to a sequence of ir options with
      //        maturities equal to step times in the parametrization */
      //    void calibrateIrLgm1fReversionsIterative(const Size ccy,
      //                                             const std::vector<boost::shared_ptr<CalibrationHelper>>& helpers,
      //                                             OptimizationMethod& method, const EndCriteria& endCriteria,
      //                                             const Constraint& constraint = Constraint(),
      //                                             const std::vector<Real>& weights = std::vector<Real>());

      //    /*! calibrate irlgm1f parameters for one ccy globally to a set
      //        of ir options */
      //    void calibrateIrLgm1fGlobal(const Size ccy, const std::vector<boost::shared_ptr<CalibrationHelper>>& helpers,
      //                                OptimizationMethod& method, const EndCriteria& endCriteria,
      //                                const Constraint& constraint = Constraint(),
      //                                const std::vector<Real>& weights = std::vector<Real>());

      //    /*! calibrate eq or fx volatilities to a sequence of options with
      //            expiry times equal to step times in the parametrization */
      //    void calibrateBsVolatilitiesIterative(const AssetType& assetType, const Size aIdx,
      //                                          const std::vector<boost::shared_ptr<CalibrationHelper>>& helpers,
      //                                          OptimizationMethod& method, const EndCriteria& endCriteria,
      //                                          const Constraint& constraint = Constraint(),
      //                                          const std::vector<Real>& weights = std::vector<Real>());

      //    /*! calibrate eq/fx volatilities globally to a set of fx options */
      //    void calibrateBsVolatilitiesGlobal(const AssetType& assetType, const Size aIdx,
      //                                       const std::vector<boost::shared_ptr<CalibrationHelper>>& helpers,
      //                                       OptimizationMethod& method, const EndCriteria& endCriteria,
      //                                       const Constraint& constraint = Constraint(),
      //                                       const std::vector<Real>& weights = std::vector<Real>());

      //    /* ... add more calibration procedures here ... */

      //protected:
      //    /* ctor to be used in extensions, initialize is not called */
      //    CrossAssetModel(const std::vector<boost::shared_ptr<Parametrization>>& parametrizations, const Matrix& correlation,
      //                    SalvagingAlgorithm::Type salvaging, const bool)
      //        : LinkableCalibratedModel(), p_(parametrizations), rho_(correlation), salvaging_(salvaging) { }

      //      /*! number of arguments for a component */
      //      Size arguments(const AssetType t, const Size i) const;

      //      /*! index of component in the arguments vector, by offset */
      //      Size aIdx(const AssetType t, const Size i, const Size offset = 0) const;

      //      /* init methods */
      //      virtual void initialize();
      //      virtual void initializeParametrizations();
      //      virtual void initializeCorrelation();
      //      virtual void initializeArguments();
      //      virtual void finalizeArguments();
      //      virtual void checkModelConsistency() const;
      //      virtual void initDefaultIntegrator();
      //      virtual void initStateProcess();

      //      /* members */

      //      Size nIrLgm1f_, nFxBs_, nEqBs_, nInfl_;
      //      Size totalNumberOfParameters_;
      //      std::vector<boost::shared_ptr<Parametrization>> p_;
      //      std::vector<boost::shared_ptr<LinearGaussMarkovModel>> lgm_;
      //      Matrix rho_;
      //      SalvagingAlgorithm::Type salvaging_;
      //      mutable boost::shared_ptr<Integrator> integrator_;
      //      boost::shared_ptr<CrossAssetStateProcess> stateProcessExact_, stateProcessEuler_;

      //      /* calibration constraints */

      List<bool> MoveBsVolatility(AssetType assetClass, int aIdx, int tIdx)
      {
         bool isFx = (assetClass == AssetType.FX);
         bool isEq = (assetClass == AssetType.EQ);
         Utils.QL_REQUIRE(isFx || isEq, () => "Invalid AssetType for MoveBsVolatility");
         string assetStr = isFx ? "FX" : "EQ";
         int volGridSize = 0;
         if (isFx)
            volGridSize = fxbs(aIdx).parameter(0).size();
         else
            volGridSize = eqbs(aIdx).parameter(0).size();
         Utils.QL_REQUIRE(tIdx < volGridSize, () => "bs volatility index (" + tIdx + ") for " + assetStr + " asset " + aIdx
                                                                  + " out of bounds 0..." + (volGridSize - 1));
         List<bool> res = new List<bool>();

         for (int j = 0; j < nIrLgm1f_; ++j)
         {

            List<bool> tmp1 = Enumerable.Repeat(true, p_[idx(AssetType.IR, j)].parameter(0).size()).ToList();
            List<bool> tmp2 = Enumerable.Repeat(true, p_[idx(AssetType.IR, j)].parameter(1).size()).ToList();


            res.AddRange(tmp1);
            res.AddRange(tmp2);



         }
         for (int j = 0; j < nFxBs_; ++j)
         {
            //        std::vector<bool> tmp(p_[idx(FX, j)]->parameter(0)->size(), true);
            List<bool> tmp = Enumerable.Repeat(true, p_[idx(AssetType.FX, j)].parameter(0).size()).ToList();
            if (isFx && aIdx == j)
            {
               tmp[tIdx] = false;
            }
            res.AddRange(tmp);

         }
         for (int j = 0; j < nEqBs_; ++j)
         {
            List<bool> tmp = Enumerable.Repeat(true, p_[idx(AssetType.EQ, j)].parameter(0).size()).ToList();
            if (isEq && aIdx == j)
            {
               tmp[tIdx] = false;
            }
            res.AddRange(tmp);
         }
         return res;
      }

      List<bool> MoveBsVolatilities(AssetType assetClass, int aIdx)
      {
         bool isFx = (assetClass == AssetType.FX);
         bool isEq = (assetClass == AssetType.EQ);
         Utils.QL_REQUIRE(isFx || isEq, () => "Invalid AssetType for MoveBsVolatility");
         string assetStr = isFx ? "FX" : "EQ";
         List<bool> res = new List<bool>();

         for (int j = 0; j < nIrLgm1f_; ++j)
         {
            List<bool> tmp1 = Enumerable.Repeat(true, p_[idx(AssetType.IR, j)].parameter(0).size()).ToList();
            List<bool> tmp2 = Enumerable.Repeat(true, p_[idx(AssetType.IR, j)].parameter(1).size()).ToList();


            res.AddRange(tmp1);
            res.AddRange(tmp2);


         }
         for (int j = 0; j < nFxBs_; ++j)
         {
            bool fixFlag = !(isFx && aIdx == j);
            List<bool> tmp = Enumerable.Repeat(fixFlag, p_[idx(AssetType.FX, j)].parameter(0).size()).ToList();
            res.AddRange(tmp);

         }

         for (int j = 0; j < nEqBs_; ++j)
         {
            bool fixFlag = !(isEq && aIdx == j);
            List<bool> tmp = Enumerable.Repeat(fixFlag, p_[idx(AssetType.EQ, j)].parameter(0).size()).ToList();
            res.AddRange(tmp);
         }
         return res;
      }




   }

   //// inline

   //inline const boost::shared_ptr<StochasticProcess>
   //CrossAssetModel::stateProcess(CrossAssetStateProcess::discretization disc) const {





}
