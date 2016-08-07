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
using System.Linq;
using System.Text;
using System.Globalization;

using dotless.Core.Parser.Infrastructure;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoFilterSet 
  {
    private string m_prevKey;
    private CartoFilterElement m_prevFilter;
    private Dictionary<string, CartoFilterElement> m_filters;

    private static NumberStyles numStyles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
    private static NumberFormatInfo numFormat = NumberFormatInfo.CurrentInfo;

    public CartoFilterSet()
    {
      m_filters = new Dictionary<string, CartoFilterElement>();
    }

    public CartoFilterSet(Dictionary<string, CartoFilterElement> elements, Env env)
    {
      m_filters = new Dictionary<string, CartoFilterElement>(elements.Count);
      foreach (KeyValuePair<string, CartoFilterElement> elem in elements)
        m_filters.Add(elem.Key, elem.Value);
    }

    public int Count
    {
      get
      {
        return m_filters.Count;
      }
    }

    public IEnumerable<CartoFilterElement> Filters
    {
      get
      {
        return m_filters.Values;
      }
    }

    public void Evaluate(Env env)
    {
      foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
        kv.Value.Evaluate(env);
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      int nItems = m_filters.Count; 
      int i = 0;
      foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
      {
        sb.Append(kv.Value.Id + ((i < nItems - 1) ? " " : ""));
        i++;
      }

      return sb.ToString();
    }

    public object Clone(Env env)
    {
      var clone = new CartoFilterSet(m_filters, env);
      return clone;
    }

    public bool CanCloneWith(CartoFilterSet other, Env env)
    {
      int additions = 0;

      if (m_filters.Count > 0)
      {
        foreach (KeyValuePair<string, CartoFilterElement> kv in other.m_filters)
        {
          string id = kv.Key;
          object status = this.Addable(kv.Value, env);
          if (status != null)
          {
            if ((bool)status == false)
            {
              return true;
            }
            if ((bool)status == true)
            {
              additions++;
            }
          }
        }
      }
      else
      {
        additions += other.m_filters.Count;
      }

      return additions > 0;
    }

    public object CloneWith(CartoFilterSet other, Env env)
    {
      List<CartoFilterElement> additions = null;

      if (m_filters.Count > 0)
      {
        foreach (KeyValuePair<string, CartoFilterElement> kv in other.m_filters)
        {
          string id = kv.Key;
          object status = this.Addable(kv.Value, env);
          if (status != null)
          {
            if ((bool)status == false)
            {
              return false;
            }
            if ((bool)status == true)
            {
              // Adding the filter will override another value.
              if (additions == null)
                additions = new List<CartoFilterElement>();
              additions.Add(other.m_filters[id]);
            }
          }
        }
      }
      else
      {
        if (other.m_filters.Count > 0)
        {
          additions = new List<CartoFilterElement>(other.m_filters.Count);
          foreach (KeyValuePair<string, CartoFilterElement> kv in other.m_filters)
            additions.Add(other.m_filters[kv.Key]);
        }
      }

      // Adding the other filters doesn't make this filterset invalid, but it
      // doesn't add anything to it either.
      if (additions == null) return null;

      // We can successfully add all filters. Now clone the filterset and add the
      // new rules.
      var clone = new CartoFilterSet();

      // We can add the rules that are already present without going through the
      // add function as a Filterset is always in it's simplest canonical form.
      foreach (KeyValuePair<string, CartoFilterElement> kv1 in m_filters)
      {
      	  CartoFilterElement filter = kv1.Value;
      	  clone.m_filters[kv1.Key] = filter;
      }

      // Only add new filters that actually change the filter.
      //while (id = additions.shift()) {
      foreach (CartoFilterElement id in additions)
      {
      	clone.Add(id, env);
      }

      return clone;
    }

    private CartoFilterElement GetFilter(string key)
    {
      if (m_filters.Count > 0)
      {
        foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
        {
          if (kv.Value.Contains(key))
            return kv.Value;
        }
      }

      return null;
    }

    private CartoFilterElement GetFilter(string key, string op)
    {
      if (m_filters.Count > 0)
      {
        if (m_prevFilter != null)
        {
          if (string.Equals(m_prevKey, key))
          {
            if (m_prevFilter.Contains(key, op))
              return m_prevFilter;
          }
        }

        m_prevFilter = null;
        m_prevKey = null;

        foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
        {
          if (kv.Value.Contains(key, op))
          {
            m_prevKey = key;
            m_prevFilter = kv.Value;

            return kv.Value;
          }
        }
      }

      return null;
    }

    private bool Conflict(CartoFilterElement filter, Env env)
		{
			string key = filter.Key.ToString();
			string value = filter.GetValue(env);

      // if (a=b) && (a=c)
      // if (a=b) && (a!=b)
      // or (a!=b) && (a=b)
      string op = filter.Op.ToString();
      if ((op == "=" && GetFilter(key, "=") != null &&
			        value != GetFilter(key , "=").GetValue(env)) ||
			        (op == "!=" && GetFilter(key , "=") != null &&
			        value == GetFilter(key , "=").GetValue(env)) ||
			        (op == "=" && GetFilter(key , "!=") != null &&
			        value == GetFilter(key, "!=").GetValue(env))) {
				return false;//filter.toString() + ' added to ' + this.toString() + ' produces an invalid filter';
			}

			return false;
		}

    public void Add(CartoFilterElement filter, Env env)
    {
      bool conflict = Conflict(filter, env);

      if (conflict)
        return; //conflict;

      string key = filter.Key.ToString();
      string value = filter.Value.ToString();
      string op = filter.Op.ToString();
      float valueF = float.NaN;
      if (value.Length > 0 && value[0] <= '9')
          ConvertUtility.TryParseFloat(value, numStyles, numFormat, out valueF);

      if (op == "=")
      {
        /*foreach (var item in m_filters.Where(kvp => kvp.Key == key).ToList())
        {
          Remove(item.Key);
				}*/
        Remove(key);
        m_filters[key + "="] = filter;
      }
      else if (op == "!=")
      {
        m_filters[key + "!=" + filter.GetValue(env)] = filter;
      }
      else if (op == "=~")
      {
        m_filters[key + "=~" + filter.GetValue(env)] = filter;
      }
      else if (op == "%")
      {
        m_filters[key + "%" + filter.GetValue(env)] = filter;
      }
      else if (op == ">")
      {
        // If there are other filters that are also >
        // but are less than this one, they don't matter, so
        // remove them.
        /*	foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) <= valueF).ToList()) {
            Remove(item.Key);
          }*/
        Remove(key, (x) => (x.GetValueF(env) <= valueF));

        m_filters[key + ">"] = filter;
      }
      else if (op == ">=")
      {
        /*foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) < valueF).ToList()) {
          Remove(item.Key);
				}*/
        Remove(key, (x) => (x.GetValueF(env) < valueF));
        if (GetFilter(key + "!=" + value) != null)
        {
          Remove(key + "!=" + value);
          filter.Op = new dotless.Core.Parser.Infrastructure.Nodes.TextNode(">");
          m_filters[key + ">"] = filter;
        }
        else
        {
          m_filters[key + ">="] = filter;
        }
      }
      else if (op == "<")
      {
        /*foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) >= valueF).ToList()) {
          Remove(item.Key);
				}*/
        Remove(key, (x) => (x.GetValueF(env) >= valueF));
        m_filters[key + "<"] = filter;
      }
      else if (op == "<=")
      {
        /*foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) > valueF).ToList()) {
          Remove(item.Key);
				}*/
        Remove(key, (x) => (x.GetValueF(env) > valueF));

        if (GetFilter(key + "!=" + value) != null)
        {
          Remove(key + "!=" + value);
          filter.Op = new dotless.Core.Parser.Infrastructure.Nodes.TextNode("<");
          m_filters[key + "<"] = filter;
        }
        else
        {
          m_filters[key + "<="] = filter;
        }
      }
    }

    private void Remove(string key, Func<CartoFilterElement, bool> func = null)
    {
      bool bFound = false;

      if (func == null)
      {
        foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
        {
          if (kv.Key == key)
          {
            bFound = true;
            break;
          }
        }
      }
      else
      {
        foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
        {
          if (kv.Key == key && func(kv.Value))
          {
            bFound = true;
            break;
          }
        }
      }

      if (bFound)
      {
        m_filters.Remove(key);
        m_prevFilter = null;
        m_prevKey = null;
      }
    }
    
    private object Addable(CartoFilterElement filter, Env env)
    {
      string key = filter.Key.ToString();
      string val = filter.GetValue(env);

      string value = val;
      float valueF = float.MinValue;

      if (val.Length > 0 && val[0] <= '9')
        ConvertUtility.TryParseFloat(value, numStyles, numFormat, out valueF);

      switch (filter.Op.ToString())
      {
         case "=":
            // if there is already foo= and we're adding foo=
            if (GetFilter(key , "=") != null) {
                if (GetFilter(key , "=").GetValue(env) != value) {
                    return false;
                } else {
                    return null;
                }
            }
            if (GetFilter(key, "!=" + value) != null) return false;
            if (GetFilter(key, ">") != null && GetFilter(key, ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key, "<") != null && GetFilter(key, "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key, ">=") != null  && GetFilter(key, ">=").GetValueF(env) > valueF) return false;
            if (GetFilter(key, "<=") != null  && GetFilter(key, "<=").GetValueF(env) < valueF) return false;
            return true;
        case "=~":
            return true;
        case "%":
          return true;
        case "!=":
            if (GetFilter(key, "=") != null) return (GetFilter(key, "=").GetValue(env) == value) ? (object)false : null;
            if (GetFilter(key, "!=" + value) != null) return null;
            if (GetFilter(key, ">") != null && GetFilter(key, ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key, "<") != null && GetFilter(key, "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key, ">=") != null && GetFilter(key, ">=").GetValueF(env) > valueF) return null;
            if (GetFilter(key, "<=") != null && GetFilter(key, "<=").GetValueF(env) < valueF) return null;
            return true;
        case ">":
            if (GetFilter(key, "=") != null)
            {
                if (GetFilter(key, "=").GetValueF(env) <= valueF) {
                    return false;
                } else {
                    return null;
                }
            }
            if (GetFilter(key, "<") != null && GetFilter(key, "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key, "<=") != null  && GetFilter(key, "<=").GetValueF(env) <= valueF) return false;
            if (GetFilter(key, ">") != null && GetFilter(key, ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key, ">=") != null  && GetFilter(key, ">=").GetValueF(env) > valueF) return null;
            return true;

        case ">=":
            if (GetFilter(key, "=" ) != null) return (GetFilter(key, "=").GetValueF(env) < valueF) ? (object)false : null;
            if (GetFilter(key, "<" ) != null && GetFilter(key, "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key, "<=") != null && GetFilter(key, "<=").GetValueF(env) < valueF) return false;
            if (GetFilter(key, ">" ) != null && GetFilter(key, ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key, ">=") != null && GetFilter(key, ">=").GetValueF(env) >= valueF) return null;
            return true;

        case "<":
            if (GetFilter(key, "=" ) != null) return (GetFilter(key, "=").GetValueF(env) >= valueF) ? (object)false : null;
            if (GetFilter(key, ">" ) != null && GetFilter(key, ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key, ">=") != null && GetFilter(key, ">=").GetValueF(env) >= valueF) return false;
            if (GetFilter(key, "<" ) != null && GetFilter(key, "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key, "<=") != null && GetFilter(key, "<=").GetValueF(env) < valueF) return null;
            return true;

        case "<=":
            if (GetFilter(key, "=" ) != null) return (GetFilter(key, "=").GetValueF(env) > valueF) ? (object)false : null;
            if (GetFilter(key, ">" ) != null && GetFilter(key, ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key, ">=") != null && GetFilter(key, ">=").GetValueF(env) > valueF) return false;
            if (GetFilter(key, "<" ) != null && GetFilter(key, "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key, "<=") != null && GetFilter(key, "<=").GetValueF(env) <= valueF) return null;
            return true;
      }
      
      return null;
    }
  }
}
