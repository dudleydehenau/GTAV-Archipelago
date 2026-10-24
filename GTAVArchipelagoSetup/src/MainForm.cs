using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVArchipelagoSetup
{
    internal sealed class MainForm : Form
    {
        private const string ScriptHookVPage = "http://www.dev-c.com/gtav/scripthookv/";
        private const string GuidePage = "https://archipelago.gg/tutorial/";

        private readonly TextBox _folder = new TextBox();
        private readonly Label _gameInfo = new Label();
        private readonly CheckedListBox _components = new CheckedListBox();
        private readonly CheckBox _keepSettings = new CheckBox();
        private readonly Button _install = new Button();
        private readonly Button _scriptHook = new Button();
        private readonly Button _toggleMods = new Button();
        private readonly Button _uninstall = new Button();
        private readonly Button _openFolder = new Button();
        private readonly Label _battlEye = new Label();
        private readonly Button _copyArgument = new Button();
        private readonly Button _launchSteam = new Button();
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly TextBox _log = new TextBox();
        private readonly SourceList _sources;
        private bool _busy;

        public MainForm()
        {
            Text = "GTA V Archipelago Setup";
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(760, 680);
            MinimumSize = new Size(700, 600);
            StartPosition = FormStartPosition.CenterScreen;
            _sources = SourceList.Load(out string origin);
            BuildLayout();
            Log("Download list: " + origin);
            string found = GameLocator.Find();
            if (found != null) _folder.Text = found;
            RefreshGame();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            Controls.Add(root);

            var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true };
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderRow.Controls.Add(new Label { Text = "GTA V folder:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 0) }, 0, 0);
            _folder.Dock = DockStyle.Fill;
            _folder.TextChanged += (s, e) => RefreshGame();
            folderRow.Controls.Add(_folder, 1, 0);
            var browse = new Button { Text = "Browse...", AutoSize = true };
            browse.Click += (s, e) => Browse();
            folderRow.Controls.Add(browse, 2, 0);
            var detect = new Button { Text = "Detect", AutoSize = true };
            detect.Click += (s, e) =>
            {
                string found = GameLocator.Find();
                if (found == null) Log("GTA V was not found automatically. Use Browse.");
                else _folder.Text = found;
            };
            folderRow.Controls.Add(detect, 3, 0);
            root.Controls.Add(folderRow);

            _gameInfo.AutoSize = true;
            _gameInfo.Margin = new Padding(3, 6, 3, 6);
            root.Controls.Add(_gameInfo);

            _components.Dock = DockStyle.Fill;
            _components.CheckOnClick = true;
            _components.IntegralHeight = false;
            foreach (Component component in _sources.Components)
                _components.Items.Add(component, component.IsConfigured);
            root.Controls.Add(_components);

            var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            _keepSettings.Text = "Keep my settings (config.json, vehicles.json, weapons.json)";
            _keepSettings.Checked = true;
            _keepSettings.AutoSize = true;
            options.Controls.Add(_keepSettings);
            root.Controls.Add(options);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            _install.Text = "Install / Update";
            _install.AutoSize = true;
            _install.Font = new Font(Font, FontStyle.Bold);
            _install.Click += async (s, e) => await Install();
            _scriptHook.Text = "ScriptHookV...";
            _scriptHook.AutoSize = true;
            _scriptHook.Click += (s, e) => ScriptHookV();
            _toggleMods.AutoSize = true;
            _toggleMods.Click += (s, e) => ToggleMods();
            _uninstall.Text = "Uninstall";
            _uninstall.AutoSize = true;
            _uninstall.Click += (s, e) => Uninstall();
            _openFolder.Text = "Open the mods folder";
            _openFolder.AutoSize = true;
            _openFolder.Click += (s, e) =>
            {
                string path = Path.Combine(_folder.Text, Installer.ModsFolder);
                if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
            };
            buttons.Controls.AddRange(new Control[] { _install, _scriptHook, _toggleMods, _uninstall, _openFolder });
            root.Controls.Add(buttons);

            var battlEye = new GroupBox { Text = "BattlEye and GTA Online", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8) };
            var battlEyeLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            _battlEye.AutoSize = true;
            _battlEye.MaximumSize = new Size(700, 0);
            battlEyeLayout.Controls.Add(_battlEye);
            var battlEyeButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            _copyArgument.Text = "Copy -nobattleye";
            _copyArgument.AutoSize = true;
            _copyArgument.Click += (s, e) =>
            {
                Clipboard.SetText("-nobattleye");
                Log("\"-nobattleye\" copied to the clipboard.");
            };
            _launchSteam.Text = "Play with Steam (BattlEye off)";
            _launchSteam.AutoSize = true;
            _launchSteam.Click += (s, e) => Open("steam://run/271590//-nobattleye/");
            var guide = new Button { Text = "Archipelago guides", AutoSize = true };
            guide.Click += (s, e) => Open(GuidePage);
            battlEyeButtons.Controls.AddRange(new Control[] { _copyArgument, _launchSteam, guide });
            battlEyeLayout.Controls.Add(battlEyeButtons);
            battlEye.Controls.Add(battlEyeLayout);
            root.Controls.Add(battlEye);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
            bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _progress.Dock = DockStyle.Fill;
            _progress.Height = 18;
            bottom.Controls.Add(_progress);
            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = SystemColors.Window;
            bottom.Controls.Add(_log);
            root.Controls.Add(bottom);
        }

        private Installer CreateInstaller() => new Installer(_folder.Text.Trim(), Log);

        private void RefreshGame()
        {
            string folder = _folder.Text.Trim();
            bool valid = GameLocator.IsGameFolder(folder);
            if (!valid)
            {
                _gameInfo.Text = GameLocator.IsEnhanced(folder)
                    ? "This is GTA V Enhanced. The mods need GTA V Legacy (GTA5.exe)."
                    : "Select the folder that contains GTA5.exe (GTA V Legacy).";
                _gameInfo.ForeColor = Color.Firebrick;
            }
            else
            {
                string version = GameLocator.Version(folder) ?? "unknown";
                Platform platform = GameLocator.Detect(folder);
                Installer installer = CreateInstaller();
                var parts = new List<string> { $"GTA V Legacy {version} ({platform})" };
                if (version != GameLocator.ExpectedVersion) parts.Add($"the mods are made for {GameLocator.ExpectedVersion}");
                parts.Add(installer.HasScriptHookV || installer.ModsDisabled ? "ScriptHookV found" : "ScriptHookV missing");
                parts.Add(Directory.Exists(installer.ModsPath) ? "mods installed" : "mods not installed");
                _gameInfo.Text = string.Join("  |  ", parts);
                _gameInfo.ForeColor = version == GameLocator.ExpectedVersion && (installer.HasScriptHookV || installer.ModsDisabled) ? Color.DarkGreen : Color.DarkOrange;
                _battlEye.Text = BattlEyeText(platform);
                _launchSteam.Visible = platform == Platform.Steam;
            }
            if (!valid) _battlEye.Text = BattlEyeText(Platform.Unknown);
            UpdateButtons(valid);
        }

        private static string BattlEyeText(Platform platform)
        {
            string how;
            switch (platform)
            {
                case Platform.Steam:
                    how = "Steam: right-click GTA V Legacy > Properties > Launch Options, type -nobattleye (or use the Play button below).";
                    break;
                case Platform.Epic:
                    how = "Epic Games: Library > GTA V > Manage > Launch Options, type -nobattleye.";
                    break;
                case Platform.Rockstar:
                    how = "Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\".";
                    break;
                default:
                    how = "Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck \"Enable BattlEye\". Steam or Epic: add -nobattleye to the launch options.";
                    break;
            }
            return "The mods only work in story mode with BattlEye turned off. " + how + Environment.NewLine +
                   "With BattlEye off, GTA Online refuses to start, and the Archipelago client closes the game if an online session ever begins. " +
                   "To play GTA Online again: click \"Disable mods\", then turn BattlEye back on.";
        }

        private void UpdateButtons(bool valid)
        {
            bool idle = !_busy;
            _install.Enabled = valid && idle;
            _scriptHook.Enabled = valid && idle;
            _uninstall.Enabled = valid && idle && Directory.Exists(Path.Combine(_folder.Text.Trim(), Installer.ModsFolder));
            _openFolder.Enabled = valid && Directory.Exists(Path.Combine(_folder.Text.Trim(), Installer.ModsFolder));
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
            UpdateButtons(true);
            try
            {
                Installer installer = CreateInstaller();
                await installer.Install(selected, _keepSettings.Checked, value => Invoke(new Action(() => _progress.Value = Math.Max(0, Math.Min(100, value)))));
                if (!installer.HasScriptHookV && !installer.ModsDisabled)
                {
                    if (MessageBox.Show(this, "ScriptHookV is not installed. It cannot be downloaded automatically.\n\nOpen the ScriptHookV page now? Download the zip, then click \"ScriptHookV...\" and select it.",
                            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Open(ScriptHookVPage);
                }
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.GetBaseException().Message);
            }
            finally
            {
                _busy = false;
                RefreshGame();
            }
        }

        private void ScriptHookV()
        {
            using (var dialog = new OpenFileDialog { Filter = "ScriptHookV zip|*.zip", Title = "Select the ScriptHookV zip downloaded from dev-c.com" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    if (MessageBox.Show(this, "Open the ScriptHookV download page?", Text, MessageBoxButtons.YesNo) == DialogResult.Yes) Open(ScriptHookVPage);
                    return;
                }
                try
                {
                    CreateInstaller().InstallScriptHookV(dialog.FileName);
                }
                catch (Exception ex)
                {
                    Log("ERROR: " + ex.Message);
                }
            }
            RefreshGame();
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
            RefreshGame();
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
            RefreshGame();
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
    }
}
