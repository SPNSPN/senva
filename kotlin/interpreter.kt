package Interpreter

interface ICons
{
	fun car (): Any
	fun cdr (): Any
}

class Nil: ICons
{
	override fun car () = this
	override fun cdr () = this
}

class Symb (var name: String)
{
}

class Cons (var a: Any, var d: Any): ICons
{
	override fun car () = this.a
	override fun cdr () = this.d
}

public val nil: Nil = Nil()

fun lread (code: String): ICons
{
	return nil
}

fun leval (expr: ICons): ICons
{
	return expr
}

fun lprint (expr: ICons): String
{
	return "nil"
}

fun main(args : Array<String>)
{
	println(lprint(leval(lread("(list 1 'symb \"strn\" nil)"))))
}

