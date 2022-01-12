using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MaMoVM.Runtime.Data
{
    internal unsafe class VMData
    {
        private static readonly Dictionary<Module, VMData> moduleVMData = new Dictionary<Module, VMData>();
        private readonly Dictionary<uint, VMExportInfo> exports;

        private readonly Dictionary<uint, RefInfo> references;
        private readonly Dictionary<uint, string> strings;


        [DllImport("kernel32.dll", EntryPoint = "CopyMemory")]
        private static extern void CopyMemory(void* dest, void* src, uint count);

        [VMProtect.BeginMutation]
        public VMData(Module module)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



            string ResDATAName = VMProtect.SDK.DecryptString("E2960449-24B8-42C3-B713-F419A5B8CE3D");
            string ResAESPasswordDATAName = VMProtect.SDK.DecryptString("C8AEF06D-C8FA-4349-BA5B-429B6E18CD43");

            byte[] AESPassword = QuickLZ.Decompress(ExtractResource(ResAESPasswordDATAName));
            byte[] SourceDATA = new AES128.AES(AESPassword).Decrypt(QuickLZ.Decompress(ExtractResource(ResDATAName)));



            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            IntPtr memIntPtr = Marshal.AllocHGlobal(SourceDATA.Length);
            byte* memBytePtr = (byte*)memIntPtr.ToPointer();

            UnmanagedMemoryStream writeStream = new UnmanagedMemoryStream(memBytePtr, SourceDATA.Length, SourceDATA.Length, FileAccess.Write);
            writeStream.Write(SourceDATA, 0, SourceDATA.Length);
            writeStream.Close();

            UnmanagedMemoryStream unmanagedMemoryStream = new UnmanagedMemoryStream(memBytePtr, SourceDATA.Length, SourceDATA.Length, FileAccess.Read);
           
            var data = (void*)Marshal.AllocHGlobal((int)(uint)(int)unmanagedMemoryStream.Length);
            CopyMemory(data, (void*)unmanagedMemoryStream.PositionPointer, (uint)(int)unmanagedMemoryStream.Length);     
            
            var header = (VMDAT_HEADER*)data;

            references = new Dictionary<uint, RefInfo>();
            strings = new Dictionary<uint, string>();
            exports = new Dictionary<uint, VMExportInfo>();

            var ptr = (byte*)(header + 1);
            for (var i = 0; i < header->MD_COUNT; i++)
            {
                var id = Utils.ReadCompressedUInt(ref ptr);
                var token = (int)Utils.FromCodedToken(Utils.ReadCompressedUInt(ref ptr));
                references[id] = new RefInfo
                {
                    module = module,
                    token = token
                };
            }
            for (var i = 0; i < header->STR_COUNT; i++)
            {
                var id = Utils.ReadCompressedUInt(ref ptr);
                var len = Utils.ReadCompressedUInt(ref ptr);
                strings[id] = new string((char*)ptr, 0, (int)len);
                ptr += len << 1;
            }
            for (var i = 0; i < header->EXP_COUNT; i++) exports[Utils.ReadCompressedUInt(ref ptr)] = new VMExportInfo(ref ptr, module);

            KoiSection = (byte*)data;

            Module = module;
            moduleVMData[module] = this;
        }

        public Module Module
        {
            get;
        }

        public byte* KoiSection
        {
            get;
            set;
        }

        [VMProtect.BeginUltra]
        public static byte[] ExtractResource(string filename)
        {
            using (Stream resFilestream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))
            {
                if (resFilestream == null) return null;
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }

        [VMProtect.BeginUltra]
        public static VMData Instance(Module module)
        {
            VMData data;
            lock (moduleVMData)
            {
                if (!moduleVMData.TryGetValue(module, out data))
                {
                    if (!BitConverter.IsLittleEndian)
                    {
                        throw new PlatformNotSupportedException();
                    }
                    else
                    {
                        data = moduleVMData[module] = new VMData(module);
                    }
                }
            }
            return data;
        }

        [VMProtect.BeginMutation]
        public MemberInfo LookupReference(uint id)
        {
            return references[id].Member;
        }

        [VMProtect.BeginUltra]
        public string LookupString(uint id)
        {
            if (id == 0)
                return null;
            return strings[id];
        }

        [VMProtect.BeginMutation]
        public VMExportInfo LookupExport(uint id)
        {
            return exports[id];
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VMDAT_HEADER
        {
            public readonly uint MAGIC;
            public readonly uint MD_COUNT;
            public readonly uint STR_COUNT;
            public readonly uint EXP_COUNT;
        }

        private class RefInfo
        {
            public Module module;
            public MemberInfo resolved;
            public int token;

            [VMProtect.BeginUltra]
            public MemberInfo Member => resolved ?? (resolved = module.ResolveMember(token));
        }
    }
}