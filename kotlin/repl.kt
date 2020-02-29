import interpreter.Interpreter;

fun main (args: Array<String>)
{
	val inp = Interpreter()
	while (true)
	{
		print("senva> ")
		val code = readLine() ?: return
		println(inp.lprint(inp.leval(inp.lread(code), inp.genv)))
	}
}
