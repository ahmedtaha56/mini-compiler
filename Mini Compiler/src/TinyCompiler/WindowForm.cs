using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TinyCompiler
{
    public partial class WindowForm : Form
    {
        public WindowForm()
        {
            InitializeComponent();

            // Subscribe to the necessary events for line number gutter
            tfSourceCode.TextChanged += (object sender, EventArgs e) => Invalidate();
            tfSourceCode.VScroll += (object sender, EventArgs e) => Invalidate();
            tfSourceCode.KeyDown += HandleRichTextBoxPaste;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawLineNumbers(tfSourceCode, e.Graphics);
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            Compiler.Compile(tfSourceCode.Text);

            PopulateTokensTable();
            PopulateParseTree();
            PrintErrors();
        }

        private void PopulateTokensTable()
        {
            tblTokens.Rows.Clear();
            foreach (var token in Compiler.TokenStream)
            {
                tblTokens.Rows.Add(token.lex, token.type);
            }
        }

        private void PrintErrors()
        {
            tfErrors.Clear();
            tfErrors.Text = string.Join("\r\n", Errors.GetAll());
        }

        private void PopulateParseTree()
        {
            treePrase.Nodes.Clear();
            treePrase.Nodes.Add(PrintParseTree(Compiler.treeRoot));
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // Resest UI Elements
            tfSourceCode.Clear();
            tfErrors.Clear();
            tblTokens.Rows.Clear();
            treePrase.Nodes.Clear();

            Compiler.Clear();
        }

        private static TreeNode PrintParseTree(Node root)
        {
            TreeNode tree = new TreeNode("Parse Tree");
            TreeNode treeRoot = PrintTree(root);

            if (treeRoot != null)
            {
                tree.Nodes.Add(treeRoot);
            }

            return tree;
        }

        private static TreeNode PrintTree(Node root)
        {
            if ((root == null) || (root.Name == null))
            {
                return null;
            }

            TreeNode tree = new TreeNode(root.Name);
            foreach (Node child in root.Children)
            {
                if (child != null)
                {
                    tree.Nodes.Add(PrintTree(child));
                }
            }

            return tree;
        }

        private void HandleRichTextBoxPaste(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.V))
            {
                if (Clipboard.ContainsText())
                {
                    string plainText = Clipboard.GetText(TextDataFormat.Text);
                    tfSourceCode.SelectedText = plainText;
                }

                // Prevent default paste behavior (i.e., disabling rich text paste)
                e.SuppressKeyPress = true;
            }
        }

        private void DrawLineNumbers(RichTextBox richTextBox, Graphics graphics)
        {
            Font font = richTextBox.Font;
            Brush brush = Brushes.Gray;

            const float xPosMargin = 4.0f;
            float yPos = richTextBox.Top;

            int firstIndex = richTextBox.GetCharIndexFromPosition(Point.Empty);
            int firstLine = richTextBox.GetLineFromCharIndex(firstIndex);

            Point bottomLeft = new Point(0, richTextBox.ClientRectangle.Height);
            int lastIndex = richTextBox.GetCharIndexFromPosition(bottomLeft);
            int lastLine = richTextBox.GetLineFromCharIndex(lastIndex);

            if (richTextBox.Lines.Any() && string.IsNullOrEmpty(richTextBox.Lines.Last()))
            {
                lastLine++;
            }

            int maxLines = richTextBox.ClientRectangle.Height / Font.Height;
            for (int i = firstLine; (i <= lastLine) && (i - firstLine <= maxLines); i++)
            {
                string numStr = (i + 1).ToString();
                PointF pos = new PointF(richTextBox.Left - xPosMargin, yPos);
                yPos += font.Height;

                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Far;
                    sf.LineAlignment = StringAlignment.Near;
                    graphics.DrawString(numStr, font, brush, pos, sf);
                }
            }
        }
    }
}
