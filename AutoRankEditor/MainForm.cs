using System;
using System.Collections.Generic;
using System.Windows.Forms;
using fCraft;
using fCraft.AutoRank;
using System.Linq;
using System.Xml.Linq;

namespace AutoRankEditor {
    public sealed partial class MainForm : Form {

        string[] rankList;

        public MainForm() {

            InitializeComponent();

            cmAddCondition.ItemClicked += cmAddCondition_ItemClicked;
            foreach( ToolStripMenuItem item in cmAddCondition.Items ) {
                if( item.DropDownItems.Count > 0 ) {
                    item.DropDownItemClicked += cmAddCondition_ItemClicked;
                }
            }

            foreach( ActionType actionType in Enum.GetValues( typeof( ActionType ) ) ) {
                cActionType.Items.Add( actionType );
            }

            foreach( string fieldName in EnumExtensions.ConditionFieldNames.Values ) {
                cConditionField.Items.Add( fieldName );
            }

            foreach( GroupNodeType groupOp in Enum.GetValues( typeof( GroupNodeType ) ) ) {
                cGroupOp.Items.Add( groupOp );
                cActionConnective.Items.Add( groupOp );
            }

            Shown += Init;
        }

        void Init( object sender, EventArgs args ) {
            Server.InitLibrary( Environment.GetCommandLineArgs() );
            Config.Load( false, false );

            rankList = RankManager.Ranks.Select( r => r.Prefix + r.Name ).ToArray();
            cFromRank.Items.AddRange( rankList );
            cToRank.Items.AddRange( rankList );

            using( LogRecorder recorder = new LogRecorder() ) {
                AutoRankManager.Init();
                if( recorder.HasMessages ) {
                    MessageBox.Show( recorder.MessageString, "Loading autorank.xml..." );
                }
            }

            if( AutoRankManager.HasCriteria ) {
                foreach( Criterion crit in AutoRankManager.Criteria ) {
                    ActionNode newNode = new ActionNode {
                        Action = ActionType.Automatic,
                        FromRank = crit.FromRank,
                        ToRank = crit.ToRank
                    };

                    if( crit.Condition is ConditionAND ) {
                        newNode.Op = GroupNodeType.AND;
                    } else if( crit.Condition is ConditionOR ) {
                        newNode.Op = GroupNodeType.OR;
                    } else if( crit.Condition is ConditionNAND ) {
                        newNode.Op = GroupNodeType.NAND;
                    } else if( crit.Condition is ConditionNOR ) {
                        newNode.Op = GroupNodeType.NOR;
                    } else {
                        throw new FormatException();
                    }

                    foreach( Condition subCondition in crit.Condition.Conditions ) {
                        ImportCondition( newNode, subCondition );
                    }
                    treeData.Nodes.Add( newNode );
                    newNode.UpdateLabel();
                }
            } else {
                treeData.Nodes.Add( new ActionNode() );
            }
            treeData.ExpandAll();
            treeData.SelectedNode = treeData.Nodes[0];
        }


        static void ImportCondition( GroupNode parent, Condition condition ) {
            if( condition is ConditionIntRange ) {
                ConditionIntRange cond = (ConditionIntRange)condition;
                ConditionNode newNode = new ConditionNode {
                    Field = cond.Field,
                    Value = cond.Value,
                    Op = cond.Comparison
                };
                parent.Nodes.Add( newNode );
            } else if( condition is ConditionSet ) {
                ConditionSet set = (ConditionSet)condition;
                GroupNode newNode = new GroupNode();
                if( set is ConditionAND ) {
                    newNode.Op = GroupNodeType.AND;
                } else if( set is ConditionOR ) {
                    newNode.Op = GroupNodeType.OR;
                } else if( set is ConditionNAND ) {
                    newNode.Op = GroupNodeType.NAND;
                } else if( set is ConditionNOR ) {
                    newNode.Op = GroupNodeType.OR;
                } else {
                    return;
                }
                foreach( Condition subCondition in set.Conditions ) {
                    ImportCondition( newNode, subCondition );
                }
                parent.Nodes.Add(newNode);
            }
        }


        static ConditionSet ExportConditions( GroupNode node ) {
            ConditionSet set;
            switch( node.Op ) {
                case GroupNodeType.AND:
                    set = new ConditionAND();
                    break;
                case GroupNodeType.OR:
                    set = new ConditionOR();
                    break;
                case GroupNodeType.NAND:
                    set = new ConditionNAND();
                    break;
                case GroupNodeType.NOR:
                    set = new ConditionNOR();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            foreach( TreeNode subNode in node.Nodes ) {
                if( subNode is GroupNode ) {
                    set.Add( ExportConditions( (GroupNode)subNode ) );

                } else if( subNode is ConditionNode ) {
                    ConditionNode sn = (ConditionNode)subNode;
                    ConditionIntRange cond = new ConditionIntRange {
                        Comparison = sn.Op,
                        Field = sn.Field,
                        Value = sn.Value
                    };
                    set.Add( cond );

                } else {
                    throw new Exception();
                }
            }

            return set;
        }


        private void treeData_AfterSelect( object sender, TreeViewEventArgs e ) {
            if( e.Node is GroupNode ) {
                bAddGroup.Enabled = true;
                bAddCondition.Enabled = true;
                gEditCondition.Visible = false;
                if( e.Node is ActionNode ) {
                    ActionNode anode = e.Node as ActionNode;
                    gEditGroup.Visible = false;
                    gEditAction.Visible = true;
                    cActionType.SelectedIndex = (int)anode.Action;
                    if( anode.FromRank != null ) {
                        cFromRank.SelectedIndex = anode.FromRank.Index;
                    }else{
                        cFromRank.SelectedIndex=-1;
                    }
                    if( anode.ToRank != null ) {
                        cToRank.SelectedIndex = anode.ToRank.Index;
                    } else {
                        cToRank.SelectedIndex = -1;
                    }
                    cActionConnective.SelectedIndex = (int)(e.Node as GroupNode).Op;
                } else {
                    gEditGroup.Visible = true;
                    gEditAction.Visible = false;
                    cGroupOp.SelectedIndex = (int)(e.Node as GroupNode).Op;
                }
            } else if( e.Node is ConditionNode ) {
                bAddGroup.Enabled = false;
                bAddCondition.Enabled = false;
                gEditGroup.Visible = false;
                gEditAction.Visible = false;
                gEditCondition.Visible = true;
                cConditionField.SelectedItem = (e.Node as ConditionNode).Field.GetLongString();
                cConditionOp.SelectedIndex = (int)(e.Node as ConditionNode).Op;
                nConditionValue.Value = (e.Node as ConditionNode).Value;
            }
        }


        private void bAddGroup_Click( object sender, EventArgs e ) {
            cmAddGroup.Show( bAddGroup, bAddGroup.Width, 0 );
            cmAddGroup.Items[0].Select();
        }


        private void bAddCondition_Click( object sender, EventArgs e ) {
            foreach( ToolStripMenuItem item in cmAddCondition.Items ) {
                if( item.DropDownItems.Count > 0 ) {
                    bool anySubItemsAvailable = false;
                    foreach( ToolStripMenuItem subItem in item.DropDownItems ) {
                        if( CheckIfNodeExists( treeData.SelectedNode, item.Text + " " + subItem.Text ) ) {
                            subItem.Available = false;
                        } else {
                            subItem.Available = true;
                            anySubItemsAvailable = true;
                        }
                    }
                    item.Available = anySubItemsAvailable;
                } else {
                    item.Available = !CheckIfNodeExists( treeData.SelectedNode, item.Text );
                }
            }
            cmAddCondition.Show( bAddCondition, bAddCondition.Width, 0 );
            cmAddCondition.Items[0].Select();
        }


        static bool CheckIfNodeExists( TreeNode node, string text ) {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach( TreeNode subNode in node.Nodes ) {
                if( (subNode is ConditionNode) && (subNode as ConditionNode).Field.GetLongString() == text ) {
                    return true;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return false;
        }


        private void cmAddGroup_ItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            ToolStripMenuItem item = (ToolStripMenuItem)e.ClickedItem;
            if( item.DropDownItems.Count > 0 ) return;
            TreeNode parent = treeData.SelectedNode;
            if( parent is GroupNode ) {
                GroupNode newNode = new GroupNode( (GroupNodeType)Enum.Parse( typeof( GroupNodeType ), item.Text ) );
                parent.Nodes.Add( newNode );
                ((GroupNode)parent).UpdateLabel();
                newNode.EnsureVisible();
                treeData.SelectedNode = newNode;
            }
        }


        private void cmAddCondition_ItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            ToolStripMenuItem item = (ToolStripMenuItem)e.ClickedItem;
            if( item.DropDownItems.Count > 0 ) return;
            GroupNode node = treeData.SelectedNode as GroupNode;
            if( node != null ) {
                string text = item.Text;
                if( item.OwnerItem != null ) {
                    text = item.OwnerItem.Text + ' ' + text;
                }
                ConditionNode newNode = new ConditionNode( text );
                node.Nodes.Add( newNode );
                node.UpdateLabel();
                newNode.EnsureVisible();
                treeData.SelectedNode = newNode;
            }
        }


        private void bDelete_Click( object sender, EventArgs e ) {
            if( treeData.SelectedNode.Nodes.Count > 0 ) {
                DialogResult result = MessageBox.Show( "Delete this group and all of its conditions?", "",
                                                       MessageBoxButtons.OKCancel );
                if( result != DialogResult.OK ) return;
            }
            TreeNode parent = treeData.SelectedNode.Parent;
            treeData.SelectedNode.Remove();
            if( parent != null ) {
                treeData.SelectedNode = parent;
                ((GroupNode)parent).UpdateLabel();
            } else {
                gEditGroup.Visible = false;
                gEditAction.Visible = false;
                gEditCondition.Visible = false;
            }
        }


        private void cGroupOp_SelectedIndexChanged( object sender, EventArgs e ) {
            ((GroupNode)treeData.SelectedNode).Op = (GroupNodeType)cGroupOp.SelectedIndex;
            ((GroupNode)treeData.SelectedNode).UpdateLabel();
        }

        private void cConditionOp_SelectedIndexChanged( object sender, EventArgs e ) {
            ((ConditionNode)treeData.SelectedNode).Op = (ComparisonOp)cConditionOp.SelectedIndex;
            ((ConditionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void cConditionField_SelectedIndexChanged( object sender, EventArgs e ) {
            ((ConditionNode)treeData.SelectedNode).Field = EnumExtensions.ConditionFieldFromString( cConditionField.SelectedItem.ToString() );
            ((ConditionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void nConditionValue_ValueChanged( object sender, EventArgs e ) {
            ((ConditionNode)treeData.SelectedNode).Value = (int)nConditionValue.Value;
            ((ConditionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void bAction_Click( object sender, EventArgs e ) {
            ActionNode newNode = new ActionNode();
            treeData.Nodes.Add( newNode);
            treeData.SelectedNode = newNode;
        }

        private void cActionType_SelectedIndexChanged( object sender, EventArgs e ) {
            ((ActionNode)treeData.SelectedNode).Action = (ActionType)cActionType.SelectedIndex;
            ((ActionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void cFromRank_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cFromRank.SelectedIndex >= 0 ) {
                ((ActionNode)treeData.SelectedNode).FromRank = RankManager.Ranks[cFromRank.SelectedIndex];
            }
            ((ActionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void cToRank_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cFromRank.SelectedIndex >= 0 ) {
                ((ActionNode)treeData.SelectedNode).ToRank = RankManager.Ranks[cToRank.SelectedIndex];
            }
            ((ActionNode)treeData.SelectedNode).UpdateLabel();
        }

        private void bOK_Click( object sender, EventArgs e ) {
            XDocument doc = new XDocument();
            XElement root = new XElement( AutoRankManager.TagName );
            foreach( TreeNode node in treeData.Nodes ) {
                ActionNode anode = (ActionNode)node;
                if( anode.FromRank == null || anode.ToRank == null ) continue;
                Criterion crit = new Criterion {
                    FromRank = anode.FromRank,
                    ToRank = anode.ToRank,
                    Condition = ExportConditions( anode )
                };
                root.Add( crit.Serialize() );
            }
            doc.Add( root );
            doc.Save( Paths.AutoRankFileName );
            Application.Exit();
        }

        private void cActionConnective_SelectedIndexChanged( object sender, EventArgs e ) {
            ((ActionNode)treeData.SelectedNode).Op = (GroupNodeType)cActionConnective.SelectedIndex;
            ((ActionNode)treeData.SelectedNode).UpdateLabel();
        }
    }


    static class EnumExtensions {
        public static string GetSymbol( this ComparisonOp op ) {
            switch( op ) {
                case ComparisonOp.Eq:
                    return "=";
                case ComparisonOp.Gt:
                    return ">";
                case ComparisonOp.Gte:
                    return ">=";
                case ComparisonOp.Lt:
                    return "<";
                case ComparisonOp.Lte:
                    return "<=";
                case ComparisonOp.Neq:
                    return "!=";
                default:
                    throw new ArgumentOutOfRangeException( "op" );
            }
        }

        public static readonly Dictionary<ConditionField, string> ConditionFieldNames = new Dictionary<ConditionField, string> {
            {ConditionField.BlocksBuilt, "Blocks Built"},
            {ConditionField.BlocksChanged, "Blocks Built + Deleted"},
            {ConditionField.BlocksDeleted, "Blocks Deleted"},
            {ConditionField.BlocksDrawn, "Blocks Drawn"},
            {ConditionField.LastSeen, "Time Since Last Seen"},
            {ConditionField.MessagesWritten, "Messages Written"},
            {ConditionField.TimeSinceFirstLogin, "Time Since First Join"},
            {ConditionField.TimeSinceLastKick, "Time Since Most Recent Kick"},
            {ConditionField.TimeSinceLastLogin, "Time Since Most Recent Join"},
            {ConditionField.TimeSinceRankChange, "Time Since Most Recent Promotion/Demotion"},
            {ConditionField.TimesKicked, "Number of Times Kicked"},
            {ConditionField.TimesVisited, "Number of Visits"},
            {ConditionField.TotalTime, "Total Time Spent"},
        };

        public static string GetLongString( this ConditionField field ) {
            return ConditionFieldNames[field];
        }

        public static ConditionField ConditionFieldFromString( string text ) {
            foreach( ConditionField field in ConditionFieldNames.Keys ) {
                if( ConditionFieldNames[field] == text ) return field;
            }
            throw new ArgumentOutOfRangeException( "text" );
        }

        public static string GetShortString( this GroupNodeType op ) {
            switch( op ) {
                case GroupNodeType.AND:
                    return "and";
                case GroupNodeType.NAND:
                    return "and not";
                case GroupNodeType.NOR:
                    return "or not";
                case GroupNodeType.OR:
                    return "or";
                default:
                    throw new ArgumentOutOfRangeException( "op" );
            }
        }
    }

    enum GroupNodeType {
        AND,
        OR,
        NAND,
        NOR
    }
}