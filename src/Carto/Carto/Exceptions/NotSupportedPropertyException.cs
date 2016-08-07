using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Exceptions
{
  public class NotSupportedPropertyException : CartoException
  {
    public NotSupportedPropertyException(string message, Exception ex, string fileName) : base(message, ex, fileName)
    {
    }

    public NotSupportedPropertyException(string message, Exception ex, string fileName, int lineNumber) : base(message, ex, fileName, lineNumber)
    {
    }

    public NotSupportedPropertyException(string message, string fileName, int lineNumber) : base(message, fileName, lineNumber)
    {
    }
  }
}
