package senva
import java.io.File
import kotlin.system.exitProcess

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

class Symb ()
{
	var name: String
	init { this.name = "" }

	constructor (name: String) : this() { this.name = name }

	companion object
	{
		var identifiers = mutableMapOf<String, Symb>()
		fun intern (name: String): Symb
		{
			return identifiers.getOrElse(name)
			{
				val newsym = Symb(name)
				identifiers[name] = newsym
				newsym
			}
		}
	}
}

val nil = Nil()
val t = Symb.intern("T")

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

	constructor (c: ICons) : this()
	{
		this.entr = last(c) as ICons
		this.exit = c
	}

	init
	{
		entr = nil
		exit = entr
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
		else { exit = cdr(exit) as ICons }
		return e
	}

	fun concat (queu: Queu): Queu
	{
		if (entr is Nil)
		{
			entr = queu.entr
			exit = queu.exit
		}
		else if (! (queu.exit is Nil))
		{
			rplacd(entr as Cons, queu.exit)
			entr = queu.entr
		}
		return this
	}
}

class Subr (val proc: (ICons) -> Any, val name: String)

class Spfm (val proc: (ICons, ICons) -> Any, val name: String)

class Func (val args: Any, val body: Any, val env: ICons)

fun cons (a: Any, d: Any): Cons = Cons(a, d)
fun car (o: ICons): Any = o.car()
fun cdr (o: ICons): Any = o.cdr()
fun atom (o: Any): Any = if (o is Cons) nil else t
//fun eq (a: Any, b: Any): Any = if ((a === b) || (a is Symb && b is Symb && a.name == b.name)) t else nil
fun eq (a: Any, b: Any): Any = if (a === b) t else nil

fun equal (a: Any, b: Any): Any
{
	var cond: Boolean = false
	if (a === b) { cond = true }
	else if (a is Symb && b is Symb) { cond = (a.name == b.name) }
	else if (a is Cons && b is Cons)
	{
		cond = (! (equal(a.a, b.a) is Nil || equal(a.d, b.d) is Nil))
	}
	else if (a is Queu && b is Queu)
	{
		cond = if (equal(a.exit, b.exit) is Nil) false else true
	}
	else if (a is Func && b is Func)
	{
		cond = (! (equal(a.args, b.args) is Nil
					|| equal(a.body, b.body) is Nil
					|| equal(a.env, b.env) is Nil))
	}
	else if (a is Spfm && b is Spfm)
	{
		cond = (! (equal(a.proc, b.proc) is Nil || equal(a.name, b.name) is Nil))
	}
	else if (a is MutableList<*> && b is MutableList<*>)
	{
		if (a.size == b.size)
		{
			cond = true
			for (idx in 0..(a.size - 1))
			{
				if (equal(a[idx]!!, b[idx]!!) is Nil)
				{
					cond = false
					break
				}
			}
		}
	}
	else if (a is Number && b is Number)
	{
		cond = (a.toDouble() == b.toDouble())
	}
	else { cond = (a == b) }
	return if (cond) t else nil
}

//fun equal (a: Any, b: Any): Any
//{
//	var cond: Any = nil
//	if (a === b) { cond = t }
//	else if (a is Symb && b is Symb) { cond = if (a.name == b.name) t else nil }
//	else if (a is Cons && b is Cons)
//	{
//		cond = if (! (equal(a.a, b.a) is Nil || equal(a.d, b.d) is Nil)) t else nil
//	}
//	else if (a is Queu && b is Queu) { cond = equal(a.exit, b.exit) }
//	else if (a is Func && b is Func)
//	{
//		cond = if (! (equal(a.args, b.args) is Nil
//					|| equal(a.body, b.body) is Nil
//					|| equal(a.env, b.env) is Nil)) t else nil
//	}
//	else if (a is Spfm && b is Spfm)
//	{
//		cond = if (! (equal(a.proc, b.proc) is Nil || equal(a.name, b.name) is Nil)) t else nil
//	}
//	else if (a is MutableList<*> && b is MutableList<*>)
//	{
//		if (a.size == b.size)
//		{
//			cond = t
//			for (idx in 0..(a.size - 1))
//			{
//				if (equal(a[idx]!!, b[idx]!!) is Nil)
//				{
//					cond = nil
//					break
//				}
//			}
//		}
//	}
//	else if (a is Number && b is Number)
//	{
//		cond = if (a.toDouble() == b.toDouble()) t else nil
//	}
//	else { cond = if (a == b) t else nil }
//	return cond
//}

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

fun ladd (nums: ICons): Number
{
	var iacc: Long = 0
	var facc: Double = 0.0
	var isint = true
	var rest: Any = nums
	while (rest is Cons)
	{
		val n = car(rest)
		if (n is Int) { iacc += n }
		else if (n is Long) { iacc += n }
		else if (n is Byte) { iacc += n }
		else if (n is Short) { iacc += n }
		else if (n is Double)
		{
			facc += n
			isint = false
		}
		else if (n is Float)
		{
			facc += n
			isint = false
		}
		else { throw Erro(ErroId.Type, "cannot add ${lprint(nums)}") }
		rest = cdr(rest)
	}
	if (isint) { return iacc }
	return iacc + facc
}

fun lsub (head: Any, nums: ICons): Number
{
	if (head is Number)
	{
		var isint = true
		var iacc: Long = 0
		var facc: Double = 0.0
		if (head is Int) { iacc = head.toLong() }
		else if (head is Long) { iacc = head }
		else if (head is Byte) { iacc = head.toLong() }
		else if (head is Short) { iacc = head.toLong() }
		else if (head is Double)
		{
			facc = head
			isint = false
		}
		else if (head is Float)
		{
			facc = head.toDouble()
			isint = false
		}
		var rest: Any = nums
		while (rest is Cons)
		{
			val n = car(rest)
			if (n is Int) { iacc -= n }
			else if (n is Long) { iacc -= n }
			else if (n is Byte) { iacc -= n }
			else if (n is Short) { iacc -= n }
			else if (n is Double)
			{
				facc -= n
				isint = false
			}
			else if (n is Float)
			{
				facc -= n
				isint = false
			}
			else
			{
				throw Erro(ErroId.Type, "cannot sub ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		if (isint) { return iacc }
		return iacc + facc
	}
	else
	{
		throw Erro(ErroId.Type, "cannot sub ${lprint(cons(head, nums))}")
	}
}

fun lmul (nums: ICons): Number
{
	var isint = true
	var iacc: Long = 1
	var facc: Double = 1.0
	var rest: Any = nums
	while (rest is Cons)
	{
		val n = car(rest)
		if (n is Int) { iacc *= n }
		else if (n is Long) { iacc *= n }
		else if (n is Byte) { iacc *= n }
		else if (n is Short) { iacc *= n }
		else if (n is Double)
		{
			facc *= n
			isint = false
		}
		else if (n is Float)
		{
			facc *= n
			isint = false
		}
		else { throw Erro(ErroId.Type, "cannot mul ${lprint(nums)}") }
		rest = cdr(rest)
	}
	if (isint) { return iacc }
	return iacc * facc
}

fun ldiv (head: Any, nums: ICons): Number
{
	if (head is Number)
	{
		var isint = true
		var iacc: Long = 1
		var facc: Double = 1.0
		if (head is Int) { iacc = head.toLong() }
		else if (head is Long) { iacc = head }
		else if (head is Byte) { iacc = head.toLong() }
		else if (head is Short) { iacc = head.toLong() }
		else if (head is Double)
		{
			facc = head
			isint = false
		}
		else if (head is Float)
		{
			facc = head.toDouble()
			isint = false
		}

		var rest: Any = nums
		while (rest is Cons)
		{
			val n = car(rest)
			if (n is Int)
			{
				if (iacc % n == 0L) { iacc /= n }
				else
				{
					facc /= n
					isint = false
				}
			}
			else if (n is Long)
			{
				if (iacc % n == 0L) { iacc /= n }
				else
				{
					facc /= n
					isint = false
				}
			}
			else if (n is Byte)
			{
				if (iacc % n == 0L) { iacc /= n }
				else
				{
					facc /= n
					isint = false
				}
			}
			else if (n is Short)
			{
				if (iacc % n == 0L) { iacc /= n }
				else
				{
					facc /= n
					isint = false
				}
			}
			else if (n is Double)
			{
				facc /= n
				isint = false
			}
			else if (n is Float)
			{
				facc /= n
				isint = false
			}
			else
			{
				throw Erro(ErroId.Type, "cannot div ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		if (isint) { return iacc }
		return iacc * facc
	}
	else { throw Erro(ErroId.Type, "cannot div ${lprint(cons(head, nums))}") }
}

fun lmod (a: Any, b: Any): Long
{
	val na: Long = when (a)
	{
		is Int -> a.toLong()
		is Long -> a
		is Byte -> a.toLong()
		is Short -> a.toLong()
		else -> throw Erro(ErroId.Type, "cannot mod ${lprint(l(a, b))}")
	}

	val nb: Long = when (b)
	{
		is Int -> b.toLong()
		is Long -> b
		is Byte -> b.toLong()
		is Short -> b.toLong()
		else -> throw Erro(ErroId.Type, "cannot mod ${lprint(l(a, b))}")
	}

	return na % nb
}

fun lgt (head: Any, nums: ICons): Any
{
	if (head is Number)
	{
		var a = head.toDouble()
		var rest: Any = nums
		while (rest is Cons)
		{
			if (car(rest) is Number)
			{
				val n = (car(rest) as Number).toDouble()
				if (a > n) { a = n }
				else { return nil }
			}
			else
			{
				throw Erro(ErroId.Type, "cannot gt ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		return t
	}
	throw Erro(ErroId.Type, "cannot gt ${lprint(cons(head, nums))}")
}

fun llt (head: Any, nums: ICons): Any
{
	if (head is Number)
	{
		var a = head.toDouble()
		var rest: Any = nums
		while (rest is Cons)
		{
			if (car(rest) is Number)
			{
				val n = (car(rest) as Number).toDouble()
				if (a < n) { a = n }
				else { return nil }
			}
			else
			{
				throw Erro(ErroId.Type, "cannot lt ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		return t
	}
	throw Erro(ErroId.Type, "cannot lt ${lprint(cons(head, nums))}")
}

fun lge (head: Any, nums: ICons): Any
{
	if (head is Number)
	{
		var a = head.toDouble()
		var rest: Any = nums
		while (rest is Cons)
		{
			if (car(rest) is Number)
			{
				val n = (car(rest) as Number).toDouble()
				if (a < n) { return nil }
				a = n
			}
			else
			{
				throw Erro(ErroId.Type, "cannot ge ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		return t
	}
	throw Erro(ErroId.Type, "cannot ge ${lprint(cons(head, nums))}")
}

fun lle (head: Any, nums: ICons): Any
{
	if (head is Number)
	{
		var a = head.toDouble()
		var rest: Any = nums
		while (rest is Cons)
		{
			if (car(rest) is Number)
			{
				val n = (car(rest) as Number).toDouble()
				if (a > n) { return nil }
				a = n
			}
			else
			{
				throw Erro(ErroId.Type, "cannot le ${lprint(cons(head, nums))}")
			}
			rest = cdr(rest)
		}
		return t
	}
	throw Erro(ErroId.Type, "cannot le ${lprint(cons(head, nums))}")
}

fun lint (o: Any): Long
{
	if (o is Number) { return o.toLong() }
	throw Erro(ErroId.Type, "cannot cast ${lprint(o)} to InumT.")
}

fun lfloat (o: Any): Double
{
	if (o is Number) { return o.toDouble() }
	throw Erro(ErroId.Type, "cannot cast ${lprint(o)} to FnumT.")
}

fun ltype (o: Any): Symb
{
	if (o is Cons) { return Symb.intern("<cons>") }
	if (o is Func) { return Symb.intern("<func>") }
	if (o is Spfm) { return Symb.intern("<spfm>") }
	if (o is Subr) { return Symb.intern("<subr>") }
	if (o is Symb) { return Symb.intern("<symb>") }
	if (o is String) { return Symb.intern("<strn>") }
	if (o is Int || o is Long || o is Byte || o is Short) { return Symb.intern("<inum>") }
	if (o is Double || o is Float) { return Symb.intern("<fnum>") }
	if (o is Nil) { return Symb.intern("<nil>") }
	if (o is MutableList<*>) { return Symb.intern("<vect>") }
	if (o is Queu) { return Symb.intern("<queu>") }
	return Symb.intern("<kotlin ${o.javaClass.kotlin.simpleName}>")
}

fun last (o: Any): Any
{
	if (o is Cons)
	{
		var rest: ICons = o
		while (cdr(rest) is Cons) { rest = cdr(rest) as Cons }
		return rest
	}
	if (o is Nil) { return nil }
	if (o is Queu) { return o.entr }
	if (o is Symb) { return Symb.intern(o.name[o.name.length - 1].toString()) }
	if (o is String) { return o[o.length - 1].toString() }
	if (o is MutableList<*>) { return mutableListOf(o[o.size - 1]) }
	throw Erro(ErroId.Type, "cannot apply last to ${lprint(o)}")
}

fun nconc (colla: ICons, collb: ICons): ICons
{
	if (colla is Nil) { return collb }
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

fun lload (path: String): Any
{
	val fin = File(path)
	if (! fin.exists())
	{
		throw Erro(ErroId.FileNotFound, "not found file: ${lprint(path)}")
	}
	return leval(lreadtop(fin.readText()), genv)
}

fun to_list (obj: Any): ICons
{
	if (obj is MutableList<*>) { return vect2cons(obj) }
	if (obj is Symb)
	{
		return vect2cons(obj.name.toCharArray().map({e -> e.toLong()}).toMutableList())
	}
	if (obj is String)
	{
		return vect2cons(obj.toCharArray().map({e -> e.toLong()}).toMutableList())
	}
	if (obj is Queu) { return obj.exit }
	if (obj is ICons) { return obj }
	throw Erro(ErroId.Type, "cannot cast ${lprint(obj)} to ConsT.")
}

fun to_vect (obj: Any): MutableList<Any>
{
	if (obj is ICons) { return cons2vect(obj) }
	if (obj is Symb)
	{
		return obj.name.toCharArray().map({e -> e.toLong()}).toMutableList()
	}
	if (obj is String)
	{
		return obj.toCharArray().map({e -> e.toLong()}).toMutableList()
	}
	if (obj is Queu) { return cons2vect(obj.exit) }
	if (obj is MutableList<*>) { return obj as MutableList<Any> }
	throw Erro(ErroId.Type, "cannot cast ${lprint(obj)} to VectT.")
}

fun to_queu (obj: Any): Queu
{
	if (obj is Cons) { return Queu(obj) }
	if (obj is Symb)
	{
		return Queu(vect2cons(obj.name.toCharArray().map({e -> e.toLong()}).toMutableList()))
	}
	if (obj is String)
	{
		return Queu(vect2cons(obj.toCharArray().map({e -> e.toLong()}).toMutableList()))
	}
	if (obj is MutableList<*>) { return Queu(vect2cons(obj as MutableList<Any>)) }
	if (obj is Queu) { return obj }
	if (obj is Nil) { return Queu() }
	throw Erro(ErroId.Type, "cannot cast ${lprint(obj)} to QueuT.")
}

fun symbol (obj: Any): Symb
{
	if (obj is ICons)
	{
		var rest: Any = obj
		var strn = ""
		while (rest is Cons)
		{
			strn += (car(rest) as Number).toChar()
			rest = cdr(rest)
		}
		return Symb.intern(strn)
	}
	if (obj is Queu)
	{
		return Symb.intern(cons2vect(obj.exit).map(
					{e -> (e as Number).toChar()}).joinToString(""))
	}
	if (obj is MutableList<*>)
	{
		return Symb.intern(obj.map({e -> (e as Number).toChar()}).joinToString(""))
	}
	if (obj is String) { return Symb.intern(obj) }
	if (obj is Symb) { return obj }
	throw Erro(ErroId.Type, "cannot cast ${lprint(obj)} to SymbT.")
}

fun lsprint (args: ICons): String
{
	var strn = ""
	var rest: Any = args
	while (rest is Cons)
	{
		val e = car(rest)
		strn += if (e is String) e else lprint(e)
		rest = cdr(rest)
	}
	return strn
}

fun tee (obj: Any): Any
{
	println(lprint(obj))
	return obj
}

fun nth (c: ICons, n: Long): Any
{
	var rest: Any = c
	var idx = 0L
	while (idx < n)
	{
		if (rest is ICons) { rest = cdr(rest) }
		else
		{
			throw Erro(ErroId.ArgsUnmatch, "cannot got nth ${n} from ${lprint(c)}")
		}
		++idx
	}
	if (rest is ICons) { return car(rest) }
	throw Erro(ErroId.ArgsUnmatch, "cannot got nth ${n} from ${lprint(c)}")
}

fun lif (env: ICons, args: ICons): Any
{
	return leval(nth(args, if (leval(car(args), env) is Nil) 2 else 1), env)
}

fun ldefine (env: ICons, args: ICons): Symb
{
	val sym = car(args)
	if (sym is Symb)
	{
		val asc = assoc(car(genv) as ICons, sym)
		if (asc === null)
		{
			rplaca(genv, cons(cons(sym, leval(nth(args, 1), env)), car(genv)))
		}
		else { rplacd(asc, leval(nth(args, 1), env)) }
		return sym
	}
	throw Erro(ErroId.Type, "cannot define ${lprint(sym)}, must be SymbT.")
}

fun lsetq (env: ICons, args: ICons): Any
{
	val sym = car(args)
	if (sym is Symb)
	{
		var rest: Any = env
		while (rest is Cons)
		{
			val asc = assoc(car(rest) as ICons, sym)
			if (asc !== null)
			{
				rplacd(asc, leval(nth(args, 1), env))
				return cdr(asc)
			}
			rest = cdr(rest)
		}
		throw Erro(ErroId.Symbol, "${lprint(sym)} is not defined.")
	}
	throw Erro(ErroId.Type, "cannot setq ${lprint(sym)}, must be SymbT.")
}

fun land (env: ICons, args: ICons): Any
{
	var ret: Any = t
	var rest: Any = args
	while (rest is Cons)
	{
		ret = leval(car(rest), env)
		if (ret is Nil) { return nil }
		rest = cdr(rest)
	}
	return ret
}

fun lor (env: ICons, args: ICons): Any
{
	var ret: Any = nil
	var rest: Any = args
	while (rest is Cons)
	{
		ret = leval(car(rest), env)
		if (! (ret is Nil)) { return ret }
		rest = cdr(rest)
	}
	return nil
}

fun ldo (env: ICons, args: ICons): Any
{
	var rest: Any = args
	while (rest is Cons && cdr(rest) is Cons)
	{
		leval(car(rest), env)
		rest = cdr(rest)
	}
	return leval(car(rest as ICons), env)
}

fun expand_quasiquote (expr: Any, env: ICons): Any
{
	if (expr is Cons)
	{
		val sym = car(expr)
		if (sym is Symb && sym.name == "unquote")
		{
			return leval(car(cdr(expr) as ICons), env)
		}
		var eexpr: ICons = nil
		var rest = expr
		while (rest is Cons)
		{
			var is_splicing = false
			val e = car(rest)
			if (e is Cons)
			{
				val esym = car(e)
				if (esym is Symb && esym.name == "splicing")
				{
					is_splicing = true
					var sexpr = leval(car(cdr(car(rest) as ICons) as ICons), env)
					while (sexpr is Cons)
					{
						eexpr = cons(car(sexpr), eexpr)
						sexpr = cdr(sexpr)
					}
				}
			}
			if (! is_splicing)
			{
				eexpr = cons(expand_quasiquote(car(rest), env), eexpr)
			}
			rest = cdr(rest)
		}
		return nreverse(eexpr)
	}
	return expr
}

fun l (vararg args: Any): ICons = args.foldRight(nil
		, fun (e: Any, acc: ICons): ICons = cons(e, acc) )

fun mapeval (args: ICons, env: ICons): ICons
{
	var eargs: ICons = nil
	var rest: Any = args
	while (rest is Cons)
	{
		eargs = cons(leval(car(rest), env), eargs)
		rest = cdr(rest)
	}
	return nreverse(eargs)
}

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



data class ReadBuf (var tok: String, var rmacs: ICons)

fun growth (tree: ICons, buff: ReadBuf): ICons
{
	val buf: String = buff.tok
	val rmacs = buff.rmacs
	if (buf.isEmpty()) { return tree }

	buff.tok = ""
	buff.rmacs = nil
	if ("nil" == buf || "NIL" == buf)
	{
		return cons(wrap_readmacros(nil, rmacs), tree)
	}
	val inum = buf.toLongOrNull(10) 
	if (inum != null) { return cons(wrap_readmacros(inum, rmacs), tree) }
	val fnum = buf.toDoubleOrNull() 
	if (fnum != null) { return cons(wrap_readmacros(fnum, rmacs), tree) }

	return cons(wrap_readmacros(Symb.intern(buf), rmacs), tree)
}

fun find_co_paren (code: String): Int
{
	var sflg = false
	var eflg = false
	var layer = 1
	for (idx in 0..(code.length - 1))
	{
		val c = code[idx]
		if (eflg) { eflg = false }
		else if (! sflg && '(' == c) { ++layer }
		else if (! sflg && ')' == c) { --layer }
		else if ('\\' == c)
		{
			eflg = true
			continue
		}
		else if ('"' == c) { sflg = ! sflg }

		if (layer < 1) { return idx }
	}
	throw Erro(ErroId.Syntax, "not found close parenthesis.")
}

fun find_co_bracket (code: String): Int
{
	var sflg = false
	var eflg = false
	var layer = 1
	for (idx in 0..(code.length - 1))
	{
		val c = code[idx]
		if (eflg) { eflg = false }
		if (! sflg && '[' == c) { ++layer }
		else if (! sflg && ']' == c) { --layer }
		else if ('\\' == c)
		{
			eflg = true
			continue
		}
		else if ('"' == c) { sflg = ! sflg }

		if (layer < 1) { return idx }
	}
	throw Erro(ErroId.Syntax, "not found close brackets.")
}

val escape_char_table = mapOf(
		'a' to '\u0007'
		, 'b' to '\b'
		, 'f' to '\u000C'
		, 'n' to '\n'
		, 'r' to '\r'
		, 't' to '\t'
		, 'v' to '\u000B'
		, '0' to '\u0000'
		)

fun take_string (code: String): Pair<String, Int>
{
	var eflg = false
	var strn = ""
	for (idx in 0..(code.length - 1))
	{
		var c: Char = code[idx]
		if (eflg)
		{
			if (escape_char_table.containsKey(c)) { c = escape_char_table[c]!! }
			eflg = false
		}
		else if ('\\' == c)
		{
			eflg = true
			continue
		}
		else if ('"' == c) { return Pair(strn, idx + 1) }
		strn += c
	}
	throw Erro(ErroId.Syntax, "not found close double quote.")
}

fun cons2vect (c: ICons): MutableList<Any>
{
	var arr = mutableListOf<Any>()
	var rest: Any = c
	while (rest is Cons)
	{
		arr.add(car(rest))
		rest = cdr(rest)
	}
	return arr
}

fun<T> vect2cons (l: MutableList<T>): ICons
{
	return l.foldRight(nil, fun (e, acc): ICons = cons(e as Any, acc))
}

fun bind_tree (treea: Any, treeb: Any): ICons
{
	if (treea is Nil) { return nil }
	if (! (treea is Cons)) { return l(cons(treea, treeb)) }
	if (! (treeb is ICons))
	{
		throw Erro(ErroId.Syntax
				, "cannot bind: ${lprint(treea)} and ${lprint(treeb)}")
	}
	try
	{
		return nconc(bind_tree(car(treea), car(treeb))
				, bind_tree(cdr(treea), cdr(treeb)))
	}
	catch (erro: Erro)
	{
		throw Erro(ErroId.Syntax
				, "cannot bind: ${lprint(treea)} and ${lprint(treeb)}")
	}
}

fun assoc (alist: ICons, key: Any): Cons?
{
	var rest: Any = alist
	while (rest is Cons)
	{
		val e = car(rest)
		if (e is Cons) { if (! (equal(car(e as ICons), key) is Nil)) { return e } }
		rest = cdr(rest)
	}
	return null
}

fun assocdr (alist: ICons, key: Any): Any?
{
	return assoc(alist, key)?.let { cdr(it as ICons) }
}

fun seekenv (env: ICons, sym: Symb): Any
{
	var rest: Any = env
	while (rest is Cons)
	{
		assocdr(car(rest) as ICons, sym)?.let { return it }
		rest = cdr(rest)
	}
	throw Erro(ErroId.Symbol, "${lprint(sym)} is not defined.")
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
			val co = find_co_paren(code.substring((idx + 1)..(code.length - 1)))
			tree = growth(tree, buff)
			tree = cons(wrap_readmacros(
						lread(code.substring((idx + 1)..(idx + co)))
						, buff.rmacs), tree)
			buff = ReadBuf("", nil)
			idx += co + 1
		}
		else if (')' == c)
		{
			throw Erro(ErroId.Syntax, "found excess close parenthesis.")
		}
		else if ('[' == c)
		{
			val co = find_co_bracket(code.substring((idx + 1)..(code.length - 1)))
			tree = growth(tree, buff)
			val invec = lread(code.substring((idx + 1)..(idx + co)))
			tree = if (buff.rmacs is Nil)
			{
				cons(cons(Symb.intern("vect"), invec), tree)
			}
			else
			{
				cons(l(Symb.intern("to-vect"), wrap_readmacros(invec, buff.rmacs)), tree)
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
			val (strn, inc) = take_string(code.substring((idx + 1)..(code.length - 1)))
			idx += inc
			tree = cons(strn, tree)
			buff = ReadBuf("", nil)
		}
		else if ('\'' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb.intern("quote"), buff.rmacs)
		}
		else if ('`' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb.intern("quasiquote"), buff.rmacs)
		}
		else if (',' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb.intern("unquote"), buff.rmacs)
		}
		else if ('@' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb.intern("splicing"), buff.rmacs)
		}
		else if ('^' == c)
		{
			tree = growth(tree, buff)
			buff.rmacs = cons(Symb.intern("tee"), buff.rmacs)
		}
		else if ('.' == c)
		{
			if (buff.tok.isEmpty())
			{
				return nconc(nreverse(cdr(tree) as ICons)
						, cons(car(tree)
							, car(lread(code.substring((idx + 1)..(code.length - 1))))))
			}
			buff.tok += "."
		}
		else { buff.tok += c }

		++idx
	}
	tree = growth(tree, buff)
	return nreverse(tree)
}

fun lreadtop (code: String): ICons
{
	return cons(Symb.intern("do"), lread(code))
}

fun leval (expr_: Any, env_: ICons): Any
{
	var expr = expr_
	var env = env_
	while (true)
	{
		if (expr is Cons)
		{
			val args = cdr(expr) as ICons
			val proc = leval(car(expr), env)
			if (proc is Func)
			{
				expr = proc.body
				env = cons(bind_tree(proc.args, mapeval(args, env)), proc.env)
			}
			else if (proc is Spfm)
			{
				if ("if" == proc.name)
				{
					expr = nth(args, if (leval(car(args), env) is Nil) 2 else 1)
				}
				else if ("do" == proc.name)
				{
					var rest = args
					while (cdr(rest) is Cons)
					{
						leval(car(rest), env)
						rest = cdr(rest) as ICons
					}
					expr = car(rest)
				}
				else if ("!" == proc.name)
				{
					expr = lapply(leval(car(args), env), cdr(args) as ICons)
				}
				else { return proc.proc(env, args) }
			}
			else if (proc is Subr) { return lapply(proc, mapeval(args, env)) }
			else
			{
				throw Erro(ErroId.UnCallable, "${lprint(proc)} is not callable.")
			}
		}
		else if (expr is Symb) { return seekenv(env, expr) }
		else { return expr }
	}
}

fun lapply (proc: Any, args: ICons): Any
{
	if (proc is Func)
	{
		return leval(proc.body, cons(bind_tree(proc.args, args), proc.env))
	}
	if (proc is Subr) { return proc.proc(args) }
	throw Erro(ErroId.UnCallable, "${lprint(proc)} is not callable.")
}

fun lthrow (eid: Int, estr: String)
{
	throw Erro(eid, estr)
}

fun lcatch (env: ICons, args: ICons): Any
{
	val excep = leval(car(args), env)
	try
	{
		return leval(car(cdr(args) as ICons), env)
	}
	catch (erro: Erro)
	{
		return lapply(excep, l(erro.eid, leval(erro.estr, env)))
	}
}

fun lempty (coll: Any): Any
{
	if (coll is Nil) { return t }
	if (coll is MutableList<*>) { return if (coll.size < 1) t else nil }
	if (coll is String) { return if (coll.length < 1) t else nil }
	if (coll is Queu) { return if (coll.exit is Nil) t else nil }
	if (coll is Symb) { return if (coll.name.length < 1) t else nil }
	return nil
}

fun llprin (objs: ICons): Nil
{
	var rest: Any = objs
	while (rest is Cons)
	{
		val a = car(rest)
		print(if (a is String) a else lprint(a))
		rest = cdr(rest)
	}
	return nil
}

fun llprint (objs: ICons): Nil
{
	llprin(objs)
	println("")
	return nil
}

fun lgetat (vect: Any, idx: Number): Any
{
	if (vect is MutableList<*>) { return vect[idx.toInt()] ?: mutableListOf<Any>() }
	if (vect is String) { return Character.toString(vect[idx.toInt()]) ?: "" }
	if (vect is Symb) { return Symb.intern(Character.toString(vect.name[idx.toInt()]) ?: "") }
	throw Erro(ErroId.Type, "cannot apply getat to ${lprint(vect)}")
}

fun lsetat (vect: Any, idx: Number, valu: Any): Any
{
	if (vect is MutableList<*>)
	{
		(vect as MutableList<Any>)[idx.toInt()]  = valu
		return vect
	}
	if (vect is String)
	{
		return vect.substring(0..(idx.toInt() - 1)) + when (valu)
			{
				is Int   -> valu.toChar()
				is Long  -> valu.toChar()
				is Byte  -> valu.toChar()
				is Short -> valu.toChar()
				is String -> valu[0]
				is Symb -> valu.name[0]
				else -> throw Erro(ErroId.Type
						, "cannot setat ${lprint(valu)} to ${lprint(vect)}")
			} + vect.substring((idx.toInt() + 1)..(vect.length - 1))
	}
	if (vect is Symb)
	{
		return Symb.intern(vect.name.substring(0..(idx.toInt() - 1)) + when (valu)
			{
				is Int   -> valu.toChar()
				is Long  -> valu.toChar()
				is Byte  -> valu.toChar()
				is Short -> valu.toChar()
				is String -> valu[0]
				is Symb -> valu.name[0]
				else -> throw Erro(ErroId.Type
						, "cannot setat ${lprint(valu)} to ${lprint(vect)}")
			} + vect.name.substring((idx.toInt() + 1)..(vect.name.length - 1)))
	}
	throw Erro(ErroId.Type, "cannot apply setat to ${lprint(vect)}")
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
	if (! (findidx_eq(expr, printed) is Nil))
	{
		return Pair(printed, if (findidx_eq(expr, dup) is Nil) cons(expr, dup) else dup)
	}
	if (expr is Cons)
	{
		val (eprinted, edup) = seek_dup(car(expr), cons(expr, printed), dup)
		return seek_dup(cdr(expr), eprinted, edup)
	}
	if (expr is Queu) { return seek_dup(expr.exit, cons(expr, printed), dup) }
	if (expr is MutableList<*>)
	{
		return expr.fold(Pair<ICons, ICons>(cons(expr, printed), dup)
				, fun (res: Pair<ICons, ICons>, elm: Any?): Pair<ICons, ICons>
					= seek_dup(elm ?: "null", res.first, res.second))
	}
	if (expr is Erro) { return seek_dup(expr.estr, cons(expr, printed), dup) }
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
	if (expr is MutableList<*>)
	{
		val s = expr.map { e -> lprint_rec(e ?: "null", dup, true) }
		return "[${s.joinToString(" ")}]"
	}
	if (expr is Queu) { return "/${lprint_rec(expr.exit, dup, true)}/" }
	if (expr is Func)
	{
		return "<Func ${lprint_rec(expr.args, dup,  true)} ${lprint_rec(expr.body, dup, true)}>"
	}
	if (expr is Spfm) { return "<Spfm ${expr.name}>" }
	if (expr is Subr) { return "<Subr ${expr.name}>" }
	return expr.toString()
}

fun printcons_rec (coll: Cons, dup: ICons, rec: Boolean): String
{
	val a = car(coll)
	val d = cdr(coll)
	if (d is Nil) { return "(${lprint_rec(a, dup, rec)})" }
	if (! (d is Cons))
	{
		return "(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})"
	}
	if (! (findidx_eq(d, dup) is Nil))
	{
		return "(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})"
	}
	val dstr = lprint_rec(d, dup, rec)
	return "(${lprint_rec(a, dup, rec)} ${dstr.substring(1..(dstr.length - 1))}"

}

fun regist (env: Cons, name: String, obj: Any)
{
	rplaca(env, cons(cons(Symb.intern(name), obj), car(env)))
}

fun<T, R> regist_subr1 (env: Cons, name: String, proc: (T) -> R)
{
	fun subr_ (args: ICons): Any
	{
		try
		{
			return proc(car(args) as T) as Any
		}
		catch (e: java.lang.ClassCastException)
		{
			throw Erro(ErroId.Type, "cannot apply ${name} to ${lprint(car(args))}")
		}
	}
	regist(env, name, Subr(::subr_, name))
}

fun<T1, T2, R> regist_subr2 (env: Cons, name: String, proc: (T1, T2) -> R)
{
	fun subr_ (args: ICons): Any
	{
		try
		{
			return proc(car(args) as T1, car(cdr(args) as ICons) as T2) as Any
		}
		catch (e: java.lang.ClassCastException)
		{
			throw Erro(ErroId.Type, "cannot apply ${name} to ${lprint(args)}")
		}
	}
	regist(env, name, Subr(::subr_, name))
}

fun<T1, T2, T3, R> regist_subr3 (env: Cons, name: String, proc: (T1, T2, T3) -> R)
{
	fun subr_ (args: ICons): Any
	{
		try
		{
			return proc(car(args) as T1, car(cdr(args) as ICons) as T2, car(cdr(cdr(args) as ICons) as ICons) as T3) as Any
		}
		catch (e: java.lang.ClassCastException)
		{
			throw Erro(ErroId.Type, "cannot apply ${name} to ${lprint(args)}")
		}
	}
	regist(env, name, Subr(::subr_, name))
}

fun make_genv (): Cons
{
	var genv = Cons(nil, nil)
	regist(genv, "nil", nil)
	regist(genv, "t", t)
	regist(genv, "T", t)
	regist_subr2(genv, "cons", ::cons)
	regist_subr1(genv, "car", ::car)
	regist_subr1(genv, "cdr", ::cdr)
	regist_subr2(genv, "eq", ::eq)
	regist_subr2(genv, "equal", ::equal)
	regist_subr1(genv, "atom", ::atom)
	regist(genv, "list", Subr({args: ICons -> args}, "list"))
	regist(genv, "+", Subr({args: ICons -> ladd(args)}, "+"))
	regist(genv, "-", Subr({args: ICons -> lsub(car(args), cdr(args) as ICons)}, "-"))
	regist(genv, "*", Subr({args: ICons -> lmul(args)}, "*"))
	regist(genv, "/", Subr({args: ICons -> ldiv(car(args), cdr(args) as ICons)}, "/"))
	regist_subr2(genv, "%", ::lmod)
	regist(genv, ">", Subr({args: ICons -> lgt(car(args), cdr(args) as ICons)}, ">"))
	regist(genv, "<", Subr({args: ICons -> llt(car(args), cdr(args) as ICons)}, "<"))
	regist(genv, ">=", Subr({args: ICons -> lge(car(args), cdr(args) as ICons)}, ">="))
	regist(genv, "<=", Subr({args: ICons -> lle(car(args), cdr(args) as ICons)}, "<="))
	regist_subr1(genv, "int", ::lint)
	regist_subr1(genv, "float", ::lfloat)

	regist_subr2(genv, "rplaca", ::rplaca)
	regist_subr2(genv, "rplacd", ::rplacd)
	regist_subr1(genv, "last", ::last)
	regist_subr2(genv, "nconc", ::nconc)
	regist_subr1(genv, "nreverse", ::nreverse)
	regist(genv, "vect", Subr({args: ICons -> cons2vect(args)}, "vect"))

	regist(genv, "queu", Subr({args: ICons -> Queu(args)}, "queu"))
	regist_subr2(genv, "pushqueu", {queu: Queu, v: Any -> queu.push(v)})
	regist_subr1(genv, "popqueu", {queu: Queu -> queu.pop()})
	regist_subr2(genv, "concqueu", {qa: Queu, qb: Queu -> qa.concat(qb)})
	regist_subr1(genv, "to-list", ::to_list)
	regist_subr1(genv, "to-vect", ::to_vect)
	regist_subr1(genv, "to-queu", ::to_queu)
	regist_subr1(genv, "symbol", ::symbol)
	regist(genv, "sprint", Subr(::lsprint, "sprint"))
	regist_subr2(genv, "apply", ::lapply)
	regist_subr2(genv, "throw", ::lthrow)
	regist_subr1(genv, "empty", ::lempty)
	regist(genv, "print", Subr({args: ICons -> llprint(args)}, "print"))
	regist(genv, "prin", Subr({args: ICons -> llprin(args)}, "prin"))
	regist_subr1(genv, "type", ::ltype)
	regist_subr1(genv, "load", ::lload)
	regist_subr2(genv, "getat", ::lgetat)
	regist_subr3(genv, "setat", ::lsetat)
	regist(genv, "processor", Subr({Symb.intern("kotlin")}, "processor"))
	regist_subr1(genv, "tee", ::tee)
	regist(genv, "exit", Subr({args: ICons -> exitProcess(0)}, "exit"))

	regist(genv, "quote", Spfm({env: ICons, args: ICons -> car(args)}, "quote"))

	regist(genv, "quasiquote", Spfm(
				{env: ICons, args: ICons -> expand_quasiquote(car(args), env)}
				, "quasiquote"))
	regist(genv, "if", Spfm(::lif, "if"))
	regist(genv, "lambda"
			, Spfm({env: ICons, args: ICons -> Func(car(args), nth(args, 1), env)}
				, "lambda"))
	regist(genv, "define", Spfm(::ldefine, "define"))
	regist(genv, "setq", Spfm(::lsetq, "setq"))

	regist(genv, "and", Spfm(::land, "and"))
	regist(genv, "or", Spfm(::lor, "or"))
	regist(genv, "!", Spfm(
				{env: ICons, args: ICons ->
				leval(lapply(leval(car(args), env), cdr(args) as ICons), env)}, "!"))
	regist(genv, "do", Spfm(::ldo, "do"))
	regist(genv, "catch", Spfm(::lcatch, "catch"))
	regist(genv, "environment",  Spfm({env: ICons, args: ICons -> env}, "environment"))
	return genv
}

var genv = make_genv()

