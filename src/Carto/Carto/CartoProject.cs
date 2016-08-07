//==========================================================================================
//
//		MapSurfer.Styling.Formats.CartoCSS
//		Copyright (c) 2008-2015, MapSurfer.NET
//
//    Authors: Maxim Rylov
//
//==========================================================================================
using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;

using MapSurfer.Data;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  public class CartoProject
  {
    public CartoProject()
    {
      Extensions = new CartoExtensions();

      Center = new string[] { "0.0", "0.0", "1" };
      MinZoom = 0;
      MaxZoom = 8;
      Srs = "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs +over";
      Format = "png";
      Bounds = new string[] { "-180", "-85.05112877980659", "180", "85.05112877980659" };
      Scale = 1;
    }

    [JsonProperty("bounds")]
    [YamlMember(Alias = "bounds")]
    public string[] Bounds { get; set; }

    [JsonProperty("center")]
    [YamlMember(Alias = "center")]
    public string[] Center { get; set; }

    [JsonProperty("format")]
    [YamlMember(Alias = "format")]
    public string Format { get; set; }

    [JsonProperty("interactivity")]
    [YamlMember(Alias = "interactivity")]
    public object Interactivity { get; set; }

    [JsonProperty("minzoom")]
    [YamlMember(Alias = "minzoom")]
    public int MinZoom { get; set; }

    [JsonProperty("maxzoom")]
    [YamlMember(Alias = "maxzoom")]
    public int MaxZoom { get; set; }

    [JsonProperty("srs")]
    [YamlMember(Alias = "srs")]
    public string Srs { get; set; }

    [JsonProperty("srs-name")]
    [YamlMember(Alias = "srs-name")]
    public string SrsName { get; set; }

    [JsonProperty("Stylesheet")]
    [YamlMember(Alias = "Stylesheet")]
    public string[] Stylesheet { get; set; }

    [JsonProperty("styles")]
    [YamlMember(Alias = "styles")]
    public string[] Styles { get; set; }

    [JsonProperty("source")]
    [YamlMember(Alias = "source")]
    public string Source { get; set; }

    [JsonProperty("Layer")]
    [YamlMember(Alias = "Layer")]
    public CartoLayer[] Layers { get; set; }

    [JsonProperty("scale")]
    [YamlMember(Alias = "scale")]
    public float Scale { get; set; }

    [JsonProperty("metatile")]
    [YamlMember(Alias = "metatile")]
    public int Metatile { get; set; }

    [JsonProperty("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [JsonProperty("attribution")]
    [YamlMember(Alias = "attribution")]
    public string Attribution { get; set; }

    [JsonProperty("legend")]
    [YamlMember(Alias = "legend")]
    public string Legend { get; set; }

    [YamlMember(Alias = "_parts")]
    [YamlIgnore]
    public CartoParts _Parts { get; set; }

    [JsonProperty("extensions")]
    [YamlMember(Alias = "extensions")]
    public CartoExtensions Extensions { get; set; }

    public Dictionary<string, object> GetInteractivity()
    {
      return Interactivity as Dictionary<string, object>;
    }

    private void PrepeareFormat(bool read)
    {
      if (read)
      {
        if (!string.IsNullOrEmpty(Source))
        {
          Stylesheet = Styles;
        }
      }
      else
      {
        if (!string.IsNullOrEmpty(Source))
        {
          Styles = Stylesheet;
        }
      }
    }

    public static CartoProject FromFile(string fileContent, string fileExt)
    {
      CartoProject cartoProject = null;

      switch (fileExt.ToLower())
      {
        case ".mml":
          cartoProject = JsonConvert.DeserializeObject<CartoProject>(fileContent);

          if (cartoProject.Interactivity != null)
          {
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
          }
          break;
        case ".yaml":
        case ".yml":
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

      cartoProject.PrepeareFormat(true);

      return cartoProject;
    }

    public void Save(string path)
    {
      string contents = null;

      switch (Path.GetExtension(path).ToLower())
      {
        case ".mml":
          JsonSerializerSettings settings = new JsonSerializerSettings();
          settings.NullValueHandling = NullValueHandling.Ignore;
          contents = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
          break;
        case ".yaml":
        case ".yml":
          using (StringWriter sw = new StringWriter())
          {
            Serializer serializer = new Serializer();
            serializer.Serialize(sw, this);
            contents = sw.ToString();
          }
          break;
        default:
          throw new Exception("Unknown extension of the CartoCSS project.");
      }

      File.WriteAllText(path, contents);
    }
  }

  public class CartoExtensions
  {
    public CartoExtensions()
    {
      Translator = "mapnik";
    }

    [JsonProperty("translator")] // Tilemill, GeoServer or MapSurfer.NET
    [YamlMember(Alias = "translator")]
    public string Translator { get; set; }

   // public CartoProjectType ProjectType { get; set; }

    [JsonProperty("textabbreviation")]
    [YamlMember(Alias = "textabbreviation")]
    public bool TextAbbreviation { get; set; }

    [JsonProperty("bidimode")]
    [YamlMember(Alias = "bidimode")]
    public bool BidiMode { get; set; }

    [JsonProperty("kerning")]
    [YamlMember(Alias = "kerning")]
    public bool Kerning { get; set; }

    [JsonProperty("imageresampling")]
    [YamlMember(Alias = "imageresampling")]
    public string ImageResampling { get; set; }

    [JsonProperty("labelingoptions")]
    [YamlMember(Alias = "labelingoptions")]
    public Dictionary<string, string> LabelingOptions { get; set; }

    [JsonProperty("placemarks")]
    [YamlMember(Alias = "placemarks")]
    public CartoPlacemark[] Placemarks { get; set; }

    [JsonProperty("user_tasks")]
    [YamlMember(Alias = "user_tasks")]
    public CartoUserTask[] UserTasks { get; set; }
  }

  public class CartoParts : Dictionary<string, Dictionary<string, object>>
  {
  }

  public class CartoLayer
  {
    public CartoLayer()
    {
      Class = string.Empty;
    }

    [JsonProperty("id")]
    [YamlMember(Alias = "id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [JsonProperty("srs")]
    [YamlMember(Alias = "srs")]
    public string Srs { get; set; }

    [JsonProperty("srs_wkt")]
    [YamlMember(Alias = "srs_wkt")]
    public string Srs_WKT { get; set; }

    [JsonProperty("srs-name")]
    [YamlMember(Alias = "srs-name")]
    public string SrsName { get; set; }

    [JsonProperty("class")]
    [YamlMember(Alias = "class")]
    public string Class { get; set; }

    [JsonProperty("geometry")]
    [YamlMember(Alias = "geometry")]
    public string Geometry { get; set; }

    [JsonProperty("Datasource")]
    [YamlMember(Alias = "Datasource")]
    public CartoDatasource Datasource { get; set; }

    [JsonProperty("properties")]
    [YamlMember(Alias = "properties")]
    public Dictionary<string, string> Properties { get; set; }

    [JsonProperty("status")]
    [YamlMember(Alias = "status")]
    public string Status { get; set; }

    [JsonProperty("extent")]
    [YamlMember(Alias = "extent")]
    public string[] Extent { get; set; }

    // Extensions.

    [JsonProperty("group")]
    [YamlMember(Alias = "group")]
    public string Group { get; set; }

    [JsonProperty("layers")]
    [YamlMember(Alias = "layers")]
    public string[] Layers { get; set; }

    public object Tag { get; set; }

    public override string ToString()
    {
      return string.Format("Id={0};Name={1};Class={2}", Id, Name, Class);
    }
  }

  public class CartoDatasource : CartoProperties
  { }

  public class CartoLabelingOptions : CartoProperties
  { }

  public class CartoProperties : Dictionary<string, object>
  {
    public CartoProperties()
    {
    }

    public bool TryGetValue(string key, out string value)
    {
      value = null;
      if (key == null)
        return false; 
       
      object objValue;
      if (base.TryGetValue(key, out objValue))
      {
        value = objValue as string;
        return true;
      }

      return false;
    }

    public new string this[string key]
    {
      get
      {
        object value;
        if (base.TryGetValue(key, out value))
        {
          return Convert.ToString(value);
        }

        return null;
      }
    }

    public string Type
    {
      get
      {
        if (this.ContainsKey("type"))
          return this["type"];
        else
        {
          if (ContainsKey("file"))
          {
            string file = this["file"];

            string ext = System.IO.Path.GetExtension(file);
            switch (ext)
            {
              case ".json":
              case ".geojson":
              case ".topojson":
                return "GeoJson";
              case ".shp":
                return "Shape";
              case ".csv":
              case ".kml":
                return "OGR";
              case "*.gpx":
                if (MapSurfer.Data.Providers.DataSourceProviderManager.Contains("GPX"))
                  return "GPX";
                else
                  return "OGR";
              default:
                FileFormatInfo[] formats = MapSurfer.Data.GDAL.GDALWrapper.GDALUtility.GetSupportedFormatsInfo();
                foreach (FileFormatInfo ffi in formats)
                {
                  if (ffi.SupportExtension(ext))
                    return "GDAL";
                }
                break;
            }
          }

          return "Shape";
        }
      }
    }
  }

  public class CartoParameter
  {
    public string Name { get; set; }

    public string Value { get; set; }
  }

  public class CartoPlacemark
  {
    [JsonProperty("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [JsonProperty("scale")]
    [YamlMember(Alias = "scale")]
    public double Scale { get; set; }

    [JsonProperty("scale_enabled")]
    [YamlMember(Alias = "scale_enabled")]
    public bool ScaleEnabled { get; set; }

    [JsonProperty("crs")]
    [YamlMember(Alias = "crs")]
    public string Crs { get; set; }

    [JsonProperty("x")]
    [YamlMember(Alias = "x")]
    public double X { get; set; }

    [JsonProperty("y")]
    [YamlMember(Alias = "y")]
    public double Y { get; set; }
  }

  public class CartoUserTask
  {
    [JsonProperty("description")]
    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [JsonProperty("completed")]
    [YamlMember(Alias = "completed")]
    public bool Completed { get; set; }

    [JsonProperty("priority")]
    [YamlMember(Alias = "priority")]
    public int Priority { get; set; }
  }
}
