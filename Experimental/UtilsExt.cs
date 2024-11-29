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

using QLNet;

namespace QLNetExt
{
   public class UtilsExt
    {


      public static bool IsIncreasingMontonically<T>(List<T> list)
    where T : IComparable
      {
         return list.Zip(list.Skip(1), (a, b) => a.CompareTo(b) <= 0)
             .All(b => b);
      }

      /// <summary>
      /// https://stackoverflow.com/questions/6422816/creating-good-hash-codes-for-net-ala-boost-functional-hash
      /// </summary>
      internal static class HashHelper
      {
         private static int InitialHash = 17; // Prime number
         private static int Multiplier = 23; // Different prime number

         public static Int32 GetHashCode(params object[] values)
         {
            unchecked // overflow is fine
            {
               int hash = InitialHash;

               if (values != null)
                  for (int i = 0; i < values.Length; i++)
                  {
                     object currentValue = values[i];
                     hash = hash * Multiplier
                         + (currentValue != null ? currentValue.GetHashCode() : 0);
                  }

               return hash;
            }
         }
      }


      //  https://stackoverflow.com/questions/1300088/distinct-with-lambda

      public class EqualityFactory
      {
         private sealed class Impl<T> : IEqualityComparer<T>
         {
            private Func<T, T, bool> m_del;
            private IEqualityComparer<T> m_comp;
            public Impl(Func<T, T, bool> del)
            {
               m_del = del;
               m_comp = EqualityComparer<T>.Default;
            }
            public bool Equals(T left, T right)
            {
               return m_del(left, right);
            }
            public int GetHashCode(T value)
            {
               return m_comp.GetHashCode(value);
            }
         }
         public static IEqualityComparer<T> Create<T>(Func<T, T, bool> del)
         {
            return new Impl<T>(del);
         }
      }

      //public static int NextIndexReset<T>(List<T> inList , T t)        
      public static int UpperBound<T>(List<T> inList, T t)
      {
         //Size LfmHullWhiteParameterization::nextIndexReset(Time t) const {
         //   return std::upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
         //            - fixingTimes_.begin();
         //}

         int result = inList.BinarySearch(t);
         if (result < 0)
            // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
            // without violating the sequence's ordering
            // if BinarySearch does not find value the value, the index of the next larger item is returned
            result = ~result - 1;

         // impose limits. we need the one before last at max or the first at min
         result = Math.Max(Math.Min(result, inList.Count - 2), 0);
         return result + 1;

      }

      public static int LowerBound<T>(List<T> inList, T t)
      {
         ////Size LfmHullWhiteParameterization::nextIndexReset(Time t) const {
         ////   return std::upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
         ////            - fixingTimes_.begin();
         ////}

         //int result = inList.BinarySearch(t);
         //if (result < 0)
         //   // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
         //   // without violating the sequence's ordering
         //   // if BinarySearch does not find value the value, the index of the next larger item is returned
         //   result = ~result - 1;

         //// impose limits. we need the one before last at max or the first at min
         //result = Math.Max(Math.Min(result, inList.Count - 2), 0);
         //return result + 1;

         return 0;
      }


      //public static int UpperBound(List<double> inList , double t)
      public static int UpperBoundVector(Vector inList , double t)
      {
         //TODO Debug this

         /*
          *
          * return std::upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
                          - fixingTimes_.begin();
                          */

         int result = inList.FindIndex(x => x>t);
            if (result < 0)
                result = ~result - 1;
            // impose limits. we need the one before last at max or the first at min
            result = Math.Max(Math.Min(result, inList.Count - 1),0);
            return result;


      }

      public static int LowerBoundVector(Vector inList, double t)
      {
         //TODO Debug this

         /*
          *
          * return std::upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
                          - fixingTimes_.begin();
                          */
         /*
                  int result = inList.FindIndex(x => x > t);
                  if (result < 0)
                     result = ~result - 1;
                  // impose limits. we need the one before last at max or the first at min
                  result = Math.Max(Math.Min(result, inList.Count - 1), 0);
                  return result;

                  */
         return 0;
      }

      public static double QL_PIECEWISE_FUNCTION(Vector X, Vector Y, double x )
{
         //todo
throw new NotImplementedException();
//List<double> test = new List<double>();
//test.Distinct
return 0.0;

}
}
}
