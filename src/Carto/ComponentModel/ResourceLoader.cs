using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;

namespace MapSurfer.ComponentModel
{
  internal class ResourceLoader
  {
    private ResourceManager m_resources = null;
    private static SR m_loader;

    static ResourceLoader()
    { }

    internal ResourceLoader()
    {
      m_resources = ResourceManager.CreateFileBasedResourceManager(GetResourceName(), GetResourceDir(), null);
    }

    protected virtual string GetResourceName()
    {
      return string.Empty;
    }

    protected virtual string GetResourceDir()
    {
      return string.Empty;
    }

    private static SR GetLoader()
    {
      if (m_loader == null)
      {
        SR sr = new SR();
        Interlocked.CompareExchange<SR>(ref m_loader, sr, null);
      }

      return m_loader;
    }

    public static object GetObject(string name)
    {
      SR loader = GetLoader();
      if (loader == null)
      {
        return null;
      }
      return loader.m_resources.GetObject(name);
    }

    public static string GetString(string name)
    {
      SR loader = GetLoader();
      if (loader == null)
      {
        return null;
      }
      return loader.m_resources.GetString(name);
    }

    public static string GetString(string name, params object[] args)
    {
      SR loader = GetLoader();
      if (loader == null)
      {
        return null;
      }
      string format = loader.m_resources.GetString(name);
      if ((args == null) || (args.Length <= 0))
      {
        return format;
      }
      for (int i = 0; i < args.Length; i++)
      {
        string str2 = args[i] as string;
        if ((str2 != null) && (str2.Length > 0x400))
        {
          args[i] = str2.Substring(0, 0x3fd) + "...";
        }
      }

      return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    public static ResourceManager Resources
    {
      get
      {
        return GetLoader().m_resources;
      }
    }
  }
}
