using System;
using System.Linq;
using Mono.Cecil;
static class Program
{
    static void Main(string[] args)
    {
        var module = ModuleDefinition.ReadModule(args[0]);
        foreach (var t in module.Types.Where(t => args.Skip(1).Contains(t.Name)))
        {
            Console.WriteLine($"== {t.FullName} : {t.BaseType?.Name}");
            foreach (var a in t.CustomAttributes) Console.WriteLine($"   [{a.AttributeType.Name}({string.Join(", ", a.ConstructorArguments.Select(c => c.Value))})]");
            foreach (var f in t.Fields) Console.WriteLine($"   field {f.FieldType.Name} {f.Name} {string.Join(" ", f.CustomAttributes.Select(c => "[" + c.AttributeType.Name + "]"))}");
            foreach (var m in t.Methods.Where(m => m.Name.Contains("RPC") || m.Name.Contains("Weaved") || m.CustomAttributes.Any(c => c.AttributeType.Name.Contains("Rpc"))))
                Console.WriteLine($"   method {m.Name} ({m.Body?.Instructions.Count ?? 0} IL) {string.Join(" ", m.CustomAttributes.Select(c => "[" + c.AttributeType.Name + "]"))}");
            var getter = t.Properties.FirstOrDefault(p => p.Name == "NetPos" || p.Name == "Round");
            if (getter != null) Console.WriteLine($"   {getter.Name} getter IL: " + string.Join(" | ", getter.GetMethod.Body.Instructions.Take(8).Select(i => i.OpCode.Name)));
        }
    }
}
