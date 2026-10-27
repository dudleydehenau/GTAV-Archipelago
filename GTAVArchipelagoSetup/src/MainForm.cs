using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVArchipelagoSetup
{
    internal sealed class MainForm : Form
    {
        private const string ScriptHookVPage = "http://www.dev-c.com/gtav/scripthookv/";
        private const string GuidePage = "https://github.com/dudleydehenau/GTAV-Archipelago#readme";
        private const int Gap = 18;

        private readonly SourceList _sources;
        private readonly Panel _scroll = new Panel();
        private readonly List<StepCard> _cards = new List<StepCard>();

        private readonly StepCard _gameCard = new StepCard(1, "Your game");
        private readonly TextBox _folder = new TextBox();

        private readonly StepCard _hookCard = new StepCard(2, "ScriptHookV");
        private readonly Label _hookHelp = Theme.Label(string.Empty, Theme.Muted);
        private readonly Button _hookPage = Theme.Secondary("1. Open the download page");
        private readonly Button _hookFound = Theme.Primary("Install");
        private readonly Button _hookPick = Theme.Secondary("Choose the zip...");
        private string _foundZip;

        private readonly StepCard _modsCard = new StepCard(3, "The mods");
        private readonly CheckedListBox _components = new CheckedListBox();
        private readonly CheckBox _keepSettings = new CheckBox();
        private readonly Button _install = Theme.Primary("Install / Update the mods");
        private readonly ProgressBar _progress = new ProgressBar();

        private readonly StepCard _playCard = new StepCard(4, "Play");
        private readonly Label _playHelp = Theme.Label(string.Empty, Theme.Muted);
        private readonly Button _launchSteam = Theme.Primary("Play with Steam (BattlEye off)");
        private readonly Button _copyArgument = Theme.Secondary("Copy -nobattleye");

        private readonly StepCard _toolsCard = new StepCard(5, "GTA Online and tools");
        private readonly Button _toggleMods = Theme.Secondary("Disable mods (GTA Online)");
        private readonly Button _openFolder = Theme.Secondary("Open the mods folder");
        private readonly Button _uninstall = Theme.Danger("Uninstall");
        private readonly Button _showLog = Theme.Secondary("Show details");
        private readonly TextBox _log = new TextBox();

        private bool _busy;

        public MainForm()
        {
            Text = "GTA V Archipelago Setup";
            Font = Theme.Regular();
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            ClientSize = new Size(820, 860);
            MinimumSize = new Size(640, 560);
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            _sources = SourceList.Load(out string origin);
            BuildLayout();
            Shown += (s, e) => Relayout();
            Log("Download list: " + origin);
            string found = GameLocator.Find();
            if (found != null) _folder.Text = found;
            RefreshAll();
        }

        private static string SetupVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private void BuildLayout()
        {
            var header = new HeaderPanel(SetupVersion) { Dock = DockStyle.Top, Height = 92 };
            _scroll.Dock = DockStyle.Fill;
            _scroll.AutoScroll = true;
            _scroll.BackColor = Theme.Background;
            _scroll.AllowDrop = true;
            _scroll.DragEnter += OnDragEnter;
            _scroll.DragDrop += OnDragDrop;
            _scroll.AutoScrollMargin = new Size(0, Gap);
            Controls.Add(_scroll);
            Controls.Add(header);

            BuildGameCard();
            BuildHookCard();
            BuildModsCard();
            BuildPlayCard();
            BuildToolsCard();

            foreach (StepCard card in new[] { _gameCard, _hookCard, _modsCard, _playCard, _toolsCard })
            {
                _cards.Add(card);
                _scroll.Controls.Add(card);
            }
            _scroll.Resize += (s, e) => Relayout();
        }

        private void BuildGameCard()
        {
            var row = Row();
            _folder.BackColor = Theme.Field;
            _folder.ForeColor = Theme.Text;
            _folder.BorderStyle = BorderStyle.FixedSingle;
            _folder.Font = Theme.Regular(10f);
            _folder.Width = 420;
            _folder.Margin = new Padding(0, 6, 8, 4);
            _folder.TextChanged += (s, e) => RefreshAll();
            var browse = Theme.Secondary("Browse...");
            browse.Click += (s, e) => Browse();
            var detect = Theme.Secondary("Find it for me");
            detect.Click += (s, e) =>
            {
                string found = GameLocator.Find();
                if (found == null) Log("GTA V was not found automatically. Use Browse and pick the folder that contains GTA5.exe.");
                else _folder.Text = found;
            };
            row.Controls.AddRange(new Control[] { _folder, browse, detect });
            _gameCard.Body.Controls.Add(Theme.Label("The folder that contains GTA5.exe (GTA V Legacy, not Enhanced).", Theme.Muted));
            _gameCard.Body.Controls.Add(row);
        }

        private void BuildHookCard()
        {
            _hookCard.Body.Controls.Add(_hookHelp);
            var row = Row();
            _hookPage.Click += (s, e) => Open(ScriptHookVPage);
            _hookFound.Click += (s, e) => InstallScriptHookV(_foundZip);
            _hookPick.Click += (s, e) => PickScriptHookV();
            row.Controls.AddRange(new Control[] { _hookPage, _hookFound, _hookPick });
            _hookCard.Body.Controls.Add(row);
            _hookCard.Body.Controls.Add(Theme.Label("Tip: you can also drag the ScriptHookV zip onto this window.", Theme.Muted, Theme.Regular(8.75f)));
        }

        private void BuildModsCard()
        {
            _modsCard.Body.Controls.Add(Theme.Label("Downloads the latest release of every part from GitHub and installs it into an \"Archipelago\" folder next to GTA5.exe. The APWorld goes to your Archipelago launcher.", Theme.Muted));
            _components.BackColor = Theme.Field;
            _components.ForeColor = Theme.Text;
            _components.BorderStyle = BorderStyle.None;
            _components.CheckOnClick = true;
            _components.IntegralHeight = false;
            _components.Font = Theme.Regular(9.5f);
            _components.Margin = new Padding(0, 8, 0, 6);
            foreach (Component component in _sources.Components)
                _components.Items.Add(component, component.IsConfigured);
            _components.Height = Math.Max(60, _components.Items.Count * (_components.ItemHeight + 2) + 6);
            _modsCard.Body.Controls.Add(_components);

            _keepSettings.Text = "Keep my settings (config.json, vehicles.json, weapons.json)";
            _keepSettings.Checked = true;
            _keepSettings.AutoSize = true;
            _keepSettings.ForeColor = Theme.Text;
            _keepSettings.BackColor = Color.Transparent;
            _keepSettings.Margin = new Padding(0, 2, 0, 6);
            _modsCard.Body.Controls.Add(_keepSettings);

            _install.Click += async (s, e) => await Install();
            _modsCard.Body.Controls.Add(_install);
            _progress.Height = 8;
            _progress.Width = 300;
            _progress.Margin = new Padding(0, 6, 0, 0);
            _progress.Visible = false;
            _modsCard.Body.Controls.Add(_progress);
        }

        private void BuildPlayCard()
        {
            _playCard.Body.Controls.Add(_playHelp);
            var row = Row();
            _launchSteam.Click += (s, e) => Open("steam://run/271590//-nobattleye/");
            _copyArgument.Click += (s, e) =>
            {
                Clipboard.SetText("-nobattleye");
                Log("\"-nobattleye\" copied to the clipboard.");
            };
            var guide = Theme.Secondary("Read the guide");
            guide.Click += (s, e) => Open(GuidePage);
            row.Controls.AddRange(new Control[] { _launchSteam, _copyArgument, guide });
            _playCard.Body.Controls.Add(row);
        }

        private void BuildToolsCard()
        {
            _toolsCard.Body.Controls.Add(Theme.Label("The mods are for story mode only. Before GTA Online, click \"Disable mods (GTA Online)\" and turn BattlEye back on. Click it again to play Archipelago.", Theme.Muted));
            var row = Row();
            _toggleMods.Click += (s, e) => ToggleMods();
            _openFolder.Click += (s, e) =>
            {
                string path = Path.Combine(_folder.Text, Installer.ModsFolder);
                if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
            };
            _uninstall.Click += (s, e) => Uninstall();
            _showLog.Click += (s, e) => ShowLog(!_log.Visible);
            row.Controls.AddRange(new Control[] { _toggleMods, _openFolder, _uninstall, _showLog });
            _toolsCard.Body.Controls.Add(row);

            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Theme.Field;
            _log.ForeColor = Theme.Muted;
            _log.BorderStyle = BorderStyle.None;
            _log.Font = new Font("Consolas", 9f);
            _log.Height = 170;
            _log.Width = 400;
            _log.Margin = new Padding(0, 8, 0, 0);
            _log.Visible = false;
            _toolsCard.Body.Controls.Add(_log);
            _toolsCard.SetStatus(StepState.Pending, "Switch back to GTA Online, open your files or remove everything.");
        }

        private static FlowLayoutPanel Row() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4),
        };

        private void Relayout()
        {
            if (_cards.Count == 0) return;
            int width = Math.Max(400, _scroll.ClientSize.Width - Gap * 2 - (_scroll.VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth));
            _scroll.SuspendLayout();
            int y = Gap + _scroll.AutoScrollPosition.Y;
            int inner = Math.Max(200, width - _toolsCard.Padding.Horizontal);
            _folder.Width = Math.Max(200, Math.Min(520, inner - 260));
            _log.Width = inner;
            _progress.Width = Math.Min(420, inner);
            foreach (StepCard card in _cards)
            {
                card.Relayout(width);
                card.Location = new Point(Gap, y);
                y += card.Height + 12;
            }
            _scroll.ResumeLayout();
        }

        private Installer CreateInstaller() => new Installer(_folder.Text.Trim(), Log);

        private void RefreshAll()
        {
            string folder = _folder.Text.Trim();
            bool valid = GameLocator.IsGameFolder(folder);
            string version = valid ? GameLocator.Version(folder) ?? "unknown" : null;
            Platform platform = valid ? GameLocator.Detect(folder) : Platform.Unknown;
            Installer installer = valid ? CreateInstaller() : null;
            RefreshGame(folder, valid, version, platform);
            RefreshScriptHookV(valid, version, installer);
            RefreshMods(valid, installer);
            RefreshPlay(valid, platform, installer);
            UpdateButtons(valid);
            Relayout();
        }

        private void RefreshGame(string folder, bool valid, string version, Platform platform)
        {
            if (!valid)
            {
                if (GameLocator.IsEnhanced(folder))
                    _gameCard.SetStatus(StepState.Error, "This is GTA V Enhanced. The mods need GTA V Legacy: pick the folder of the Legacy edition.");
                else
                    _gameCard.SetStatus(StepState.Error, "GTA5.exe not found. Click \"Find it for me\" or \"Browse...\".");
                return;
            }
            string supported = string.Join(" or ", GameLocator.SupportedVersions);
            if (GameLocator.IsSupported(version))
            {
                _gameCard.SetStatus(StepState.Done, $"GTA V Legacy {version} on {platform}. This version is supported.");
                return;
            }
            int build = GameLocator.Build(version);
            _gameCard.SetStatus(StepState.Warning, build > GameLocator.LatestBuild
                ? $"GTA V Legacy {version} on {platform}: this game update is newer than the mods ({supported}). Check for a newer release of the mods."
                : $"GTA V Legacy {version} on {platform}: this version is too old. Update the game in your launcher to {GameLocator.SupportedVersions[0]}.");
        }

        private void RefreshScriptHookV(bool valid, string version, Installer installer)
        {
            int build = GameLocator.Build(version);
            string versionText = valid && build > 0 ? $"for game version {build}" : "for your game version";
            _foundZip = Installer.FindScriptHookVZip();
            IReadOnlyList<int> foundBuilds = Installer.BuildsIn(_foundZip == null ? null : Path.GetFileName(_foundZip));
            bool foundMatches = _foundZip != null && (build == 0 || foundBuilds.Count == 0 || foundBuilds.Contains(build));

            _hookHelp.Text =
                "Every GTA V script mod needs ScriptHookV. Its author does not allow other programs to download it, so you get it yourself:" + Environment.NewLine +
                "1.  Click \"Open the download page\"." + Environment.NewLine +
                $"2.  On that page, click the \"Download\" link and save the zip (ScriptHookV_...zip). It must be the version {versionText}." + Environment.NewLine +
                "3.  Come back here: the setup finds the zip in your Downloads folder. Click the green \"Install\" button.";

            if (_foundZip != null)
            {
                _hookFound.Text = "2. Install " + Path.GetFileName(_foundZip);
                _hookFound.Visible = true;
            }
            else
            {
                _hookFound.Visible = false;
            }

            if (!valid)
            {
                _hookCard.SetStatus(StepState.Pending, "Choose your game folder first.");
                return;
            }
            if (installer.ModsDisabled)
            {
                _hookCard.SetStatus(StepState.Warning, "The mods are disabled for GTA Online (see step 5).");
                return;
            }
            if (!installer.HasScriptHookV)
            {
                _hookCard.SetStatus(StepState.Error, _foundZip == null
                    ? "Not installed yet. Follow the three steps below."
                    : foundMatches
                        ? $"Not installed yet. {Path.GetFileName(_foundZip)} is in your Downloads folder: click the green button."
                        : $"Not installed yet. {Path.GetFileName(_foundZip)} in your Downloads folder is not made for game version {build}: download the new one.");
                return;
            }
            string source = installer.ScriptHookVSource;
            IReadOnlyList<int> installedBuilds = Installer.BuildsIn(source);
            if (source != null && build > 0 && installedBuilds.Count > 0 && !installedBuilds.Contains(build))
            {
                _hookCard.SetStatus(StepState.Warning, $"Installed from {source}, which is not made for game version {build}. Download the new ScriptHookV and install it.");
                return;
            }
            _hookCard.SetStatus(StepState.Done, source == null
                ? "Installed. If the game shows a ScriptHookV error at start, download the latest version and install it here."
                : $"Installed ({source}).");
        }

        private void RefreshMods(bool valid, Installer installer)
        {
            if (!valid)
            {
                _modsCard.SetStatus(StepState.Pending, "Choose your game folder first.");
                return;
            }
            bool installed = Directory.Exists(installer.ModsPath) && Directory.GetFiles(installer.ModsPath, "GameArchipelago.dll").Length > 0;
            _modsCard.SetStatus(installed ? StepState.Done : StepState.Pending,
                installed ? "Installed. Click again any time to update to the latest release." : "Not installed yet.");
        }

        private void RefreshPlay(bool valid, Platform platform, Installer installer)
        {
            string how;
            switch (platform)
            {
                case Platform.Steam:
                    how = "Steam: click \"Play with Steam\" below, or add -nobattleye to GTA V Legacy > Properties > Launch Options.";
                    break;
                case Platform.Epic:
                    how = "Epic Games: Library > GTA V > Manage > Launch Options, then paste -nobattleye.";
                    break;
                case Platform.Rockstar:
                    how = "Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\".";
                    break;
                default:
                    how = "Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\". Steam or Epic: add -nobattleye to the launch options.";
                    break;
            }
            _playHelp.Text = "Start the game with BattlEye turned off. " + how + Environment.NewLine +
                             "In game, press F6 > Connection, enter the room address, your slot name and password, then start a New Game from the pause menu.";
            _launchSteam.Visible = platform == Platform.Steam;
            bool ready = valid && installer != null && installer.HasScriptHookV && !installer.ModsDisabled && Directory.Exists(installer.ModsPath);
            _playCard.SetStatus(ready ? StepState.Done : StepState.Pending, ready ? "Everything is ready." : "Finish the steps above first.");
        }

        private void UpdateButtons(bool valid)
        {
            bool idle = !_busy;
            bool modsFolder = valid && Directory.Exists(Path.Combine(_folder.Text.Trim(), Installer.ModsFolder));
            _install.Enabled = valid && idle;
            _hookFound.Enabled = valid && idle;
            _hookPick.Enabled = valid && idle;
            _uninstall.Enabled = valid && idle && modsFolder;
            _openFolder.Enabled = modsFolder;
            if (valid)
            {
                bool disabled = CreateInstaller().ModsDisabled;
                _toggleMods.Text = disabled ? "Enable mods (story mode)" : "Disable mods (GTA Online)";
                _toggleMods.Enabled = idle;
            }
            else
            {
                _toggleMods.Text = "Disable mods (GTA Online)";
                _toggleMods.Enabled = false;
            }
        }

        private void Browse()
        {
            using (var dialog = new FolderBrowserDialog { Description = "Select the GTA V Legacy folder (the one with GTA5.exe)." })
            {
                if (Directory.Exists(_folder.Text)) dialog.SelectedPath = _folder.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) _folder.Text = dialog.SelectedPath;
            }
        }

        private async Task Install()
        {
            List<Component> selected = _components.CheckedItems.Cast<Component>().ToList();
            if (selected.Count == 0)
            {
                Log("Nothing selected.");
                return;
            }
            if (Process.GetProcessesByName("GTA5").Length > 0)
            {
                MessageBox.Show(this, "Close GTA V before installing.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _busy = true;
            _progress.Value = 0;
            _progress.Visible = true;
            _modsCard.SetStatus(StepState.Pending, "Downloading and installing...");
            UpdateButtons(true);
            Relayout();
            bool failed = false;
            try
            {
                Installer installer = CreateInstaller();
                await installer.Install(selected, _keepSettings.Checked, value => Invoke(new Action(() => _progress.Value = Math.Max(0, Math.Min(100, value)))));
            }
            catch (Exception ex)
            {
                failed = true;
                Log("ERROR: " + ex.GetBaseException().Message);
            }
            finally
            {
                _busy = false;
                _progress.Visible = false;
                RefreshAll();
            }
            if (failed)
            {
                _modsCard.SetStatus(StepState.Error, "The installation failed. Check your internet connection and look at the details below.");
                ShowLog(true);
            }
            else if (!CreateInstaller().HasScriptHookV && !CreateInstaller().ModsDisabled)
            {
                MessageBox.Show(this, "The mods are installed. One thing is left: ScriptHookV (step 2).\n\nIt cannot be downloaded automatically. Follow the three steps of the ScriptHookV box.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PickScriptHookV()
        {
            using (var dialog = new OpenFileDialog { Filter = "ScriptHookV zip|*.zip", Title = "Select the ScriptHookV zip downloaded from dev-c.com" })
            {
                string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloads)) dialog.InitialDirectory = downloads;
                if (dialog.ShowDialog(this) == DialogResult.OK) InstallScriptHookV(dialog.FileName);
            }
        }

        private void InstallScriptHookV(string zip)
        {
            if (string.IsNullOrEmpty(zip) || !File.Exists(zip)) return;
            if (Process.GetProcessesByName("GTA5").Length > 0)
            {
                MessageBox.Show(this, "Close GTA V first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                CreateInstaller().InstallScriptHookV(zip);
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RefreshAll();
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DroppedZip(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string zip = DroppedZip(e);
            if (zip != null) InstallScriptHookV(zip);
        }

        private static string DroppedZip(DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files)) return null;
            return files.FirstOrDefault(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }

        private void ToggleMods()
        {
            try
            {
                Installer installer = CreateInstaller();
                if (installer.ModsDisabled) installer.EnableMods();
                else installer.DisableMods();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            RefreshAll();
        }

        private void Uninstall()
        {
            DialogResult answer = MessageBox.Show(this, "Keep your settings and Archipelago saves?\n\nYes: remove the mods only.\nNo: delete the whole Archipelago folder.",
                Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) return;
            try
            {
                CreateInstaller().Uninstall(answer == DialogResult.Yes);
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            RefreshAll();
        }

        private void ShowLog(bool visible)
        {
            _log.Visible = visible;
            _showLog.Text = visible ? "Hide details" : "Show details";
            Relayout();
            if (visible) _scroll.ScrollControlIntoView(_toolsCard);
        }

        private void Open(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log("Could not open " + target + ": " + ex.Message);
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), message);
                return;
            }
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private sealed class HeaderPanel : Panel
        {
            private readonly string _version;

            public HeaderPanel(string version)
            {
                _version = version;
                DoubleBuffered = true;
                BackColor = Theme.Background;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(24, 40, 30), Theme.Background, LinearGradientMode.Horizontal))
                    g.FillRectangle(brush, ClientRectangle);
                using (var accent = new SolidBrush(Theme.Accent))
                    g.FillRectangle(accent, 0, Height - 3, Width, 3);
                using (Font title = Theme.Bold(20f))
                using (Font small = Theme.Regular(9.5f))
                {
                    TextRenderer.DrawText(g, "GTA V ARCHIPELAGO", title, new Point(Gap, 16), Theme.Text);
                    TextRenderer.DrawText(g, $"Setup {_version}  •  story mode randomizer for GTA V Legacy", small, new Point(Gap + 2, 56), Theme.Muted);
                }
            }

            protected override void OnResize(EventArgs eventargs)
            {
                base.OnResize(eventargs);
                Invalidate();
            }
        }
    }
}
