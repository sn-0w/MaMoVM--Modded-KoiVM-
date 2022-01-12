using Confuser.Core;
using MaMoVM.Confuser.Internal;

namespace MaMoVM.Confuser
{
    [BeforeProtection("Ki.ControlFlow", "Ki.AntiTamper")]
    public class MaMoVMProtection : Protection
    {
        public override string Id => "Virtualization";

        public override string FullId => "Ki.Virtualization";

        public override string Name => Id;

        public override string Description => "It virtualizes your code using MVM and VMP technology.";

        public override ProtectionPreset Preset => ProtectionPreset.Maximum;

        protected override void Initialize(ConfuserContext context) { }

        protected override void PopulatePipeline(ProtectionPipeline pipeline)
        {
            pipeline.InsertPreStage(PipelineStage.EndModule, new InitializePhase(this));
            pipeline.InsertPreStage(PipelineStage.Pack, new SavePhase(this));
        }
    }
}