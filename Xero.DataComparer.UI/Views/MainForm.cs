namespace Xero.DataComparer.UI;

/// <summary>
/// Comparison screen after the ComparisonScreen of Xatis: toolbar, title, indicators per kind of gap (a click filters the grid),
/// the gaps (<see cref="ResultGrid"/>), the details of the current row and the timings of the parallel steps of the comparator.
/// The form lays the parts out and connects them; each part draws and handles its own content.
/// </summary>
sealed class MainForm : Form
{
    const string Hint = "Click an indicator to show only that kind of gap. Click a column header to sort (gaps by magnitude). "
        + "The relevance threshold applies at once, without comparing again. F5 compares again.";

    readonly ComparisonToolbar _toolbar = new();
    readonly Label _title = new() { Text = "Xero.DataComparer — list comparison", Font = Theme.Title, AutoSize = false, Dock = DockStyle.Fill };
    readonly Label _description = new() { AutoSize = false, Dock = DockStyle.Fill, AutoEllipsis = true };
    readonly IndicatorPanel _indicators = new();
    readonly FilterBar _filterBar = new();
    readonly ResultGrid _grid = new();
    readonly DetailsGrid _details = new();
    readonly GanttChart _gantt = new() { Dock = DockStyle.Fill };
    readonly SplitContainer _split = new();
    readonly Panel _detailsBox = new() { Dock = DockStyle.Fill, Padding = new Padding(Metrics.Border), Margin = new Padding(0, 0, Metrics.PanelGap, 0) };
    readonly Panel _ganttBox = new() { Dock = DockStyle.Fill, Padding = new Padding(Metrics.Border), Margin = Padding.Empty };
    readonly TableLayoutPanel _bottom = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = Padding.Empty };
    readonly ComparisonStatusBar _statusBar = new();

    DemoComparison? _run;
    Filter _filter = Filter.All;
    bool _running;

    public MainForm()
    {
        SuspendLayout();
        Text = "Xero.DataComparer";
        Font = Theme.UI;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = Metrics.WindowSize;
        MinimumSize = Metrics.WindowMinSize;
        // Below this layout size the whole window scrolls instead of squeezing toolbar, tiles and grid.
        AutoScroll = true;
        AutoScrollMinSize = LogicalToDeviceUnits(Metrics.ScrollMinSize);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized; // use the whole screen, whatever its size and scaling
        KeyPreview = true;

        _toolbar.CompareClicked += () => _ = CompareAsync();
        _toolbar.IgnoredChanged += () =>
        {
            if (_run != null && !_toolbar.Ignored.SequenceEqual(_run.Ignored.Select(f => f.Name))) _ = CompareAsync();
        };
        _toolbar.ThresholdChanged += ApplyThreshold;
        _toolbar.DifferingOnlyChanged += () => _grid.DifferingOnly = _toolbar.DifferingOnly;
        _toolbar.SearchChanged += ShowRows;
        _toolbar.ThemeClicked += () =>
        {
            Theme.SetDark(!Theme.Dark);
            ApplyTheme();
        };
        _indicators.FilterClicked += SetFilter;
        _filterBar.FilterChanged += SetFilter;
        _grid.CurrentDiffChanged += () => _details.ShowRow(_grid.CurrentDiff);
        _description.Text = "Choose a dataset and click Compare: two lists of synthetic risk indicators are generated, then compared by ListComparer<T>.";
        _statusBar.ShowMessage(Hint);

        // Without an explicit style the column auto-sizes to its widest child (the grid) and overflows the window.
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, Metrics.ToolbarHeight));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, Metrics.HeaderHeight));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, Metrics.TilesHeight));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, Metrics.FilterBarHeight));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // grids
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // status
        _root.Controls.Add(_toolbar, 0, 0);
        _root.Controls.Add(BuildHeader(), 0, 1);
        _root.Controls.Add(_indicators, 0, 2);
        _root.Controls.Add(_filterBar, 0, 3);
        _root.Controls.Add(BuildGrids(), 0, 4);
        _root.Controls.Add(_statusBar, 0, 5);
        Controls.Add(_root);

        Shown += (_, _) =>
        {
            SetDetailsHeight(Metrics.DetailsHeight);
            WindowTheme.Apply(this, _grid, _details); // the grids' scrollbars exist now
            ActiveControl = _grid;
        };
        ApplyTheme();
        ResumeLayout(true);
    }

    /// <summary>Title + description.</summary>
    Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(Metrics.Gutter, 4, Metrics.Gutter, 0), Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, Metrics.TitleHeight));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(_title, 0, 0);
        header.Controls.Add(_description, 0, 1);
        return header;
    }

    /// <summary>Gaps on top; details of the current row and timings of the comparison underneath.</summary>
    Control BuildGrids()
    {
        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.FixedPanel = FixedPanel.Panel2;
        _split.SplitterWidth = Metrics.SplitterWidth;
        _split.Panel1.Padding = new Padding(Metrics.Border); // border, coloured in ApplyTheme
        _split.Panel1.Controls.Add(_grid);
        _detailsBox.Controls.Add(_details);
        _ganttBox.Controls.Add(_gantt);
        _bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _bottom.Controls.Add(_detailsBox, 0, 0);
        _bottom.Controls.Add(_ganttBox, 1, 0);
        _split.Panel2.Controls.Add(_bottom);
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Metrics.Gutter, 0, Metrics.Gutter, 0), Margin = Padding.Empty };
        host.Controls.Add(_split);
        return host;
    }

    /// <summary>Re-applies the colours set on controls; owner-drawn parts read the theme when they paint.</summary>
    void ApplyTheme()
    {
        KindStyles.ApplyTheme();
        _toolbar.ApplyTheme();
        _filterBar.ApplyTheme();
        _grid.ApplyTheme();
        _details.ApplyTheme();
        _statusBar.ApplyTheme();
        BackColor = _root.BackColor = _split.BackColor = _split.Panel2.BackColor = _bottom.BackColor = Theme.Colors.Body;
        ForeColor = _title.ForeColor = Theme.Colors.Ink;
        _description.ForeColor = Theme.Colors.Muted;
        _split.Panel1.BackColor = _detailsBox.BackColor = _ganttBox.BackColor = Theme.Colors.Border;
        WindowTheme.Apply(this, _grid, _details);
        Invalidate(true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(this, _grid, _details);
    }

    void SetDetailsHeight(int height)
    {
        // SplitterDistance throws when out of range (small or minimized window): clamp it.
        int max = _split.Height - _split.Panel2MinSize - _split.SplitterWidth;
        if (max <= _split.Panel1MinSize) return;
        _split.SplitterDistance = Math.Clamp(_split.Height - _split.SplitterWidth - LogicalToDeviceUnits(height), _split.Panel1MinSize, max);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.F: _toolbar.FocusSearch(); return true;
            case Keys.F5: _ = CompareAsync(); return true;
            case Keys.Escape when _toolbar.ClearSearch(): return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Generates the dataset and compares it in the background. Never throws.</summary>
    async Task CompareAsync()
    {
        if (_running || IsDisposed) return;
        SetRunning(true);
        var dataset = _toolbar.SelectedDataset;
        var ignored = _toolbar.Ignored;
        try
        {
            // Release the previous result before generating millions of rows again.
            _run = null;
            _grid.ClearRows();
            _details.ShowRow(null);
            var progress = new Progress<string>(_statusBar.ShowMessage);
            var run = await Task.Run(() => DemoComparison.RunAsync(dataset, ignored, progress));
            if (IsDisposed) return;
            _run = run;
            run.ApplyThreshold(_toolbar.Threshold);
            _grid.Build(run);
            _details.Build(run);
            _gantt.SetTasks(run.Tasks);
            _description.Text = Describe(run);
            _statusBar.ShowTimings(run);
            _indicators.Display(run, _toolbar.Threshold);
            ShowRows();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            if (!IsDisposed) _statusBar.ShowMessage($"⚠ Comparison failed: {ex.Message} (see {ErrorLog.Path})");
        }
        finally
        {
            _running = false;
            if (!IsDisposed) SetRunning(false);
        }
    }

    void SetRunning(bool running)
    {
        _running = running;
        _toolbar.SetBusy(running);
        _statusBar.SetBusy(running);
        UseWaitCursor = running;
    }

    static string Describe(DemoComparison run) =>
        $"{run.ReferenceCount:N0} reference and {run.TargetCount:N0} target risk indicators (synthetic, same generator as the console demo). "
        + $"Keys: {string.Join(", ", run.Keys.Select(k => k.Name))} · ignored: {(run.Ignored.Length == 0 ? "none" : string.Join(", ", run.Ignored.Select(f => f.Name)))}"
        + $" · compared: {string.Join(", ", run.Properties.Select(p => p.Name))}. One call returns the rows only in the reference, only in the target, "
        + "and in both with the properties that differ; the relevance threshold then masks the gaps too small to matter.";

    /// <summary>Applied to the current result at once (no new comparison).</summary>
    void ApplyThreshold()
    {
        if (_run == null) return;
        _run.ApplyThreshold(_toolbar.Threshold);
        _indicators.Display(_run, _toolbar.Threshold);
        ShowRows();
    }

    /// <summary>Applies a filter, from a tile or the combo, and keeps both in sync.</summary>
    void SetFilter(Filter filter)
    {
        if (filter == _filter) return;
        _filter = filter;
        _indicators.Highlight(filter);
        _filterBar.ShowFilter(filter);
        ShowRows();
    }

    void ShowRows()
    {
        if (_run == null || IsDisposed) return;
        var search = _toolbar.Search;
        _grid.ShowRows(_run.Rows.Where(r => _filter.Matches(r, search)));
        _statusBar.ShowMessage($"n = {_grid.RowCount:N0} · {Hint}");
    }
}
