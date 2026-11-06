using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace GTAVArchipelagoSetup
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(14, 18, 21);
        public static readonly Color Surface = Color.FromArgb(22, 28, 33);
        public static readonly Color Line = Color.FromArgb(38, 48, 58);
        public static readonly Color Field = Color.FromArgb(28, 35, 41);
        public static readonly Color Text = Color.FromArgb(235, 230, 221);
        public static readonly Color Muted = Color.FromArgb(154, 164, 173);
        public static readonly Color Accent = Color.FromArgb(242, 194, 48);
        public static readonly Color OnAccent = Color.FromArgb(22, 19, 10);
        public static readonly Color Good = Color.FromArgb(92, 199, 125);
        public static readonly Color Warning = Color.FromArgb(242, 158, 64);
        public static readonly Color Error = Color.FromArgb(234, 90, 67);
        public static readonly Color Info = Color.FromArgb(84, 168, 224);
        public static readonly Color Neutral = Color.FromArgb(44, 54, 63);

        public static readonly float Scale = ReadScale();

        private static float ReadScale()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    return Math.Max(1f, g.DpiX / 96f);
            }
            catch
            {
                return 1f;
            }
        }

        public static Label Heading(string text, Color color, Font font) => new TightLabel
        {
            Text = text,
            ForeColor = color,
            Font = font,
            Margin = new Padding(0, 0, 0, 6),
        };

        private static readonly string[] Installed = new InstalledFontCollection().Families.Select(f => f.Name).ToArray();

        private static string Pick(params string[] names) => names.FirstOrDefault(n => Installed.Contains(n, StringComparer.OrdinalIgnoreCase)) ?? "Segoe UI";

        private static readonly string DisplayFamily = Pick("Bahnschrift SemiBold Condensed", "Bahnschrift Condensed", "Bahnschrift", "Segoe UI Semibold");
        private static readonly string BodyFamily = Pick("Segoe UI");
        private static readonly string StrongFamily = Pick("Segoe UI Semibold", "Segoe UI");
        private static readonly string MonoFamily = Pick("Cascadia Mono", "Consolas", "Courier New");

        public static Font Display(float size) => new Font(DisplayFamily, size, FontStyle.Regular);
        public static Font Regular(float size = 10f) => new Font(BodyFamily, size, FontStyle.Regular);
        public static Font Bold(float size = 10f) => new Font(StrongFamily, size, FontStyle.Regular);
        public static Font Mono(float size = 8.5f) => new Font(MonoFamily, size, FontStyle.Regular);

        public static FlatButton Primary(string text) => new FlatButton(text, Accent, OnAccent, Bold(10.5f), new Padding(20, 9, 20, 9));

        public static FlatButton Secondary(string text) => new FlatButton(text, Neutral, Text, Regular(10f), new Padding(14, 8, 14, 8));

        public static FlatButton Quiet(string text) => new FlatButton(text, Background, Muted, Regular(9f), new Padding(8, 6, 8, 6));

        public static Label Label(string text, Color color, Font font = null) => new Label
        {
            Text = text,
            ForeColor = color,
            Font = font ?? Regular(),
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6),
        };

        public static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color For(StepState state) =>
            state == StepState.Done ? Good : state == StepState.Warning ? Warning : state == StepState.Error ? Error : Muted;
    }

    internal sealed class FlatButton : Button
    {
        private bool _hover;

        public FlatButton(string text, Color back, Color fore, Font font, Padding padding)
        {
            Text = text;
            BackColor = back;
            ForeColor = fore;
            Font = font;
            Padding = padding;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Margin = new Padding(0, 4, 8, 4);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
            bool quiet = BackColor == Theme.Background;
            Color back = !Enabled ? Color.FromArgb(32, 38, 44) : _hover ? (quiet ? Theme.Surface : ControlPaint.Light(BackColor, 0.12f)) : BackColor;
            Color fore = !Enabled ? Color.FromArgb(96, 104, 112) : quiet && _hover ? Theme.Text : ForeColor;
            using (GraphicsPath path = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 4))
            using (var brush = new SolidBrush(back))
                e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    internal enum StepState
    {
        Pending,
        Done,
        Warning,
        Error,
    }

    internal sealed class StatusLine : Control
    {
        private StepState _state = StepState.Pending;

        public StatusLine()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            Font = Theme.Regular(10f);
            Margin = new Padding(0, 6, 0, 10);
        }

        public void Set(StepState state, string text)
        {
            _state = state;
            Text = text;
            Height = Measure(Width);
            Invalidate();
        }

        public int Measure(int width)
        {
            float k = Theme.Scale;
            Size size = TextRenderer.MeasureText(Text ?? string.Empty, Font, new Size(Math.Max(50, width - (int)(52 * k)), 0), TextFormatFlags.WordBreak);
            return Math.Max((int)(44 * k), size.Height + (int)(24 * k));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int height = Measure(Width);
            if (Height != height) Height = height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            Color color = Theme.For(_state);
            using (GraphicsPath path = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 4))
            using (var brush = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.Line))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            using (var bar = new SolidBrush(color))
                g.FillRectangle(bar, 0, 4, 3, Height - 8);
            float k = Theme.Scale;
            int d = (int)(12 * k);
            var dot = new Rectangle((int)(18 * k), Height / 2 - d / 2, d, d);
            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, dot);
            var text = new Rectangle((int)(40 * k), (int)(12 * k), Width - (int)(52 * k), Height - (int)(24 * k));
            TextRenderer.DrawText(g, Text, Font, text, Theme.Text, TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    internal sealed class StepRail : Control
    {
        private readonly string[] _steps;
        private readonly bool[] _done;
        private readonly float[] _open;
        private readonly Timer _timer = new Timer { Interval = 15 };
        private int _current = -1;

        public StepRail(params string[] steps)
        {
            _steps = steps;
            _done = new bool[steps.Length];
            _open = new float[steps.Length];
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Font = Theme.Bold(9.5f);
            _timer.Tick += (s, e) => Animate();
        }

        public void Set(int current, bool[] done)
        {
            _current = current;
            for (int i = 0; i < _done.Length && i < done.Length; i++) _done[i] = done[i];
            _timer.Enabled = true;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        private void Animate()
        {
            bool moving = false;
            for (int i = 0; i < _open.Length; i++)
            {
                float target = i == _current ? 1f : 0f;
                float next = _open[i] + (target - _open[i]) * 0.2f;
                if (Math.Abs(target - next) < 0.01f) next = target;
                if (next != _open[i]) moving = true;
                _open[i] = next;
            }
            if (!moving) _timer.Enabled = false;
            Invalidate();
        }

        private static Color Mix(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            float k = Theme.Scale;
            int count = _steps.Length;
            float dot = 24 * k;
            float pillHeight = 34 * k;
            float gap = 56 * k;
            var widths = new float[count];
            var texts = new string[count];
            for (int i = 0; i < count; i++)
            {
                texts[i] = (i + 1) + "   " + _steps[i].ToUpperInvariant();
                float full = TextRenderer.MeasureText(g, texts[i], Font, Size.Empty, TextFormatFlags.NoPadding).Width + 36 * k;
                widths[i] = dot + (full - dot) * _open[i];
            }
            float total = widths.Sum() + gap * (count - 1);
            float x = (Width - total) / 2f;
            float cy = Height / 2f;
            var lefts = new float[count];
            for (int i = 0; i < count; i++)
            {
                lefts[i] = x;
                x += widths[i] + gap;
            }
            for (int i = 0; i < count - 1; i++)
            {
                Color color = _done[i] ? Mix(Theme.Line, Theme.Good, 0.55f) : Theme.Line;
                using (var pen = new Pen(color, 2 * k))
                    g.DrawLine(pen, lefts[i] + widths[i] + 6 * k, cy, lefts[i + 1] - 6 * k, cy);
            }
            using (var number = Theme.Bold(8.5f))
            {
                for (int i = 0; i < count; i++)
                {
                    float t = _open[i];
                    float h = dot + (pillHeight - dot) * t;
                    var box = new RectangleF(lefts[i], cy - h / 2f, widths[i], h);
                    Color rest = _done[i] ? Theme.Good : Theme.Neutral;
                    Color fill = Mix(rest, Theme.Accent, t);
                    using (GraphicsPath path = Theme.Rounded(Rectangle.Round(box), (int)(h / 2f)))
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                    if (t > 0.6f)
                    {
                        float alpha = (t - 0.6f) / 0.4f;
                        Color fore = Mix(fill, Theme.OnAccent, alpha);
                        TextRenderer.DrawText(g, texts[i], Font, Rectangle.Round(box), fore,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                    }
                    else if (t < 0.3f)
                    {
                        float alpha = 1f - t / 0.3f;
                        if (_done[i])
                        {
                            float mx = box.X + box.Width / 2f;
                            using (var pen = new Pen(Mix(fill, Theme.OnAccent, alpha), 2.2f * k) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                                g.DrawLines(pen, new[]
                                {
                                    new PointF(mx - 5f * k, cy + 0.5f * k),
                                    new PointF(mx - 1.5f * k, cy + 4f * k),
                                    new PointF(mx + 5f * k, cy - 4f * k),
                                });
                        }
                        else
                        {
                            TextRenderer.DrawText(g, (i + 1).ToString(), number, Rectangle.Round(box), Mix(fill, Theme.Text, alpha),
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                        }
                    }
                }
            }
        }
    }

    internal sealed class TightLabel : Label
    {
        public TightLabel()
        {
            AutoSize = true;
            BackColor = Color.Transparent;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size size = TextRenderer.MeasureText(Text ?? string.Empty, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return new Size(size.Width + Padding.Horizontal + 2, size.Height + Padding.Vertical);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, Text, Font, new Point(Padding.Left, Padding.Top), ForeColor, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
            base.OnRenderItemText(e);
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Surface;
            public override Color ImageMarginGradientBegin => Theme.Surface;
            public override Color ImageMarginGradientMiddle => Theme.Surface;
            public override Color ImageMarginGradientEnd => Theme.Surface;
            public override Color MenuBorder => Theme.Line;
            public override Color MenuItemBorder => Theme.Line;
            public override Color MenuItemSelected => Theme.Neutral;
            public override Color MenuItemSelectedGradientBegin => Theme.Neutral;
            public override Color MenuItemSelectedGradientEnd => Theme.Neutral;
            public override Color SeparatorDark => Theme.Line;
            public override Color SeparatorLight => Theme.Line;
        }
    }

    internal sealed class ThinScrollBar : Control
    {
        private int _value;
        private int _content = 1;
        private int _view = 1;
        private int _step = 48;
        private bool _hover;
        private bool _dragging;
        private int _dragOffset;

        public event EventHandler Scrolled;

        public ThinScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Cursor = Cursors.Default;
        }

        private int Max => Math.Max(0, _content - _view);

        public bool CanScroll => Max > 0;

        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Max(0, Math.Min(Max, value));
                if (clamped == _value) return;
                _value = clamped;
                Invalidate();
                Scrolled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetRange(int content, int view, int step)
        {
            _content = Math.Max(1, content);
            _view = Math.Max(1, view);
            _step = Math.Max(1, step);
            int clamped = Math.Max(0, Math.Min(Max, _value));
            if (clamped != _value)
            {
                _value = clamped;
                Scrolled?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
        }

        private Rectangle Thumb
        {
            get
            {
                int track = Height;
                int length = Math.Max(Width * 4, (int)(track * (_view / (float)_content)));
                length = Math.Min(track, length);
                int top = Max == 0 ? 0 : (int)((track - length) * (_value / (float)Max));
                return new Rectangle(0, top, Width, length);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !CanScroll) return;
            Rectangle thumb = Thumb;
            if (thumb.Contains(e.Location))
            {
                _dragging = true;
                _dragOffset = e.Y - thumb.Top;
                Capture = true;
            }
            else
            {
                Value += e.Y < thumb.Top ? -_view + _step : _view - _step;
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            Rectangle thumb = Thumb;
            int room = Height - thumb.Height;
            if (room <= 0) return;
            Value = (int)((e.Y - _dragOffset) / (float)room * Max);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (!CanScroll) return;
            int radius = Math.Max(1, Width / 2);
            using (GraphicsPath track = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), radius))
            using (var brush = new SolidBrush(Theme.Surface))
                g.FillPath(brush, track);
            Rectangle thumb = Thumb;
            Color color = _dragging ? Theme.Accent : _hover ? Theme.Muted : Theme.Neutral;
            using (GraphicsPath path = Theme.Rounded(new Rectangle(thumb.X, thumb.Y, thumb.Width - 1, thumb.Height - 1), radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }
    }
}
