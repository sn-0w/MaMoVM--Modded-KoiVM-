using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.DynCipher;
using Confuser.Renamer;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;

namespace Confuser.Protections.Constants
{
	// Token: 0x0200009D RID: 157
	internal class InjectPhase : ProtectionPhase
	{
		// Token: 0x06000262 RID: 610 RVA: 0x00004A51 File Offset: 0x00002C51
		public InjectPhase(ConstantProtection parent) : base(parent)
		{
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000263 RID: 611 RVA: 0x00009294 File Offset: 0x00007494
		public override ProtectionTargets Targets
		{
			get
			{
				return ProtectionTargets.Methods;
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x06000264 RID: 612 RVA: 0x00014544 File Offset: 0x00012744
		public override string Name
		{
			get
			{
				return "Constant encryption helpers injection";
			}
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0001455C File Offset: 0x0001275C
		protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
		{
			bool flag = parameters.Targets.Any<IDnlibDef>();
			if (flag)
			{
				ICompressionService compression = context.Registry.GetService<ICompressionService>();
				INameService name = context.Registry.GetService<INameService>();
				IMarkerService marker = context.Registry.GetService<IMarkerService>();
				IRuntimeService rt = context.Registry.GetService<IRuntimeService>();
				CEContext moduleCtx = new CEContext
				{
					Protection = (ConstantProtection)base.Parent,
					Random = context.Registry.GetService<IRandomService>().GetRandomGenerator(base.Parent.Id),
					Context = context,
					Module = context.CurrentModule,
					Marker = marker,
					DynCipher = context.Registry.GetService<IDynCipherService>(),
					Name = name
				};
				moduleCtx.Mode = parameters.GetParameter<Mode>(context, context.CurrentModule, "mode", Mode.Dynamic);
				moduleCtx.DecoderCount = parameters.GetParameter<int>(context, context.CurrentModule, "decoderCount", 15);
				switch (moduleCtx.Mode)
				{
					case Mode.Normal:
						moduleCtx.ModeHandler = new NormalMode();
						break;
					case Mode.Dynamic:
						moduleCtx.ModeHandler = new DynamicMode();
						break;
					case Mode.x86:
						{
							moduleCtx.ModeHandler = new x86Mode();
							bool flag2 = (context.CurrentModule.Cor20HeaderFlags & ComImageFlags.ILOnly) > (ComImageFlags)0U;
							if (flag2)
							{
								context.CurrentModuleWriterOptions.Cor20HeaderOptions.Flags &= ~ComImageFlags.ILOnly;
							}
							break;
						}
					default:
						throw new UnreachableException();
				}
				MethodDef decomp = compression.GetRuntimeDecompressor(context.CurrentModule, delegate (IDnlibDef member)
				{
					name.MarkHelper(member, marker, (Protection)this.Parent);
					bool flag3 = member is MethodDef;
					if (flag3)
					{
						ProtectionParameters.GetParameters(context, member).Remove(this.Parent);
					}
				});
				this.InjectHelpers(context, compression, rt, moduleCtx);
				this.MutateInitializer(moduleCtx, decomp);
				MethodDef cctor = context.CurrentModule.GlobalType.FindStaticConstructor();
				cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, moduleCtx.InitMethod));
				context.Annotations.Set<CEContext>(context.CurrentModule, ConstantProtection.ContextKey, moduleCtx);
			}
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0001486C File Offset: 0x00012A6C
		private void InjectHelpers(ConfuserContext context, ICompressionService compression, IRuntimeService rt, CEContext moduleCtx)
		{
			IEnumerable<IDnlibDef> members = InjectHelper.Inject(rt.GetRuntimeType("Confuser.Runtime.Constant"), context.CurrentModule.GlobalType, context.CurrentModule);
			foreach (IDnlibDef member in members)
			{
				bool flag = member.Name == "Get";
				if (flag)
				{
					context.CurrentModule.GlobalType.Remove((MethodDef)member);
				}
				else
				{
					bool flag2 = member.Name == "b";
					if (flag2)
					{
						moduleCtx.BufferField = (FieldDef)member;
					}
					else
					{
						bool flag3 = member.Name == "Initialize";
						if (flag3)
						{
							moduleCtx.InitMethod = (MethodDef)member;
						}
					}
					moduleCtx.Name.MarkHelper(member, moduleCtx.Marker, (Protection)base.Parent);
				}
			}
			ProtectionParameters.GetParameters(context, moduleCtx.InitMethod).Remove(base.Parent);
			TypeDefUser dataType = new TypeDefUser("", moduleCtx.Name.RandomName(), context.CurrentModule.CorLibTypes.GetTypeRef("System", "ValueType"));
			dataType.Layout = TypeAttributes.ExplicitLayout;
			dataType.Visibility = TypeAttributes.NestedPrivate;
			dataType.IsSealed = true;
			moduleCtx.DataType = dataType;
			context.CurrentModule.GlobalType.NestedTypes.Add(dataType);
			moduleCtx.Name.MarkHelper(dataType, moduleCtx.Marker, (Protection)base.Parent);
			moduleCtx.DataField = new FieldDefUser(moduleCtx.Name.RandomName(), new FieldSig(dataType.ToTypeSig()))
			{
				IsStatic = true,
				Access = FieldAttributes.PrivateScope
			};
			context.CurrentModule.GlobalType.Fields.Add(moduleCtx.DataField);
			moduleCtx.Name.MarkHelper(moduleCtx.DataField, moduleCtx.Marker, (Protection)base.Parent);
			MethodDef decoder = rt.GetRuntimeType("Confuser.Runtime.Constant").FindMethod("Get");
			moduleCtx.Decoders = new List<Tuple<MethodDef, DecoderDesc>>();
			for (int i = 0; i < moduleCtx.DecoderCount; i++)
			{
				MethodDef decoderInst = InjectHelper.Inject(decoder, context.CurrentModule);
				for (int j = 0; j < decoderInst.Body.Instructions.Count; j++)
				{
					Instruction instr = decoderInst.Body.Instructions[j];
					IMethod method = instr.Operand as IMethod;
					IField field = instr.Operand as IField;
					bool flag4 = instr.OpCode == OpCodes.Call && method.DeclaringType.Name == "Mutation" && method.Name == "Value";
					if (flag4)
					{
						decoderInst.Body.Instructions[j] = Instruction.Create(OpCodes.Sizeof, new GenericMVar(0).ToTypeDefOrRef());
					}
					else
					{
						bool flag5 = instr.OpCode == OpCodes.Ldsfld && method.DeclaringType.Name == "Constant";
						if (flag5)
						{
							bool flag6 = field.Name == "b";
							if (!flag6)
							{
								throw new UnreachableException();
							}
							instr.Operand = moduleCtx.BufferField;
						}
					}
				}
				context.CurrentModule.GlobalType.Methods.Add(decoderInst);
				moduleCtx.Name.MarkHelper(decoderInst, moduleCtx.Marker, (Protection)base.Parent);
				ProtectionParameters.GetParameters(context, decoderInst).Remove(base.Parent);
				DecoderDesc decoderDesc = new DecoderDesc();
				decoderDesc.StringID = (byte)(moduleCtx.Random.NextByte() & 3);
				do
				{
					decoderDesc.NumberID = (byte)(moduleCtx.Random.NextByte() & 3);
				}
				while (decoderDesc.NumberID == decoderDesc.StringID);
				do
				{
					decoderDesc.InitializerID = (byte)(moduleCtx.Random.NextByte() & 3);
				}
				while (decoderDesc.InitializerID == decoderDesc.StringID || decoderDesc.InitializerID == decoderDesc.NumberID);
				MutationHelper.InjectKeys(decoderInst, new int[]
				{
					0,
					1,
					2
				}, new int[]
				{
					(int)decoderDesc.StringID,
					(int)decoderDesc.NumberID,
					(int)decoderDesc.InitializerID
				});
				decoderDesc.Data = moduleCtx.ModeHandler.CreateDecoder(decoderInst, moduleCtx);
				moduleCtx.Decoders.Add(Tuple.Create<MethodDef, DecoderDesc>(decoderInst, decoderDesc));
			}
		}

		// Token: 0x06000267 RID: 615 RVA: 0x00014D60 File Offset: 0x00012F60
		private void MutateInitializer(CEContext moduleCtx, MethodDef decomp)
		{
			moduleCtx.InitMethod.Body.SimplifyMacros(moduleCtx.InitMethod.Parameters);
			List<Instruction> instrs = moduleCtx.InitMethod.Body.Instructions.ToList<Instruction>();
			for (int i = 0; i < instrs.Count; i++)
			{
				Instruction instr = instrs[i];
				IMethod method = instr.Operand as IMethod;
				bool flag = instr.OpCode == OpCodes.Call;
				if (flag)
				{
					bool flag2 = method.DeclaringType.Name == "Mutation" && method.Name == "Crypt";
					if (flag2)
					{
						Instruction ldBlock = instrs[i - 2];
						Instruction ldKey = instrs[i - 1];
						Debug.Assert(ldBlock.OpCode == OpCodes.Ldloc && ldKey.OpCode == OpCodes.Ldloc);
						instrs.RemoveAt(i);
						instrs.RemoveAt(i - 1);
						instrs.RemoveAt(i - 2);
						instrs.InsertRange(i - 2, moduleCtx.ModeHandler.EmitDecrypt(moduleCtx.InitMethod, moduleCtx, (Local)ldBlock.Operand, (Local)ldKey.Operand));
					}
					else
					{
						bool flag3 = method.DeclaringType.Name == "Lzma" && method.Name == "Decompress";
						if (flag3)
						{
							instr.Operand = decomp;
						}
					}
				}
			}
			moduleCtx.InitMethod.Body.Instructions.Clear();
			foreach (Instruction instr2 in instrs)
			{
				moduleCtx.InitMethod.Body.Instructions.Add(instr2);
			}
		}
	}
}
