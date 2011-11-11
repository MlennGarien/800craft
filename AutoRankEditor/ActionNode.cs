using System;
using System.Windows.Forms;
using fCraft;

namespace AutoRankEditor {
    sealed class ActionNode : GroupNode {
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public ActionType Action { get; set; }

        public ActionNode()
            : base( GroupNodeType.AND ) {
            FromRank = null;
            ToRank = null;
            UpdateLabel();
        }

        public override void UpdateLabel() {
            string fromRankStr = "?";
            string toRankStr = "?";
            if( FromRank != null ) fromRankStr = FromRank.Name;
            if( ToRank != null ) toRankStr = ToRank.Name;
            if( FromRank != null && ToRank != null ) {
                if( FromRank < ToRank ) {
                    Text = String.Format( "Promote ({0} {1} to {2})",
                                          Action, fromRankStr, toRankStr );
                } else {
                    Text = String.Format( "Demote ({0} {1} to {2})",
                                          Action, fromRankStr, toRankStr );
                }
            } else {
                Text = String.Format( "Action ({0} {1} to {2})",
                                      Action, fromRankStr, toRankStr );
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
