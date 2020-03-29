using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senva
{

class Interpreter
{
	public interface ICons
	{
		object car ();
		object cdr ();
	}

	public class Nil : ICons
	{
		public Nil () {}

		public object car () { return this; }
		public object cdr () { return this; }

		public static bool operator true (Nil o) { return false; }
		public static bool operator false (Nil o) { return false; }
	}

	public class Symb
	{
		public string name;

		public Symb (string name_) { this.name = name_; }
	}

	public class Cons : ICons
	{
		public object a;
		public object d;

		public Cons (object a_, object d_)
		{
			this.a = a_;
			this.d = d_;
		}

		public object car () { return this.a; }
		public object cdr () { return this.d; }

		public static bool operator true (Cons o) { return true; }
		public static bool operator false (Cons o) { return true; }
	}

	public class Queu
	{
		public ICons entr;
		public ICons exit;

		public Queu ()
		{
			this.entr = nil;
			this.exit = this.entr;
		}

		public Queu (ICons c)
		{
			this.entr = last(c) as ICons;
			this.exit = c;
		}

		public Queu push (object o)
		{
			var c = cons(o, nil);
			if (this.entr is Nil)
			{
				this.entr = c;
				this.exit = c;
			}
			else
			{
				rplacd(this.entr as Cons, c);
				this.entr = c;
			}
			return this;
		}
	}

	public enum ErroId
	{
		FullMemory
		, UnknownOpcode
		, OutOfEnvironment
		, Type
		, Symbol
		, Syntax
		, UnCallable
		, ArgsUnmatch
		, UnEvaluatable
		, FileNotFound
	}

	[Serializable()]
	public class Erro : System.Exception
	{
		public int eid;

		public Erro () : base() {}
		public Erro (int eid_, string estr) : base(estr) { this.eid = eid_; }
		public Erro (ErroId eid_, string estr) : base(estr) { this.eid = (int)eid_; }
		public Erro (int eid_, string estr, System.Exception inner)
			: base(estr, inner) { this.eid = eid_; }
		public Erro (ErroId eid_, string estr, System.Exception inner)
			: base(estr, inner) { this.eid = (int)eid_; }
		public Erro (System.Runtime.Serialization.SerializationInfo info
				, System.Runtime.Serialization.StreamingContext context) {}
	}

	public class Subr
	{
		public Func<ICons, object> proc;
		public string name;

		public Subr (Func<ICons, object> proc_, string name_)
		{
			this.proc = proc_;
			this.name = name_;
		}
	}

	public class Spfm
	{
		public Func<ICons, ICons, object> proc;
		public string name;

		public Spfm (Func<ICons, ICons, object> proc_, string name_)
		{
			this.proc = proc_;
			this.name = name_;
		}
	}

	public class LFunc
	{
		public ICons args;
		public ICons body;
		public ICons env;

		public LFunc (ICons args_, ICons body_, ICons env_)
		{
			this.args = args_;
			this.body = body_;
			this.env = env_;
		}
	}

	static public Nil nil;
	static public Symb t;
	static private Dictionary<char, char> escape_char_table;

	public Cons genv;


	static Interpreter ()
	{
		nil = new Nil();
		t = new Symb("T");
	
		escape_char_table = new Dictionary<char, char>
		{
			{'a', '\a'}
			, {'b', '\b'}
			, {'f', '\f'}
			, {'n', '\n'}
			, {'r', '\r'}
			, {'t', '\t'}
			, {'v', '\v'}
			, {'0', '\0'}
		};
	}

	public Interpreter ()
	{
		genv = new Cons(nil, nil);
		regist(genv, "nil", nil);
		regist(genv, "t", t);
		regist(genv, "T", t);
		regist_subr2<object, object, Cons>(genv, "cons", cons);
		regist_subr1<ICons, object>(genv, "car", car);
		regist_subr1<ICons, object>(genv, "cdr", cdr);
		regist_subr2<object, object, object>(genv, "eq", eq);
		regist_subr2<object, object, object>(genv, "equal", equal);
		regist_subr1<object, object>(genv, "atom", atom);
		regist(genv, "list", new Subr((ICons args) => { return args; }, "list"));

		regist(genv, "quote", new Spfm((ICons env, ICons args)
					=> { return car(args); }, "quote"));

		regist(genv, "do", new Spfm(ldo, "do"));

	}

	static private void regist (Cons env, string name, object obj)
	{
		rplaca(env, cons(cons(new Symb(name), obj), car(env)));
	}

	static private void regist_subr1<T, R> (Cons env, string name, Func<T, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T)(car(args)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private void regist_subr2<T1, T2, R> (Cons env, string name
			, Func<T1, T2, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T1)(car(args)), (T2)(nth(args, 1)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private void regist_subr3<T1, T2, T3, R> (Cons env, string name
			, Func<T1, T2, T3, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T1)(car(args))
									, (T2)(nth(args, 1))
									, (T3)(nth(args, 2)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private bool is_inum (object n)
	{
		return (n is int) || (n is uint) || (n is long) || (n is ulong)
			|| (n is byte) || (n is sbyte) || (n is short) || (n is ushort);
   	}

	static private bool is_fnum (object n)
	{
		return (n is float) || (n is double) || (n is decimal);
	}

	static public Cons cons (object a, object d) { return new Cons(a, d); }
	static public object car (ICons c) { return c.car(); }
	static public object cdr (ICons c) { return c.cdr(); }
	static public object atom (object o) { return (o is Cons) ? nil as object : t as object; }
	static public object eq (object a, object b)
	{
		return (ReferenceEquals(a, b)
				|| (a is Symb && b is Symb
					&& ((a as Symb).name == (b as Symb).name)))
			? t as object : nil as object;
	}

	static public object equal (object a, object b)
	{
		bool cond = false;
		if (ReferenceEquals(a, b)) { cond = true; }
		else if (a is Symb && b is Symb)
		{
			cond = ((a as Symb).name == (b as Symb).name);
		}
		else if (a is Cons && b is Cons)
		{
			cond = (! (equal((a as Cons).a, (b as Cons).a) is Nil
						|| equal((a as Cons).d, (b as Cons).d) is Nil));
		}
		else if (a is Queu && b is Queu)
		{
			cond = (equal((a as Queu).exit, (b as Queu).exit) is Nil) ? false : true;
		}
		else if (a is LFunc && b is LFunc)
		{
			cond = (! (equal((a as LFunc).args, (b as LFunc).args) is Nil
						|| equal((a as LFunc).body, (b as LFunc).body) is Nil
						|| equal((a as LFunc).env, (b as LFunc).env) is Nil));
		}
		else if (a is Spfm && b is Spfm)
		{
			cond = (! (equal((a as Spfm).proc, (b as Spfm).proc) is Nil
					|| equal((a as Spfm).name, (b as Spfm).name) is Nil));
		}
		else if (a.GetType().IsArray && b.GetType().IsArray)
		{
			if ((a as object[]).Length == (b as object[]).Length)
			{
				cond = true;
				for (var idx = 0; idx < (a as object[]).Length; ++idx)
				{
					if (equal((a as object[])[idx], (b as object[])[idx]) is Nil)
					{
						cond = false;
						break;
					}
				}
			}
		}
		else if ((is_inum(a) || is_fnum(a)) && (is_inum(b) || is_fnum(b)))
		{
			cond = (a == b);
		}
		else { cond = (a == b); }
		return (cond) ? t as object : nil as object;
	}

	static public Cons rplaca (Cons c, object o)
	{
		c.a = o;
		return c;
	}

	static public Cons rplacd (Cons c, object o)
	{
		c.d = o;
		return c;
	}

	static public object last (object o)
	{
		if (o is Cons)
		{
			ICons rest = o as Cons;
			for (; cdr(rest) is Cons; rest = cdr(rest) as Cons) {}
			return rest;
		}
		if (o is Nil) { return nil; }
		if (o is Queu) { return (o as Queu).entr; }
		if (o is Symb) { return new Symb((o as Symb).name[(o as Symb).name.Length - 1].ToString()); }
		if (o is string) { return (o as string)[(o as string).Length - 1].ToString(); }
		if (o.GetType().IsArray) { return new object[]{(o as object[])[(o as object[]).Length - 1]}; }
		throw new Erro(ErroId.Type, string.Format("cannot apply last to {0}", lprint(o)));
	}

	static public ICons nconc (ICons colla, ICons collb)
	{
		if (colla is Nil) { return collb; }
		var las = last(colla);
		rplacd(las as Cons, collb);
		return colla;
	}

	static public ICons nreverse (ICons coll)
	{
		ICons rev = nil;
		for (object rest = coll; rest is Cons; )
		{
			var tmp = cdr(rest as Cons);
			rplacd(rest as Cons, rev);
			rev = rest as Cons;
			rest = tmp;
		}
		return rev;
	}

	static public object nth (ICons c, int n)
	{
		object rest = c;
		for (var idx = 0; idx < n; ++idx)
		{
			if (rest is Cons) { rest = cdr(rest as Cons); }
			else
			{
				throw new Erro(ErroId.ArgsUnmatch
						, string.Format("cannot got nth {0} from {1}", n, lprint(c)));
			}
		}
		if (rest is ICons) { return car(rest as ICons); }
		throw new Erro(ErroId.ArgsUnmatch
				, string.Format("cannot got nth {0} from {1}", n, lprint(c)));
	}

	public object ldo (ICons env, ICons args)
	{
		object rest = args;
		for (; rest is Cons && cdr(rest as Cons) is Cons; rest = cdr(rest as Cons))
		{
			leval(car(rest as Cons), env);
		}
		return leval(car(rest as ICons), env);
	}

	static public ICons l (params object[] args)
	{
		return args.Reverse().Aggregate(nil
				, (ICons acc, object e) => { return cons(e, acc); });
	}

	private ICons mapeval (ICons args, ICons env)
	{
		ICons eargs = nil;
		for (object rest = args; rest is Cons; rest = cdr(rest as Cons))
		{
			eargs = cons(leval(car(rest as Cons), env), eargs);
		}
		return nreverse(eargs);
	}

	static private object findidx_eq (object o, ICons coll)
	{
		var idx = 0;
		for (object rest = coll; rest is Cons; rest = cdr(rest as Cons))
		{
			if (ReferenceEquals(o, car(rest as Cons))) { return idx; }
			++idx;
		}
		return nil;
	}


	static private ICons growth (ICons tree, ref string tok, ref ICons rmacs)
	{
		if (tok.Length == 0) { return tree; }

		object o;
		long inum;
		double fnum;

		if ("nil" == tok || "NIL" == tok) { o = nil; }
		else if (Int64.TryParse(tok, out inum)) { o = inum; }
		else if (Double.TryParse(tok, out fnum)) { o = fnum; }
		else { o = new Symb(tok); }

		var res = cons(wrap_readmacros(o, rmacs), tree);

		tok = "";
		rmacs = nil;

		return res;
	}

	static private int find_co_paren (string code)
	{
		var sflg = false;
		var eflg = false;
		var layer = 1;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg) { eflg = false; }
			else if (! sflg && '(' == c) { ++layer; }
			else if (! sflg && ')' == c) { --layer; }
			else if ('\\' == c) { eflg = true; continue; }
			else if ('"' == c) { sflg = ! sflg; }

			if (layer < 1) { return idx; }
		}
		throw new Erro(ErroId.Syntax, "not found close parenthesis.");
	}

	static private int find_co_bracket (string code)
	{
		var sflg = false;
		var eflg = false;
		var layer = 1;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg) { eflg = false; }
			else if (! sflg && '[' == c) { ++layer; }
			else if (! sflg && ']' == c) { --layer; }
			else if ('\\' == c) { eflg = true; continue; }
			else if ('"' == c) { sflg = ! sflg; }

			if (layer < 1) { return idx; }
		}
		throw new Erro(ErroId.Syntax, "not found close brackets.");
	}

	static private dynamic take_string (string code)
	{
		var eflg = false;
		var strn = "";
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg)
			{
				if (escape_char_table.ContainsKey(c)) { c = escape_char_table[c]; }
				eflg = false;
			}
			else if ('\\' == c)
			{
				eflg = true;
				continue;
			}
			else if ('"' == c) { return new {strn = strn, inc = idx + 1}; }
			strn += c;
		}
		throw new Erro(ErroId.Syntax, "not found close double quote.");
	}

	static private object[] cons2vect (ICons c)
	{
		var arr = new List<object>();
		for (object rest = c; rest is Cons; rest = cdr(rest as Cons))
		{
			arr.Add(car(rest as Cons));
		}
		return arr.ToArray();
	}

	static private ICons vect2cons (object[] l)
	{
		return l.Reverse().Aggregate(nil
				, (ICons acc, object e) => { return cons(e, acc); });
	}

	static private ICons bind_tree (object treea, object treeb)
	{
		if (treea is Nil) { return nil; }
		if (! (treea is Cons)) { return l(cons(treea, treeb)); }
		if (! (treeb is ICons))
		{
			throw new Erro(ErroId.Syntax
					, string.Format("cannot bind: {0} and {1}"
						, lprint(treea), lprint(treeb)));
		}
		try
		{
			return nconc(bind_tree(car(treea as Cons), car(treeb as Cons))
					, bind_tree(cdr(treea as Cons), cdr(treeb as Cons)));
		}
		catch (Erro)
		{
			throw new Erro(ErroId.Syntax
					, string.Format("cannot bind: {0} and {1}"
						, lprint(treea), lprint(treeb)));
		}
	}

	static public object assoc (ICons alist, object key)
	{
		for (object rest = alist; rest is Cons; rest = cdr(rest as Cons))
		{
			var e = car(rest as Cons);
			if (e is Cons && (! (equal(car(e as ICons), key) is Nil))) { return e; }
		}
		return null;
	}

	static public object assocdr (ICons alist, object key)
	{
		var res = assoc(alist, key);
		if (res == null) { return null; }
		return cdr(res as ICons);
	}
	
	static private object seekenv (ICons env, Symb sym)
	{
		for (object rest = env; rest is Cons; rest = cdr(rest as Cons))
		{
			var res = assocdr(car(rest as Cons) as ICons, sym);
			if (res != null) { return res; }
		}
		throw new Erro(ErroId.Symbol
				, string.Format("{0} is not defined.", lprint(sym)));
	}

	static private object wrap_readmacros (object o, ICons rmacs)
	{
		var wraped = o;
		for (object rest = rmacs; rest is Cons; rest = cdr(rest as Cons))
		{
			wraped = l(car(rest as Cons), wraped);
		}
		return wraped;
	}

	static public ICons lread (string code)
	{
		ICons tree = nil;
		var tok = "";
		ICons rmacs = nil;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if ('(' == c)
			{
				var co = find_co_paren(code.Substring(idx + 1));
				tree = growth(tree, ref tok, ref rmacs);
				tree = cons(wrap_readmacros(lread(code.Substring(idx + 1, co)), rmacs)
						, tree);
				tok = "";
				rmacs = nil;
				idx += co + 1;
			}
			else if (')' == c)
			{
				throw new Erro(ErroId.Syntax, "found excess close parenthesis.");
			}
			else if ('[' == c)
			{
				var co = find_co_bracket(code.Substring(idx + 1));
				tree = growth(tree, ref tok, ref rmacs);
				ICons invec = lread(code.Substring(idx + 1, co));
				tree = (rmacs is Nil)
					? cons(cons(new Symb("vect"), invec), tree)
					: cons(l(new Symb("to-vect"), wrap_readmacros(invec, rmacs)), tree);
				tok = "";
				rmacs = nil;
				idx += co + 1;
			}
			else if (']' == c)
			{
				throw new Erro(ErroId.Syntax, "found excess close brackets.");
			}
			else if (' ' == c || '\t' == c || '\n' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
			}
			else if (';' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				for (; idx < code.Length && '\n' != code[idx]; ++idx) {}
			}
			else if ('"' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				var res = take_string(code.Substring(idx + 1));
				idx += res.inc;
				tree = cons(res.strn, tree);
				tok = "";
				rmacs = nil;
			}
			else if ('\'' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("quote"), rmacs);
			}
			else if ('`' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("quasiquote"), rmacs);
			}
			else if (',' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("unquote"), rmacs);
			}
			else if ('@' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("splicing"), rmacs);
			}
			else if ('^' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("tee"), rmacs);
			}
			else if ('.' == c)
			{
				if (tok.Length == 0)
				{
					return nconc(nreverse(cdr(tree) as ICons)
							, cons(car(tree), car(lread(code.Substring(idx + 1)))));
				}
				tok += ".";
			}
			else { tok += c; }
		}
		tree = growth(tree, ref tok, ref rmacs);
		return nreverse(tree);
	}

	static public ICons lreadtop (string code)
	{
		return cons(new Symb("do"), lread(code));
	}

	public object leval (object expr_, ICons env_)
	{
		var expr = expr_;
		var env = env_;
		while (true)
		{
			if (expr is Cons)
			{
				var args = cdr(expr as Cons) as ICons;
				var proc = leval(car(expr as Cons), env);
				if (proc is LFunc)
				{
					expr = (proc as LFunc).body;
					env = cons(bind_tree((proc as LFunc).args, mapeval(args, env))
							, (proc as LFunc).env);
				}
				else if (proc is Spfm)
				{
					if ("if" == (proc as Spfm).name)
					{
						expr = nth(args, (leval(car(args), env) is Nil) ? 2 : 1);
					}
					else if ("do" == (proc as Spfm).name)
					{
						ICons rest = args;
						for (; cdr(rest) is Cons; rest = cdr(rest) as Cons)
						{
							leval(car(rest), env);
						}
						expr = car(rest);
					}
					else if ("!" == (proc as Spfm).name)
					{
						expr = lapply(leval(car(args), env), cdr(args) as ICons);
					}
					else { return (proc as Spfm).proc(env, args); }
				}
				else if (proc is Subr) { return lapply(proc, mapeval(args, env)); }
				else
				{
					throw new Erro(ErroId.UnCallable
							, string.Format("{0} is not callable.", lprint(proc)));
				}
			}
			else if (expr is Symb) { return seekenv(env, expr as Symb); }
			else { return expr; }
		}
	}

	public object lapply (object proc, ICons args)
	{
		if (proc is LFunc)
		{
			return leval((proc as LFunc).body
					, cons(bind_tree((proc as LFunc).args, args)
						, (proc as LFunc).env));
		}
		if (proc is Subr) { return (proc as Subr).proc(args); }
		throw new Erro(ErroId.UnCallable
				, string.Format("{0} is not callable.", lprint(proc)));
	}

	static public string lprint (object expr)
	{
		var dup = seek_dup(expr, nil, nil).dup;
		var s = "";
		var idx = 0;
		for (var rest = dup; rest is Cons; rest = cdr(rest as Cons))
		{
			s += string.Format("${0} = {1}\n"
					, idx, lprint_rec(car(rest as Cons), dup, false)); 
			++idx;
		}
		s += lprint_rec(expr, dup, true);
		return s;
	}

	static private dynamic seek_dup (object expr, ICons printed, ICons dup)
	{
		if (! (findidx_eq(expr, printed) is Nil))
		{
			if (findidx_eq(expr, dup) is Nil)
			{
				return new {printed = printed, dup = cons(expr, dup)};
			}
			return new {printed = printed, dup = dup};
		}
		if (expr is Cons)
		{
			var res = seek_dup(car(expr as Cons), cons(expr, printed), dup);
			return seek_dup(cdr(expr as Cons), res.printed, res.dup);
		}
		if (expr is Queu) { return seek_dup((expr as Queu).exit, cons(expr, printed), dup); }
		if (expr.GetType().IsArray)
		{
			return (expr as object[])
				.Aggregate(new {printed = cons(expr, printed), dup = dup}
					, (dynamic acc, object elm)
					=> { return seek_dup(elm, acc.printed, acc.dup); });
		}
		if (expr is Erro) { return seek_dup((expr as Erro).Message, cons(expr, printed), dup); }
		return new {printed = printed, dup = dup};
	}

	static private string lprint_rec (object expr, ICons dup, bool rec)
	{
		var idx = findidx_eq(expr, dup);
		if (rec && ! (idx is Nil)) { return string.Format("${0}", idx); }
		if (expr is Nil) { return "NIL"; }
		if (expr is Cons) { return printcons_rec(expr as Cons, dup, true); }
		if (expr is Symb) { return (expr as Symb).name; }
		if (expr is string) { return string.Format("\"{0}\"", expr); }
		if (expr.GetType().IsArray)
		{
			var s = (expr as object[]).Select(
					(object e) => { return lprint_rec(e, dup, true); });
			return string.Format("[{0}]", string.Join(" ", s));
		}
		if (expr is Queu)
		{ return string.Format("/{0}/", lprint_rec((expr as Queu).exit, dup, true)); }
		if (expr is LFunc)
		{
			return string.Format("<Func {0} {1}>",
				   	lprint_rec((expr as LFunc).args, dup, true)
					, lprint_rec((expr as LFunc).body, dup, true));
		}
		if (expr is Spfm) { return string.Format("<Spfm {0}>", (expr as Spfm).name); }
		if (expr is Subr) { return string.Format("<Subr {0}>", (expr as Subr).name); }
		return expr.ToString();
	}

	static private string printcons_rec (Cons coll, ICons dup, bool rec)
	{
		var a = car(coll);
		var d = cdr(coll);
		if (d is Nil) { return string.Format("({0})", lprint_rec(a, dup, rec)); }
		if (! (d is Cons))
		{
			return string.Format("({0} . {1})"
					, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec));
		}
		if (! (findidx_eq(d, dup) is Nil))
		{
			return string.Format("({0} . {1})"
					, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec));
		}
		return string.Format("({0} {1}"
				, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec).Substring(1));
	}
}
}

