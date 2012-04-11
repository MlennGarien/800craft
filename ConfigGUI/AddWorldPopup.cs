// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using fCraft.GUI;
using fCraft.MapConversion;


namespace fCraft.ConfigGUI {
    sealed partial class AddWorldPopup : Form {
        readonly BackgroundWorker bwLoader = new BackgroundWorker(),
                                  bwGenerator = new BackgroundWorker(),
                                  bwRenderer = new BackgroundWorker();

        const string MapLoadFilter = "Minecraft Maps|*.fcm;*.lvl;*.dat;*.mclevel;*.gz;*.map;*.meta;*.mine;*.save";

        readonly object redrawLock = new object();

        Map map;
        Map Map {
            get {
                return map;
            }
            set {
                try {
                    bOK.Invoke( (MethodInvoker)delegate {
                        try {
                            bOK.Enabled = (value != null);
                            lCreateMap.Visible = !bOK.Enabled;
                        } catch( ObjectDisposedException ) {
                        } catch( InvalidOperationException ) { }
                    } );
                } catch( ObjectDisposedException ) {
                } catch( InvalidOperationException ) { }
                map = value;
            }
        }

        Stopwatch stopwatch;
        int previewRotation;
        Bitmap previewImage;
        bool floodBarrier;
        string originalWorldName;
        readonly List<WorldListEntry> copyOptionsList = new List<WorldListEntry>();
        Tabs tab;


        internal WorldListEntry World { get; private set; }


        public AddWorldPopup( WorldListEntry world ) {
            InitializeComponent();
            fileBrowser.Filter = MapLoadFilter;

            cBackup.Items.AddRange( WorldListEntry.BackupEnumNames );
            cTemplates.Items.AddRange( Enum.GetNames( typeof( MapGenTemplate ) ) );
            cTheme.Items.AddRange( Enum.GetNames( typeof( MapGenTheme ) ) );

            bwLoader.DoWork += AsyncLoad;
            bwLoader.RunWorkerCompleted += AsyncLoadCompleted;

            bwGenerator.DoWork += AsyncGen;
            bwGenerator.WorkerReportsProgress = true;
            bwGenerator.ProgressChanged += AsyncGenProgress;
            bwGenerator.RunWorkerCompleted += AsyncGenCompleted;

            bwRenderer.WorkerReportsProgress = true;
            bwRenderer.WorkerSupportsCancellation = true;
            bwRenderer.DoWork += AsyncDraw;
            bwRenderer.ProgressChanged += AsyncDrawProgress;
            bwRenderer.RunWorkerCompleted += AsyncDrawCompleted;

            nMapWidth.Validating += MapDimensionValidating;
            nMapLength.Validating += MapDimensionValidating;
            nMapHeight.Validating += MapDimensionValidating;

            cAccess.Items.Add( "(everyone)" );
            cBuild.Items.Add( "(everyone)" );
            foreach( Rank rank in RankManager.Ranks ) {
                cAccess.Items.Add( MainForm.ToComboBoxOption( rank ) );
                cBuild.Items.Add( MainForm.ToComboBoxOption( rank ) );
            }

            tStatus1.Text = "";
            tStatus2.Text = "";

            World = world;

            savePreviewDialog.Filter = "PNG Image|*.png|TIFF Image|*.tif;*.tiff|Bitmap Image|*.bmp|JPEG Image|*.jpg;*.jpeg";
            savePreviewDialog.Title = "Saving preview image...";

            browseTemplateDialog.Filter = "MapGenerator Template|*.ftpl";
            browseTemplateDialog.Title = "Opening a MapGenerator template...";

            saveTemplateDialog.Filter = browseTemplateDialog.Filter;
            saveTemplateDialog.Title = "Saving a MapGenerator template...";

            Shown += LoadMap;
        }


        void LoadMap( object sender, EventArgs args ) {
            // Fill in the "Copy existing world" combobox
            foreach( WorldListEntry otherWorld in MainForm.Worlds ) {
                if( otherWorld != World ) {
                    cWorld.Items.Add( otherWorld.Name + " (" + otherWorld.Description + ")" );
                    copyOptionsList.Add( otherWorld );
                }
            }

            if( World == null ) {
                Text = "Adding a New World";

                // keep trying "NewWorld#" until we find an unused number
                int worldNameCounter = 1;
                while( MainForm.IsWorldNameTaken( "NewWorld" + worldNameCounter ) ) {
                    worldNameCounter++;
                }

                World = new WorldListEntry( "NewWorld" + worldNameCounter );

                tName.Text = World.Name;
                cAccess.SelectedIndex = 0;
                cBuild.SelectedIndex = 0;
                cBackup.SelectedIndex = 5;
                xBlockDB.CheckState = CheckState.Indeterminate;
                Map = null;

            } else {
                // Editing a world
                World = new WorldListEntry( World );
                Text = "Editing World \"" + World.Name + "\"";
                originalWorldName = World.Name;
                tName.Text = World.Name;
                cAccess.SelectedItem = World.AccessPermission;
                cBuild.SelectedItem = World.BuildPermission;
                cBackup.SelectedItem = World.Backup;
                xHidden.Checked = World.Hidden;

                switch( World.BlockDBEnabled ) {
                    case YesNoAuto.Auto:
                        xBlockDB.CheckState = CheckState.Indeterminate;
                        break;
                    case YesNoAuto.Yes:
                        xBlockDB.CheckState = CheckState.Checked;
                        break;
                    case YesNoAuto.No:
                        xBlockDB.CheckState = CheckState.Unchecked;
                        break;
                }
            }

            // Disable "copy" tab if there are no other worlds
            if( cWorld.Items.Count > 0 ) {
                cWorld.SelectedIndex = 0;
            } else {
                tabs.TabPages.Remove( tabCopy );
            }

            // Disable "existing map" tab if mapfile does not exist
            fileToLoad = World.FullFileName;
            if( File.Exists( fileToLoad ) ) {
                ShowMapDetails( tExistingMapInfo, fileToLoad );
                StartLoadingMap();
            } else {
                tabs.TabPages.Remove( tabExisting );
                tabs.SelectTab( tabLoad );
            }

            // Set Generator comboboxes to defaults
            cTemplates.SelectedIndex = (int)MapGenTemplate.River;

            savePreviewDialog.FileName = World.Name;
        }


        #region Loading/Saving Map

        void StartLoadingMap() {
            Map = null;
            tStatus1.Text = "Loading " + new FileInfo( fileToLoad ).Name;
            tStatus2.Text = "";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            bwLoader.RunWorkerAsync();
        }

        private void bBrowseFile_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            if( fileBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( fileBrowser.FileName ) ) {
                tFolder.Text = "";
                tFile.Text = fileBrowser.FileName;
                tFile.SelectAll();

                fileToLoad = fileBrowser.FileName;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
                World.MapChangedBy = WorldListEntry.WorldInfoSignature;
                World.MapChangedOn = DateTime.UtcNow;
            }
        }

        private void bBrowseFolder_Click( object sender, EventArgs e ) {
            if( folderBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( folderBrowser.SelectedPath ) ) {
                tFile.Text = "";
                tFolder.Text = folderBrowser.SelectedPath;
                tFolder.SelectAll();

                fileToLoad = folderBrowser.SelectedPath;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
                World.MapChangedBy = WorldListEntry.WorldInfoSignature;
                World.MapChangedOn = DateTime.UtcNow;
            }
        }

        string fileToLoad;
        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            try {
                Map = MapUtility.Load( fileToLoad );
                Map.CalculateShadows();
            } catch( Exception ex ) {
                MessageBox.Show( String.Format( "Could not load specified map: {0}: {1}",
                                                ex.GetType().Name, ex.Message ) );
            }
        }

        void AsyncLoadCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( Map == null ) {
                tStatus1.Text = "Load failed!";
            } else {
                tStatus1.Text = "Load successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            if( tab == Tabs.CopyWorld ) {
                bShow.Enabled = true;
            }
        }

        #endregion Loading


        #region Map Preview

        IsoCat renderer;

        void Redraw( bool drawAgain ) {
            lock( redrawLock ) {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                if( bwRenderer.IsBusy ) {
                    bwRenderer.CancelAsync();
                    while( bwRenderer.IsBusy ) {
                        Thread.Sleep( 1 );
                        Application.DoEvents();
                    }
                }
                if( drawAgain ) bwRenderer.RunWorkerAsync();
            }
        }

        void AsyncDraw( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            renderer = new IsoCat( Map, IsoCatMode.Normal, previewRotation );
            Rectangle cropRectangle;
            if( bwRenderer.CancellationPending ) return;
            Bitmap rawImage = renderer.Draw( out cropRectangle, bwRenderer );
            if( bwRenderer.CancellationPending ) return;
            if( rawImage != null ) {
                previewImage = rawImage.Clone( cropRectangle, rawImage.PixelFormat );
            }
            renderer = null;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }

        void AsyncDrawProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
        }

        void AsyncDrawCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            tStatus2.Text = String.Format( "drawn ({0:0.000}s)", stopwatch.Elapsed.TotalSeconds );
            if( previewImage != null && previewImage != preview.Image ) {
                Image oldImage = preview.Image;
                if( oldImage != null ) oldImage.Dispose();
                preview.Image = previewImage;
                bSavePreview.Enabled = true;
            }
            progressBar.Visible = false;
        }

        private void bPreviewPrev_Click( object sender, EventArgs e ) {
            if( Map == null ) return;
            if( previewRotation == 0 ) previewRotation = 3;
            else previewRotation--;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }

        private void bPreviewNext_Click( object sender, EventArgs e ) {
            if( Map == null ) return;
            if( previewRotation == 3 ) previewRotation = 0;
            else previewRotation++;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }

        #endregion


        #region Map Generation

        MapGeneratorArgs generatorArgs = new MapGeneratorArgs();

        private void bGenerate_Click( object sender, EventArgs e ) {
            Map = null;
            bGenerate.Enabled = false;
            bFlatgrassGenerate.Enabled = false;

            if( tab == Tabs.Generator ) {
                if( !xSeed.Checked ) {
                    nSeed.Value = GetRandomSeed();
                }

                SaveGeneratorArgs();
            }

            tStatus1.Text = "Generating...";
            tStatus2.Text = "";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;

            Refresh();
            bwGenerator.RunWorkerAsync();
            World.MapChangedBy = WorldListEntry.WorldInfoSignature;
            World.MapChangedOn = DateTime.UtcNow;
        }

        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Map generatedMap;
            if( tab == Tabs.Generator ) {
                MapGenerator gen = new MapGenerator( generatorArgs );
                gen.ProgressChanged +=
                    ( progressSender, progressArgs ) =>
                    bwGenerator.ReportProgress( progressArgs.ProgressPercentage, progressArgs.UserState );
                generatedMap = gen.Generate();
            } else {
                generatedMap = MapGenerator.GenerateFlatgrass( Convert.ToInt32( nFlatgrassDimX.Value ),
                                                               Convert.ToInt32( nFlatgrassDimY.Value ),
                                                               Convert.ToInt32( nFlatgrassDimZ.Value ) );
            }

            if( floodBarrier ) generatedMap.MakeFloodBarrier();
            generatedMap.CalculateShadows();
            Map = generatedMap;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncGenProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
            tStatus1.Text = (string)e.UserState;
        }

        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( Map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                tStatus1.Text = "Generation successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            bGenerate.Enabled = true;
            bFlatgrassGenerate.Enabled = true;
        }


        readonly Random rand = new Random();
        int GetRandomSeed() {
            return rand.Next() - rand.Next();
        }

        #endregion


        #region Input Handlers

        void xFloodBarrier_CheckedChanged( object sender, EventArgs e ) {
            floodBarrier = xFloodBarrier.Checked;
        }


        static void MapDimensionValidating( object sender, CancelEventArgs e ) {
            ((NumericUpDown)sender).Value = Convert.ToInt32( ((NumericUpDown)sender).Value / 16 ) * 16;
        }


        void tName_Validating( object sender, CancelEventArgs e ) {
            if( fCraft.World.IsValidName( tName.Text ) &&
                (!MainForm.IsWorldNameTaken( tName.Text ) ||
                (originalWorldName != null && tName.Text.ToLower() == originalWorldName.ToLower())) ) {
                tName.ForeColor = SystemColors.ControlText;
            } else {
                tName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            }
        }


        void tName_Validated( object sender, EventArgs e ) {
            World.Name = tName.Text;
        }


        void cAccess_SelectedIndexChanged( object sender, EventArgs e ) {
            World.AccessPermission = cAccess.SelectedItem.ToString();
        }


        void cBuild_SelectedIndexChanged( object sender, EventArgs e ) {
            World.BuildPermission = cBuild.SelectedItem.ToString();
        }


        void cBackup_SelectedIndexChanged( object sender, EventArgs e ) {
            World.Backup = cBackup.SelectedItem.ToString();
        }


        void xHidden_CheckedChanged( object sender, EventArgs e ) {
            World.Hidden = xHidden.Checked;
        }


        void bShow_Click( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 && File.Exists( copyOptionsList[cWorld.SelectedIndex].FullFileName ) ) {
                bShow.Enabled = false;
                fileToLoad = copyOptionsList[cWorld.SelectedIndex].FullFileName;
                ShowMapDetails( tCopyInfo, fileToLoad );
                StartLoadingMap();
            }
        }


        void cWorld_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 ) {
                string fileName = copyOptionsList[cWorld.SelectedIndex].FullFileName;
                bShow.Enabled = File.Exists( fileName );
                ShowMapDetails( tCopyInfo, fileName );
            }
        }


        void xAdvanced_CheckedChanged( object sender, EventArgs e ) {
            gTerrainFeatures.Visible = xAdvanced.Checked;
            gHeightmapCreation.Visible = xAdvanced.Checked;
            gTrees.Visible = xAdvanced.Checked && xAddTrees.Checked;
            gCaves.Visible = xCaves.Checked && xAdvanced.Checked;
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }


        void MapDimensionChanged( object sender, EventArgs e ) {
            sFeatureScale.Maximum = (int)Math.Log( (double)Math.Max( nMapWidth.Value, nMapLength.Value ), 2 );
            int value = sDetailScale.Maximum - sDetailScale.Value;
            sDetailScale.Maximum = sFeatureScale.Maximum;
            sDetailScale.Value = sDetailScale.Maximum - value;

            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
        }


        void sFeatureSize_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
            if( sDetailScale.Value < sFeatureScale.Value ) {
                sDetailScale.Value = sFeatureScale.Value;
            }
        }


        void sDetailSize_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            if( sFeatureScale.Value > sDetailScale.Value ) {
                sFeatureScale.Value = sDetailScale.Value;
            }
        }


        void xMatchWaterCoverage_CheckedChanged( object sender, EventArgs e ) {
            sWaterCoverage.Enabled = xMatchWaterCoverage.Checked;
        }


        void sWaterCoverage_ValueChanged( object sender, EventArgs e ) {
            lMatchWaterCoverageDisplay.Text = sWaterCoverage.Value + "%";
        }


        void sBias_ValueChanged( object sender, EventArgs e ) {
            lBiasDisplay.Text = sBias.Value + "%";
            bool useBias = (sBias.Value != 0);

            nRaisedCorners.Enabled = useBias;
            nLoweredCorners.Enabled = useBias;
            cMidpoint.Enabled = useBias;
            xDelayBias.Enabled = useBias;
        }


        void sRoughness_ValueChanged( object sender, EventArgs e ) {
            lRoughnessDisplay.Text = sRoughness.Value + "%";
        }


        void xSeed_CheckedChanged( object sender, EventArgs e ) {
            nSeed.Enabled = xSeed.Checked;
        }


        void nRaisedCorners_ValueChanged( object sender, EventArgs e ) {
            nLoweredCorners.Value = Math.Min( 4 - nRaisedCorners.Value, nLoweredCorners.Value );
        }


        void nLoweredCorners_ValueChanged( object sender, EventArgs e ) {
            nRaisedCorners.Value = Math.Min( 4 - nLoweredCorners.Value, nRaisedCorners.Value );
        }

        #endregion


        #region Tabs

        private void tabs_SelectedIndexChanged( object sender, EventArgs e ) {
            if( tabs.SelectedTab == tabExisting ) {
                tab = Tabs.ExistingMap;
            } else if( tabs.SelectedTab == tabLoad ) {
                tab = Tabs.LoadFile;
            } else if( tabs.SelectedTab == tabCopy ) {
                tab = Tabs.CopyWorld;
            } else if( tabs.SelectedTab == tabFlatgrass ) {
                tab = Tabs.Flatgrass;
            } else {
                tab = Tabs.Generator;
            }

            switch( tab ) {
                case Tabs.ExistingMap:
                    fileToLoad = World.FullFileName;
                    ShowMapDetails( tExistingMapInfo, fileToLoad );
                    StartLoadingMap();
                    return;
                case Tabs.LoadFile:
                    if( !String.IsNullOrEmpty( tFile.Text ) ) {
                        tFile.SelectAll();
                        fileToLoad = tFile.Text;
                        ShowMapDetails( tLoadFileInfo, fileToLoad );
                        StartLoadingMap();
                    }
                    return;
                case Tabs.CopyWorld:
                    if( cWorld.SelectedIndex != -1 ) {
                        bShow.Enabled = File.Exists( copyOptionsList[cWorld.SelectedIndex].FullFileName );
                    }
                    return;
                case Tabs.Flatgrass:
                    return;
                case Tabs.Generator:
                    return;
            }
        }

        enum Tabs {
            ExistingMap,
            LoadFile,
            CopyWorld,
            Flatgrass,
            Generator
        }

        #endregion


        static void ShowMapDetails( TextBox textBox, string fileName ) {
            DateTime creationTime, modificationTime;
            long fileSize;

            if( File.Exists( fileName ) ) {
                FileInfo existingMapFileInfo = new FileInfo( fileName );
                creationTime = existingMapFileInfo.CreationTime;
                modificationTime = existingMapFileInfo.LastWriteTime;
                fileSize = existingMapFileInfo.Length;

            } else if( Directory.Exists( fileName ) ) {
                DirectoryInfo dirInfo = new DirectoryInfo( fileName );
                creationTime = dirInfo.CreationTime;
                modificationTime = dirInfo.LastWriteTime;
                fileSize = dirInfo.GetFiles().Sum( finfo => finfo.Length );

            } else {
                textBox.Text = "File or directory \"" + fileName + "\" does not exist.";
                return;
            }

            MapFormat format = MapUtility.Identify( fileName, true );
            try {
                Map loadedMap = MapUtility.LoadHeader( fileName );
                const string msgFormat =
@"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}
Dimensions: {5}×{6}×{7}
    Blocks: {8}";
                textBox.Text = String.Format( msgFormat,
                                              fileName,
                                              format,
                                              (fileSize / 1024),
                                              creationTime.ToLongDateString(),
                                              modificationTime.ToLongDateString(),
                                              loadedMap.Width,
                                              loadedMap.Length,
                                              loadedMap.Height,
                                              loadedMap.Volume );

            } catch( Exception ex ) {
                const string msgFormat =
@"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}

Could not load more information:
{5}: {6}";
                textBox.Text = String.Format( msgFormat,
                                              fileName,
                                              format,
                                              (fileSize / 1024),
                                              creationTime.ToLongDateString(),
                                              modificationTime.ToLongDateString(),
                                              ex.GetType().Name,
                                              ex.Message );
            }
        }


        private void AddWorldPopup_FormClosing( object sender, FormClosingEventArgs e ) {
            Redraw( false );
            if( DialogResult == DialogResult.OK ) {
                if( Map == null ) {
                    e.Cancel = true;
                } else {
                    bwRenderer.CancelAsync();
                    Enabled = false;
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    tStatus1.Text = "Saving map...";
                    tStatus2.Text = "";
                    Refresh();

                    string newFileName = World.FullFileName;
                    Map.Save( newFileName );
                    string oldFileName = Path.Combine( Paths.MapPath, originalWorldName + ".fcm" );

                    if( originalWorldName != null && originalWorldName != World.Name && File.Exists( oldFileName ) ) {
                        try {
                            File.Delete( oldFileName );
                        } catch( Exception ex ) {
                            string errorMessage = String.Format( "Renaming the map file failed. Please delete the old file ({0}.fcm) manually.{1}{2}",
                                                                 originalWorldName, Environment.NewLine, ex );
                            MessageBox.Show( errorMessage, "Error renaming the map file" );
                        }
                    }
                }
            }
        }


        readonly SaveFileDialog savePreviewDialog = new SaveFileDialog();
        private void bSavePreview_Click( object sender, EventArgs e ) {
            try {
                using( Image img = (Image)preview.Image.Clone() ) {
                    if( savePreviewDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( savePreviewDialog.FileName ) ) {
                        switch( savePreviewDialog.FilterIndex ) {
                            case 1:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Png ); break;
                            case 2:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Tiff ); break;
                            case 3:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Bmp ); break;
                            case 4:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Jpeg ); break;
                        }
                    }
                }
            } catch( Exception ex ) {
                MessageBox.Show( "Could not prepare image for saving: " + ex );
            }
        }


        readonly OpenFileDialog browseTemplateDialog = new OpenFileDialog();
        private void bBrowseTemplate_Click( object sender, EventArgs e ) {
            if( browseTemplateDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( browseTemplateDialog.FileName ) ) {
                try {
                    generatorArgs = new MapGeneratorArgs( browseTemplateDialog.FileName );
                    LoadGeneratorArgs();
                    bGenerate.PerformClick();
                } catch( Exception ex ) {
                    MessageBox.Show( "Could not open template file: " + ex );
                }
            }
        }

        void LoadGeneratorArgs() {
            nMapHeight.Value = generatorArgs.MapHeight;
            nMapWidth.Value = generatorArgs.MapWidth;
            nMapLength.Value = generatorArgs.MapLength;

            cTheme.SelectedIndex = (int)generatorArgs.Theme;

            sDetailScale.Value = generatorArgs.DetailScale;
            sFeatureScale.Value = generatorArgs.FeatureScale;

            xLayeredHeightmap.Checked = generatorArgs.LayeredHeightmap;
            xMarbledMode.Checked = generatorArgs.MarbledHeightmap;
            xMatchWaterCoverage.Checked = generatorArgs.MatchWaterCoverage;
            xInvert.Checked = generatorArgs.InvertHeightmap;

            nMaxDepth.Value = generatorArgs.MaxDepth;
            nMaxHeight.Value = generatorArgs.MaxHeight;
            sRoughness.Value = (int)(generatorArgs.Roughness * 100);
            nSeed.Value = generatorArgs.Seed;
            xWater.Checked = generatorArgs.AddWater;

            if( generatorArgs.UseBias ) sBias.Value = (int)(generatorArgs.Bias * 100);
            else sBias.Value = 0;
            xDelayBias.Checked = generatorArgs.DelayBias;

            sWaterCoverage.Value = (int)(100 * generatorArgs.WaterCoverage);
            cMidpoint.SelectedIndex = generatorArgs.MidPoint + 1;
            nRaisedCorners.Value = generatorArgs.RaisedCorners;
            nLoweredCorners.Value = generatorArgs.LoweredCorners;

            xAddTrees.Checked = generatorArgs.AddTrees;
            xGiantTrees.Checked = generatorArgs.AddGiantTrees;
            nTreeHeight.Value = (generatorArgs.TreeHeightMax + generatorArgs.TreeHeightMin) / 2m;
            nTreeHeightVariation.Value = (generatorArgs.TreeHeightMax - generatorArgs.TreeHeightMin) / 2m;
            nTreeSpacing.Value = (generatorArgs.TreeSpacingMax + generatorArgs.TreeSpacingMin) / 2m;
            nTreeSpacingVariation.Value = (generatorArgs.TreeSpacingMax - generatorArgs.TreeSpacingMin) / 2m;

            xCaves.Checked = generatorArgs.AddCaves;
            xCaveLava.Checked = generatorArgs.AddCaveLava;
            xCaveWater.Checked = generatorArgs.AddCaveWater;
            xOre.Checked = generatorArgs.AddOre;
            sCaveDensity.Value = (int)(generatorArgs.CaveDensity * 100);
            sCaveSize.Value = (int)(generatorArgs.CaveSize * 100);

            xWaterLevel.Checked = generatorArgs.CustomWaterLevel;
            nWaterLevel.Maximum = generatorArgs.MapHeight;
            nWaterLevel.Value = Math.Min( generatorArgs.WaterLevel, generatorArgs.MapHeight );

            xAddSnow.Checked = generatorArgs.AddSnow;

            nSnowAltitude.Value = generatorArgs.SnowAltitude - (generatorArgs.CustomWaterLevel ? generatorArgs.WaterLevel : generatorArgs.MapHeight / 2);
            nSnowTransition.Value = generatorArgs.SnowTransition;

            xAddCliffs.Checked = generatorArgs.AddCliffs;
            sCliffThreshold.Value = (int)(generatorArgs.CliffThreshold * 100);
            xCliffSmoothing.Checked = generatorArgs.CliffSmoothing;

            xAddBeaches.Checked = generatorArgs.AddBeaches;
            nBeachExtent.Value = generatorArgs.BeachExtent;
            nBeachHeight.Value = generatorArgs.BeachHeight;

            sAboveFunc.Value = ExponentToTrackBar( sAboveFunc, generatorArgs.AboveFuncExponent );
            sBelowFunc.Value = ExponentToTrackBar( sBelowFunc, generatorArgs.BelowFuncExponent );

            nMaxHeightVariation.Value = generatorArgs.MaxHeightVariation;
            nMaxDepthVariation.Value = generatorArgs.MaxDepthVariation;
        }

        void SaveGeneratorArgs() {
            generatorArgs = new MapGeneratorArgs {
                DetailScale = sDetailScale.Value,
                FeatureScale = sFeatureScale.Value,
                MapHeight = (int)nMapHeight.Value,
                MapWidth = (int)nMapWidth.Value,
                MapLength = (int)nMapLength.Value,
                LayeredHeightmap = xLayeredHeightmap.Checked,
                MarbledHeightmap = xMarbledMode.Checked,
                MatchWaterCoverage = xMatchWaterCoverage.Checked,
                MaxDepth = (int)nMaxDepth.Value,
                MaxHeight = (int)nMaxHeight.Value,
                AddTrees = xAddTrees.Checked,
                AddGiantTrees = xGiantTrees.Checked,
                Roughness = sRoughness.Value / 100f,
                Seed = (int)nSeed.Value,
                Theme = (MapGenTheme)cTheme.SelectedIndex,
                TreeHeightMax = (int)(nTreeHeight.Value + nTreeHeightVariation.Value),
                TreeHeightMin = (int)(nTreeHeight.Value - nTreeHeightVariation.Value),
                TreeSpacingMax = (int)(nTreeSpacing.Value + nTreeSpacingVariation.Value),
                TreeSpacingMin = (int)(nTreeSpacing.Value - nTreeSpacingVariation.Value),
                UseBias = (sBias.Value != 0),
                DelayBias = xDelayBias.Checked,
                WaterCoverage = sWaterCoverage.Value / 100f,
                Bias = sBias.Value / 100f,
                MidPoint = cMidpoint.SelectedIndex - 1,
                RaisedCorners = (int)nRaisedCorners.Value,
                LoweredCorners = (int)nLoweredCorners.Value,
                InvertHeightmap = xInvert.Checked,
                AddWater = xWater.Checked,
                AddCaves = xCaves.Checked,
                AddOre = xOre.Checked,
                AddCaveLava = xCaveLava.Checked,
                AddCaveWater = xCaveWater.Checked,
                CaveDensity = sCaveDensity.Value / 100f,
                CaveSize = sCaveSize.Value / 100f,
                CustomWaterLevel = xWaterLevel.Checked,
                WaterLevel = (int)(xWaterLevel.Checked ? nWaterLevel.Value : nMapHeight.Value / 2),
                AddSnow = xAddSnow.Checked,
                SnowTransition = (int)nSnowTransition.Value,
                SnowAltitude = (int)(nSnowAltitude.Value + (xWaterLevel.Checked ? nWaterLevel.Value : nMapHeight.Value / 2)),
                AddCliffs = xAddCliffs.Checked,
                CliffThreshold = sCliffThreshold.Value / 100f,
                CliffSmoothing = xCliffSmoothing.Checked,
                AddBeaches = xAddBeaches.Checked,
                BeachExtent = (int)nBeachExtent.Value,
                BeachHeight = (int)nBeachHeight.Value,
                AboveFuncExponent = TrackBarToExponent( sAboveFunc ),
                BelowFuncExponent = TrackBarToExponent( sBelowFunc ),
                MaxHeightVariation = (int)nMaxHeightVariation.Value,
                MaxDepthVariation = (int)nMaxDepthVariation.Value
            };
        }


        readonly SaveFileDialog saveTemplateDialog = new SaveFileDialog();
        private void bSaveTemplate_Click( object sender, EventArgs e ) {
            if( saveTemplateDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( saveTemplateDialog.FileName ) ) {
                try {
                    SaveGeneratorArgs();
                    generatorArgs.Save( saveTemplateDialog.FileName );
                } catch( Exception ex ) {
                    MessageBox.Show( "Could not open template file: " + ex );
                }
            }
        }

        private void cTemplates_SelectedIndexChanged( object sender, EventArgs e ) {
            generatorArgs = MapGenerator.MakeTemplate( (MapGenTemplate)cTemplates.SelectedIndex );
            LoadGeneratorArgs();
            bGenerate.PerformClick();
        }

        private void xCaves_CheckedChanged( object sender, EventArgs e ) {
            gCaves.Visible = xCaves.Checked && xAdvanced.Checked;
        }

        private void sCaveDensity_ValueChanged( object sender, EventArgs e ) {
            lCaveDensityDisplay.Text = sCaveDensity.Value + "%";
        }

        private void sCaveSize_ValueChanged( object sender, EventArgs e ) {
            lCaveSizeDisplay.Text = sCaveSize.Value + "%";
        }

        private void xWaterLevel_CheckedChanged( object sender, EventArgs e ) {
            nWaterLevel.Enabled = xWaterLevel.Checked;
        }

        private void nHeight_ValueChanged( object sender, EventArgs e ) {
            nWaterLevel.Value = Math.Min( nWaterLevel.Value, nMapHeight.Value );
            nWaterLevel.Maximum = nMapHeight.Value;
        }

        private void xAddTrees_CheckedChanged( object sender, EventArgs e ) {
            gTrees.Visible = xAddTrees.Checked;
        }

        private void xWater_CheckedChanged( object sender, EventArgs e ) {
            xAddBeaches.Enabled = xWater.Checked;
        }

        private void sAboveFunc_ValueChanged( object sender, EventArgs e ) {
            lAboveFuncUnits.Text = (1 / TrackBarToExponent( sAboveFunc )).ToString( "0.0%" );
        }

        private void sBelowFunc_ValueChanged( object sender, EventArgs e ) {
            lBelowFuncUnits.Text = (1 / TrackBarToExponent( sBelowFunc )).ToString( "0.0%" );
        }

        static float TrackBarToExponent( TrackBar bar ) {
            if( bar.Value >= bar.Maximum / 2 ) {
                float normalized = (bar.Value - bar.Maximum / 2f) / (bar.Maximum / 2f);
                return 1 + normalized * normalized * 3;
            } else {
                float normalized = (bar.Value / (bar.Maximum / 2f));
                return normalized * .75f + .25f;
            }
        }

        static int ExponentToTrackBar( TrackBar bar, float val ) {
            if( val >= 1 ) {
                float normalized = (float)Math.Sqrt( (val - 1) / 3f );
                return (int)(bar.Maximum / 2f + normalized * (bar.Maximum / 2f));
            } else {
                float normalized = (val - .25f) / .75f;
                return (int)(normalized * bar.Maximum / 2f);
            }
        }

        private void sCliffThreshold_ValueChanged( object sender, EventArgs e ) {
            lCliffThresholdUnits.Text = sCliffThreshold.Value + "%";
        }

        private void xAddSnow_CheckedChanged( object sender, EventArgs e ) {
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
        }

        private void xAddCliffs_CheckedChanged( object sender, EventArgs e ) {
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
        }

        private void xAddBeaches_CheckedChanged( object sender, EventArgs e ) {
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }

        private void xBlockDB_CheckStateChanged( object sender, EventArgs e ) {
            switch( xBlockDB.CheckState ) {
                case CheckState.Indeterminate:
                    World.BlockDBEnabled = YesNoAuto.Auto;
                    xBlockDB.Text = "BlockDB (Auto)";
                    break;
                case CheckState.Checked:
                    World.BlockDBEnabled = YesNoAuto.Yes;
                    xBlockDB.Text = "BlockDB (On)";
                    break;
                case CheckState.Unchecked:
                    World.BlockDBEnabled = YesNoAuto.No;
                    xBlockDB.Text = "BlockDB (Off)";
                    break;
            }
        }
    }
}