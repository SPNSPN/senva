function Symb (name_)
{
	this.name = name_;
}

const nil = false;
const NIL = nil;
const t = new Symb("T");
const T = t;

function Cons (a, d)
{
	this.car = a;
	this.cdr = d;
}

function Queu ()
{
	this.entr = nil;
	this.exit = this.entr;

	this.push = function (val)
	{
		let c = new Cons(val, nil);
		if (this.entr === nil)
		{
			this.entr = c;
			this.exit = c;
		}
		else
		{
			rplacd(this.entr, c);
			this.entr = c;
		}
		return this;
	};

	this.pop = function ()
	{
		if (this.exit === nil) { return nil; }

		let e = car(this.exit);
		if (this.exit === this.entr)
		{
			this,exit = nil;
			this.entr = nil;
		}
		else
		{
			this.exit = cdr(this.exit);
		}
		return e;
	};

	this.concat = function (queu)
	{
		if (this.entr === nil)
		{
			this.entr = queu.entr;
			this.exit = queu.exit;
		}
		else if (queu instanceof Queu)
		{
			rplacd(this.entr, queu.exit);
		}
		return this;
	};
}


function Spfm (proc, name)
{
	this.proc = proc;
	this.name = name;
}

function Func (args, body, env)
{
	this.args = args;
	this.body = body;
	this.env = env;
}

class Erro extends Error
{
	constructor (eid, estr)
	{
		super(estr);
		this.eid = eid;
		this.estr = estr;
	}
}

const ErroId =
{
	FullMemory: 0
	, UnknownOpcode: 1
	, OutOfEnvironment: 2
	, Type: 3
	, Symbol: 4
	, Syntax: 5
	, UnCallable: 6
	, ArgsUnmatch: 7
	, UnEvaluatable: 8
	, FileNotFound: 9
};

let genv = new Cons(nil, nil);

function cons (a, d)
{
	return new Cons(a, d);
}

function car (o)
{
	return (o === nil) ? nil : o.car;
}

function cdr (o)
{
	return (o === nil) ? nil : o.cdr;
}

function atom (o)
{
	return (o instanceof Cons) ? nil : t;
}

function eq (a, b)
{
	if (a === b) { return t; }
	if ((a instanceof Symb) && (b instanceof Symb) && (a.name == b.name)) { return t; }
	return nil;
}

function equal (a, b)
{
	let cond = false;
	if (a === b)
	{
		cond = true
	}
	else if (a instanceof Symb && b instanceof Symb)
	{
		cond = (a.name == b.name);
	}
	else if (a instanceof Cons && b instanceof Cons)
	{
		cond = (equal(a.car, b.car) && equal(a.cdr, b.cdr));
	}
	else if (a instanceof Queu && b instanceof Queu)
	{
		cond = equal(a.exit, b.exit);
	}
	else if (a instanceof Func && b instanceof Func)
	{
		cond = (equal(a.args, b.args)
				&& equal(a.body, b.body)
				&& equal(a.env, b.env));
	}
	else if (a instanceof Spfm && b instanceof Spfm)
	{
		cond = (equal(a.proc, b.proc)
				&& equal(a.name, b.name));
	}
	else if (Array.isArray(a) && Array.isArray(b))
	{
		if (a.length == b.length)
		{
			cond = true;
			for (let idx = 0; idx < a.length; ++idx)
			{
				if (! equal(a[idx], b[idx]))
				{
					cond = false;
					break;
				}
			}
		}
	}
	else
	{
		cond = ((typeof a == typeof b) && (a == b));
	}

	return cond ? t : nil;
}

function rplaca (c, v)
{
	c.car = v;
}

function rplacd (c, v)
{
	c.cdr = v;
}

function last (o)
{
	if (o instanceof Cons)
	{
		let rest = o;
		for (; ! atom(cdr(rest)); rest = cdr(rest)) { ; }
		return rest;
	}
	if (o === nil) { return nil; }
	if (o instanceof Queu) { return o.entr; }
	if (o instanceof Symb) { return new Symb(o.name.slice(-1)); }
	if (o instanceof String
			|| (typeof o) == "string") { return o.slice(-1); }
	if (Array.isArray(o)) { return [o[o.length - 1]]; }
	throw new Erro(ErroId.Type, `cannot apply last to ${lprint(o)}`);
}

function nconc (colla, collb)
{
	if (atom(colla)) { return collb; }
	let las = last(colla);
	rplacd(las, collb);
	return colla;
}

function nreverse (coll)
{
	let rev = nil;
	while (! atom(coll))
	{
		let tmp = cdr(coll);
		rplacd(coll, rev);
		rev = coll;
		coll = tmp;
	}
	return rev;
}

function vect ()
{
	return Array.from(arguments);
}

function queu ()
{
	let q = new Queu();
	q.exit = vect2cons(Array.from(arguments));
	q.entr = last(q.exit);
	return q;
}

function pushqueu (q, o)
{
	return q.push(o);
}

function popqueu (q)
{
	return q.pop();
}

function concqueu (qa, qb)
{
	return qa.concat(qb);
}

function to_list (obj)
{
	if (Array.isArray(obj)) { return vect2cons(obj); }
	if (obj instanceof Symb) { return vect2cons(to_vect(obj.name)); } 
	if (obj instanceof String || (typeof obj) == "string") {
		return vect2cons(to_vect(obj)); }
	if (obj instanceof Queu) { return obj.exit; }
	if (obj instanceof Cons) { return obj; }
	if (obj === nil) { return obj; }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(obj)} to ConsT.`);
}

function to_vect (obj)
{
	if (obj instanceof Cons) { return cons2vect(obj); }
	if (obj instanceof Symb) {
		return obj.name.split("").map(function (c) { return c.charCodeAt(0); }); }
	if (obj instanceof String || (typeof obj) == "string") {
		return obj.split("").map(function (c) { return c.charCodeAt(0); }); }
	if (obj instanceof Queu) { return cons2vect(obj.exit); }
	if (Array.isArray(obj)) { return obj; }
	if (obj === nil) { return []; }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(obj)} to VectT.`);
}

function to_queu (obj)
{
	if (obj instanceof Cons)
	{
		let q = new Queu();
		q.exit = obj;
		q.entr = last(obj);
		return q;
	}
	if (obj instanceof Symb) { return queu.apply(null, to_vect(obj.name)); }
	if (obj instanceof String || (typeof obj) == "string") {
		return queu.apply(null, to_vect(obj)); }
	if (Array.isArray(obj)) { return queu.apply(null, obj); }
	if (obj instanceof Queu) { return obj; }
	if (obj === nil) { return new Queu(); }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(obj)} to QueuT.`);
}

function symbol (obj)
{
	if (obj instanceof Cons) {
		return new Symb(String.fromCharCode.apply(null, cons2vect(obj))); }
	if (obj instanceof Queu) {
		return new Symb(String.fromCharCode.apply(null, cons2vect(obj.exit))); }
	if (Array.isArray(obj)) {
		return new Symb(String.fromCharCode.apply(null, obj)); }
	if (obj instanceof String || (typeof obj) == "string") { return new Symb(obj); }
	if (obj instanceof Symb) { return obj; }
	if (obj === nil) { return new Symb(""); }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(obj)} to SymbT.`);
}

function sprint ()
{
	return Array.from(arguments).reduce(function (strn, e)
			{
				return (e instanceof String || (typeof e) == "string") ?
					 strn + e : strn + lprint(e);
			}, "");
}

function l ()
{
	return Array.from(arguments).reduceRight((acc, e) => cons(e, acc), nil);
}

function mapeval (args, env)
{
	let eargs = nil;
	for (let rest = args; ! atom(rest); rest = cdr(rest))
	{
		eargs = cons(leval(car(rest), env), eargs);
	}
	return nreverse(eargs);
}


function reverse (coll)
{
	let rev = nil;
	for (let rest = coll; ! atom(rest); rest = cdr(rest))
	{
		rev = cons(car(rest), rev);
	}
	return rev;
}

function find (val, coll)
{
	for (let rest = coll; ! atom(rest); rest = cdr(rest))
	{
		if (equal(val, car(rest))) { return t; }
	}
	return nil;
}

function findidx_eq (val, coll)
{
	for (let idx = 0, rest = coll; ! atom(rest); ++idx, rest = cdr(rest))
	{
		if (val === car(rest)) { return idx; }
	}
	return nil;
}

function rplaca (c, o)
{
	c.car = o;
	return c;
}

function rplacd (c, o)
{
	c.cdr = o;
	return c;
}

function add ()
{
	let nums = Array.from(arguments);
	return nums.reduce(function (acc, n)
			{
				if (! Number.isFinite(n))
				{
					throw new Erro(ErroId.Type
							, `cannot add ${lprint(vect2cons(nums))}`);
				}
				return acc + n;
			}
			, 0);
}

function sub (head)
{
	let nums = Array.from(arguments).slice(1);
	if (! Number.isFinite(head)) { throw new Erro(ErroId.Type
			, `cannot sub ${lprint(cons(head, vect2cons(nums)))}`); }
	return nums.reduce(function (acc, n)
			{
				if (! Number.isFinite(n))
				{
					throw new Erro(ErroId.Type
							, `cannot sub ${lprint(cons(head, vect2cons(nums)))}`);
				}
				return acc - n;
			}
			, head);
}

function mul ()
{
	let nums = Array.from(arguments);
	return nums.reduce(function (acc, n)
			{
				if (! Number.isFinite(n))
				{
					throw new Erro(ErroId.Type
							, `cannot mul ${lprint(vect2cons(nums))}`);
				}
				return acc * n;
			}
			, 1);
}

function div (head)
{
	let nums = Array.from(arguments).slice(1);
	if (! Number.isFinite(head)) { throw new Erro(ErroId.Type
			, `cannot div ${lprint(cons(head, vect2cons(nums)))}`); }
	return nums.reduce(function (acc, n)
			{
				if (! Number.isFinite(n))
				{
					throw new Erro(ErroId.Type
							, `cannot div ${lprint(cons(head, vect2cons(nums)))}`);
				}
				return acc / n;
			}
			, head);
}

function mod (a, b)
{
	if (! Number.isFinite(a) || ! Number.isFinite(b))
	{
		throw new Erro(ErroId.Type, `cannot mod ${lprint(l(a, b))}`);
	}
	return a % b;
}

function gt (head)
{
	let nums = Array.from(arguments).slice(1);
	let a = head;
	for (let idx = 0; idx < nums.length; ++idx)
	{
		if (a > nums[idx]) { a = nums[idx]; }
		else { return nil; }
	}
	return t;
}

function ge (head)
{
	let nums = Array.from(arguments).slice(1);
	let a = head;
	for (let idx = 0; idx < nums.length; ++idx)
	{
		if (a < nums[idx]) { return nil; }
		a = nums[idx];
	}
	return t;
}

function lt (head)
{
	let nums = Array.from(arguments).slice(1);
	let a = head;
	for (let idx = 0; idx < nums.length; ++idx)
	{
		if (a < nums[idx]) { a = nums[idx]; }
		else { return nil; }
	}
	return t;
}

function le (head)
{
	let nums = Array.from(arguments).slice(1);
	let a = head;
	for (let idx = 0; idx < nums.length; ++idx)
	{
		if (a > nums[idx]) { return nil; }
		a = nums[idx];
	}
	return t;
}

function lint (n)
{
	if (Number.isFinite(n)) { return Math.trunc(n); }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(n)} to InumT.`);
}

function lfloat (n)
{
	if (Number.isFinite(n)) { return n; }
	throw new Erro(ErroId.Type, `cannot cast ${lprint(n)} to FnumT.`);
}

function ltype (o)
{
	if (o instanceof Cons) { return new Symb("<cons>"); }
	if (o instanceof Func) { return new Symb("<func>"); }
	if (o instanceof Spfm) { return new Symb("<spfm>"); }
	if (o instanceof Function || (typeof o) == "function") {
		return new Symb("<subr>"); }
	if (o instanceof Symb) { return new Symb("<symb>"); }
	if (o instanceof String || (typeof o) == "string") { return new Symb("<strn>"); }
	if (Number.isFinite(o))
	{
		if (Number.isInteger(o)) { return new Symb("<inum>"); }
		return new Symb("<fnum>");
	}
	if (o === nil) { return new Symb("<nil>"); }
	if (Array.isArray(o)) { return new Symb("<vect>"); }
	if (o instanceof Queu) { return new Symb("<queu>"); }
	return new Symb(`<js ${typeof o}>`);
}


function lif (env, args)
{
	if (leval(car(args), env) === nil)
	{
		return leval(car(cdr(cdr(args))), env);
	}
	return leval(car(cdr(args)), env);
}

function llambda (env, args)
{
	return new Func(car(args), car(cdr(args)), env);
}

function ldefine (env, args)
{
	let sym = car(args);
	let asc = assoc(car(genv), sym);
	if (asc === nil)
	{
		rplaca(genv, cons(cons(sym, leval(car(cdr(args)), env)), car(genv)));
	}
	else
	{
		rplacd(asc, leval(car(cdr(args)), env));
	}
	return sym;
}

function lsetq (env, args)
{
	let sym = car(args);
	for (let rest = env; ! atom(rest); rest = cdr(rest))
	{
		let asc = assoc(car(rest), sym);
		if (asc !== nil)
		{
			rplacd(asc, leval(car(cdr(args)), env));
			return cdr(asc);
		}
	}
	throw new Erro(ErroId.Symbol, `${lprint(sym)} is not defined.`);
}

function land (env, args)
{
	let ret = t;
	for (let rest = args; ! atom(rest); rest = cdr(rest))
	{
		ret = leval(car(rest), env);
		if (ret === nil) { return nil; }
	}
	return ret;
}

function lor (env, args)
{
	let ret = nil;
	for (let rest = args; ! atom(rest); rest = cdr(rest))
	{
		ret = leval(car(rest), env);
		if (! ret === nil) { return ret; }
	}
	return nil;
}

function ldo (env, args)
{
	for (let rest = args; ! atom(rest) && ! atom(cdr(rest)); rest = cdr(rest))
	{
		leval(car(rest), env);
	}
	return leval(car(rest), env);
}

function lsyntax (env, args)
{
	return leval(lapply(leval(car(args), env), cdr(args)), env);
}

function lload (path)
{
	if (! (path instanceof String) && (typeof path) != "string")
	{
		throw new Erro(ErroId.Type, `cannot apply load to ${lprint(path)}`);
	}
	let req = new XMLHttpRequest();	
	req.open("GET", `${location.origin}/${path}`, false);
	req.send(null);
	if (req.status == 200)
	{
		return leval(lreadtop(req.responseText), genv);
	}
	throw new Erro(ErroId.FileNotFound, `not found file: ${lprint(path)}`);
}


function expand_quasiquote (expr, env)
{
	if (atom(expr)) { return expr; }
	if (car(expr) instanceof Symb && car(expr).name == "unquote") {
		return leval(car(cdr(expr)), env); }
	let eexpr = nil;
	for (let rest = expr; ! atom(rest); rest = cdr(rest))
	{
		if (! atom(car(rest)) && car(car(rest)) instanceof Symb
				&& car(car(rest)).name == "splicing")
		{
			for (let sexpr = leval(car(cdr(car(rest))), env)
					; ! atom(sexpr); sexpr = cdr(sexpr))
			{
				eexpr = cons(car(sexpr), eexpr);
			}
		}
		else
		{
			eexpr = cons(expand_quasiquote(car(rest), env), eexpr);
		}
	}
	return nreverse(eexpr);
}

function inumable (str)
{
	return /^[+-]?\d+$/.test(str);
}

function fnumable (str)
{
	return /^[+-]?[0-9.]+$/.test(str);
}

function growth (tree, buff)
{
	let buf = buff[0];
	let rmacs = buff[1];
	if (buf)
	{
		buff[0] = "";
		buff[1] = nil;
		if ("nil" == buf || "NIL" == buf)
		{
			return cons(wrap_readmacros(nil, rmacs), tree);
		}
		if (inumable(buf))
		{
			return cons(wrap_readmacros(parseInt(buf, 10), rmacs), tree);
		}
		if (fnumable(buf))
		{
			return cons(wrap_readmacros(parseFloat(buf), rmacs), tree);
		}
		return cons(wrap_readmacros(new Symb(buf), rmacs), tree);
	}
	return tree;
}

function find_co_paren (code)
{
	let sflg = false;
	let layer = 1;
	for (let idx = 0; idx < code.length; ++idx)
	{
		let c = code.charAt(idx);
		if (! sflg && "(" == c)
		{
			++layer;
		}
		else if (! sflg && ")" == c)
		{
			--layer;
		}
		else if ("\\" == c)
		{
			++idx;
		}
		else if ("\"" == c)
		{
			sflg = ! sflg;
		}

		if (layer < 1) { return idx; }
	}
	throw new Erro(ErroId.Syntax, "not found close parenthesis.");
}

function find_co_bracket (code)
{
	let sflg = false;
	let layer = 1;
	for (let idx = 0; idx < code.length; ++idx)
	{
		let c = code.charAt(idx);
		if (! sflg && "[" == c)
		{
			++layer;
		}
		else if (! sflg && "]" == c)
		{
			--layer;
		}
		else if ("\\" == c)
		{
			++idx;
		}
		else if ("\"" == c)
		{
			sflg = ! sflg;
		}

		if (layer < 1) { return idx; }
	}
	throw new Erro(ErroId.Syntax, "not found close brackets.");
}

const escape_char_table = {
	"a": "\a"
	, "b": "\b"
	, "f": "\f"
	, "n": "\n"
	, "r": "\r"
	, "t": "\t"
	, "v": "\v"
	, "0": "\0"
};

function take_string (code)
{
	let strn = "";
	for (let idx = 0; idx < code.length; ++idx)
	{
		let c = code.charAt(idx);
		if ("\"" == c) { return {strn: strn, inc: idx + 1}; }
		if ("\\" == c)
		{
			++idx;
			c = code.charAt(idx);
			if (c in escape_char_table) { c = escape_char_table[c]; }
		}
		strn += c;
	}
	throw new Erro(ErroId.Syntax, "not found close double quote.");
}

function cons2vect (c)
{
	arr = [];
	for (let rest = c; ! atom(rest); rest = cdr(rest))
	{
		arr.push(car(rest));
	}
	return arr;
}

function vect2cons (l)
{
	return nreverse(l.reduce((acc, e) => cons(e, acc), nil));
}

function bind_tree (treea, treeb)
{
	if (treea)
	{
		if (atom(treea)) { return l(cons(treea, treeb)); }
		if (atom(treeb) && treeb)
		{
			throw new Erro(ErroId.Syntax
					, `cannot bind: ${lprint(treea)} and ${lprint(treeb)}`);
		}
		try
		{
			return nconc(bind_tree(car(treea), car(treeb))
					, bind_tree(cdr(treea), cdr(treeb)));
		}
		catch (erro)
		{
			if (erro instanceof Erro)
			{
				throw new Erro(ErroId.Syntax
						, `cannot bind: ${lprint(treea)} and ${lprint(treeb)}`);
			}
			throw erro;
		}
	}
	return nil;
}

function assoc (alist, key)
{
	for (let rest = alist; ! atom(rest); rest = cdr(rest))
	{
		let e = car(rest);
		if (equal(car(e), key)) { return e; }
	}
	return nil;
}

function assocdr (alist, key)
{
	let asc = assoc(alist, key);
	if (asc === nil) { return null; }
	return cdr(asc);
}

function seekenv (env, sym)
{
	for (let rest = env; ! atom(rest); rest = cdr(rest))
	{
		let val = assoc(car(rest), sym);
		if (val != nil)
		{
			return cdr(val);
		}
	}
	throw new Erro(ErroId.Symbol, `${lprint(sym)} is not defined.`);
}

function wrap_readmacros (o, rmacs)
{
	let wraped = o;
	for (let rest = rmacs; ! atom(rest); rest = cdr(rest))
	{
		wraped = l(car(rest), wraped);
	}
	return wraped;
}

function lread (code)
{
	let tree = nil;
	let buff = ["", nil];
	for (let idx = 0; idx < code.length; ++idx)
	{
		let c = code.charAt(idx);
		if ("(" == c)
		{
			let co = find_co_paren(code.slice(idx + 1));
			tree = growth(tree, buff)
			tree = cons(wrap_readmacros(lread(code.slice(idx + 1, idx + co + 1))
						, buff[1]), tree);
			buff = ["", nil];
			idx += co + 1
		}
		else if (")" == c)
		{
			throw new Erro(ErroId.Syntax, "found excess close parenthesis.");
		}
		else if ("[" == c)
		{
			let co = find_co_bracket(code.slice(idx + 1));
			tree = growth(tree, buff);
			invec = lread(code.slice(idx + 1, idx + co + 1));
			if (buff[1])
			{
				tree = cons(l(new Symb("to-vect")
							, wrap_readmacros(invec, buff[1])), tree);
			}
			else
			{
				tree = cons(cons(new Symb("vect"), invec), tree);
			}
			buff = ["", nil];
			idx += co + 1;
		}
		else if ("]" == c)
		{
			throw new Erro(ErroId.Syntax, "found excess close brackets.");
		}
		else if (" " == c || "\t" == c || "\n" == c)
		{
			tree = growth(tree, buff);
		}
		else if (";" == c)
		{
			tree = growth(tree, buff);
			for (; idx < code.length && "\n" != code.charAt(idx); ++idx) { ; }
		}
		else if ("\"" == c)
		{
			tree = growth(tree, buff);
			let res = take_string(code.slice(idx + 1));
			idx += res.inc;
			tree = cons(res.strn, tree);
			buff = ["", nil];
		}
		else if ("'" == c)
		{
			tree = growth(tree, buff);
			buff[1] = cons(new Symb("quote"), buff[1]);
		}
		else if ("`" == c)
		{
			tree = growth(tree, buff);
			buff[1] = cons(new Symb("quasiquote"), buff[1]);
		}
		else if ("," == c)
		{
			tree = growth(tree, buff);
			buff[1] = cons(new Symb("unquote"), buff[1]);
		}
		else if ("@" == c)
		{
			tree = growth(tree, buff);
			buff[1] = cons(new Symb("splicing"), buff[1]);
		}
		else if ("^" == c)
		{
			tree = growth(tree, buff);
			buff[1] = cons(new Symb("tee"), buff[1]);
		}
		else if ("." == c)
		{
			if (buff[0]) { buff[0] += "."; }
			else
			{
				return nconc(reverse(cdr(tree)), cons(car(tree)
							, car(lread(code.slice(idx + 1)))));
			}
		}
		else { buff[0] += c }
	}
	tree = growth(tree, buff);
	return nreverse(tree);
}

function lreadtop (code)
{
	return cons(new Symb("do"), lread(code));
}

function leval (expr, env)
{
	while (true)
	{
		if (expr instanceof Cons)
		{
			let args = cdr(expr);
			let proc = leval(car(expr), env);
			if (proc instanceof Func)
			{
				expr = proc.body;
				env = cons(bind_tree(proc.args, mapeval(args, env)), proc.env);
			}
			else if (proc instanceof Spfm)
			{
				if ("if" == proc.name)
				{
					expr = (leval(car(args), env) === nil)
						? car(cdr(cdr(args))) : car(cdr(args));
				}
				else if ("do" == proc.name)
				{
					let rest = args;
					for (; ! atom(cdr(rest)); rest = cdr(rest))
					{
						leval(car(rest), env);
					}
					expr = car(rest);
				}
				else if ("!" == proc.name)
				{
					expr = lapply(leval(car(args), env), cdr(args));
				}
				else
				{
					return proc.proc(env, args);
				}
			}
			else if (proc instanceof Function || (typeof proc) == "function")
			{
				return lapply(proc, mapeval(args, env));
			}
			else
			{
				throw new Erro(ErroId.UnCallable, `${lprint(proc)} is not callable.`);
			}
		}
		else if (expr instanceof Symb)
		{
			return seekenv(env, expr);
		}
		else
		{
			return expr;
		}
	}
}

function lapply (proc, args)
{
	if (proc instanceof Func)
	{
		return leval(proc.body, cons(bind_tree(proc.args, args), proc.env));
	}
	if (proc instanceof Function || (typeof proc) == "function")
	{
		return proc.apply(null, cons2vect(args));
	}
	throw new Erro(ErroId.UnCallable, `${lprint(proc)} is not callable.`);
}

function lthrow (eid, estr)
{
	throw new Erro(eid, estr)
}

function lcatch (env, args)
{
	let excep = leval(car(args), env);
	try
	{
		return leval(car(cdr(args)), env);
	}
	catch (erro)
	{
		if (erro instanceof Erro)
		{
			return lapply(excep, l(erro.eid, leval(erro.estr, env)));
		}
		throw erro;
	}
}

function lempty (coll)
{
	if (coll === nil) { return t; }
	if (Array.isArray(coll) || coll instanceof String || (typeof coll) == "string")
	{
		return (coll.length < 1) ? t : nil;
	}
	if (coll instanceof Queu)
	{
		return (coll.exit === nil) ? t : nil;
	}
	if (coll instanceof Symb)
	{
		return (coll.name.length < 1) ? t : nil;
	}
	return nil;
}

function llprin ()
{
	Array.from(arguments).map(function (a)
			{
				console.log((a instanceof String || (typeof a) == "string")
						? a : lprint(a));
			});
	return nil;
}

function llprint ()
{
	Array.from(arguments).map(function (e) { llprin(e); });
	console.log(" ");
	return nil;
}

function tee (obj)
{
	console.log(lprint(obj));
	return obj;
}

function attr (obj)
{
	let anames = Array.from(arguments).slice(1);
	let res = obj;
	return anames.reduce(function (acc, aname)
			{
				let at = acc[aname];
				if (at instanceof Function || (typeof at) == "function")
				{
					return function () { return at.apply(acc, arguments); };
				}
				else
				{
					return at;
				}
			}, res);
}

function lnew (cname)
{
	let args = Array.from(arguments);
	return new (Function.prototype.bind.apply(eval(cname), args));
}

function lgetat (vect, idx)
{
	if (Array.isArray(vect) || vect instanceof String || (typeof vect) == "string")
	{
		return vect[idx];
	}
	if (vect instanceof Symb)
	{
		return new Symb(vect.name[idx]);
	}
	throw new Erro(ErroId.Type, `cannot apply getat to ${lprint(vect)}`);
}

function lsetat (vect, idx, val)
{
	if (Array.isArray(vect))
	{
		vect[idx] = val;
		return vect;
	}
	if (vect instanceof String || (typeof vect) == "string")
	{
		if (Number.isInteger(val))
		{
			return vect.slice(0, idx) + String.fromCharCode(val)
				+ vect.slice(idx + 1);
		}
		if (val instanceof String || (typeof val) == "string")
		{
			return vect.slice(0, idx) + val[0] + vect.slice(idx + 1);
		}
		if (val instanceof Symb)
		{
			return vect.slice(0, idx) + val.name[0] + vect.slice(idx + 1);
		}
		throw new Erro(ErroId.Type, `cannot setat ${lprint(val)} to ${lprint(vect)}`);
	}
	if (vect instanceof Symb)
	{
		if (Number.isInteger(val))
		{
			vect.name = vect.name.slice(0, idx) + String.fromCharCode(val)
				+ vect.name.slice(idx + 1);
		}
		else if (val instanceof String || (typeof val) == "string")
		{
			vect.name = vect.name.slice(0, idx) + val[0] + vect.name.slice(idx + 1);
		}
		else if (val instanceof Symb)
		{
			vect.name = vect.name.slice(0, idx) + val.name[0]
				+ vect.name.slice(idx + 1);
		}
		else
		{
			throw new Erro(ErroId.Type
					, `cannot setat ${lprint(val)} to ${lprint(vect)}`);
		}
		return vect;
	}
	try
	{
		vect[idx] = val;
		return vect;
	}
	catch (e)
	{
		throw new Erro(ErroId.Type, `cannot apply setat to ${lprint(vect)}`);
	}
}

function lprint (expr)
{
	let dup = seek_dup(expr, nil, nil).dup;

	let s = "";
	let idx = 0;
	for (let rest = dup; ! atom(rest); rest = cdr(rest))
	{
		s += `\$${idx} = ${lprint_rec(car(rest), dup, false)}, `;
		++idx;
	}
	s += lprint_rec(expr,  dup, true);
	return s;
}

function seek_dup (expr, printed, dup)
{
	if (findidx_eq(expr, printed) !== nil)
	{
		if (findidx_eq(expr, dup) !== nil) { return {printed: printed, dup: dup}; }
		return {printed: printed, dup: cons(expr, dup)}
	}
	if (expr instanceof Cons)
	{
		let res = seek_dup(car(expr), cons(expr, printed), dup);
		return seek_dup(cdr(expr), res.printed, res.dup);
	}
	if (expr instanceof Queu)
	{
		return seek_dup(expr.exit, cons(expr, printed), dup);
	}
	if (Array.isArray(expr))
	{
		return expr.reduce(function (res, elm)
				{
					return seek_dup(elm, res.printed, res.dup);
				}, {printed: cons(expr, printed), dup: dup});
	}
	if (expr instanceof Erro)
	{
		return seek_dup(expr.estr, cons(expr, printed), dup);
	}
	return {printed: printed, dup: dup};
}

function lprint_rec (expr, dup, rec)
{
	let idx = findidx_eq(expr, dup);
	if (rec && idx !== nil) { return `\$${idx}`; }
	if (expr === nil) { return "NIL"; }
	if (expr instanceof Cons) { return printcons_rec(expr, dup, true); }
	if (expr instanceof Symb) { return expr.name; }
	if (expr instanceof String
			|| (typeof expr) == "string") { return `"${expr}"`; }
	if (Array.isArray(expr))
	{
		let s = expr.map(function (e)
				{ return lprint_rec(e, dup, true); }).join(" ");
		return `[${s}]`;
	}
	if (expr instanceof Queu) { return `/${lprint_rec(expr.exit, dup, true)}/`; }
	if (expr instanceof Func)
	{
		return `<Func ${lprint_rec(expr.args, dup, true)} ${lprint_rec(expr.body, dup, true)}>`;
	}
	if (expr instanceof Spfm) { return `<Spfm ${expr.name}>`; }
	if (expr instanceof Function || (typeof expr) == "function")
	{
		return `<Subr ${expr.name}>`;
	}
	return "" + expr;
}

function printcons_rec (coll, dup, rec)
{
	let a = car(coll);
	let d = cdr(coll);
	if (d === nil) { return `(${lprint_rec(a, dup, rec)})`; }
	if (atom(d))
	{
		return `(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})`;
	}
	if (findidx_eq(d, dup) !== nil)
	{
		return `(${lprint_rec(a,  dup, rec)} . ${lprint_rec(d, dup, rec)})`;
	}
	return `(${lprint_rec(a, dup, rec)} ${lprint_rec(d, dup, rec).slice(1)}`;
}


function regist (name, obj)
{
	rplaca(genv, cons(cons(new Symb(name), obj), car(genv)));
}

regist("nil", nil);
regist("t", t);
regist("T", t);
regist("cons", cons);
regist("car", car);
regist("cdr", cdr);
regist("eq", eq);
regist("equal", equal);
regist("atom", atom);
regist("list", l);
regist("+", add);
regist("-", sub);
regist("*", mul);
regist("/", div);
regist("%", mod);
regist(">", gt);
regist(">=", ge);
regist("<", lt);
regist("<=", le);
regist("int", lint);
regist("float", lfloat);
regist("rplaca", rplaca);
regist("rplacd", rplacd);
regist("last", last);
regist("nconc", nconc);
regist("nreverse", nreverse);
regist("vect", vect);
regist("queu", queu);
regist("pushqueu", pushqueu);
regist("popqueu", popqueu);
regist("concqueu", concqueu);
regist("to-list", to_list);
regist("to-vect", to_vect);
regist("to-queu", to_queu);
regist("symbol", symbol);
regist("sprint", sprint);
regist("apply", lapply);
regist("throw", lthrow);
regist("empty", lempty);
regist("print", llprint);
regist("prin", llprin);
regist("type", ltype);
regist("load", lload);
regist("getat", lgetat);
regist("setat", lsetat);
regist("processor", function () { return new Symb("javascript"); });
regist("tee", tee);
regist("js", eval);
regist("->", attr);
regist("new", lnew);
		
regist("quote", new Spfm(function (env, args) { return car(args); }, "quote"));
regist("quasiquote", new Spfm(function (env, args)
			{ return expand_quasiquote(car(args), env); }, "quasiquote"));
regist("if", new Spfm(lif, "if"));
regist("lambda", new Spfm(llambda, "lambda"));
regist("define", new Spfm(ldefine, "define"));
regist("setq", new Spfm(lsetq, "setq"));
regist("and", new Spfm(land, "and"));
regist("or", new Spfm(lor, "or"));
regist("environment", new Spfm(function (env, args) { return env; }, "environment"));
regist("!", new Spfm(lsyntax, "!"));
regist("do", new Spfm(ldo, "do"));
regist("catch", new Spfm(lcatch, "catch"));

