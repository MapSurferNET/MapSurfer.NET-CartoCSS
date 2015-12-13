//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
// 
//    A C# port of the carto library written by Mapbox (https://github.com/mapbox/carto/)
//    and released under the Apache License Version 2.0.
//
//==========================================================================================
using System;

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoZoomElement : CartoElement
  {
    private int MAX_ZOOM = 22;

    private double[] SCALE_DENOMINATORS = new double[] {
      1000000000,
      500000000,
      200000000,
      100000000,
      50000000,
      25000000,
      12500000,
      6500000,
      3000000,
      1500000,
      750000,
      400000,
      200000,
      100000,
      50000,
      25000,
      12500,
      5000,
      2500,
      1500,
      750,
      500,
      250,
      100
    };

    private Node m_comp;
    private Node m_value;
    private int m_zoom;

    public CartoZoomElement(Node comp, Node number, Combinator combinator, Node value):base(combinator, value)
    {
       m_comp = comp;
       m_value = number;
    }

    public int Zoom
    {
      get
      {
        return m_zoom;
      }
      set
      {
        m_zoom = value;
      }
    }

    public override Node Evaluate(Env env)
    {
      int minZoom = 0;
      int maxZoom = int.MaxValue;
      m_zoom = 0;

      m_value.Evaluate(env);
      
      Number number = m_value as Number;

      int value = 0;
    
      if (number != null) 
      	value =  Convert.ToInt32(number.ToNumber());
      else
      	value =  Convert.ToInt32(m_value.ToString());

      if (value > MAX_ZOOM || value < 0)
        throw new Exception(string.Format("Zoom '{0}' level is out of range", value));

      string comp = m_comp.ToString();

      switch (comp)
      {
        case "=":
          m_zoom = 1 << value;
          return this;
        case ">":
          minZoom = value + 1;
          break;
        case ">=":
          minZoom = value;
          break;
        case "<":
          maxZoom = value - 1;
          break;
        case "<=":
          maxZoom = value;
          break;
      }

      for (var i = 0; i <= MAX_ZOOM; i++)
      {
        if (i >= minZoom && i <= maxZoom)
        {
          m_zoom |= (1 << i);
        }
      }

      return base.Evaluate(env);
    }
  }
}
