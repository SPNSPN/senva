using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace ReplForm
{
	class Repl : Form
	{
		private Senva.Interpreter senva;
		private Label label;

		public Repl ()
		{
			this.label = new Label();
			this.label.Dock = DockStyle.Fill;
			this.senva = new Senva.Interpreter();
			this.label.Text = Senva.Interpreter.print(
					senva.eval(Senva.Interpreter.read("(list 1 'symb \"strn\" nil)")
						, senva.genv));
			this.Controls.Add(this.label);
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
