using dotless.Core.Parser;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  public class NodePropertyValue
  {
    public NodePropertyValue(string name, string value, NodeLocation location)
    {
      Name = name;
      Value = value;
      Location = location;
    }

    public string Name { get; set; }
    public string Value { get; set; }
    public NodeLocation Location { get; set; }
  }
}
