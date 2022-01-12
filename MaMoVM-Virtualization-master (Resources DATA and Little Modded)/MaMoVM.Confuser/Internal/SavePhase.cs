using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Confuser.Core;
using dnlib.DotNet;

namespace MaMoVM.Confuser.Internal
{
    public class SavePhase : ProtectionPhase
    {
        public SavePhase(Protection parent) : base(parent) { }

        public override ProtectionTargets Targets => ProtectionTargets.Modules;

        public override string Name => "Save Runtime Library";

        protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
        {
            if (File.Exists(Path.Combine(context.OutputDirectory, (string)VMHelper.OUTPUTDLLName)))
            {
                File.Delete(Path.Combine(context.OutputDirectory, (string)VMHelper.OUTPUTDLLName));
            }

            ModuleDef DLLModule = ModuleDefMD.Load(Core.RT.VMRuntime.RuntimeByte);

            MemoryStream DuplicateRemovedTypeList = new MemoryStream();
            StringReader reader = new StringReader(Core.RT.VMRuntime.TypeList.ToString());
            using (StreamWriter writer = new StreamWriter(DuplicateRemovedTypeList))
            {
                string currentLine;
                string lastLine = null;

                while ((currentLine = reader.ReadLine()) != null)
                {
                    if (currentLine != lastLine)
                    {
                        writer.WriteLine(currentLine);
                        lastLine = currentLine;
                    }
                }
            }

            /**********************************************************************************************************************************************************************************************************************************************************************************************************/


            ////// Write Resources DATA
            byte[] AESPassword = GetByteArray(15);
            byte[] VMData = Core.QuickLZ.Compress(new Core.AES128.AES(AESPassword).Encrypt(Core.RT.VMRuntime.DATA));

            DLLModule.Resources.Add(new EmbeddedResource("E2960449-24B8-42C3-B713-F419A5B8CE3D", VMData, ManifestResourceAttributes.Private));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            ////// Write Resources AES Password
            DLLModule.Resources.Add(new EmbeddedResource("C8AEF06D-C8FA-4349-BA5B-429B6E18CD43", Core.QuickLZ.Compress(AESPassword), ManifestResourceAttributes.Private));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




            ////// Write Type About
            byte[] TypeEncryptionPasswordArray = GetByteArray(1);

            DLLModule.Resources.Add(new EmbeddedResource("B3D2A594-D219-4527-A122-BF81B5EBAE99", TypeEncryptSystem(DuplicateRemovedTypeList.ToArray(), TypeEncryptionPasswordArray), ManifestResourceAttributes.Private));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            ////// Write Type About Encryption Password
            DLLModule.Resources.Add(new EmbeddedResource("FB15D486-9EB4-4E00-9104-FCC1D0309D55", TypeEncryptionPasswordArray, ManifestResourceAttributes.Private));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




            MemoryStream memorystream = new MemoryStream();
            DLLModule.Write(memorystream);
            Core.RT.VMRuntime.TypeList.Clear();
            /**********************************************************************************************************************************************************************************************************************************************************************************************************/




            var file = Path.Combine(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            Directory.CreateDirectory(context.OutputDirectory);
            File.WriteAllBytes(file, memorystream.ToArray());


            byte[] EX1 = new byte[8];
            EX1[0] = 46;
            EX1[1] = 0;
            EX1[2] = 101;
            EX1[3] = 0;
            EX1[4] = 120;
            EX1[5] = 0;
            EX1[6] = 101;
            EX1[7] = 0;
            string VMPEXEName = Path.GetRandomFileName() + Encoding.Unicode.GetString(EX1); // EX1 = ".exe"
            string VMPPROJName = Path.GetRandomFileName();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), VMPPROJName), Properties.Resources.SeKLxcb6qX0KIM8JWy3GiA);
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), VMPEXEName), Properties.Resources._80DMUlAjbQM3mEyuNvXTog);
            ReplaceInFile(Path.Combine(Path.GetDirectoryName(file), VMPPROJName), "dlldir", file);
            ReplaceInFile(Path.Combine(Path.GetDirectoryName(file), VMPPROJName), "dlloutdir", Path.Combine(context.OutputDirectory, (string)VMHelper.OUTPUTDLLName));
            ReplaceInFile(Path.Combine(Path.GetDirectoryName(file), VMPPROJName), "sectionName", Path.GetRandomFileName());
            Thread.Sleep(300);
            ProcessSTR("\"" + Path.Combine(Path.GetDirectoryName(file), VMPEXEName) + "\"", "\"" + Path.Combine(Path.GetDirectoryName(file), VMPPROJName) + "\"");
            Thread.Sleep(300);
            Directory.Delete(Path.GetDirectoryName(file), true);
        }

        public byte[] GetByteArray(int sizeInKb)
        {
            byte[] b = new byte[sizeInKb * 1024];
            new Random().NextBytes(b);
            return b;
        }

        public static byte[] TypeEncryptSystem(byte[] array, byte[] password)
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
            for (int i = 0; i < array.Length; i++)
            {
                int shift = hashedPasswordBytes[passwordShiftIndex];
                array[i] = shift <= 128
                    ? (byte)(array[i] + (shiftFlag
                        ? (byte)(((shift << 2)) % 255)
                        : (byte)(((shift << 4)) % 255)))
                    : (byte)(array[i] - (shiftFlag
                        ? (byte)(((shift << 4)) % 255)
                        : (byte)(((shift << 2)) % 255)));
                passwordShiftIndex = (passwordShiftIndex + 1) % 64;
                shiftFlag = !shiftFlag;
            }
            return array;
        }

        static public void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            StreamReader reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();
            content = System.Text.RegularExpressions.Regex.Replace(content, searchText, replaceText);
            StreamWriter writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }


        static public void ProcessSTR(string filename, string command)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = filename,
                Arguments = command
            };
            process.Start();
            process.WaitForExit();
        }
    }
}