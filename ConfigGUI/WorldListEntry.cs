using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using fCraft.MapConversion;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.ConfigGUI
{
    /// <summary>
    /// A wrapper for per-World metadata, designed to be usable with SortableBindingList.
    /// All these properties map directly to the UI controls.
    /// </summary>
    sealed class WorldListEntry : ICloneable
    {
        public const string WorldInfoSignature = "(ConfigGUI)";
        public const string DefaultRankOption = "(everyone)";
        const string MapFileExtension = ".fcm";

        internal bool LoadingFailed { get; private set; }


        public WorldListEntry([NotNull] string newName)
        {
            if (newName == null) throw new ArgumentNullException("newName");
            name = newName;
        }


        public WorldListEntry([NotNull] WorldListEntry original)
        {
            if (original == null) throw new ArgumentNullException("original");
            name = original.Name;
            Hidden = original.Hidden;
            Backup = original.Backup;
            BlockDBEnabled = original.BlockDBEnabled;
            blockDBIsPreloaded = original.blockDBIsPreloaded;
            blockDBLimit = original.blockDBLimit;
            blockDBTimeLimit = original.blockDBTimeLimit;
            accessSecurity = new SecurityController(original.accessSecurity);
            buildSecurity = new SecurityController(original.buildSecurity);
            LoadedBy = original.LoadedBy;
            LoadedOn = original.LoadedOn;
            MapChangedBy = original.MapChangedBy;
            MapChangedOn = original.MapChangedOn;
            environmentEl = original.environmentEl;
        }


        public WorldListEntry([NotNull] XElement el)
        {
            if (el == null) throw new ArgumentNullException("el");
            XAttribute temp;

            if ((temp = el.Attribute("name")) == null)
            {
                throw new FormatException("WorldListEntity: Cannot parse XML: Unnamed worlds are not allowed.");
            }
            if (!World.IsValidName(temp.Value))
            {
                throw new FormatException("WorldListEntity: Cannot parse XML: Invalid world name skipped \"" + temp.Value + "\".");
            }
            name = temp.Value;

            if ((temp = el.Attribute("hidden")) != null && !String.IsNullOrEmpty(temp.Value))
            {
                bool hidden;
                if (Boolean.TryParse(temp.Value, out hidden))
                {
                    Hidden = hidden;
                }
                else
                {
                    throw new FormatException("WorldListEntity: Cannot parse XML: Invalid value for \"hidden\" attribute.");
                }
            }
            else
            {
                Hidden = false;
            }

            if ((temp = el.Attribute("backup")) != null)
            {
                TimeSpan realBackupTimer;
                if (temp.Value.ToTimeSpan(out realBackupTimer))
                {
                    Backup = BackupNameFromValue(realBackupTimer);
                }
                else
                {
                    Logger.Log(LogType.Error,
                                "WorldListEntity: Cannot parse backup settings for world \"{0}\". Assuming default.", name);
                    Backup = BackupEnumNames[0];
                }
            }
            else
            {
                Backup = BackupEnumNames[0];
            }

            XElement tempEl;
            if ((tempEl = el.Element(WorldManager.AccessSecurityXmlTagName)) != null ||
                (tempEl = el.Element("accessSecurity")) != null)
            {
                accessSecurity = new SecurityController(tempEl, false);
            }
            if ((tempEl = el.Element(WorldManager.BuildSecurityXmlTagName)) != null ||
                (tempEl = el.Element("buildSecurity")) != null)
            {
                buildSecurity = new SecurityController(tempEl, false);
            }

            XElement blockEl = el.Element(BlockDB.XmlRootName);
            if (blockEl == null)
            {
                BlockDBEnabled = YesNoAuto.Auto;
            }
            else
            {
                if ((temp = blockEl.Attribute("enabled")) != null)
                {
                    YesNoAuto enabledStateTemp;
                    if (EnumUtil.TryParse(temp.Value, out enabledStateTemp, true))
                    {
                        BlockDBEnabled = enabledStateTemp;
                    }
                    else
                    {
                        Logger.Log(LogType.Warning,
                                    "WorldListEntity: Could not parse BlockDB \"enabled\" attribute of world \"{0}\", assuming \"Auto\".",
                                    name);
                        BlockDBEnabled = YesNoAuto.Auto;
                    }
                }

                if ((temp = blockEl.Attribute("preload")) != null)
                {
                    bool isPreloaded;
                    if (Boolean.TryParse(temp.Value, out isPreloaded))
                    {
                        blockDBIsPreloaded = isPreloaded;
                    }
                    else
                    {
                        Logger.Log(LogType.Warning,
                                    "WorldListEntity: Could not parse BlockDB \"preload\" attribute of world \"{0}\", assuming NOT preloaded.",
                                    name);
                    }
                }
                if ((temp = blockEl.Attribute("limit")) != null)
                {
                    int limit;
                    if (Int32.TryParse(temp.Value, out limit))
                    {
                        blockDBLimit = limit;
                    }
                    else
                    {
                        Logger.Log(LogType.Warning,
                                    "WorldListEntity: Could not parse BlockDB \"limit\" attribute of world \"{0}\", assuming NO limit.",
                                    name);
                    }
                }
                if ((temp = blockEl.Attribute("timeLimit")) != null)
                {
                    int timeLimitSeconds;
                    if (Int32.TryParse(temp.Value, out timeLimitSeconds))
                    {
                        blockDBTimeLimit = TimeSpan.FromSeconds(timeLimitSeconds);
                    }
                    else
                    {
                        Logger.Log(LogType.Warning,
                                    "WorldListEntity: Could not parse BlockDB \"timeLimit\" attribute of world \"{0}\", assuming NO time limit.",
                                    name);
                    }
                }
            }

            if ((tempEl = el.Element("LoadedBy")) != null)
            {
                LoadedBy = tempEl.Value;
            }
            if ((tempEl = el.Element("MapChangedBy")) != null)
            {
                MapChangedBy = tempEl.Value;
            }

            if ((tempEl = el.Element("LoadedOn")) != null)
            {
                if (!tempEl.Value.ToDateTime(ref LoadedOn))
                {
                    LoadedOn = DateTime.MinValue;
                }
            }
            if ((tempEl = el.Element("MapChangedOn")) != null)
            {
                if (!tempEl.Value.ToDateTime(ref MapChangedOn))
                {
                    MapChangedOn = DateTime.MinValue;
                }
            }
            environmentEl = el.Element(WorldManager.EnvironmentXmlTagName);
        }

        public string LoadedBy, MapChangedBy;
        public DateTime LoadedOn, MapChangedOn;
        readonly XElement environmentEl;


        #region List Properties

        string name;
        [SortableProperty(typeof(WorldListEntry), "Compare")]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name == value) return;
                if (!World.IsValidName(value))
                {
                    throw new FormatException("Invalid world name");

                }
                else if (!value.Equals(name, StringComparison.OrdinalIgnoreCase) && MainForm.IsWorldNameTaken(value))
                {
                    throw new FormatException("Duplicate world names are not allowed.");

                }
                else
                {
                    string oldName = name;
                    string oldFileName = Path.Combine(Paths.MapPath, oldName + ".fcm");
                    string newFileName = Path.Combine(Paths.MapPath, value + ".fcm");
                    if (File.Exists(oldFileName))
                    {
                        bool isSameFile;
                        if (MonoCompat.IsCaseSensitive)
                        {
                            isSameFile = newFileName.Equals(oldFileName, StringComparison.Ordinal);
                        }
                        else
                        {
                            isSameFile = newFileName.Equals(oldFileName, StringComparison.OrdinalIgnoreCase);
                        }
                        if (File.Exists(newFileName) && !isSameFile)
                        {
                            string messageText = String.Format("Map file \"{0}\" already exists. Overwrite?", value + ".fcm");
                            var result = MessageBox.Show(messageText, "", MessageBoxButtons.OKCancel);
                            if (result == DialogResult.Cancel) return;
                        }
                        Paths.ForceRename(oldFileName, newFileName);
                    }
                    name = value;
                    if (oldName != null)
                    {
                        MainForm.HandleWorldRename(oldName, name);
                    }
                }
            }
        }


        [SortableProperty(typeof(WorldListEntry), "Compare")]
        public string Description
        {
            get
            {
                Map mapHeader = MapHeader;
                if (LoadingFailed)
                {
                    return "(cannot load file)";
                }
                else
                {
                    return String.Format("{0} × {1} × {2}",
                                          mapHeader.Width,
                                          mapHeader.Length,
                                          mapHeader.Height);
                }
            }
        }


        public bool Hidden { get; set; }


        readonly SecurityController accessSecurity = new SecurityController();
        string accessRankString;
        public string AccessPermission
        {
            get
            {
                if (accessSecurity.HasRankRestriction)
                {
                    return MainForm.ToComboBoxOption(accessSecurity.MinRank);
                }
                else
                {
                    return DefaultRankOption;
                }
            }
            set
            {
                foreach (Rank rank in RankManager.Ranks)
                {
                    if (MainForm.ToComboBoxOption(rank) == value)
                    {
                        accessSecurity.MinRank = rank;
                        accessRankString = rank.FullName;
                        return;
                    }
                }
                accessSecurity.ResetMinRank();
                accessRankString = "";
            }
        }


        readonly SecurityController buildSecurity = new SecurityController();
        string buildRankString;
        public string BuildPermission
        {
            get
            {
                if (buildSecurity.HasRankRestriction)
                {
                    return MainForm.ToComboBoxOption(buildSecurity.MinRank);
                }
                else
                {
                    return DefaultRankOption;
                }
            }
            set
            {
                foreach (Rank rank in RankManager.Ranks)
                {
                    if (MainForm.ToComboBoxOption(rank) == value)
                    {
                        buildSecurity.MinRank = rank;
                        buildRankString = rank.FullName;
                        return;
                    }
                }
                buildSecurity.ResetMinRank();
                buildRankString = null;
            }
        }


        public string Backup { get; set; }

        #endregion


        internal XElement Serialize()
        {
            XElement element = new XElement("World");
            element.Add(new XAttribute("name", Name));
            element.Add(new XAttribute("hidden", Hidden));
            if (Backup != BackupEnumNames[0])
            {
                element.Add(new XAttribute("backup", BackupValueFromName(Backup).ToTickString()));
            }
            element.Add(accessSecurity.Serialize(WorldManager.AccessSecurityXmlTagName));
            element.Add(buildSecurity.Serialize(WorldManager.BuildSecurityXmlTagName));
            XElement blockDB = new XElement(BlockDB.XmlRootName);
            blockDB.Add(new XAttribute("enabled", BlockDBEnabled));
            blockDB.Add(new XAttribute("preload", blockDBIsPreloaded));
            blockDB.Add(new XAttribute("limit", blockDBLimit));
            blockDB.Add(new XAttribute("timeLimit", (int)blockDBTimeLimit.TotalSeconds));
            element.Add(blockDB);

            if (environmentEl != null) element.Add(environmentEl);

            if (!String.IsNullOrEmpty(LoadedBy)) element.Add(new XElement("LoadedBy", LoadedBy));
            if (!String.IsNullOrEmpty(MapChangedBy)) element.Add(new XElement("MapChangedBy", MapChangedBy));
            if (LoadedOn != DateTime.MinValue) element.Add(new XElement("LoadedOn", LoadedOn));
            if (MapChangedOn != DateTime.MinValue) element.Add(new XElement("MapChangedOn", MapChangedOn));
            return element;
        }


        public void ReparseRanks()
        {
            Rank accessMinRank = Rank.Parse(accessRankString);
            if (accessMinRank != null)
            {
                accessSecurity.MinRank = accessMinRank;
            }
            else
            {
                accessSecurity.ResetMinRank();
            }

            Rank buildMinRank = Rank.Parse(buildRankString);
            if (buildMinRank != null)
            {
                buildSecurity.MinRank = buildMinRank;
            }
            else
            {
                buildSecurity.ResetMinRank();
            }
        }


        Map cachedMapHeader;
        internal Map MapHeader
        {
            get
            {
                if (cachedMapHeader == null && !LoadingFailed)
                {
                    string fullFileName = Path.Combine(Paths.MapPath, name + ".fcm");
                    LoadingFailed = !MapUtility.TryLoadHeader(fullFileName, out cachedMapHeader);
                }
                return cachedMapHeader;
            }
        }


        internal string FileName
        {
            get { return Name + MapFileExtension; }
        }


        internal string FullFileName
        {
            get { return Path.Combine(Paths.MapPath, Name + MapFileExtension); }
        }


        #region Backup

        public static string BackupNameFromValue(TimeSpan value)
        {
            TimeSpan closestMatch = BackupEnumValues.OrderBy(t => Math.Abs(value.Subtract(t).Ticks)).First();
            return BackupEnumNames[Array.IndexOf(BackupEnumValues, closestMatch)];
        }

        public static TimeSpan BackupValueFromName(string name)
        {
            return BackupEnumValues[Array.IndexOf(BackupEnumNames, name)];
        }

        public static readonly string[] BackupEnumNames = new[] {
            "(default)",
            "Never",
            "5 Minutes",
            "10 Minutes",
            "15 Minutes",
            "20 Minutes",
            "30 Minutes",
            "45 Minutes",
            "1 Hour",
            "2 Hours",
            "3 Hours",
            "4 Hours",
            "6 Hours",
            "8 Hours",
            "12 Hours",
            "24 Hours",
            "48 Hours"
        };

        static readonly TimeSpan[] BackupEnumValues = new[] {
            TimeSpan.FromSeconds(-1), // default
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(45),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(3),
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(12),
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(48)
        };

        #endregion


        public YesNoAuto BlockDBEnabled { get; set; }
        readonly bool blockDBIsPreloaded;
        readonly int blockDBLimit;
        TimeSpan blockDBTimeLimit;


        public object Clone()
        {
            return new WorldListEntry(this);
        }


        // Comparison method used to customize sorting
        [UsedImplicitly]
        public static object Compare(string propertyName, object a, object b)
        {
            WorldListEntry entry1 = (WorldListEntry)a;
            WorldListEntry entry2 = (WorldListEntry)b;
            switch (propertyName)
            {
                case "Description":
                    if (entry1.MapHeader == null && entry2.MapHeader == null) return 0;
                    if (entry1.MapHeader == null) return -1;
                    if (entry2.MapHeader == null) return 1;
                    int volumeDifference = entry1.MapHeader.Volume - entry2.MapHeader.Volume;
                    return Math.Min(1, Math.Max(-1, volumeDifference));

                case "Name":
                    return StringComparer.OrdinalIgnoreCase.Compare(entry1.name, entry2.name);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}