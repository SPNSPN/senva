const nil = false;
const t = true;

function Symb (name_)
{
	this.name = name_;
}

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
		if (isnil(this.entr))
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
		if (isnil(this.exit)) { return nil; }

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
		if (isnil(this.entr))
		{
			this.entr = queu.entr;
			this.exit = queu.exit;
		}
		else if (! isnil(queu))
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
	constructor (eid, emess)
	{
		super(emess);
		this.eid = eid;
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
	if (isnil(o)) { return nil; }
	return o.car;
}

function cdr (o)
{
	if (isnil(o)) { return nil; }
	return o.cdr;
}

function eq (a, b)
{
	if (a === b) { return t; }
	return nil;
}

function equal (a, b)
{
	let cond = false;
	if (a instanceof Symb && b instanceof Symb)
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
		cond = (a == b);
	}

	if (cond) { return t; }
	return nil;
}

function atom (o)
{
	if (o instanceof Cons) { return nil; }
	return t;
}

function isnil (o)
{
	if (o === nil) { return t; }
	return nil;
}

function l ()
{
	return arguments.reverse().reduce((acc, e) => cons(e, acc), nil);
}

function append (colla, collb)
{
	let app = collb;
	for (let rest = reverse(colla); ! isnil(rest); rest = cdr(rest))
	{
		app = cons(car(rest), app);
	}
	return app;
}

function reverse (coll)
{
	let rev = nil;
	let rest = coll;
	for (let rest = coll; ! isnil(rest); rest = cdr(rest))
	{
		let e = car(rest);
		rev = cons(e, rev);
	}
	return rev;
}

function find (val, coll)
{
	for (let rest = coll; ! isnil(rest); rest = cdr(rest))
	{
		if (equal(val, car(rest))) { return t; }
	}
	return nil;
}

function findidx_eq (val, coll)
{
	for (let idx = 0, rest = coll; ! isnil(rest); ++idx, rest = cdr(rest))
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

		if (layer < 1)
		{
			return idx;
		}
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

function wrap_readmacros (tree, rmacs)
{
	let wraped = tree;
	let rest = rmacs;
	for (let rest = rmacs; ! isnil(rest); rest = cdr(rest))
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
			tree = growth(tree, buff);
			let co = find_co_paren(code.substring(idx + 1));
			tree = cons(wrap_readmacros(lread(code.substring(idx + 1, idx + co + 1))
						, buff[1]), tree);
			buff = ["", nil];
			idx += co + 1
		}
		else if (")" == c)
		{
			throw new Erro(ErrorId.Syntax, "found excess close parenthesis.");
		}
		else if ("[" == c)
		{
			tree = growth(tree, buff);
			let co = find_co_bracket(code.substring(idx + 1));
			buff[0] = lread(code.substring(idx + 1, idx + co + 1));
			if (buff[1])
			{
				tree = cons(l(new Symb("to-vect"), wrap_readmacros(buff[0], buff[1]))
						, tree);
			}
			else
			{
				tree = cons(cons(new Symb("vect"), buff[0]), tree);
			}
			buff = ["", nil];
			idx += co + 1;
		}
		else if ("]" == c)
		{
			throw new Erro(ErroId.Syntax, "found excess close brackets.");
		}
		else if (" " == c)
		{
			tree = growth(tree, buff);
		}
		else if (";" == c)
		{
			tree = growth(tree, buff);
			for (; idx < code.length && "\n" != code.charAt(idx); ++idx) { ; }
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
		else if ("." == c)
		{
			if (buff[0])
			{
				buff[0] += ".";
			}
			else
			{
				return append(reverse(cdr(tree)), cons(car(tree)
							, car(lread(code.substring(idx + 1)))));
			}
		}
		else
		{
			buff[0] += c
		}
	}
	tree = growth(tree, buff);
	return reverse(tree);
}

function lreadtop (code)
{
	return cons(new Symb("do"), lread(code));
}

function leval (expr)
{
	return expr; // TODO
}

function lprint (expr)
{
	let dup = seek_dup(expr, nil, nil);
	let s = "";
	for (let idx = 0, rest = dup; ! isnil(rest); ++idx, rest = cdr(rest))
	{
		s += `\$${idx} = ${lprint_rec(car(rest), dup, false)}\n`;
	}
	s += lprint_rec(expr, dup, true);
	return s;
}

function seek_dup (expr, printed,  dup)
{
	if (find(expr, printed)) { return cons(expr, dup); }
	if (atom(expr)) { return dup; }
	let pd = cons(expr, printed);
	return append(seek_dup(car(expr), pd, dup), seek_dup(cdr(expr), pd, dup));
}

function lprint_rec (expr, dup, rec)
{
	let idx = findidx_eq(expr, dup);
	if (rec && ! isnil(idx)) { return `\$${idx}`; }
	if (isnil(expr)) { return "NIL"; }
	if (atom(expr))
	{
		if (expr instanceof Symb)
		{
			return expr.name;
		}
		if (typeof expr == "string" || expr instanceof String)
		{
			return `\"${expr}\"`;
		}
		if (Array.isArray(expr))
		{
			return `[${expr.map(lprint).join(" ")}]`;
		}
		if (expr instanceof Queu)
		{
			return `/${lprint(expr.exit)}/`;
		}
		if (expr instanceof Func)
		{
			return `<Func ${lprint(expr.args)} ${lprint(expr.body)}>`;
		}
		if (expr instanceof Spfm)
		{
			return `<Spfm ${expr.name}>`;
		}
		if (typeof expr == "function" || expr instanceof Function)
		{
			return `<Subr ${expr.name}>`;
		}
		return expr.toString();
	}
	else
	{
		return printcons_rec(expr, dup, true);
	}

}

function printcons_rec (coll, dup, rec)
{
	let a = car(coll);
	let d = cdr(coll);
	if (isnil(d)) { return `(${lprint_rec(a, dup, rec)})`; }
	if (atom(d))
	{
		return `(${lprint_rec(a, dup, rec)} . ${lprint_rec(d, dup, rec)})`;
	}
	if (isnil(findidx_eq(d, dup)))
	{
		return `(${lprint_rec(a, dup, rec)} ${lprint_rec(d, dup, rec).slice(1)}`;
	}
	return `(${lprint_rec(a, dup, rec)} ${lprint_rec(d, dup, rec)})`;
}

