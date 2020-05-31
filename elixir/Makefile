PRJ:=interpreter
SRC:=src
BLD:=build

.PHONY: all script clean
all: $(BLD)/$(PRJ).beam

$(BLD)/$(PRJ).beam: $(SRC)/$(PRJ).ex
	elixirc -o $@ $<

script: $(SRC)/$(PRJ).ex
	elixir $<

