using System.Windows.Forms;

namespace AutoRankEditor {
    class GroupNode : TreeNode {
        public GroupNodeType Op { get; set; }

        public GroupNode() { }

        public GroupNode( GroupNodeType op ) {
            Op = op;
            UpdateLabel();
        }

        public virtual void UpdateLabel() {
            if( Parent != null ) {
                if( Parent.FirstNode == this ) {
                    Text = "Group (" + Op + ", " + Nodes.Count + ")";
                } else {
                    Text = ((GroupNode)Parent).Op.GetShortString() + " Group (" + Op + ", " + Nodes.Count + ")";
                }
            } else {
                Text = "Criterion";
            }
            foreach( TreeNode node in Nodes ) {
                if( node is GroupNode ) {
                    (node as GroupNode).UpdateLabel();
                } else if( node is ConditionNode ) {
                    (node as ConditionNode).UpdateLabel();
                }
            }
        }
    }
}
