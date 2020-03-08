import senva.*;

fun main (args: Array<String>)
{
	while (true)
	{
		print("senva> ")
		val code = readLine() ?: return
		println(lprint(leval(lreadtop(code), genv)))
	}
}
