let typeid = {}
let typeid.nil = 6
let typeid.symb = 7
let typeid.cons = 8
let typeid.queu = 9
let typeid.func = 10
let typeid.spfm = 11
let typeid.erro = 12

let erroid = {}
let erroid.FullMemory = 0
let erroid.OutOfEnvironment = 2
let erroid.Type = 3
let erroid.Symbol = 4
let erroid.Syntax = 5
let erroid.UnCallable = 6
let erroid.ArgsUnmatch = 7
let erroid.UnEvaluatable = 8
let erroid.FileNotFound = 9

function! Nil () abort
	let self = {}
	let self.type = g:typeid.nil
	let self.a = self
	let self.d = self
	return self
endfunction

function! Symb (name) abort
	let self = {}
	let self.type = g:typeid.symb
	let self.name = a:name
	return self
endfunction

function! Cons (a, d) abort
	let self = {}
	let self.type = g:typeid.cons
	let self.a = a:a
	let self.d = a:d
	return self
endfunction

let nil = Nil()
let t = Symb("T")

let typetable = {}
let typetable[type(0)] = Symb("<Inum>")
let typetable[type("")] = Symb("<Strn>")
let typetable[type(function("tr"))] = Symb("<Subr>")
let typetable[type([])] = Symb("<Vect>")
let typetable[type({})] = Symb("<vim dict>")
let typetable[type(0.0)] = Symb("<Fnum>")
let typetable[typeid.nil] = Symb("<Nil>")
let typetable[typeid.symb] = Symb("<Symb>")
let typetable[typeid.cons] = Symb("<Cons>")
let typetable[typeid.queu] = Symb("<Queu>")
let typetable[typeid.func] = Symb("<Func>")
let typetable[typeid.spfm] = Symb("<Spfm>")
let typetable[typeid.erro] = Symb("<Erro>")

function! Ltype (o) abort
	let typ = type(a:o)
	if type({}) == typ
		return g:typetable[get(a:o, "type", type({}))]
	endif
	return g:typetable[typ]
endfunction


function! Car (c) abort
	return a:c.a
endfunction

function! Cdr (c) abort
	return a:c.d
endfunction

function! Atom (o) abort
	return (Ltype(a:o).name != "<Cons>") ? g:t : g:nil
endfunction

function! Eq (a, b) abort
	return (a:a is a:b) ? g:t : g:nil
endfunction



function! Lthrow (eid, emess) abort
	throw "," . a:eid . "," . a:emess
endfunction


" TODO
function! Inumable (str)
endfunction

function! Fnumable (str)
endfunction

function! Growth (tree, buff) abort
	let buf = a:buff[0]
	let rmacs = a:buff[1]
	if buf
		let a:buff[0] = ""
		let a:buff[1] = g:nil
		if "nil" == buf || "NIL" == buf
			return Cons(WrapReadmacros(g:nil, rmacs), a:tree)
		endif
		if Inumable(buf)
			return Cons(WrapReadmacros(str2nr(buf, 10), rmacs), a:tree)
		endif
		if Fnumable(buf)
			return Cons(WrapReadmacros(str2float(buf), rmacs), a:tree)
		endif
		return Cons(WrapReadmacros(Symb(buf), rmacs), a:tree)
	endif
	return a:tree
endfunction

function! FindCoParen (code)
endfunction

function! FindCoBracket (code)
endfunction

let escape_char_table = {}

function! TakeString (code)
endfunction


function! Lread (code) abort
	let tree = g:nil
	let buff = ["", g:nil]
	for idx in range(len(a:code))
		let c = a:code[idx]
		if "(" == c
			let co = FindCoParen(strpart(a:code, idx + 1))
			let tree = Growth(tree, buff)
			let tree = Cons(WrapReadmacros(Lread(strpart(a:code, idx + 1, co)), buff[1]), tree)
			let buff = ["", g:nil]
			let idx += co + 1
		elseif ")" == c
			Lthrow(g:erroid.Syntax, "found excess close parenthesis.")
		elseif "[" == c
			let co = FindCoBracket(strpart(a:code, idx + 1))
			let tree = Growth(tree, buff)
			let invec = Lread(strpart(a:code, idx + 1, co))
			let tree = Cons(buff[1] ? l(Symb("to-vect"), WrapReadmacros(invec, buff[1])) : Cons(Symb("vect"), invec), tree)
			let buff = ["", g:nil]
			let idx += co + 1
		elseif "]" == c
			Lthrow(g:erroid.Syntax, "found excess close brackets.")
		elseif " " == c || "\t" == c || "\n" == c
			let tree = Growth(tree, buff)
		elseif ";" == c
			let tree = Growth(tree, buff)
			while idx < len(a:code) && "\n" != a:code[idx]
				let idx += 1
			endwhile
		elseif '"' == c
			let tree = Growth(tree, buff)
			let res = TakeString(strpart(a:code, idx + 1))
			let idx += res.inc
			let tree = Cons(res.strn, tree)
			let buff = ["", g:nil]
		elseif "'" == c
			let tree = Growth(tree, buff)
			let buff[1] = Cons(Symb("quote"), buff[1])
		elseif "`" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Symb("quasiquote"), buff[1])
		elseif "," == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Symb("unquote"), buff[1])
		elseif "@" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Symb("splicing"), buff[1])
		elseif "^" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Symb("tee"), buff[1])
		elseif "." == c
			if buff[0]
				let buff[0] += "."
			else
				return Nconc(Nreverse(Cdr(tree)), Cons(Car(tree), Car(Lread(strpart(a:code, idx + 1)))))
			endif
		else
			let buff[0] += c
		endif
	endfor
	let tree = Growth(tree, buff)
	return Nreaverse(tree)
endfunction

function! Lprint (expr) abort
	let typ = Ltype(a:expr).name
	if "<Inum>" == typ || "<vim dict>" == typ || "<Fnum>" == typ
		return a:expr
	endif
	if "<Strn>" == typ
		return '"' . a:expr . '"'
	endif
	if "<Subr>" == typ
		return "<Subr " . a:expr . ">"
	endif
	if "<Vect>" == typ
		return "[" . join(map(a:expr, 'Lprint(v:val)'), " ") . "]"
	endif
	if "<Nil>" == typ
		return "NIL"
	endif
	if "<Symb>" == typ
		return a:expr.name
	endif
	if "<Cons>" == typ
		" TODO printcons_rec
		return "(" . Lprint(Car(a:expr)) . " . " . Lprint(Cdr(a:expr)) . ")"
	endif
	if "<Queu>" == typ
" TODO
	endif
	if "<Func>" == typ
" TODO
	endif
	if "<Spfm>" == typ
" TODO
	endif
	if "<Erro>" == typ
" TODO
	endif
endfunction

