//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2016, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MapSurfer.Styling.Formats.CartoCSS.Translators.Referencers
{
  internal class SymbolizerDescriptor
  {
    public Type Type { get; set; }
    public string ShortName { get; set; }
    public Dictionary<string, CartoPropertyInfo> Properties { get; set; }

    public SymbolizerDescriptor(Type type)
    {
      Type = type;
      Properties = new Dictionary<string, CartoPropertyInfo>();
    }
  }

  internal abstract class CartoPropertyReferencer : ICartoPropertyReferencer
  {
    private Dictionary<string, SymbolizerDescriptor> m_typesDict;
    private Dictionary<string, SymbolizerDescriptor> m_propertyTypesDict;
    private Dictionary<string, object> m_setterDict;

    public CartoPropertyReferencer()
    {
      m_typesDict = new Dictionary<string, SymbolizerDescriptor>();
      m_propertyTypesDict = new Dictionary<string, SymbolizerDescriptor>();
      m_setterDict = new Dictionary<string, object>();

      Prepare();
    }

    protected void AddTypeProperty(Type type, string name, CartoPropertyInfo[] properties)
    {
      SymbolizerDescriptor symDescriptor = null;

      string symName = type.Name + "_" + name; 
      if (!m_typesDict.TryGetValue(symName, out symDescriptor))
      {
        symDescriptor = new SymbolizerDescriptor(type);
        symDescriptor.ShortName = name;
        m_typesDict.Add(symName, symDescriptor);
      }

      Dictionary<string, CartoPropertyInfo> props = symDescriptor.Properties;

      foreach (CartoPropertyInfo pi in properties)
      {
        if (!props.ContainsKey(pi.CssName))
          props.Add(pi.CssName, pi);

        if (!m_propertyTypesDict.ContainsKey(pi.CssName))
          m_propertyTypesDict.Add(pi.CssName, symDescriptor);
      }
    }

    private Type DetectSymbolizerType(string[] cssProperties)
    {
      Dictionary<Type, int> counter = new Dictionary<Type, int>();

      foreach (string cssProp in cssProperties)
      {
        SymbolizerDescriptor symDesc = null;
        if (m_propertyTypesDict.TryGetValue(cssProp, out symDesc))
        {
          Type t = symDesc.Type;
          if (!counter.ContainsKey(t))
            counter.Add(t, 0);

          counter[t]++;
        }
      }

      Type type = null;
      int max = 0;
      foreach (Type t in counter.Keys)
      {
        int value = counter[t];
        if (max < value)
        {
          max = value;
          type = t;
        }
      }


      return type;
    }

    public string GetSymbolizerName(string property)
    {
      SymbolizerDescriptor symDesc = null;
      if (m_propertyTypesDict.TryGetValue(property, out symDesc))
        return symDesc.Type.Name + "_" + symDesc.ShortName;

      return null;
    }

    public bool IsSymbolizerPropertyValid(string symbolizer, NodePropertyValue property)
    {
      SymbolizerDescriptor symDesc = null;
      if (m_typesDict.TryGetValue(symbolizer, out symDesc))
      {
        bool bFound = false;
        foreach (string symProperty in symDesc.Properties.Keys)
        {
          if (string.Equals(symProperty, property.Name))
          {
            bFound = true;
            break;
          }
        }

        return bFound;
      }

      return false;
    }

    public bool HasRequiredProperties(string symbolizer, NodePropertyValue[] properties, ref string missingProperty)
    {
      SymbolizerDescriptor symDesc = null;
      if (m_typesDict.TryGetValue(symbolizer, out symDesc))
      {
        foreach (string key in symDesc.Properties.Keys)
        {
          CartoPropertyInfo cpi = symDesc.Properties[key];
          if (cpi.Required)
          {
            bool bFound = false;

            foreach (NodePropertyValue prop in properties)
            {
              if (string.Equals(cpi.CssName, prop.Name))
              {
                bFound = true;
                break;
              }
            }

            if (!bFound)
            {
              missingProperty = cpi.CssName;
              return false;
            }
          }
        }
      }

      return true;
    }

    public abstract void Prepare();
  }
}
