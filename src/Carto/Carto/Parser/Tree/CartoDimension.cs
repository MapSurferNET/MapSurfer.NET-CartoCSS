//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
// 
//    A C# port of the carto library written by Mapbox (https://github.com/mapbox/carto/)
//    and released under the Apache License Version 2.0.
//
//==========================================================================================
using System;
using System.Collections.Generic;

using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Tree;
using dotless.Core.Parser;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoDimension : Number, IOperable
  {
    private readonly string[] physical_units = new string[] { "m", "cm", "in", "mm", "pt", "pc" };
    private readonly string[] screen_units = new string[] { "px", "%" };
    private readonly string[] all_units = new string[] { "m", "cm", "in", "mm", "pt", "pc", "px", "%" };
    private static Dictionary<string, float> densities = null;
    private static float DPI;

    private NodeLocation m_index;

    static CartoDimension()
    {
      densities = new Dictionary<string, float>();
      densities.Add("m", 0.0254f);
      densities.Add("mm", 25.4f);
      densities.Add("cm", 2.54f);
      densities.Add("pt", 72);
      densities.Add("pc", 6);

      using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
      {
        DPI = g.DpiX;
      }
    }

    public CartoDimension(Node value, string unit, NodeLocation index) :
      base(value.ToCSS(new Env()), unit)
    {
      m_index = index;

      if ("px".Equals(unit))
        Unit = unit.Replace("px", string.Empty);
    }

    public CartoDimension(string value, string unit, NodeLocation index) :
      base(value, unit)
    {
      m_index = index;

      if ("px".Equals(unit))
        Unit = unit.Replace("px", string.Empty);
    }

    public float Round()
    {
      return (float)Math.Round(Value);
    }

    public Color ToColor()
    {
      return new Color(Value, Value, Value);
    }

    public override dotless.Core.Parser.Infrastructure.Nodes.Node Evaluate(dotless.Core.Parser.Infrastructure.Env env)
    {
      if (!string.IsNullOrEmpty(Unit) && !Contains(all_units, Unit))
      {
        env.Logger.Error("Invalid unit: '" + Unit + "'");
      }

      if (!string.IsNullOrEmpty(Unit) && Contains(physical_units, Unit))
      {
        //if (!env.ppi)

        // convert all units to inch
        // convert inch to px using ppi
        if (!"px".Equals(Unit))
        {
          Value = (Value / densities[Unit]) * DPI;
          //m_unit = "px";
        }

        Unit = "";
      }

      return this;
    }

    public new Node Operate(Operation op, Node other)
    {
      CartoDimension dim = other as CartoDimension;

      if ("%".Equals(Unit) && ("%".Equals(dim.Unit)))
      {
        //env.Logger.Error("If two operands differ, the first must not be %");
        return null;
      }

      if (!"%".Equals(Unit) && "%".Equals(dim.Unit))
      {
        if (op.Operator.Equals("*") || op.Operator.Equals("/") || op.Operator.Equals("%"))
        {
          //env.Logger.Error("Percent values can only be added or subtracted from other values");
          return null;
        }

        Operation op2 = new Operation(op.Operator, new Number(Value.ToString(), Unit), new Number((Value * dim.Value * 0.01).ToString(), Unit));

        return new CartoDimension(op2, Unit, m_index);
      }

      //here the operands are either the same (% or undefined or px), or one is undefined and the other is px

      Operation op3 = new Operation(op.Operator, new Number(Value.ToString(), Unit), new Number(dim.Value.ToString(), Unit));
      return new CartoDimension(op3, Unit ?? dim.Unit, m_index);
    }

    private bool Contains(String[] list, String value)
    {
      foreach (string str in list)
        if (str.Equals(value))
          return true;

      return false;
    }
  }
}
