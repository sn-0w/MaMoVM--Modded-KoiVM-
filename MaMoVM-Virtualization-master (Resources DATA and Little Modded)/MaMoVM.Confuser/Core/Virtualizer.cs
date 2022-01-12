using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using MaMoVM.Confuser.Core.RT;
using MaMoVM.Confuser.Core.VMIL;

namespace MaMoVM.Confuser.Core
{
    public class Virtualizer : IVMSettings
    {
        private readonly bool debug = false;
        private readonly HashSet<MethodDef> doInstantiation = new HashSet<MethodDef>();
        private readonly GenericInstantiation instantiation = new GenericInstantiation();
        private readonly Dictionary<MethodDef, bool> methodList = new Dictionary<MethodDef, bool>();
        private readonly HashSet<ModuleDef> processed = new HashSet<ModuleDef>();
        private readonly int seed = new Random().Next(1, int.MaxValue);
        private MethodVirtualizer vr;

        public Virtualizer()
        {
            Runtime = null;

            instantiation.ShouldInstantiate += spec => doInstantiation.Contains(spec.Method.ResolveMethodDefThrow());
        }

        public ModuleDef RuntimeModule => Runtime.Module;

        public VMRuntime Runtime
        {
            get;
            set;
        }

        bool IVMSettings.IsExported(MethodDef method)
        {
            bool ret;
            if(!methodList.TryGetValue(method, out ret))
                return false;
            return ret;
        }

        bool IVMSettings.IsVirtualized(MethodDef method)
        {
            return methodList.ContainsKey(method);
        }

        int IVMSettings.Seed => seed;

        bool IVMSettings.IsDebug => debug;

        public bool ExportDbgInfo
        {
            get;
            set;
        }

        public bool DoStackWalk
        {
            get;
            set;
        }

        public void Initialize(string baselib, string outlibname)
        {
            var rtModule = ModuleDefMD.Load(baselib);
            rtModule.Assembly.Name = outlibname.Remove(outlibname.LastIndexOf('.'));
            rtModule.Name = outlibname;
            Runtime = new VMRuntime(this, rtModule);
            vr = new MethodVirtualizer(Runtime);
            Runtime.CommitRuntime(null);
        }

        public void AddModule(ModuleDef module)
        {
            foreach(var method in new Scanner(module).Scan())
                AddMethod(method.Item1, method.Item2);
        }

        public void AddMethod(MethodDef method, bool isExport)
        {
            //TODO : Here is the place where method are being added to the
            //MaMoVM.Confuser.Core protection queue. So you can decide which one must be protected or not.
            if(!method.HasBody)
                return;
            if(method.HasGenericParameters) return;
            methodList.Add(method, isExport);

            if(!isExport)
            {
                // Need to force set declaring type because will be used in VM compilation
                var thisParam = method.HasThis ? method.Parameters[0].Type : null;

                var declType = method.DeclaringType;
                declType.Methods.Remove(method);
                if(method.SemanticsAttributes != 0)
                {
                    foreach(var prop in declType.Properties)
                    {
                        if(prop.GetMethod == method)
                            prop.GetMethod = null;
                        if(prop.SetMethod == method)
                            prop.SetMethod = null;
                    }
                    foreach(var evt in declType.Events)
                    {
                        if(evt.AddMethod == method)
                            evt.AddMethod = null;
                        if(evt.RemoveMethod == method)
                            evt.RemoveMethod = null;
                        if(evt.InvokeMethod == method)
                            evt.InvokeMethod = null;
                    }
                }
                method.DeclaringType2 = declType;

                if(thisParam != null)
                    method.Parameters[0].Type = thisParam;
            }
        }

        public IEnumerable<MethodDef> GetMethods()
        {
            return methodList.Keys;
        }

        public void ProcessMethods(ModuleDef module, Action<int, int> progress = null)
        {
            if(processed.Contains(module))
                throw new InvalidOperationException("Module already processed.");

            if(progress == null)
                progress = (num, total) => { };

            var targets = methodList.Keys.Where(method => method.Module == module).ToList();

            for(var i = 0; i < targets.Count; i++)
            {
                var method = targets[i];
                instantiation.EnsureInstantiation(method, (spec, instantation) =>
                {
                    if(instantation.Module == module || processed.Contains(instantation.Module))
                        targets.Add(instantation);
                    methodList[instantation] = false;
                });
                try
                {
                    ProcessMethod(method, methodList[method]);
                }
                catch(Exception)
                {
                    Console.WriteLine("! error on process method : " + method.FullName);
                }
                progress(i, targets.Count);
            }
            progress(targets.Count, targets.Count);
            processed.Add(module);
        }

        public IModuleWriterListener CommitModule(ModuleDefMD module, Action<int, int> progress = null)
        {
            if(progress == null)
                progress = (num, total) => { };

            var methods = methodList.Keys.Where(method => method.Module == module).ToArray();
            for(var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                PostProcessMethod(method, methodList[method]);
                progress(i, methodList.Count);
            }
            progress(methods.Length, methods.Length);

            return Runtime.CommitModule(module);
        }

        private void ProcessMethod(MethodDef method, bool isExport)
        {
            vr.Run(method, isExport);
        }

        private void PostProcessMethod(MethodDef method, bool isExport)
        {
            var scope = Runtime.LookupMethod(method);

            var ilTransformer = new ILPostTransformer(method, scope, Runtime);
            ilTransformer.Transform();
        }
    }
}