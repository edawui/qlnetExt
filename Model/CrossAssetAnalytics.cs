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

namespace QLNetExt.CrossAssetAnalytics
{



   public partial class Utils
   {




      /*! CrossAssetModelTypes.AssetType.IR H component */

      public struct Hz:IE
   {
      public Hz(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.irlgm1f(i_).H(t);
      }

      public static Hz Helper(int i)
      {
         return new Hz(i);
      }
      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.IR alpha component */
   public struct az:IE
   {
      public az(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.irlgm1f(i_).alpha(t);
      }

      public static az Helper(int i)
      {
         return new az(i);
      }

      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.IR zeta component */
   public class zetaz:IE
   {
      public zetaz(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.irlgm1f(i_).zeta(t);
      }

      public static zetaz  Helper(int i)
      {
         return new zetaz(i);
      }
      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.FX sigma component */
   public struct sx : IE
      {
      public sx(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.fxbs(i_).sigma(t);
      }


      public static sx Helper(int i)
      {
         return new sx(i);
      }

      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.FX variance component */
   public struct vx : IE
      {
      public vx(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.fxbs(i_).variance(t);
      }


      public static vx Helper(int i)
      {
         return new vx(i);
      }
      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.IR-CrossAssetModelTypes.AssetType.IR correlation component */
   public struct rzz : IE
      {
      public rzz(int i, int j) { i_ = i; j_ = j; }
      public double eval(CrossAssetModel x, double d )
      {
         return x.correlation(CrossAssetModelTypes.AssetType.IR, i_, CrossAssetModelTypes.AssetType.IR, j_, 0, 0);
      }


      public static rzz Helper(int i,int j)
      {
         return new rzz(i,j);
      }
      int i_, j_;
   }

   /*! CrossAssetModelTypes.AssetType.IR-CrossAssetModelTypes.AssetType.FX correlation component */
   public struct rzx : IE
      {
      public rzx(int i, int j)
      { i_ = i; j_ = j; }
      public double eval(CrossAssetModel x, double d)
      {
         return x.correlation(CrossAssetModelTypes.AssetType.IR, i_, CrossAssetModelTypes.AssetType.FX, j_, 0, 0);
      }

      public static rzx Helper(int i, int j)
      {
         return new rzx(i,j);
      }

      int i_, j_;
   }

   /*! CrossAssetModelTypes.AssetType.FX-CrossAssetModelTypes.AssetType.FX correlation component */
   public struct rxx : IE
      {
      public rxx(int i, int j)
      {
         i_ = i; j_ = j;
      }
      public double eval(CrossAssetModel x, double d)
      {
         return x.correlation(CrossAssetModelTypes.AssetType.FX, i_, CrossAssetModelTypes.AssetType.FX, j_, 0, 0);
      }
      int i_, j_;


      public static rxx Helper(int i, int j)
      {
         return new rxx(i, j);
      }
   }

   /*! CrossAssetModelTypes.AssetType.EQ sigma component */
   public struct ss : IE
      {
      public ss(int i)
      {
         i_ = i;
      }
      public double eval(CrossAssetModel x, double t)
      {
         return x.eqbs(i_).sigma(t);
      }

      public static ss Helper(int i)
      {
         return new ss(i);
      }
      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.FX variance component */
   public struct vs : IE
      {
      public vs(int i) { i_ = i; }
      public double eval(CrossAssetModel x, double t)
      {
         return x.eqbs(i_).variance(t);
      }


      public static vs Helper(int i)
      {
         return new vs(i);
      }
      int i_;
   }

   /*! CrossAssetModelTypes.AssetType.EQ-CrossAssetModelTypes.AssetType.EQ correlation component */
   public struct rss : IE
      {
      public rss(int i, int j) { i_ = i; j_ = j; }
      public double eval(CrossAssetModel x, double d)
      {
         return x.correlation(CrossAssetModelTypes.AssetType.EQ, i_, CrossAssetModelTypes.AssetType.EQ, j_, 0, 0);
      }

      public static rss Helper(int i, int j)
      {
         return new rss(i, j);
      }
      int i_, j_;
   };

   /*! CrossAssetModelTypes.AssetType.IR-CrossAssetModelTypes.AssetType.EQ correlation component */
   public struct rzs : IE
      {
      public rzs(int i, int j) { i_ = i; j_ = j; }
      public double eval(CrossAssetModel x, double d)
      {
         return x.correlation(CrossAssetModelTypes.AssetType.IR, i_, CrossAssetModelTypes.AssetType.EQ, j_, 0, 0);
      }


      public static rzs Helper(int i, int j)
      {
         return new rzs(i, j);
      }

      int i_, j_;
   }

   /*! CrossAssetModelTypes.AssetType.FX-CrossAssetModelTypes.AssetType.EQ correlation component */
   public struct rxs : IE
      {
      public rxs(int i, int j) { i_ = i; j_ = j; }
      public double eval(CrossAssetModel x, double d)
      {
         return x.correlation(CrossAssetModelTypes.AssetType.FX, i_, CrossAssetModelTypes.AssetType.EQ, j_, 0, 0);
      }

      public static rxs Helper(int i, int j)
      {
         return new rxs(i, j);
      }

      int i_, j_;
   }

      public static double ir_expectation_1(CrossAssetModel x, int i, double t0, double dt)
      {
         double res = 0.0;
         if (i > 0)
         {
            res += -integral(x, P(Hz.Helper(i), az.Helper(i), az.Helper(i)), t0, t0 + dt) -
                   integral(x, P(az.Helper(i), sx.Helper(i - 1), rzx.Helper(i, i - 1)), t0, t0 + dt) +
                   integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(i), rzz.Helper(0, i)), t0, t0 + dt);
         }
         return res;
      }

      public static double ir_expectation_2(CrossAssetModel crossAssetModel, int i, double zi_0) { return zi_0; }

      public static double fx_expectation_1(CrossAssetModel x, int i, double t0, double dt)
      {
         double H0_a = Hz.Helper(0).eval(x, t0);
         double Hi_a = Hz.Helper(i + 1).eval(x, t0);
         double H0_b = Hz.Helper(0).eval(x, t0 + dt);
         double Hi_b = Hz.Helper(i + 1).eval(x, t0 + dt);
         double zeta0_a = zetaz.Helper(0).eval(x, t0);
         double zetai_a = zetaz.Helper(i + 1).eval(x, t0);
         double zeta0_b = zetaz.Helper(0).eval(x, t0 + dt);
         double zetai_b = zetaz.Helper(i + 1).eval(x, t0 + dt);
         double res = System.Math.Log(
             x.irlgm1f(i + 1).termStructure().currentLink().discount(t0 + dt) / x.irlgm1f(i + 1).termStructure().currentLink().discount(t0) *
             x.irlgm1f(0).termStructure().currentLink().discount(t0) / x.irlgm1f(0).termStructure().currentLink().discount(t0 + dt));
         res -= 0.5 * (vx.Helper(i).eval(x, t0 + dt) - vx.Helper(i).eval(x, t0));
         res +=
             0.5 * (H0_b * H0_b * zeta0_b - H0_a * H0_a * zeta0_a - integral(x, P(Hz.Helper(0), Hz.Helper(0), az.Helper(0), az.Helper(0)), t0, t0 + dt));
         res -= 0.5 * (Hi_b * Hi_b * zetai_b - Hi_a * Hi_a * zetai_a -
                       integral(x, P(Hz.Helper(i + 1), Hz.Helper(i + 1), az.Helper(i + 1), az.Helper(i + 1)), t0, t0 + dt));
         res += integral(x, P(Hz.Helper(0), az.Helper(0), sx.Helper(i), rzx.Helper(0, i)), t0, t0 + dt);
         res -= Hi_b * (-integral(x, P(Hz.Helper(i + 1), az.Helper(i + 1), az.Helper(i + 1)), t0, t0 + dt) +
                        integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(i + 1), rzz.Helper(0, i + 1)), t0, t0 + dt) -
                        integral(x, P(az.Helper(i + 1), sx.Helper(i), rzx.Helper(i + 1, i)), t0, t0 + dt));
         res += -integral(x, P(Hz.Helper(i + 1), Hz.Helper(i + 1), az.Helper(i + 1), az.Helper(i + 1)), t0, t0 + dt) +
                integral(x, P(Hz.Helper(0), Hz.Helper(i + 1), az.Helper(0), az.Helper(i + 1), rzz.Helper(0, i + 1)), t0, t0 + dt) -
                integral(x, P(Hz.Helper(i + 1), az.Helper(i + 1), sx.Helper(i), rzx.Helper(i + 1, i)), t0, t0 + dt);
         return res;
      }

      public static double fx_expectation_2(CrossAssetModel x, int i, double t0, double xi_0, double zi_0,
                                double z0_0, double dt)
      {
         double res = xi_0 + (Hz.Helper(0).eval(x, t0 + dt) - Hz.Helper(0).eval(x, t0)) * z0_0 -
                    (Hz.Helper(i + 1).eval(x, t0 + dt) - Hz.Helper(i + 1).eval(x, t0)) * zi_0;
         return res;
      }

     public static double ir_ir_covariance(CrossAssetModel x, int i, int j, double t0, double dt)
      {
         double res = integral(x, P(az.Helper(i), az.Helper(j), rzz.Helper(i, j)), t0, t0 + dt);
         return res;
      }

     public static double ir_fx_covariance(CrossAssetModel x, int i, int j, double t0, double dt)
      {
         double res = Hz.Helper(0).eval(x, t0 + dt) * integral(x, P(az.Helper(0), az.Helper(i), rzz.Helper(0, i)), t0, t0 + dt) -
                    integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(i), rzz.Helper(0, i)), t0, t0 + dt) -
                    Hz.Helper(j + 1).eval(x, t0 + dt) * integral(x, P(az.Helper(j + 1), az.Helper(i), rzz.Helper(j + 1, i)), t0, t0 + dt) +
                    integral(x, P(Hz.Helper(j + 1), az.Helper(j + 1), az.Helper(i), rzz.Helper(j + 1, i)), t0, t0 + dt) +
                    integral(x, P(az.Helper(i), sx.Helper(j), rzx.Helper(i, j)), t0, t0 + dt);
         return res;
      }

      public static double fx_fx_covariance(CrossAssetModel x, int i, int j, double t0, double dt)
      {
         double H0 = Hz.Helper(0).eval(x, t0 + dt);
         double Hi = Hz.Helper(i + 1).eval(x, t0 + dt);
         double Hj = Hz.Helper(j + 1).eval(x, t0 + dt);
         double res =
             // row 1
             H0 * H0 * (zetaz.Helper(0).eval(x, t0 + dt) - zetaz.Helper(0).eval(x, t0)) -
             2.0 * H0 * integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(0)), t0, t0 + dt) +
             integral(x, P(Hz.Helper(0), Hz.Helper(0), az.Helper(0), az.Helper(0)), t0, t0 + dt) -
             // row 2
             H0 * Hj * integral(x, P(az.Helper(0), az.Helper(j + 1), rzz.Helper(0, j + 1)), t0, t0 + dt) +
             Hj * integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(j + 1), rzz.Helper(0, j + 1)), t0, t0 + dt) +
             H0 * integral(x, P(Hz.Helper(j + 1), az.Helper(j + 1), az.Helper(0), rzz.Helper(j + 1, 0)), t0, t0 + dt) -
             integral(x, P(Hz.Helper(0), Hz.Helper(j + 1), az.Helper(0), az.Helper(j + 1), rzz.Helper(0, j + 1)), t0, t0 + dt) -
             // row 3
             H0 * Hi * integral(x, P(az.Helper(0), az.Helper(i + 1), rzz.Helper(0, i + 1)), t0, t0 + dt) +
             Hi * integral(x, P(Hz.Helper(0), az.Helper(0), az.Helper(i + 1), rzz.Helper(0, i + 1)), t0, t0 + dt) +
             H0 * integral(x, P(Hz.Helper(i + 1), az.Helper(i + 1), az.Helper(0), rzz.Helper(i + 1, 0)), t0, t0 + dt) -
             integral(x, P(Hz.Helper(0), Hz.Helper(i + 1), az.Helper(0), az.Helper(i + 1), rzz.Helper(0, i + 1)), t0, t0 + dt) +
             // row 4
             H0 * integral(x, P(az.Helper(0), sx.Helper(j), rzx.Helper(0, j)), t0, t0 + dt) -
             integral(x, P(Hz.Helper(0), az.Helper(0), sx.Helper(j), rzx.Helper(0, j)), t0, t0 + dt) +
             // row 5
             H0 * integral(x, P(az.Helper(0), sx.Helper(i), rzx.Helper(0, i)), t0, t0 + dt) -
             integral(x, P(Hz.Helper(0), az.Helper(0), sx.Helper(i), rzx.Helper(0, i)), t0, t0 + dt) -
             // row 6
             Hi * integral(x, P(az.Helper(i + 1), sx.Helper(j), rzx.Helper(i + 1, j)), t0, t0 + dt) +
             integral(x, P(Hz.Helper(i + 1), az.Helper(i + 1), sx.Helper(j), rzx.Helper(i + 1, j)), t0, t0 + dt) -
             // row 7
             Hj * integral(x, P(az.Helper(j + 1), sx.Helper(i), rzx.Helper(j + 1, i)), t0, t0 + dt) +
             integral(x, P(Hz.Helper(j + 1), az.Helper(j + 1), sx.Helper(i), rzx.Helper(j + 1, i)), t0, t0 + dt) +
             // row 8
             Hi * Hj * integral(x, P(az.Helper(i + 1), az.Helper(j + 1), rzz.Helper(i + 1, j + 1)), t0, t0 + dt) -
             Hj * integral(x, P(Hz.Helper(i + 1), az.Helper(i + 1), az.Helper(j + 1), rzz.Helper(i + 1, j + 1)), t0, t0 + dt) -
             Hi * integral(x, P(Hz.Helper(j + 1), az.Helper(j + 1), az.Helper(i + 1), rzz.Helper(j + 1, i + 1)), t0, t0 + dt) +
             integral(x, P(Hz.Helper(i + 1), Hz.Helper(j + 1), az.Helper(i + 1), az.Helper(j + 1), rzz.Helper(i + 1, j + 1)), t0, t0 + dt) +
             // row 9
             integral(x, P(sx.Helper(i), sx.Helper(j), rxx.Helper(i, j)), t0, t0 + dt);
         return res;
      }

      public static double ir_eq_covariance(CrossAssetModel x, int j, int k, double t0, double dt)
      {
         int i = x.ccyIndex(x.eqbs(k).currency()); // the equity underlying currency
         double Hi_b = Hz.Helper(i).eval(x, t0 + dt);
         double res = Hi_b * integral(x, P(rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(i), rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         res += integral(x, P(rzs.Helper(j, k), az.Helper(j), ss.Helper(k)), t0, t0 + dt);
         return res;
      }

      public static double fx_eq_covariance(CrossAssetModel x, int j, int k, double t0, double dt)
      {
         int i = x.ccyIndex(x.eqbs(k).currency()); // the equity underlying currency
         int j_lgm = j + 1;                    // indexing of the FX currency for extracting the LGM terms
         double Hi_b = Hz.Helper(i).eval(x, t0 + dt);
         double Hj_b = Hz.Helper(j_lgm).eval(x, t0 + dt);
         double H0_b = Hz.Helper(0).eval(x, t0 + dt);
         double res = 0.0;
         res += Hi_b * H0_b * integral(x, P(rzz.Helper(0, i), az.Helper(0), az.Helper(i)), t0, t0 + dt);
         res -= Hi_b * integral(x, P(Hz.Helper(0), rzz.Helper(0, i), az.Helper(0), az.Helper(i)), t0, t0 + dt);
         res -= H0_b * integral(x, P(Hz.Helper(i), rzz.Helper(0, i), az.Helper(0), az.Helper(i)), t0, t0 + dt);
         res += integral(x, P(Hz.Helper(0), Hz.Helper(i), rzz.Helper(0, i), az.Helper(0), az.Helper(i)), t0, t0 + dt);

         res -= Hi_b * Hj_b * integral(x, P(rzz.Helper(j_lgm, i), az.Helper(j_lgm), az.Helper(i)), t0, t0 + dt);
         res += Hi_b * integral(x, P(Hz.Helper(j_lgm), rzz.Helper(j_lgm, i), az.Helper(j_lgm), az.Helper(i)), t0, t0 + dt);
         res += Hj_b * integral(x, P(Hz.Helper(i), rzz.Helper(j_lgm, i), az.Helper(j_lgm), az.Helper(i)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(j_lgm), Hz.Helper(i), rzz.Helper(j_lgm, i), az.Helper(j_lgm), az.Helper(i)), t0, t0 + dt);

         res += Hi_b * integral(x, P(rzx.Helper(i, j), sx.Helper(j), az.Helper(i)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(i), rzx.Helper(i, j), sx.Helper(j), az.Helper(i)), t0, t0 + dt);

         res += H0_b * integral(x, P(rzs.Helper(0, k), az.Helper(0), ss.Helper(k)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(0), rzs.Helper(0, k), az.Helper(0), ss.Helper(k)), t0, t0 + dt);

         res -= Hj_b * integral(x, P(rzs.Helper(j_lgm, k), az.Helper(j_lgm), ss.Helper(k)), t0, t0 + dt);
         res += integral(x, P(Hz.Helper(j_lgm), rzs.Helper(j_lgm, k), az.Helper(j_lgm), ss.Helper(k)), t0, t0 + dt);

         res += integral(x, P(rxs.Helper(j, k), sx.Helper(j), ss.Helper(k)), t0, t0 + dt);

         return res;
      }

      public static double eq_eq_covariance(CrossAssetModel x, int k, int l, double t0, double dt)
      {
         int i = x.ccyIndex(x.eqbs(k).currency()); // ccy underlying equity k
         int j = x.ccyIndex(x.eqbs(l).currency()); // ccy underlying equity l
         double Hi_b = Hz.Helper(i).eval(x, t0 + dt);
         double Hj_b = Hz.Helper(j).eval(x, t0 + dt);
         double res = integral(x, P(rss.Helper(k, l), ss.Helper(k), ss.Helper(l)), t0, t0 + dt);
         res += Hj_b * integral(x, P(rzs.Helper(j, k), az.Helper(j), ss.Helper(k)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(j), rzs.Helper(j, k), az.Helper(j), ss.Helper(k)), t0, t0 + dt);
         res += Hi_b * integral(x, P(rzs.Helper(i, l), az.Helper(i), ss.Helper(l)), t0, t0 + dt);
         res -= integral(x, P(Hz.Helper(i), rzs.Helper(i, l), az.Helper(i), ss.Helper(l)), t0, t0 + dt);
         res += Hi_b * Hj_b * integral(x, P(rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         res -= Hi_b * integral(x, P(Hz.Helper(j), rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         res -= Hj_b * integral(x, P(Hz.Helper(i), rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         res += integral(x, P(Hz.Helper(i), Hz.Helper(j), rzz.Helper(i, j), az.Helper(i), az.Helper(j)), t0, t0 + dt);
         return res;
      }

      public static double eq_expectation_1(CrossAssetModel x, int k, double t0, double dt)
      {
         int i = x.ccyIndex(x.eqbs(k).currency());
         int eps_i = (i == 0) ? 0 : 1;
         double Hi_a = Hz.Helper(i).eval(x, t0);
         double Hi_b = Hz.Helper(i).eval(x, t0 + dt);
         double zetai_a = zetaz.Helper(i).eval(x, t0);
         double zetai_b = zetaz.Helper(i).eval(x, t0 + dt);
         double res =
             System.Math.Log(x.eqbs(k).equityDivYieldCurveToday().currentLink().discount(t0 + dt) /
                      x.eqbs(k).equityDivYieldCurveToday().currentLink().discount(t0) * x.eqbs(k).equityIrCurveToday().currentLink().discount(t0) /
                      x.eqbs(k).equityIrCurveToday().currentLink().discount(t0 + dt));
         res -= 0.5 * (vs.Helper(k).eval(x, t0 + dt) - vs.Helper(k).eval(x, t0));
         res +=
             0.5 * (Hi_b * Hi_b * zetai_b - Hi_a * Hi_a * zetai_a - integral(x, P(Hz.Helper(i), Hz.Helper(i), az.Helper(i), az.Helper(i)), t0, t0 + dt));
         res += integral(x, P(rzs.Helper(0, k), Hz.Helper(0), az.Helper(0), ss.Helper(k)), t0, t0 + dt);
         if (eps_i > 0)
         {
            res -= integral(x, P(rxs.Helper(i - 1, k), sx.Helper(i - 1), ss.Helper(k)), t0, t0 + dt);
         }
         // expand gamma term
         if (eps_i > 0)
         {
            res += Hi_b * (-integral(x, P(Hz.Helper(i), az.Helper(i), az.Helper(i)), t0, t0 + dt) -
                           integral(x, P(rzx.Helper(i, i - 1), sx.Helper(i - 1), az.Helper(i)), t0, t0 + dt) +
                           integral(x, P(rzz.Helper(0, i), az.Helper(i), az.Helper(0), Hz.Helper(0)), t0, t0 + dt));
            res -= (-integral(x, P(Hz.Helper(i), Hz.Helper(i), az.Helper(i), az.Helper(i)), t0, t0 + dt) -
                    integral(x, P(Hz.Helper(i), rzx.Helper(i, i - 1), sx.Helper(i - 1), az.Helper(i)), t0, t0 + dt) +
                    integral(x, P(Hz.Helper(i), rzz.Helper(0, i), az.Helper(i), az.Helper(0), Hz.Helper(0)), t0, t0 + dt));
         }
         return res;
      }

      public static double eq_expectation_2(CrossAssetModel x, int k, double t0, double sk_0, double zi_0,
                                double dt)
      {
         int i = x.ccyIndex(x.eqbs(k).currency());
         double Hi_a = Hz.Helper(i).eval(x, t0);
         double Hi_b = Hz.Helper(i).eval(x, t0 + dt);
         double res = sk_0 + (Hi_b - Hi_a) * zi_0;
         return res;
      }

   } // namesapce crossassetanalytics
} // namespace QuantExt
