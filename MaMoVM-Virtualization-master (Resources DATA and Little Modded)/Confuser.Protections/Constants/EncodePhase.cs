using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Confuser.Protections.Constants
{
	// Token: 0x02000096 RID: 150
	internal class EncodePhase : ProtectionPhase
	{
		// Token: 0x06000245 RID: 581 RVA: 0x00004A51 File Offset: 0x00002C51
		public EncodePhase(ConstantProtection parent) : base(parent)
		{
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x06000246 RID: 582 RVA: 0x00009294 File Offset: 0x00007494
		public override ProtectionTargets Targets
		{
			get
			{
				return ProtectionTargets.Methods;
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000247 RID: 583 RVA: 0x000131D4 File Offset: 0x000113D4
		public override string Name
		{
			get
			{
				return "Constants encoding";
			}
		}

		// Token: 0x06000248 RID: 584 RVA: 0x000131EC File Offset: 0x000113EC
		protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
		{
			CEContext moduleCtx = context.Annotations.Get<CEContext>(context.CurrentModule, ConstantProtection.ContextKey, null);
			bool flag = !parameters.Targets.Any<IDnlibDef>() || moduleCtx == null;
			if (!flag)
			{
				Dictionary<object, List<Tuple<MethodDef, Instruction>>> ldc = new Dictionary<object, List<Tuple<MethodDef, Instruction>>>();
				Dictionary<byte[], List<Tuple<MethodDef, Instruction>>> ldInit = new Dictionary<byte[], List<Tuple<MethodDef, Instruction>>>(new EncodePhase.ByteArrayComparer());
				this.ExtractConstants(context, parameters, moduleCtx, ldc, ldInit);
				moduleCtx.ReferenceRepl = new Dictionary<MethodDef, List<Tuple<Instruction, uint, IMethod>>>();
				moduleCtx.EncodedBuffer = new List<uint>();
				foreach (KeyValuePair<byte[], List<Tuple<MethodDef, Instruction>>> entry in ldInit.WithProgress(context.Logger))
				{
					this.EncodeInitializer(moduleCtx, entry.Key, entry.Value);
					context.CheckCancellation();
				}
				foreach (KeyValuePair<object, List<Tuple<MethodDef, Instruction>>> entry2 in ldc.WithProgress(context.Logger))
				{
					bool flag2 = entry2.Key is string;
					if (flag2)
					{
						this.EncodeString(moduleCtx, (string)entry2.Key, entry2.Value);
					}
					else
					{
						bool flag3 = entry2.Key is int;
						if (flag3)
						{
							this.EncodeConstant32(moduleCtx, (uint)((int)entry2.Key), context.CurrentModule.CorLibTypes.Int32, entry2.Value);
						}
						else
						{
							bool flag4 = entry2.Key is long;
							if (flag4)
							{
								this.EncodeConstant64(moduleCtx, (uint)((long)entry2.Key >> 32), (uint)((long)entry2.Key), context.CurrentModule.CorLibTypes.Int64, entry2.Value);
							}
							else
							{
								bool flag5 = entry2.Key is float;
								if (flag5)
								{
									EncodePhase.RTransform t = default(EncodePhase.RTransform);
									t.R4 = (float)entry2.Key;
									this.EncodeConstant32(moduleCtx, t.Lo, context.CurrentModule.CorLibTypes.Single, entry2.Value);
								}
								else
								{
									bool flag6 = entry2.Key is double;
									if (!flag6)
									{
										throw new UnreachableException();
									}
									EncodePhase.RTransform t2 = default(EncodePhase.RTransform);
									t2.R8 = (double)entry2.Key;
									this.EncodeConstant64(moduleCtx, t2.Hi, t2.Lo, context.CurrentModule.CorLibTypes.Double, entry2.Value);
								}
							}
						}
					}
					context.CheckCancellation();
				}
				ReferenceReplacer.ReplaceReference(moduleCtx, parameters);
				byte[] encodedBuff = new byte[moduleCtx.EncodedBuffer.Count * 4];
				int buffIndex = 0;
				foreach (uint dat in moduleCtx.EncodedBuffer)
				{
					encodedBuff[buffIndex++] = (byte)(dat & 255U);
					encodedBuff[buffIndex++] = (byte)(dat >> 8 & 255U);
					encodedBuff[buffIndex++] = (byte)(dat >> 16 & 255U);
					encodedBuff[buffIndex++] = (byte)(dat >> 24 & 255U);
				}
				Debug.Assert(buffIndex == encodedBuff.Length);
				encodedBuff = context.Registry.GetService<ICompressionService>().Compress(encodedBuff, null);
				context.CheckCancellation();
				uint compressedLen = (uint)((encodedBuff.Length + 3) / 4);
				compressedLen = (compressedLen + 15U & 4294967280U);
				uint[] compressedBuff = new uint[compressedLen];
				Buffer.BlockCopy(encodedBuff, 0, compressedBuff, 0, encodedBuff.Length);
				Debug.Assert(compressedLen % 16U == 0U);
				uint keySeed = moduleCtx.Random.NextUInt32();
				uint[] key = new uint[16];
				uint state = keySeed;
				for (int i = 0; i < 16; i++)
				{
					state ^= state >> 12;
					state ^= state << 25;
					state ^= state >> 27;
					key[i] = state;
				}
				byte[] encryptedBuffer = new byte[compressedBuff.Length * 4];
				for (buffIndex = 0; buffIndex < compressedBuff.Length; buffIndex += 16)
				{
					uint[] enc = moduleCtx.ModeHandler.Encrypt(compressedBuff, buffIndex, key);
					for (int j = 0; j < 16; j++)
					{
						key[j] ^= compressedBuff[buffIndex + j];
					}
					Buffer.BlockCopy(enc, 0, encryptedBuffer, buffIndex * 4, 64);
				}
				Debug.Assert(buffIndex == compressedBuff.Length);
				moduleCtx.DataField.InitialValue = encryptedBuffer;
				moduleCtx.DataField.HasFieldRVA = true;
				moduleCtx.DataType.ClassLayout = new ClassLayoutUser(0, (uint)encryptedBuffer.Length);
				MutationHelper.InjectKeys(moduleCtx.InitMethod, new int[]
				{
					0,
					1
				}, new int[]
				{
					encryptedBuffer.Length / 4,
					(int)keySeed
				});
				MutationHelper.ReplacePlaceholder(moduleCtx.InitMethod, delegate (Instruction[] arg)
				{
					List<Instruction> repl = new List<Instruction>();
					repl.AddRange(arg);
					repl.Add(Instruction.Create(OpCodes.Dup));
					repl.Add(Instruction.Create(OpCodes.Ldtoken, moduleCtx.DataField));
					repl.Add(Instruction.Create(OpCodes.Call, moduleCtx.Module.Import(typeof(RuntimeHelpers).GetMethod("InitializeArray"))));
					return repl.ToArray();
				});
			}
		}

		// Token: 0x06000249 RID: 585 RVA: 0x000137A8 File Offset: 0x000119A8
		private void EncodeString(CEContext moduleCtx, string value, List<Tuple<MethodDef, Instruction>> references)
		{
			int buffIndex = this.EncodeByteArray(moduleCtx, Encoding.UTF8.GetBytes(value));
			this.UpdateReference(moduleCtx, moduleCtx.Module.CorLibTypes.String, references, buffIndex, (DecoderDesc desc) => desc.StringID);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00013804 File Offset: 0x00011A04
		private void EncodeConstant32(CEContext moduleCtx, uint value, TypeSig valueType, List<Tuple<MethodDef, Instruction>> references)
		{
			int buffIndex = moduleCtx.EncodedBuffer.IndexOf(value);
			bool flag = buffIndex == -1;
			if (flag)
			{
				buffIndex = moduleCtx.EncodedBuffer.Count;
				moduleCtx.EncodedBuffer.Add(value);
			}
			this.UpdateReference(moduleCtx, valueType, references, buffIndex, (DecoderDesc desc) => desc.NumberID);
		}

		// Token: 0x0600024B RID: 587 RVA: 0x00013870 File Offset: 0x00011A70
		private void EncodeConstant64(CEContext moduleCtx, uint hi, uint lo, TypeSig valueType, List<Tuple<MethodDef, Instruction>> references)
		{
			int buffIndex = -1;
			do
			{
				buffIndex = moduleCtx.EncodedBuffer.IndexOf(lo, buffIndex + 1);
				bool flag = buffIndex + 1 < moduleCtx.EncodedBuffer.Count && moduleCtx.EncodedBuffer[buffIndex + 1] == hi;
				if (flag)
				{
					break;
				}
			}
			while (buffIndex >= 0);
			bool flag2 = buffIndex == -1;
			if (flag2)
			{
				buffIndex = moduleCtx.EncodedBuffer.Count;
				moduleCtx.EncodedBuffer.Add(lo);
				moduleCtx.EncodedBuffer.Add(hi);
			}
			this.UpdateReference(moduleCtx, valueType, references, buffIndex, (DecoderDesc desc) => desc.NumberID);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x00013924 File Offset: 0x00011B24
		private void EncodeInitializer(CEContext moduleCtx, byte[] init, List<Tuple<MethodDef, Instruction>> references)
		{
			int buffIndex = -1;
			foreach (Tuple<MethodDef, Instruction> instr in references)
			{
				IList<Instruction> instrs = instr.Item1.Body.Instructions;
				int i = instrs.IndexOf(instr.Item2);
				bool flag = buffIndex == -1;
				if (flag)
				{
					buffIndex = this.EncodeByteArray(moduleCtx, init);
				}
				Tuple<MethodDef, DecoderDesc> decoder = moduleCtx.Decoders[moduleCtx.Random.NextInt32(moduleCtx.Decoders.Count)];
				uint id = (uint)(buffIndex | (int)decoder.Item2.InitializerID << 30);
				id = moduleCtx.ModeHandler.Encode(decoder.Item2.Data, moduleCtx, id);
				instrs[i - 4].Operand = (int)id;
				instrs[i - 3].OpCode = OpCodes.Call;
				SZArraySig arrType = new SZArraySig(((ITypeDefOrRef)instrs[i - 3].Operand).ToTypeSig());
				instrs[i - 3].Operand = new MethodSpecUser(decoder.Item1, new GenericInstMethodSig(arrType));
				instrs.RemoveAt(i - 2);
				instrs.RemoveAt(i - 2);
				instrs.RemoveAt(i - 2);
			}
		}

		// Token: 0x0600024D RID: 589 RVA: 0x00013A98 File Offset: 0x00011C98
		private int EncodeByteArray(CEContext moduleCtx, byte[] buff)
		{
			int buffIndex = moduleCtx.EncodedBuffer.Count;
			moduleCtx.EncodedBuffer.Add((uint)buff.Length);
			int integral = buff.Length / 4;
			int remainder = buff.Length % 4;
			for (int i = 0; i < integral; i++)
			{
				uint data = (uint)((int)buff[i * 4] | (int)buff[i * 4 + 1] << 8 | (int)buff[i * 4 + 2] << 16 | (int)buff[i * 4 + 3] << 24);
				moduleCtx.EncodedBuffer.Add(data);
			}
			bool flag = remainder > 0;
			if (flag)
			{
				int baseIndex = integral * 4;
				uint r = 0U;
				for (int j = 0; j < remainder; j++)
				{
					r |= (uint)((uint)buff[baseIndex + j] << j * 8);
				}
				moduleCtx.EncodedBuffer.Add(r);
			}
			return buffIndex;
		}

		// Token: 0x0600024E RID: 590 RVA: 0x00013B6C File Offset: 0x00011D6C
		private void UpdateReference(CEContext moduleCtx, TypeSig valueType, List<Tuple<MethodDef, Instruction>> references, int buffIndex, Func<DecoderDesc, byte> typeID)
		{
			foreach (Tuple<MethodDef, Instruction> instr in references)
			{
				Tuple<MethodDef, DecoderDesc> decoder = moduleCtx.Decoders[moduleCtx.Random.NextInt32(moduleCtx.Decoders.Count)];
				uint id = (uint)(buffIndex | (int)typeID(decoder.Item2) << 30);
				id = moduleCtx.ModeHandler.Encode(decoder.Item2.Data, moduleCtx, id);
				MethodSpecUser targetDecoder = new MethodSpecUser(decoder.Item1, new GenericInstMethodSig(valueType));
				moduleCtx.ReferenceRepl.AddListEntry(instr.Item1, Tuple.Create<Instruction, uint, IMethod>(instr.Item2, id, targetDecoder));
			}
		}

		// Token: 0x0600024F RID: 591 RVA: 0x00013C40 File Offset: 0x00011E40
		private void RemoveDataFieldRefs(ConfuserContext context, HashSet<FieldDef> dataFields, HashSet<Instruction> fieldRefs)
		{
			foreach (TypeDef type in context.CurrentModule.GetTypes())
			{
				foreach (MethodDef method in from m in type.Methods
											 where m.HasBody
											 select m)
				{
					foreach (Instruction instr in method.Body.Instructions)
					{
						bool flag = instr.Operand is FieldDef && !fieldRefs.Contains(instr);
						if (flag)
						{
							dataFields.Remove((FieldDef)instr.Operand);
						}
					}
				}
			}
			foreach (FieldDef fieldToRemove in dataFields)
			{
				fieldToRemove.DeclaringType.Fields.Remove(fieldToRemove);
			}
		}

		// Token: 0x06000250 RID: 592 RVA: 0x00013DB8 File Offset: 0x00011FB8
		private void ExtractConstants(ConfuserContext context, ProtectionParameters parameters, CEContext moduleCtx, Dictionary<object, List<Tuple<MethodDef, Instruction>>> ldc, Dictionary<byte[], List<Tuple<MethodDef, Instruction>>> ldInit)
		{
			HashSet<FieldDef> dataFields = new HashSet<FieldDef>();
			HashSet<Instruction> fieldRefs = new HashSet<Instruction>();
			foreach (MethodDef method in parameters.Targets.OfType<MethodDef>().WithProgress(context.Logger))
			{
				bool flag = !method.HasBody;
				if (!flag)
				{
					moduleCtx.Elements = (EncodeElements)0;
					string elements = parameters.GetParameter<string>(context, method, "elements", "SINP");
					string text = elements;
					int j = 0;
					while (j < text.Length)
					{
						char elem = text[j];
						char c = elem;
						if (c <= 'S')
						{
							if (c <= 'N')
							{
								if (c == 'I')
								{
									goto IL_104;
								}
								if (c == 'N')
								{
									goto IL_E4;
								}
							}
							else
							{
								if (c == 'P')
								{
									goto IL_F4;
								}
								if (c == 'S')
								{
									goto IL_D4;
								}
							}
						}
						else if (c <= 'n')
						{
							if (c == 'i')
							{
								goto IL_104;
							}
							if (c == 'n')
							{
								goto IL_E4;
							}
						}
						else
						{
							if (c == 'p')
							{
								goto IL_F4;
							}
							if (c == 's')
							{
								goto IL_D4;
							}
						}
					IL_114:
						j++;
						continue;
					IL_D4:
						moduleCtx.Elements |= EncodeElements.Strings;
						goto IL_114;
					IL_E4:
						moduleCtx.Elements |= EncodeElements.Numbers;
						goto IL_114;
					IL_F4:
						moduleCtx.Elements |= EncodeElements.Primitive;
						goto IL_114;
					IL_104:
						moduleCtx.Elements |= EncodeElements.Initializers;
						goto IL_114;
					}
					bool flag2 = moduleCtx.Elements == (EncodeElements)0;
					if (!flag2)
					{
						foreach (Instruction instr in method.Body.Instructions)
						{
							bool eligible = false;
							bool flag3 = instr.OpCode == OpCodes.Ldstr && (moduleCtx.Elements & EncodeElements.Strings) > (EncodeElements)0;
							if (flag3)
							{
								string operand = (string)instr.Operand;
								bool flag4 = string.IsNullOrEmpty(operand) && (moduleCtx.Elements & EncodeElements.Primitive) == (EncodeElements)0;
								if (flag4)
								{
									continue;
								}
								eligible = true;
							}
							else
							{
								bool flag5 = instr.OpCode == OpCodes.Call && (moduleCtx.Elements & EncodeElements.Initializers) > (EncodeElements)0;
								if (flag5)
								{
									IMethod operand2 = (IMethod)instr.Operand;
									bool flag6 = operand2.DeclaringType.DefinitionAssembly.IsCorLib() && operand2.DeclaringType.Namespace == "System.Runtime.CompilerServices" && operand2.DeclaringType.Name == "RuntimeHelpers" && operand2.Name == "InitializeArray";
									if (flag6)
									{
										IList<Instruction> instrs = method.Body.Instructions;
										int i = instrs.IndexOf(instr);
										bool flag7 = instrs[i - 1].OpCode != OpCodes.Ldtoken;
										if (flag7)
										{
											continue;
										}
										bool flag8 = instrs[i - 2].OpCode != OpCodes.Dup;
										if (flag8)
										{
											continue;
										}
										bool flag9 = instrs[i - 3].OpCode != OpCodes.Newarr;
										if (flag9)
										{
											continue;
										}
										bool flag10 = instrs[i - 4].OpCode != OpCodes.Ldc_I4;
										if (flag10)
										{
											continue;
										}
										FieldDef dataField = instrs[i - 1].Operand as FieldDef;
										bool flag11 = dataField == null;
										if (flag11)
										{
											continue;
										}
										bool flag12 = !dataField.HasFieldRVA || dataField.InitialValue == null;
										if (flag12)
										{
											continue;
										}
										int arrLen = (int)instrs[i - 4].Operand;
										bool flag13 = ldc.ContainsKey(arrLen);
										if (flag13)
										{
											List<Tuple<MethodDef, Instruction>> list = ldc[arrLen];
											list.RemoveWhere((Tuple<MethodDef, Instruction> entry) => entry.Item2 == instrs[i - 4]);
											bool flag14 = list.Count == 0;
											if (flag14)
											{
												ldc.Remove(arrLen);
											}
										}
										dataFields.Add(dataField);
										fieldRefs.Add(instrs[i - 1]);
										byte[] value = new byte[dataField.InitialValue.Length + 4];
										value[0] = (byte)arrLen;
										value[1] = (byte)(arrLen >> 8);
										value[2] = (byte)(arrLen >> 16);
										value[3] = (byte)(arrLen >> 24);
										Buffer.BlockCopy(dataField.InitialValue, 0, value, 4, dataField.InitialValue.Length);
										ldInit.AddListEntry(value, Tuple.Create<MethodDef, Instruction>(method, instr));
									}
								}
								else
								{
									bool flag15 = (moduleCtx.Elements & EncodeElements.Numbers) > (EncodeElements)0;
									if (flag15)
									{
										bool flag16 = instr.OpCode == OpCodes.Ldc_I4;
										if (flag16)
										{
											int operand3 = (int)instr.Operand;
											bool flag17 = operand3 >= -1 && operand3 <= 8 && (moduleCtx.Elements & EncodeElements.Primitive) == (EncodeElements)0;
											if (flag17)
											{
												continue;
											}
											eligible = true;
										}
										else
										{
											bool flag18 = instr.OpCode == OpCodes.Ldc_I8;
											if (flag18)
											{
												long operand4 = (long)instr.Operand;
												bool flag19 = operand4 >= -1L && operand4 <= 1L && (moduleCtx.Elements & EncodeElements.Primitive) == (EncodeElements)0;
												if (flag19)
												{
													continue;
												}
												eligible = true;
											}
											else
											{
												bool flag20 = instr.OpCode == OpCodes.Ldc_R4;
												if (flag20)
												{
													float operand5 = (float)instr.Operand;
													bool flag21 = (operand5 == -1f || operand5 == 0f || operand5 == 1f) && (moduleCtx.Elements & EncodeElements.Primitive) == (EncodeElements)0;
													if (flag21)
													{
														continue;
													}
													eligible = true;
												}
												else
												{
													bool flag22 = instr.OpCode == OpCodes.Ldc_R8;
													if (flag22)
													{
														double operand6 = (double)instr.Operand;
														bool flag23 = (operand6 == -1.0 || operand6 == 0.0 || operand6 == 1.0) && (moduleCtx.Elements & EncodeElements.Primitive) == (EncodeElements)0;
														if (flag23)
														{
															continue;
														}
														eligible = true;
													}
												}
											}
										}
									}
								}
							}
							bool flag24 = eligible;
							if (flag24)
							{
								ldc.AddListEntry(instr.Operand, Tuple.Create<MethodDef, Instruction>(method, instr));
							}
						}
						context.CheckCancellation();
					}
				}
			}
			this.RemoveDataFieldRefs(context, dataFields, fieldRefs);
		}

		// Token: 0x02000097 RID: 151
		private class ByteArrayComparer : IEqualityComparer<byte[]>
		{
			// Token: 0x06000251 RID: 593 RVA: 0x0001446C File Offset: 0x0001266C
			public bool Equals(byte[] x, byte[] y)
			{
				return x.SequenceEqual(y);
			}

			// Token: 0x06000252 RID: 594 RVA: 0x00014488 File Offset: 0x00012688
			public int GetHashCode(byte[] obj)
			{
				int ret = 31;
				foreach (byte v in obj)
				{
					ret = ret * 17 + (int)v;
				}
				return ret;
			}
		}

		// Token: 0x02000098 RID: 152
		[StructLayout(LayoutKind.Explicit)]
		private struct RTransform
		{
			// Token: 0x04000194 RID: 404
			[FieldOffset(0)]
			public float R4;

			// Token: 0x04000195 RID: 405
			[FieldOffset(0)]
			public double R8;

			// Token: 0x04000196 RID: 406
			[FieldOffset(4)]
			public readonly uint Hi;

			// Token: 0x04000197 RID: 407
			[FieldOffset(0)]
			public readonly uint Lo;
		}
	}
}
