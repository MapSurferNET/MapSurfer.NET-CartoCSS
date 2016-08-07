using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Exceptions
{
  public class ParsingException : CartoException
  {
    public ParsingException(string message, Exception ex, string fileName) : base(message, ex, fileName)
    {
   }

    public ParsingException(string message, Exception ex, string fileName, int lineNumber) : base(message, ex, fileName, lineNumber)
    {
    }

    public ParsingException(string message, string fileName, int lineNumber) : base(message, fileName, lineNumber)
    {
    }
  }
}
