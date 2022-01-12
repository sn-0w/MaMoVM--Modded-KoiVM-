using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using MaMoVM.Confuser.Core.AST;
using MaMoVM.Confuser.Core.AST.IL;
using MaMoVM.Confuser.Core.CFG;
using MaMoVM.Confuser.Core.RT.Mutation;
using MaMoVM.Confuser.Core.VM;

namespace MaMoVM.Confuser.Core.RT
{
    public class VMRuntime
    {
        private List<Tuple<MethodDef, ILBlock>> basicBlocks;

        internal DbgWriter dbgWriter;

        private List<IVMChunk> extraChunks;
        private List<IVMChunk> finalChunks;
        internal Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>> methodMap;

        private RuntimeMutator rtMutator;
        internal BasicBlockSerializer serializer;
        private readonly IVMSettings settings;

        public VMRuntime(IVMSettings settings, ModuleDef rt)
        {
            this.settings = settings;
            Init(rt);
        }

        public ModuleDef Module => rtMutator.RTModule;

        public VMDescriptor Descriptor
        {
            get;
            private set;
        }

        public static byte[] RuntimeByte
        {
            get;
            private set;
        }

        public static StringBuilder TypeList = new StringBuilder();

        public byte[] DebugInfo => dbgWriter.GetDbgInfo();

        private void Init(ModuleDef rt)
        {
            Descriptor = new VMDescriptor(settings);
            methodMap = new Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>>();
            basicBlocks = new List<Tuple<MethodDef, ILBlock>>();

            extraChunks = new List<IVMChunk>();
            finalChunks = new List<IVMChunk>();
            serializer = new BasicBlockSerializer(this);

            rtMutator = new RuntimeMutator(rt, this);
        }

        public void AddMethod(MethodDef method, ScopeBlock rootScope)
        {
            ILBlock entry = null;
            foreach (ILBlock block in rootScope.GetBasicBlocks())
            {
                if (block.Id == 0)
                    entry = block;
                basicBlocks.Add(Tuple.Create(method, block));
            }
            Debug.Assert(entry != null);
            methodMap[method] = Tuple.Create(rootScope, entry);
        }

        internal void AddHelper(MethodDef method, ScopeBlock rootScope, ILBlock entry)
        {
            methodMap[method] = Tuple.Create(rootScope, entry);
        }

        public void AddBlock(MethodDef method, ILBlock block)
        {
            basicBlocks.Add(Tuple.Create(method, block));
        }

        public ScopeBlock LookupMethod(MethodDef method)
        {
            var m = methodMap[method];
            return m.Item1;
        }

        public ScopeBlock LookupMethod(MethodDef method, out ILBlock entry)
        {
            var m = methodMap[method];
            entry = m.Item2;
            return m.Item1;
        }

        public void AddChunk(IVMChunk chunk)
        {
            extraChunks.Add(chunk);
        }

        public void ExportMethod(MethodDef method)
        {
            rtMutator.ReplaceMethodStub(method);
        }

        public IModuleWriterListener CommitModule(ModuleDefMD module)
        {
            return rtMutator.CommitModule(module);
        }

        public void CommitRuntime(ModuleDef targetModule = null)
        {
            rtMutator.CommitRuntime(targetModule);
            RuntimeByte = rtMutator.RuntimeLib;
        }

        public void OnKoiRequested()
        {
            var header = new HeaderChunk(this);

            foreach (var block in basicBlocks) finalChunks.Add(block.Item2.CreateChunk(this, block.Item1));
            finalChunks.AddRange(extraChunks);
            Descriptor.Random.Shuffle(finalChunks);
            finalChunks.Insert(0, header);

            ComputeOffsets();
            FixupReferences();
            header.WriteData(this);
            VMDATACreate();
        }


        ////////////////////////////////////////////////////////// Data
        public static byte[] DATA = new byte[0];

        public void VMDATACreate()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            foreach (var chunk in finalChunks)
            {
                writer.Write(chunk.GetData());
            }
            DATA = stream.ToArray();
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void ComputeOffsets()
        {
            uint offset = 0;
            foreach (var chunk in finalChunks)
            {
                chunk.OnOffsetComputed(offset);
                offset += chunk.Length;
            }
        }

        private void FixupReferences()
        {
            foreach (var block in basicBlocks)
            {
                foreach (var instr in block.Item2.Content)
                {
                    if (instr.Operand is ILRelReference)
                    {
                        var reference = (ILRelReference)instr.Operand;
                        instr.Operand = ILImmediate.Create(reference.Resolve(this), ASTType.I4);
                    }
                }
            }
        }

        public void ResetData()
        {
            methodMap = new Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>>();
            basicBlocks = new List<Tuple<MethodDef, ILBlock>>();

            extraChunks = new List<IVMChunk>();
            finalChunks = new List<IVMChunk>();
            Descriptor.ResetData();

            rtMutator.InitHelpers();
        }
    }
}