let typeid_nil = 6
let typeid_symb = 7
let typeid_cons = 8
let typeid_queu = 9
let typeid_func = 10
let typeid_spfm = 11
let typeid_erro = 12

function! Nil () abort
	let self = {}
	let self.type = g:typeid_nil
	let self.a = self
	let self.d = self
	return self
endfunction

function! Symb (name) abort
	let self = {}
	let self.type = g:typeid_symb
	let self.name = a:name
	return self
endfunction

function! Cons (a, d) abort
	let self = {}
	let self.type = g:typeid_cons
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
let typetable[typeid_nil] = Symb("<Nil>")
let typetable[typeid_symb] = Symb("<Symb>")
let typetable[typeid_cons] = Symb("<Cons>")
let typetable[typeid_queu] = Symb("<Queu>")
let typetable[typeid_func] = Symb("<Func>")
let typetable[typeid_spfm] = Symb("<Spfm>")
let typetable[typeid_erro] = Symb("<Erro>")

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
	return (a:a == a:b) ? g:t : g:nil
endfunction

function! Lread (code) abort
	let tree = g:nil;
	let buff = ["", nil];
	for idx in range(len(code))
		let c = code[idx]
		if "(" == c
			let co = find_co_paren(strpart(code, idx + 1))
			let tree = growth(tree, buff)
			let tree = cons(wrap_readmacros(lread(strpart(code, idx + 1, co)), buff[1]), tree)
			let buff = ["", g:nil]
			let idx += co + 1
		elseif ")" == c
			throw Erro(erroid.syntax, "found excess close parenthesis.")
		elseif "[" == c
			let co = find_co_bracket(strpart(code, idx + 1))
			let tree = growth(tree, buff)
			let invec = lread(strpart(code, idx + 1, co))
			let tree = cons(buff[1] ? l(Symb("to-vect"), wrap_readmacros(invec, buff[1])) : cons(Symb("vect"), invec), tree)
			let buff = ["", g:nil]
			letidx += co + 1
			" TODO
		endif
	endfor
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

