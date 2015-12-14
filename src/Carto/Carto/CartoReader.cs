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
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

using ProjNet.CoordinateSystems;

using Newtonsoft.Json;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using dotless.Core.Parser;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Tree;

using MapSurfer.Logging;
using MapSurfer.Layers;
using MapSurfer.Drawing;
using MapSurfer.Configuration;
using MapSurfer.Rendering;
using MapSurfer.Labeling;
using MapSurfer.Drawing.Drawing2D;
using MapSurfer.Drawing.Text;
using MapSurfer.CoordinateSystems;
using MapSurfer.CoordinateSystems.Transformations;
using MapSurfer.Styling.Formats.CartoCSS.Parser;
using MapSurfer.Styling.Formats.CartoCSS.Parser.Infrastructure;
using MapSurfer.Styling.Formats.CartoCSS.Parser.Tree;
using MapSurfer.Styling.Formats.CartoCSS.Translators;

using Parameter = MapSurfer.Configuration.Parameter;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  internal static class CartoReader
  {
    #region ------------- Internal Structs/Classes -------------

    private class ZoomStruct
    {
      public int Available;
      public int Rule;
      public int Current;
    }

    #endregion

    private static ConcurrentDictionary<string, FontSet> _dictFontSets = new ConcurrentDictionary<string, FontSet>();
    
    public static Map ReadFromFile(string fileContent, string fileName)
    {
      string path = Path.GetDirectoryName(fileName);

      CartoProject cartoProject = null;

      switch (Path.GetExtension(fileName).ToLower())
      {
        case ".mml":
          cartoProject = JsonConvert.DeserializeObject<CartoProject>(fileContent);

          try
          {
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(cartoProject.Interactivity.ToString());
            cartoProject.Interactivity = dict;
          }
          catch
          { }

          try
          {
            bool enabled = JsonConvert.DeserializeObject<bool>(cartoProject.Interactivity.ToString());
            cartoProject.Interactivity = enabled;
          }
          catch
          { }
          break;
        case ".yaml":
          using (StringReader input = new StringReader(fileContent))
          {
            Deserializer deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            var parser = new MergingParser(new YamlDotNet.Core.Parser(input));
            cartoProject = deserializer.Deserialize<CartoProject>(new EventReader(parser));
          }
          break;
        default:
          throw new Exception("Unknown extension of the CartoCSS project.");
    }

      Map map = null;

      if (cartoProject.Stylesheet != null && cartoProject.Stylesheet.Length > 0 && cartoProject.Layers.Length > 0)
      {
        ICartoTranslator cartoTranslator = CartoGeneratorConverterFactory.CreateTranslator(cartoProject.Generator);

        CartoParser parser = new CartoParser();
        parser.NodeProvider = new CartoNodeProvider();
        Env env = new Env(); 
        List<Ruleset> ruleSets = new List<Ruleset>();
        List<CartoDefinition> definitions = new List<CartoDefinition>();

        foreach (string styleName in cartoProject.Stylesheet)
        {
          string styleFileName = Path.Combine(path, styleName);

          try
          {
            Ruleset ruleSet = parser.Parse(File.ReadAllText(styleFileName), styleFileName, env);

            ruleSets.Add(ruleSet);

            // Get an array of Ruleset objects, flattened
            // and sorted according to specificitySort
            var defs = new List<CartoDefinition>();
            defs = ruleSet.Flatten(defs, null, env);
            defs.Sort(new SpecificitySorter());

            definitions.AddRange(defs);
            env.Frames.Push(ruleSet);
          }
          catch (Exception ex)
          {
            Exception ex2 = new IOException(string.Format("An error occured during parsing of the style '{0}'.", styleFileName) + ex.Message);
            LogFactory.WriteLogEntry(Logger.Default, ex2);
            throw ex2;
          }
        }

        string interactivityLayer = null;
        if (cartoProject.GetInteractivity() != null && cartoProject.GetInteractivity().ContainsKey("layer"))
         	interactivityLayer = cartoProject.GetInteractivity()["layer"].ToString();

        map = CreateMap(cartoProject, definitions, env, cartoTranslator);

        foreach (CartoLayer cartoLayer in cartoProject.Layers)
        {
          CartoDatasource datasource = cartoLayer.Datasource;
          StyledLayer styledLayer = CreateStyledLayer(cartoLayer, map, cartoTranslator);

          try
          {
            string[] classes = (cartoLayer.Class.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            Dictionary<string, bool> classIndex = new Dictionary<string, bool>(classes.Length);
            for (int i = 0; i < classes.Length; i++)
              classIndex[classes[i]] = true;

			      var matching = definitions.FindAll(delegate (CartoDefinition def) { return def.AppliesTo(cartoLayer.Name, classIndex); });
			      
			      if (matching.Count > 0)
			      {
			      	List<CartoStyle> rules = InheritDefinitions(matching, env);

			      	if (rules.Count > 0)
			      	{
			      		SortStyles(rules, env);

			      		for (int k = 0; k < rules.Count; k++)
			      		{
			      			CartoStyle cartoStyle = rules[k];
			      			cartoStyle.Fold(env);
			      			string styleName = cartoLayer.Name + (cartoStyle.Attachment != "__default__" ? "-" + cartoStyle.Attachment : "");
			      			FeatureTypeStyle style = CreateStyle(styleName, cartoStyle, env, cartoTranslator);

			      			if (style.Rules.Count > 0)
			      				styledLayer.Styles.Add(style);
			      		}
			      		
			      		cartoTranslator.ProcessStyles(styledLayer.Styles);
			      	}
			      	
			      	if (!string.IsNullOrEmpty(interactivityLayer) && interactivityLayer.Equals(styledLayer.Name))
			      		styledLayer.Enabled = false;

			      	map.AddLayer(styledLayer);
			      }
          }
          catch (Exception ex)
          {
            Exception ex2 = new IOException(string.Format("Unable to create data source provider with type '{0}' for the layer '{1}'.", datasource.Type, cartoLayer.Name) + ex.Message);
            LogFactory.WriteLogEntry(Logger.Default, ex2);
          }
        }
      }

      return map;
    }

    /// <summary>
    /// Creates map object from <see cref="CartoProject"/>
    /// </summary>
    /// <param name="project"></param>
    /// <param name="definitions"></param>
    /// <param name="env"></param>
    /// <param name="cartoTranslator"></param>
    /// <returns></returns>
    private static Map CreateMap(CartoProject project, List<CartoDefinition> definitions, Env env, ICartoTranslator cartoTranslator)
    {
      Map map = new Map(project.Name);
      map.Size = new SizeF(700, 500);
      map.MinimumScale = ConvertUtility.ToScaleDenominator(project.MinZoom);
      map.MaximumScale = ConvertUtility.ToScaleDenominator(project.MaxZoom);
			 
     // if (project.Bounds != null)
     //   map.WGS84Bounds = project.Bounds[0] + "," + project.Bounds[1] + "," + project.Bounds[2] + "," + project.Bounds[3];
      map.CRS = cartoTranslator.ToCoordinateSystem(string.IsNullOrEmpty(project.Srs) ? project.SrsName: project.Srs, !string.IsNullOrEmpty(project.SrsName));

      SetMapProperties(map, GetProperties(definitions, env, "Map"), cartoTranslator);
      SetFontSets(map, definitions, env, cartoTranslator);

      if (project.Center != null)
      {
        if (map.CoordinateSystem == null)
        {
          CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
          map.CoordinateSystem = (CoordinateSystem)csFactory.CreateSphericalMercatorCoordinateSystem();
        }

        double cx = Convert.ToDouble(project.Center[0]);
        double cy = Convert.ToDouble(project.Center[1]);
        double cz = Convert.ToDouble(project.Center[2]);

        double scale = MapSurfer.Utilities.MapUtility.GetTileMapResolution(cz);
        GeoAPI.CoordinateSystems.Transformations.ICoordinateTransformation trans = CoordinateTransformationFactory.CreateCoordinateTransformation(GeographicCoordinateSystem.WGS84, map.CoordinateSystem);
        trans.MathTransform.Transform(ref cx, ref cy, ref cz);
        map.SetCenterAndZoom(cx, cy, scale);
      }

      return map;
    }

    /// <summary>
    /// Create styled layer
    /// </summary>
    /// <param name="cartoLayer"></param>
    /// <param name="cartoTranslator"></param>
    /// <returns></returns>
    private static StyledLayer CreateStyledLayer(CartoLayer cartoLayer, Map map, ICartoTranslator cartoTranslator)
    {
      ParameterCollection parameters = cartoTranslator.ToDatasourceParameters(cartoLayer);
      StyledLayer layer = new StyledLayer(cartoLayer.Name, parameters);
      layer.FeaturesCaching.Enabled = true;

      if (cartoLayer.Properties != null)
      {
        if (cartoLayer.Properties.ContainsKey("minzoom"))
          layer.MinimumScale = ConvertUtility.ToScaleDenominator(Convert.ToInt32(cartoLayer.Properties["minzoom"]));
        if (cartoLayer.Properties.ContainsKey("maxzoom"))
          layer.MaximumScale = ConvertUtility.ToScaleDenominator(Convert.ToInt32(cartoLayer.Properties["maxzoom"]));
        if (cartoLayer.Properties.ContainsKey("queryable"))
          layer.Queryable = Convert.ToBoolean(cartoLayer.Properties["queryable"]);
        if (cartoLayer.Properties.ContainsKey("cache-features")) // Extension
          layer.FeaturesCaching.Enabled = Convert.ToBoolean(cartoLayer.Properties["cache-features"]);
      }
      
     	layer.Enabled = cartoLayer.Status != "off";

      string providerName = parameters.GetValue("Type");
      if (string.IsNullOrEmpty(providerName))
        LogFactory.WriteLogEntry(Logger.Default, new IOException(string.Format("Unable to detect the type of a data source provider for the layer '{0}'.", cartoLayer.Name)));

      if(!string.IsNullOrEmpty(cartoLayer.Srs))
         layer.CRS = cartoTranslator.ToCoordinateSystem(string.IsNullOrEmpty(cartoLayer.Srs) ? cartoLayer.SrsName : cartoLayer.Srs, !string.IsNullOrEmpty(cartoLayer.SrsName));

      return layer;
    }

    /// <summary>
    /// Creates <see cref="FeatureTypeStyle"/> object
    /// </summary>
    /// <param name="styleName"></param>
    /// <param name="cartoStyle"></param>
    /// <param name="env"></param>
    /// <param name="cartoTranslator"></param>
    /// <returns></returns>
    private static FeatureTypeStyle CreateStyle(string styleName, CartoStyle cartoStyle, Env env, ICartoTranslator cartoTranslator)
    {
      FeatureTypeStyle style = new FeatureTypeStyle(styleName);
      style.ProcessFeatureOnce = true;

      List<CartoDefinition> definitions = cartoStyle.Definitions;
      Dictionary<string, int> existingFilters = null;
      List<string> imageFilters = new List<string>();
      List<string> imageFiltersBuffer = new List<string>();

      int k = 0;

      for (int i = 0; i < definitions.Count; i++)
      {
        CartoDefinition def = definitions[i];
        NodeList<CartoRule> rules = def.Rules;
        for (int j = 0; j < rules.Count; j++)
        {
          CartoRule cartoRule = rules[j];
          string ruleName = cartoRule.Name;
          if (ruleName == "image-filters")
          {
            string filter = cartoRule.Value.ToString();
            if (!imageFilters.Contains(filter))
            {
              style.ImageFiltersOptions.Filters.Add(cartoTranslator.ToImageFilter(filter));
              style.ImageFiltersOptions.Enabled = true;
              imageFilters.Add(filter);
            }
            k++;
          }
          else if (ruleName == "image-filters-inflate")
          {
            bool inflate = Convert.ToBoolean(cartoRule.Value.ToString());

            if (inflate)
              style.ImageFiltersOptions.Enabled = true;
            k++;
          }
          else if (ruleName == "direct-image-filters")
          {
            string filter = cartoRule.Value.ToString();
            if (!imageFiltersBuffer.Contains(filter))
            {
              style.ImageFiltersOptions.BufferFilters.Add(cartoTranslator.ToImageFilter(filter));
              style.ImageFiltersOptions.Enabled = true;
              imageFiltersBuffer.Add(filter);
            }
            k++;
          }
          else if (ruleName == "comp-op")
          {
            style.BlendingOptions.CompositingMode = cartoTranslator.ToCompositingMode(cartoRule.Value.Evaluate(env).ToString());
            k++;
          }
          else if (ruleName == "opacity")
          {
            style.BlendingOptions.Opacity = (float)(cartoRule.Value.Evaluate(env) as Number).Value;
            k++;
          }
          if (ruleName == "filter-mode")
          {
            string filterMode = cartoRule.Value.Evaluate(env).ToCSS(env);
            if (string.Equals(filterMode, "all"))
              style.ProcessFeatureOnce = false;
            else if (string.Equals(filterMode, "first"))
              style.ProcessFeatureOnce = true;
            
            k++;
          }
        }

        if (k < rules.Count)
        {
          if (existingFilters == null)
            existingFilters = new Dictionary<string, int>();

          CreateRules(style, def, env, existingFilters, cartoTranslator);
        }
      }

      return style;
    }

    /// <summary>
    /// Creates rules
    /// </summary>
    /// <param name="style"></param>
    /// <param name="def"></param>
    /// <param name="env"></param>
    /// <param name="existingFilters"></param>
    /// <param name="cartoTranslator"></param>
    private static void CreateRules(FeatureTypeStyle style, CartoDefinition def, Env env, Dictionary<string, int> existingFilters, ICartoTranslator cartoTranslator)
    {
      string filter = def.Filters.ToString();

      if (!(existingFilters.ContainsKey(filter)))
        existingFilters[filter] = 0x7FFFFF;

      int available = 0x7FFFFF;
      ZoomStruct zooms = new ZoomStruct();
      zooms.Available = available;

      NodeList<CartoRule> rules = def.Rules;
      for (int i = 0; i < rules.Count; i++)
      {
        CartoRule cartoRule = rules[i];
        zooms.Rule = cartoRule.Zoom;
        
        if ((existingFilters[filter] & zooms.Rule) == 0)
          continue;

        while (((zooms.Current = zooms.Rule) & available) != 0)
        {
          Dictionary<string, Dictionary<string, CartoRule>> symbolizers = CollectSymbolizers(def, zooms, i, env, cartoTranslator);

          if (symbolizers != null && symbolizers.Count > 0)
          {
            if ((existingFilters[filter] & zooms.Current) == 0)
              continue;

            int zoom = existingFilters[filter] & zooms.Current;
            int startZoom = -1, endZoom = 0;
            for (int zi = 0; zi <= 22; zi++)
            {
              if ((zoom & (1 << zi)) != 0)
              {
                if (startZoom == -1)
                  startZoom = zi;
                endZoom = zi;
              }
            }

            Rule rule = new Rule();
            rule.Filter = ConvertUtility.ToFilter(def.Filters);
            rule.Name = cartoRule.Instance;

            if (startZoom > 0)
              rule.MaxScale = ConvertUtility.ToScaleDenominator(startZoom);
            if (endZoom > 0)
              rule.MinScale = ConvertUtility.ToScaleDenominator(endZoom + 1);

            CreateSymbolizers(rule, symbolizers, env, cartoTranslator);

            existingFilters[filter] &= ~zooms.Current;

            // Check whether the rule has at least one visible symbolizer
            if (rule.Symbolizers.Count > 0 && rule.Symbolizers.Any(s => s.Enabled == true))
               style.Rules.Add(rule);
          }
        }
      }
      
      //style.Rules.Sort(new RuleComparer());
    }

    private static void CreateSymbolizers(Rule rule, Dictionary<string, Dictionary<string, CartoRule>> symbolizers, Env env, ICartoTranslator cartoTranslator)
    {
      // Sort symbolizers by the index of their first property definition
      List<KeyValuePair<string, int>> symOrder = new List<KeyValuePair<string, int>>();
      List<int> indexes = new List<int>();

      foreach (string key in symbolizers.Keys)
      {
        int minIdx = int.MaxValue;

        var props = symbolizers[key];
        foreach (var prop in props)
        {
          int index = props[prop.Key].Index;
          if (index < minIdx)
            minIdx = index;
        }

        symOrder.Add(new KeyValuePair<string, int>(key, minIdx));
      }

      // Get a simple list of the symbolizers, in order
      symOrder.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b) { return a.Value.CompareTo(b.Value); });

      foreach(KeyValuePair<string, int> kv in symOrder)
      {
        var attributes = symbolizers[kv.Key];
        var symbolizer = kv.Key.Split('/')[1];

        // Skip the magical * symbolizer which is used for universal properties
        // which are bubbled up to Style elements intead of Symbolizer elements.
        if (symbolizer == "*")
          continue;

        // Check if we have all required properties

        string[] properties = attributes.Keys.ToArray();
        string[] values = new string[properties.Length];

        string missingProperty = null;
        bool succ = cartoTranslator.HasRequiredProperties(symbolizer, properties, ref missingProperty);

        if (!succ)
        {
          NodeLocation loc = attributes[missingProperty].Location;
          throw new Exception(string.Format("A required property '{0}' is missing. Location: Index = {1} ; Source = {2}", missingProperty, loc.Index, loc.Source));
        }

        int i = 0;
        foreach (string propName in attributes.Keys)
        {
          CartoRule cartoRule = attributes[propName];
          values[i] = cartoRule.Value.GetValueAsString(env);
          i++;
        }

        Symbolizer sym = cartoTranslator.ToSymbolizer(symbolizer, properties, values);
        if (sym != null && sym.Enabled)
          rule.Symbolizers.Add(sym);
      }
    }

    private static Dictionary<string, Dictionary<string, CartoRule>> CollectSymbolizers(CartoDefinition def, ZoomStruct zooms, int i, Env env, ICartoTranslator cartoTranslator)
    {
      Dictionary<string, Dictionary<string, CartoRule>> symbolizers = new Dictionary<string, Dictionary<string, CartoRule>>();

      NodeList<CartoRule> rules = def.Rules;
      for (int j = i; j < rules.Count; j++)
      {
        CartoRule child = rules[j];
        string symName = cartoTranslator.GetSymbolizerName(child.Name);
        if (symName == null)
          continue;

        string key = child.Instance + "/" + symName;
        if ((zooms.Current & child.Zoom) != 0 &&
           (!(symbolizers.ContainsKey(key)) || (symbolizers[key] != null && !(symbolizers[key].ContainsKey(child.Name)))))
        {
          zooms.Current &= child.Zoom;
          if (!(symbolizers.ContainsKey(key)))
            symbolizers[key] = new Dictionary<string, CartoRule>();

          symbolizers[key][child.Name] = child;
        }
      }

      if (symbolizers.Count > 0)
      {
        zooms.Rule &= (zooms.Available &= ~zooms.Current);

        return symbolizers;
      }

      return null;
    }

    /// <summary>
    /// Detects font sets by analyzing all "text-face-name" properties.
    /// </summary>
    /// <param name="definitions"></param>
    /// <param name="env"></param>
    public static void SetFontSets(Map map, List<CartoDefinition> definitions, Env env, ICartoTranslator cartoTranslator)
    {
      List<FontSet> fontSets = new List<FontSet>();
      Dictionary<string, FontSet> dictFontSets = new Dictionary<string, FontSet>();
      
      using (FontFamilyCollection fontCollection = FontUtility.GetFontFamilyCollection())
      {
        List<FontFamilyTypeface> typeFaces = new List<FontFamilyTypeface>();
        List<string> notFoundFontNames = new List<string>();

        int fontSetCounter = 0;

        foreach (CartoDefinition def in definitions)
        {
          foreach (CartoRule rule in def.Rules)
          {
            if (rule.Name == "text-face-name" || rule.Name == "shield-face-name")
            {
              try
              {
                Value value = rule.Value as Value;
                string strFontNames = value.Evaluate(env).ToString().Replace("\"", "");

                FontSet fontSet = null;
                if (_dictFontSets.TryGetValue(strFontNames, out fontSet))
                {
                  if (!fontSets.Contains(fontSet))
                  {
                    fontSets.Add((FontSet)fontSet.Clone());
                    dictFontSets.Add(strFontNames, fontSet);
                  }

                  continue;
                }

                string[] fontNames = strFontNames.Split(',');

                for (int i = 0; i < fontNames.Length; i++)
                {
                  fontNames[i] = fontNames[i].Trim('\'').Trim();
                }

                typeFaces.Clear();

                foreach (string fontName in fontNames)
                {
                  string fName = fontName.Replace(" ", "");
                  FontFamilyTypeface typeFace = null;

                  foreach (MapSurfer.Drawing.FontFamily fontFamily in fontCollection.FontFamilies)
                  {
                    if (typeFace != null)
                      break;

                    foreach (FontFamilyTypeface fft in fontFamily.GetTypefaces())
                    {
                      foreach (string faceName in fft.FaceNames)
                      {
                        string fullName = (fontFamily.Name + faceName).Replace(" ", "");

                        if (string.Equals(fName, fullName, StringComparison.OrdinalIgnoreCase))
                        {
                          typeFace = fft;
                          break;
                        }
                      }
                    }
                  }

                  // We were not able to find appropriate type face
                  if (typeFace == null)
                  {
                    if (!notFoundFontNames.Contains(fontName))
                    {
                      notFoundFontNames.Add(fontName);
                      LogFactory.WriteLogEntry(Logger.Default, string.Format("Unable to find font face(s) for '{0}'", fontName), LogEntryType.Warning);
                    }
                  }
                  else
                  {
                    typeFaces.Add(typeFace);
                  }
                }

                string fontSetName = "fontset-" + fontSetCounter.ToString();

                if (typeFaces.Count > 0)
                {
                  FontSet fs = new FontSet(fontSetName, typeFaces[0].Style, typeFaces[0].Weight);
                  foreach (FontFamilyTypeface fft in typeFaces)
                  {
                    fs.FontNames.Add(fft.FontFamily.Name);
                  }

                  fontSets.Add((FontSet)fs.Clone());
                  _dictFontSets.TryAdd(strFontNames, fs);
                  dictFontSets.Add(strFontNames, fs);

                  fontSetCounter++;
                }
              }
              catch
              { }
            }
          }
        }
      }

      if (fontSets.Count > 0)
        map.FontSets.AddRange(fontSets);

      cartoTranslator.FontSets = dictFontSets;
    }

    private static void SetMapProperties(Map map, Dictionary<string, string> properties, ICartoTranslator cartoTranslator)
    {
      if (properties.Count == 0)
        return;

      string paramValue;

      // ************************* Set background properties *************************
      Background background = map.Background;
      if (properties.TryGetValue("background-color", out paramValue))
        background.ColorHtml = paramValue;

      if (properties.TryGetValue("background-opacity", out paramValue))
        background.Opacity = Convert.ToSingle(paramValue, CultureInfo.InvariantCulture);

      // TODO more image stuff
      if (properties.TryGetValue("background-image-file", out paramValue) || properties.TryGetValue("background-image", out paramValue))
      {
        background.Image.FileName = cartoTranslator.ToPath(paramValue);
      }

      // ************************* Set render modes *************************

      RenderModes renderModes = map.RenderModes;
      if (properties.TryGetValue("render-modes-bidi", out paramValue))
      {
        renderModes.BidiMode = Convert.ToBoolean(paramValue);
      }

      if (properties.TryGetValue("render-modes-kerning", out paramValue))
      {
        renderModes.Kerning = Convert.ToBoolean(paramValue);
      }

      if (properties.TryGetValue("render-modes-scale-factor", out paramValue))
      {
        renderModes.ScaleFactor = Convert.ToSingle(paramValue);
      }

      if (properties.TryGetValue("render-modes-text-abbreviation", out paramValue))
      {
        renderModes.TextAbbreviation = Convert.ToBoolean(paramValue);
      }

      if (properties.TryGetValue("render-modes-text-contrast", out paramValue))
      {
        renderModes.TextContrast = Convert.ToInt32(paramValue);
      }

      if (properties.TryGetValue("render-modes-image-resampling", out paramValue))
      {
        renderModes.ImageResamplingMode = EnumExtensions.GetValueByName<ImageResamplingMode>(typeof(ImageResamplingMode), paramValue, StringComparison.OrdinalIgnoreCase);
      }

      if (properties.TryGetValue("render-modes-smoothing", out paramValue))
      {
        renderModes.SmoothingMode = EnumExtensions.GetValueByName<SmoothingMode>(typeof(SmoothingMode), paramValue, StringComparison.OrdinalIgnoreCase);
      }

      if (properties.TryGetValue("render-modes-text-rendering", out paramValue))
      {
        renderModes.TextRenderingMode = EnumExtensions.GetValueByName<TextRenderMode>(typeof(TextRenderMode), paramValue, StringComparison.OrdinalIgnoreCase);
      }

      // ************************* Set label settings *************************

      LabelPlacementProblemSettings labelSettings = map.LabelPlacementSettings;
      if (properties.TryGetValue("label-settings-solver", out paramValue))
      {
        labelSettings.Solver = paramValue;
        labelSettings.SolverParameters = GetParameters(properties, "label-settings-solver-param");
      }

      if (properties.TryGetValue("label-settings-accuracy", out paramValue))
      {
        labelSettings.AccuracyMode = EnumExtensions.GetValueByName<MapSurfer.Labeling.CandidatePositionGenerators.LabelPlacementAccuracyMode>(typeof(MapSurfer.Labeling.CandidatePositionGenerators.LabelPlacementAccuracyMode), paramValue, StringComparison.OrdinalIgnoreCase);
      }

      if (properties.TryGetValue("label-settings-candidate-position-generator", out paramValue))
      {
        labelSettings.CandidatePositionGenerator = paramValue;
        labelSettings.CandidatePositionGeneratorParameters = GetParameters(properties, "label-settings-candidate-position-generator-param");
      }

      if (properties.TryGetValue("label-settings-collision-detector", out paramValue))
      {
        labelSettings.CollisionDetector = paramValue;
        labelSettings.CollisionDetectorParameters = GetParameters(properties, "label-settings-collision-detector-param");
      }

      string[] values = GetStringValues(properties, "label-settings-quality-evaluator-param");
      if (values.Length > 0)
      {
        labelSettings.QualityEvaluators.Clear();
        labelSettings.QualityEvaluators.AddRange(values);
      }

      // ************************* Font files *******************************

      values = GetStringValues(properties, "font-files-file");

      if (values.Length > 0)
      {
        map.FontFiles.AddRange(values);
      }

      if (properties.TryGetValue("font-directory", out paramValue))
      {
        if (Directory.Exists(paramValue))
        {
          foreach (string fileName in Directory.EnumerateFiles(paramValue))
          {
            map.FontFiles.Add(fileName);
          }
        }
      }

      // ************************* Font sets ******************************* 

      //TODO

      // ************************* Scales *************************

      if (properties.TryGetValue("minimum-scale", out paramValue))
        map.MinimumScale = Convert.ToDouble(paramValue, CultureInfo.InvariantCulture);
      if (properties.TryGetValue("maximum-scale", out paramValue))
        map.MaximumScale = Convert.ToDouble(paramValue, CultureInfo.InvariantCulture);

      // ************************* Buffers *************************
      int size = 256;
      if (properties.TryGetValue("buffer-size", out paramValue))
        size = Convert.ToInt32(paramValue);
      
     	map.Padding = new Size(size, size);

      if (properties.TryGetValue("buffer-image-size", out paramValue)) // Extension
      {
        size = Convert.ToInt32(paramValue);
        map.Buffer = new Size(size, size);
      }
    }

    /// <summary>
    /// Apply inherited styles from their ancestors to them.
    /// </summary>
    /// <param name="definitions"></param>
    /// <param name="env"></param>
    /// <returns>an array of arrays is returned, in which each array refers to a specific attachment</returns>
    private static List<CartoStyle> InheritDefinitions(List<CartoDefinition> definitions, Env env)
    {
      // definitions are ordered by specificity,
      // high (index 0) to low
      Dictionary<string, CartoStyle> byAttachment = new Dictionary<string, CartoStyle>();
      Dictionary<string, SortedDictionary<string, CartoDefinition>> byFilter = new Dictionary<string, SortedDictionary<string, CartoDefinition>>();
      List<CartoStyle> result = new List<CartoStyle>();
      List<CartoDefinition> current;
      string attachment;

      // Evaluate the filters specified by each definition with the given
      // environment to correctly resolve variable references
      definitions.ForEach(delegate(CartoDefinition def) {
           def.Filters.Evaluate(env);
      });

      current = new List<CartoDefinition>();

      for (int i = 0; i < definitions.Count; i++)
      {
      	CartoDefinition defI = definitions[i];
        attachment = defI.Attachment;

        current.Clear();
        current.Add(defI);

        if (!byAttachment.ContainsKey(attachment))
        {
          CartoStyle style = new CartoStyle();
          style.Attachment = attachment;

          byAttachment.Add(attachment, style);
          byAttachment[attachment].Attachment = attachment;
          byFilter[attachment] = new SortedDictionary<string, CartoDefinition>();
          
          result.Add(byAttachment[attachment]);
        }

        // Iterate over all subsequent rules.
        for (var j = i + 1; j < definitions.Count; j++)
        {
        	CartoDefinition defJ = definitions[j];
          if (defJ.Attachment == attachment)
          {
              // Only inherit rules from the same attachment.
              current = AddRules(current, defJ, byFilter[attachment], env);
          }
        }

        for (var k = 0; k < current.Count; k++)
        {
          byFilter[attachment][current[k].Filters.ToString()] = current[k];
          byAttachment[attachment].Add(current[k]);
        }
      }

      return result;
    }

    private static List<CartoDefinition> AddRules(List<CartoDefinition> current, CartoDefinition definition, SortedDictionary<string, CartoDefinition> byFilter, Env env)
    {
      var newFilters = definition.Filters;
      var newRules = definition.Rules;
      object updatedFilters;
      CartoDefinition clone, previous;

      // The current definition might have been split up into
      // multiple definitions already.
      for (var k = 0; k < current.Count; k++)
      {
        updatedFilters = current[k].Filters.CloneWith(newFilters, env);
        if (updatedFilters is CartoFilterSet)
        {
        	string filtersString = (updatedFilters as CartoFilterSet).ToString();
        	
          if (!byFilter.TryGetValue(filtersString, out previous))
             previous = null;

          if (previous != null)
          {
            // There's already a definition with those exact
            // filters. Add the current definitions' rules
            // and stop processing it as the existing rule
            // has already gone down the inheritance chain.
            previous.AddRules(newRules);
          }
          else
          {
            clone = (CartoDefinition)current[k].Clone((CartoFilterSet)updatedFilters, env);
            // Make sure that we're only maintaining the clone
            // when we did actually add rules. If not, there's
            // no need to keep the clone around.
            if (clone.AddRules(newRules) > 0)
            {
              // We inserted an element before this one, so we need
              // to make sure that in the next loop iteration, we're
              // not performing the same task for this element again,
              // hence the k++.
              byFilter.Add(filtersString, clone);
              current.Insert(k, clone);
              k++;
            }
          }
        }
        else if (updatedFilters == null)
        {
          // if updatedFilters is null, then adding the filters doesn't
          // invalidate or split the selector, so we addRules to the
          // combined selector

          // Filters can be added, but they don't change the
          // filters. This means we don't have to split the
          // definition.
          //
          // this is cloned here because of shared classes, see
          // sharedclass.mss
          current[k] = (CartoDefinition)current[k].Clone(env);
          current[k].AddRules(newRules);
        }
        // if updatedFeatures is false, then the filters split the rule,
        // so they aren't the same inheritance chain
      }

      return current;
    }

    /// <summary>
    /// Sort styles by the minimum index of their rules.
    /// This sorts a slice of the styles, so it returns a sorted
    /// array but does not change the input.
    /// </summary>
    /// <param name="styles"></param>
    /// <param name="env"></param>
    private static void SortStyles(List<CartoStyle> styles, Env env)
    {
      if (styles.Count == 0)
        return;

      for (var i = 0; i < styles.Count; i++)
      {
        CartoStyle style = styles[i];
        style.Index = int.MaxValue;

        for (int b = 0; b < style.Count; b++)
        {
          NodeList<CartoRule> rules = style[b].Rules;
          for (var r = 0; r < rules.Count; r++)
          {
            CartoRule rule = rules[r];
            if (rule.Index < style.Index)
              style.Index = rule.Index;
          }
        }
      }

      styles.Sort(delegate (CartoStyle a, CartoStyle b) { return a.Index.CompareTo(b.Index); });
    }

    private static ParameterCollection GetParameters(Dictionary<string, string> dict, string template)
    {
      ParameterCollection parameters = new ParameterCollection();

      int tempLength = template.Length;

      foreach (string key in dict.Keys)
      {
        int pos = key.IndexOf(template);
        if (pos >= 0)
        {
          string value = dict[key];

          if (key.Contains("key"))
          {
            string key1 = key.Replace("key", "value");
            //  label-settings-solver-param0-key: CoolingSchedule;
            //  label-settings-solver-param0-value: AartsLaarhoven;
 
            if (dict.ContainsKey(key1))
            {
              string value1 = dict[key1];

              Parameter parameter = new Parameter(value, value1);
              parameters.Add(parameter);
            }
          }
        }
      }

      return parameters;
    }

    private static string[] GetStringValues(Dictionary<string, string> dict, string template)
    {
      List<string> values = new List<string>();

      int tempLength = template.Length;

      foreach (string key in dict.Keys)
      {
        int pos = key.IndexOf(template);
        if (pos >= 0)
        {
          string value = dict[key];
          values.Add(value);
        }
      }

      return values.ToArray();
    }

    private static Dictionary<string, string> GetProperties(List<CartoDefinition> definitions, Env env, string elementName)
    {
      Dictionary<string, string> props = new Dictionary<string, string>();

      foreach (CartoDefinition def in definitions)
      {
        if (def.Elements.Count == 1 && def.Elements[0].ToCSS(env).Trim() == elementName)
        {
          foreach (CartoRule rule in def.Rules)
          {
            if (!props.ContainsKey(rule.Name))
              props.Add(rule.Name, rule.Value.ToCSS(env));
          }
        }
      }

      return props;
    }
  }
}
