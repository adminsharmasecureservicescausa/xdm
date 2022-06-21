﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using Application = Gtk.Application;
using IoPath = System.IO.Path;
using XDM.Core.Lib.Common;
using Translations;
using UI = Gtk.Builder.ObjectAttribute;
using XDM.GtkUI.Utils;
using XDMApp;
using XDM.Core.Lib.Util;
using TraceLog;

namespace XDM.GtkUI.Dialogs.Settings
{
    internal class SettingsDialog : Dialog
    {
        //private int[] minVidSize = new int[] { 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 };
        private WindowGroup group;
        [UI]
        private Label TabHeader1, TabHeader2, TabHeader3, TabHeader4, TabHeader5;
        [UI]
        private Label Label1, Label2, Label3, Label4, Label5, Label6, Label7,
            Label8, Label9, Label10, Label11, Label12, Label13, Label14, Label15,
            Label16, Label17, Label18, Label19, Label20, Label21,
            Label22, Label23, Label24, Label25, Label26, Label27,
            Label28, Label29, Label30;
        [UI]
        private LinkButton VideoWikiLink;
        [UI]
        Button BtnChrome, BtnFirefox, BtnEdge, BtnOpera, BtnDefault1, BtnDefault2,
            BtnDefault3, CatAdd, CatEdit, CatDel, CatDef, AddPass, EditPass, DelPass, BtnUserAgentReset,
            BtnCopy1, BtnCopy2;
        [UI]
        private CheckButton ChkMonitorClipboard, ChkTimestamp, ChkDarkTheme, ChkAutoCat, ChkShowPrg,
            ChkShowComplete, ChkStartAuto, ChkOverwrite, ChkEnableSpeedLimit, ChkHalt, ChkKeepAwake,
            ChkRunCmd, ChkRunAntivirus, ChkAutoRun;
        [UI]
        private ComboBox CmbMinVidSize, CmbDblClickAction, CmbMaxParallalDownloads,
            CmbTimeOut, CmbMaxSegments, CmbMaxRetry, CmbProxyType;
        [UI]
        private Entry TxtChromeWebStoreUrl, TxtFirefoxAMOUrl, TxtTempFolder, TxtDownloadFolder,
            TxtMaxSpeedLimit, TxtProxyHost, TxtProxyPort, TxtProxyUser, TxtProxyPassword,
            TxtCustomCmd, TxtAntiVirusCmd, TxtAntiVirusArgs, TxtDefaultUserAgent;
        [UI]
        private TextView TxtExceptions, TxtDefaultVideoFormats, TxtDefaultFileTypes;
        [UI]
        private TreeView LvCategories, LvPasswords;

        private ListStore categoryStore, passwordStore;
        private IApp app;

        private SettingsDialog(Builder builder,
            Window parent,
            WindowGroup group,
            IAppUI ui,
            IApp app) : base(builder.GetRawOwnedObject("dialog"))
        {
            builder.Autoconnect(this);

            Modal = true;
            SetDefaultSize(640, 480);
            SetPosition(WindowPosition.CenterAlways);
            TransientFor = parent;
            this.group = group;
            this.group.AddWindow(this);
            this.app = app;
            GtkHelper.AttachSafeDispose(this);
            LoadTexts();
            Title = TextResource.GetText("TITLE_SETTINGS");
            GtkHelper.PopulateComboBoxGeneric<int>(CmbMinVidSize, new int[] { 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 });
            GtkHelper.PopulateComboBoxGeneric<int>(CmbMaxParallalDownloads, Enumerable.Range(1, 50).ToArray());
            GtkHelper.PopulateComboBox(CmbDblClickAction, TextResource.GetText("CTX_OPEN_FOLDER"), TextResource.GetText("MSG_OPEN_FILE"));
            CreateCategoryListView();

            GtkHelper.PopulateComboBoxGeneric<int>(CmbTimeOut, Enumerable.Range(1, 300).ToArray());
            GtkHelper.PopulateComboBoxGeneric<int>(CmbMaxSegments, Enumerable.Range(1, 64).ToArray());
            GtkHelper.PopulateComboBoxGeneric<int>(CmbMaxRetry, Enumerable.Range(1, 100).ToArray());
            GtkHelper.PopulateComboBox(CmbProxyType, TextResource.GetText("NET_SYSTEM_PROXY"),
                TextResource.GetText("ND_NO_PROXY"), TextResource.GetText("ND_MANUAL_PROXY"));

            CreatePasswordManagerListView();

            VideoWikiLink.Clicked += VideoWikiLink_Clicked;
            BtnChrome.Clicked += BtnChrome_Clicked;
            BtnFirefox.Clicked += BtnFirefox_Clicked;
            BtnEdge.Clicked += BtnEdge_Clicked;
            BtnOpera.Clicked += BtnOpera_Clicked;
            BtnCopy1.Clicked += BtnCopy1_Clicked;
            BtnCopy2.Clicked += BtnCopy2_Clicked;
            BtnDefault1.Clicked += BtnDefault1_Clicked;
            BtnDefault2.Clicked += BtnDefault2_Clicked;
            BtnDefault3.Clicked += BtnDefault3_Clicked;

            BtnCopy1.Image = new Image(GtkHelper.LoadSvg("file-copy-line"));
            BtnCopy2.Image = new Image(GtkHelper.LoadSvg("file-copy-line"));
        }

        private void BtnDefault3_Clicked(object? sender, EventArgs e)
        {
            TxtExceptions.Buffer.Text = string.Join(",", Config.DefaultBlockedHosts);
        }

        private void BtnDefault2_Clicked(object? sender, EventArgs e)
        {
            TxtDefaultVideoFormats.Buffer.Text = string.Join(",", Config.DefaultVideoExtensions);
        }

        private void BtnDefault1_Clicked(object? sender, EventArgs e)
        {
            TxtDefaultFileTypes.Buffer.Text = string.Join(",", Config.DefaultFileExtensions);
        }

        private void BtnCopy2_Clicked(object? sender, EventArgs e)
        {
            var cb = Clipboard.Get(Gdk.Selection.Clipboard);
            if (cb != null)
            {
                cb.Text = TxtFirefoxAMOUrl.Text;
            }
        }

        private void BtnCopy1_Clicked(object? sender, EventArgs e)
        {
            var cb = Clipboard.Get(Gdk.Selection.Clipboard);
            if (cb != null)
            {
                cb.Text = TxtChromeWebStoreUrl.Text;
            }
        }

        private void BtnOpera_Clicked(object? sender, EventArgs e)
        {
            try
            {
                Helpers.InstallNativeMessagingHost(NativeHostBrowser.Chrome);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error installing native host");
                GtkHelper.ShowMessageBox(this, TextResource.GetText("MSG_NATIVE_HOST_FAILED"));
                return;
            }

            try
            {
                BrowserLauncher.LaunchOperaBrowser(this.app.ChromeExtensionUrl);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error launching Opera");
                GtkHelper.ShowMessageBox(this, $"{TextResource.GetText("MSG_BROWSER_LAUNCH_FAILED")} Opera");
            }
        }

        private void BtnEdge_Clicked(object? sender, EventArgs e)
        {
            try
            {
                Helpers.InstallNativeMessagingHost(NativeHostBrowser.Chrome);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error installing native host");
                GtkHelper.ShowMessageBox(this, TextResource.GetText("MSG_NATIVE_HOST_FAILED"));
                return;
            }

            try
            {
                BrowserLauncher.LaunchMicrosoftEdge(this.app.ChromeExtensionUrl);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error Microsoft Edge");
                GtkHelper.ShowMessageBox(this, $"{TextResource.GetText("MSG_BROWSER_LAUNCH_FAILED")} Microsoft Edge");
            }
        }

        private void BtnFirefox_Clicked(object? sender, EventArgs e)
        {
            try
            {
                Helpers.InstallNativeMessagingHost(NativeHostBrowser.Firefox);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error installing native host");
                GtkHelper.ShowMessageBox(this, TextResource.GetText("MSG_NATIVE_HOST_FAILED"));
                return;
            }

            try
            {
                BrowserLauncher.LaunchFirefox(this.app.FirefoxExtensionUrl);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error launching Firefox");
                GtkHelper.ShowMessageBox(this, $"{TextResource.GetText("MSG_BROWSER_LAUNCH_FAILED")} Firefox");
            }
        }

        private void BtnChrome_Clicked(object? sender, EventArgs e)
        {
            try
            {
                Helpers.InstallNativeMessagingHost(NativeHostBrowser.Chrome);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error installing native host");
                GtkHelper.ShowMessageBox(this, TextResource.GetText("MSG_NATIVE_HOST_FAILED"));
                return;
            }

            try
            {
                BrowserLauncher.LaunchGoogleChrome(this.app.ChromeExtensionUrl);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error launching Google Chrome");
                GtkHelper.ShowMessageBox(this, $"{TextResource.GetText("MSG_BROWSER_LAUNCH_FAILED")} Google Chrome");
            }
        }

        private void VideoWikiLink_Clicked(object? sender, EventArgs e)
        {
            Helpers.OpenBrowser("https://subhra74.github.io/xdm/redirect-support.html?path=video");
        }

        private void LoadTexts()
        {
            TabHeader1.Text = TextResource.GetText("SETTINGS_MONITORING");
            TabHeader2.Text = TextResource.GetText("SETTINGS_GENERAL");
            TabHeader3.Text = TextResource.GetText("SETTINGS_NETWORK");
            TabHeader4.Text = TextResource.GetText("SETTINGS_CRED");
            TabHeader5.Text = TextResource.GetText("SETTINGS_ADV");

            Label1.StyleContext.AddClass("medium-font");

            Label1.Text = TextResource.GetText("SETTINGS_MONITORING");
            Label2.Text = TextResource.GetText("DESC_MONITORING_1");
            Label3.Text = TextResource.GetText("MSG_VID_WIKI_TEXT");
            VideoWikiLink.Label = TextResource.GetText("MSG_VID_WIKI_LINK");

            Label4.Text = TextResource.GetText("DESC_OTHER_BROWSERS");
            Label5.Text = TextResource.GetText("DESC_CHROME");
            Label6.Text = TextResource.GetText("DESC_MOZ");

            //BtnChrome.Label = TextResource.GetText("MSG_VID_WIKI_LINK");
            //BtnFirefox.Label = TextResource.GetText("MSG_VID_WIKI_LINK");
            //BtnEdge.Label = TextResource.GetText("MSG_VID_WIKI_LINK");
            //BtnOpera.Label = TextResource.GetText("MSG_VID_WIKI_LINK");

            Label7.Text = TextResource.GetText("DESC_FILETYPES");
            Label8.Text = TextResource.GetText("DESC_VIDEOTYPES");
            Label9.Text = TextResource.GetText("DESC_SITEEXCEPTIONS");
            Label10.Text = TextResource.GetText("LBL_MIN_VIDEO_SIZE");

            BtnDefault1.Label = BtnDefault2.Label = BtnDefault3.Label = TextResource.GetText("DESC_DEF");

            ChkMonitorClipboard.Label = TextResource.GetText("MENU_CLIP_ADD");
            ChkTimestamp.Label = TextResource.GetText("LBL_GET_TIMESTAMP");

            Label11.StyleContext.AddClass("medium-font");

            Label11.Text = TextResource.GetText("SETTINGS_GENERAL");
            Label12.Text = TextResource.GetText("MSG_DOUBLE_CLICK_ACTION");
            Label13.Text = TextResource.GetText("LBL_TEMP_FOLDER");
            Label14.Text = TextResource.GetText("SETTINGS_FOLDER");
            Label15.Text = TextResource.GetText("MSG_MAX_DOWNLOAD");
            Label16.Text = TextResource.GetText("SETTINGS_CAT");

            ChkDarkTheme.Label = TextResource.GetText("SETTINGS_DARK_THEME");
            ChkAutoCat.Label = TextResource.GetText("SETTINGS_ATUO_CAT");
            ChkShowPrg.Label = TextResource.GetText("SHOW_DWN_PRG");
            ChkShowComplete.Label = TextResource.GetText("SHOW_DWN_COMPLETE");
            ChkStartAuto.Label = TextResource.GetText("LBL_START_AUTO");
            ChkOverwrite.Label = TextResource.GetText("LBL_OVERWRITE_EXISTING");
            CatAdd.Label = TextResource.GetText("SETTINGS_CAT_ADD");
            CatEdit.Label = TextResource.GetText("SETTINGS_CAT_EDIT");
            CatDel.Label = TextResource.GetText("DESC_DEL");
            CatDef.Label = TextResource.GetText("DESC_DEF");

            Label17.StyleContext.AddClass("medium-font");

            Label17.Text = TextResource.GetText("SETTINGS_NETWORK");
            Label18.Text = TextResource.GetText("DESC_NET1");
            Label19.Text = TextResource.GetText("DESC_NET2");
            Label20.Text = TextResource.GetText("NET_MAX_RETRY");
            Label21.Text = TextResource.GetText("DESC_NET4");
            Label22.Text = TextResource.GetText("PROXY_HOST");
            Label23.Text = TextResource.GetText("PROXY_PORT");
            Label24.Text = TextResource.GetText("DESC_NET7");
            Label25.Text = TextResource.GetText("DESC_NET8");

            ChkEnableSpeedLimit.Label = TextResource.GetText("MSG_SPEED_LIMIT");

            Label26.Text = TextResource.GetText("SETTINGS_CRED");
            Label26.StyleContext.AddClass("medium-font");

            AddPass.Label = TextResource.GetText("SETTINGS_CAT_ADD");
            EditPass.Label = TextResource.GetText("SETTINGS_CAT_EDIT");
            DelPass.Label = TextResource.GetText("DESC_DEL");

            Label27.Text = TextResource.GetText("SETTINGS_ADV");
            Label27.StyleContext.AddClass("medium-font");

            ChkHalt.Label = TextResource.GetText("MSG_HALT");
            ChkKeepAwake.Label = TextResource.GetText("MSG_AWAKE");
            ChkRunCmd.Label = TextResource.GetText("EXEC_CMD");
            ChkRunAntivirus.Label = TextResource.GetText("EXE_ANTI_VIR");
            ChkAutoRun.Label = TextResource.GetText("AUTO_START");
            BtnUserAgentReset.Label = TextResource.GetText("DESC_DEF");

            Label28.Text = TextResource.GetText("ANTIVIR_CMD");
            Label29.Text = TextResource.GetText("ANTIVIR_ARGS");
            Label30.Text = TextResource.GetText("MSG_FALLBACK_UA");
        }

        public void LoadConfig()
        {
            //Browser monitoring
            TxtChromeWebStoreUrl.Text = Config.ChromeWebstoreUrl;
            TxtFirefoxAMOUrl.Text = Config.FirefoxAMOUrl;
            TxtDefaultFileTypes.Buffer.Text = string.Join(",", Config.Instance.FileExtensions);
            TxtDefaultVideoFormats.Buffer.Text = string.Join(",", Config.Instance.VideoExtensions);
            TxtExceptions.Buffer.Text = string.Join(",", Config.Instance.BlockedHosts);
            GtkHelper.SetSelectedComboBoxValue<int>(CmbMinVidSize, Config.Instance.MinVideoSize);
            ChkMonitorClipboard.Active = Config.Instance.MonitorClipboard;
            ChkTimestamp.Active = Config.Instance.FetchServerTimeStamp;

            //General settings
            ChkShowPrg.Active = Config.Instance.ShowProgressWindow;
            ChkShowComplete.Active = Config.Instance.ShowDownloadCompleteWindow;
            ChkStartAuto.Active = Config.Instance.StartDownloadAutomatically;
            ChkOverwrite.Active = Config.Instance.FileConflictResolution == FileConflictResolution.Overwrite;
            ChkDarkTheme.Active = Config.Instance.AllowSystemDarkTheme;
            TxtTempFolder.Text = Config.Instance.TempDir;
            GtkHelper.SetSelectedComboBoxValue<int>(CmbMaxParallalDownloads, Config.Instance.MaxParallelDownloads);
            //CmbMaxParallalDownloads.SelectedItem = Config.Instance.MaxParallelDownloads;
            ChkAutoCat.Active = Config.Instance.FolderSelectionMode == FolderSelectionMode.Auto;
            TxtDownloadFolder.Text = Config.Instance.DefaultDownloadFolder;
            CmbDblClickAction.Active = Config.Instance.DoubleClickOpenFile ? 1 : 0;

            foreach (var cat in Config.Instance.Categories)
            {
                categoryStore.AppendValues(cat.DisplayName, string.Join(",", cat.FileExtensions), cat.DefaultFolder, cat);
            }
            //LvCategories.ItemsSource = categories;

            //Network settings
            GtkHelper.SetSelectedComboBoxValue<int>(CmbTimeOut, Config.Instance.NetworkTimeout);
            GtkHelper.SetSelectedComboBoxValue<int>(CmbMaxSegments, Config.Instance.MaxSegments);
            GtkHelper.SetSelectedComboBoxValue<int>(CmbMaxRetry, Config.Instance.MaxRetry);
            TxtMaxSpeedLimit.Text = Config.Instance.DefaltDownloadSpeed.ToString();
            ChkEnableSpeedLimit.Active = Config.Instance.EnableSpeedLimit;
            CmbProxyType.Active = (int)(Config.Instance.Proxy?.ProxyType ?? ProxyType.System);
            TxtProxyHost.Text = Config.Instance.Proxy?.Host;
            TxtProxyPort.Text = (Config.Instance.Proxy?.Port ?? 0).ToString();
            TxtProxyUser.Text = Config.Instance.Proxy?.UserName;
            TxtProxyPassword.Text = Config.Instance.Proxy?.Password;

            //Password manager
            foreach (var password in Config.Instance.UserCredentials)
            {
                passwordStore.AppendValues(password.Host, password.User, password);
            }

            //Advanced settings
            ChkHalt.Active = Config.Instance.ShutdownAfterAllFinished;
            ChkKeepAwake.Active = Config.Instance.KeepPCAwake;
            ChkRunCmd.Active = Config.Instance.RunCommandAfterCompletion;
            ChkRunAntivirus.Active = Config.Instance.ScanWithAntiVirus;
            ChkAutoRun.Active = Helpers.IsAutoStartEnabled();

            TxtCustomCmd.Text = Config.Instance.AfterCompletionCommand;
            TxtAntiVirusCmd.Text = Config.Instance.AntiVirusExecutable;
            TxtAntiVirusArgs.Text = Config.Instance.AntiVirusArgs;
            TxtDefaultUserAgent.Text = Config.Instance.FallbackUserAgent;
        }

        private void CreateCategoryListView()
        {
            categoryStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(Category));
            LvCategories.Model = categoryStore;

            var k = 0;
            foreach (var key in new string[] { "SETTINGS_CAT_NAME", "SETTINGS_CAT_TYPES", "SETTINGS_CAT_FOLDER" })
            {
                var cellRendererText = new CellRendererText();
                var treeViewColumn = new TreeViewColumn(TextResource.GetText(key), cellRendererText, "text", k++)
                {
                    Resizable = true,
                    Reorderable = false,
                    Sizing = TreeViewColumnSizing.Fixed,
                    FixedWidth = 150
                };
                LvCategories.AppendColumn(treeViewColumn);
            }
        }

        private void CreatePasswordManagerListView()
        {
            passwordStore = new ListStore(typeof(string), typeof(string), typeof(PasswordEntry));
            LvPasswords.Model = passwordStore;

            var k = 0;
            foreach (var key in new string[] { "DESC_HOST", "DESC_USER" })
            {
                var cellRendererText = new CellRendererText();
                var treeViewColumn = new TreeViewColumn(TextResource.GetText(key), cellRendererText, "text", k++)
                {
                    Resizable = true,
                    Reorderable = false,
                    Sizing = TreeViewColumnSizing.Fixed,
                    FixedWidth = 150
                };
                LvPasswords.AppendColumn(treeViewColumn);
            }
        }

        public static SettingsDialog CreateFromGladeFile(Window parent, WindowGroup group, IAppUI ui, IApp app)
        {
            var builder = new Builder();
            builder.AddFromFile(IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "glade", "settings-dialog.glade"));
            return new SettingsDialog(builder, parent, group, ui, app);
        }
    }
}