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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using dotless.Core.Parser.Infrastructure;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser.Tree
{
  internal class CartoFilterSet 
  {
    private class FilterComparer : IComparer<string>
    {
      public int Compare(string x, string y)
      {
        return x.CompareTo(y);
      }
    }

    private SortedDictionary<string, CartoFilterElement> m_filters;

    public CartoFilterSet()
    {
      m_filters = new SortedDictionary<string, CartoFilterElement>(new FilterComparer());
    }

    public CartoFilterSet(IEnumerable<CartoFilterElement> elements, Env env):this()
    {
      foreach (CartoFilterElement elem in elements)
        Add(elem, env);
    }

    public int Count
    {
      get
      {
        return m_filters.Count;
      }
    }

    public CartoFilterElement this[string id]
    {
      get
      {
        CartoFilterElement result = null;

        if (m_filters.TryGetValue(id, out result))
          return result;
        else
          return null;
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
      var clone = new CartoFilterSet(m_filters.Values.ToList(), env);
      return clone;
    }

    public object CloneWith(CartoFilterSet other, Env env)
    {
      List<CartoFilterElement> additions = null;
      foreach (string id in other.m_filters.Keys)
      {
        object status = this.Addable(other[id], env);
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

      // Adding the other filters doesn't make this filterset invalid, but it
      // doesn't add anything to it either.
      if (additions == null) return null;

      // We can successfully add all filters. Now clone the filterset and add the
      // new rules.
      var clone = new CartoFilterSet();

      // We can add the rules that are already present without going through the
      // add function as a Filterset is always in it's simplest canonical form.
      foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
      {
      	  CartoFilterElement filter = kv.Value;
      	  clone.m_filters[kv.Key] = filter;
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
      foreach (KeyValuePair<string, CartoFilterElement> kv in m_filters)
      {
        if (kv.Value.Contains(key))
          return kv.Value;
      }

      return null;
    }
    
		private bool Conflict(CartoFilterElement filter, Env env)
		{
			string key = filter.Key.ToString();
			string value = filter.Value.ToCSS(env);
      
			// if (a=b) && (a=c)
			// if (a=b) && (a!=b)
			// or (a!=b) && (a=b)
			if ((filter.Op.ToString() == "=" && GetFilter(key + "=") != null &&
			        value != GetFilter(key + "=").GetValue(env)) ||
			        (filter.Op.ToString() == "!=" && GetFilter(key + "=") != null &&
			        value == GetFilter(key + "=").GetValue(env)) ||
			        (filter.Op.ToString() == "=" && GetFilter(key + "!=") != null &&
			        value == GetFilter(key + "!=").GetValue(env))) {
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
			float.TryParse(value, out valueF);
    	
			if (op == "=") {
				foreach (var item in m_filters.Where(kvp => kvp.Key == key).ToList()) {
					m_filters.Remove(item.Key);
				}
				m_filters[key + "="] = filter;
			} else if (op == "!=") {
				m_filters[key + "!=" + filter.GetValue(env)] = filter;
			} else if (op == "=~") {
				m_filters[key + "=~" + filter.GetValue(env)] = filter;
			} else if (op == ">") {
				// If there are other filters that are also >
				// but are less than this one, they don't matter, so
				// remove them.
				foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) <= valueF).ToList()) {
					m_filters.Remove(item.Key);
				}
				m_filters[key + ">"] = filter;
			} else if (op == ">=") {
				foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) < valueF).ToList()) {
					m_filters.Remove(item.Key);
				}
     
				if (GetFilter(key + "!=" + value) != null) {
					m_filters.Remove(key + "!=" + value);
					filter.Op = new dotless.Core.Parser.Infrastructure.Nodes.TextNode(">");
					m_filters[key + ">"] = filter;
				} else {
					m_filters[key + ">="] = filter;
				}
			} else if (op == "<") {
				foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) >= valueF).ToList()) {
					m_filters.Remove(item.Key);
				}
				m_filters[key + "<"] = filter;
			} else if (op == "<=") {
				foreach (var item in m_filters.Where(kvp => kvp.Key == key && kvp.Value.GetValueF(env) > valueF).ToList()) {
					m_filters.Remove(item.Key);
				}
				if (GetFilter(key + "!=" + value) != null) {
					m_filters.Remove(key + "!=" + value);
					filter.Op = new dotless.Core.Parser.Infrastructure.Nodes.TextNode("<");
					m_filters[key + "<"] = filter;
				} else {
					m_filters[key + "<="] = filter;
				}
			}
		}
    
    private object Addable(CartoFilterElement filter, Env env)
    {
      string key = filter.Key.ToString();
      string val = filter.Value.ToCSS(env);

      string value = val;
      float valueF = float.NaN;
      float.TryParse(val, out valueF);
      // RegexMatchResult match = value as RegexMatchResult;
      // Regex
      //if (match != null && match. .match("^[0-9]+(\.[0-9]*)?$")) 
      
      switch(filter.Op.ToString())
      {
         case "=":
            // if there is already foo= and we're adding foo=
            if (GetFilter(key + "=") != null) {
                if (GetFilter(key + "=").GetValue(env) != value) {
                    return false;
                } else {
                    return null;
                }
            }
            if (GetFilter(key + "!=" + value) != null) return false;
            if (GetFilter(key + ">") != null && GetFilter(key + ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key + "<") != null && GetFilter(key + "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key + ">=") != null  && GetFilter(key + ">=").GetValueF(env) > valueF) return false;
            if (GetFilter(key + "<=") != null  && GetFilter(key + "<=").GetValueF(env) < valueF) return false;
            return true;
        case "=~":
            return true;
        case "!=":
            if (GetFilter(key + "=") != null) return (GetFilter(key + "=").GetValue(env) == value) ? (object)false : null;
            if (GetFilter(key + "!=" + value) != null) return null;
            if (GetFilter(key + ">") != null && GetFilter(key + ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key + "<") != null && GetFilter(key + "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key + ">=") != null && GetFilter(key + ">=").GetValueF(env) > valueF) return null;
            if (GetFilter(key + "<=") != null && GetFilter(key + "<=").GetValueF(env) < valueF) return null;
            return true;
        case ">":
            if (GetFilter(key + "=") != null)
            {
                if (GetFilter(key + "=").GetValueF(env) <= valueF) {
                    return false;
                } else {
                    return null;
                }
            }
            if (GetFilter(key + "<") != null && GetFilter(key + "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key + "<=") != null  && GetFilter(key + "<=").GetValueF(env) <= valueF) return false;
            if (GetFilter(key + ">") != null && GetFilter(key + ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key + ">=") != null  && GetFilter(key + ">=").GetValueF(env) > valueF) return null;
            return true;

        case ">=":
            if (GetFilter(key + "=" ) != null) return (GetFilter(key + "=").GetValueF(env) < valueF) ? (object)false : null;
            if (GetFilter(key + "<" ) != null && GetFilter(key + "<").GetValueF(env) <= valueF) return false;
            if (GetFilter(key + "<=") != null && GetFilter(key + "<=").GetValueF(env) < valueF) return false;
            if (GetFilter(key + ">" ) != null && GetFilter(key + ">").GetValueF(env) >= valueF) return null;
            if (GetFilter(key + ">=") != null && GetFilter(key + ">=").GetValueF(env) >= valueF) return null;
            return true;

        case "<":
            if (GetFilter(key + "=" ) != null) return (GetFilter(key + "=").GetValueF(env) >= valueF) ? (object)false : null;
            if (GetFilter(key + ">" ) != null && GetFilter(key + ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key + ">=") != null && GetFilter(key + ">=").GetValueF(env) >= valueF) return false;
            if (GetFilter(key + "<" ) != null && GetFilter(key + "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key + "<=") != null && GetFilter(key + "<=").GetValueF(env) < valueF) return null;
            return true;

        case "<=":
            if (GetFilter(key + "=" ) != null) return (GetFilter(key + "=").GetValueF(env) > valueF) ? (object)false : null;
            if (GetFilter(key + ">" ) != null && GetFilter(key + ">").GetValueF(env) >= valueF) return false;
            if (GetFilter(key + ">=") != null && GetFilter(key + ">=").GetValueF(env) > valueF) return false;
            if (GetFilter(key + "<" ) != null && GetFilter(key + "<").GetValueF(env) <= valueF) return null;
            if (GetFilter(key + "<=") != null && GetFilter(key + "<=").GetValueF(env) <= valueF) return null;
            return true;
      }
      
      return null;
    }
  }
}
