using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;

namespace ReplForm
{
	class Repl : Form
	{
		private Senva.Interpreter senva;
		private Label label;
		private TextBox input_text;
		private Button input_btn;

		private void input_Click (object sender, EventArgs e)
		{
			this.label.ForeColor = Color.Black;
			try
			{
				this.label.Text = Senva.Interpreter.lprint(
						senva.leval(Senva.Interpreter.lreadtop(this.input_text.Text)
							, senva.genv));
			}
			catch (Senva.Interpreter.Erro erro)
			{
				this.label.ForeColor = Color.Red;
				this.label.Text = Senva.Interpreter.lprint(erro);
			}
		}

		public Repl ()
		{
			this.Text = "SENVA";

			this.label = new Label();
			this.label.Dock = DockStyle.Fill;

			this.input_text = new TextBox();
			this.input_text.Dock = DockStyle.Top;

			this.input_btn = new Button();
			this.input_btn.Text = "EVAL";
			this.input_btn.Click += new EventHandler(this.input_Click);
			this.input_btn.Size = new Size(0, 25);
			this.input_btn.Dock = DockStyle.Top;

			this.senva = new Senva.Interpreter();

			this.Controls.Add(this.label);
			this.Controls.Add(this.input_btn);
			this.Controls.Add(this.input_text);
		}
	}
}

class Program
{
	[STAThread]
	public static void Main (string[] args)
	{
		Application.EnableVisualStyles();
		Application.Run(new ReplForm.Repl());
	}
}
