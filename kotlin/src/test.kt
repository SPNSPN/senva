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
	print("ECHECK: \"${code}\" -> ")
	try
	{
		println(lprint(leval(lreadtop(code), genv)))
	}
	catch (e: Erro)
	{
		println("<Erro \"${erro.estr}\">")
		if (ceid != erro.eid)
		{
			throw Erro(444, "expect code: ${ceid}, but got code: ${erro.eid}")
		}
		if (succ != erro.estr)
		{
			throw Erro(444, "expect mess: ${succ}, but got mess: ${erro.estr}")
		}
		return nil
	}
	throw Erro(4444, "not got erro!")
}


fun main (args: Array<String>)
{
	CHECK("", "NIL")
	CHECK("nil", "NIL")
}

