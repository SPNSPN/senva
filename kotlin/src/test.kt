import kotlin.system.exitProcess
import senva.*;

fun CHECK (code: String, succ: Any?)
{
	print("CHECK: \"${code}\" -> ")
	val s = lprint(leval(lreadtop(code), genv))
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

