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
		if (entr is Nil)
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
		if (exit is Nil) { return nil }

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
		if (entr is Nil)
		{
			entr = queu.entr
			exit = queu.exit
		}
		rplacd(entr as Cons, queu.exit)
		return this
	}
}

class Spfm (val proc: Function<*>, val name: String)

class Func (val args: ICons, val body: ICons, val env: ICons)

fun cons (a: Any, d: Any): Cons = Cons(a, d)
fun car (o: ICons): Any = o.car()
fun cdr (o: ICons): Any = o.cdr()
fun eq (a: Any, b: Any): Any = if (a === b) t else nil
fun atom (o: Any): Any = if (o is Cons) nil else t

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

fun last (o: ICons): ICons
{
	if (o is Cons)
	{
		var rest: ICons = o
		while (cdr(rest) is Cons)
		{
			rest = cdr(rest) as Cons
		}
		return rest
	}
	return nil	
}

fun nconc (colla: ICons, collb: ICons): ICons
{
	if (colla is Nil) { return collb; }
	val las = last(colla)
	rplacd(las as Cons, collb)
	return colla
}

fun nreverse (coll: ICons): ICons
{
	var ccoll: Any = coll
	var rev: ICons = nil
	while (ccoll is Cons)
	{
		val tmp = cdr(ccoll)
		rplacd(ccoll, rev)
		rev = ccoll
		ccoll = tmp
	}
	return rev
}

fun lif () {}// TODO
fun ldefine () {}
fun lsetq () {}
fun llambda () {}
fun lquote () {}

fun l (vararg args: Any): ICons = args.foldRight(nil
		, fun (e: Any, acc: ICons): ICons = cons(e, acc) )

fun find (v: Any, coll: ICons): Any
{
	var rest = coll
	while (rest is Cons)
	{
		if (v == car(rest)) { return t }
		rest = cdr(rest) as ICons
	}
	return nil
}

fun findidx_eq (v: Any, coll: ICons): Any
{
	var idx = 0
	var rest = coll
	while (rest is Cons)
	{
		if (v === car(rest)) { return idx }
		++idx
		rest = cdr(rest) as ICons
	}
	return nil
}


var genv = Cons(nil, nil)

data class ReadBuf (var tok: String, var rmacs: ICons)

fun growth (tree: ICons, buff: ReadBuf): ICons
{
	if (! buff.tok.isEmpty())
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
	while (rest is Cons)
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
			val invec = lread(code.substring((idx + 1)..(idx + co + 1)))
			tree = if (buff.rmacs is Nil)
			{
				cons(cons(Symb("vect"), invec), tree)
			}
			else
			{
				cons(l(Symb("to-vect"), wrap_readmacros(invec, buff.rmacs)), tree)
			}
			buff = ReadBuf("", nil)
			idx += co + 1
		}
		else if (']' == c)
		{
			throw Erro(ErroId.Syntax, "found excess close brackets.")
		}
		else if (' ' == c || '\t' == c || '\n' == c)
		{
			tree = growth(tree, buff)
		}
		else if (';' == c)
		{
			tree = growth(tree, buff)
			while (idx < code.length && '\n' != code[idx]) { ++idx }
		}
		else if ('"' == c)
		{
			tree = growth(tree, buff)
			val (strn, inc) = take_string(code.substring((idx + 1)..(code.length)))
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
				return nconc(nreverse(cdr(tree) as ICons)
						, cons(car(tree)
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
	return nreverse(tree)
}

fun lreadtop (code: String): ICons
{
	return cons(Symb("do"), lread(code))
}

fun leval (expr: ICons, env: ICons): Any
{
	return expr // TODO
}

fun lprint (expr: Any): String
{
	val dup = seek_dup(expr, nil, nil).second
	var s = ""
	var idx = 0
	var rest = dup
	while (rest is Cons)
	{
		s += "\$${idx} = ${lprint_rec(car(rest), dup, false)}\n"
		++idx
		rest = cdr(rest) as ICons
	}
	s += lprint_rec(expr, dup, true)
	return s
}

fun seek_dup (expr: Any, printed: ICons, dup: ICons): Pair<ICons, ICons>
{
	if (! (find(expr, printed) is Nil))
	{
		if (! (find(expr, dup) is Nil)) { return Pair(printed, cons(expr, dup)) }
		return Pair(printed, dup)
	}
	if (expr is Cons)
	{
		val (eprinted, edup) = seek_dup(car(expr), cons(expr, printed), dup)
		return seek_dup(cdr(expr), eprinted, edup)
	}
	if (expr is Queu)
	{
		return seek_dup(expr.exit, cons(expr, printed), dup)
	}
	if (expr is Array<*>)
	{
		return expr.fold(Pair<ICons, ICons>(cons(expr, printed), dup)
				, fun (res: Pair<ICons, ICons>, elm: Any?): Pair<ICons, ICons>
					= seek_dup(elm ?: "null", res.first, res.second))
	}
	if (expr is Erro)
	{
		return seek_dup(expr.estr, cons(expr, printed), dup)
	}
	return Pair(printed, dup)
}

fun lprint_rec (expr: Any, dup: ICons, rec: Boolean): String
{
	val idx = findidx_eq(expr, dup)
	if (rec && ! (idx is Nil)) { return "\$${idx}" }
	if (expr is Nil) { return "NIL" }
	if (expr is Cons) { return printcons_rec(expr, dup, true) }
	if (expr is Symb) { return expr.name }
	if (expr is String) { return "\"${expr}\"" }
	if (expr is Array<*>)
	{
		val s = expr.map { e -> lprint_rec(e ?: "null", dup, true) }
		return "[${s.joinToString(" ")}]"
	}
	if (expr is Queu) { return "/${lprint_rec(expr.exit, dup, true)}/" }
	if (expr is Func) { return "<Func ${lprint_rec(expr.args, dup,  true)} ${lprint_rec(expr.body, dup, true)}>" }
	if (expr is Spfm) { return "<Spfm ${expr.name}>" }
//	if (expr is Function<*>) { return "<Subr ${expr.name} >" } TODO
	return expr.toString()
}

fun printcons_rec (coll: Cons, dup: ICons, rec: Boolean): String
{
	val a = car(coll)
	val d = cdr(coll)
	if (d is Nil) { return "(${lprint_rec(a,  dup, rec)})" }
	if (! (d is Cons))
	{
		return "(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})"
	}
	if (! (find(d, dup) is Nil))
	{
		"(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})"
	}
	val dstr = lprint_rec(d, dup, rec)
	return "(${lprint_rec(a, dup, rec)} ${dstr.substring(1..(dstr.length))}"

}

