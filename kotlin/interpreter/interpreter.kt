package interpreter

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

class Interpreter ()
{
	val nil = Nil()
	var genv = Cons(nil, nil)

	fun lread (code: String): ICons
	{
		return nil // TODO
	}

	fun lreadtop (code: String): ICons
	{
		return nil // TODO
	}

	fun leval (expr: ICons, env: ICons): Any
	{
		return expr // TODO
	}

	fun lprint (expr: Any): String
	{
		return "nil" // TODO
	}
}

