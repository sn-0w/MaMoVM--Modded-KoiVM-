using System;
using System.Collections.Generic;
using System.Linq;
using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Protections;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace MaMoVM.Confuser.Internal
{
    public class InitializePhase : ProtectionPhase
    {
        public InitializePhase(Protection parent) : base(parent){ }

        public override ProtectionTargets Targets => ProtectionTargets.Methods;

        public override string Name => "Virtualization Initialization";

        private IModuleWriterListener commitListener;

        protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
        {
            var vr = new Core.Virtualizer();

            var marker = context.Registry.GetService<IMarkerService>();
            var antiTamper = context.Registry.GetService<IAntiTamperService>();

            var methods = new HashSet<MethodDef>(parameters.Targets.OfType<MethodDef>());
            var refRepl = new Dictionary<IMemberRef, IMemberRef>();

            var oldType = context.CurrentModule.GlobalType;
            var newType = new TypeDefUser(oldType.Name);
            oldType.Name = "{" + System.Guid.NewGuid().ToString() + "}";
            oldType.BaseType = context.CurrentModule.CorLibTypes.GetTypeRef("System", "Object");
            context.CurrentModule.Types.Insert(0, newType);

            var old_cctor = oldType.FindOrCreateStaticConstructor();
            var cctor = newType.FindOrCreateStaticConstructor();
            old_cctor.Name = ".ctor";
            old_cctor.IsRuntimeSpecialName = false;
            old_cctor.IsSpecialName = false;
            old_cctor.Access = MethodAttributes.PrivateScope;
            cctor.Body = new CilBody(true, new List<Instruction>
            {
                Instruction.Create(OpCodes.Call, old_cctor),
                Instruction.Create(OpCodes.Ret)
            }, new List<ExceptionHandler>(), new List<Local>());

            marker.Mark(cctor, Parent);
            antiTamper.ExcludeMethod(context, cctor);

            for(var i = 0; i < oldType.Methods.Count; i++)
            {
                var nativeMethod = oldType.Methods[i];
                if(nativeMethod.IsNative)
                {
                    var methodStub = new MethodDefUser(nativeMethod.Name, nativeMethod.MethodSig.Clone());
                    methodStub.Attributes = MethodAttributes.Assembly | MethodAttributes.Static;
                    methodStub.Body = new CilBody();
                    methodStub.Body.Instructions.Add(new Instruction(OpCodes.Jmp, nativeMethod));
                    methodStub.Body.Instructions.Add(new Instruction(OpCodes.Ret));

                    oldType.Methods[i] = methodStub;
                    newType.Methods.Add(nativeMethod);
                    refRepl[nativeMethod] = methodStub;
                    marker.Mark(methodStub, Parent);
                    antiTamper.ExcludeMethod(context, methodStub);
                }
            }

            context.Registry.GetService<ICompressionService>().TryGetRuntimeDecompressor(context.CurrentModule, def =>
            {
                if(def is MethodDef def1)
                {
                    methods.Remove(def1);
                }                   
            });

            string rtName = null;
            foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>())
            {
                rtName = parameters.GetParameter<string>(context, module, "rtName");
            }
            rtName = rtName ?? ((string)VMHelper.OUTPUTDLLName).Remove(((string)VMHelper.OUTPUTDLLName).LastIndexOf('.'));

            vr.Initialize
            (
               AppDomain.CurrentDomain.BaseDirectory + (string)VMHelper.ReleaseDLLName,
               (string)VMHelper.OUTPUTDLLName
            );

            var toProcess = new Dictionary<ModuleDef, List<MethodDef>>();
            foreach (var entry in new Core.Scanner(context.CurrentModule, methods).Scan())
            {
                vr.AddMethod(entry.Item1, entry.Item2);
                toProcess.AddListEntry(entry.Item1.Module, entry.Item1);
            }

            context.CurrentModuleWriterListener.OnWriterEvent += delegate(object sender, ModuleWriterListenerEventArgs e)
            {
                var writer = (ModuleWriter)sender;
                if (commitListener != null)
                {
                    commitListener.OnWriterEvent(writer, e.WriterEvent);
                }

                if (e.WriterEvent == ModuleWriterEvent.MDBeginWriteMethodBodies && toProcess.ContainsKey(writer.Module))
                {
                    vr.ProcessMethods(writer.Module);

                    foreach (var repl in refRepl)
                    {
                        vr.Runtime.Descriptor.Data.ReplaceReference(repl.Key, repl.Value);
                    }

                    commitListener = vr.CommitModule(context.CurrentModule);
                }
            };
        }
    }
}