import kotlin.system.exitProcess
import interpreter.Interpreter;

val inp = Interpreter()

fun CHECK (code: String, succ: Any?)
{
	print("CHECK: \"${code}\" -> ")
	val s = inp.lprint(inp.leval(inp.lreadtop(code), inp.genv))
	println(s)
	succ ?: return
	if (s != succ)
	{
		println("Fail CHECK.")
		exitProcess(-1)
	}
}

fun ECHECK (code: String, ceid: Int, succ: Any?)
{
}


fun main (args: Array<String>)
{
	CHECK("", "NIL")
	CHECK("nil", "NIL")
}

