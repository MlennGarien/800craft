using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class KeywordPicker : Form {
        public string Result;

        readonly ToolTip tips;
        static readonly KeywordInfo[] Keywords = new[]{
            new KeywordInfo("{SERVER_NAME}", "Server name", "Name of your server, as specified in config." ),
            new KeywordInfo("{RANK}", "Player's rank", "Player's rank, including prefix and colors (if applicable)." ),
            new KeywordInfo("{PLAYER_NAME}", "Player's name", "Name of the player, including prefix and colors (if applicable)." ),
            new KeywordInfo("{TIME}", "Time", "Current time (server clock)."),
            new KeywordInfo("{WORLD}", "Main world name","Name of the main/starting world, including prefix and colors (if applicable)." ),
            new KeywordInfo("{PLAYERS}", "Number of players online", "Note that hidden players will not be included in this number." ),
            new KeywordInfo("{WORLDS}", "Number of worlds", "Number of worlds accessible by the player. Does not count hidden worlds." ),
            new KeywordInfo("{MOTD}", "MOTD", "Message-of-the-day (server subtitle), as specified in config." ),
            new KeywordInfo("{VERSION}", "fCraft version", "Version of fCraft that this server is running." )
        };

        const int ButtonWidth = 150,
                  ButtonHeight = 28;

        public KeywordPicker() {
            InitializeComponent();
            tips = new ToolTip();
            foreach( KeywordInfo keyword in Keywords ) {
                Button newButton = new Button {
                    Text = keyword.LongName,
                    Tag = keyword.Keyword,
                    Width = ButtonWidth,
                    Height = ButtonHeight
                };
                pFlow.Controls.Add( newButton );
                newButton.Click += delegate {
                    Result = (string)newButton.Tag;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                tips.SetToolTip( newButton, keyword.Description );
            }
        }


        struct KeywordInfo {
            public KeywordInfo( string keyword, string name, string description ) {
                Keyword = keyword;
                LongName = name;
                Description = description;
            }
            public readonly string Keyword, LongName, Description;
        }
    }
}