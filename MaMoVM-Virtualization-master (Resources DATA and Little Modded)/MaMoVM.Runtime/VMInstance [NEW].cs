using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using MaMoVM.Runtime.Data;
using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;
using MaMoVM.Runtime.Execution.Internal;

namespace MaMoVM.Runtime
{
    internal unsafe class VMInstance
    {
        [ThreadStatic]
        private static Dictionary<Module, VMInstance> instances;

        private static readonly object initLock = new object();
        private static readonly Dictionary<Module, int> initialized = new Dictionary<Module, int>();

        private readonly Stack<VMContext> ctxStack = new Stack<VMContext>();
        private VMContext currentCtx;

        public static readonly bool x64 = IntPtr.Size == 8;

        [VMProtect.BeginMutation]
        internal static object InvokeInternal(object[] args, void*[] typedRefs)
        {
            return Instance(RuntimeTypeModule()).Invoke((string)args[0], typedRefs);
        }

        [VMProtect.BeginMutation]
        internal static object InvokeInternal(int moduleId, ulong codeAddr, uint key, uint sigId, object[] args)
        {
            return Instance(moduleId).Invoke(codeAddr, key, sigId, args);
        }

        private VMInstance(VMData data)
        {
            Data = data;
        }

        public VMData Data
        {
            get;
        }

        [VMProtect.BeginMutation]
        public static byte[] Decrypt(byte[] data, byte[] password)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(password);
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            byte[] hashedPasswordBytes = System.Text.Encoding.ASCII.GetBytes(hash.ToString());
            int passwordShiftIndex = 0;
            bool shiftFlag = false;
            for (int i = 0; i < data.Length; i++)
            {
                int shift = hashedPasswordBytes[passwordShiftIndex];
                data[i] = shift <= 128
                    ? (byte)(data[i] - (shiftFlag
                        ? (byte)(((shift << 2)) % 255)
                        : (byte)(((shift << 4)) % 255)))
                    : (byte)(data[i] + (shiftFlag
                        ? (byte)(((shift << 4)) % 255)
                        : (byte)(((shift << 2)) % 255)));
                passwordShiftIndex = (passwordShiftIndex + 1) % 64;
                shiftFlag = !shiftFlag;
            }
            return data;
        }

        [VMProtect.BeginMutation]
        internal static Module RuntimeTypeModule()
        {
            ///////////// Type Stream
            byte[] TypeArray = new byte[0];
            using (Stream TypeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("B3D2A594-D219-4527-A122-BF81B5EBAE99"))
            {
                if (TypeStream == null) return null;
                byte[] ba = new byte[TypeStream.Length];
                TypeStream.Read(ba, 0, ba.Length);
                TypeArray = ba;
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            ///////////// Type Decrypt Password Stream
            byte[] TypeEncryptionPasswordArray = new byte[0];
            using (Stream TypeEncryptionPasswordStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FB15D486-9EB4-4E00-9104-FCC1D0309D55"))
            {
                if (TypeEncryptionPasswordStream == null) return null;
                byte[] ba = new byte[TypeEncryptionPasswordStream.Length];
                TypeEncryptionPasswordStream.Read(ba, 0, ba.Length);
                TypeEncryptionPasswordArray = ba;
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            RuntimeTypeHandle typeHandle = new RuntimeTypeHandle();
            using (StreamReader reader = new StreamReader(new MemoryStream(Decrypt(TypeArray, TypeEncryptionPasswordArray))))
            {
                typeHandle = Type.GetType(reader.ReadLine()).TypeHandle;
            }
            return Type.GetTypeFromHandle(typeHandle).Module;
        }

        [VMProtect.BeginMutation]
        public static VMInstance Instance(Module module)
        {
            if (instances == null) instances = new Dictionary<Module, VMInstance>();
            if (!instances.TryGetValue(module, out VMInstance inst))
            {
                inst = new VMInstance(VMData.Instance(module));
                instances[module] = inst;
                lock (initLock)
                {
                    if (!initialized.ContainsKey(module))
                    {
                        inst.Initialize();
                        initialized.Add(module, initialized.Count);
                    }
                }
            }
            return inst;
        }

        [VMProtect.BeginMutation]
        public static VMInstance Instance(int id)
        {
            foreach (var entry in initialized)
                if (entry.Value == id)
                    return Instance(entry.Key);
            return null;
        }

        [VMProtect.BeginMutation]
        public static int GetModuleId(Module module)
        {
            return initialized[module];
        }

        [VMProtect.BeginUltra]
        private void Initialize()
        {
            var initFunc = Data.LookupExport(Constants.HELPER_INIT);
            var codeAddr = (ulong)(Data.KoiSection + initFunc.CodeOffset);
            Invoke(codeAddr, initFunc.EntryKey, initFunc.Signature, new void*[0]);
        }

        [VMProtect.BeginMutation]
        public object Invoke(string ID, void*[] typedRefs)
        {
            int length = ID.Length;
            char[] array = new char[length];
            for (int i = 0; i < array.Length; i++)
            {
                char c = ID[i];
                byte b = (byte)(c ^ length - i);
                byte b2 = (byte)(c >> 8 ^ i);
                array[i] = (char)(b2 << 8 | b);
            }
            var export = Data.LookupExport(uint.Parse(string.Intern(new string(array)).Substring(10)) / 8);
            var codeAddr = (ulong)(Data.KoiSection + export.CodeOffset);
            return Invoke(codeAddr, export.EntryKey, export.Signature, typedRefs);
        }

        [VMProtect.BeginMutation]
        public object Invoke(ulong codeAddr, uint key, uint sigId, object[] arguments)
        {
            var sig = Data.LookupExport(sigId).Signature;
            return Invoke(codeAddr, key, sig, arguments);
        }

        [VMProtect.BeginMutation]
        private object Invoke(ulong codeAddr, uint key, VMFuncSig sig, object[] arguments)
        {
            if (currentCtx != null)
                ctxStack.Push(currentCtx);
            currentCtx = new VMContext(this);

            try
            {
                Debug.Assert(sig.ParamTypes.Length == arguments.Length);
                currentCtx.Stack.SetTopPosition((uint)arguments.Length + 1);
                for (uint i = 0; i < arguments.Length; i++) currentCtx.Stack[i + 1] = VMSlot.FromObject(arguments[i], sig.ParamTypes[i]);
                currentCtx.Stack[(uint)arguments.Length + 1] = new VMSlot { U8 = 1 };

                currentCtx.Registers[Constants.REG_K1] = new VMSlot { U4 = key };
                currentCtx.Registers[Constants.REG_BP] = new VMSlot { U4 = 0 };
                currentCtx.Registers[Constants.REG_SP] = new VMSlot { U4 = (uint)arguments.Length + 1 };
                currentCtx.Registers[Constants.REG_IP] = new VMSlot { U8 = codeAddr };
                VMDispatcher.Invoke(currentCtx);
                Debug.Assert(currentCtx.EHStack.Count == 0);

                object retVal = null;
                if (sig.RetType != typeof(void))
                {
                    var retSlot = currentCtx.Registers[Constants.REG_R0];
                    if (Type.GetTypeCode(sig.RetType) == TypeCode.String && retSlot.O == null)
                        retVal = Data.LookupString(retSlot.U4);
                    else
                        retVal = retSlot.ToObject(sig.RetType);
                }
                return retVal;
            }
            finally
            {
                currentCtx.Stack.FreeAllLocalloc();

                if (ctxStack.Count > 0)
                    currentCtx = ctxStack.Pop();
            }
        }

        [VMProtect.BeginMutation]
        private object Invoke(ulong codeAddr, uint key, VMFuncSig sig, void*[] arguments)
        {
            if (currentCtx != null)
                ctxStack.Push(currentCtx);
            currentCtx = new VMContext(this);

            try
            {
                Debug.Assert(sig.ParamTypes.Length == arguments.Length);
                currentCtx.Stack.SetTopPosition((uint)arguments.Length + 1);
                for (uint i = 0; i < arguments.Length; i++)
                {
                    var paramType = sig.ParamTypes[i];
                    if (paramType.IsByRef)
                    {
                        currentCtx.Stack[i + 1] = new VMSlot { O = new TypedRef(arguments[i]) };
                    }
                    else
                    {
                        var typedRef = *(TypedReference*)arguments[i];
                        currentCtx.Stack[i + 1] = VMSlot.FromObject(TypedReference.ToObject(typedRef), __reftype(typedRef));
                    }
                }
                currentCtx.Stack[(uint)arguments.Length + 1] = new VMSlot { U8 = 1 };

                currentCtx.Registers[Constants.REG_K1] = new VMSlot { U4 = key };
                currentCtx.Registers[Constants.REG_BP] = new VMSlot { U4 = 0 };
                currentCtx.Registers[Constants.REG_SP] = new VMSlot { U4 = (uint)arguments.Length + 1 };
                currentCtx.Registers[Constants.REG_IP] = new VMSlot { U8 = codeAddr };
                VMDispatcher.Invoke(currentCtx);
                Debug.Assert(currentCtx.EHStack.Count == 0);

                object retVal = null;
                if (sig.RetType != typeof(void))
                {
                    var retSlot = currentCtx.Registers[Constants.REG_R0];
                    if (Type.GetTypeCode(sig.RetType) == TypeCode.String && retSlot.O == null)
                        retVal = Data.LookupString(retSlot.U4);
                    else
                        retVal = retSlot.ToObject(sig.RetType);
                }
                return retVal;
            }
            finally
            {
                currentCtx.Stack.FreeAllLocalloc();

                if (ctxStack.Count > 0)
                    currentCtx = ctxStack.Pop();
            }
        }
    }
}