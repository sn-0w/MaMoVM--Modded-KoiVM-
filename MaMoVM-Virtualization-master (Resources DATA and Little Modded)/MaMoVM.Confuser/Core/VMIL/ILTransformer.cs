#region

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using MaMoVM.Confuser.Core.AST.IL;
using MaMoVM.Confuser.Core.CFG;
using MaMoVM.Confuser.Core.RT;
using MaMoVM.Confuser.Core.VM;
using MaMoVM.Confuser.Core.VMIL.Transforms;

#endregion

namespace MaMoVM.Confuser.Core.VMIL
{
    public class ILTransformer
    {
        private ITransform[] pipeline;

        public ILTransformer(MethodDef method, ScopeBlock rootScope, VMRuntime runtime)
        {
            RootScope = rootScope;
            Method = method;
            Runtime = runtime;

            Annotations = new Dictionary<object, object>();
            pipeline = InitPipeline();
        }

        public VMRuntime Runtime
        {
            get;
        }

        public MethodDef Method
        {
            get;
        }

        public ScopeBlock RootScope
        {
            get;
        }

        public VMDescriptor VM => Runtime.Descriptor;

        internal Dictionary<object, object> Annotations
        {
            get;
        }

        internal ILBlock Block
        {
            get;
            private set;
        }

        internal ILInstrList Instructions => Block.Content;

        private ITransform[] InitPipeline()
        {
            return new ITransform[]
            {
                // new SMCILTransform(),
                new ReferenceOffsetTransform(),
                new EntryExitTransform(),
                new SaveInfoTransform()
            };
        }

        public void Transform()
        {
            if(pipeline == null)
                throw new InvalidOperationException("Transformer already used.");

            foreach(var handler in pipeline)
            {
                handler.Initialize(this);

                RootScope.ProcessBasicBlocks<ILInstrList>(block =>
                {
                    Block = (ILBlock) block;
                    handler.Transform(this);
                });
            }

            pipeline = null;
        }
    }
}