namespace dotless.Core.Parser.Tree
{
  using System.Text.RegularExpressions;
  using Infrastructure;
  using Infrastructure.Nodes;
  using System.Text;
  using System;
  using System.Threading;

  public class Quoted : TextNode
  {
    public static ThreadLocal<Regex> _evaluate =
       new ThreadLocal<Regex>(() =>
       {
         return new Regex(@"@\{([\w-]+)\}", RegexOptions.Compiled);
       });

        public static ThreadLocal<Regex> _unescape =
         new ThreadLocal<Regex>(() =>
         {
           return new Regex(@"(^|[^\\])\\(['""])", RegexOptions.Compiled);
         });

    public char? Quote { get; set; }
    public bool Escaped { get; set; }

    public Quoted(string value, char? quote)
        : base(value)
    {
      Quote = quote;
    }

    public Quoted(string value, char? quote, bool escaped)
        : base(value)
    {
      Escaped = escaped;
      Quote = quote;
    }

    public Quoted(string value, string contents, bool escaped)
        : base(contents)
    {
      Escaped = escaped;
      Quote = value[0];
    }

    public Quoted(string value, bool escaped)
        : base(value)
    {
      Escaped = escaped;
      Quote = null;
    }

    public override void AppendCSS(Env env)
    {
      env.Output
          .Append(RenderString());
    }

    public StringBuilder RenderString()
    {
      if (Escaped)
      {
        return new StringBuilder(UnescapeContents());
      }

      return new StringBuilder()
          .Append(Quote)
          .Append(Value)
          .Append(Quote);
    }

    public override string ToString()
    {
      // Runge
      if (!Escaped)
      {
        if (Quote == null)
          return Value;
        else
          return Quote + Value + Quote;
      }
      else
        return RenderString().ToString();
    }

    public override Node Evaluate(Env env)
    {
      var value = _evaluate.Value.Replace(Value,//Regex.Replace(Value, @"@\{([\w-]+)\}",
                    m =>
                    {
                      var v = new Variable('@' + m.Groups[1].Value)
                      { Location = new NodeLocation(Location.Index + m.Index, Location.Source, Location.FileName) }
                                  .Evaluate(env);
                      return v is TextNode ? (v as TextNode).Value : v.ToCSS(env);
                    });

      return new Quoted(value, Quote, Escaped).ReducedFrom<Quoted>(this);
    }

    public string UnescapeContents()
    {
      return _unescape.Value.Replace(Value, @"$1$2");
    }

  }
}