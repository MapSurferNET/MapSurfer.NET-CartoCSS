//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
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
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

using ProjNet.CoordinateSystems;

using dotless.Core.Parser;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Tree;

using MapSurfer.Logging;
using MapSurfer.Layers;
using MapSurfer.Drawing;
using MapSurfer.Configuration;
using MapSurfer.ComponentModel;
using MapSurfer.Labeling;
using MapSurfer.Drawing.Drawing2D;
using MapSurfer.Styling.Formats.CartoCSS.Exceptions;
using MapSurfer.CoordinateSystems;
using MapSurfer.CoordinateSystems.Transformations;
using MapSurfer.Threading.Tasks;
using MapSurfer.Collections.Generic;
using MapSurfer.Styling.Formats.CartoCSS.Parser;
using MapSurfer.Styling.Formats.CartoCSS.Parser.Infrastructure;
using MapSurfer.Styling.Formats.CartoCSS.Parser.Tree;
using MapSurfer.Styling.Formats.CartoCSS.Translators;

using Parameter = MapSurfer.Configuration.Parameter;
using MapSurfer.Data;
using MapSurfer.Geometries;
using MapSurfer.Utilities;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  public static class CartoProcessor
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
    private static ObjectPool<ICartoTranslator> _translatorsPool = new ObjectPool<ICartoTranslator>(CartoGeneratorConverterFactory.CreateTranslator, true, true);

    public static void UpdateMap(Map map, CartoProject cartoProject, Dictionary<string, string> stylesheets, string path, IProgressIndicator progress, Logger logger, int threads = -1)
    {
      ICartoTranslator cartoTranslator = _translatorsPool.GetObject(cartoProject.Extensions.Translator);

      CartoReaderContext cntx = new CartoReaderContext(new Env(), cartoTranslator);

      UpdateMapInternal(map, cartoProject, stylesheets, path, cntx, progress, logger, threads);

      _translatorsPool.ReturnObject(cartoTranslator);
    }

    private static void UpdateMapInternal(Map map, CartoProject cartoProject, Dictionary<string, string> stylesheets, string path, CartoReaderContext cntx, IProgressIndicator progress, Logger logger, int threads = -1)
    {
      ICartoTranslator cartoTranslator = cntx.Translator;
      cartoTranslator.SetLogger(logger);
      Env env = cntx.Env;

      CartoParser parser = new CartoParser();
      parser.NodeProvider = new CartoNodeProvider();
      
      List<Ruleset> ruleSets = new List<Ruleset>();
      List<CartoDefinition> definitions = new List<CartoDefinition>();

      if (progress != null)
      {
        progress.TaskPercentage(10);
        progress.SubTaskText = SR.GetString("Strings.ParseProject");
        progress.SubTaskPercentage(-1);
      }

      foreach (KeyValuePair<string, string> kvStylesheet in stylesheets)
      {
        string styleFileName = path == null ? kvStylesheet.Key : Path.Combine(path, kvStylesheet.Key);

        try
        { 
          Ruleset ruleSet = parser.Parse(kvStylesheet.Value, styleFileName, env);
          env.Frames.Push(ruleSet);
          ruleSets.Add(ruleSet);

          // Get an array of Ruleset objects, flattened
          // and sorted according to specificitySort
          var defs = new List<CartoDefinition>();
          defs = ruleSet.Flatten(defs, null, env, logger);
          defs.Sort(new SpecificitySorter());

          definitions.AddRange(defs);

        }
        catch (Exception ex)
        {
          ParsingException ex2 = CreateParsingException(ex, kvStylesheet.Key);

          LogFactory.WriteLogEntry(logger, ex2);
        }
      }

      if (progress != null)
        progress.TaskPercentage(20);

      string interactivityLayer = null;
      if (cartoProject.GetInteractivity() != null && cartoProject.GetInteractivity().ContainsKey("layer"))
        interactivityLayer = cartoProject.GetInteractivity()["layer"].ToString();

      SetMapProperties(map, GetProperties(definitions, cntx.Env, "Map"), cntx.Translator, path);
      SetFontSets(map, definitions, cntx);

      int layerCounter = 0;
      object lockObj = new object();
      // System.Text.StringBuilder sb = new System.Text.StringBuilder();

      LayerCollection mapLayers = GetMapLayers(map);
      CartoLayer[] layers = GetCartoLayers(cartoProject);

      ParallelUtility.ForEach(layers, threads, cartoLayer =>
      {
        layerCounter = System.Threading.Interlocked.Increment(ref layerCounter);

        if (progress != null)
        {
          lock (lockObj)
          {
            int p = (int)((100 * layerCounter) / (float)layers.Length); 
            progress.TaskPercentage(20 + (int)(60 * p / 100F));
            progress.SubTaskText = string.Format(SR.GetString("Strings.PreapareLayer.Format"), layerCounter, layers.Length, cartoLayer.Name);
            progress.SubTaskPercentage(p);
          }
        }

        if (!string.Equals(cartoLayer.Group, "true", StringComparison.OrdinalIgnoreCase))
        {
          CartoDatasource datasource = cartoLayer.Datasource;
          StyledLayer styledLayer = null;

          lock (lockObj)
          {
            Layer layer = mapLayers.FindLayerByName(cartoLayer.Name);

            if (layer is StyledLayer)
            {
              styledLayer = (StyledLayer)layer;
              styledLayer.Styles.Clear();
            }

            if (styledLayer == null)
            {
              layer = CreateLayer(cartoLayer, cartoTranslator, logger);

              if (layer is StyledLayer)
              {
                styledLayer = (StyledLayer)layer;
                mapLayers.Add(layer);
              }
            }
          }

          if (styledLayer != null)
          {
            try
            {
              // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
              //  sw.Start();

              string[] classes = (cartoLayer.Class.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
              Dictionary<string, bool> classIndex = new Dictionary<string, bool>(classes.Length);
              for (int i = 0; i < classes.Length; i++)
                classIndex[classes[i]] = true;

              var matching = FindDefinitions(definitions, cartoLayer.Name, classIndex);

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
                    FeatureTypeStyle style = CreateStyle(styleName, cartoStyle, cntx);

                    if (style.Rules.Count > 0)
                      styledLayer.Styles.Add(style);
                  }

                  cartoTranslator.ProcessStyles(styledLayer.Styles);
                }

                if (!string.IsNullOrEmpty(interactivityLayer) && interactivityLayer.Equals(styledLayer.Name))
                  styledLayer.Enabled = false;

                //sw.Stop();
              }
            }
            catch (Exception ex)
            {
              LogFactory.WriteLogEntry(logger, ex);
            }
          }
        }
      });

      // Populate layers by taking groups into account.
      map.Layers.Clear();

      List<string> childLayers = new List<string>();

      foreach (CartoLayer cartoLayer in layers)
      {
        if (string.Equals(cartoLayer.Group, "true", StringComparison.OrdinalIgnoreCase))
        {
          GroupLayer groupLayer = new GroupLayer(cartoLayer.Name);

          if (cartoLayer.Layers != null)
          {
            foreach (string childName in cartoLayer.Layers)
            {
              Layer layer = mapLayers.FindLayerByName(childName);
              if (layer != null)
                groupLayer.ChildLayers.Add(layer);
              childLayers.Add(childName);
            }
          }
          map.AddLayer(groupLayer);
        }
        else
        {
          if (!childLayers.Contains(cartoLayer.Name))
          {
            Layer layer = mapLayers.FindLayerByName(cartoLayer.Name);
            if (layer != null)
              map.AddLayer(layer);
          }
        }
      }

      if (progress != null)
      {
        progress.TaskPercentage(100);
        progress.SubTaskText = string.Empty;
      }
    }

    public static Map GetMap(string path, IProgressIndicator progress, int threads = 1)
    {
      return GetMap(path, progress, Logger.Default, threads);
    }

    public static Map GetMap(string path, IProgressIndicator progress, Logger logger = null, int threads = 1)
    {
      CartoProject cartoProject = CartoProject.FromFile(File.ReadAllText(path), Path.GetExtension(path));
      return CartoProcessor.GetMap(cartoProject, Path.GetDirectoryName(path), progress, logger, threads);
    }

    public static Map GetMap(CartoProject cartoProject, string path, IProgressIndicator progress, Logger logger = null, int threads = 1)
    {
      Map map = null;

      ICartoTranslator cartoTranslator = _translatorsPool.GetObject(cartoProject.Extensions.Translator);

      if (path == null)
      {
        map = CreateMap(cartoProject, new CartoReaderContext(new Env(), cartoTranslator));
      }
      else if (cartoProject.Stylesheet != null && cartoProject.Stylesheet.Length > 0)
      {
        Dictionary<string, string> stylesheets = new Dictionary<string, string>();

        foreach (string styleName in cartoProject.Stylesheet)
        {
          string styleFileName = Path.Combine(path, styleName);

          if (!stylesheets.ContainsKey(styleName))
           stylesheets.Add(styleName, File.ReadAllText(styleFileName));
        }

        CartoReaderContext cntx = new CartoReaderContext(new Env(), cartoTranslator);

        map = CreateMap(cartoProject, cntx);

        UpdateMapInternal(map, cartoProject, stylesheets, path, cntx, progress, logger, threads);
      }

      _translatorsPool.ReturnObject(cartoTranslator);

      return map;
    }

    /// <summary>
    /// Creates map object from <see cref="CartoProject"/>
    /// </summary>
    /// <param name="project"></param>
    /// <param name="definitions"></param>
    /// <param name="cntx"></param>
    /// <returns></returns>
    private static Map CreateMap(CartoProject project, CartoReaderContext cntx)
    {
      Map map = new Map(project.Name);
      map.Background.Opacity = 0.0F;
      map.Size = new SizeF(700, 500);
      map.MinimumScale = ConvertUtility.ToScaleDenominator(project.MinZoom);
      map.MaximumScale = ConvertUtility.ToScaleDenominator(project.MaxZoom);
      map.Name = project.Name;
      map.RenderModes.ScaleFactor = project.Scale;
      map.RenderModes.TextAbbreviation = project.Extensions.TextAbbreviation;
      map.RenderModes.Kerning = project.Extensions.Kerning;
      //map.RenderModes.ImageResamplingMode = project.ImageResampling;
      ///N, E, S, W, NE, SE, NW, SW
      map.LabelPlacementSettings.PointPlacementPrioritization = new LabelAlignment[] {
      ///  LabelAlignment.MiddleCenter,
        LabelAlignment.TopCenter,
        LabelAlignment.MiddleRight,
        LabelAlignment.BottomCenter,
        LabelAlignment.MiddleLeft,
        LabelAlignment.TopRight,
        LabelAlignment.BottomRight,
        LabelAlignment.TopLeft,
        LabelAlignment.BottomLeft };

      if (string.Equals(project.Extensions.Translator, "mapnik"))
        map.RenderModes.StronglyTypedExpressions = false;

      // ************************* Set label settings *************************

      Dictionary<string, string> dictLabelingOpts = project.Extensions.LabelingOptions;
      if (dictLabelingOpts != null && dictLabelingOpts.Count > 0)
      {
        LabelPlacementProblemSettings labelSettings = map.LabelPlacementSettings;

        string paramValue = null;
        if (dictLabelingOpts.TryGetValue("solver", out paramValue))
        {
          labelSettings.Solver = paramValue;
          labelSettings.SolverParameters = GetParameters(dictLabelingOpts, "solver-param");
        }

        if (dictLabelingOpts.TryGetValue("accuracy-mode", out paramValue))
          labelSettings.AccuracyMode = EnumExtensions.GetValueByName<MapSurfer.Labeling.CandidatePositionGenerators.LabelPlacementAccuracyMode>(typeof(MapSurfer.Labeling.CandidatePositionGenerators.LabelPlacementAccuracyMode), paramValue, StringComparison.OrdinalIgnoreCase);

        if (dictLabelingOpts.TryGetValue("candidate-position-generator", out paramValue))
        {
          labelSettings.CandidatePositionGenerator = paramValue;
          labelSettings.CandidatePositionGeneratorParameters = GetParameters(dictLabelingOpts, "candidate-position-generator-param");
        }

        if (dictLabelingOpts.TryGetValue("collision-detector", out paramValue))
        {
          labelSettings.CollisionDetector = paramValue;
          labelSettings.CollisionDetectorParameters = GetParameters(dictLabelingOpts, "collision-detector-param");
        }

        string[] values = GetStringValues(dictLabelingOpts, "quality-evaluator-param");
        if (values.Length > 0)
        {
          labelSettings.QualityEvaluators.Clear();
          labelSettings.QualityEvaluators.AddRange(values);
        }

        if (dictLabelingOpts.TryGetValue("threads-number", out paramValue))
          labelSettings.ThreadsCount = Convert.ToInt32(paramValue);

        if (dictLabelingOpts.TryGetValue("point-prioritization", out paramValue))
        {
          values = paramValue.Split(',');
          LabelAlignment[] aligns = new LabelAlignment[values.Length];
          for (int i = 0; i < values.Length; i++)
            aligns[i] = EnumExtensions.GetValueByName<LabelAlignment>(typeof(LabelAlignment), values[i], StringComparison.OrdinalIgnoreCase);
        }
      }

      ICartoTranslator cartoTranslator = cntx.Translator;
      // if (project.Bounds != null)
      //   map.WGS84Bounds = project.Bounds[0] + "," + project.Bounds[1] + "," + project.Bounds[2] + "," + project.Bounds[3];
      map.CRS = cartoTranslator.ToCoordinateSystem(string.IsNullOrEmpty(project.Srs) ? project.SrsName : project.Srs, !string.IsNullOrEmpty(project.SrsName));

      if (project.Center != null)
      {
        if (map.CoordinateSystem == null)
          map.CoordinateSystem = (CoordinateSystem)new CoordinateSystemFactory().CreateSphericalMercatorCoordinateSystem();

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

    private static bool IsVectorTilesLayer(CartoLayer layer)
    {
      if (layer.Group == "true" && layer.Datasource != null && string.Equals(layer.Datasource["type"], "VectorTiles", StringComparison.OrdinalIgnoreCase))
        return true;
      else
        return false;
    }

    private static CartoLayer[] GetCartoLayers(CartoProject project)
    {
      List<CartoLayer> result = new List<CartoLayer>();
      
      foreach (CartoLayer layer in project.Layers)
      {
        result.Add(layer);

        if (IsVectorTilesLayer(layer))
        {
          VectorTilesSource vtSource = VectorTilesSource.FromSource(layer.Datasource["source"]);
          if (vtSource != null)
          {
            string bounds = vtSource.Bounds != null ? vtSource.Bounds[0] + "," + vtSource.Bounds[1] + "," + vtSource.Bounds[2] + "," + vtSource.Bounds[3] : string.Empty;

            List<string> layerNames = new List<string>(vtSource.VectorLayers.Length);

            foreach (VectorLayer vtLayer in vtSource.VectorLayers)
            {
              vtLayer.Owner = vtSource;
              layerNames.Add(vtLayer.Id);

              CartoLayer layer2 = new CartoLayer();
              layer2.Name = vtLayer.Id;
              layer2.Id = vtLayer.Id;
              layer2.Properties = new Dictionary<string, string>();
              layer2.Properties.Add("minzoom", vtLayer.MinZoom.ToString());
              layer2.Properties.Add("maxzoom", vtLayer.MaxZoom.ToString());
              layer2.Datasource = new CartoDatasource();
              layer2.Datasource.Add("type", "vectortiles");
              layer2.Datasource.Add("compression", layer.Datasource["compression"] == null ? "GZip": layer.Datasource["compression"]);
             // layer2.Datasource.Add("datasource", StringExtensions.Combine(vtSource.Tiles, ";"));
              if (!string.IsNullOrEmpty(bounds))
               layer2.Datasource.Add("extent", bounds);
              layer2.Datasource.Add("format", vtSource.Format);
              layer2.Datasource.Add("layers", vtLayer.Id);
              //              layer2.Datasource.Add("LittleEndian", vtSource.Format);
              layer2.Datasource.Add("minzoom", vtLayer.MinZoom);
              layer2.Datasource.Add("maxzoom", vtLayer.MaxZoom);
              layer2.Datasource.Add("mode", "WebService"); // TODO

              if (vtLayer.Fields != null)
              {
                string props = string.Empty;
                int nFields = vtLayer.Fields.Count;
                int i = 0;
                foreach (KeyValuePair<string, string> kv in vtLayer.Fields)
                {
                  props += kv.Key + "$=$" + kv.Value;
                  if (i < nFields - 1)
                    props += "$;$";

                  i++;
                }

                layer2.Datasource.Add("properties", props);
              }

              layer2.Tag = vtLayer;

              result.Add(layer2);
            }

            layer.Layers = layerNames.ToArray();
          }
        }
      }

      return result.ToArray();
    }
    
    private static LayerCollection GetMapLayers(Map map)
    {
      LayerCollection coll = new LayerCollection();

      foreach (Layer layer in map.Layers)
      {
        if (layer is StyledLayer)
          coll.Add(layer);
        else if (layer is GroupLayer)
        {
          GroupLayer groupLayer = layer as GroupLayer;
          foreach (Layer childLayer in groupLayer.ChildLayers)
          {
            if (childLayer is StyledLayer)
              coll.Add(childLayer);
          }
        }
      }

      return coll;
    }

    private static List<CartoDefinition> FindDefinitions(List<CartoDefinition> defs, string layerName, Dictionary<string, bool> classIndex)
    {
      List<CartoDefinition> result = new List<CartoDefinition>(defs.Count / 2);

      for (int i = 0; i < defs.Count; i++)
      {
        CartoDefinition def = defs[i];
        if (def.AppliesTo(layerName, classIndex))
          result.Add(def);
      }

      return result;
    }

    /// <summary>
    /// Create styled layer
    /// </summary>
    /// <param name="cartoLayer"></param>
    /// <param name="map"></param>
    /// <param name="cartoTranslator"></param> 
    /// <returns></returns>
    private static Layer CreateLayer(CartoLayer cartoLayer, ICartoTranslator cartoTranslator, Logger logger)
    {
      ParameterCollection parameters = cartoTranslator.ToDatasourceParameters(cartoLayer.Datasource);
      Layer layer = null;


      // hack
      if (string.Equals(parameters.GetParameterByName("Type").Value, "GeoJson", StringComparison.OrdinalIgnoreCase))
        cartoLayer.Srs = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";

      StyledLayer styledLayer = new StyledLayer(cartoLayer.Name, parameters);
      styledLayer.FeaturesCaching.Enabled = false;

      Dictionary<string, object> propsExt = new Dictionary<string, object>();
      propsExt.Add("Class", cartoLayer.Class);
      styledLayer.Tag = propsExt;

      Dictionary<string, string> propsLayer = cartoLayer.Properties;
      if (propsLayer != null)
      {
        string paramValue = null;
        if (propsLayer.TryGetValue("minzoom", out paramValue))
          styledLayer.MaximumScale = ConvertUtility.ToScaleDenominator(Convert.ToInt32(paramValue));
        if (propsLayer.TryGetValue("maxzoom", out paramValue))
          styledLayer.MinimumScale = ConvertUtility.ToScaleDenominator(Convert.ToInt32(paramValue));
        if (propsLayer.TryGetValue("queryable", out paramValue))
          styledLayer.Queryable = Convert.ToBoolean(paramValue);
        if (propsLayer.TryGetValue("padding", out paramValue)) // Extension
        {
          string padding = paramValue;
          if (!string.IsNullOrEmpty(padding))
          {
            string[] paddingValues = padding.Split(new char[] { ',' });
            if (paddingValues.Length == 2)
              styledLayer.Padding = new Size(Convert.ToInt32(paddingValues[0]), Convert.ToInt32(paddingValues[1]));
          }
        }
        if (propsLayer.TryGetValue("cache-features", out paramValue)) // Extension
          styledLayer.FeaturesCaching.Enabled = Convert.ToBoolean(paramValue);
        if (propsLayer.TryGetValue("cache-features-minscale", out paramValue)) // Extension
          styledLayer.FeaturesCaching.MinimumScale = Convert.ToDouble(paramValue);
        if (propsLayer.TryGetValue("cache-features-maxscale", out paramValue)) // Extension
          styledLayer.FeaturesCaching.MaximumScale = Convert.ToDouble(paramValue);

        if (propsLayer.TryGetValue("blend-options-opacity", out paramValue))
          styledLayer.BlendingOptions.Opacity = Convert.ToSingle(paramValue);
        if (propsLayer.TryGetValue("blend-options-comp-op", out paramValue))
        {
          CompositingMode compMode = cartoTranslator.ToCompositingMode(paramValue);
          if (compMode != CompositingMode.SourceOver)
            styledLayer.BlendingOptions.CompositingMode = compMode;
        }
      }

      styledLayer.Enabled = cartoLayer.Status != "off";

      string providerName = parameters.GetValue("Type");
      if (string.IsNullOrEmpty(providerName))
        LogFactory.WriteLogEntry(logger, new IOException(string.Format("Unable to detect the type of a data source provider for the layer '{0}'.", cartoLayer.Name)));

      if (!string.IsNullOrEmpty(cartoLayer.Srs_WKT))
        styledLayer.CRS = cartoLayer.Srs_WKT;
      else if (!string.IsNullOrEmpty(cartoLayer.Srs))
        styledLayer.CRS = cartoTranslator.ToCoordinateSystem(string.IsNullOrEmpty(cartoLayer.Srs) ? cartoLayer.SrsName : cartoLayer.Srs, !string.IsNullOrEmpty(cartoLayer.SrsName));

      layer = styledLayer;


      return layer;
    }

    /// <summary>
    /// Creates <see cref="FeatureTypeStyle"/> object
    /// </summary>
    /// <param name="styleName"></param>
    /// <param name="cartoStyle"></param>
    /// <param name="cntx"></param>
    /// <returns></returns>
    private static FeatureTypeStyle CreateStyle(string styleName, CartoStyle cartoStyle, CartoReaderContext cntx)
    {
      FeatureTypeStyle style = new FeatureTypeStyle(styleName);
      style.ProcessFeatureOnce = true;

      List<CartoDefinition> definitions = cartoStyle.Definitions;
      Dictionary<string, int> existingFilters = null;
      List<string> imageFilters = new List<string>();
      List<string> imageFiltersBuffer = new List<string>();
      ICartoTranslator cartoTranslator = cntx.Translator;
      Env env = cntx.Env;
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

          CreateRules(style, def, existingFilters, cntx);
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
    private static void CreateRules(FeatureTypeStyle style, CartoDefinition def, Dictionary<string, int> existingFilters, CartoReaderContext cntx)
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
          Dictionary<string, Dictionary<string, CartoRule>> symbolizers = CollectSymbolizers(def, zooms, i, cntx);

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
            rule.Filter = ConvertUtility.ToFilter(def.Filters, cntx);
            rule.Name = cartoRule.Instance;

            if (startZoom > 0)
              rule.MaxScale = ConvertUtility.ToScaleDenominator(startZoom);
            if (endZoom > 0)
              rule.MinScale = ConvertUtility.ToScaleDenominator(endZoom + 1);

            CreateSymbolizers(rule, symbolizers, cntx);

            existingFilters[filter] &= ~zooms.Current;

            // Check whether the rule has at least one visible symbolizer
            if (rule.Symbolizers.Count > 0 && rule.Symbolizers.Any(s => s.Enabled == true))
              style.Rules.Add(rule);
          }
        }
      }

      //style.Rules.Sort(new RuleComparer());
    }

    private static void CreateSymbolizers(Rule rule, Dictionary<string, Dictionary<string, CartoRule>> symbolizers, CartoReaderContext cntx)
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
      ICartoTranslator translator = cntx.Translator;

      foreach (KeyValuePair<string, int> kv in symOrder)
      {
        var attributes = symbolizers[kv.Key];
        var symbolizer = kv.Key.Split('/')[1];

        // Skip the magical * symbolizer which is used for universal properties
        // which are bubbled up to Style elements intead of Symbolizer elements.
        if (symbolizer == "*")
          continue;

        // Check if we have all required properties

        NodePropertyValue[] properties = new NodePropertyValue[attributes.Count];

        int i = 0;
        foreach (string propName in attributes.Keys)
        {
          CartoRule cartoRule = attributes[propName];
          try
          {
            NodePropertyValue pv = new NodePropertyValue(cartoRule.Name, cntx.GetValue(cartoRule.Value), cartoRule.Location);
            if (!translator.IsSymbolizerPropertyValid(symbolizer, pv))
            {
              NodeLocation loc = cartoRule.Location;
              throw new ParsingException(string.Format("Unknown property '{0}'.", propName), loc.FileName, Zone.GetLineNumber(loc));
            }
            else
              properties[i] = pv;
          }
          catch(Exception ex)
          {
            throw new ParsingException(ex.Message, cartoRule.Location.FileName, Zone.GetLineNumber(cartoRule.Location));
          }

          i++;
        }

        string missingProperty = null;
        bool succ = translator.HasRequiredProperties(symbolizer, properties, ref missingProperty);

        if (!succ)
        {
          NodeLocation loc = null;
          if (attributes.ContainsKey(missingProperty))
            loc = attributes[missingProperty].Location;
          else
          {
            CartoRule cartoRule = attributes.First().Value;
            loc = cartoRule.Location;
          }

          throw new ParsingException(string.Format("A required property '{0}' is missing.", missingProperty), loc.FileName, Zone.GetLineNumber(loc));
        }

        Symbolizer sym = translator.ToSymbolizer(symbolizer, properties);
        if (sym != null && sym.Enabled)
          rule.Symbolizers.Add(sym);
      }
      
      // Here, we perform some optimizations
      // 1. Since PolygonSymbolizer in MapSurfer.NET has Stroke element, it can be combined with a LineSymbolizer.
      if (rule.Symbolizers.Count == 2)
      {
        PolygonSymbolizer symPoly = rule.Symbolizers[0] as PolygonSymbolizer;
        if (symPoly != null && (symPoly.Stroke.Opacity == 0 || symPoly.Stroke.Width == 0))
        {
          LineSymbolizer symLine = rule.Symbolizers[1] as LineSymbolizer;
          if (symLine != null)
          {
            if (symPoly.Clip == symLine.Clip && string.Equals(symPoly.GeometryExpression, symLine.GeometryExpression, StringComparison.OrdinalIgnoreCase))
            {
              symPoly.Stroke = symLine.Stroke;
              rule.Symbolizers.RemoveAt(1);
            }
          }
        }
      }
    }

    private static Dictionary<string, Dictionary<string, CartoRule>> CollectSymbolizers(CartoDefinition def, ZoomStruct zooms, int i, CartoReaderContext cntx)
    {
      Dictionary<string, Dictionary<string, CartoRule>> symbolizers = new Dictionary<string, Dictionary<string, CartoRule>>();

      NodeList<CartoRule> rules = def.Rules;
      for (int j = i; j < rules.Count; j++)
      {
        CartoRule child = rules[j];
        string symName = cntx.Translator.GetSymbolizerName(child.Name);

        string key = child.Instance + "/" + symName;
        if ((zooms.Current & child.Zoom) != 0 &&
           (!(symbolizers.ContainsKey(key)) || (symbolizers[key] != null && !(symbolizers[key].ContainsKey(child.Name)))))
        {
          zooms.Current &= child.Zoom;

          Dictionary<string, CartoRule> dict = null;
          if (!symbolizers.TryGetValue(key, out dict))
          {
            dict = new Dictionary<string, CartoRule>();
            symbolizers.Add(key, dict);
          }

          dict[child.Name] = child;
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
    /// <param name="cntx"></param>
    private static void SetFontSets(Map map, List<CartoDefinition> definitions, CartoReaderContext cntx)
    {
      map.FontSets.Clear();

      List<FontSet> fontSets = new List<FontSet>();
      Dictionary<string, FontSet> dictFontSets = new Dictionary<string, FontSet>();
      ICartoTranslator cartoTranslator = cntx.Translator;

      using (FontFamilyCollection fontCollection = FontUtility.GetFontFamilyCollection())
      {
        List<FontFamilyTypeface> typeFaces = new List<FontFamilyTypeface>();
        List<string> notFoundFontNames = new List<string>();

        int fontSetCounter = 0;

        foreach (CartoDefinition def in definitions)
        {
          foreach (CartoRule rule in def.Rules)
          {
            if (cntx.Translator.IsFontSetProperty(rule.Name))
            {
              try
              {
                Node nodeValue = rule.Value;
                string strFontNames = cntx.GetValue(nodeValue).Replace("\"", "");

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
                    if (!fs.FontNames.Contains(fft.FontFamily.Name))
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

      cntx.Translator.FontSets = dictFontSets;
    }

    private static void SetMapProperties(Map map, Dictionary<string, NodePropertyValue> properties, ICartoTranslator cartoTranslator, string path)
    {
      Background background = map.Background;
      background.Color = System.Drawing.Color.Transparent;
      background.Opacity = 0.0F;

      if (properties.Count == 0)
        return;

      NodePropertyValue nodeProperty = null;

      try
      {
        // ************************* Set background properties *************************
        if (properties.TryGetValue("background-color", out nodeProperty))
        {
          background.ColorHtml = nodeProperty.Value;
          background.Opacity = 1.0F;
        }

        if (properties.TryGetValue("background-opacity", out nodeProperty))
          background.Opacity = Convert.ToSingle(nodeProperty.Value, CultureInfo.InvariantCulture);

        // TODO more image stuff
        if (properties.TryGetValue("background-image-file", out nodeProperty) || properties.TryGetValue("background-image", out nodeProperty))
        {
          background.Image.FileName = cartoTranslator.ToPath(nodeProperty.Value);
          background.Opacity = 1.0F;
        }

        map.FontFiles.Clear();

        string[] values = GetStringValues(properties, "font-files-file");

        if (values.Length > 0)
          map.FontFiles.AddRange(values);

        if (properties.TryGetValue("font-directory", out nodeProperty))
        {
          string urlValue = nodeProperty.Value.TrimStart(@"url(./".ToCharArray()).TrimEnd(')');

          if (Directory.Exists(urlValue))
          {
            foreach (string fileName in Directory.EnumerateFiles(urlValue))
            {
              string filePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(path, fileName);
              if (!MapSurfer.Drawing.Text.FontFactory.IsFontRegistered(filePath))
                MapSurfer.Drawing.Text.FontFactory.RegisterFont(filePath);

              map.FontFiles.Add(filePath);
            }
          }
        }

        // ************************* Scales *************************

        if (properties.TryGetValue("minimum-scale", out nodeProperty))
          map.MinimumScale = Convert.ToDouble(nodeProperty.Value, CultureInfo.InvariantCulture);
        if (properties.TryGetValue("maximum-scale", out nodeProperty))
          map.MaximumScale = Convert.ToDouble(nodeProperty.Value, CultureInfo.InvariantCulture);

        // ************************* Buffers *************************
        int size = 256;
        if (properties.TryGetValue("buffer-size", out nodeProperty))
          size = Convert.ToInt32(nodeProperty.Value);

        map.Padding = new Size(size, size);

        if (properties.TryGetValue("buffer-image-size", out nodeProperty)) // Extension
        {
          size = Convert.ToInt32(nodeProperty.Value);
          map.Buffer = new Size(size, size);
        }
      }
      catch (Exception ex)
      {
        ParsingException pe = new ParsingException(string.Format("Invalid value '{0}' for '{1}'", nodeProperty.Value, nodeProperty.Name), ex, nodeProperty.Location.FileName);
        pe.LineNumber = Zone.GetLineNumber(nodeProperty.Location);
        throw pe;
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
      definitions.ForEach(delegate (CartoDefinition def) {
        def.Filters.Evaluate(env);
      });

      current = new List<CartoDefinition>();
      int nDefs = definitions.Count;

      for (int i = 0; i < nDefs; i++)
      {
        CartoDefinition defI = definitions[i];
        attachment = defI.Attachment;

        current.Clear();
        current.Add(defI);

        CartoStyle style = null;

        if (!byAttachment.TryGetValue(attachment, out style))
        {
          style = new CartoStyle();
          style.Attachment = attachment;

          byAttachment.Add(attachment, style);
          style.Attachment = attachment;
          byFilter[attachment] = new SortedDictionary<string, CartoDefinition>();

          result.Add(style);
        }

        SortedDictionary<string, CartoDefinition> filterAttachment = byFilter[attachment];
        // Iterate over all subsequent rules.
        for (var j = i + 1; j < nDefs; j++)
        {
          CartoDefinition defJ = definitions[j];
          if (defJ.Attachment == attachment)
          {
            // Only inherit rules from the same attachment.
            current = AddRules(current, defJ, filterAttachment, env);
          }
        }

        for (var k = 0; k < current.Count; k++)
        {
          filterAttachment[current[k].Filters.ToString()] = current[k];
          style.Add(current[k]);
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
      int nStyles = styles.Count;

      if (nStyles == 0)
        return;

      for (var i = 0; i < nStyles; i++)
      {
        CartoStyle style = styles[i];
        style.Index = int.MaxValue;

        for (int b = 0; b < style.Count; b++)
        {
          NodeList<CartoRule> rules = style[b].Rules;
          for (int r = 0; r < rules.Count; r++)
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

    private static string[] GetStringValues(Dictionary<string, NodePropertyValue> dict, string template)
    {
      List<string> values = new List<string>();

      int tempLength = template.Length;

      foreach (string key in dict.Keys)
      {
        int pos = key.IndexOf(template);
        if (pos >= 0)
        {
          string value = dict[key].Value;
          values.Add(value);
        }
      }

      return values.ToArray();
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

    private static Dictionary<string, NodePropertyValue> GetProperties(List<CartoDefinition> definitions, Env env, string elementName)
    {
      Dictionary<string, NodePropertyValue> props = new Dictionary<string, NodePropertyValue>();

      if (definitions != null)
      {
        foreach (CartoDefinition def in definitions)
        {
          if (def.Elements.Count == 1 && def.Elements[0].ToCSS(env).Trim() == elementName)
          {
            foreach (CartoRule rule in def.Rules)
            {
              if (!props.ContainsKey(rule.Name))
              {
                props.Add(rule.Name, new NodePropertyValue(rule.Name, rule.Value.ToCSS(env), rule.Location));
              }
            }
          }
        }
      }

      return props;
    }


    public static CartoLayer[] GetCartoLayers(List<Layer> layers, string translator, bool skipProperties = false)
    {
      ICartoTranslator trans = CartoGeneratorConverterFactory.CreateTranslator(translator);

      List<CartoLayer> res = new List<CartoLayer>();

      foreach (ILayer layer in layers)
      {
        if (layer is StyledLayer)
        {
          res.Add(ToCartoLayer(layer, false, trans, skipProperties));
        }
        else if (layer is GroupLayer)
        {
          res.Add(ToCartoLayer(layer, true, trans, skipProperties));

          GroupLayer groupLayer = (GroupLayer)layer;

          if (groupLayer is VectorTilesGroupLayer)
            continue;

          foreach (ILayer childLayer in groupLayer.ChildLayers)
          {
            if (childLayer is StyledLayer)
            {
              res.Add(ToCartoLayer(childLayer, false, trans, skipProperties));
            }
          }
        }
      }

      return res.ToArray();
    }

    private static CartoLayer ToCartoLayer(ILayer layer, bool group, ICartoTranslator trans, bool skipProperties)
    {
      CartoLayer cl = new CartoLayer();
      cl.Name = layer.Name;
      cl.Id = layer.Name; 

      if (skipProperties)
      {
        cl.Datasource = new CartoDatasource(); 
        return cl;
      }

      if (!layer.Enabled)
        cl.Status = "off";
      if (layer.CoordinateSystem != null)
      {
        cl.Srs_WKT = layer.CoordinateSystem.WKT;
        cl.Srs = SpatialReferenceUtility.ToProj4(cl.Srs_WKT);
      }

      if (layer is StyledLayer)
      {
        StyledLayer styleLayer = (StyledLayer)layer;

        Dictionary<string, object> propsExt = styleLayer.Tag as Dictionary<string, object>;
        if (propsExt != null)
        {
          if (propsExt.ContainsKey("Class"))
            cl.Class = propsExt["Class"].ToString();
        }
      }

      if (group)
        cl.Group = "true";

      Dictionary<string, string> properties = new Dictionary<string, string>();
      if (layer.MinimumScale != 0.0)
        properties.Add("maxzoom", MapUtility.ScaleDenominatorToZoom(layer.MinimumScale).ToString());
      if (layer.MaximumScale != double.MaxValue)
        properties.Add("minzoom", MapUtility.ScaleDenominatorToZoom(layer.MaximumScale).ToString());
      if (layer.Queryable)
        properties.Add("queryable", "true");
      if (layer.Padding.Width != 0 || layer.Padding.Height != 0)
        properties.Add("padding", layer.Padding.Width.ToString() + "," + layer.Padding.Height.ToString());

      if (layer.BlendingOptions != null && layer.BlendingOptions.IsActive)
      {
        if (layer.BlendingOptions.Opacity != 1.0F)
          properties.Add("blend-options-opacity", layer.BlendingOptions.Opacity.ToString());

        if (layer.BlendingOptions.CompositingMode != Drawing.Drawing2D.CompositingMode.None)
          properties.Add("blend-options-comp-op", trans.ToCompositingOperation(layer.BlendingOptions.CompositingMode));
      }

      if (layer is DataSourceLayer)
      {
        DataSourceLayer dsLayer = layer as DataSourceLayer;
        if (dsLayer.FeaturesCaching.Enabled)
        {
          properties.Add("cache-features", "true");
          if (dsLayer.FeaturesCaching.MinimumScale != 0.0)
            properties.Add("cache-features-minscale", dsLayer.FeaturesCaching.MinimumScale.ToString());
          if (dsLayer.FeaturesCaching.MaximumScale != double.MaxValue)
            properties.Add("cache-features-maxscale", dsLayer.FeaturesCaching.MaximumScale.ToString());
        }
      }

      if (properties.Count > 0)
        cl.Properties = properties;

      if (layer is GroupLayer)
      {
        GroupLayer groupLayer = (GroupLayer)layer;
        cl.Layers = new string[groupLayer.ChildLayers.Count];
        int i = 0;
        foreach (ILayer childLayer in groupLayer.ChildLayers)
        {
          cl.Layers[i] = childLayer.Name;
          i++;
        }
      }
      else
      {
        DataSourceLayer dsLayer = layer as DataSourceLayer;
        if (dsLayer != null && dsLayer.DataSource != null)
        {
          cl.Datasource = trans.ToDatasource(dsLayer.DataSource.Parameters);

          try
          {
            Envelope env = dsLayer.DataSource.GetExtent();
            cl.Extent = new string[] { env.MinX.ToString("0.0000"), env.MinY.ToString("0.0000"), env.MaxX.ToString("0.0000"), env.MaxY.ToString("0.0000") };

            DataSourceGeometryType geomType = dsLayer.DataSource.GeometryType;
            switch (geomType)
            {
              case DataSourceGeometryType.Point:
                cl.Geometry = "point";
                break;
              case DataSourceGeometryType.Linestring:
                cl.Geometry = "linestring";
                break;
              case DataSourceGeometryType.Polygon:
                cl.Geometry = "polygon";
                break;
              case DataSourceGeometryType.GeometryCollection:
                cl.Geometry = "collection";
                break;
            }
          }
          catch
          { }
        }
      }

      return cl;
    }

    private static void AddParameters(Dictionary<string, string> dict, ParameterCollection parameters, string setName)
    {
      if (parameters == null)
        return;

      if (parameters.Count > 0)
      {
        //  label-settings-solver-param0-key: CoolingSchedule;
        //  label-settings-solver-param0-value: AartsLaarhoven;

        int i = 0;
        foreach (Parameter param in parameters)
        {
          dict.Add(setName + i.ToString() + "-key", param.Name);
          dict.Add(setName + i.ToString() + "-value", param.Value);
          i++;
        }
      }
    }

    private static void AddParameters(Dictionary<string, string> dict, ICollection<string> parameters, string setName)
    {
      if (parameters == null)
        return;

      int i = 0;
      foreach (string param in parameters)
      {
        dict.Add(setName + i.ToString() + "-key", param);
        i++;
      }
    }

    private static void AddArrayElements<T>(Dictionary<string, string> dict, T[] array, string setName)
    {
      string value = string.Empty;

      int n = array.Length;

      for (int i = 0; i < n; i++)
        value += array[i].ToString() + ((i != n - 1) ? "," : string.Empty);

      dict.Add(setName, value);
    }

    public static Dictionary<string, string> GetLabelingOptions(LabelPlacementProblemSettings labelingOptions)
    {
      Dictionary<string, string> props = new Dictionary<string, string>();

      props.Add("accuracy-mode", labelingOptions.AccuracyMode.ToString());

      props.Add("threads-number", labelingOptions.ThreadsCount.ToString());

      AddArrayElements<LabelAlignment>(props, labelingOptions.PointPlacementPrioritization, "point-prioritization");

      props.Add("solver", labelingOptions.Solver);
      AddParameters(props, labelingOptions.SolverParameters, "solver-param");

      props.Add("candidate-position-generator", labelingOptions.CandidatePositionGenerator);
      AddParameters(props, labelingOptions.CandidatePositionGeneratorParameters, "candidate-position-generator-param");

      props.Add("collision-detector", labelingOptions.CollisionDetector);
      AddParameters(props, labelingOptions.CollisionDetectorParameters, "collision-detector-param");

      AddParameters(props, labelingOptions.QualityEvaluators, "quality-evaluator-param");

      return props;
    }

    private static ParsingException CreateParsingException(Exception ex, string fileName)
    {
      int lineNumber = -1;
      string message = null;
      GetExceptionInfo(ex, ref message, ref lineNumber);

      ParsingException res = new ParsingException(message, ex, fileName);
      res.LineNumber = lineNumber;

      return res;
    }

    private static void GetExceptionInfo(Exception ex, ref string message, ref int lineNumber)
    {
      message = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;

      lineNumber = -1;
      dotless.Core.Exceptions.ParserException pe = ex as dotless.Core.Exceptions.ParserException;
      if (pe != null)
      {
        if (pe.ErrorLocation != null)
          lineNumber = pe.ErrorLocation.LineNumber;
        else
        {
          dotless.Core.Exceptions.ParsingException pe3 = (pe.InnerException as dotless.Core.Exceptions.ParsingException);
          lineNumber = Zone.GetLineNumber(pe3.Location);
        }
      }
      else
      {
        dotless.Core.Exceptions.ParsingException pe2 = ex as dotless.Core.Exceptions.ParsingException;
        if (pe2 != null)
          lineNumber = pe.ErrorLocation.LineNumber;
      }
    }
  }
}
