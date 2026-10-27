using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GTAVArchipelagoSetup
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(16, 18, 22);
        public static readonly Color Card = Color.FromArgb(28, 31, 38);
        public static readonly Color CardBorder = Color.FromArgb(44, 48, 58);
        public static readonly Color Field = Color.FromArgb(38, 42, 51);
        public static readonly Color Text = Color.FromArgb(236, 238, 241);
        public static readonly Color Muted = Color.FromArgb(150, 157, 168);
        public static readonly Color Accent = Color.FromArgb(87, 196, 105);
        public static readonly Color AccentDark = Color.FromArgb(62, 150, 78);
        public static readonly Color Warning = Color.FromArgb(244, 166, 52);
        public static readonly Color Error = Color.FromArgb(232, 84, 76);
        public static readonly Color Neutral = Color.FromArgb(58, 63, 75);
        public static readonly Color Link = Color.FromArgb(110, 180, 255);

        public static Font Regular(float size = 9.5f) => new Font("Segoe UI", size, FontStyle.Regular);
        public static Font Bold(float size = 9.5f) => new Font("Segoe UI Semibold", size, FontStyle.Regular);

        public static Button Primary(string text) => MakeButton(text, Accent, Color.FromArgb(14, 30, 18), true);

        public static Button Secondary(string text) => MakeButton(text, Neutral, Text, false);

        public static Button Danger(string text) => MakeButton(text, Color.FromArgb(90, 40, 40), Text, false);

        private static Button MakeButton(string text, Color back, Color fore, bool bold)
        {
            var button = new FlatButton
            {
                Text = text,
                BackColor = back,
                ForeColor = fore,
                Font = bold ? Bold(10f) : Regular(9.5f),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(bold ? 14 : 10, 6, bold ? 14 : 10, 6),
                Margin = new Padding(0, 4, 8, 4),
                Cursor = Cursors.Hand,
            };
            return button;
        }

        public static Label Label(string text, Color color, Font font = null) => new Label
        {
            Text = text,
            ForeColor = color,
            Font = font ?? Regular(),
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 2),
        };

        public static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class FlatButton : Button
    {
        private bool _hover;

        public FlatButton()
        {
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
            Color back = !Enabled ? Color.FromArgb(45, 48, 56) : _hover ? ControlPaint.Light(BackColor, 0.15f) : BackColor;
            Color fore = Enabled ? ForeColor : Color.FromArgb(110, 115, 125);
            using (GraphicsPath path = Theme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 6))
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

    internal sealed class StepCard : Panel
    {
        private readonly Label _title;
        private readonly Label _status;
        private readonly FlowLayoutPanel _layout;
        private readonly int _number;
        private StepState _state = StepState.Pending;

        public StepCard(int number, string title)
        {
            _number = number;
            BackColor = Theme.Card;
            Padding = new Padding(60, 14, 18, 16);
            DoubleBuffered = true;
            _layout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
            };
            _title = Theme.Label(title, Theme.Text, Theme.Bold(12.5f));
            _status = Theme.Label(string.Empty, Theme.Muted, Theme.Regular(9.5f));
            _status.Margin = new Padding(0, 2, 0, 8);
            Body = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
            };
            _layout.Controls.Add(_title);
            _layout.Controls.Add(_status);
            _layout.Controls.Add(Body);
            Controls.Add(_layout);
        }

        public FlowLayoutPanel Body { get; }

        public int InnerWidth => Math.Max(200, Width - Padding.Horizontal);

        public void SetStatus(StepState state, string text)
        {
            _state = state;
            _status.Text = text;
            _status.ForeColor = state == StepState.Done ? Theme.Accent : state == StepState.Warning ? Theme.Warning : state == StepState.Error ? Theme.Error : Theme.Muted;
            Invalidate();
        }

        public void Relayout(int width)
        {
            Width = width;
            int inner = InnerWidth;
            _title.MaximumSize = new Size(inner, 0);
            _status.MaximumSize = new Size(inner, 0);
            Wrap(Body, inner);
            _layout.Location = new Point(Padding.Left, Padding.Top);
            Size preferred = _layout.GetPreferredSize(new Size(inner, 0));
            _layout.Size = new Size(inner, preferred.Height);
            Height = preferred.Height + Padding.Vertical;
            Invalidate();
        }

        private static void Wrap(Control parent, int width)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Label label) label.MaximumSize = new Size(width, 0);
                else if (control is FlowLayoutPanel panel)
                {
                    if (panel.FlowDirection == FlowDirection.TopDown) Wrap(panel, width);
                    else panel.MaximumSize = new Size(width, 0);
                }
                else if (control is CheckedListBox list) list.Width = width;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color color = _state == StepState.Done ? Theme.Accent : _state == StepState.Warning ? Theme.Warning : _state == StepState.Error ? Theme.Error : Theme.Neutral;
            using (var bar = new SolidBrush(color))
                g.FillRectangle(bar, 0, 0, 4, Height);
            using (var pen = new Pen(Theme.CardBorder))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            var circle = new Rectangle(18, 14, 28, 28);
            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, circle);
            string mark = _state == StepState.Done ? "\u2713" : _state == StepState.Pending ? _number.ToString() : "!";
            Color fore = _state == StepState.Pending ? Theme.Text : Theme.Background;
            using (Font font = Theme.Bold(11f))
                TextRenderer.DrawText(g, mark, font, circle, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
