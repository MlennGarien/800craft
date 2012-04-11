// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    sealed class QuickPasteDrawOperation : PasteDrawOperation {
        public override string Name {
            get {
                return Not ? "PasteNot" : "Paste";
            }
        }

        public QuickPasteDrawOperation( Player player, bool not )
            : base( player, not ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            return base.Prepare( new[] { marks[0], marks[0] } );
        }
    }
}
