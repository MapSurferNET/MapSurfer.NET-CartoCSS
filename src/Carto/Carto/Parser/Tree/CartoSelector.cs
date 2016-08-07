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
using System.Collections.Generic;

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoSelector : Selector
  {
    private CartoFilterSet m_filters;
    private string m_attachment;
    private NodeList<CartoZoomElement> m_zooms;
    private NodeList<CartoElement> m_elements;
    private int m_zoom;
    private int m_index = -1;
    private int m_conditions;

    public CartoSelector(CartoSelector selector)
      : base(selector.ElementsInternal)
    {
      m_zooms = new NodeList<CartoZoomElement>();
      m_elements = new NodeList<CartoElement>();
      m_filters = new CartoFilterSet();
      m_attachment = selector.Attachment;
      m_zoom = selector.Zoom;
      m_index = selector.m_index;

      Location = selector.Location;
    }

    public CartoSelector(IEnumerable<Element> elements, Env env)
      : base(elements)
    {
      m_filters = new CartoFilterSet();
      m_zooms = new NodeList<CartoZoomElement>();
      m_elements = new NodeList<CartoElement>();

      m_conditions = 0;
      if (env == null)
      	env = new Env(); // TODO
      
      foreach (Element elem in elements)
      {
        if (elem is CartoFilterElement)
        {
          m_filters.Add(elem as CartoFilterElement, env);
          m_conditions++;
        }
        else if (elem is CartoZoomElement)
        {
          m_zooms.Add(elem as CartoZoomElement);
          m_conditions++;
        }
        else if (elem is CartoAttachmentElement)
          m_attachment = (elem as CartoAttachmentElement).Value;
        else
          m_elements.Add((CartoElement)elem);
      }
    }

    private IEnumerable<Element> ElementsInternal
    {
      get
      {
        foreach (CartoElement elem in m_elements)
        {
          yield return (Element)elem;
        }
      }
    }

    public int Conditions
    {
      get { return m_conditions; }
      set { m_conditions = value; }
    }

    public new NodeList<CartoElement> Elements
    {
      get
      {
        return m_elements;
      }
      set
      {
        m_elements = value;
      }
    }

    public CartoFilterSet Filters
    {
      get
      {
        return m_filters;
      }
      set
      {
        m_filters = value;
      }
    }

    public NodeList<CartoZoomElement> Zooms
    {
      get
      {
        return m_zooms;
      }
      set
      {
        m_zooms = value;
      }
    }

    public string Attachment
    {
      get
      {
        return m_attachment;
      }
      set
      {
        m_attachment = value;
      }
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
      NodeList<Element> evaldElements = new NodeList<Element>();
      foreach (Element element in Elements)
      {
        evaldElements.Add(element.Evaluate(env) as Element);
      }

      return new CartoSelector(evaldElements, env).ReducedFrom<CartoSelector>(this);
    }

    public bool ElementsEqual(CartoSelector selector)
    {
      if (m_elements.Count == selector.Elements.Count)
      {
        for (int i = 0; i < m_elements.Count; i++)
        {
          if (m_elements[i] != selector.m_elements[i])
            return false;
        }
      }

      return false;
    }

    public int[] Specificity()
    {
      int[] res = new int[] { 0, 0, m_conditions, Index };

      foreach (Element elem in m_elements)
      {
        CartoElement telem = elem as CartoElement;
        if (telem != null)
        {
          int[] spec = telem.Specificity();
          res[0] += spec[0];
          res[1] += spec[1];
        }
      }

      return res;
    }
  }
}
