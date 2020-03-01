package senva

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

class Cons (var a: Any, var d: Any): ICons
{
	override fun car () = this.a
	override fun cdr () = this.d
}

class Symb (var name: String) { }

val nil = Nil()
val t = Symb("T")

enum class ErroId (val value: Int)
{
	FullMemory			 (0)
	, UnknownOpcode		 (1)
	, OutOfEnvironment	 (2)
	, Type				 (3)
	, Symbol			 (4)
	, Syntax			 (5)
	, UnCallable		 (6)
	, ArgsUnmatch		 (7)
	, UnEvaluatable		 (8)
	, FileNotFound		 (9)
}

class Erro (val eid: Int, val estr: String) : Exception(estr)
{
	constructor (eid: ErroId, estr: String) : this(eid.value, estr)
}

class Queu ()
{
	var entr: ICons
	var exit: ICons

	init
	{
		entr = nil;
		exit = entr;
	}

	fun push (v: Any): Queu
	{
		val c = Cons(v, nil)
		if (isnil(entr))
		{
			entr = c
			exit = c
		}
		else
		{
			rplacd(entr as Cons, c)
			entr = c
		}
		return this
	}

	fun pop (): Any
	{
		if (isnil(exit)) { return nil }

		val e = car(exit)
		if (exit === entr)
		{
			exit = nil
			entr = nil
		}
		else
		{
			exit = cdr(exit) as ICons
		}
		return e
	}

	fun concat (queu: Queu): Queu
	{
		if (isnil(entr))
		{
			entr = queu.entr
			exit = queu.exit
		}
		else if (! isnil(queu))
		{
			rplacd(entr as Cons, queu.exit)
		}
		return this
	}
}

fun cons (a: Any, d: Any): Cons = Cons(a, d)
fun car (o: ICons): Any = o.car()
fun cdr (o: ICons): Any = o.cdr()
fun eq (a: Any, b: Any): Any = if (a === b) t else nil
fun atom (o: Any): Any = if (o is Cons) nil else t

fun append (colla: ICons, collb: ICons): ICons
{
	var app = collb
	var rest = reverse(colla)
	while (! isnil(rest))
	{
		app = cons(car(rest), app)
		rest = cdr(rest)
	}
	return app
}

fun reverse (coll: ICons): ICons
{
	var rev: ICons = nil
	var rest = coll
	while (! isnil(rest))
	{
		rev = cons(car(rest), rev)
		rest = cdr(rest)
	}
	return rev
}

fun rplaca (c: Cons, o: Any): Cons
{
	c.a = o
	return c
}

fun rplacd (c: Cons, o: Any): Cons
{
	c.d = o
	return c
}

fun lif () {}// TODO
fun ldefine () {}
fun lsetq () {}
fun llambda () {}
fun lquote () {}

fun isnil (o: Any): Boolean = o === nil
fun l (vararg args: Any): ICons = args.foldRight(nil
		, fun (e: Any, acc: ICons): ICons = cons(e, acc) )

fun find (v: Any, coll: ICons): Any
{
	var rest = coll
	while (! isnil(rest))
	{
		if (equal(v, car(rest)) { return t }
		rest = cdr(rest)
	}
	return nil
}

fun findidx_eq (v: Any, coll: ICons): Any
{
	var idx = 0
	var rest = coll
	while (! isnil(rest))
	{
		if (v === car(rest)) { return idx }
		++idx
		rest = cdr(rest)
	}
	return nil
}


var genv = Cons(nil, nil)

data class ReadBuf (var tok: String, var rmacs: ICons)

fun growth (tree: ICons, buff: ReadBuf): ICons
{
	if (! isnil(buff.tok))
	{
		buff.tok = ""
		buff.rmacs = nil
		if ("nil" == buff.tok || "NIL" == buff.tok)
		{
			return cons(wrap_readmacros(nil, buff.rmacs), tree)
		}
		val inum = buff.tok.toIntOrNull(10) 
		if (inum != null) { return cons(wrap_readmacros(inum, buff.rmacs), tree) }
		val fnum = buff.tok.toDoubleOrNull() 
		if (fnum != null) { return cons(wrap_readmacros(fnum, buff.rmacs), tree) }

		return cons(wrap_readmacros(Symb(buff.tok), buff.rmacs), tree)
	}
	return tree
}

fun find_co_paren (code: String): Int
{
	var sflg = false
	var layer = 1
	var idx = 0
	while (idx < code.length)
	{
		val c = code[idx]
		if (! sflg && '(' == c)
		{
			++layer
		}
		else if (! sflg && ')' == c)
		{
			--layer;
		}
		else if ('\\' == c)
		{
			++idx
		}
		else if ('"' == c)
		{
			sflg = ! sflg
		}

		if (layer < 1) { return idx }
		++idx
	}
	throw Erro(ErroId.Syntax, "not found close parenthesis.")
}

fun find_co_bracket (code: String): Int
{
	return 0 // TODO
}

fun take_string (code: String): Pair<String, Int>
{
	return Pair("", 1) // TODO
}

fun wrap_readmacros (o: Any, rmacs: ICons): Any
{
	var wraped = o
	var rest = rmacs
	while (! isnil(rest))
	{
		wraped = l(car(rest), wraped)
		rest = cdr(rest) as ICons
	}
	return wraped
}

fun lread (code: String): ICons
{
	var tree: ICons = nil
	var buff = ReadBuf("", nil)
	var idx = 0
	while (idx < code.length)
	{
		val c = code[idx]
		if ('(' == c)
		{
			val co = find_co_paren(code.substring((idx + 1)..(code.length)))
			tree = growth(tree, buff)
			tree = cons(wrap_readmacros(
						lread(code.substring((idx + 1)..(idx + co + 1)))
						, buff.rmacs), tree)
			buff = ReadBuf("", nil)
			idx += co + 1
		}
		else if (')' == c)
		{
			throw Erro(ErroId.Syntax, "found excess close parennthesis.")
		}
		else if ('[' == c)
		{
			val co = find_co_bracket(code.substring((idx + 1)..(code.length)))
			tree = growth(tree, buff)
			buff.tok = lread(code.substring((idx + 1)..(idx + co + 1)))
			tree = if (isnil(buff.rmacs))
			{
				cons(cons(Symb("vect"), buff.tok), tree)
			}
			else
			{
				cons(l(Symb("to-vect"), wrap_readmacros(buff.tok, buff.rmacs)), tree)
			}
			buff = ReadBuf("", nil)
			idx += co + 1
		}
		else if (']' == c)
		{
			throw Erro(ErroId.Syntax, "found excess close brackets.")
		}
		else if (' ' == c)
		{
			tree = growth(tree, buff)
		}
		else if (';' == c)
		{
			tree = growth(tree, buff)
			while (idx < code.length && '\n' != code.charAt(idx)) { ++idx }
		}
		else if ('"' == c)
		{
			tree = growth(tree, buff)
			(strn, inc) = take_string(code.substring((idx + 1)..(code.length)))
			idx += inc
			tree = cons(strn, tree)
			buff = ReadBuf("", nil)
		}
		else if ('\'' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb("quote"), buff.rmacs)
		}
		else if ('`' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb("quasiquote"), buff.rmacs)
		}
		else if (',' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb("unquote"), buff.rmacs)
		}
		else if ('@' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb("splicing"), buff.rmacs)
		}
		else if ('.' == c)
		{
			if (buff.tok.isEmpty())
			{
				return append(reverse(cdr(tree)), cons(car(tree)
							, car(lread(code.substring((idx + 1)..(code.length))))))
			}
			else
			{
				buff.tok += "."
			}
		}
		else
		{
			buff.tok += c
		}

		++idx
	}
	tree = growth(tree, buff)
	return reverse(tree)
}

fun lreadtop (code: String): ICons
{
	return cons(Symb("do"), lread(read))
}

fun leval (expr: ICons, env: ICons): Any
{
	return expr // TODO
}

fun lprint (expr: Any): String
{
	val dup = seek_dup(expr, nil, nil)
	var s = ""
	var idx = 0
	var rest = dup
	while (! isnil(rest))
	{
		s += "\$${idx} = ${lprint_rec(car(rest), dup, false)}\n"
		++idx
		rest = cdr(rest)
	}
	s += lprint_rec(expr, dup, true)
	return s
}

fun seek_dup (expr: Any, printed: ICons, dup: ICons): ICons
{
	if (! isnil(find(expr, printed))) { return cons(expr, dup) }
	if (! isnil(atom(expr))) { return dup }
	val pd = cons(expr,  printed)
	return append(seek_dup(car(expr), pd, dup), seek_dup(cdr(expr), pd, dup))
}

fun lprint_rec (expr: ICons, dup: ICons, rec: Boolean): String // TODO
{
	return ""
}

