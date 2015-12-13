//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
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

using dotless.Core.Parser.Tree;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Infrastructure;

using MapSurfer.Styling.Formats.CartoCSS.Parser.Tree;

namespace MapSurfer.Styling.Formats.CartoCSS.Parser
{
	internal static class LessExtensions
	{
		public static IEnumerable<Node> FilterNodes(this NodeList<Node> nodes, Type nodeType)
		{
			foreach (Node node in nodes) {
				if (node.GetType() == nodeType)
					yield return node;
			}
		}

		public static IEnumerable<T> FilterElements<T>(this Selector selector) where T : Element
		{
			Type elemType = typeof(T);
			foreach (Element elem in selector.Elements) {
				if (elem.GetType() == elemType) {
					yield return (T)elem;
				}
			}
		}

		public static IEnumerable<T> FilterElements<T>(this NodeList<Selector> selectors) where T : Element
		{
			Type elemType = typeof(T);
			foreach (Selector selector in selectors) {
				foreach (Element elem in selector.Elements) {
					if (elem.GetType() == elemType) {
						yield return (T)elem;
					}
				}
			}
		}

		private static void EvaluateZooms(NodeList<CartoSelector> selectors, Env env)
		{
		
			foreach (CartoSelector selector in selectors) {
				var zoomValue = 0x7FFFFF;

				foreach (CartoZoomElement zoomElem in selector.Zooms) {
					zoomValue = zoomValue & (zoomElem.Evaluate(env) as CartoZoomElement).Zoom;
				}

				selector.Zoom = zoomValue;
			}
		}

		public static List<CartoDefinition> Flatten(this Ruleset ruleset, List<CartoDefinition> result, NodeList<CartoSelector> parents, Env env)
		{
			NodeList<CartoSelector> selectors = GetCartoSelectors(ruleset);
			NodeList<CartoSelector> selectorsResult = new NodeList<CartoSelector>();

			if (selectors.Count == 0) {
				env.Frames.Concat(ruleset.Rules);
			}

			// evaluate zoom variables on this object.
			EvaluateZooms(selectors, env);

			for (int i = 0; i < selectors.Count; i++) {
				CartoSelector child = selectors[i];
				if (child.Filters == null) {
					// TODO: is this internal inconsistency?
					// This is an invalid filterset.

					continue;
				}

				if (parents.Count > 0) {
					foreach (CartoSelector parent in parents) {
						object mergedFilters = parent.Filters.CloneWith(child.Filters, env);//new CartoFilterSet(child.Filters);

						if (mergedFilters == null) {
						
							// Filters could be added, but they didn't change the
							// filters. This means that we only have to clone when
							// the zoom levels or the attachment is different too.

							if (parent.Zoom == (parent.Zoom & child.Zoom) && parent.Attachment == child.Attachment && parent.ElementsEqual(child)) {
								selectorsResult.Add(parent);
								continue;
							} else {
								mergedFilters = parent.Filters;
							}
						} else if (mergedFilters is bool && !(Convert.ToBoolean(mergedFilters))) {
							// The merged filters are invalid, that means we don't
							// have to clone.

							continue;
						}

						CartoSelector clone = new CartoSelector(child);
						clone.Filters = (CartoFilterSet)mergedFilters;
						clone.Zoom = parent.Zoom & child.Zoom;
						clone.Elements.Clear();
						clone.Elements.AddRange(parent.Elements.Concat(child.Elements));

						if (parent.Attachment != null && child.Attachment != null) {
							clone.Attachment = parent.Attachment + '/' + child.Attachment;
						} else {
							clone.Attachment = child.Attachment ?? parent.Attachment;
						}

						clone.Conditions = parent.Conditions + child.Conditions;
						clone.Index = child.Index;
						selectorsResult.Add(clone);
					}
				} else {
					selectorsResult.Add(child);
				}
			}

			NodeList<CartoRule> rules = new NodeList<CartoRule>();

			foreach (dotless.Core.Parser.Infrastructure.Nodes.Node rule in ruleset.Rules) {
				// Recursively flatten any nested rulesets
				if (rule is Ruleset) {
					List<CartoDefinition> defs = Flatten(rule as Ruleset, result, selectorsResult, env);
				} else if (rule is CartoRule) {
					rules.Add(rule as CartoRule);
				} else if (rule as CartoInvalidElement) {
					env.Logger.Log(dotless.Core.Loggers.LogLevel.Error, "Rule");
				}
			}

			int index = rules.Count > 0 ? rules[0].Index : -1;

			for (int i = 0; i < selectorsResult.Count; i++) {
				// For specificity sort, use the position of the first rule to allow
				// defining attachments that are under current element as a descendant
				// selector.

				CartoSelector selector = selectorsResult[i];
				if (index >= 0) {
					selector.Index = index;
				}

				// make a copy of the rules array, since the next iteration will change zooms
				CartoRule[] arrRules = new CartoRule[rules.Count];
				for (int j = 0; j < arrRules.Length; j++)
					arrRules[j] = (CartoRule)rules[j].Clone();

				result.Add(new CartoDefinition(selector, arrRules));
			}

			return result;
		}

		public static NodeList<CartoSelector> GetCartoSelectors(this Ruleset ruleset)
		{
			NodeList<CartoSelector> selectors = new NodeList<CartoSelector>();

			foreach (Selector selector in ruleset.Selectors) {
				CartoSelector cs = selector as CartoSelector;
				if (cs) {
					selectors.Add(cs);
				}
			}

			return selectors;
		}

		public static string GetValueAsString(this Node value, Env env)
		{
			Node v = value.Evaluate(env);

			Color clr = v as Color;
			if (clr != null) {
				return clr.ToArgb();
			}

			return v.ToCSS(env);
		}
	}
}
