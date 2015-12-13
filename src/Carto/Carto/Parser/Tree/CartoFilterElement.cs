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
using System.Linq;

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Infrastructure;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoFilterElement : CartoElement
  {
    private Node m_key;
    private Node m_op;
    private Node m_value;
    private string m_id;

    public CartoFilterElement(Node key, Node op, Node value, Combinator combinator, Env env)
      : base(combinator, value)
    {
      m_key = key;
      m_op = op;
      m_value = value;

      m_id = "["+m_key.ToString() + "]" + m_op.ToString() + m_value.ToCSS(env).ToString();
    }

    public Node Key
    {
      get { return m_key; }
    }

    public new Node Value
    {
      get { return m_value; }
    }

    public Node Op
    {
      get { return m_op; }
      set { m_op = value; }
    }

    public string Id
    {
      get { return m_id; }
    }

    public override string ToString()
    {
      return m_id;
    }

    public string GetValue(Env env)
    {
      return m_value.ToCSS(env);
    }

    public float GetValueF(Env env)
    {
      return Convert.ToSingle(m_value.ToCSS(env));
    }

    public bool Contains(string key)
    {
      string key2 = m_key.ToString() + m_op;
      return (string.Equals(key2, key));
    }
  }
}
