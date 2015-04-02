using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.IO;
using System.Diagnostics;

namespace OpenRoslyn
{
  enum ReferenceStatus {OK, MissingMSCorLib, MissingFacades, Broken};
  class Program
  {
#if true
    static void Main(string[] args)
    {

      var messages = new List<string>();
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(@"C:\Users\carr27\Documents\GitHub\roslyn\src\RoslynLight.sln").Result;
      var proj = sln.Projects.First(x => x.FilePath.EndsWith("BasicCodeAnalysis.Desktop.vbproj"));
      proj = proj.AddMetadataReferences(GetFacadeReferences());
      var errs = proj.GetCompilationAsync().Result.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
      foreach (var e in errs)
      {
        Console.WriteLine(e);
      }
      File.WriteAllLines("output.txt", errs.Select(x => x.GetMessage()));
      Console.WriteLine("done.");
      Console.ReadKey();

    }
#else
    static void Main(string[] args)
    {

      var messages = new List<string>();
      var msbw = MSBuildWorkspace.Create();
      var sln = msbw.OpenSolutionAsync(@"C:\Users\carr27\Documents\GitHub\roslyn\src\RoslynLight.sln").Result;
      foreach(var p in sln.Projects)
      {
        //if (p.Language == LanguageNames.CSharp)
        //{
          FindIfMissingReferences(p);
        //}
      }
      File.WriteAllLines("log.txt", messages);
      Console.WriteLine("done.");
      Console.ReadKey();
    }
#endif
    static IEnumerable<MetadataReference> GetFacadeReferences()
    {
      var refs = new List<MetadataReference>();
      var facadesDir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\";
      string[] dlls =
      {
       "System.Collections.dll",
       "System.Collections.Concurrent.dll",
       "System.Globalization.dll",
       "System.IO.dll",
       "System.Reflection.dll",
       "System.Reflection.Extensions.dll",
       "System.Reflection.Primitives.dll",
       "System.Resources.ResourceManager.dll",
       "System.Runtime.dll",
       "System.Runtime.Extensions.dll",
       "System.Runtime.InteropServices.dll",
       "System.Text.Encoding.dll",
       "System.Threading.Tasks.dll",
       "System.Xml.ReaderWriter.dll",
      };
      var dllPaths = dlls.Select(x => facadesDir + x);
      refs.AddRange(dllPaths.Select(f => MetadataReference.CreateFromFile(f)));
      return refs;
    }
    static MetadataReference GetMSCorLibRef()
    {
      return MetadataReference.CreateFromAssembly(typeof(object).Assembly);
    }
    static ReferenceStatus FindIfMissingReferences(Project proj)
    {
      List<string> orig_msg, mscl_msg, facade_msg;
      if (!HasErrors(proj, out orig_msg))
      {
        return ReferenceStatus.OK;
      }
      var projWithMSCL = proj.AddMetadataReference(GetMSCorLibRef());
      if (!HasErrors(projWithMSCL, out mscl_msg))
      {
        return ReferenceStatus.MissingMSCorLib;
      }
      var projWithFacades = proj.AddMetadataReferences(GetFacadeReferences());
      if (!HasErrors(projWithFacades, out facade_msg))
      {
        return ReferenceStatus.MissingFacades;
      }
      List<string>[] msgs = { orig_msg, mscl_msg, facade_msg };
      var lens = msgs.Select(x => x.Count());
      var shortest_len = lens.Min();
      var shortest = msgs.First(x => x.Count() == shortest_len);
      if (shortest == orig_msg) { Console.WriteLine("orig: "); }
      if (shortest == mscl_msg) { Console.WriteLine("mscl: "); }
      if (shortest == facade_msg) { Console.WriteLine("facade: "); }
      foreach (var m in shortest)
      {
        Console.WriteLine(m);
      }
      Debug.Assert(false, "Couldn't fix project: " + proj.FilePath);
      return ReferenceStatus.Broken;
    }
    static bool HasErrors(Project proj, out List<string> why)
    {
      why = new List<string>();
      try
      {
        var cu = proj.GetCompilationAsync().Result;
        foreach (var e in cu.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
        {
          why.Add(String.Format("{0}: {1}", e.Location, e.GetMessage()));
        }
        return why.Count() != 0;
      }
      catch (AggregateException e)
      {
        foreach (var ie in e.InnerExceptions)
        {
          why.Add(ie.Message);
        }
        return false;

      }
      catch (Exception e)
      {
        why.Add(e.Message);
        return false;
      }
    }
  }
}
