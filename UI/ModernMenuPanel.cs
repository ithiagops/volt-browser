using System.Drawing.Drawing2D;

namespace Volt;

sealed partial class BrowserWindow
{
    sealed class ModernPillButton : Button
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsActive { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Dark { get; set; }

        public ModernPillButton(string text)
        {
            Text = text;
            AutoSize = true;
            Height = 28;
            Padding = new Padding(8, 2, 8, 2);
            Margin = new Padding(2, 3, 2, 3);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = IsActive
                ? (Dark ? Color.FromArgb(14, 165, 233) : Color.FromArgb(2, 132, 199))
                : (Dark ? Color.FromArgb(45, 56, 73) : Color.FromArgb(235, 240, 246));

            Color textCol = IsActive
                ? Color.White
                : (Dark ? Color.FromArgb(215, 226, 238) : Color.FromArgb(51, 65, 85));

            using var path = CreateRoundedRect(new RectangleF(0, 0, Width - 1, Height - 1), 5);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);

            if (!IsActive)
            {
                using var border = new Pen(Dark ? Color.FromArgb(60, 75, 96) : Color.FromArgb(210, 220, 232), 1);
                g.DrawPath(border, path);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        static GraphicsPath CreateRoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    sealed class ModernActionBtn : Button
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Dark { get; set; }

        public ModernActionBtn(string text)
        {
            Text = text;
            Height = 30;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
            Margin = new Padding(2, 3, 2, 3);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = Dark ? Color.FromArgb(45, 56, 73) : Color.FromArgb(235, 240, 246);
            Color textCol = Dark ? Color.FromArgb(220, 230, 242) : Color.FromArgb(40, 55, 75);

            using var path = CreateRoundedRect(new RectangleF(0, 0, Width - 1, Height - 1), 5);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);

            using var border = new Pen(Dark ? Color.FromArgb(60, 75, 96) : Color.FromArgb(210, 220, 232), 1);
            g.DrawPath(border, path);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        static GraphicsPath CreateRoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    sealed class ModernToolsDropDown : ToolStripDropDown
    {
        public ModernToolsDropDown()
        {
            Padding = Padding.Empty;
            Margin = Padding.Empty;
            DropShadowEnabled = true;
            AutoClose = true;
            DoubleBuffered = true;
        }
    }

    sealed class ModernMenuScrollContainer : Panel
    {
        public ModernMenuScrollContainer()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }

    ModernToolsDropDown? modernToolsPopup;
    Panel? modernMenuPanel;
    readonly List<(ModernPillButton Btn, int Minutes)> modernTimePills = new();
    readonly List<(ModernPillButton Btn, int Count)> modernPriorityPills = new();
    ModernPillButton? modernCustomPriorityPill;

    CheckBox? modernEconomyCheck;
    CheckBox? modernPinCheck;
    CheckBox? modernMediaCheck;
    CheckBox? modernDeepCheck;
    CheckBox? modernAutoMemCheck;
    CheckBox? modernBlockersCheck;
    CheckBox? modernDarkCheck;

    void InitializeModernToolsMenu(Button menuButton)
    {
        modernToolsPopup = new ModernToolsDropDown();
        int panelWidth = (int)(380 * DeviceDpi / 96f);
        int panelHeight = (int)(560 * DeviceDpi / 96f);

        var container = new ModernMenuScrollContainer
        {
            Width = panelWidth,
            Height = panelHeight,
            AutoScroll = true,
            Padding = new Padding(12, 10, 12, 12)
        };
        modernMenuPanel = container;

        // --- HEADER ---
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 46, Margin = Padding.Empty };
        var titleLbl = new Label
        {
            Text = "VOLT • Ferramentas & Desempenho",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 24
        };
        var subTitleLbl = new Label
        {
            Text = "Controle de abas, economia e privacidade",
            Font = new Font("Segoe UI", 8.5f),
            Dock = DockStyle.Top,
            Height = 18
        };
        headerPanel.Controls.Add(subTitleLbl);
        headerPanel.Controls.Add(titleLbl);

        // --- SEÇÃO 1: MODO DE ECONOMIA & DESEMPENHO ---
        var econSection = CreateSectionPanel("⚡ MODO DE ECONOMIA & DESEMPENHO");

        modernEconomyCheck = CreateModernCheckBox("Ativar modo de economia (suspender abas)", economy);
        modernEconomyCheck.CheckedChanged += (_, _) =>
        {
            economy = modernEconomyCheck.Checked;
            if (!economy)
            {
                foreach (var tab in items.Values)
                    if (tab.Ready) tab.View!.CoreWebView2.Resume();
            }
            UpdateChrome();
        };

        var timeLabel = CreateSubtitleLabel("Tempo de inatividade antes de suspender:");
        var timeFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = new Padding(0, 2, 0, 6) };
        modernTimePills.Clear();
        foreach (var (mins, label) in new[]
        {
            (1, "1 min"),
            (3, "3 min"),
            (5, "5 min (Padrão)"),
            (15, "15 min"),
            (30, "30 min"),
            (60, "1 hora"),
            (0, "Nunca")
        })
        {
            var pill = new ModernPillButton(label) { IsActive = mins == sleepMinutes };
            int m = mins;
            pill.Click += (_, _) =>
            {
                SetSleepMinutes(m);
                UpdateModernMenuValues();
            };
            timeFlow.Controls.Add(pill);
            modernTimePills.Add((pill, mins));
        }

        var priorityLabel = CreateSubtitleLabel("Prioridade de abas (manter sempre ativas):");
        var priorityFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = new Padding(0, 2, 0, 6) };
        modernPriorityPills.Clear();
        foreach (var (count, label) in new[]
        {
            (0, "0 (Nenhuma)"),
            (2, "2 abas"),
            (3, "3 abas (Recomendado)"),
            (5, "5 abas"),
            (10, "10 abas")
        })
        {
            var pill = new ModernPillButton(label) { IsActive = count == browsing.PriorityTabs };
            int c = count;
            pill.Click += (_, _) =>
            {
                SetPriorityTabs(c);
                UpdateModernMenuValues();
            };
            priorityFlow.Controls.Add(pill);
            modernPriorityPills.Add((pill, count));
        }

        modernCustomPriorityPill = new ModernPillButton("Personalizar…");
        modernCustomPriorityPill.Click += (_, _) =>
        {
            modernToolsPopup?.Hide();
            using var dialog = new Form
            {
                Text = "Prioridade das abas ativas",
                ClientSize = new(440, 160),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = darkMode ? BrowserTheme.Surface : SystemColors.Control,
                ForeColor = darkMode ? BrowserTheme.Text : SystemColors.ControlText
            };
            var label = new Label
            {
                Text = "Manter as primeiras N abas ativas (da esquerda para a direita):\nElas nunca entrarão em modo de economia.",
                AutoSize = false,
                Bounds = new(16, 16, 408, 44),
                ForeColor = dialog.ForeColor
            };
            var countInput = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = browsing.PriorityTabs,
                Bounds = new(16, 68, 90, 30),
                BackColor = darkMode ? BrowserTheme.Background : Color.White,
                ForeColor = dialog.ForeColor
            };
            var ok = new Button
            {
                Text = "Salvar",
                DialogResult = DialogResult.OK,
                Bounds = new(320, 110, 100, 32),
                BackColor = darkMode ? Color.FromArgb(45, 58, 73) : SystemColors.ButtonFace,
                ForeColor = dialog.ForeColor
            };
            dialog.Controls.AddRange(new Control[] { label, countInput, ok });
            dialog.AcceptButton = ok;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SetPriorityTabs((int)countInput.Value);
            }
        };
        priorityFlow.Controls.Add(modernCustomPriorityPill);

        modernPinCheck = CreateModernCheckBox("Manter esta aba atual ativa", Current?.KeepAwake == true);
        modernPinCheck.CheckedChanged += (_, _) =>
        {
            if (Current is { } tab) tab.KeepAwake = modernPinCheck.Checked;
        };

        modernMediaCheck = CreateModernCheckBox("Pausar mídia ao sair da aba", pauseBackgroundMedia);
        modernMediaCheck.CheckedChanged += async (_, _) =>
        {
            pauseBackgroundMedia = modernMediaCheck.Checked;
            foreach (var tab in items.Values.ToArray())
                if (tab.Ready && tab.MediaInstalled && tab.View is { } web) await InstallMediaGuard(tab, web);
        };

        modernDeepCheck = CreateModernCheckBox("Economia forte (descartar páginas após prazo)", deepEconomy);
        modernDeepCheck.CheckedChanged += (_, _) =>
        {
            if (modernDeepCheck.Checked && !deepEconomy)
            {
                if (MessageBox.Show(this,
                    "Ativar economia forte? Abas elegíveis serão descartadas depois do prazo configurado, economizando ainda mais memória.",
                    "Economia forte", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                {
                    modernDeepCheck.Checked = false;
                    return;
                }
            }
            deepEconomy = modernDeepCheck.Checked;
            UpdateChrome();
        };

        modernAutoMemCheck = CreateModernCheckBox("Liberar memória acima de 1,2 GB", automaticMemory);
        modernAutoMemCheck.CheckedChanged += (_, _) =>
        {
            automaticMemory = modernAutoMemCheck.Checked;
            memoryPressure = false;
        };

        var econActionsFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 6, 0, 4) };
        var btnSuspend = new ModernActionBtn("💤 Suspender inativas") { Width = 140 };
        btnSuspend.Click += async (_, _) =>
        {
            modernToolsPopup?.Hide();
            await SuspendTabs(true);
        };
        var btnUnload = new ModernActionBtn("🧹 Liberar 2º plano") { Width = 140 };
        btnUnload.Click += (_, _) =>
        {
            modernToolsPopup?.Hide();
            UnloadBackgroundTabs();
        };
        var btnDiag = new ModernActionBtn("📊 Diagnóstico") { Width = (int)(panelWidth - 40) };
        btnDiag.Click += (_, _) =>
        {
            modernToolsPopup?.Hide();
            ShowTabDiagnostics();
        };
        econActionsFlow.Controls.Add(btnSuspend);
        econActionsFlow.Controls.Add(btnUnload);
        econActionsFlow.Controls.Add(btnDiag);

        // Add controls in reverse order for DockStyle.Top
        econSection.Controls.Add(econActionsFlow);
        econSection.Controls.Add(modernAutoMemCheck);
        econSection.Controls.Add(modernDeepCheck);
        econSection.Controls.Add(modernMediaCheck);
        econSection.Controls.Add(modernPinCheck);
        econSection.Controls.Add(priorityFlow);
        econSection.Controls.Add(priorityLabel);
        econSection.Controls.Add(timeFlow);
        econSection.Controls.Add(timeLabel);
        econSection.Controls.Add(modernEconomyCheck);

        // --- SEÇÃO 2: NAVEGAÇÃO & PRIVACIDADE ---
        var navSection = CreateSectionPanel("🔒 NAVEGAÇÃO & PRIVACIDADE");
        var btnPass = new ModernActionBtn("🔑 Senhas — importar e gerenciar…") { Dock = DockStyle.Top, Height = 32 };
        btnPass.Click += (_, _) =>
        {
            modernToolsPopup?.Hide();
            ShowPasswords();
        };

        modernBlockersCheck = CreateModernCheckBox("Bloquear rastreadores conhecidos", blockTrackers);
        modernBlockersCheck.CheckedChanged += (_, _) =>
        {
            blockTrackers = modernBlockersCheck.Checked;
            UpdateChrome();
        };

        var btnClearHist = new ModernActionBtn("🗑 Limpar histórico de endereços") { Dock = DockStyle.Top, Height = 32 };
        btnClearHist.Click += (_, _) =>
        {
            browsing.History.Clear();
            SaveBrowsingPreferences();
            RefreshAddressSuggestions();
            status.Text = "Histórico de endereços limpo com sucesso.";
            modernToolsPopup?.Hide();
        };

        navSection.Controls.Add(btnClearHist);
        navSection.Controls.Add(modernBlockersCheck);
        navSection.Controls.Add(btnPass);

        // --- SEÇÃO 3: APARÊNCIA & SISTEMA ---
        var sysSection = CreateSectionPanel("🎨 APARÊNCIA & SISTEMA");
        modernDarkCheck = CreateModernCheckBox("Modo escuro", darkMode);
        modernDarkCheck.CheckedChanged += (_, _) =>
        {
            if (darkMode != modernDarkCheck.Checked)
            {
                darkMode = modernDarkCheck.Checked;
                using (var redraw = new VisualUpdate(this)) BrowserTheme.Apply(this, darkMode);
                addressSuggestions?.ApplyTheme(darkMode);
                ApplyModernMenuTheme(darkMode);
                BeginInvoke(new Action(() =>
                {
                    if (closing) return;
                    var ready = items.Values.FirstOrDefault(t => t.Ready);
                    if (ready is not null) SetWebTheme(ready.View!);
                    else if (whatsAppView?.CoreWebView2 is not null) SetWebTheme(whatsAppView);
                }));
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(themeFile)!);
                    File.WriteAllText(themeFile, darkMode ? "dark" : "light");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        };

        var btnAbout = new ModernActionBtn("ℹ Informações de memória e otimizações") { Dock = DockStyle.Top, Height = 32 };
        btnAbout.Click += (_, _) =>
        {
            modernToolsPopup?.Hide();
            MessageBox.Show(this,
                "VOLT 0.6.7 • Otimização e Desempenho\n\n• Modo de economia: suspende abas inativas após o tempo configurado.\n• Prioridade de abas: protege as primeiras N abas (ex: 3 abas) para que fiquem sempre ativas.\n• Proteção inteligente: abas com áudio em reprodução, downloads ativos ou marcadas como fixas nunca são suspensas.\n• Economia forte: libera memória RAM descartando páginas elegíveis após o prazo.\n• Autocompletar: digite o início do site ou termo para preencher e navegar rapidamente.",
                "VOLT — Desempenho");
        };

        sysSection.Controls.Add(btnAbout);
        sysSection.Controls.Add(modernDarkCheck);

        // --- SEÇÃO 4: ATUALIZAÇÕES DO NAVEGADOR ---
        var updateSection = CreateSectionPanel("🔄 ATUALIZAÇÕES DO NAVEGADOR");
        var updateStatusLbl = new Label
        {
            Text = $"Versão atual: v{UpdateManager.CurrentVersionString}",
            Font = new Font("Segoe UI", 8.5f),
            Dock = DockStyle.Top,
            Height = 22
        };

        var btnUpdate = new ModernActionBtn("🔍 Verificar atualizações agora") { Dock = DockStyle.Top, Height = 32 };
        UpdateInfo? pendingUpdate = null;

        btnUpdate.Click += async (_, _) =>
        {
            if (pendingUpdate != null && pendingUpdate.HasUpdate)
            {
                btnUpdate.Enabled = false;
                btnUpdate.Text = "Baixando atualização... 0%";
                updateStatusLbl.Text = $"Baixando v{pendingUpdate.LatestVersion}...";
                try
                {
                    var progress = new Progress<int>(percent =>
                    {
                        btnUpdate.Text = $"Baixando atualização... {percent}%";
                    });

                    string downloaded = await UpdateManager.DownloadUpdateAsync(pendingUpdate, progress);
                    btnUpdate.Text = "Atualização pronta!";
                    updateStatusLbl.Text = "Pronto para aplicar a nova versão.";

                    if (MessageBox.Show(this,
                        $"A atualização para a versão v{pendingUpdate.LatestVersion} foi baixada com sucesso!\n\nDeseja reiniciar o VOLT agora para aplicar a atualização?",
                        "Atualização do VOLT",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        UpdateManager.ApplyUpdateAndRestart(downloaded);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Falha ao baixar atualização: " + ex.Message, "Erro de Atualização");
                    btnUpdate.Text = "Tentar novamente";
                    btnUpdate.Enabled = true;
                }
                return;
            }

            btnUpdate.Enabled = false;
            btnUpdate.Text = "Verificando releases no GitHub...";
            updateStatusLbl.Text = "Consultando repositório...";

            try
            {
                var info = await UpdateManager.CheckForUpdatesAsync();
                if (info.HasUpdate)
                {
                    pendingUpdate = info;
                    updateStatusLbl.Text = $"🎉 Nova versão disponível: v{info.LatestVersion}!";
                    btnUpdate.Text = $"⬇ Baixar e Atualizar para v{info.LatestVersion}";
                    btnUpdate.Enabled = true;
                }
                else
                {
                    pendingUpdate = null;
                    updateStatusLbl.Text = $"✓ O VOLT está atualizado (v{info.CurrentVersion}).";
                    btnUpdate.Text = "Verificar novamente";
                    btnUpdate.Enabled = true;
                }
            }
            catch
            {
                updateStatusLbl.Text = "Não foi possível checar atualizações.";
                btnUpdate.Text = "Tentar novamente";
                btnUpdate.Enabled = true;
            }
        };

        updateSection.Controls.Add(btnUpdate);
        updateSection.Controls.Add(updateStatusLbl);

        // Add sections in reverse order so header is at top
        container.Controls.Add(updateSection);
        container.Controls.Add(sysSection);
        container.Controls.Add(navSection);
        container.Controls.Add(econSection);
        container.Controls.Add(headerPanel);

        var host = new ToolStripControlHost(container)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = container.Size
        };
        modernToolsPopup.Items.Add(host);

        menuButton.Click += (_, _) => ShowModernToolsMenu(menuButton);
        ApplyModernMenuTheme(darkMode);
    }

    static Panel CreateSectionPanel(string headerText)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8, 8, 8, 8),
            Margin = new Padding(0, 0, 0, 10)
        };
        var lbl = new Label
        {
            Text = headerText,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22
        };
        panel.Controls.Add(lbl);
        return panel;
    }

    static Label CreateSubtitleLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f),
            Dock = DockStyle.Top,
            Height = 20,
            Margin = new Padding(0, 4, 0, 2)
        };
    }

    static CheckBox CreateModernCheckBox(string text, bool isChecked)
    {
        return new CheckBox
        {
            Text = text,
            Checked = isChecked,
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
    }

    void UpdateModernMenuValues()
    {
        if (modernPinCheck != null) modernPinCheck.Checked = Current?.KeepAwake == true;
        if (modernEconomyCheck != null) modernEconomyCheck.Checked = economy;
        if (modernMediaCheck != null) modernMediaCheck.Checked = pauseBackgroundMedia;
        if (modernDeepCheck != null) modernDeepCheck.Checked = deepEconomy;
        if (modernAutoMemCheck != null) modernAutoMemCheck.Checked = automaticMemory;
        if (modernBlockersCheck != null) modernBlockersCheck.Checked = blockTrackers;
        if (modernDarkCheck != null) modernDarkCheck.Checked = darkMode;

        foreach (var (btn, mins) in modernTimePills)
        {
            btn.IsActive = browsing.SleepMinutes == mins;
            btn.Invalidate();
        }

        bool matchedPriority = false;
        foreach (var (btn, count) in modernPriorityPills)
        {
            btn.IsActive = browsing.PriorityTabs == count;
            if (btn.IsActive) matchedPriority = true;
            btn.Invalidate();
        }

        if (modernCustomPriorityPill != null)
        {
            modernCustomPriorityPill.IsActive = !matchedPriority && browsing.PriorityTabs > 0;
            modernCustomPriorityPill.Text = browsing.PriorityTabs > 0 && !matchedPriority
                ? $"Personalizar ({browsing.PriorityTabs})"
                : "Personalizar…";
            modernCustomPriorityPill.Invalidate();
        }
    }

    void ApplyModernMenuTheme(bool isDark)
    {
        if (modernMenuPanel == null) return;

        Color bg = isDark ? Color.FromArgb(28, 35, 46) : Color.FromArgb(248, 250, 252);
        Color cardBg = isDark ? Color.FromArgb(36, 45, 59) : Color.White;
        Color textPrimary = isDark ? Color.FromArgb(240, 244, 250) : Color.FromArgb(30, 41, 59);
        Color textSec = isDark ? Color.FromArgb(150, 165, 185) : Color.FromArgb(100, 116, 139);

        modernMenuPanel.BackColor = bg;
        modernMenuPanel.ForeColor = textPrimary;

        foreach (Control c in modernMenuPanel.Controls)
        {
            if (c is Panel sec)
            {
                sec.BackColor = cardBg;
                foreach (Control sc in sec.Controls)
                {
                    if (sc is Label l)
                    {
                        l.ForeColor = l.Font.Bold ? (isDark ? Color.FromArgb(14, 165, 233) : Color.FromArgb(2, 132, 199)) : textSec;
                    }
                    else if (sc is CheckBox cb)
                    {
                        cb.ForeColor = textPrimary;
                    }
                    else if (sc is FlowLayoutPanel flp)
                    {
                        foreach (Control pc in flp.Controls)
                        {
                            if (pc is ModernPillButton pb)
                            {
                                pb.Dark = isDark;
                                pb.Invalidate();
                            }
                            else if (pc is ModernActionBtn ab)
                            {
                                ab.Dark = isDark;
                                ab.Invalidate();
                            }
                        }
                    }
                    else if (sc is ModernActionBtn ab2)
                    {
                        ab2.Dark = isDark;
                        ab2.Invalidate();
                    }
                }
            }
        }
        modernMenuPanel.Invalidate(true);
    }

    void ShowModernToolsMenu(Button menuButton)
    {
        if (modernToolsPopup == null || modernMenuPanel == null) return;

        UpdateModernMenuValues();
        ApplyModernMenuTheme(darkMode);

        var buttonScreen = menuButton.PointToScreen(Point.Empty);
        var windowRect = RectangleToScreen(ClientRectangle);
        var menuSize = modernMenuPanel.Size;

        // Position menu inside window: align right edge of menu to right edge of button
        int targetX = buttonScreen.X + menuButton.Width - menuSize.Width;
        int targetY = buttonScreen.Y + menuButton.Height + 2;

        // Strictly clamp entirely within window bounds so no part ever spills outside
        targetX = Math.Clamp(targetX, windowRect.Left + 6, Math.Max(windowRect.Left + 6, windowRect.Right - menuSize.Width - 6));
        if (targetY + menuSize.Height > windowRect.Bottom - 6)
        {
            targetY = Math.Max(windowRect.Top + 6, buttonScreen.Y - menuSize.Height - 2);
        }

        modernToolsPopup.Show(targetX, targetY);
    }
}
