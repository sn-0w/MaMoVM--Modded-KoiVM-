using System;
using System.Collections.Generic;
using MaMoVM.Runtime.OpCodes;

namespace MaMoVM.Runtime.Data
{
    internal static class OpCodeMap
    {
        private static readonly Dictionary<byte, IOpCode> opCodes;

        [VMProtect.BeginUltra]
        static OpCodeMap()
        {
            opCodes = new Dictionary<byte, IOpCode>();
            foreach(var type in typeof(OpCodeMap).Assembly.GetTypes())
                if(typeof(IOpCode).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var opCode = (IOpCode) Activator.CreateInstance(type);
                    opCodes[opCode.Code] = opCode;
                }
        }

        [VMProtect.BeginMutation]
        public static IOpCode Lookup(byte code)
        {
            return opCodes[code];
        }
    }
}