using System.Windows.Forms;
using fCraft.AutoRank;

namespace AutoRankEditor {
    sealed class ConditionNode : TreeNode {
        public ConditionField Field { get; set; }
        public ComparisonOp Op { get; set; }
        public int Value { get; set; }

        public ConditionNode() { }

        public ConditionNode( string conditionDesc ) {
            Field = EnumExtensions.ConditionFieldFromString( conditionDesc );
        }

        public void UpdateLabel() {
            if( Parent.FirstNode == this ) {
                Text = Field.GetLongString() + ' ' + Op.GetSymbol() + ' ' + Value;
            } else {
                Text = ( (GroupNode)Parent ).Op.GetShortString() + ' ' + Field.GetLongString() + ' ' + Op.GetSymbol() + ' ' + Value;
            }
        }
    }
}