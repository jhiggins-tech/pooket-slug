// Runs Unity ILPostProcessors (here: Photon Fusion's IL weaver) outside the editor, the way Unity's
// compilation pipeline does: hand it the compiled assembly + references + defines, print diagnostics.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

sealed class Compiled : ICompiledAssembly
{
    public InMemoryAssembly InMemoryAssembly { get; set; }
    public string Name { get; set; }
    public string[] References { get; set; }
    public string[] Defines { get; set; }
}

static class Program
{
    // args: projectRoot postProcessorDll assembly.dll assembly.pdb out.dll refs.txt "DEF1;DEF2" "probeDir1;probeDir2"
    static int Main(string[] args)
    {
        string project = args[0], ppDll = Path.GetFullPath(args[1]), asmPath = Path.GetFullPath(args[2]), pdbPath = Path.GetFullPath(args[3]), outPath = Path.GetFullPath(args[4]);
        string[] refs = File.ReadAllLines(args[5]).Where(l => l.Length > 0).ToArray();
        string[] defines = args[6].Split(';', StringSplitOptions.RemoveEmptyEntries);
        string[] probe = args[7].Split(';', StringSplitOptions.RemoveEmptyEntries);
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            foreach (string dir in probe)
            {
                string p = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(p)) return ctx.LoadFromAssemblyPath(p);
            }
            return null;
        };
        Directory.SetCurrentDirectory(project);

        Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(ppDll);
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); foreach (var le in e.LoaderExceptions.Take(5)) Console.WriteLine("loader: " + le.Message); }
        Type ppType = types.Single(t => typeof(ILPostProcessor).IsAssignableFrom(t) && !t.IsAbstract);
        var pp = ((ILPostProcessor)Activator.CreateInstance(ppType, true)).GetInstance();

        var compiled = new Compiled
        {
            Name = Path.GetFileNameWithoutExtension(asmPath),
            InMemoryAssembly = new InMemoryAssembly(File.ReadAllBytes(asmPath), File.ReadAllBytes(pdbPath)),
            References = refs,
            Defines = defines,
        };
        bool will = pp.WillProcess(compiled);
        Console.WriteLine($"{ppType.FullName}.WillProcess({compiled.Name}) = {will}");
        if (!will) return 2;

        ILPostProcessResult result = pp.Process(compiled);
        int errors = 0;
        foreach (DiagnosticMessage d in result.Diagnostics)
        {
            Console.WriteLine($"[{d.DiagnosticType}] {(string.IsNullOrEmpty(d.File) ? "" : d.File + "(" + d.Line + "," + d.Column + "): ")}{d.MessageData}");
            if (d.DiagnosticType == DiagnosticType.Error) errors++;
        }
        if (result.InMemoryAssembly != null)
        {
            File.WriteAllBytes(outPath, result.InMemoryAssembly.PeData);
            Console.WriteLine($"woven assembly -> {outPath} ({result.InMemoryAssembly.PeData.Length} bytes)");
        }
        else Console.WriteLine("weaver returned no assembly (nothing to weave, or it failed)");
        return errors == 0 ? 0 : 1;
    }
}
