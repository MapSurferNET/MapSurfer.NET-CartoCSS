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
using System.Linq;

using dotless.Core.Exceptions;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoRule : dotless.Core.Parser.Tree.Rule
  {
    private int m_index = -1;
    private string m_id;
    private string m_instance;
    private int m_zoom;
    private string[] m_parts;

    private CartoRule(CartoRule rule):base(rule.Name, rule.Value)
    {
      m_index = rule.m_index;
      m_id = rule.m_id;
      m_instance = rule.m_instance;
      m_zoom = rule.m_zoom;
      m_parts = rule.m_parts;
      Location = rule.Location;
    }

    public CartoRule(string name, Node value)
      : this(name, value, false)
    { }

    public CartoRule(string name, Node value, bool variadic)
      : base(name, value, variadic)
    {
      m_parts = name.Split('/');
      Name = m_parts.Last();
      m_instance = m_parts.Length >= 2 ? m_parts[m_parts.Length - 2] : "__default__";

      UpdateID();
    }

    public object Clone()
    {
      return new CartoRule(this);
    }

    public bool Contains(string key)
    {
      return m_parts.Contains(key);
    }

    public override string ToString()
    {
      return Name + ":" + Value.ToString();
    }

    public string Id
    {
      get
      {
        return m_id;
      }
    }

    public string UpdateID()
    {
      m_id = m_zoom.ToString() + '#' + m_instance + '#' + this.Name;
      return m_id;
    }

    public override Node Evaluate(Env env)
    {
      env.Rule = this;

      if (Value == null)
      {
        throw new ParsingException("No value found for rule " + Name, Location);
      }

      var rule = new CartoRule(Name, Value.Evaluate(env)).ReducedFrom<CartoRule>(this);
      rule.IsSemiColonRequired = this.IsSemiColonRequired;
      rule.PostNameComments = this.PostNameComments;

      env.Rule = null;

      return rule;
    }

    public int Index
    {
      get
      {
        if (m_index == -1)
          return Location.Index;
        else
          return m_index;
      }
      set
      {
        m_index = value;
      }
    }

    public string Instance
    {
      get
      {
        return m_instance;
      }
    }

    public int Zoom
    {
      get
      {
        return m_zoom;
      }
      set
      {
        if (m_zoom != value)
        {
          m_zoom = value;
          UpdateID();
        }
      }
    }
  }
}
