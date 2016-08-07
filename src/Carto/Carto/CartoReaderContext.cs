using System;
using System.Collections.Generic;

using dotless.Core.Parser;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;

using MapSurfer.Styling.Formats.CartoCSS.Exceptions;
using MapSurfer.Styling.Formats.CartoCSS.Translators;

namespace MapSurfer.Styling.Formats.CartoCSS
{
  internal class CartoReaderContext
  {
    public Env Env { get; set; }
    public Dictionary<int, string> NodeValues { get; set; }
    public ICartoTranslator Translator { get; set; }
    private object _lockObj;

    public CartoReaderContext(Env env, ICartoTranslator translator)
    {
      NodeValues = new Dictionary<int, string>();
      Env = env;
      Translator = translator;
      _lockObj = new object();
    }

    public string GetValue(Node node)
    {
      string res = null;
      int hash = node.GetHashCode();
      if (NodeValues.TryGetValue(hash, out res))
        return res;

      lock (_lockObj)
      {
        try
        {
          Node v = node.Evaluate(Env);

          dotless.Core.Parser.Tree.Color clr = v as dotless.Core.Parser.Tree.Color;
          if (clr != null)
            res = clr.ToArgb();
          else
            res = v.ToCSS(Env);
        }
        catch (Exception ex)
        {
          throw new ParsingException(ex.Message, node.Location.FileName, Zone.GetLineNumber(node.Location));
        }

        NodeValues.Add(hash, res);
      }

      return res;
    }
  }
}
