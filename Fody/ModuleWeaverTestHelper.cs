﻿using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Scalpel;

[Remove]
public class ModuleWeaverTestHelper
{
    public string BeforeAssemblyPath;
    public string AfterAssemblyPath;
    public Assembly Assembly;

    public ModuleWeaverTestHelper(string inputAssembly)
    {
        BeforeAssemblyPath = Path.GetFullPath(inputAssembly);
#if (!DEBUG)
        BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
#endif
        AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
        var oldPdb = BeforeAssemblyPath.Replace(".dll", ".pdb");
        var newPdb = BeforeAssemblyPath.Replace(".dll", "2.pdb");
        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
        File.Copy(oldPdb, newPdb, true);

        var assemblyResolver = new MockAssemblyResolver
            {
                Directory = Path.GetDirectoryName(BeforeAssemblyPath)
            };

        using (var symbolStream = File.OpenRead(newPdb))
        {
            var readerParameters = new ReaderParameters
                {
                    ReadSymbols = true,
                    SymbolStream = symbolStream,
                    SymbolReaderProvider = new PdbReaderProvider()
                };
            var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath, readerParameters);

            var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition,
                    AssemblyResolver = assemblyResolver,
                };

            weavingTask.Execute();
            moduleDefinition.Write(AfterAssemblyPath);
        }
        Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

}