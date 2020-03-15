import senva.*;

fun main (args: Array<String>)
{
	while (true)
	{
		try
		{
			print("senva> ")
			val code = readLine() ?: return
			println(lprint(leval(lreadtop(code), genv)))
		}
		catch (erro: Erro)
		{
			println(lprint(erro))
		}
	}
}
