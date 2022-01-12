using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using MaMoVM.Confuser.Core.AST.IL;

namespace MaMoVM.Confuser.Core.RT
{
    internal class DbgWriter
    {
        private byte[] dbgInfo;
        private readonly HashSet<string> documents = new HashSet<string>();

        private readonly Dictionary<ILBlock, List<DbgEntry>> entries = new Dictionary<ILBlock, List<DbgEntry>>();

        public void AddSequencePoint(ILBlock block, uint offset, uint len, string document, uint lineNum)
        {
            List<DbgEntry> entryList;
            if(!entries.TryGetValue(block, out entryList))
                entryList = entries[block] = new List<DbgEntry>();

            entryList.Add(new DbgEntry
            {
                offset = offset,
                len = len,
                document = document,
                lineNum = lineNum
            });
            documents.Add(document);
        }

        public byte[] GetDbgInfo()
        {
            return dbgInfo;
        }

        private struct DbgEntry
        {
            public uint offset;
            public uint len;

            public string document;
            public uint lineNum;
        }
    }
}