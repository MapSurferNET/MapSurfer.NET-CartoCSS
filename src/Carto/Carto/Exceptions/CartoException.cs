using System;

namespace MapSurfer.Styling.Formats.CartoCSS.Exceptions
{
  public class CartoException : Exception
  {
    public CartoException(string message, Exception ex, string fileName) : base(message, ex)
    {
      FileName = fileName;
    }

    public CartoException(string message, Exception ex, string fileName, int lineNumber) : base(message, ex)
    {
      FileName = fileName;
      LineNumber = lineNumber;
    }

    public CartoException(string message, string fileName, int lineNumber) : base(message)
    {
      FileName = fileName;
      LineNumber = lineNumber;
    }

    public int LineNumber { get; set; }
    public string FileName { get; set; }
  }
}
