using System;
using System.Windows.Forms;
using ReplForm;

class Program
{
	[STAThread]
	public static void Main (string[] args)
	{
		Application.EnableVisualStyles();
		Application.Run(new ReplForm.Repl());
	}
}
