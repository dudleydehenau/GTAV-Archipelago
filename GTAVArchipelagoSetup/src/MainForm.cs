using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVArchipelagoSetup
{
    internal sealed class MainForm : Form, IMessageFilter
    {
        private const string ScriptHookVPage = "http://www.dev-c.com/gtav/scripthookv/";
        private const string GuidePage = "https://dudleydehenau.github.io/GTAV-Archipelago/";
        private const string HelpPage = "https://github.com/dudleydehenau/GTAV-Archipelago#feedback-and-bug-reports";

        private const int WelcomePage = 0;
        private const int GamePage = 1;
        private const int HookPage = 2;
        private const int ModsPage = 3;
        private const int PlayPage = 4;

        private readonly SourceList _sources;
        private readonly StringBuilder _history = new StringBuilder();
        private readonly Panel _host = new Panel();
        private readonly Panel _viewport = new Panel();
        private readonly ThinScrollBar _scroll = new ThinScrollBar();
        private readonly List<FlowLayoutPanel> _pages = new List<FlowLayoutPanel>();
        private readonly StepRail _rail = new StepRail("Game", "ScriptHookV", "Mods", "Play");
        private readonly Timer _watch = new Timer { Interval = 2000 };
        private int _page = -1;
        private bool _busy;
        private LogWindow _logWindow;

        private readonly FlatButton _back = Theme.Secondary("Back");
        private readonly FlatButton _next = Theme.Primary("Start");
        private readonly FlatButton _onlineMode = Theme.Quiet("GTA Online mode");
        private readonly FlatButton _more = Theme.Quiet("More ▾");
        private readonly ContextMenuStrip _moreMenu = new ContextMenuStrip();

        private readonly Label _welcomeState = Theme.Label(string.Empty, Theme.Good, Theme.Bold(10f));

        private readonly TextBox _folder = new TextBox();
        private readonly StatusLine _gameStatus = new StatusLine();

        private readonly Label _hookStep2 = Theme.Label(string.Empty, Theme.Text);
        private readonly FlatButton _hookInstall = Theme.Primary("Install");
        private readonly StatusLine _hookStatus = new StatusLine();
        private string _foundZip;

        private readonly FlatButton _install = Theme.Primary("Install the mods");
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly Label _progressText = Theme.Label(string.Empty, Theme.Muted, Theme.Mono(8.5f));
        private readonly StatusLine _modsStatus = new StatusLine();
        private readonly FlatButton _advancedToggle = Theme.Quiet("Choose what to install ▾");
        private readonly FlowLayoutPanel _advanced = Column();
        private readonly CheckedListBox _components = new CheckedListBox();
        private readonly CheckBox _keepSettings = new CheckBox();

        private readonly Label _battlEye = Theme.Label(string.Empty, Theme.Text);
        private readonly FlatButton _launchSteam = Theme.Primary("Play with Steam (BattlEye off)");
        private readonly FlatButton _copyArgument = Theme.Secondary("Copy -nobattleye");
        private readonly StatusLine _playStatus = new StatusLine();

        public MainForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "GTA V Archipelago Setup";
            Font = Theme.Regular();
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            ClientSize = new Size(760, 640);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
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
            BuildChrome();
            _pages.Add(BuildWelcome());
            _pages.Add(BuildGame());
            _pages.Add(BuildHook());
            _pages.Add(BuildMods());
            _pages.Add(BuildPlay());
            foreach (FlowLayoutPanel page in _pages)
            {
                page.Visible = false;
                _viewport.Controls.Add(page);
                page.SizeChanged += (s, e) => PlacePage();
            }
            _viewport.Resize += (s, e) => Fit();
            _scroll.Scrolled += (s, e) => PlacePage();
            ResumeLayout(false);
            PerformLayout();
            _watch.Tick += (s, e) => { if (!_busy) RefreshAll(); };
            Log("Setup " + SetupVersion + ". Download list: " + origin + ".");

            string found = GameLocator.Find();
            if (found != null) _folder.Text = found;
            RefreshAll();
            GoTo(WelcomePage);
        }

        private static string SetupVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private string GameFolder => _folder.Text.Trim();

        private bool GameValid => GameLocator.IsGameFolder(GameFolder);

        private Installer CreateInstaller() => new Installer(GameFolder, Log);

        private bool ModsInstalled
        {
            get
            {
                if (!GameValid) return false;
                string path = Path.Combine(GameFolder, Installer.ModsFolder);
                return Directory.Exists(path) && File.Exists(Path.Combine(path, "GameArchipelago.dll"));
            }
        }

        private void BuildChrome()
        {
            var header = new HeaderPanel(SetupVersion) { Dock = DockStyle.Top, Height = 64 };
            _rail.Dock = DockStyle.Top;
            _rail.Height = 64;

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.Background, Padding = new Padding(0, 1, 0, 0) };
            footer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Line))
                    e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Theme.Background,
                Padding = new Padding(16, 14, 0, 0),
            };
            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Background,
                Padding = new Padding(0, 12, 16, 0),
            };
            _onlineMode.Click += (s, e) => ToggleMods();
            _more.Click += (s, e) => _moreMenu.Show(_more, new Point(0, -_moreMenu.PreferredSize.Height));
            left.Controls.Add(_onlineMode);
            left.Controls.Add(_more);
            _back.Click += (s, e) => GoTo(_page - 1);
            _next.Click += (s, e) => Next();
            right.Controls.Add(_back);
            right.Controls.Add(_next);
            footer.Controls.Add(left);
            footer.Controls.Add(right);

            _moreMenu.Renderer = new DarkMenuRenderer();
            _moreMenu.Font = Theme.Regular(9.5f);
            _moreMenu.Items.Add("Open the mods folder", null, (s, e) => OpenModsFolder());
            _moreMenu.Items.Add("Install ScriptHookV again", null, (s, e) => GoTo(HookPage));
            _moreMenu.Items.Add("Show the details", null, (s, e) => ShowLog());
            _moreMenu.Items.Add("Read the guide", null, (s, e) => Open(GuidePage));
            _moreMenu.Items.Add("Report a problem", null, (s, e) => Open(HelpPage));
            _moreMenu.Items.Add(new ToolStripSeparator());
            _moreMenu.Items.Add("Uninstall", null, (s, e) => Uninstall());
            _moreMenu.Opening += (s, e) =>
            {
                bool installed = ModsInstalled;
                _moreMenu.Items[0].Enabled = installed;
                _moreMenu.Items[1].Enabled = GameValid;
                _moreMenu.Items[6].Enabled = installed && !_busy;
            };

            _host.Dock = DockStyle.Fill;
            _host.BackColor = Theme.Background;
            _host.Padding = new Padding(40, 18, 14, 10);
            _host.AllowDrop = true;
            _scroll.Dock = DockStyle.Right;
            _scroll.Width = 8;
            _viewport.Dock = DockStyle.Fill;
            _viewport.BackColor = Theme.Background;
            _viewport.Padding = new Padding(0, 0, 18, 0);
            _host.Controls.Add(_viewport);
            _host.Controls.Add(_scroll);
            _host.DragEnter += OnDragEnter;
            _host.DragDrop += OnDragDrop;

            Controls.Add(_host);
            Controls.Add(footer);
            Controls.Add(_rail);
            Controls.Add(header);
        }

        private FlowLayoutPanel BuildWelcome()
        {
            FlowLayoutPanel page = Page();
            page.Controls.Add(Eyebrow("Archipelago randomizer · GTA V story mode"));
            var title = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 10) };
            Label first = Theme.Heading("LOS SANTOS,", Theme.Text, Theme.Display(30f));
            first.Margin = new Padding(0, 0, 10, 0);
            title.Controls.Add(first);
            Label second = Theme.Heading("SHUFFLED.", Theme.Accent, Theme.Display(30f));
            second.Margin = new Padding(0);
            title.Controls.Add(second);
            page.Controls.Add(title);
            page.Controls.Add(Theme.Label("This setup installs everything you need to play GTA V story mode in an Archipelago multiworld. It takes about five minutes, and it walks you through each step.", Theme.Muted, Theme.Regular(11f)));
            page.Controls.Add(Spacer(8));
            page.Controls.Add(Bullet(Theme.Accent, "GTA V Legacy 1.0.3889 or 1.0.3725, on Steam, Epic or the Rockstar Games Launcher."));
            page.Controls.Add(Bullet(Theme.Info, "An internet connection: the mods come from GitHub (about 10 MB)."));
            page.Controls.Add(Bullet(Theme.Error, "Story mode only. Before GTA Online, come back here and click \"GTA Online mode\"."));
            page.Controls.Add(Spacer(10));
            page.Controls.Add(_welcomeState);
            return page;
        }

        private FlowLayoutPanel BuildGame()
        {
            FlowLayoutPanel page = Page();
            page.Controls.Add(Eyebrow("Step 1 of 4"));
            page.Controls.Add(Title("Where is GTA V?"));
            page.Controls.Add(Theme.Label("The folder that contains GTA5.exe. We already looked in Steam, Epic and the Rockstar Games Launcher.", Theme.Muted, Theme.Regular(10.5f)));
            _folder.BackColor = Theme.Field;
            _folder.ForeColor = Theme.Text;
            _folder.BorderStyle = BorderStyle.FixedSingle;
            _folder.Font = Theme.Regular(10.5f);
            _folder.Margin = new Padding(0, 10, 0, 6);
            _folder.TextChanged += (s, e) => RefreshAll();
            page.Controls.Add(_folder);
            var row = Row();
            var browse = Theme.Secondary("Browse...");
            browse.Click += (s, e) => Browse();
            var detect = Theme.Secondary("Find it for me");
            detect.Click += (s, e) =>
            {
                string found = GameLocator.Find();
                if (found == null) Log("GTA V was not found automatically.");
                else _folder.Text = found;
                RefreshAll();
            };
            row.Controls.Add(browse);
            row.Controls.Add(detect);
            page.Controls.Add(row);
            page.Controls.Add(_gameStatus);
            return page;
        }

        private FlowLayoutPanel BuildHook()
        {
            FlowLayoutPanel page = Page();
            page.Controls.Add(Eyebrow("Step 2 of 4"));
            page.Controls.Add(Title("Get ScriptHookV"));
            page.Controls.Add(Theme.Label("Every GTA V script mod needs ScriptHookV. Its author does not allow programs to download it, so this is the one file you grab yourself. Three clicks:", Theme.Muted, Theme.Regular(10.5f)));

            var open = Theme.Secondary("Open the ScriptHookV page");
            open.Click += (s, e) => Open(ScriptHookVPage);
            page.Controls.Add(Numbered(1, "Open the ScriptHookV page in your browser.", open));
            page.Controls.Add(Numbered(2, null, _hookStep2));
            var pick = Theme.Quiet("Choose the zip myself...");
            pick.Click += (s, e) => PickScriptHookV();
            _hookInstall.Click += (s, e) => InstallScriptHookV(_foundZip);
            page.Controls.Add(Numbered(3, "Come back here. The setup spots the zip in your Downloads folder by itself, then click Install. You can also drag the zip onto this window.", _hookInstall, pick));
            page.Controls.Add(_hookStatus);
            return page;
        }

        private FlowLayoutPanel BuildMods()
        {
            FlowLayoutPanel page = Page();
            page.Controls.Add(Eyebrow("Step 3 of 4"));
            page.Controls.Add(Title("Install the mods"));
            page.Controls.Add(Theme.Label("Downloads the five mods, ScriptHookVDotNet, LemonUI and the APWorld, and puts them in an \"Archipelago\" folder next to GTA5.exe. Your settings are kept when you update.", Theme.Muted, Theme.Regular(10.5f)));
            _install.Margin = new Padding(0, 12, 0, 8);
            _install.Click += async (s, e) => await Install();
            page.Controls.Add(_install);
            _progress.Height = 6;
            _progress.Margin = new Padding(0, 4, 0, 4);
            _progress.Visible = false;
            page.Controls.Add(_progress);
            _progressText.Visible = false;
            page.Controls.Add(_progressText);
            page.Controls.Add(_modsStatus);

            _advancedToggle.Margin = new Padding(0, 2, 0, 2);
            _advancedToggle.Click += (s, e) =>
            {
                _advanced.Visible = !_advanced.Visible;
                _advancedToggle.Text = _advanced.Visible ? "Choose what to install ▴" : "Choose what to install ▾";
                Fit();
            };
            page.Controls.Add(_advancedToggle);
            _components.BackColor = Theme.Field;
            _components.ForeColor = Theme.Text;
            _components.BorderStyle = BorderStyle.None;
            _components.CheckOnClick = true;
            _components.IntegralHeight = false;
            _components.Font = Theme.Regular(9.5f);
            _components.Margin = new Padding(0, 4, 0, 6);
            foreach (Component component in _sources.Components)
                _components.Items.Add(component, component.IsConfigured);
            _components.Height = Math.Max(60, _components.Items.Count * (_components.ItemHeight + 2) + 6);
            _advanced.Controls.Add(_components);
            _keepSettings.Text = "Keep my settings (config.json, vehicles.json, weapons.json)";
            _keepSettings.Checked = true;
            _keepSettings.AutoSize = true;
            _keepSettings.ForeColor = Theme.Text;
            _keepSettings.BackColor = Color.Transparent;
            _advanced.Controls.Add(_keepSettings);
            _advanced.Visible = false;
            page.Controls.Add(_advanced);
            return page;
        }

        private FlowLayoutPanel BuildPlay()
        {
            FlowLayoutPanel page = Page();
            page.Controls.Add(Eyebrow("Step 4 of 4"));
            page.Controls.Add(Title("Turn BattlEye off and play"));
            page.Controls.Add(Theme.Label("BattlEye is the anti-cheat of GTA Online. Mods only run when the game starts without it.", Theme.Muted, Theme.Regular(10.5f)));
            _battlEye.Margin = new Padding(0, 8, 0, 6);
            page.Controls.Add(_battlEye);
            var row = Row();
            _launchSteam.Click += (s, e) => Open("steam://run/271590//-nobattleye/");
            _copyArgument.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText("-nobattleye");
                    _copyArgument.Text = "Copied!";
                }
                catch (Exception ex)
                {
                    Log("Could not copy: " + ex.Message);
                }
            };
            row.Controls.Add(_launchSteam);
            row.Controls.Add(_copyArgument);
            page.Controls.Add(row);
            page.Controls.Add(Spacer(6));
            page.Controls.Add(Subtitle("In the game"));
            page.Controls.Add(Theme.Label("Press F6, open Connection, type the room address, your slot name and the password, then start a New Game from the pause menu. That is it.", Theme.Text));
            page.Controls.Add(_playStatus);
            return page;
        }

        private void Next()
        {
            if (_page == PlayPage)
            {
                Close();
                return;
            }
            GoTo(_page + 1);
        }

        private void GoTo(int page)
        {
            page = Math.Max(WelcomePage, Math.Min(PlayPage, page));
            if (page != WelcomePage && page > GamePage && !GameValid) page = GamePage;
            _page = page;
            _scroll.Value = 0;
            for (int i = 0; i < _pages.Count; i++) _pages[i].Visible = i == page;
            _watch.Enabled = page == HookPage;
            if (page == PlayPage) _copyArgument.Text = "Copy -nobattleye";
            RefreshAll();
            Fit();
        }

        private void Fit()
        {
            if (_page < 0) return;
            int width = Math.Max(S(300), _viewport.ClientSize.Width - _viewport.Padding.Horizontal);
            FlowLayoutPanel page = _pages[_page];
            page.SuspendLayout();
            page.MinimumSize = new Size(width, 0);
            page.MaximumSize = new Size(width, 0);
            FitChildren(page, width);
            page.ResumeLayout(true);
            PlacePage();
        }

        private void PlacePage()
        {
            if (_page < 0) return;
            FlowLayoutPanel page = _pages[_page];
            int view = _viewport.ClientSize.Height;
            _scroll.SetRange(page.Height, view, S(48));
            page.Location = new Point(0, -_scroll.Value);
        }

        private int S(int value) => (int)Math.Round(value * Theme.Scale);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != 0x020A || !_scroll.CanScroll || !Visible || Form.ActiveForm != this) return false;
            Point cursor = Cursor.Position;
            if (!_host.RectangleToScreen(_host.ClientRectangle).Contains(cursor)) return false;
            int delta = (short)(((long)m.WParam >> 16) & 0xFFFF);
            _scroll.Value -= delta * S(56) / 120;
            return true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Application.AddMessageFilter(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            base.OnFormClosed(e);
        }

        private void FitChildren(Control parent, int width)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case Label label:
                        label.MaximumSize = new Size(width, 0);
                        break;
                    case StatusLine status:
                        status.Width = width;
                        break;
                    case TextBox box:
                        box.Width = width;
                        break;
                    case ProgressBar bar:
                        bar.Width = width;
                        break;
                    case CheckedListBox list:
                        list.Width = width;
                        break;
                    case FlowLayoutPanel panel when panel.Tag is string kind && kind == "numbered":
                        FitChildren(panel.Controls[1], width - panel.Controls[0].Width - panel.Controls[0].Margin.Horizontal);
                        break;
                    case FlowLayoutPanel panel when panel.Tag is string kind && kind == "bullet":
                        panel.Controls[1].MaximumSize = new Size(width - panel.Controls[0].Width - panel.Controls[0].Margin.Horizontal, 0);
                        break;
                    case FlowLayoutPanel panel when panel.FlowDirection == FlowDirection.TopDown:
                        FitChildren(panel, width);
                        break;
                    case FlowLayoutPanel panel:
                        panel.MaximumSize = new Size(width, 0);
                        break;
                }
            }
        }

        private void RefreshAll()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshAll));
                return;
            }
            string folder = GameFolder;
            bool valid = GameValid;
            string version = valid ? GameLocator.Version(folder) ?? "unknown" : null;
            Platform platform = valid ? GameLocator.Detect(folder) : Platform.Unknown;
            Installer installer = valid ? CreateInstaller() : null;

            bool gameDone = RefreshGame(folder, valid, version, platform);
            bool hookDone = RefreshHook(valid, version, installer);
            bool modsDone = RefreshMods(valid);
            bool playDone = RefreshPlay(platform, installer, hookDone, modsDone);
            _rail.Set(_page - 1, new[] { gameDone, hookDone, modsDone, playDone });

            _welcomeState.Text = modsDone && hookDone
                ? "Already installed. Click Start to update the mods or check your setup."
                : modsDone ? "The mods are installed. ScriptHookV is still missing." : string.Empty;
            _welcomeState.Visible = _welcomeState.Text.Length > 0;

            bool disabled = valid && installer.ModsDisabled;
            _onlineMode.Text = disabled ? "Story mode: turn the mods back on" : "GTA Online mode";
            _onlineMode.Enabled = valid && !_busy && (disabled || modsDone);

            _back.Visible = _page > WelcomePage;
            _back.Enabled = !_busy;
            switch (_page)
            {
                case WelcomePage:
                    _next.Text = "Start";
                    _next.Enabled = true;
                    break;
                case GamePage:
                    _next.Text = "Next";
                    _next.Enabled = valid && !_busy;
                    break;
                case HookPage:
                    _next.Text = hookDone ? "Next" : "Skip for now";
                    _next.Enabled = !_busy;
                    break;
                case ModsPage:
                    _next.Text = "Next";
                    _next.Enabled = modsDone && !_busy;
                    break;
                default:
                    _next.Text = "Finish";
                    _next.Enabled = !_busy;
                    break;
            }
            _install.Enabled = valid && !_busy;
            _hookInstall.Enabled = valid && !_busy && _foundZip != null;
            Fit();
        }

        private bool RefreshGame(string folder, bool valid, string version, Platform platform)
        {
            if (!valid)
            {
                if (GameLocator.IsEnhanced(folder))
                    _gameStatus.Set(StepState.Error, "This is GTA V Enhanced. The mods need GTA V Legacy: pick the folder of the Legacy edition.");
                else if (folder.Length == 0)
                    _gameStatus.Set(StepState.Error, "GTA V was not found. Click \"Browse...\" and pick the folder that contains GTA5.exe.");
                else
                    _gameStatus.Set(StepState.Error, "There is no GTA5.exe in this folder. Click \"Browse...\" and pick the right one.");
                return false;
            }
            string supported = string.Join(" or ", GameLocator.SupportedVersions);
            if (GameLocator.IsSupported(version))
            {
                _gameStatus.Set(StepState.Done, $"Found GTA V Legacy {version} on {platform}. This version is supported.");
                return true;
            }
            int build = GameLocator.Build(version);
            _gameStatus.Set(StepState.Warning, build > GameLocator.LatestBuild
                ? $"Found GTA V Legacy {version} on {platform}. This game update is newer than the mods ({supported}): check for a newer release before playing."
                : $"Found GTA V Legacy {version} on {platform}. This version is too old: update the game in your launcher to {GameLocator.SupportedVersions[0]}.");
            return true;
        }

        private bool RefreshHook(bool valid, string version, Installer installer)
        {
            int build = GameLocator.Build(version);
            _foundZip = Installer.FindScriptHookVZip();
            IReadOnlyList<int> foundBuilds = Installer.BuildsIn(_foundZip == null ? null : Path.GetFileName(_foundZip));
            bool foundMatches = _foundZip != null && (build == 0 || foundBuilds.Count == 0 || foundBuilds.Contains(build));

            _hookStep2.Text = build > 0
                ? $"On that page, click \"Download\" and save the zip. Take the version made for game version {build} (the file name ends with {build}.0)."
                : "On that page, click \"Download\" and save the zip.";
            _hookInstall.Text = _foundZip == null ? "Install" : "Install " + Path.GetFileName(_foundZip);

            if (!valid)
            {
                _hookStatus.Set(StepState.Pending, "Choose your game folder first (step 1).");
                return false;
            }
            if (installer.ModsDisabled)
            {
                _hookStatus.Set(StepState.Warning, "The mods are turned off for GTA Online. Click \"Story mode: turn the mods back on\" at the bottom left.");
                return false;
            }
            if (!installer.HasScriptHookV)
            {
                _hookStatus.Set(_foundZip != null && foundMatches ? StepState.Pending : StepState.Error, _foundZip == null
                    ? "Waiting for the zip in your Downloads folder..."
                    : foundMatches
                        ? $"Found {Path.GetFileName(_foundZip)} in your Downloads folder. Click Install."
                        : $"{Path.GetFileName(_foundZip)} is not made for game version {build}. Download the new one from the ScriptHookV page.");
                return false;
            }
            string source = installer.ScriptHookVSource;
            IReadOnlyList<int> installedBuilds = Installer.BuildsIn(source);
            if (source != null && build > 0 && installedBuilds.Count > 0 && !installedBuilds.Contains(build))
            {
                _hookStatus.Set(StepState.Warning, $"Installed from {source}, which is not made for game version {build}. Download the new ScriptHookV and install it.");
                return false;
            }
            _hookStatus.Set(StepState.Done, source == null ? "ScriptHookV is installed." : $"ScriptHookV is installed ({source}).");
            return true;
        }

        private bool RefreshMods(bool valid)
        {
            if (_busy) return false;
            if (!valid)
            {
                _modsStatus.Set(StepState.Pending, "Choose your game folder first (step 1).");
                return false;
            }
            bool installed = ModsInstalled;
            _install.Text = installed ? "Update the mods" : "Install the mods";
            _modsStatus.Set(installed ? StepState.Done : StepState.Pending,
                installed ? "The mods are installed. Click \"Update the mods\" any time to get the latest release." : "Not installed yet. Click \"Install the mods\".");
            return installed;
        }

        private bool RefreshPlay(Platform platform, Installer installer, bool hookDone, bool modsDone)
        {
            switch (platform)
            {
                case Platform.Steam:
                    _battlEye.Text = "On Steam, the button below starts GTA V with BattlEye off. You can also add -nobattleye to GTA V Legacy > Properties > Launch Options in Steam, then start it as usual.";
                    break;
                case Platform.Epic:
                    _battlEye.Text = "In the Epic Games Launcher: Library > GTA V Legacy > the three dots > Manage > Launch Options. Turn them on and paste -nobattleye.";
                    break;
                case Platform.Rockstar:
                    _battlEye.Text = "In the Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\".";
                    break;
                default:
                    _battlEye.Text = "Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\". Steam or Epic: add -nobattleye to the launch options.";
                    break;
            }
            _launchSteam.Visible = platform == Platform.Steam;
            _copyArgument.Visible = platform != Platform.Rockstar;
            bool ready = installer != null && hookDone && modsDone && !installer.ModsDisabled;
            if (ready) _playStatus.Set(StepState.Done, "Everything is ready. Have fun, and before GTA Online, come back here and click \"GTA Online mode\".");
            else if (!modsDone) _playStatus.Set(StepState.Error, "The mods are not installed yet (step 3).");
            else _playStatus.Set(StepState.Error, "ScriptHookV is missing (step 2). The game will not load the mods without it.");
            return ready;
        }

        private void Browse()
        {
            using (var dialog = new FolderBrowserDialog { Description = "Select the GTA V Legacy folder (the one with GTA5.exe)." })
            {
                if (Directory.Exists(GameFolder)) dialog.SelectedPath = GameFolder;
                if (dialog.ShowDialog(this) == DialogResult.OK) _folder.Text = dialog.SelectedPath;
            }
        }

        private async Task Install()
        {
            List<Component> selected = _components.CheckedItems.Cast<Component>().ToList();
            if (selected.Count == 0)
            {
                _modsStatus.Set(StepState.Warning, "Nothing is selected in \"Choose what to install\".");
                return;
            }
            if (Process.GetProcessesByName("GTA5").Length > 0)
            {
                _modsStatus.Set(StepState.Warning, "Close GTA V first, then click again.");
                return;
            }
            _busy = true;
            _progress.Value = 0;
            _progress.Visible = true;
            _progressText.Visible = true;
            _modsStatus.Set(StepState.Pending, "Downloading and installing...");
            RefreshAll();
            bool failed = false;
            try
            {
                await CreateInstaller().Install(selected, _keepSettings.Checked,
                    value => BeginInvoke(new Action(() => _progress.Value = Math.Max(0, Math.Min(100, value)))));
            }
            catch (Exception ex)
            {
                failed = true;
                Log("ERROR: " + ex.GetBaseException().Message);
            }
            _busy = false;
            _progress.Visible = false;
            _progressText.Visible = false;
            RefreshAll();
            if (failed)
            {
                _modsStatus.Set(StepState.Error, "The installation failed. Check your internet connection and try again. The details window tells what went wrong.");
                ShowLog();
            }
            else
                _modsStatus.Set(StepState.Done, "Done! The mods are installed. Click Next.");
            Fit();
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
            if (string.IsNullOrEmpty(zip) || !File.Exists(zip) || !GameValid) return;
            if (Process.GetProcessesByName("GTA5").Length > 0)
            {
                _hookStatus.Set(StepState.Warning, "Close GTA V first, then click Install again.");
                return;
            }
            try
            {
                CreateInstaller().InstallScriptHookV(zip);
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                RefreshAll();
                _hookStatus.Set(StepState.Error, ex.Message);
                return;
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
            if (zip == null) return;
            if (_page != HookPage) GoTo(HookPage);
            InstallScriptHookV(zip);
        }

        private static string DroppedZip(DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files)) return null;
            return files.FirstOrDefault(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }

        private void ToggleMods()
        {
            if (!GameValid) return;
            Installer installer = CreateInstaller();
            bool disabled = installer.ModsDisabled;
            if (!disabled)
            {
                DialogResult answer = MessageBox.Show(this,
                    "Turn the mods off to play GTA Online?\n\nAfter this, turn BattlEye back on in your launcher. Come back here and click \"Story mode\" to play Archipelago again.",
                    Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (answer != DialogResult.OK) return;
            }
            try
            {
                if (disabled) installer.EnableMods();
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

        private void OpenModsFolder()
        {
            string path = Path.Combine(GameFolder, Installer.ModsFolder);
            if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
        }

        private void ShowLog()
        {
            if (_logWindow == null || _logWindow.IsDisposed) _logWindow = new LogWindow(_history.ToString());
            _logWindow.Show(this);
            _logWindow.BringToFront();
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
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _history.AppendLine(line);
            if (_busy) _progressText.Text = message;
            if (_logWindow != null && !_logWindow.IsDisposed) _logWindow.Append(line);
        }

        private static FlowLayoutPanel Page() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(0, 0),
            BackColor = Theme.Background,
            Margin = new Padding(0),
        };

        private static FlowLayoutPanel Column() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
        };

        private static FlowLayoutPanel Row() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 6),
        };

        private static Label Eyebrow(string text)
        {
            Label label = Theme.Label(text.ToUpperInvariant(), Theme.Muted, Theme.Mono(8.5f));
            label.Margin = new Padding(0, 6, 0, 2);
            return label;
        }

        private static Label Title(string text)
        {
            Label label = Theme.Heading(text.ToUpperInvariant(), Theme.Text, Theme.Display(26f));
            label.Margin = new Padding(0, 0, 0, 6);
            return label;
        }

        private static Label Subtitle(string text)
        {
            Label label = Theme.Heading(text.ToUpperInvariant(), Theme.Accent, Theme.Display(14f));
            label.Margin = new Padding(0, 6, 0, 4);
            return label;
        }

        private static Control Spacer(int height) => new Panel { Height = height, Width = 1, BackColor = Color.Transparent, Margin = new Padding(0) };

        private static FlowLayoutPanel Bullet(Color color, string text)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 2, 0, 4), Tag = "bullet" };
            var dot = new Panel { Width = 10, Height = 10, Margin = new Padding(0, 6, 12, 0), BackColor = Theme.Background };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(color))
                    e.Graphics.FillEllipse(brush, 0, 0, 9, 9);
            };
            row.Controls.Add(dot);
            var label = Theme.Label(text, Theme.Text, Theme.Regular(10.5f));
            label.MaximumSize = new Size(600, 0);
            row.Controls.Add(label);
            return row;
        }

        private static FlowLayoutPanel Numbered(int number, string text, params Control[] extra)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 6, 0, 6), Tag = "numbered" };
            var badge = new Label
            {
                Text = number.ToString(),
                Font = Theme.Display(18f),
                ForeColor = Theme.Accent,
                AutoSize = false,
                Width = 34,
                Height = 30,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Color.Transparent,
            };
            row.Controls.Add(badge);
            FlowLayoutPanel body = Column();
            if (text != null)
            {
                Label label = Theme.Label(text, Theme.Text, Theme.Regular(10.5f));
                label.Margin = new Padding(0, 4, 0, 4);
                body.Controls.Add(label);
            }
            var buttons = Row();
            foreach (Control control in extra)
            {
                if (control is Label label)
                {
                    label.Font = Theme.Regular(10.5f);
                    label.Margin = new Padding(0, 4, 0, 4);
                    body.Controls.Add(label);
                }
                else buttons.Controls.Add(control);
            }
            if (buttons.Controls.Count > 0) body.Controls.Add(buttons);
            row.Controls.Add(body);
            return row;
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
                g.Clear(Theme.Background);
                int pad = (int)(40 * Theme.Scale);
                int top = (int)(20 * Theme.Scale);
                using (Font brand = Theme.Display(18f))
                using (Font small = Theme.Mono(8.5f))
                {
                    Size first = TextRenderer.MeasureText(g, "GTA V ", brand, Size.Empty, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, "GTA V ", brand, new Point(pad, top), Theme.Text, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, "ARCHIPELAGO", brand, new Point(pad + first.Width, top), Theme.Accent, TextFormatFlags.NoPadding);
                    string right = "SETUP " + _version;
                    Size size = TextRenderer.MeasureText(g, right, small);
                    TextRenderer.DrawText(g, right, small, new Point(Width - pad - size.Width, top + (int)(6 * Theme.Scale)), Theme.Muted);
                }
                using (var pen = new Pen(Theme.Line))
                    g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }

            protected override void OnResize(EventArgs eventargs)
            {
                base.OnResize(eventargs);
                Invalidate();
            }
        }

        private sealed class LogWindow : Form
        {
            private readonly TextBox _text = new TextBox();

            public LogWindow(string history)
            {
                SuspendLayout();
                AutoScaleDimensions = new SizeF(96f, 96f);
                AutoScaleMode = AutoScaleMode.Dpi;
                Text = "Details";
                BackColor = Theme.Background;
                ClientSize = new Size(720, 420);
                StartPosition = FormStartPosition.CenterParent;
                ShowInTaskbar = false;
                _text.Multiline = true;
                _text.ReadOnly = true;
                _text.ScrollBars = ScrollBars.Vertical;
                _text.Dock = DockStyle.Fill;
                _text.BackColor = Theme.Field;
                _text.ForeColor = Theme.Muted;
                _text.BorderStyle = BorderStyle.None;
                _text.Font = Theme.Mono(9f);
                _text.Text = history;
                Controls.Add(_text);
                var copy = Theme.Secondary("Copy everything");
                copy.Dock = DockStyle.Bottom;
                copy.AutoSize = false;
                copy.Height = 40;
                copy.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(_text.Text);
                        copy.Text = "Copied";
                    }
                    catch
                    {
                    }
                };
                Controls.Add(copy);
                ResumeLayout(false);
                PerformLayout();
            }

            public void Append(string line) => _text.AppendText(line + Environment.NewLine);
        }
    }
}
