//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;

using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Tree;

using MapSurfer.Styling.Formats.CartoCSS.Parser.Tree;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  internal static class ConvertUtility
  {
    public static string ToFilter(CartoFilterSet filterSet)
    {
      string result = string.Empty;

      if (filterSet.Count > 0)
      {
        int i = 0;
        foreach (CartoFilterElement filter in filterSet.Filters)
        {
          if (i != 0)
            result += " and ";

          result += "[" + filter.Key + "]" + ToExpressionOperator(filter.Op) + ToExpressionValue(filter.Value);
          i++;
        }
      }

      return result;
    }

    private static string ToExpressionOperator(Node op)
    {
      string strOp = op.ToString();

      switch (strOp)
      {
        case "=":
          return "=";
        case "!=":
          return "!=";
        case ">=":
          return ">=";
        case "<=":
          return "<=";
        case ">":
          return ">";
        case "<":
          return "<";
        default:
          return strOp;
      }
    }

    private static string ToExpressionValue(Node value)
    {
      string result = null;

      Number num = value as Number;
      if (num != null)
        result = num.Value.ToString();
      else
      {
        Quoted quoted = value as Quoted;
        if (quoted != null)
          result = QuoteValue(quoted.Value.ToString());
      }
      return result;
    }

    public static string QuoteValue(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      if (value.StartsWith("[") && value.EndsWith("]"))
        return value;
      else
      if (value.StartsWith("\"") && value.EndsWith("\""))
        return value;
      else
        return "\"" + value + "\"";
    }

    public static double ToScaleDenominator(int zoom)
    {
      switch (zoom)
      {
        case 0: return 1000000000;
        case 1: return 500000000;
        case 2: return 200000000;
        case 3: return 100000000;
        case 4: return 50000000;
        case 5: return 25000000;
        case 6: return 12500000;
        case 7: return 6500000;
        case 8: return 3000000;
        case 9: return 1500000;
        case 10: return 750000;
        case 11: return 400000;
        case 12: return 200000;
        case 13: return 100000;
        case 14: return 50000;
        case 15: return 25000;
        case 16: return 12500;
        case 17: return 5000;
        case 18: return 2500;
        case 19: return 1500;
        case 20: return 750;
        case 21: return 500;
        case 22: return 250;
        case 23: return 100;
      }

      return 0;
    }

    public static float[] ToFloatArray(string value)
    {
      string[] strValues = value.Split(',');
      int n = strValues.Length;
      float[] values = new float[n];

      for (int i = 0; i < n; i++)
      {
        values[i] = System.Convert.ToSingle(strValues[i]);
      }

      return values;
    }
  }
}
