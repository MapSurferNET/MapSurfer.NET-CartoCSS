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

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoDefinition : ISpecificity
  {
    private NodeList<CartoRule> m_rules;
    private NodeList<CartoElement> m_elements;
    private CartoFilterSet m_filters;
    private string m_attachment;
    private List<string> m_ruleIndex;
    private int m_zoom;
    private int[] m_specificity;

    public CartoDefinition(CartoSelector selector, CartoRule[] rules)
    {
      m_rules = new NodeList<CartoRule>();
      m_elements = selector.Elements;
      m_filters = selector.Filters;
      m_rules.AddRange(rules);
      m_zoom = selector.Zoom;
      m_attachment = selector.Attachment ?? "__default__";
      m_ruleIndex = new List<string>();
      for (int i = 0; i < m_rules.Count; i++)
      {
        m_rules[i].Zoom = selector.Zoom;
        m_ruleIndex.Add(m_rules[i].Id);
      }

      m_specificity = selector.Specificity();
    }

    public CartoDefinition(CartoDefinition def, CartoFilterSet filters, Env env)
    {
      m_rules = CreateCopy(def.Rules);
      m_elements = new NodeList<CartoElement>(def.m_elements);
      m_ruleIndex = new List<string>(def.m_ruleIndex);
      m_filters = filters != null ? filters : (CartoFilterSet)def.Filters.Clone(env);
      m_attachment = def.Attachment;
      m_zoom = def.m_zoom;
      m_specificity = def.m_specificity;
    }

    public string Attachment
    {
      get { return m_attachment; }
      set { m_attachment = value; }
    }

    public NodeList<CartoElement> Elements
    {
      get { return m_elements; }
      set { m_elements = value; }
    }

    public CartoFilterSet Filters
    {
      get { return m_filters; }
      set { m_filters = value; }
    }

    public NodeList<CartoRule> Rules
    {
      get { return m_rules; }
      set { m_rules = value; }
    }

    public int AddRules(CartoRule[] rules)
    {
      int added = 0;

      // Add only unique rules.
      for (int i = 0; i < rules.Length; i++)
      {
        if (!m_ruleIndex.Contains(rules[i].Id))
        {
          m_rules.Add(rules[i]);
          m_ruleIndex.Add(rules[i].Id);
          added++;
        }
      }

      return added;
    }

    public int AddRules(NodeList<CartoRule> rules)
    {
      int added = 0;

      // Add only unique rules.
      for (int i = 0; i < rules.Count; i++)
      {
        if (!m_ruleIndex.Contains(rules[i].Id))
        {
          m_rules.Add(rules[i]);
          m_ruleIndex.Add(rules[i].Id);
          added++;
        }
      }

      return added;
    }

    public int[] Specificity()
    {
      return m_specificity;
    }

    public object Clone(Env env)
    {
      CartoDefinition clone = new CartoDefinition(this, null, env);      
      return clone;
    }

    public object Clone(CartoFilterSet filters, Env env)
    {
      CartoDefinition clone = new CartoDefinition(this, filters, env);
      return clone;
    }

    /// <summary>
    /// Determine whether this selector matches a given id
    /// and array of classes, by determining whether
    /// all elements it contains match.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="classes"></param>
    /// <returns></returns>
    public bool AppliesTo(string id, Dictionary<string, bool> classes)
    {
      for (int i = 0, l = m_elements.Count; i < l; i++)
      {
        var elem = m_elements[i];
        ElementType type = elem.Type;
        if (!(type == ElementType.Wildchar ||
            (type == ElementType.Class && classes.ContainsKey(elem.Value)) ||
            (type == ElementType.Id && id == elem.Value)))
          return false;
      }

      return true;
    }

    private NodeList<CartoRule> CreateCopy(NodeList<CartoRule> rules)
    {
      NodeList<CartoRule> list = new NodeList<CartoRule>(rules.Count);
      for (int j = 0; j < rules.Count; j++)
        list.Add((CartoRule)rules[j].Clone());

      return list;
    }

    public override string ToString()
    {
    	return "Filters:" + m_filters.ToString()+"; Rules:"+m_rules.Count.ToString();
    }
  }
}
