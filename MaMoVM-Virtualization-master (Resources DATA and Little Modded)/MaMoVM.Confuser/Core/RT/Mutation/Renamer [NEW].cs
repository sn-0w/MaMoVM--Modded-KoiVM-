using System.Linq;
using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;

namespace MaMoVM.Confuser.Core.RT.Mutation
{
    public class Renamer
    {
        private static readonly System.Random _RND = new System.Random();

        public Renamer(int seed) { }

        public string NewName(string name)
        {
            byte[] hash = SHA256.Create().ComputeHash(Encoding.Unicode.GetBytes(name));
            string result = string.Empty;

            for (int i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("X2");
            }
            return result.Substring(0, 8);
        }

        public void Process(ModuleDef module)
        {
            foreach (var type in module.GetTypes())
            {
                type.Namespace = "";
                type.Name = string.Concat(Enumerable.Range(0, 8).Select(_ => _RND.Next(16).ToString("X")));

                foreach (var genParam in type.GenericParameters)
                    genParam.Name = "";

                var isDelegate = type.BaseType != null &&
                                 (type.BaseType.FullName == "System.Delegate" ||
                                  type.BaseType.FullName == "System.MulticastDelegate");

                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                        foreach (var instr in method.Body.Instructions)
                        {
                            var memberRef = instr.Operand as MemberRef;
                            if (memberRef != null)
                            {
                                var typeDef = memberRef.DeclaringType.ResolveTypeDef();

                                if (memberRef.IsMethodRef && typeDef != null)
                                {
                                    var target = typeDef.ResolveMethod(memberRef);
                                    if (target != null && target.IsRuntimeSpecialName)
                                        typeDef = null;
                                }

                                if (typeDef != null && typeDef.Module == module)
                                    memberRef.Name = NewName(memberRef.Name);
                            }
                        }

                    foreach (var arg in method.Parameters)
                        arg.Name = "";
                    if (method.IsRuntimeSpecialName || isDelegate /* || type.IsPublic */)
                        continue;
                    method.Name = NewName(method.Name);
                }
                for (var i = 0; i < type.Fields.Count; i++)
                {
                    var field = type.Fields[i];
                    if (field.IsLiteral)
                    {
                        type.Fields.RemoveAt(i--);
                        continue;
                    }
                    if (field.IsRuntimeSpecialName)
                        continue;
                    field.Name = NewName(field.Name);
                }
                type.Properties.Clear();
                type.Events.Clear();
            }
        }
    }
}