using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace MaMoVM.Confuser.Core.RT.Mutation
{
    internal class MethodPatcher
    {
        private readonly MethodDef vmEntry;

        public MethodPatcher(ModuleDef rtModule)
        {
            foreach (var entry in rtModule.Find(RTMap.VMEntry, true).FindMethods(RTMap.VMRun))
            {
                vmEntry = entry;
            }
        }

        public void Patch(ModuleDef module, MethodDef method, int id)
        {
            var body = new CilBody();
            method.Body = body;
            id *= 8;






            // Object Array
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 1)); // array içinde kaç tane işlem ekliyecen onu belirleme 1 tane varsa 1 2 tane varsa 2 yaz
            body.Instructions.Add(Instruction.Create(OpCodes.Newarr, method.Module.CorLibTypes.Object.ToTypeDefOrRef())); // buda arrayı object olarak belirlemek için.
            body.Instructions.Add(Instruction.Create(OpCodes.Dup));




            ////////// Array[0]
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 0)); // Array[0]
            body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, EncryptID(Guid.NewGuid().ToString().Substring(0, 10) + string.Format("{0}", id))));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            ////////// Eğer Array'a bir işlem daha ekliyeceksen kodlarının aralarına bu 2 kodu ekle:
            //body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            //body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




            body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, method.Parameters.Count));
            body.Instructions.Add(Instruction.Create(OpCodes.Newarr, new PtrSig(method.Module.CorLibTypes.Void).ToTypeDefOrRef()));

            foreach (var param in method.Parameters)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, param.Index));
                if (param.Type.IsByRef)
                {
                    body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, param));
                    body.Instructions.Add(Instruction.Create(OpCodes.Mkrefany, param.Type.Next.ToTypeDefOrRef()));
                }
                else
                {
                    body.Instructions.Add(Instruction.Create(OpCodes.Ldarga, param));
                    body.Instructions.Add(Instruction.Create(OpCodes.Mkrefany, param.Type.ToTypeDefOrRef()));
                }
                var locals = new Local(method.Module.CorLibTypes.TypedReference);
                body.Variables.Add(locals);
                body.Instructions.Add(Instruction.Create(OpCodes.Stloc, locals));
                body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, locals));
                body.Instructions.Add(Instruction.Create(OpCodes.Conv_I));
                body.Instructions.Add(Instruction.Create(OpCodes.Stelem_I));
            }
            body.Instructions.Add(Instruction.Create(OpCodes.Call, method.Module.Import(vmEntry)));

            if (method.ReturnType.ElementType == ElementType.Void)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            else if (method.ReturnType.IsValueType)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.ReturnType.ToTypeDefOrRef()));
            }
            else
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Castclass, method.ReturnType.ToTypeDefOrRef()));
            }
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            body.OptimizeMacros();

            VMRuntime.TypeList.Append(method.DeclaringType.AssemblyQualifiedName + Environment.NewLine);
        }

        private string EncryptID(string inputStr)
        {
            int length = inputStr.Length;
            char[] array = new char[length];
            for (int i = 0; i < array.Length; i++)
            {
                char c = inputStr[i];
                byte b = (byte)(c ^ length - i);
                byte b2 = (byte)(c >> 8 ^ i);
                array[i] = (char)(b2 << 8 | (int)b);
            }
            return string.Intern(new string(array));
        }
    }
}