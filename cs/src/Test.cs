using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

class Program
{

	[STAThread]
	public static void Main (string[] args)
	{
		Senva.Interpreter senva = new Senva.Interpreter();
		Console.WriteLine(Senva.Interpreter.print(senva.eval(senva.read("(list 1 'symb \"strn\" nil)"))));
	}
}
