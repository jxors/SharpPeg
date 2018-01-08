using SharpPeg.Common;
using SharpPeg.Runner.ILRunner;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ILTest
{
    class CustomJitter : ILJitter
    {
        public AssemblyBuilder AssemblyBuilder { get; private set; }
        private string fname;
        private Dictionary<Method, (string filename, string[] lines, ISymbolDocumentWriter symbolDocument)> source = new Dictionary<Method, (string filename, string[] lines, ISymbolDocumentWriter)>();
        private ModuleBuilder moduleBuilder;
        private int counter = 0;

        public CustomJitter(string fname)
        {
            this.fname = fname;
        }

        protected override ModuleBuilder CreateModuleBuilder()
        {
            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(fname), AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = AssemblyBuilder.DefineDynamicModule(fname, $"{fname}.dll", true);

            return moduleBuilder;
        }

        protected override void BeginCompile(Method method)
        {
            Directory.CreateDirectory("Symbols");

            var fileName = $"Symbols/{counter++}_{moduleBuilder}.txt";
            var documentBuilder = moduleBuilder.DefineDocument(Path.GetFullPath(fileName), SymDocumentType.Text, Guid.Empty, Guid.Empty);
            var lines = method.Instructions.Select(item => item.ToString()).ToArray();
            File.WriteAllLines(fileName, lines);

            source[method] = (fileName, lines, documentBuilder);
        }

        protected override void MarkSequencePoint(ILGenerator gen, Method method, int instructionIndex)
        {
            var (file, lines, document) = source[method];
            gen.MarkSequencePoint(document, instructionIndex + 1, 0, instructionIndex + 1, lines[instructionIndex].Length + 1);
        }

        protected override LocalBuilder DeclareLocal(ILGenerator gen, Type type, string name)
        {
            var local = gen.DeclareLocal(type);
            local.SetLocalSymInfo(name);

            return local;
        }

        public void Save()
        {
            AssemblyBuilder.Save($"{fname}.dll");
        }
    }
}
