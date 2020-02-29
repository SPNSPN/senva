const nil = false;
const t = true;

const Symb = function (name_)
{
	this.name = name_;
};

const Cons = function (a, d)
{
	this.car = a;
	this.cdr = d;
	this.type = "cons";
};

let genv = new Cons(nil, nil);

const cons = function (a, d)
{
	return new Cons(a, d);
};

const car = function (o)
{
	if (isnil(o)) { return nil; }
	return o.car;
};

const cdr = function (o)
{
	if (isnil(o)) { return nil; }
	return o.cdr;
};

const eq = function (a, b)
{
	if (a === b) { return t; }
	return nil;
};

const equal = function (a, b)
{
	if (a == b) { return t; }
	return nil;
};

const atom = function (o)
{
	if (consp(o)) { return nil; }
	return t;
};

const lread = function (code)
{
	return nil; // TODO
};

const lreadtop = function (code)
{
	return nil; // TODO
};

const leval = function (expr)
{
	return expr; // TODO
};

const lprint = function (expr)
{
	return "nil"; // TODO
};

